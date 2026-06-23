using GameLike.Data.Balance;
using NUnit.Framework;
using UnityEngine;

namespace GameLike.Tests.EditMode.Data
{
    /// <summary>
    /// Validates the default weight values shipped with the project sum to
    /// 1.0, mirroring the same check the Inspector-side
    /// QualityFormulaBalanceEditor performs visually. Catching this in a
    /// test means CI (or a pre-commit run) fails loudly if someone edits the
    /// default values in a way that breaks the formula's normalization.
    /// </summary>
    [TestFixture]
    public sealed class QualityFormulaBalanceTests
    {
        private QualityFormulaBalanceSO _balance;

        [SetUp]
        public void SetUp()
        {
            _balance = ScriptableObject.CreateInstance<QualityFormulaBalanceSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_balance);
        }

        [Test]
        public void DefaultWeights_SumToOne()
        {
            Assert.AreEqual(1f, _balance.TotalWeight, 0.001f,
                "Default quality formula weights must sum to 1.0 (GDD 19.7). " +
                "If this fails, someone changed a default field value without rebalancing the others.");
        }
    }
}
