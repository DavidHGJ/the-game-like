using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Quality formula weights and hype-to-sales conversion thresholds from
    /// GDD section 19.6 and 19.7. The formula's structure (which components
    /// exist and how they combine) is code — see the quality calculation
    /// system (TODO, Fase 2). Only the numeric weights live here, so
    /// calibration does not require touching the formula's logic.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Balance/Quality Formula Balance", fileName = "QualityFormulaBalance")]
    public sealed class QualityFormulaBalanceSO : BalanceAsset
    {
        [Header("Pesos da Fórmula de Qualidade (GDD 19.7) — devem somar 1.0")]
        [Range(0f, 1f)] [SerializeField] private float sliderProfileWeight = 0.40f;
        [Range(0f, 1f)] [SerializeField] private float themeGenreCompatibilityWeight = 0.20f;
        [Range(0f, 1f)] [SerializeField] private float teamLevelWeight = 0.15f;
        [Range(0f, 1f)] [SerializeField] private float engineBudgetUsageWeight = 0.10f;
        [Range(0f, 1f)] [SerializeField] private float marketTrendWeight = 0.10f;
        [Range(0f, 1f)] [SerializeField] private float developmentEventsWeight = 0.05f;

        [Header("Conversão de Hype em Vendas (GDD 19.6)")]
        [SerializeField] private int weakHypeUpperBound = 20;
        [SerializeField] private int mediumHypeUpperBound = 50;
        [SerializeField] private int goodHypeUpperBound = 80;
        [Tooltip("Quantas vendas cada 100 wishlists geram na semana de lançamento.")]
        [SerializeField] private int salesPer100Wishlists = 80;

        public float SliderProfileWeight => sliderProfileWeight;
        public float ThemeGenreCompatibilityWeight => themeGenreCompatibilityWeight;
        public float TeamLevelWeight => teamLevelWeight;
        public float EngineBudgetUsageWeight => engineBudgetUsageWeight;
        public float MarketTrendWeight => marketTrendWeight;
        public float DevelopmentEventsWeight => developmentEventsWeight;

        public int WeakHypeUpperBound => weakHypeUpperBound;
        public int MediumHypeUpperBound => mediumHypeUpperBound;
        public int GoodHypeUpperBound => goodHypeUpperBound;
        public int SalesPer100Wishlists => salesPer100Wishlists;

        /// <summary>
        /// Sum of all formula weights. Should equal 1.0 — validated in the
        /// editor via <see cref="GameLike.Editor.Data.BalanceAssetValidator"/>,
        /// not enforced at runtime, since designers may want to experiment
        /// mid-tuning before the sum is exact.
        /// </summary>
        public float TotalWeight =>
            sliderProfileWeight + themeGenreCompatibilityWeight + teamLevelWeight +
            engineBudgetUsageWeight + marketTrendWeight + developmentEventsWeight;
    }
}
