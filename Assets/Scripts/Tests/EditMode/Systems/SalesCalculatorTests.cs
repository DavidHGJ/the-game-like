using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.Systems.Projects;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Cobre a conversão nota → vendas (GDD 19.6): notas maiores vendem mais, e uma
    /// nota zero não gera vendas/receita negativas. Determinístico (sem ruído).
    /// </summary>
    [TestFixture]
    public sealed class SalesCalculatorTests
    {
        private GameLoopBalanceSO _loop;
        private QualityFormulaBalanceSO _qf;

        [SetUp]
        public void SetUp()
        {
            _loop = ScriptableObject.CreateInstance<GameLoopBalanceSO>();
            _qf = ScriptableObject.CreateInstance<QualityFormulaBalanceSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_loop);
            Object.DestroyImmediate(_qf);
        }

        // Helper: hype e wishlists correlacionados com a nota (como o loop produziria).
        private SalesCalculator.Result Sell(float score, ProjectSize size)
        {
            float frac = Mathf.Clamp01(score / 10f);
            long wishlists = (long)(_loop.BaseWishlistsFor(size) * frac);
            return SalesCalculator.Compute(score, size, frac, wishlists, _loop, _qf);
        }

        [Test]
        public void HigherScore_YieldsMoreUnits()
        {
            var high = Sell(9f, ProjectSize.Medium);
            var low = Sell(4f, ProjectSize.Medium);
            Assert.Greater(high.units, low.units);
            Assert.Greater(high.revenue, low.revenue);
        }

        [Test]
        public void ZeroScore_IsNonNegative()
        {
            var result = Sell(0f, ProjectSize.Small);
            Assert.GreaterOrEqual(result.units, 0L);
            Assert.GreaterOrEqual(result.revenue, 0L);
        }

        [Test]
        public void ExcellentHype_SellsMoreThanWeakAtSameBase()
        {
            var excellent = Sell(10f, ProjectSize.Large);
            var weak = Sell(1f, ProjectSize.Large);
            Assert.Greater(excellent.units, weak.units);
        }

        [Test]
        public void MoreWishlists_YieldMoreLaunchSales()
        {
            var many = SalesCalculator.Compute(7f, ProjectSize.Medium, 0.7f, 50_000, _loop, _qf);
            var few = SalesCalculator.Compute(7f, ProjectSize.Medium, 0.7f, 5_000, _loop, _qf);
            Assert.Greater(many.units, few.units);
        }

        [Test]
        public void PlatformReach_IsMonotonicInPopularity()
        {
            Assert.Greater(SalesCalculator.PlatformReach(0.9f), SalesCalculator.PlatformReach(0.5f));
            Assert.Greater(SalesCalculator.PlatformReach(0.5f), SalesCalculator.PlatformReach(0.1f));
        }

        [Test]
        public void PopularPlatform_SellsMoreThanNichePlatform()
        {
            float popularReach = SalesCalculator.PlatformReach(0.95f);
            float nicheReach = SalesCalculator.PlatformReach(0.15f);
            var popular = SalesCalculator.Compute(7f, ProjectSize.Medium, 0.7f, 20_000, _loop, _qf, popularReach);
            var niche = SalesCalculator.Compute(7f, ProjectSize.Medium, 0.7f, 20_000, _loop, _qf, nicheReach);
            Assert.Greater(popular.units, niche.units);
        }

        [Test]
        public void DefaultReach_IsNeutral()
        {
            var withDefault = SalesCalculator.Compute(7f, ProjectSize.Medium, 0.7f, 20_000, _loop, _qf);
            var withOne = SalesCalculator.Compute(7f, ProjectSize.Medium, 0.7f, 20_000, _loop, _qf, 1f);
            Assert.AreEqual(withDefault.units, withOne.units);
        }

        [Test]
        public void NonPositiveReach_IsTreatedAsNeutral()
        {
            var neutral = SalesCalculator.Compute(7f, ProjectSize.Medium, 0.7f, 20_000, _loop, _qf, 1f);
            var zero = SalesCalculator.Compute(7f, ProjectSize.Medium, 0.7f, 20_000, _loop, _qf, 0f);
            Assert.AreEqual(neutral.units, zero.units);
        }
    }
}
