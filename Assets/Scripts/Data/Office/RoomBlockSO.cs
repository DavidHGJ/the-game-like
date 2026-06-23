using GameLike.Data.Base;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Tilemaps;

namespace GameLike.Data.Office
{
    /// <summary>
    /// Defines a room block "skin": a 7x7-tile square unit that can be
    /// placed and connected to adjacent blocks to form the office layout
    /// for a given company phase (Garage/Indie/Office/Studio — GDD section
    /// 3.4). The 7x7 size is fixed project-wide (see CLAUDE.md, "Office
    /// layout system") so every block is interchangeable in the grid.
    ///
    /// A RoomBlockSO only defines the visual skin (floor/wall tile palette)
    /// — actual placement, adjacency (open/closed walls) and furniture
    /// instances for a specific phase live in OfficeLayoutSO, not here.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Definitions/Room Block", fileName = "NewRoomBlock")]
    public sealed class RoomBlockSO : DefinitionAsset
    {
        /// <summary>Fixed side length, in tiles, of every room block in the project.</summary>
        public const int SizeInTiles = 7;

        [Header("Identidade")]
        [SerializeField] private LocalizedString displayName;

        [Header("Paleta Visual (Tile Palette do Unity)")]
        [SerializeField] private TileBase floorTile;
        [SerializeField] private TileBase wallTile;
        [Tooltip("Tile usado na passagem entre dois blocos quando a parede está aberta (porta/vão).")]
        [SerializeField] private TileBase openWallTile;

        public LocalizedString DisplayName => displayName;
        public TileBase FloorTile => floorTile;
        public TileBase WallTile => wallTile;
        public TileBase OpenWallTile => openWallTile;
    }
}
