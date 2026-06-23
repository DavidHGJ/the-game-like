using GameLike.Data.Base;
using UnityEngine;
using UnityEngine.Localization;

namespace GameLike.Data.Office
{
    /// <summary>
    /// Defines a piece of office furniture: its visual composition (stacked
    /// tintable/fixed sprite layers, GDD-aligned "base + upholstery + color"
    /// pattern) and the single gameplay effect it grants, per the design
    /// decision that furniture upgrades are purely mechanical-by-variant —
    /// a specific chair grants a specific bonus, nothing more.
    ///
    /// Instances are placed in the office as independent GameObjects (not
    /// painted on a Tilemap), so swapping a piece for an upgraded variant is
    /// just replacing the referenced FurnitureDefinitionSO on that instance.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Definitions/Furniture", fileName = "NewFurniture")]
    public sealed class FurnitureDefinitionSO : DefinitionAsset
    {
        [Header("Identidade")]
        [SerializeField] private LocalizedString displayName;
        [SerializeField] private FurnitureCategory category;

        [Header("Composição Visual (camadas, base → topo)")]
        [SerializeField] private FurnitureVisualLayer[] visualLayers;

        [Header("Footprint no Grid de Slots")]
        [Tooltip("Quantos slots de 32x32 esta peça ocupa (largura x profundidade), na perspectiva isométrica do grid de tiles.")]
        [SerializeField] private Vector2Int footprintInTiles = Vector2Int.one;

        [Header("Efeito de Gameplay (GDD — upgrades são puramente mecânicos por variante)")]
        [SerializeField] private FurnitureEffect effect;

        [Header("Custo de Aquisição")]
        [SerializeField] private long purchaseCost;

        public LocalizedString DisplayName => displayName;
        public FurnitureCategory Category => category;
        public FurnitureVisualLayer[] VisualLayers => visualLayers;
        public Vector2Int FootprintInTiles => footprintInTiles;
        public FurnitureEffect Effect => effect;
        public long PurchaseCost => purchaseCost;
    }
}
