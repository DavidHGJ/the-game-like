using System.Collections.Generic;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Projects;
using NUnit.Framework;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Cobre a lógica de franquias (GDD 13): média por franquia, contagem e o hype
    /// inicial herdado — franquia forte dá empurrão, franquia fraca quase nada.
    /// </summary>
    [TestFixture]
    public sealed class FranchiseCalculatorTests
    {
        private static ProjectState Game(string franchiseId, float score)
            => new ProjectState("G", "g", "G", ProjectSize.Small, null) { franchiseId = franchiseId, finalScore = score };

        [Test]
        public void AverageScore_AveragesOnlyThatFranchise()
        {
            var released = new List<ProjectState> { Game("fA", 8f), Game("fA", 6f), Game("fB", 2f) };
            Assert.AreEqual(7f, FranchiseCalculator.AverageScore(released, "fA"), 0.001f);
            Assert.AreEqual(2f, FranchiseCalculator.AverageScore(released, "fB"), 0.001f);
            Assert.AreEqual(2, FranchiseCalculator.Count(released, "fA"));
        }

        [Test]
        public void InitialHype_StrongFranchise_BeatsWeak()
        {
            Assert.Greater(FranchiseCalculator.InitialHype01(9f), FranchiseCalculator.InitialHype01(5f));
        }

        [Test]
        public void InitialHype_NoHistory_IsZero()
        {
            Assert.AreEqual(0f, FranchiseCalculator.InitialHype01(0f), 0.001f);
        }
    }
}
