using GameLike.Core.Events;
using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Economy-related balance values from GDD section 19.1 and 19.2.
    /// All values are initial and marked for testing/adjustment per the GDD —
    /// editing them here in the Inspector takes effect immediately, no
    /// recompilation needed.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Balance/Economy Balance", fileName = "EconomyBalance")]
    public sealed class EconomyBalanceSO : BalanceAsset
    {
        [Header("Thresholds de Valuation por Fase (GDD 19.1)")]
        [SerializeField] private long garageToIndieThreshold = 250_000;
        [SerializeField] private long indieToOfficeThreshold = 500_000;
        [SerializeField] private long officeToStudioThreshold = 5_000_000;

        [Header("Salários Base por Semana (GDD 19.2)")]
        [SerializeField] private long juniorWeeklySalary = 2_000;
        [SerializeField] private long midWeeklySalary = 3_500;
        [SerializeField] private long seniorWeeklySalary = 6_000;
        [SerializeField] private long starWeeklySalary = 10_000;

        [Header("Custo operacional semanal por fase (aluguel/overhead — GDD 2.3)")]
        [Tooltip("Sangria fixa por semana além da folha salarial; cresce com a fase. Slice — calibrar.")]
        [SerializeField] private long garageWeeklyOperating = 300;
        [SerializeField] private long indieWeeklyOperating = 1_200;
        [SerializeField] private long officeWeeklyOperating = 4_000;
        [SerializeField] private long studioWeeklyOperating = 12_000;

        [Header("Expansão de fase (GDD 2.1 / adendo 23.8)")]
        [SerializeField] private long expandToIndieCost = 40_000;
        [SerializeField] private long expandToOfficeCost = 150_000;
        [SerializeField] private long expandToStudioCost = 1_500_000;
        [Tooltip("Fração do valuation perdida por semana enquanto a expansão disponível não é feita.")]
        [Range(0f, 0.05f)] [SerializeField] private float stagnationLossPerWeek = 0.005f;
        [Range(0f, 0.9f)] [SerializeField] private float stagnationLossMax = 0.30f;

        public float StagnationLossPerWeek => stagnationLossPerWeek;
        public float StagnationLossMax => stagnationLossMax;

        /// <summary>Valuation necessário para a fase seguinte à dada; 0 se já é a última.</summary>
        public long ThresholdAfter(CompanyPhase phase) => phase switch
        {
            CompanyPhase.Garage => garageToIndieThreshold,
            CompanyPhase.Indie => indieToOfficeThreshold,
            CompanyPhase.Office => officeToStudioThreshold,
            _ => 0
        };

        /// <summary>Custo para expandir PARA a fase dada.</summary>
        public long ExpansionCostTo(CompanyPhase phase) => phase switch
        {
            CompanyPhase.Indie => expandToIndieCost,
            CompanyPhase.Office => expandToOfficeCost,
            CompanyPhase.Studio => expandToStudioCost,
            _ => 0
        };

        public long GarageToIndieThreshold => garageToIndieThreshold;
        public long IndieToOfficeThreshold => indieToOfficeThreshold;
        public long OfficeToStudioThreshold => officeToStudioThreshold;

        public long JuniorWeeklySalary => juniorWeeklySalary;
        public long MidWeeklySalary => midWeeklySalary;
        public long SeniorWeeklySalary => seniorWeeklySalary;
        public long StarWeeklySalary => starWeeklySalary;

        /// <summary>Custo operacional semanal (overhead) para a fase dada (GDD 2.3).</summary>
        public long WeeklyOperatingFor(CompanyPhase phase) => phase switch
        {
            CompanyPhase.Garage => garageWeeklyOperating,
            CompanyPhase.Indie => indieWeeklyOperating,
            CompanyPhase.Office => officeWeeklyOperating,
            CompanyPhase.Studio => studioWeeklyOperating,
            _ => garageWeeklyOperating
        };
    }
}
