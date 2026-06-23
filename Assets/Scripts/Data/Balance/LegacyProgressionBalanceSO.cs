using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Legacy Points (PL) and meta-progression balance values from GDD section 19.5.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Balance/Legacy Progression Balance", fileName = "LegacyProgressionBalance")]
    public sealed class LegacyProgressionBalanceSO : BalanceAsset
    {
        [Header("Ganho de Pontos de Legado por Marco")]
        [SerializeField] private int plForReachingIndie = 5;
        [SerializeField] private int plForReachingOffice = 15;
        [SerializeField] private int plForReachingStudio = 30;
        [SerializeField] private int plPerGameAboveScoreSeven = 2;
        [SerializeField] private int plForFranchiseWithThreeOrMoreGames = 10;
        [SerializeField] private int plForRunWithoutMajorCrisis = 5;
        [SerializeField] private int plForValuationAboveOneMillion = 5;
        [SerializeField] private int plForValuationAboveTenMillion = 15;

        [Header("Custo dos Nós da Árvore")]
        [SerializeField] private long incrementalNodeFirstCost = 10_000;
        [Tooltip("Multiplicador aplicado ao custo a cada compra subsequente do mesmo nó incremental.")]
        [SerializeField] private float incrementalNodeCostMultiplier = 2f;
        [SerializeField] private int uniqueNodeMinCostPL = 5;
        [SerializeField] private int uniqueNodeMaxCostPL = 25;
        [SerializeField] private long legacyResetCost = 50_000;

        public int PlForReachingIndie => plForReachingIndie;
        public int PlForReachingOffice => plForReachingOffice;
        public int PlForReachingStudio => plForReachingStudio;
        public int PlPerGameAboveScoreSeven => plPerGameAboveScoreSeven;
        public int PlForFranchiseWithThreeOrMoreGames => plForFranchiseWithThreeOrMoreGames;
        public int PlForRunWithoutMajorCrisis => plForRunWithoutMajorCrisis;
        public int PlForValuationAboveOneMillion => plForValuationAboveOneMillion;
        public int PlForValuationAboveTenMillion => plForValuationAboveTenMillion;

        public long IncrementalNodeFirstCost => incrementalNodeFirstCost;
        public float IncrementalNodeCostMultiplier => incrementalNodeCostMultiplier;
        public int UniqueNodeMinCostPL => uniqueNodeMinCostPL;
        public int UniqueNodeMaxCostPL => uniqueNodeMaxCostPL;
        public long LegacyResetCost => legacyResetCost;
    }
}
