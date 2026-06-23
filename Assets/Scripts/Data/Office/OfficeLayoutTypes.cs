using System;
using UnityEngine;

namespace GameLike.Data.Office
{
    /// <summary>
    /// A single RoomBlockSO placed at a grid coordinate within an office
    /// layout. Coordinates are in block units (not tiles) — block (1,0) sits
    /// immediately to the east of block (0,0), each occupying a
    /// RoomBlockSO.SizeInTiles x SizeInTiles footprint.
    /// </summary>
    [Serializable]
    public struct PlacedRoomBlock
    {
        public Vector2Int gridPosition;
        public RoomBlockSO block;
    }

    /// <summary>
    /// Whether the wall segment between two adjacent room blocks is solid or
    /// open (a doorway/passage). Per the design decision in CLAUDE.md
    /// ("Office layout system"), this is the only thing that varies at a
    /// block boundary — block skins themselves don't define adjacency.
    /// </summary>
    [Serializable]
    public struct BlockConnection
    {
        public Vector2Int blockA;
        public Vector2Int blockB;
        public bool isOpen;
    }

    /// <summary>
    /// A furniture piece placed within the layout. Position is in tile
    /// coordinates relative to the whole layout's origin (not relative to a
    /// single block), so a piece can be unambiguously located even though
    /// the grid is composed of multiple blocks.
    /// </summary>
    [Serializable]
    public struct PlacedFurniture
    {
        public Vector2Int tilePosition;
        public FurnitureDefinitionSO furniture;
    }
}
