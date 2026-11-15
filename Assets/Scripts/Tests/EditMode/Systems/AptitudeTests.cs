using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Economy;
using GameLike.Systems.Projects;
using GameLike.Systems.Staff;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Cobre os pontos por aptidão do adendo 23.16: pontos semanais proporcionais à aptidão,
    /// prática e treinamento sobem a aptidão, sorteios iniciais e o peso do volume de pontos na nota.
    /// </summary>
    [TestFixture]
    public sealed class AptitudeTests
    {
        private GameLoopBalanceSO _loop;
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void SetUp() => _loop = ScriptableObject.CreateInstance<GameLoopBalanceSO>();

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_loop);
            foreach (var o in _spawned) if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
            EventBus.Clear();
        }

        [Test]
        public void WeeklyPoints_AptitudeTen_DoublesTheReference()
        {
            float reference = AptitudeCalculator.WeeklyPoints(5f, 1f, 1f, _loop);
            Assert.AreEqual(_loop.BasePointsPerWeek, reference, 0.0001f);
            Assert.AreEqual(2f * reference, AptitudeCalculator.WeeklyPoints(10f, 1f, 1f, _loop), 0.0001f);
            Assert.AreEqual(0.4f * reference, AptitudeCalculator.WeeklyPoints(5f, 0.4f, 1f, _loop), 0.0001f,
                "40% do esforço = 40% dos pontos gerados");
        }

        [Test]
        public void Practice_RaisesAptitude_SlowerNearTheTop()
        {
            var low = new[] { 3f, 3f, 3f, 3f, 3f };
            var high = new[] { 9f, 9f, 9f, 9f, 9f };
            AptitudeCalculator.Practice(low, 0, 10f, _loop);
            AptitudeCalculator.Practice(high, 0, 10f, _loop);
            Assert.Greater(low[0], 3f);
            Assert.Greater(low[0] - 3f, high[0] - 9f, "quem está perto de 10 aprende mais devagar");
            Assert.AreEqual(3f, low[1], "só a área praticada sobe");
        }

        [Test]
        public void Train_RaisesAptitude_AndCostGrowsWithLevel()
        {
            var aptitude = new[] { 4f, 4f, 4f, 4f, 4f };
            Assert.IsTrue(AptitudeCalculator.Train(aptitude, 2, _loop));
            Assert.AreEqual(4f + _loop.TrainingGain, aptitude[2], 0.0001f);
            Assert.Greater(AptitudeCalculator.TrainingCost(8f, _loop), AptitudeCalculator.TrainingCost(4f, _loop));
            var maxed = new[] { 10f, 10f, 10f, 10f, 10f };
            Assert.IsFalse(AptitudeCalculator.Train(maxed, 0, _loop), "não passa de 10");
        }

        [Test]
        public void RollFounder_HasStrongAndWeakAreas_InRange()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var a = AptitudeCalculator.RollFounder(new System.Random(seed));
                Assert.AreEqual(ProjectState.AreaCount, a.Length);
                float min = 10f, max = 0f;
                foreach (float v in a) { min = Mathf.Min(min, v); max = Mathf.Max(max, v); }
                Assert.GreaterOrEqual(min, AptitudeCalculator.Min);
                Assert.LessOrEqual(max, AptitudeCalculator.Max);
                Assert.GreaterOrEqual(max, 6f, "uma área forte");
                Assert.LessOrEqual(min, 3f, "uma área fraca");
            }
        }

        [Test]
        public void ForEmployee_SpecialtyIsTheBestArea_AndGrowsWithSeniority()
        {
            var rng = new System.Random(7);
            var junior = AptitudeCalculator.ForEmployee(Specialty.Art, Seniority.Junior, rng);
            var star = AptitudeCalculator.ForEmployee(Specialty.Art, Seniority.Star, rng);
            int art = (int)DevArea.Art;
            for (int i = 0; i < junior.Length; i++)
                if (i != art) Assert.Greater(junior[art], junior[i]);
            Assert.Greater(star[art], junior[art]);
        }


        [Test]
        public void ProjectManager_Train_ChargesCashAndRaisesFounder()
        {
            var go = new GameObject("aptitude-test");
            _spawned.Add(go);
            var eco = go.AddComponent<EconomyManager>();
            var pm = go.AddComponent<ProjectManager>();
            var company = new CompanyState("Test", 100_000);
            var run = new RunState(company) { founderAptitude = new[] { 4f, 4f, 4f, 4f, 4f } };
            eco.Bind(company);
            pm.Bind(run, eco);

            Assert.IsTrue(pm.Train(ProjectManager.FounderId, (int)DevArea.Tech, out long cost));
            Assert.Greater(cost, 0);
            Assert.AreEqual(100_000 - cost, company.cash);
            Assert.Greater(run.founderAptitude[(int)DevArea.Tech], 4f);

            company.cash = 0;
            Assert.IsFalse(pm.Train(ProjectManager.FounderId, (int)DevArea.Tech, out _), "sem caixa não treina");
        }
    }
}
