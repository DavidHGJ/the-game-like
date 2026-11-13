using System.Collections.Generic;
using GameLike.State;
using GameLike.State.Staff;
using GameLike.Systems.Staff;
using NUnit.Framework;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Cobre os cálculos de equipe (GDD 7): nível médio sobe com senioridade, burnout
    /// derruba produtividade, e a cobertura de especialistas premia alinhar a equipe
    /// com a alocação do projeto.
    /// </summary>
    [TestFixture]
    public sealed class StaffCalculatorTests
    {
        private static EmployeeInstance Make(Specialty sp, Seniority sr, float burnout = 0f, float mood = 0.8f)
            => new EmployeeInstance("id-" + sp + sr, "T", sp, sr, PersonalityTrait.None, 1000)
            { burnout01 = burnout, mood01 = mood };

        [Test]
        public void TeamLevel_StarTeam_HigherThanJuniorTeam()
        {
            var juniors = new List<EmployeeInstance> { Make(Specialty.Programming, Seniority.Junior) };
            var stars = new List<EmployeeInstance> { Make(Specialty.Programming, Seniority.Star) };
            Assert.Greater(StaffCalculator.TeamLevel01(stars), StaffCalculator.TeamLevel01(juniors));
        }

        [Test]
        public void TeamLevel_HighBurnout_LowersLevel()
        {
            var rested = new List<EmployeeInstance> { Make(Specialty.Programming, Seniority.Senior, burnout: 0f) };
            var burned = new List<EmployeeInstance> { Make(Specialty.Programming, Seniority.Senior, burnout: 0.9f) };
            Assert.Greater(StaffCalculator.TeamLevel01(rested), StaffCalculator.TeamLevel01(burned));
        }

        [Test]
        public void SpecialtyCoverage_MatchingSpecialist_BeatsMismatch()
        {
            // Alocação toda em Tech (índice 3).
            var alloc = new float[ProjectState.AreaCount];
            alloc[(int)DevArea.Tech] = 10f;

            var techTeam = new List<EmployeeInstance> { Make(Specialty.Programming, Seniority.Senior) };
            var artTeam = new List<EmployeeInstance> { Make(Specialty.Art, Seniority.Senior) };

            Assert.Greater(
                StaffCalculator.SpecialtyCoverage01(techTeam, alloc),
                StaffCalculator.SpecialtyCoverage01(artTeam, alloc));
        }

        [Test]
        public void TeamLevel_EmptyStaff_UsesFounderBaseline()
        {
            float level = StaffCalculator.TeamLevel01(new List<EmployeeInstance>());
            Assert.Greater(level, 0f);
            Assert.Less(level, 1f);
        }
    }
}
