using GameLike.Data.LevelBuilder;
using GameLike.LevelBuilder.Grid;
using UnityEngine;

namespace GameLike.LevelBuilder.Components
{
    /// <summary>
    /// Instância de mobília na cena (LEVEL_BUILDER.md §9): um GameObject
    /// data-driven — <b>não</b> um <c>TileBase</c> — que monta os
    /// <see cref="SpriteRenderer"/> das camadas visuais da sua
    /// <see cref="FurnitureDefinitionSO"/>, aplica tint nas camadas tintáveis e
    /// participa do <b>sort isométrico unificado</b> (Sorting Layer "Iso") pela
    /// posição de mundo do seu ponto de contato (§5.3, §6).
    ///
    /// Fase 3a entrega colocação 1×1 numa direção, usando o sprite único da
    /// camada; footprint multi-célula e rotação 4 direções são a Fase 3b. Sem
    /// arte de mobília ainda, camadas sem sprite renderizam um <i>placeholder</i>
    /// gerado em runtime, só para validar posição/sorting.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class FurnitureInstance : MonoBehaviour
    {
        /// <summary>Sorting Layer única do Level Builder (mesma de chão/parede) — sort unificado pelo eixo.</summary>
        public const string SortingLayerName = "Iso";

        /// <summary>
        /// Deslocamento mínimo em world Y para empilhar camadas INTRA-peça. Usa-se o eixo de
        /// profundidade (Y dominante), nunca <c>sortingOrder</c>: order é avaliado ANTES do eixo de
        /// transparência e faria a peça saltar acima/abaixo de chão/parede (mesmo bug do sort de
        /// parede). Sub-pixel a PPU 32, então não desloca visualmente.
        /// </summary>
        private const float LayerDepthNudge = 0.0008f;

        /// <summary>
        /// Viés mínimo de profundidade (world Z) para a mobília vencer o chão da própria célula no
        /// eixo de transparência custom (0,1,-0.26): Z maior → dot menor → desenhado por último (à
        /// frente). Pequeno o bastante para nunca inverter a ordem entre fileiras de células.
        /// </summary>
        private const float ContactSortBias = 0.05f;

        [SerializeField] private FurnitureDefinitionSO definition;
        [SerializeField] private Vector3Int anchorCell;
        [SerializeField] private IsoDirection direction;
        [SerializeField, Min(0)] private int storey;
        [SerializeField, Min(0)] private int baseLevel;

        /// <summary>Definição de mobília desta instância.</summary>
        public FurnitureDefinitionSO Definition => definition;
        /// <summary>Célula-âncora (ponto de contato) no grid do andar.</summary>
        public Vector3Int AnchorCell => anchorCell;
        /// <summary>Direção de orientação atual.</summary>
        public IsoDirection Direction => direction;
        /// <summary>Andar onde a peça vive.</summary>
        public int Storey => storey;
        /// <summary>Nível Z de apoio (altura inicial): 0 = plano do chão; aumenta para suspender a peça.</summary>
        public int BaseLevel => baseLevel;

        private static Sprite _placeholderSprite;

        private void OnEnable()
        {
            // Após um domain reload, o sprite placeholder (gerado em runtime, não serializável)
            // volta nulo nos renderers já existentes — reatribui sem recriar a hierarquia.
            if (definition == null || HasNoArt(definition))
                foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.sprite == null) sr.sprite = Placeholder();
        }

        /// <summary>Define os dados desta instância e (re)monta os renderers no grid dado.</summary>
        public void Configure(FurnitureDefinitionSO def, Vector3Int cell, IsoDirection dir, int storeyIndex, int baseLevelZ, UnityEngine.Grid grid)
        {
            definition = def;
            anchorCell = cell;
            direction  = dir;
            storey     = Mathf.Max(0, storeyIndex);
            baseLevel  = Mathf.Max(0, baseLevelZ);
            Rebuild(grid);
        }

        /// <summary>(Re)constrói os SpriteRenderer das camadas e reposiciona no ponto de contato da célula-âncora.</summary>
        public void Rebuild(UnityEngine.Grid grid)
        {
            ClearChildren();

            if (grid != null)
                transform.position = ContactPoint(grid) + IsoGrid.StoreyOffset(storey);

            var layers = definition != null ? definition.VisualLayers : null;
            int count  = layers != null ? layers.Length : 0;

            if (count == 0)
            {
                CreateLayerRenderer("Placeholder", Placeholder(), tintable: false, layerIndex: 0, isPlaceholder: true);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var layer        = layers[i];
                bool hasSprite   = layer.sprite != null;
                var sprite       = hasSprite ? layer.sprite : Placeholder();
                string layerName = string.IsNullOrEmpty(layer.layerName) ? $"Layer{i}" : layer.layerName;
                CreateLayerRenderer(layerName, sprite, layer.tintable, i, isPlaceholder: !hasSprite);
            }
        }

        private void CreateLayerRenderer(string layerName, Sprite sprite, bool tintable, int layerIndex, bool isPlaceholder)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            // Empilhamento intra-peça por nudge mínimo no eixo de profundidade (ver LayerDepthNudge):
            // camadas posteriores ficam por cima das anteriores.
            go.transform.localPosition = new Vector3(0f, layerIndex * LayerDepthNudge, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite           = sprite;
            sr.sortingLayerName = SortingLayerName;
            sr.sortingOrder     = 0;
            // Sorteia pelo pivô (ponto de contato da base, §5.3), não pelo centro do sprite — assim a
            // profundidade da peça é a do seu contato com o chão, igual à âncora do tile de chão.
            sr.spriteSortPoint  = SpriteSortPoint.Pivot;
            // O placeholder já vem colorido (cubo iso) e as camadas reais ficam em branco — tint real
            // (variante de cor) entra numa fase posterior, com fonte de cor própria.
            sr.color = Color.white;
        }

        /// <summary>
        /// Ponto de contato no mundo: o <b>vértice inferior</b> da célula-âncora no plano Z do nível
        /// de apoio (<see cref="baseLevel"/>) — exatamente onde o chão ancora seu cubo (mesmo Y de
        /// profundidade, §5.3). Usa <c>CellToWorld</c>, <b>nunca</b> <c>GetCellCenterWorld</c> (que
        /// também centra em Z e, com cellSize.z=2, ergueria a peça meia célula, ~32px). O pivô do
        /// sprite (bico inferior da base) faz o diamante da base cobrir a célula. O viés em Z
        /// (<see cref="ContactSortBias"/>) garante que a peça vença o chão no eixo de transparência.
        /// </summary>
        private Vector3 ContactPoint(UnityEngine.Grid grid)
        {
            var corner = grid.CellToWorld(new Vector3Int(anchorCell.x, anchorCell.y, baseLevel));
            return corner + new Vector3(0f, 0f, ContactSortBias);
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private static bool HasNoArt(FurnitureDefinitionSO def)
        {
            var layers = def.VisualLayers;
            if (layers == null || layers.Length == 0) return true;
            foreach (var l in layers)
                if (l.sprite != null) return false;
            return true;
        }

        /// <summary>
        /// Sprite placeholder gerado uma vez: um <b>cubo isométrico</b> do tamanho de uma célula
        /// (diamante 32×16, 2:1, + altura), com o pivô no <b>bico inferior do diamante da base</b>
        /// (bottom-center do sprite) — o mesmo ponto de ancoragem do cubo de chão, então a base da
        /// peça cobre exatamente a célula. Dá uma leitura honesta de posição e sorting até existir
        /// arte real (§5.3).
        /// </summary>
        private static Sprite Placeholder()
        {
            if (_placeholderSprite != null) return _placeholderSprite;

            const int halfW   = 16; // meia largura do diamante (largura total 32)
            const int quartH  = 8;  // meia altura do diamante (altura total 16, razão 2:1)
            const int cubeH   = 14; // altura vertical do corpo do cubo, em pixels
            const int w       = halfW * 2;          // 32
            const int h       = cubeH + quartH * 2; // 30
            const int topCY   = cubeH + quartH;     // centro do diamante do topo

            var top   = new Color32(245, 120, 235, 255);
            var left  = new Color32(205, 80, 200, 255);
            var right = new Color32(165, 55, 160, 255);
            var clear = new Color32(0, 0, 0, 0);

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = clear;
                float dxN = Mathf.Abs(x - halfW) / (float)halfW;

                // Topo: diamante centrado em (halfW, topCY).
                if (dxN + Mathf.Abs(y - topCY) / (float)quartH <= 1f)
                    c = top;
                else if (x <= halfW)
                {
                    // Face frontal-esquerda: aresta superior de (0,topCY) a (halfW, topCY-quartH).
                    float yTop = topCY - x * 0.5f;
                    if (y <= yTop && y >= yTop - cubeH) c = left;
                }
                else
                {
                    // Face frontal-direita: aresta superior de (halfW, topCY-quartH) a (w, topCY).
                    float yTop = (topCY - quartH) + (x - halfW) * 0.5f;
                    if (y <= yTop && y >= yTop - cubeH) c = right;
                }

                px[y * w + x] = c;
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply();

            // Pivô bottom-center (bico inferior da base): casa com a âncora do cubo de chão.
            _placeholderSprite = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0f), 32f);
            _placeholderSprite.name = "FurniturePlaceholder";
            return _placeholderSprite;
        }
    }
}
