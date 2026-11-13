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
        [Tooltip("PP por semana de DESENVOLVIMENTO de um jogo (adendo 23.5 — nunca com a empresa parada).")]
        [SerializeField] private int ppPerDevelopmentWeek = 2;
        [SerializeField] private int ppPerGameLaunched = 5;
        [Tooltip("PP ganho ao concluir uma pesquisa (adendo 23.5).")]
        [SerializeField] private int ppPerResearchCompleted = 2;

        [Header("Duração da Pesquisa (semanas)")]
        [SerializeField] private int researchWeeksBasic = 2;
        [SerializeField] private int researchWeeksIntermediate = 4;
        [SerializeField] private int researchWeeksAdvanced = 8;

        public int BasicResearchCostPP => basicResearchCostPP;
        public int IntermediateResearchCostPP => intermediateResearchCostPP;
        public int AdvancedResearchCostPP => advancedResearchCostPP;

        public long BasicResearchWeeklyCost => basicResearchWeeklyCost;
        public long IntermediateResearchWeeklyCost => intermediateResearchWeeklyCost;
        public long AdvancedResearchWeeklyCost => advancedResearchWeeklyCost;

        public int PpPerDevelopmentWeek => ppPerDevelopmentWeek;
        public int PpPerGameLaunched => ppPerGameLaunched;
        public int PpPerResearchCompleted => ppPerResearchCompleted;

        public int ResearchWeeksBasic => researchWeeksBasic;
        public int ResearchWeeksIntermediate => researchWeeksIntermediate;
        public int ResearchWeeksAdvanced => researchWeeksAdvanced;
    }
}
