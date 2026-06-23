using System;
using UnityEngine;

namespace GameLike.Data.Balance
{
    /// <summary>
    /// Engine Budget (EB) tiers from GDD section 19.4 and 6.2. Modules are
    /// categorized into three weight tiers, each with its own EB cost and
    /// weekly upkeep cost. Individual EngineModuleSO assets (Fase 1, TODO)
    /// will reference one of these tiers rather than duplicating the numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "GameLike/Balance/Engine Balance", fileName = "EngineBalance")]
    public sealed class EngineBalanceSO : BalanceAsset
    {
        [Serializable]
        public struct ModuleTierCost
        {
            public int engineBudgetCost;
            public long weeklyUpkeepCost;
        }

        [Header("Custo por Categoria de Módulo (GDD 19.4)")]
        [SerializeField] private ModuleTierCost lightModule = new() { engineBudgetCost = 100, weeklyUpkeepCost = 100 };
        [SerializeField] private ModuleTierCost mediumModule = new() { engineBudgetCost = 250, weeklyUpkeepCost = 300 };
        [SerializeField] private ModuleTierCost heavyModule = new() { engineBudgetCost = 500, weeklyUpkeepCost = 700 };

        [Header("Engine Budget Total Estimado por Porte")]
        [SerializeField] private int basicEngineBudget = 200;
        [SerializeField] private int intermediateEngineBudget = 600;
        [SerializeField] private int advancedEngineBudget = 1_200;
        [SerializeField] private int completeEngineBudget = 2_000;

        public ModuleTierCost LightModule => lightModule;
        public ModuleTierCost MediumModule => mediumModule;
        public ModuleTierCost HeavyModule => heavyModule;

        public int BasicEngineBudget => basicEngineBudget;
        public int IntermediateEngineBudget => intermediateEngineBudget;
        public int AdvancedEngineBudget => advancedEngineBudget;
        public int CompleteEngineBudget => completeEngineBudget;
    }
}
