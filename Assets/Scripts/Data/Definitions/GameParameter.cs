using System.Collections.Generic;

namespace GameLike.Data.Definitions
{
    /// <summary>As 5 fases sequenciais de produção de um jogo (GDD 5.2 / adendo 23.2).</summary>
    public enum ProductionPhase
    {
        PreProduction = 0,
        Art = 1,
        Audio = 2,
        Technology = 3,
        Content = 4
    }

    /// <summary>Os 19 parâmetros de qualidade em que a equipe distribui pontos (GDD 5.2 / 18.5).</summary>
    public enum GameParameter
    {
        GameDesign = 0,
        Narrative = 1,
        ArtDirection = 2,
        GameFeel = 3,
        Scope = 4,
        Graphics = 5,
        Animation = 6,
        UserInterface = 7,
        Vfx = 8,
        Soundtrack = 9,
        SoundDesign = 10,
        VoiceActing = 11,
        Programming = 12,
        PerformanceOptimization = 13,
        StabilityTesting = 14,
        Gameplay = 15,
        Content = 16,
        ImplementedNarrative = 17,
        Balancing = 18
    }

    /// <summary>
    /// Catálogo estático dos parâmetros: a que fase pertencem, a partir de que escopo existem
    /// (GDD 5.2) e o rótulo pt-BR provisório (até a ponte com o Localization Package).
    /// </summary>
    public static class ParameterCatalog
    {
        public const int ParameterCount = 19;
        public const int PhaseCount = 5;

        private static readonly ProductionPhase[] Phases =
        {
            ProductionPhase.PreProduction, ProductionPhase.PreProduction, ProductionPhase.PreProduction,
            ProductionPhase.PreProduction, ProductionPhase.PreProduction,
            ProductionPhase.Art, ProductionPhase.Art, ProductionPhase.Art, ProductionPhase.Art,
            ProductionPhase.Audio, ProductionPhase.Audio, ProductionPhase.Audio,
            ProductionPhase.Technology, ProductionPhase.Technology, ProductionPhase.Technology,
            ProductionPhase.Content, ProductionPhase.Content, ProductionPhase.Content, ProductionPhase.Content
        };

        private static readonly ProjectSize[] MinSizes =
        {
            ProjectSize.Small, ProjectSize.Small, ProjectSize.Small, ProjectSize.Medium, ProjectSize.Large,
            ProjectSize.Small, ProjectSize.Small, ProjectSize.Small, ProjectSize.Small,
            ProjectSize.Small, ProjectSize.Small, ProjectSize.Large,
            ProjectSize.Small, ProjectSize.Large, ProjectSize.Medium,
            ProjectSize.Small, ProjectSize.Small, ProjectSize.Medium, ProjectSize.Medium
        };

        private static readonly string[] Labels =
        {
            "Game Design", "Narrativa", "Direção de Arte", "Game Feel", "Escopo",
            "Gráficos", "Animação", "UI/UX", "VFX",
            "Trilha Sonora", "Sound Design", "Dublagem",
            "Programação", "Performance", "Estabilidade",
            "Gameplay", "Conteúdo", "Narrativa Impl.", "Balanceamento"
        };

        private static readonly string[] PhaseLabels =
        {
            "Pré-produção", "Arte e Visual", "Áudio", "Tecnologia", "Conteúdo e Design"
        };

        private static readonly string[] PhaseShortLabels = { "Pré-prod", "Arte", "Áudio", "Tech", "Conteúdo" };

        /// <summary>Fração da duração total do projeto que cada fase ocupa (adendo 23.2).</summary>
        private static readonly float[] PhaseDurationShare = { 0.15f, 0.25f, 0.15f, 0.25f, 0.20f };

        public static ProductionPhase PhaseOf(GameParameter p) => Phases[(int)p];
        public static ProjectSize MinSizeOf(GameParameter p) => MinSizes[(int)p];
        public static string Label(GameParameter p) => Labels[(int)p];
        public static string PhaseLabel(ProductionPhase phase) => PhaseLabels[(int)phase];
        public static string PhaseShortLabel(ProductionPhase phase) => PhaseShortLabels[(int)phase];
        public static float DurationShare(ProductionPhase phase) => PhaseDurationShare[(int)phase];

        /// <summary>Verdadeiro se o parâmetro existe para o escopo dado (GDD 5.2).</summary>
        public static bool IsAvailable(GameParameter p, ProjectSize size) => (int)size >= (int)MinSizes[(int)p];

        /// <summary>Todos os parâmetros de uma fase, na ordem do GDD.</summary>
        public static List<GameParameter> ParametersOf(ProductionPhase phase)
        {
            var list = new List<GameParameter>(5);
            for (int i = 0; i < ParameterCount; i++)
                if (Phases[i] == phase) list.Add((GameParameter)i);
            return list;
        }

        /// <summary>Parâmetros de uma fase disponíveis para o escopo dado.</summary>
        public static List<GameParameter> AvailableFor(ProductionPhase phase, ProjectSize size)
        {
            var list = new List<GameParameter>(5);
            for (int i = 0; i < ParameterCount; i++)
                if (Phases[i] == phase && IsAvailable((GameParameter)i, size)) list.Add((GameParameter)i);
            return list;
        }
    }
}
