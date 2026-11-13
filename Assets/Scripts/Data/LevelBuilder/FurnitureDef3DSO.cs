using System;
using GameLike.Data.Base;
using UnityEngine;
using UnityEngine.Localization;

namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// Uma fonte de luz embutida numa peça (point light). A posição é local ao
    /// pivô da peça, então a luz <b>gira junto</b> com a peça. Usada por
    /// luminárias, telas, LEDs — e por "pontos de luz" avulsos (peça sem malha,
    /// só luz). Range/intensity em unidades Unity; cor em sRGB.
    /// </summary>
    [Serializable]
    public struct FurnitureLight
    {
        public Vector3 localPosition;
        [ColorUsage(false, true)] public Color color;
        public float intensity;
        public float range;

        [Tooltip("true = holofote (spot) direcional, p/ luminária/abajur que joga luz principalmente pra baixo; false = point light abrangente (teto, LED).")]
        public bool spot;
        [Tooltip("Ângulo do cone do spot em graus (ignorado se não for spot).")]
        public float spotAngle;
        [Tooltip("Direção local do spot (default: pra baixo, 0,-1,0). Normalizada no uso.")]
        public Vector3 spotDirection;
    }
    /// <summary>
    /// Definição 3D de uma peça de mobília do escritório (direção low-poly 3D).
    /// Aponta para um <b>prefab 3D</b> (não mais pilha de sprites), ocupa um
    /// footprint em células do grid de mundo, e carrega um único efeito de
    /// gameplay (GDD — upgrades são puramente mecânicos por variante).
    ///
    /// Trocar uma peça por um upgrade é substituir a referência a este SO na
    /// instância; o <c>OfficeBuilder</c> reinstancia o prefab correspondente.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Definitions/Furniture 3D", fileName = "NewFurniture3D")]
    public sealed class FurnitureDef3DSO : DefinitionAsset
    {
        [Header("Identidade")]
        [SerializeField] private LocalizedString displayName;
        [Tooltip("Nome curto para a paleta do editor (pt-BR direto; a UI de jogo usa displayName).")]
        [SerializeField] private string editorName = "Móvel";
        [SerializeField] private FurnitureCategory category;

        [Header("Visual 3D")]
        [Tooltip("Prefab/FBX 3D low-poly com pivô na base e submeshes fixo/tintável (ART_STYLE.md).")]
        [SerializeField] private GameObject prefab;

        [Header("Footprint no grid (células)")]
        [Tooltip("Quantas células (largura × profundidade) a peça ocupa no grid de mundo.")]
        [SerializeField] private Vector2Int footprintInCells = Vector2Int.one;

        [Tooltip("Se a peça pode ser girada em passos de 90° no editor de fases.")]
        [SerializeField] private bool rotatable = true;

        [Header("Encaixe")]
        [Tooltip("Peça de parede (quadro, pôster) encosta na parede do fundo, não ocupa chão livre.")]
        [SerializeField] private bool isWallPiece;

        [Header("Empilhamento")]
        [Tooltip("Peça pequena que pode ficar em cima de outro móvel (abajur, monitor, plantinha).")]
        [SerializeField] private bool placeableOnTop;
        [Tooltip("Móvel com tampo: aceita peças pequenas em cima (mesa, armário baixo).")]
        [SerializeField] private bool supportsItemsOnTop;
        [Tooltip("Altura do tampo em metros. 0 = usa o topo da malha.")]
        [Min(0f)] [SerializeField] private float surfaceHeight;

        [Header("Efeito de Gameplay (GDD — mecânico por variante)")]
        [SerializeField] private FurnitureEffect effect;

        [Header("Luz embutida (point lights filhas; giram com a peça)")]
        [Tooltip("Fontes de luz emitidas pela peça (luminária, tela, LED). Vazio = não emite luz própria.")]
        [SerializeField] private FurnitureLight[] lights = Array.Empty<FurnitureLight>();

        [Header("Custo de Aquisição")]
        [SerializeField] private long purchaseCost;

        public LocalizedString DisplayName => displayName;
        public string EditorName => editorName;
        public FurnitureCategory Category => category;
        public GameObject Prefab => prefab;
        public Vector2Int FootprintInCells => footprintInCells;
        public bool Rotatable => rotatable;
        public bool IsWallPiece => isWallPiece;
        public bool PlaceableOnTop => placeableOnTop;
        public bool SupportsItemsOnTop => supportsItemsOnTop;
        public float SurfaceHeight => surfaceHeight;
        public FurnitureEffect Effect => effect;
        public FurnitureLight[] Lights => lights;
        public long PurchaseCost => purchaseCost;

#if UNITY_EDITOR
        /// <summary>Define as luzes embutidas (somente ferramenta de autoria).</summary>
        public void EditorSetLights(FurnitureLight[] value) => lights = value ?? Array.Empty<FurnitureLight>();
        /// <summary>Define se a peça é de parede (somente ferramenta de autoria).</summary>
        public void EditorSetWallPiece(bool value) => isWallPiece = value;
#endif

        /// <summary>Define o comportamento de empilhamento (ferramenta de autoria e testes).</summary>
        public void SetStacking(bool canGoOnTop, bool acceptsItemsOnTop, float tabletopHeight)
        {
            placeableOnTop = canGoOnTop;
            supportsItemsOnTop = acceptsItemsOnTop;
            surfaceHeight = Mathf.Max(0f, tabletopHeight);
        }
    }
}
