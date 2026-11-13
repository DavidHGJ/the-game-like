using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Economy;
using GameLike.Systems.Meta;
using NUnit.Framework;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Cobre o valuation (GDD 4) e os Pontos de Legado (GDD 19.5): fatores somam,
    /// reputação e equipe elevam o valor, e o legado reflete fase e notas altas.
    /// </summary>
    [TestFixture]
    public sealed class ValuationAndLegacyTests
    {
        [Test]
        public void Valuation_HigherReputation_IncreasesValue()
        {
            var low = new CompanyState("A", 100_000) { reputation01 = 0.2f };
            var high = new CompanyState("B", 100_000) { reputation01 = 0.9f };
            long vLow = ValuationCalculator.Compute(low, null);
            long vHigh = ValuationCalculator.Compute(high, null);
            Assert.Greater(vHigh, vLow);
        }

        [Test]
        public void Valuation_TeamAddsValue()
        {
            var solo = new CompanyState("A", 100_000);
            var withTeam = new CompanyState("B", 100_000);
            withTeam.employees.Add(new EmployeeInstance("1", "X", Specialty.Programming, Seniority.Senior, PersonalityTrait.None, 6000));
            Assert.Greater(ValuationCalculator.Compute(withTeam, null), ValuationCalculator.Compute(solo, null));
        }

        [Test]
        public void Legacy_StudioPhase_BeatsGarage()
        {
            var garage = new CompanyState("A", 0) { phase = CompanyPhase.Garage };
            var studio = new CompanyState("B", 0) { phase = CompanyPhase.Studio };
            Assert.Greater(
                LegacyCalculator.Compute(studio, null, false),
                LegacyCalculator.Compute(garage, null, false));
        }

        [Test]
        public void Legacy_Bankruptcy_LosesNoCrisisBonus()
        {
            var c = new CompanyState("A", 0) { phase = CompanyPhase.Indie };
            Assert.Greater(
                LegacyCalculator.Compute(c, null, false),
                LegacyCalculator.Compute(c, null, true));
        }

        [Test]
        public void Legacy_HighScoredGames_AddPoints()
        {
            var c = new CompanyState("A", 0) { phase = CompanyPhase.Garage };
            var released = new List<ProjectState>
            {
                new ProjectState("G1", "g", "G", ProjectSize.Small, null) { finalScore = 8.5f },
                new ProjectState("G2", "g", "G", ProjectSize.Small, null) { finalScore = 4f }
            };
            // Um jogo acima de 7 → +2 PL sobre o baseline sem jogos.
            Assert.AreEqual(LegacyCalculator.Compute(c, null, false) + 2,
                            LegacyCalculator.Compute(c, released, false));
        }
    }
}
