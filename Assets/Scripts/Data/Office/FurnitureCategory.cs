namespace GameLike.Data.Office
{
    /// <summary>
    /// Functional category of a furniture piece. Used to group items in the
    /// office editor tooling and to validate which slots a piece may occupy
    /// (e.g. a Chair must be paired with a Desk, a Decoration has no
    /// functional constraint).
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
