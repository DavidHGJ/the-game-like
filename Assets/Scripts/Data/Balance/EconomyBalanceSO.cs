using GameLike.Data.Balance;
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

        public long GarageToIndieThreshold => garageToIndieThreshold;
        public long IndieToOfficeThreshold => indieToOfficeThreshold;
        public long OfficeToStudioThreshold => officeToStudioThreshold;

        public long JuniorWeeklySalary => juniorWeeklySalary;
        public long MidWeeklySalary => midWeeklySalary;
        public long SeniorWeeklySalary => seniorWeeklySalary;
        public long StarWeeklySalary => starWeeklySalary;
    }
}
