using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Meta;
using GameLike.Systems.Economy;
using GameLike.Systems.Market;
using GameLike.Systems.Meta;
using GameLike.Systems.Projects;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Regras determinísticas do loop (adendo 23 do GDD): produção em fases, cauda de vendas,
    /// preço, probabilidade de M&amp;A (GDD 12.3) e a árvore de habilidades (GDD 14 / 19.5).
    /// </summary>
    [TestFixture]
    public class GameLoopRulesTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            FounderPerks.Bind(null);
            foreach (var o in _spawned)
                if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        // ---------------- parâmetros por fase ----------------

        [Test]
        public void ParameterCatalog_SmallScope_HidesGatedParameters()
        {
            var pre = ParameterCatalog.AvailableFor(ProductionPhase.PreProduction, ProjectSize.Small);
            CollectionAssert.DoesNotContain(pre, GameParameter.GameFeel, "Game Feel é Médio+ (GDD 5.2)");
            CollectionAssert.DoesNotContain(pre, GameParameter.Scope, "Escopo é Grande+ (GDD 5.2)");
            Assert.AreEqual(3, pre.Count);
            Assert.AreEqual(5, ParameterCatalog.AvailableFor(ProductionPhase.PreProduction, ProjectSize.Large).Count);
        }

        [Test]
        public void ParameterCatalog_PhaseDurations_SumToOne()
        {
            float sum = 0f;
            for (int i = 0; i < ParameterCatalog.PhaseCount; i++) sum += ParameterCatalog.DurationShare((ProductionPhase)i);
            Assert.AreEqual(1f, sum, 0.0001f);
        }

        [Test]
        public void PhaseMatch_IdealAllocation_ScoresHigherThanInverted()
        {
            var genre = ScriptableObject.CreateInstance<GenreSO>();
            _spawned.Add(genre);
            var phase = ProductionPhase.Art;
            var parameters = ParameterCatalog.AvailableFor(phase, ProjectSize.Small);
            var ideal = QualityCalculator.IdealPhaseShares(genre, ProjectSize.Small, phase);

            var good = new float[ParameterCatalog.ParameterCount];
            var bad = new float[ParameterCatalog.ParameterCount];
            for (int i = 0; i < parameters.Count; i++) good[(int)parameters[i]] = ideal[i] * 100f;
            bad[(int)parameters[0]] = 100f; // tudo em um parâmetro só

            Assert.AreEqual(1f, QualityCalculator.PhaseMatch(genre, ProjectSize.Small, phase, good), 0.001f);
            Assert.Less(QualityCalculator.PhaseMatch(genre, ProjectSize.Small, phase, bad),
                QualityCalculator.PhaseMatch(genre, ProjectSize.Small, phase, good));
        }

        [Test]
        public void PhaseMatch_NoPoints_IsZero()
        {
            var genre = ScriptableObject.CreateInstance<GenreSO>();
            _spawned.Add(genre);
            var empty = new float[ParameterCatalog.ParameterCount];
            Assert.AreEqual(0f, QualityCalculator.PhaseMatch(genre, ProjectSize.Small, ProductionPhase.Audio, empty));
        }

        [Test]
        public void AreaSharesFromParameters_SumToOne()
        {
            var points = new float[ParameterCatalog.ParameterCount];
            points[(int)GameParameter.Programming] = 60f;
            points[(int)GameParameter.Graphics] = 40f;
            var areas = QualityCalculator.AreaSharesFromParameters(points);
            float sum = 0f;
            foreach (float a in areas) sum += a;
            Assert.AreEqual(1f, sum, 0.0001f);
            Assert.AreEqual(0.6f, areas[(int)DevArea.Tech], 0.0001f);
        }

        // ---------------- vendas ----------------

        [Test]
        public void TailUnits_SumToTotal_AndDecay()
        {
            const long total = 100_000;
            const int weeks = 26;
            long sum = 0;
            long previous = long.MaxValue;
            for (int w = 0; w < weeks; w++)
            {
                long u = SalesCalculator.TailUnitsForWeek(total, w, weeks, 0.8f);
                Assert.LessOrEqual(u, previous, "a cauda só cai depois do pico de lançamento");
                previous = u;
                sum += u;
            }
            Assert.AreEqual(total, sum, total * 0.001, "a cauda distribui o total previsto");
            Assert.AreEqual(0, SalesCalculator.TailUnitsForWeek(total, weeks, weeks, 0.8f), "depois da cauda não vende");
        }

        [Test]
        public void TailRetention_BetterScore_SlowerDecay()
        {
            Assert.Greater(SalesCalculator.TailRetention(9f, null), SalesCalculator.TailRetention(4f, null));
        }

        [Test]
        public void PriceFactor_HigherPrice_SellsLess()
        {
            Assert.AreEqual(1f, SalesCalculator.PriceFactor(20, 20, 1.3f), 0.0001f);
            Assert.Less(SalesCalculator.PriceFactor(40, 20, 1.3f), 1f);
            Assert.Greater(SalesCalculator.PriceFactor(10, 20, 1.3f), 1f);
        }

        [Test]
        public void LaunchBudgetHype_HasDiminishingReturns()
        {
            float first = SalesCalculator.LaunchBudgetHype(10_000, null);
            float second = SalesCalculator.LaunchBudgetHype(20_000, null) - first;
            Assert.Greater(first, 0f);
            Assert.Less(second, first);
            Assert.AreEqual(0f, SalesCalculator.LaunchBudgetHype(0, null));
        }

        // ---------------- M&A (GDD 12.3) ----------------

        [TestCase(1f, 0.05f)]
        [TestCase(1.5f, 0.25f)]
        [TestCase(2f, 0.55f)]
        [TestCase(3f, 0.85f)]
        public void AcceptChance_FollowsGddCurve(float multiple, float expected)
        {
            float chance = AcquisitionCalculator.AcceptChance(multiple, false, CompanyPhase.Office, 0f, 0f);
            Assert.AreEqual(expected, chance, 0.001f);
        }

        [Test]
        public void AcceptChance_CrisisNetworkingAndSmallRival_Help_HostilityHurts()
        {
            float baseline = AcquisitionCalculator.AcceptChance(1.5f, false, CompanyPhase.Office, 0f, 0f);
            Assert.Greater(AcquisitionCalculator.AcceptChance(1.5f, true, CompanyPhase.Office, 0f, 0f), baseline);
            Assert.Greater(AcquisitionCalculator.AcceptChance(1.5f, false, CompanyPhase.Office, 0.2f, 0f), baseline);
            Assert.Greater(AcquisitionCalculator.AcceptChance(1.5f, false, CompanyPhase.Garage, 0f, 0f), baseline);
            Assert.Less(AcquisitionCalculator.AcceptChance(1.5f, false, CompanyPhase.Studio, 0f, 0f), baseline);
            Assert.Less(AcquisitionCalculator.AcceptChance(1.5f, false, CompanyPhase.Office, 0f, 1f), baseline);
        }

        // ---------------- árvore de habilidades (GDD 14 / 19.5) ----------------

        [Test]
        public void SkillTree_HasSixTreesWithFourPerksEach_AndCostsInRange()
        {
            Assert.AreEqual(6, SkillTreeCatalog.All.Count);
            foreach (var tree in SkillTreeCatalog.All)
            {
                Assert.AreEqual(4, tree.Perks.Length, tree.Name);
                foreach (var perk in tree.Perks)
                    Assert.That(perk.CostPL, Is.InRange(5, 25), perk.Name + " (GDD 19.5: 5 a 25 PL)");
            }
        }

        [Test]
        public void BuyRoot_CostDoublesEachPurchase()
        {
            var meta = new MetaProgressionState { personalMoney = 100_000 };
            Assert.AreEqual(10_000, SkillTreeCatalog.RootCost(0));
            Assert.IsTrue(SkillTreeCatalog.TryBuyRoot(meta, FounderSkill.Technical));
            Assert.AreEqual(90_000, meta.personalMoney);
            Assert.AreEqual(20_000, SkillTreeCatalog.RootCost(meta.skillLevels[(int)FounderSkill.Technical - 1]));
            Assert.IsTrue(SkillTreeCatalog.TryBuyRoot(meta, FounderSkill.Technical));
            Assert.AreEqual(70_000, meta.personalMoney);
            Assert.AreEqual(2, meta.skillLevels[(int)FounderSkill.Technical - 1]);
        }

        [Test]
        public void BuyPerk_IsUnique_AndResetRefundsLegacyButKeepsRoots()
        {
            var meta = new MetaProgressionState { personalMoney = 100_000, legacyPoints = 20 };
            SkillTreeCatalog.TryBuyRoot(meta, FounderSkill.Technical);
            Assert.IsTrue(SkillTreeCatalog.TryBuyPerk(meta, SkillTreeCatalog.CodeReview));
            Assert.IsFalse(SkillTreeCatalog.TryBuyPerk(meta, SkillTreeCatalog.CodeReview), "nó único só compra uma vez");
            Assert.AreEqual(15, meta.legacyPoints);

            Assert.IsTrue(SkillTreeCatalog.TryResetPerks(meta));
            Assert.AreEqual(20, meta.legacyPoints, "PL devolvidos");
            Assert.AreEqual(0, meta.perks.Count);
            Assert.AreEqual(1, meta.skillLevels[(int)FounderSkill.Technical - 1], "incrementais não resetam (GDD 14.3)");
            Assert.AreEqual(100_000 - 10_000 - 50_000, meta.personalMoney, "reset custa 50.000 (GDD 19.5)");
        }

        [Test]
        public void FounderPerks_ReflectMeta()
        {
            var meta = new MetaProgressionState { legacyPoints = 50 };
            FounderPerks.Bind(meta);
            Assert.AreEqual(1f, FounderPerks.BugMultiplier);
            SkillTreeCatalog.TryBuyPerk(meta, SkillTreeCatalog.CodeReview);
            Assert.Less(FounderPerks.BugMultiplier, 1f);
            meta.skillLevels[(int)FounderSkill.Business - 1] = 2;
            Assert.AreEqual(1.10f, FounderPerks.ValuationMultiplier, 0.0001f);
        }

        // ---------------- fluxo de fases no ProjectManager ----------------

        [Test]
        public void Project_RunsFivePhases_ThenWaitsForLaunch_ThenReviews_ThenSellsOverWeeks()
        {
            var go = new GameObject("project-test");
            _spawned.Add(go);
            var eco = go.AddComponent<EconomyManager>();
            var pm = go.AddComponent<ProjectManager>();
            var genre = ScriptableObject.CreateInstance<GenreSO>();
            _spawned.Add(genre);

            var company = new CompanyState("Test", 100_000);
            var run = new RunState(company);
            eco.Bind(company);
            pm.Bind(run, eco, null, null, _ => genre, _ => null);

            var project = new ProjectState("Jogo", genre.Id, "G", ProjectSize.Small, null);
            pm.StartProject(genre, null, project);
            Assert.IsTrue(project.awaitingAllocation, "a 1ª fase espera a distribuição de pontos");
            Assert.AreEqual(100, project.phasePointBudget, "fundador sozinho = 100 pontos por fase (GDD 5.3)");

            int guard = 0;
            int phasesConfirmed = 0;
            while (project.status == ProjectStatus.InDevelopment && guard++ < 200)
            {
                if (project.awaitingAllocation)
                {
                    Assert.IsTrue(pm.ConfirmAllocation(pm.IdealAllocation(project), false));
                    phasesConfirmed++;
                }
                pm.AdvanceWeek();
            }

            Assert.AreEqual(5, phasesConfirmed, "5 fases de produção (GDD 5.2)");
            Assert.AreEqual(ProjectStatus.ReadyToLaunch, project.status, "fim da produção não lança sozinho");
            Assert.IsTrue(run.HasActiveProject);

            long cashBefore = company.cash;
            Assert.IsTrue(pm.Launch(pm.ReferencePrice(project), 0));
            Assert.AreEqual(ProjectStatus.Released, project.status);
            Assert.AreEqual(ProjectManager.ReviewerCount, project.reviewScores.Length);
            Assert.IsNull(run.currentProject);
            Assert.IsTrue(project.awaitingReview, "as críticas só saem na semana seguinte");
            Assert.AreEqual(0, project.salesWeek, "nada vende no dia do lançamento");
            Assert.AreEqual(cashBefore, company.cash, "sem receita antes das críticas");

            int launchedEvents = 0;
            EventBus.Subscribe<GameLaunchedEvent>(_ => launchedEvents++);
            pm.AdvanceWeek(); // semana seguinte: críticas publicadas, ainda sem vendas
            Assert.IsFalse(project.awaitingReview);
            Assert.AreEqual(1, launchedEvents, "o fato do lançamento (com a nota) é anunciado junto com as críticas");
            Assert.AreEqual(0, project.salesWeek, "as vendas começam DEPOIS das críticas");

            pm.AdvanceWeek(); // primeira semana de vendas: o pico
            Assert.AreEqual(1, project.salesWeek);
            Assert.Greater(company.cash, cashBefore, "a receita entra semana a semana");

            long afterFirst = company.cash;
            pm.AdvanceWeek();
            Assert.AreEqual(2, project.salesWeek);
            Assert.GreaterOrEqual(company.cash, afterFirst, "a cauda continua pagando");
            Assert.LessOrEqual(project.weeklyUnits[1], project.weeklyUnits[0], "o pico é na primeira semana de vendas");
        }

        [Test]
        public void ConfirmAllocation_ClampsToPhaseBudget()
        {
            var go = new GameObject("project-test");
            _spawned.Add(go);
            var eco = go.AddComponent<EconomyManager>();
            var pm = go.AddComponent<ProjectManager>();
            var genre = ScriptableObject.CreateInstance<GenreSO>();
            _spawned.Add(genre);
            var company = new CompanyState("Test", 100_000);
            var run = new RunState(company);
            eco.Bind(company);
            pm.Bind(run, eco, null, null, _ => genre, _ => null);

            var project = new ProjectState("Jogo", genre.Id, "G", ProjectSize.Small, null);
            pm.StartProject(genre, null, project);

            var greedy = new float[ParameterCatalog.ParameterCount];
            foreach (var p in ParameterCatalog.AvailableFor(ProductionPhase.PreProduction, ProjectSize.Small))
                greedy[(int)p] = 300f;
            greedy[(int)GameParameter.Programming] = 300f; // fora da fase: ignorado
            Assert.IsTrue(pm.ConfirmAllocation(greedy, false));

            float total = 0f;
            foreach (float v in project.paramAllocated) total += v;
            Assert.AreEqual(project.phasePointBudget, total, 0.01f, "não dá para gastar mais que o orçamento da fase");
            Assert.AreEqual(0f, project.paramAllocated[(int)GameParameter.Programming]);
        }
    }
}
