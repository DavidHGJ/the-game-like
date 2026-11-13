using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.Systems.Projects;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>
    /// Cobre a cobertura de escopo (GDD 5.3): equipe igual à referência cobre 100%,
    /// equipe muito pequena para o escopo fica no piso, e equipe grande demais não
    /// passa de 100%. Usa uma instância de balance com os valores padrão.
    /// </summary>
    [TestFixture]
    public sealed class ProductionCalculatorTests
    {
        private GameLoopBalanceSO _loop;

        [SetUp]
        public void SetUp() => _loop = ScriptableObject.CreateInstance<GameLoopBalanceSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_loop);

        [Test]
        public void ScopeCoverage_TeamEqualsReference_IsOne()
        {
            // Medium tem equipe de referência 3 por padrão.
            float coverage = ProductionCalculator.ScopeCoverage01(3, ProjectSize.Medium, _loop);
            Assert.AreEqual(1f, coverage, 0.001f);
        }

        [Test]
        public void ScopeCoverage_UndersizedTeam_ClampedToFloor()
        {
            // 1 pessoa para AAA (referência 9) daria ~0.11 → limitado ao piso (0.15 padrão).
            float coverage = ProductionCalculator.ScopeCoverage01(1, ProjectSize.AAA, _loop);
            Assert.AreEqual(_loop.ScopeCoverageFloor, coverage, 0.001f);
            Assert.Less(coverage, 1f);
        }

        [Test]
        public void ScopeCoverage_OversizedTeam_CapsAtOne()
        {
            // 9 pessoas para um projeto Small (referência 1) → limitado a 1.0.
            float coverage = ProductionCalculator.ScopeCoverage01(9, ProjectSize.Small, _loop);
            Assert.AreEqual(1f, coverage, 0.001f);
        }
    }
}
