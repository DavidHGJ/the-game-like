using System;

namespace GameLike.Data.Definitions
{
    /// <summary>
    /// The seven-level weight scale used throughout the GDD (section 18.2) for
    /// genre quality profiles, subgenre-to-parent combination, theme-genre and
    /// genre-platform compatibility. Maps directly to -3..+3 for the
    /// deterministic combination math described in GDD 18.4.
    /// </summary>
    public enum WeightLevel
    {
        StronglyNegative = -3, // ---
        Negative = -2,         // --
        SlightlyNegative = -1, // -
        Neutral = 0,           // ~
        SlightlyPositive = 1,  // +
        Positive = 2,          // ++
        StronglyPositive = 3   // +++
    }

    /// <summary>
    /// Project scope sizes from GDD section 5 — each has a different points
    /// ceiling per parameter and a different quality profile per genre.
    /// </summary>
    public enum ProjectSize
    {
        Small,
        Medium,
        Large,
        AAA
    }

    /// <summary>
    /// A single parameter's weight across all four project sizes.
    /// "Unavailable" (no weight) is represented by leaving the size's slot
    /// absent in the source JSON — see GenreSO for how that is modeled.
    /// </summary>
    [Serializable]
    public struct ParameterWeightBySize
    {
        public WeightLevel small;
        public WeightLevel medium;
        public WeightLevel large;
        public WeightLevel aaa;

        public WeightLevel ForSize(ProjectSize size) => size switch
        {
            ProjectSize.Small => small,
            ProjectSize.Medium => medium,
            ProjectSize.Large => large,
            ProjectSize.AAA => aaa,
            _ => throw new ArgumentOutOfRangeException(nameof(size))
        };
    }
}
