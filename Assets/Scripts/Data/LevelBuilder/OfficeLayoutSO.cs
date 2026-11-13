using System;
using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Base;
using UnityEngine;

namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// Uma colocação de mobília dentro de um <see cref="OfficeLayoutSO"/>:
    /// qual peça, em qual célula (canto inferior do footprint) e com qual
    /// rotação (passos de 90° no eixo Y).
    /// </summary>
    [Serializable]
    public struct FurniturePlacement
    {
        public FurnitureDef3DSO furniture;
        public Vector2Int cell;
        [Range(0, 3)] public int rotationSteps;
        [Tooltip("Posição dentro do footprint: (0,0) = centro; -1/+1 em x ou y encosta a peça naquela borda (cantos = os dois).")]
        public Vector2Int align;
        [Tooltip("true = a peça está em cima do móvel que ocupa a célula (abajur sobre a mesa).")]
        public bool onTop;
    }

    /// <summary>Acabamento de chão/parede: liso ou um padrão gerado por código na cor escolhida.</summary>
    public enum SurfaceStyle
    {
        Plain,
        Brick,
        WoodPlanks,
        WoodPanel,
        Tiles,
        Checker,
        Stripes,
        Carpet,
        Concrete
    }

    /// <summary>
    /// Um ponto de luz de teto posicionado numa célula do grid, pendurado à altura
    /// da parede. O dev escolhe onde ficam no criador de fases ("luz de teto").
    /// </summary>
    [Serializable]
    public struct CeilingLight
    {
        public Vector2Int cell;
        [ColorUsage(false, true)] public Color color;
        public float intensity;
        public float range;
        [Tooltip("Quanto abaixo do teto a luz fica (m). 0 = usa o padrão.")]
        public float dropFromCeiling;
    }

    /// <summary>
    /// Um assento do cenário: onde um boneco (fundador ou funcionário) aparece sentado. Fica
    /// atrelado à célula de uma cadeira já posicionada (pega a posição e a rotação dela) ou,
    /// se não houver cadeira ali, vale a própria célula. A ORDEM na lista define quem senta
    /// onde: o assento 0 é do fundador; os seguintes são ocupados pelos funcionários na ordem
    /// de contratação.
    /// </summary>
    [Serializable]
    public struct SeatPoint
    {
        public Vector2Int cell;
        [Tooltip("Giro extra do boneco em passos de 90° (somado à rotação da cadeira).")]
        [Range(0, 3)] public int rotationSteps;
        [Tooltip("Ajuste fino local (metros): X = lado, Y = altura, Z = para frente.")]
        public Vector3 offset;
        [Tooltip("O que o boneco fica fazendo neste assento por padrão.")]
        public SeatActivity activity;
    }

    /// <summary>Atividade padrão de quem senta num assento (a animação que o boneco faz).</summary>
    public enum SeatActivity
    {
        Working,
        Thinking,
        Relaxed,
        Sleeping
    }

    /// <summary>
    /// Layout 3D do escritório de uma fase, autorado pelo dev no criador de fases
    /// (ferramenta de produção, não editável pelo jogador em runtime). Guarda as
    /// dimensões da sala em células, o tamanho da célula em mundo, a aparência de
    /// chão/parede e a lista de móveis posicionados. O <c>OfficeBuilder</c> lê
    /// este asset e instancia a sala sem sobreposição.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Level/Office Layout", fileName = "NewOfficeLayout")]
    public sealed class OfficeLayoutSO : DefinitionAsset
    {
        [Header("Fase")]
        [SerializeField] private CompanyPhase phase = CompanyPhase.Garage;

        [Header("Dimensões da sala (células)")]
        [Min(2)] [SerializeField] private int cols = 10;
        [Min(2)] [SerializeField] private int rows = 10;

        [Header("Escala e paredes")]
        [Tooltip("Tamanho de uma célula em unidades de mundo (calibrar contra a arte).")]
        [Min(0.1f)] [SerializeField] private float cellSize = 1f;
        [Min(0.5f)] [SerializeField] private float wallHeight = 2.8f;
        [Min(0.05f)] [SerializeField] private float wallThickness = 0.2f;

        [Header("Cores provisórias (até a paleta de materiais)")]
        [SerializeField] private Color floorColor = new Color(0.62f, 0.60f, 0.56f);
        [SerializeField] private Color wallColor = new Color(0.55f, 0.70f, 0.50f);
        [SerializeField] private SurfaceStyle floorStyle = SurfaceStyle.Plain;
        [SerializeField] private SurfaceStyle wallStyle = SurfaceStyle.Plain;
        [Tooltip("true = a parede lateral tem cor/acabamento próprios; false = igual à do fundo.")]
        [SerializeField] private bool sideWallCustom;
        [SerializeField] private Color sideWallColor = new Color(0.55f, 0.70f, 0.50f);
        [SerializeField] private SurfaceStyle sideWallStyle = SurfaceStyle.Plain;

        [Header("Mobília posicionada")]
        [SerializeField] private List<FurniturePlacement> placements = new List<FurniturePlacement>();

        [Header("Luzes de teto")]
        [SerializeField] private List<CeilingLight> ceilingLights = new List<CeilingLight>();

        [Header("Assentos (onde os bonecos aparecem sentados)")]
        [SerializeField] private List<SeatPoint> seats = new List<SeatPoint>();
        [Tooltip("Prefab do boneco sentado (pivô nos pés).")]
        [SerializeField] private GameObject characterPrefab;

        public CompanyPhase Phase => phase;
        public int Cols => cols;
        public int Rows => rows;
        public float CellSize => cellSize;
        public float WallHeight => wallHeight;
        public float WallThickness => wallThickness;
        public Color FloorColor => floorColor;
        public Color WallColor => wallColor;
        public SurfaceStyle FloorStyle => floorStyle;
        public SurfaceStyle WallStyle => wallStyle;

        /// <summary>true se a parede lateral foi configurada separada da parede do fundo.</summary>
        public bool SideWallCustom => sideWallCustom;
        /// <summary>Cor da parede lateral (a do fundo, se não foi separada).</summary>
        public Color SideWallColor => sideWallCustom ? sideWallColor : wallColor;
        /// <summary>Acabamento da parede lateral (o do fundo, se não foi separada).</summary>
        public SurfaceStyle SideWallStyle => sideWallCustom ? sideWallStyle : wallStyle;
        public IReadOnlyList<FurniturePlacement> Placements => placements;
        public IReadOnlyList<CeilingLight> CeilingLights => ceilingLights;

        /// <summary>Assentos do cenário, em ordem (0 = fundador).</summary>
        public IReadOnlyList<SeatPoint> Seats => seats;

        /// <summary>Prefab do boneco que senta nos assentos.</summary>
        public GameObject CharacterPrefab => characterPrefab;

        /// <summary>Acesso de edição à lista de colocações (ferramentas de autoria: editor e Map Builder in-game).</summary>
        public List<FurniturePlacement> EditorPlacements => placements;
        /// <summary>Acesso de edição às luzes de teto (ferramentas de autoria).</summary>
        public List<CeilingLight> EditorCeilingLights => ceilingLights;
        /// <summary>Acesso de edição aos assentos (ferramentas de autoria).</summary>
        public List<SeatPoint> EditorSeats => seats;
        public void EditorSetPhase(CompanyPhase value) { phase = value; }
        public void EditorSetCharacterPrefab(GameObject value) { characterPrefab = value; }
        public void EditorSetSize(int c, int r) { cols = Mathf.Max(2, c); rows = Mathf.Max(2, r); }
        public void EditorSetColors(Color floor, Color wall) { floorColor = floor; wallColor = wall; }
        public void EditorSetStyles(SurfaceStyle floor, SurfaceStyle wall) { floorStyle = floor; wallStyle = wall; }
        public void EditorSetSideWall(bool custom, Color color, SurfaceStyle style) { sideWallCustom = custom; sideWallColor = color; sideWallStyle = style; }

        /// <summary>Copia este cenário para a forma de dado puro (JSON) usada pelo Map Builder.</summary>
        public OfficeMapData ToMapData(string mapName, bool usedInGame)
        {
            var data = new OfficeMapData
            {
                name = mapName,
                phase = phase,
                usedInGame = usedInGame,
                cols = cols,
                rows = rows,
                cellSize = cellSize,
                wallHeight = wallHeight,
                wallThickness = wallThickness,
                floorColor = floorColor,
                wallColor = wallColor,
                floorStyle = floorStyle,
                wallStyle = wallStyle,
                sideWallCustom = sideWallCustom,
                sideWallColor = sideWallColor,
                sideWallStyle = sideWallStyle,
                lights = new List<CeilingLight>(ceilingLights),
                seats = new List<SeatPoint>(seats)
            };
            foreach (var p in placements)
            {
                if (p.furniture == null) continue;
                data.pieces.Add(new OfficeMapPiece
                {
                    furnitureId = p.furniture.Id,
                    furnitureKey = p.furniture.name,
                    cell = p.cell,
                    rotationSteps = p.rotationSteps,
                    align = p.align,
                    onTop = p.onTop
                });
            }
            return data;
        }

        /// <summary>
        /// Sobrescreve este cenário com o conteúdo de um <see cref="OfficeMapData"/>, resolvendo
        /// a mobília pelo catálogo. Peças cuja definição não existe mais são descartadas;
        /// devolve quantas foram descartadas.
        /// </summary>
        public int ApplyMapData(OfficeMapData data, FurnitureCatalogSO catalog)
        {
            phase = data.phase;
            cols = Mathf.Max(2, data.cols);
            rows = Mathf.Max(2, data.rows);
            cellSize = Mathf.Max(0.1f, data.cellSize);
            wallHeight = Mathf.Max(0.5f, data.wallHeight);
            wallThickness = Mathf.Max(0.05f, data.wallThickness);
            floorColor = data.floorColor;
            wallColor = data.wallColor;
            floorStyle = data.floorStyle;
            wallStyle = data.wallStyle;
            sideWallCustom = data.sideWallCustom;
            sideWallColor = data.sideWallColor;
            sideWallStyle = data.sideWallStyle;
            ceilingLights = new List<CeilingLight>(data.lights ?? new List<CeilingLight>());
            seats = new List<SeatPoint>(data.seats ?? new List<SeatPoint>());
            if (catalog != null) characterPrefab = catalog.DefaultCharacter;

            placements = new List<FurniturePlacement>();
            if (data.pieces == null) return 0;
            int missing = 0;
            foreach (var piece in data.pieces)
            {
                var def = catalog != null ? catalog.Find(piece.furnitureId, piece.furnitureKey) : null;
                if (def == null) { missing++; continue; }
                placements.Add(new FurniturePlacement
                {
                    furniture = def,
                    cell = piece.cell,
                    rotationSteps = ((piece.rotationSteps % 4) + 4) % 4,
                    align = new Vector2Int(Mathf.Clamp(piece.align.x, -1, 1), Mathf.Clamp(piece.align.y, -1, 1)),
                    onTop = piece.onTop
                });
            }
            return missing;
        }

        /// <summary>Cria um cenário em memória (não é asset) a partir de um <see cref="OfficeMapData"/>.</summary>
        public static OfficeLayoutSO FromMapData(OfficeMapData data, FurnitureCatalogSO catalog)
        {
            var layout = CreateInstance<OfficeLayoutSO>();
            layout.name = data.name;
            layout.ApplyMapData(data, catalog);
            return layout;
        }
    }
}
