namespace GameLike.Data.LevelBuilder
{
    /// <summary>
    /// Functional category of a furniture piece. Used to organize the Level
    /// Builder palette and as the default furniture upgrade group
    /// (LEVEL_BUILDER.md §10.2) — a piece's category seeds which group it
    /// belongs to unless explicitly reassigned.
    /// </summary>
    public enum FurnitureCategory
    {
        Desk,
        Chair,
        Storage,
        Decoration,
        WallFixture,
        FloorFixture
    }
}
