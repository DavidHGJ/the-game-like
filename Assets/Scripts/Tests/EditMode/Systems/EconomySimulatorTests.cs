using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Projects;
using GameLike.Systems.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// O simulador de economia tem de seguir as mesmas regras do jogo. Os testes não fixam
    /// valores de balanceamento (eles mudam na tela de Parâmetros): conferem as regras.
    /// </summary>
    [TestFixture]
    public sealed class EconomySimulatorTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _spawned)
                if (obj != null) Object.DestroyImmediate(obj);
            _spawned.Clear();
            EventBus.Clear();
        }

        private static SimScenario Profitability(ProjectSize size, params float[] scores) => new SimScenario
        {
            phase = CompanyPhase.Garage, size = size, scores = scores, startingCash = 0, ignoreBankruptcy = true
        };

        [Test]
        public void DevelopmentWeeks_MatchesTheRealProjectPhases()
        {
            var go = new GameObject("sim-test");
            _spawned.Add(go);
            var pm = go.AddComponent<ProjectManager>();
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            foreach (ProjectSize size in System.Enum.GetValues(typeof(ProjectSize)))
            {
                var project = new ProjectState("x", "g", "G", size, null) { targetWeeks = loop.WeeksFor(size) };
                int real = 0;
                for (int phase = 0; phase < ParameterCatalog.PhaseCount; phase++) real += pm.WeeksForPhase(project, phase, false);
                Assert.AreEqual(real, EconomySimulator.DevelopmentWeeks(size, loop), size.ToString());
            }
        }

        [Test]
        public void Run_SalesStartTwoWeeksAfterLaunch_LikeTheGame()
        {
            var result = EconomySimulator.Run(Profitability(ProjectSize.Small, 8f));
            Assert.AreEqual(1, result.games.Count);
            Assert.AreEqual(result.games[0].launchWeek + 2, result.firstSaleWeek, "críticas na semana seguinte; vendas depois delas");
        }

        [Test]
        public void Run_SellsTheWholeForecastOverTheTail()
        {
            var game = EconomySimulator.Run(Profitability(ProjectSize.Small, 7f)).games[0];
            Assert.Greater(game.forecastUnits, 0);
            Assert.AreEqual(game.forecastUnits, game.units, game.forecastUnits * 0.02 + 2, "a cauda soma a venda prevista");
            Assert.AreEqual(game.units * BalanceProvider.Get<GameLoopBalanceSO>().PriceFor(ProjectSize.Small), game.revenue);
        }

        [Test]
        public void Run_ForecastMatchesTheGameSalesFormula()
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var quality = BalanceProvider.Get<QualityFormulaBalanceSO>();
            var game = EconomySimulator.Run(Profitability(ProjectSize.Medium, 6.5f)).games[0];
            var expected = SalesCalculator.Compute(6.5f, ProjectSize.Medium, game.hypeAtLaunch, game.wishlists, loop, quality);
            Assert.AreEqual(expected.units, game.forecastUnits);
        }

        [Test]
        public void Run_BetterScoreNeverSellsLess()
        {
            long previous = -1;
            foreach (float score in new[] { 2f, 4f, 6f, 8f, 10f })
            {
                long units = EconomySimulator.Run(Profitability(ProjectSize.Small, score)).games[0].forecastUnits;
                Assert.GreaterOrEqual(units, previous, "nota " + score);
                previous = units;
            }
        }

        [Test]
        public void Run_ChargesOperatingCostEveryWeek()
        {
            var economy = BalanceProvider.Get<EconomyBalanceSO>();
            var result = EconomySimulator.Run(new SimScenario
            {
                phase = CompanyPhase.Garage, size = ProjectSize.Small, scores = new float[0],
                startingCash = 1_000_000, ignoreBankruptcy = true, maxWeeks = 1
            });
            Assert.LessOrEqual(result.cashByWeek[0], 1_000_000 - economy.WeeklyOperatingFor(CompanyPhase.Garage) + 1);
        }

        [Test]
        public void Run_GoesBankruptAfterTheGraceWeeksInTheRed()
        {
            int grace = BalanceProvider.Get<GameTuningBalanceSO>().BankruptcyGraceWeeks;
            var result = EconomySimulator.Run(new SimScenario
            {
                phase = CompanyPhase.Studio, size = ProjectSize.AAA, scores = new[] { 5f }, startingCash = 0, employees = 8
            });
            Assert.IsTrue(result.Bankrupt);
            int firstNegative = result.cashByWeek.FindIndex(c => c < 0) + 1;
            Assert.AreEqual(firstNegative + grace, result.bankruptWeek, "quebra quando passa da carência");
        }

        [Test]
        public void Run_IgnoringBankruptcy_LaunchesEveryGame()
        {
            var result = EconomySimulator.Run(new SimScenario
            {
                phase = CompanyPhase.Studio, size = ProjectSize.Small, scores = new[] { 2f, 2f, 2f },
                startingCash = 0, employees = 8, ignoreBankruptcy = true
            });
            Assert.IsFalse(result.Bankrupt);
            Assert.AreEqual(3, result.games.Count);
        }

        [Test]
        public void SalesShare_WithoutFloorAndThreshold_IsTheOldPowerCurve()
        {
            var loop = ScriptableObject.CreateInstance<GameLoopBalanceSO>();
            _spawned.Add(loop);
            JsonUtility.FromJsonOverwrite("{\"salesScoreExponent\":2.4,\"salesFloorShare\":0,\"salesScoreThreshold\":0}", loop);
            foreach (float score in new[] { 0f, 3f, 6.5f, 10f })
                Assert.AreEqual(Mathf.Pow(score / 10f, 2.4f), loop.SalesShareForScore(score), 1e-5f, "nota " + score);
        }

        [Test]
        public void SalesShare_BelowThreshold_SellsOnlyTheFloor_AndRisesAboveIt()
        {
            var loop = ScriptableObject.CreateInstance<GameLoopBalanceSO>();
            _spawned.Add(loop);
            JsonUtility.FromJsonOverwrite("{\"salesScoreExponent\":2,\"salesFloorShare\":0.3,\"salesScoreThreshold\":5}", loop);
            Assert.AreEqual(0.3f, loop.SalesShareForScore(2f), 1e-5f);
            Assert.AreEqual(0.3f, loop.SalesShareForScore(5f), 1e-5f, "fracassos vendem o mesmo piso");
            Assert.AreEqual(0.3f + 0.7f * 0.25f, loop.SalesShareForScore(7.5f), 1e-5f);
            Assert.AreEqual(1f, loop.SalesShareForScore(10f), 1e-5f, "nota 10 vende tudo");
        }

        [Test]
        public void Run_StopAtLastLaunch_EndsWhenTheLastGameIsReleased()
        {
            var result = EconomySimulator.Run(new SimScenario
            {
                phase = CompanyPhase.Garage, size = ProjectSize.Small, scores = new[] { 8f, 8f },
                startingCash = 10_000_000, stopAtLastLaunch = true
            });
            Assert.AreEqual(2, result.games.Count);
            Assert.AreEqual(result.games[1].launchWeek, result.weeks);
        }

        [Test]
        public void Report_RunsEveryScenarioInEveryBand()
        {
            var rows = EconomyReport.RunAll();
            int expected = 0;
            foreach (var scenario in EconomyReport.StandardScenarios()) expected += scenario.bands.Length;
            Assert.AreEqual(expected, rows.Count);
            Assert.IsTrue(rows.TrueForAll(r => !string.IsNullOrEmpty(r.verdict)));
        }
    }
}
