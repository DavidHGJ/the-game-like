using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Economy;
using GameLike.Systems.Meta;
using UnityEngine;

namespace GameLike.Systems.Research
{
    /// <summary>
    /// Sistema de P&amp;D (GDD 6 / adendo 23.5). Pontos de Pesquisa (PP) nascem APENAS durante o
    /// desenvolvimento de um jogo, ao lançar e ao concluir uma pesquisa — nunca com a empresa
    /// parada. Cada pesquisa custa PP na largada e um budget semanal enquanto roda; uma por vez.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResearchManager : MonoBehaviour
    {
        private RunState _run;
        private EconomyManager _economy;
        private List<ResearchNode> _catalog = new List<ResearchNode>();
        private float _pointRemainder;

        public int Points => _run?.research?.points ?? 0;
        public IReadOnlyList<ResearchNode> Catalog => _catalog;

        private void OnEnable()
        {
            EventBus.Subscribe<WeekTickEvent>(OnWeek);
            EventBus.Subscribe<GameLaunchedEvent>(OnLaunched);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WeekTickEvent>(OnWeek);
            EventBus.Unsubscribe<GameLaunchedEvent>(OnLaunched);
        }

        /// <summary>Liga o manager ao estado da run e monta o catálogo a partir dos temas disponíveis.</summary>
        public void Bind(RunState run, EconomyManager economy, IEnumerable<ThemeSO> themes)
        {
            _run = run;
            _economy = economy;
            _catalog = ResearchCatalog.Build(themes);
            _pointRemainder = 0f;
            if (_run != null)
            {
                if (_run.research == null) _run.research = new ResearchState();
                SeedDefaults(themes);
            }
        }

        private void SeedDefaults(IEnumerable<ThemeSO> themes)
        {
            var r = _run.research;
            for (int i = 0; i < ResearchCatalog.StarterEngineModules.Length; i++)
            {
                string id = ResearchCatalog.EngineNodeId(ResearchCatalog.StarterEngineModules[i]);
                if (!r.unlocked.Contains(id)) r.unlocked.Add(id);
            }

            bool hasTheme = false;
            for (int i = 0; i < r.unlocked.Count; i++)
                if (r.unlocked[i].StartsWith("theme:")) { hasTheme = true; break; }
            if (hasTheme || themes == null) return;

            int seeded = 0;
            foreach (var t in themes)
            {
                if (t == null) continue;
                r.unlocked.Add(ResearchCatalog.ThemeNodeId(t.name));
                if (++seeded >= 2) break;
            }
        }

        public bool IsUnlocked(string id) => _run != null && _run.research.IsUnlocked(id);
        public bool IsThemeUnlocked(string themeKey) => IsUnlocked(ResearchCatalog.ThemeNodeId(themeKey));
        public bool IsSizeUnlocked(ProjectSize size) =>
            size == ProjectSize.Small || IsUnlocked(ResearchCatalog.SizeNodeId(size));
        public bool IsMarketingUnlocked(string key) => IsUnlocked(ResearchCatalog.MarketingNodeId(key));

        public ResearchNode Active =>
            _run != null && _run.research.HasActiveResearch ? Find(_run.research.activeId) : null;
        public int ActiveWeeksRemaining => _run?.research?.activeWeeksRemaining ?? 0;

        /// <summary>Progresso 0..1 da pesquisa em andamento.</summary>
        public float ActiveProgress01
        {
            get
            {
                var r = _run?.research;
                if (r == null || !r.HasActiveResearch || r.activeWeeksTotal <= 0) return 0f;
                return Mathf.Clamp01(1f - r.activeWeeksRemaining / (float)r.activeWeeksTotal);
            }
        }

        /// <summary>Engine Budget atual da engine da empresa (adendo 23.7).</summary>
        public int EngineBudget =>
            EngineCalculator.TotalBudget(_catalog, _run?.research, BalanceProvider.Get<EngineBalanceSO>());

        /// <summary>Custo semanal da engine enquanto há jogo em produção.</summary>
        public long EngineWeeklyCost =>
            EngineCalculator.WeeklyCost(_catalog, _run?.research, BalanceProvider.Get<EngineBalanceSO>());

        /// <summary>Pesquisas ainda não desbloqueadas e permitidas pela fase atual.</summary>
        public IEnumerable<ResearchNode> Available()
        {
            if (_run == null) yield break;
            var phase = _run.company.phase;
            for (int i = 0; i < _catalog.Count; i++)
            {
                var n = _catalog[i];
                if (_run.research.IsUnlocked(n.Id)) continue;
                if ((int)n.MinPhase > (int)phase) continue;
                yield return n;
            }
        }

        public int CostPP(ResearchNode node)
        {
            var b = BalanceProvider.Get<ResearchBalanceSO>();
            if (node == null) return 0;
            int cost = node.Tier switch
            {
                ResearchTier.Basic => b != null ? b.BasicResearchCostPP : 10,
                ResearchTier.Intermediate => b != null ? b.IntermediateResearchCostPP : 30,
                ResearchTier.Advanced => b != null ? b.AdvancedResearchCostPP : 80,
                _ => 10
            };
            if (node.Kind == UnlockKind.Engine) cost = Mathf.CeilToInt(cost * FounderPerks.EngineResearchCostMultiplier);
            if (node.Id == ResearchCatalog.FeatureAcquisition) cost = Mathf.CeilToInt(cost * FounderPerks.AcquisitionResearchCostMultiplier);
            return cost;
        }

        public long WeeklyCost(ResearchNode node) =>
            node == null ? 0 : WeeklyCostFor(node.Tier, BalanceProvider.Get<ResearchBalanceSO>());

        public int WeeksToComplete(ResearchNode node) =>
            node == null ? 0 : WeeksFor(node.Tier, BalanceProvider.Get<ResearchBalanceSO>());

        public bool CanResearch(ResearchNode node)
        {
            if (node == null || _run == null) return false;
            if (_run.research.HasActiveResearch) return false;
            if (_run.research.IsUnlocked(node.Id)) return false;
            if ((int)node.MinPhase > (int)_run.company.phase) return false;
            return _run.research.points >= CostPP(node);
        }

        /// <summary>Inicia uma pesquisa: debita os PP e passa a cobrar o budget semanal.</summary>
        public bool StartResearch(string id)
        {
            var node = Find(id);
            if (!CanResearch(node)) return false;
            var b = BalanceProvider.Get<ResearchBalanceSO>();
            _run.research.points -= CostPP(node);
            _run.research.activeId = node.Id;
            _run.research.activeWeeksRemaining = WeeksFor(node.Tier, b);
            _run.research.activeWeeksTotal = _run.research.activeWeeksRemaining;
            _run.research.activeWeeklyCost = WeeklyCostFor(node.Tier, b);
            EventBus.Publish(new ResearchStartedEvent(node.Id, node.Label));
            EventBus.Publish(new ResearchPointsChangedEvent(_run.research.points));
            return true;
        }

        private void OnWeek(WeekTickEvent e) => AdvanceWeek();
        private void OnLaunched(GameLaunchedEvent e) => RegisterGameLaunched();

        /// <summary>Avança uma semana de P&amp;D (público para testes).</summary>
        public void AdvanceWeek()
        {
            var r = _run?.research;
            if (r == null || _run.ended) return;
            var b = BalanceProvider.Get<ResearchBalanceSO>();

            if (_run.IsDeveloping)
                AddPoints(b != null ? b.PpPerDevelopmentWeek : 2);

            if (!r.HasActiveResearch) return;
            if (_economy != null && r.activeWeeklyCost > 0)
                _economy.ApplyDelta(-r.activeWeeklyCost, "P&D: pesquisa");
            r.activeWeeksRemaining--;
            if (r.activeWeeksRemaining > 0) return;

            var doneId = r.activeId;
            var node = Find(doneId);
            if (!r.unlocked.Contains(doneId)) r.unlocked.Add(doneId);
            r.activeId = null;
            r.activeWeeklyCost = 0;
            r.activeWeeksRemaining = 0;
            r.activeWeeksTotal = 0;
            EventBus.Publish(new ResearchCompletedEvent(doneId, node != null ? node.Label : doneId));
            AddPoints(b != null ? b.PpPerResearchCompleted : 2);
        }

        /// <summary>Soma os PP de um jogo lançado (GDD 19.3).</summary>
        public void RegisterGameLaunched()
        {
            if (_run?.research == null) return;
            var b = BalanceProvider.Get<ResearchBalanceSO>();
            AddPoints(b != null ? b.PpPerGameLaunched : 5);
        }

        private void AddPoints(int basePoints)
        {
            float total = basePoints * FounderPerks.ResearchPointMultiplier + _pointRemainder;
            int whole = Mathf.FloorToInt(total);
            _pointRemainder = total - whole;
            if (whole <= 0) return;
            _run.research.points += whole;
            EventBus.Publish(new ResearchPointsChangedEvent(_run.research.points));
        }

        public ResearchNode Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < _catalog.Count; i++)
                if (_catalog[i].Id == id) return _catalog[i];
            return null;
        }

        private static int WeeksFor(ResearchTier tier, ResearchBalanceSO b) => tier switch
        {
            ResearchTier.Basic => b != null ? b.ResearchWeeksBasic : 2,
            ResearchTier.Intermediate => b != null ? b.ResearchWeeksIntermediate : 4,
            ResearchTier.Advanced => b != null ? b.ResearchWeeksAdvanced : 8,
            _ => 2
        };

        private static long WeeklyCostFor(ResearchTier tier, ResearchBalanceSO b) => tier switch
        {
            ResearchTier.Basic => b != null ? b.BasicResearchWeeklyCost : 200,
            ResearchTier.Intermediate => b != null ? b.IntermediateResearchWeeklyCost : 800,
            ResearchTier.Advanced => b != null ? b.AdvancedResearchWeeklyCost : 2000,
            _ => 200
        };
    }
}
