using System;

namespace GameLike.Editor.Content
{
    /// <summary>
    /// Plain data shape an agent must produce for a Genre. Mirrors GenreSO's
    /// fields one-to-one. Lives in the Editor assembly because it only exists
    /// to be deserialized by <see cref="GenreContentImporter"/> — it is not
    /// game runtime data.
    ///
    /// Weight fields accept the GDD scale as strings: "---", "--", "-", "~",
    /// "+", "++", "+++". A missing/null size slot means the parameter does
    /// not exist for that project size (see GDD 5.2 availability rules).
    /// </summary>
    [Serializable]
    public sealed class GenreContentDto
    {
        public string id; // empty for a brand-new genre; filled by the tool afterwards
        public string key; // stable slug used to match existing assets, e.g. "action_rpg"
        public string displayNamePtBr;
        public string descriptionPtBr;

        public ParameterWeightDto gameDesign;
        public ParameterWeightDto narrative;
        public ParameterWeightDto artDirection;
        public ParameterWeightDto gameFeel;
        public ParameterWeightDto scope;

        public ParameterWeightDto graphics;
        public ParameterWeightDto animation;
        public ParameterWeightDto userInterface;
        public ParameterWeightDto vfx;

        public ParameterWeightDto soundtrack;
        public ParameterWeightDto soundDesign;
        public ParameterWeightDto voiceActing;

        public ParameterWeightDto programming;
        public ParameterWeightDto performanceOptimization;
        public ParameterWeightDto stabilityTesting;

        public ParameterWeightDto gameplay;
        public ParameterWeightDto content;
        public ParameterWeightDto implementedNarrative;
        public ParameterWeightDto balancing;
    }

    [Serializable]
    public sealed class ParameterWeightDto
    {
        public string small;
        public string medium;
        public string large;
        public string aaa;
    }
}
