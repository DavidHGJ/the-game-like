using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Systems.Simulation;
using NUnit.Framework;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>Regras da simulação longa (carreiras de várias runs), sem fixar valores de balanceamento.</summary>
    [TestFixture]
    public sealed class CareerSimulatorTests
    {
        private static CareerProfile Profile(float mean, bool sells = true) => new CareerProfile
        {
            name = "teste", skillMean = mean, skillSpread = 1f, sellsBeforeBankruptcy = sells
        };

        [Test]
        public void RunCareer_SameSeed_GivesTheSameResult()
        {
            var a = CareerSimulator.RunCareer(Profile(6.5f), 3, 300, new System.Random(42));
            var b = CareerSimulator.RunCareer(Profile(6.5f), 3, 300, new System.Random(42));
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(a[i].weeks, b[i].weeks);
                Assert.AreEqual(a[i].payout, b[i].payout);
                Assert.AreEqual(a[i].maxPhase, b[i].maxPhase);
            }
        }

        [Test]
        public void RunCareer_NextRunStartsWithThePreviousPayout()
        {
            var runs = CareerSimulator.RunCareer(Profile(7f), 3, 300, new System.Random(7));
            long baseCash = BalanceProvider.Get<GameLoopBalanceSO>().StartingCash;
            Assert.AreEqual(baseCash, runs[0].startingCash);
            for (int i = 1; i < runs.Count; i++)
                Assert.AreEqual(baseCash + runs[i - 1].payout, runs[i].startingCash, "o dinheiro da venda vira capital");
        }

        [Test]
        public void RunOnce_BankruptRun_PaysNothing()
        {
            var run = CareerSimulator.RunOnce(Profile(1f, sells: false), 0, 520, new System.Random(3));
            Assert.AreEqual(CareerRunEnd.Bankrupt, run.end);
            Assert.AreEqual(0, run.payout);
        }

        [Test]
        public void RunOnce_PhaseWeeksAreRecordedForEveryPhaseReached()
        {
            var run = CareerSimulator.RunOnce(Profile(8.5f), 50_000_000, 520, new System.Random(11));
            for (int p = 1; p <= (int)run.maxPhase; p++)
                Assert.Greater(run.phaseWeek[p], 0, ((CompanyPhase)p).ToString());
        }

        [Test]
        public void Report_GoalsAreComputedForEveryStandardProfile()
        {
            var results = CareerReport.RunAll(4);
            Assert.AreEqual(CareerReport.StandardProfiles().Count, results.Count);
            Assert.IsTrue(results.TrueForAll(r => r.runs.Count == CareerReport.RunsPerCareer));
            Assert.IsNotEmpty(CareerReport.Goals(results));
        }
    }
}
