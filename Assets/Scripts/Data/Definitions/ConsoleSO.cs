using GameLike.Data.Base;
using UnityEngine;
using UnityEngine.Localization;

namespace GameLike.Data.Definitions
{
    /// <summary>
    /// Define um console/plataforma de lançamento (GDD seção 15). Campos do GDD:
    /// ano de lançamento, nome, popularidade inicial, curva de popularidade e custo
    /// de licença. Consoles são lançados progressivamente ao longo dos anos da run,
    /// criando janelas estratégicas. Novos consoles entram sem refatoração de código
    /// (basta criar o asset via pipeline de conteúdo).
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Definitions/Console", fileName = "NewConsole")]
    public sealed class ConsoleSO : DefinitionAsset
    {
        [Header("Identidade")]
        [SerializeField] private LocalizedString displayName;
        [SerializeField] private LocalizedString description;
        [Tooltip("Fabricante/estúdio da paródia (texto técnico/flavor, não localizado).")]
        [SerializeField] private string manufacturer;

        [Header("Classificação")]
        [SerializeField] private PlatformCategory category = PlatformCategory.Console;

        [Header("Linha do tempo (GDD 15.2)")]
        [Tooltip("Ano da run em que o console fica disponível (a run começa no Ano 1).")]
        [SerializeField] private int releaseYear = 3;
        [Tooltip("Custo de licença para lançar neste console (valor inicial, a balancear).")]
        [SerializeField] private long licenseCost = 3000;

        [Header("Popularidade (0..1, a balancear)")]
        [Tooltip("Popularidade logo no lançamento.")]
        [Range(0f, 1f)] [SerializeField] private float initialPopularity = 0.25f;
        [Tooltip("Popularidade no pico da curva.")]
        [Range(0f, 1f)] [SerializeField] private float peakPopularity = 0.8f;
        [Tooltip("Anos de relevância do console (do lançamento ao fim da curva).")]
        [SerializeField] private int lifespanYears = 10;
        [Tooltip("Forma da curva de popularidade ao longo da vida: x = 0..1 (fração da vida desde o lançamento), y = 0..1 (fração entre inicial e pico, com declínio no fim).")]
        [SerializeField] private AnimationCurve popularityCurve =
            new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.15f));

        public LocalizedString DisplayName => displayName;
        public LocalizedString Description => description;
        public string Manufacturer => manufacturer;
        public PlatformCategory Category => category;
        public int ReleaseYear => releaseYear;
        public long LicenseCost => licenseCost;
        public float InitialPopularity => initialPopularity;
        public float PeakPopularity => peakPopularity;
        public int LifespanYears => lifespanYears;

        /// <summary>Verdadeiro se o console já foi lançado no ano de jogo informado.</summary>
        public bool IsAvailable(int gameYear) => gameYear >= releaseYear;

        /// <summary>
        /// Popularidade (0..1) do console num ano da run. Zero antes do lançamento.
        /// Interpola entre popularidade inicial e pico pela forma de <see cref="popularityCurve"/>,
        /// ao longo de <see cref="lifespanYears"/>. Managers de mercado/vendas leem isto.
        /// </summary>
        public float PopularityAtYear(int gameYear)
        {
            if (gameYear < releaseYear) return 0f;
            float life = Mathf.Max(1, lifespanYears);
            float t = Mathf.Clamp01((gameYear - releaseYear) / life);
            float shape = Mathf.Clamp01(popularityCurve.Evaluate(t));
            return Mathf.Lerp(initialPopularity, peakPopularity, shape);
        }
    }
}
