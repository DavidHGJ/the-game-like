using GameLike.Data.Base;
using UnityEngine;
using UnityEngine.Localization;

namespace GameLike.Data.Definitions
{
    /// <summary>
    /// Defines a core game genre (Action RPG, Tower Defense, Simulator, ...).
    /// Holds the quality weight profile per development parameter and project
    /// size, as described in GDD section 18.5. Subgenres reference a parent
    /// GenreSO and combine weights using the rule in GDD section 18.4.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Definitions/Genre", fileName = "NewGenre")]
    public sealed class GenreSO : DefinitionAsset
    {
        [Header("Identidade")]
        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString description;

        [Header("Pré-produção")]
        [SerializeField] private ParameterWeightBySize gameDesign;
        [SerializeField] private ParameterWeightBySize narrative;
        [SerializeField] private ParameterWeightBySize artDirection;
        [SerializeField] private ParameterWeightBySize gameFeel;
        [SerializeField] private ParameterWeightBySize scope;

        [Header("Produção — Arte e Visual")]
        [SerializeField] private ParameterWeightBySize graphics;
        [SerializeField] private ParameterWeightBySize animation;
        [SerializeField] private ParameterWeightBySize userInterface;
        [SerializeField] private ParameterWeightBySize vfx;

        [Header("Produção — Áudio")]
        [SerializeField] private ParameterWeightBySize soundtrack;
        [SerializeField] private ParameterWeightBySize soundDesign;
        [SerializeField] private ParameterWeightBySize voiceActing;

        [Header("Produção — Tecnologia")]
        [SerializeField] private ParameterWeightBySize programming;
        [SerializeField] private ParameterWeightBySize performanceOptimization;
        [SerializeField] private ParameterWeightBySize stabilityTesting;

        [Header("Produção — Conteúdo e Design")]
        [SerializeField] private ParameterWeightBySize gameplay;
        [SerializeField] private ParameterWeightBySize content;
        [SerializeField] private ParameterWeightBySize implementedNarrative;
        [SerializeField] private ParameterWeightBySize balancing;

        public LocalizedString DisplayName => displayName;
        public LocalizedString Description => description;

        public ParameterWeightBySize GameDesign => gameDesign;
        public ParameterWeightBySize Narrative => narrative;
        public ParameterWeightBySize ArtDirection => artDirection;
        public ParameterWeightBySize GameFeel => gameFeel;
        public ParameterWeightBySize Scope => scope;

        public ParameterWeightBySize Graphics => graphics;
        public ParameterWeightBySize Animation => animation;
        public ParameterWeightBySize UserInterface => userInterface;
        public ParameterWeightBySize Vfx => vfx;

        public ParameterWeightBySize Soundtrack => soundtrack;
        public ParameterWeightBySize SoundDesign => soundDesign;
        public ParameterWeightBySize VoiceActing => voiceActing;

        public ParameterWeightBySize Programming => programming;
        public ParameterWeightBySize PerformanceOptimization => performanceOptimization;
        public ParameterWeightBySize StabilityTesting => stabilityTesting;

        public ParameterWeightBySize Gameplay => gameplay;
        public ParameterWeightBySize Content => content;
        public ParameterWeightBySize ImplementedNarrative => implementedNarrative;
        public ParameterWeightBySize Balancing => balancing;
    }
}
