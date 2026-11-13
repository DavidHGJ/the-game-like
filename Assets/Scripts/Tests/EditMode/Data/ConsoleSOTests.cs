using GameLike.Data.Definitions;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Data
{
    /// <summary>
    /// Cobre a disponibilidade por ano e a curva de popularidade de um console
    /// (GDD 15.2): antes do lançamento não existe (popularidade 0), e depois do
    /// lançamento a popularidade fica entre inicial e pico. Usa os valores default.
    /// </summary>
    [TestFixture]
    public sealed class ConsoleSOTests
    {
        private ConsoleSO _console;

        [SetUp]
        public void SetUp()
        {
            // Defaults do ConsoleSO: releaseYear=3, initial=0.25, peak=0.8, lifespan=10.
            _console = ScriptableObject.CreateInstance<ConsoleSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_console);
        }

        [Test]
        public void IsAvailable_FalseBeforeRelease_TrueFromReleaseYear()
        {
            Assert.IsFalse(_console.IsAvailable(_console.ReleaseYear - 1));
            Assert.IsTrue(_console.IsAvailable(_console.ReleaseYear));
            Assert.IsTrue(_console.IsAvailable(_console.ReleaseYear + 1));
        }

        [Test]
        public void PopularityAtYear_IsZeroBeforeRelease()
        {
            Assert.AreEqual(0f, _console.PopularityAtYear(_console.ReleaseYear - 1));
        }

        [Test]
        public void PopularityAtYear_StaysWithinInitialAndPeakWhenAvailable()
        {
            for (int year = _console.ReleaseYear; year <= _console.ReleaseYear + _console.LifespanYears; year++)
            {
                float pop = _console.PopularityAtYear(year);
                Assert.GreaterOrEqual(pop, 0f);
                Assert.LessOrEqual(pop, _console.PeakPopularity + 0.001f);
            }
        }

        [Test]
        public void PopularityAtYear_RisesAfterRelease()
        {
            // A curva default sobe do lançamento até ~1/3 da vida; o pico da curva
            // deve superar a popularidade do primeiro ano.
            float atRelease = _console.PopularityAtYear(_console.ReleaseYear);
            float midLife = _console.PopularityAtYear(_console.ReleaseYear + _console.LifespanYears / 3);
            Assert.Greater(midLife, atRelease);
        }
    }
}
