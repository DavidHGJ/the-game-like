using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Parâmetros gerais de ritmo e custo da run (GDD 17 e adendo 23.12/23.13): duração da
    /// semana, inflação, variação de custo por run, carência de falência e prêmio do IPO.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Balance/Game Tuning", fileName = "GameTuningBalance")]
    public sealed class GameTuningBalanceSO : BalanceAsset
    {
        [Header("Tempo")]
        [Tooltip("Segundos reais que uma semana de jogo dura na velocidade normal (1x).")]
        [Min(0.2f)] [SerializeField] private float secondsPerWeek = 5f;

        [Header("Inflação e variação de custo (GDD 23.12)")]
        [Tooltip("Quanto TODO custo sobe por semana de run, em %. 0 = sem inflação. Ex.: 0,2 dá ~11% ao ano.")]
        [Range(0f, 5f)] [SerializeField] private float weeklyInflationPercent;
        [Tooltip("Faixa do sorteio de custo de cada run, em %: 10 = cada run sai entre -10% e +10% do custo base.")]
        [Range(0f, 50f)] [SerializeField] private float runCostVariationPercent;

        [Header("Fim de run")]
        [Tooltip("Semanas seguidas com caixa negativo antes da falência.")]
        [Min(0)] [SerializeField] private int bankruptcyGraceWeeks = 4;
        [Tooltip("Multiplicador do valuation pago no IPO.")]
        [Min(0.1f)] [SerializeField] private float ipoPremium = 1.5f;

        public float SecondsPerWeek => Mathf.Max(0.2f, secondsPerWeek);
        public float WeeklyInflationPercent => weeklyInflationPercent;
        public float RunCostVariationPercent => runCostVariationPercent;
        public int BankruptcyGraceWeeks => bankruptcyGraceWeeks;
        public float IpoPremium => ipoPremium;

        /// <summary>Multiplicador de custo depois de <paramref name="weeks"/> semanas de inflação.</summary>
        public float InflationFactor(int weeks) =>
            Mathf.Pow(1f + weeklyInflationPercent / 100f, Mathf.Max(0, weeks));
    }
}
