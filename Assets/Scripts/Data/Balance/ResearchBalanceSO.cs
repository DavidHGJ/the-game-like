using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Research & Development balance values from GDD section 19.3.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Balance/Research Balance", fileName = "ResearchBalance")]
    public sealed class ResearchBalanceSO : BalanceAsset
    {
        [Header("Custo em Pontos de Pesquisa (PP)")]
        [SerializeField] private int basicResearchCostPP = 10;
        [SerializeField] private int intermediateResearchCostPP = 30;
        [SerializeField] private int advancedResearchCostPP = 80;

        [Header("Custo Semanal Durante Pesquisa ($)")]
        [SerializeField] private long basicResearchWeeklyCost = 200;
        [SerializeField] private long intermediateResearchWeeklyCost = 800;
        [SerializeField] private long advancedResearchWeeklyCost = 2_000;

        [Header("Geração de PP")]
        [SerializeField] private int ppPerActiveResearchWeek = 2;
        [SerializeField] private int ppPerGameLaunched = 5;
        [SerializeField] private int ppPerManualStudyWeek = 3;

        public int BasicResearchCostPP => basicResearchCostPP;
        public int IntermediateResearchCostPP => intermediateResearchCostPP;
        public int AdvancedResearchCostPP => advancedResearchCostPP;

        public long BasicResearchWeeklyCost => basicResearchWeeklyCost;
        public long IntermediateResearchWeeklyCost => intermediateResearchWeeklyCost;
        public long AdvancedResearchWeeklyCost => advancedResearchWeeklyCost;

        public int PpPerActiveResearchWeek => ppPerActiveResearchWeek;
        public int PpPerGameLaunched => ppPerGameLaunched;
        public int PpPerManualStudyWeek => ppPerManualStudyWeek;
    }
}
