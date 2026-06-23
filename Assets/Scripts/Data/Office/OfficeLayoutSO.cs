using GameLike.Data.Base;
using UnityEngine;

namespace GameLike.Data.Office
{
    /// <summary>
    /// The complete office layout for one company phase (Garage, Indie,
    /// Office, Studio — GDD section 3.4): which RoomBlockSO instances are
    /// placed where, which block boundaries are open vs solid, and which
    /// furniture pieces sit where.
    ///
    /// This asset is produced by you, the developer, using the office
    /// editor tooling (a Tilemap-backed scene + a "bake to asset" editor
    /// script) — it is not something a content agent generates from JSON,
    /// because it requires visual judgment about room composition, not
    /// numeric weights. See CLAUDE.md, "Office layout system", for the
    /// authoring workflow.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Definitions/Office Layout", fileName = "NewOfficeLayout")]
    public sealed class OfficeLayoutSO : DefinitionAsset
    {
        [Header("Blocões de Sala")]
        [SerializeField] private PlacedRoomBlock[] blocks;

        [Header("Conexões Entre Blocões (parede aberta ou fechada)")]
        [SerializeField] private BlockConnection[] connections;

        [Header("Mobília Posicionada")]
        [SerializeField] private PlacedFurniture[] furniture;

        public PlacedRoomBlock[] Blocks => blocks;
        public BlockConnection[] Connections => connections;
        public PlacedFurniture[] Furniture => furniture;
    }
}
