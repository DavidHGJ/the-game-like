using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.Systems.Projects;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Vendas semana a semana (adendo 23.19): a 1ª semana responde a nota, hype, preço, plataforma e wishlists;
    /// a queda responde ao hype e aos bugs; a variação semanal pode subir um pouco ou cair bastante; as vendas acabam.
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

        private long First(float score, float hype = 0.5f, int price = 15, int reference = 15, long wishlists = 0, float reach = 1f) =>
            SalesCalculator.FirstWeekUnits(score, ProjectSize.Small, hype, wishlists, price, reference, reach, _loop, _qf);

        [Test]
        public void FirstWeek_BetterScore_SellsMore_AndTenIsTheBase()
        {
            Assert.Greater(First(9f), First(6f));
            Assert.Greater(First(6f), First(2f) - 1);
            Assert.AreEqual(_loop.FirstWeekUnitsFor(ProjectSize.Small) * _loop.HitMultiplier(10f), First(10f), 1.0, "nota 10 = base × estouro");
        }

        [Test]
        public void FirstWeek_HypeMultiplies_FromHalfToOneAndAHalf()
        {
            Assert.AreEqual(0.5f, SalesCalculator.HypeFactor(0f, 1f, _loop), 0.0001f);
            Assert.AreEqual(1.5f, SalesCalculator.HypeFactor(1f, 1f, _loop), 0.0001f);
            Assert.Greater(First(8f, 0.9f), First(8f, 0.1f));
        }

        [Test]
        public void FirstWeek_PriceBands_CheapSellsMore_ExpensiveLess()
        {
            Assert.AreEqual(_loop.CheapPriceFactor, SalesCalculator.PriceFactor(10, 15, _loop), 0.0001f);
            Assert.AreEqual(1f, SalesCalculator.PriceFactor(15, 15, _loop), 0.0001f);
            Assert.AreEqual(_loop.ExpensivePriceFactor, SalesCalculator.PriceFactor(20, 15, _loop), 0.0001f);
            Assert.Greater(First(8f, 0.5f, 10), First(8f, 0.5f, 20));
        }

        [Test]
        public void FirstWeek_WishlistsAndPlatformAdd()
        {
            Assert.Greater(First(7f, 0.5f, 15, 15, 10_000), First(7f));
            Assert.Greater(First(7f, 0.5f, 15, 15, 0, 1.4f), First(7f, 0.5f, 15, 15, 0, 0.7f));
        }

        [Test]
        public void Decay_HypeSlowsIt_BugsSpeedItUp()
        {
            Assert.AreEqual(_loop.WeeklyDecayBase, SalesCalculator.WeeklyDecay(0f, 1f, 0f, _loop), 0.0001f);
            Assert.AreEqual(_loop.WeeklyDecayBase * (1f - _loop.HypeDecayReductionMax), SalesCalculator.WeeklyDecay(1f, 1f, 0f, _loop), 0.0001f);
            Assert.Greater(SalesCalculator.WeeklyDecay(0.5f, 1f, 0.2f, _loop), SalesCalculator.WeeklyDecay(0.5f, 1f, 0f, _loop));
        }

        [Test]
        public void NextWeek_VariationCanGoUpALittle_OrDownALot()
        {
            float decay = 0.06f;
            Assert.Greater(SalesCalculator.NextWeekUnits(1000f, decay, 1f, _loop), 1000f, "com hype alto a semana seguinte pode vender um pouco mais");
            float worst = SalesCalculator.NextWeekUnits(1000f, decay, 0f, _loop);
            Assert.AreEqual(1000f * (1f - decay) * (1f - _loop.WeeklyVariationDown), worst, 0.01f);
        }

        [Test]
        public void Sales_EndOnTheirOwn_AndMoreHypeSellsLonger()
        {
            int Weeks(float hype)
            {
                long first = First(8f, hype);
                float decay = SalesCalculator.WeeklyDecay(hype, SalesCalculator.Delivery(8f, _loop), 0f, _loop);
                float units = first;
                int weeks = 0;
                while (!SalesCalculator.SalesEnded(units, first, _loop) && weeks < 1000)
                {
                    units = SalesCalculator.NextWeekUnits(units, decay, 0.5f, _loop);
                    weeks++;
                }
                return weeks;
            }
            Assert.Less(Weeks(0.1f), 1000, "as vendas acabam sozinhas");
            Assert.Greater(Weeks(0.9f), Weeks(0.1f), "mais hype vende por mais tempo");
            Assert.Greater(SalesCalculator.ExpectedTotalUnits(First(8f, 0.9f), SalesCalculator.WeeklyDecay(0.9f, SalesCalculator.Delivery(8f, _loop), 0f, _loop), _loop),
                SalesCalculator.ExpectedTotalUnits(First(8f, 0.1f), SalesCalculator.WeeklyDecay(0.1f, SalesCalculator.Delivery(8f, _loop), 0f, _loop), _loop) * 2,
                "hype multiplica o total: começa maior e cai mais devagar");
        }


        [Test]
        public void Delivery_IsPlusOneAtTen_ZeroAtTheThreshold_MinusOneAtZero()
        {
            float t = _loop.SalesScoreThreshold;
            Assert.AreEqual(1f, SalesCalculator.Delivery(10f, _loop), 0.0001f);
            Assert.AreEqual(0f, SalesCalculator.Delivery(t, _loop), 0.0001f);
            if (t > 0f) Assert.AreEqual(-1f, SalesCalculator.Delivery(0f, _loop), 0.0001f);
        }

        private double Total(float score, float hype)
        {
            long first = First(score, hype);
            return SalesCalculator.ExpectedTotalUnits(first, SalesCalculator.WeeklyDecay(hype, SalesCalculator.Delivery(score, _loop), 0f, _loop), _loop);
        }

        [Test]
        public void Backlash_HighHypeOnABadGame_SellsLessThanLowHype()
        {
            JsonUtility.FromJsonOverwrite("{\"salesScoreThreshold\":5,\"salesFloorShare\":0.24,\"salesScoreExponent\":1}", _loop);
            Assert.Less(Total(3f, 0.9f), Total(3f, 0.1f), "hype alto num jogo ruim = rebote, vende menos");
            Assert.Greater(Total(8f, 0.9f), Total(8f, 0.1f) * 2, "hype alto num jogo bom multiplica as vendas");
            Assert.Greater(SalesCalculator.WeeklyDecay(0.9f, SalesCalculator.Delivery(3f, _loop), 0f, _loop), _loop.WeeklyDecayBase,
                "no jogo ruim o hype acelera a queda");
            Assert.Greater(SalesCalculator.Backlash01(0.9f, SalesCalculator.Delivery(3f, _loop)), 0f);
            Assert.AreEqual(0f, SalesCalculator.Backlash01(0.9f, SalesCalculator.Delivery(8f, _loop)), 0.0001f);
        }

        [Test]
        public void Hit_NineAndUp_BlowsUpSales_ByTier()
        {
            Assert.AreEqual(1f, _loop.HitMultiplier(8.9f));
            Assert.AreEqual(5f, _loop.HitMultiplier(9f));
            Assert.AreEqual(10f, _loop.HitMultiplier(9.5f));
            Assert.AreEqual(15f, _loop.HitMultiplier(10f));
            Assert.Greater(First(9.5f), First(8.9f) * 9, "um 9,5 vende uma ordem de grandeza a mais que um 8,9");
        }
        [Test]
        public void TrendingTheme_GivesHype_OnlyAboveNeutral()
        {
            Assert.AreEqual(0f, SalesCalculator.TrendingThemeHype(0.3f, _loop), 0.0001f);
            Assert.AreEqual(_loop.TrendingThemeHypeBonus, SalesCalculator.TrendingThemeHype(1f, _loop), 0.0001f);
        }
    }
}
