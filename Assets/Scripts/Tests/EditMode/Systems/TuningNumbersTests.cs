using GameLike.UI.Tuning;
using NUnit.Framework;

namespace GameLike.Tests.EditMode.Systems
{
    /// <summary>Números digitados na tela de Parâmetros: leitura no formato brasileiro e tipo exato do campo.</summary>
    [TestFixture]
    public sealed class TuningNumbersTests
    {
        [Test]
        public void TryParse_WholeNumberWithThousandDots_ReadsFullValue()
        {
            Assert.IsTrue(TuningNumbers.TryParse("300.000", true, out double a));
            Assert.AreEqual(300_000d, a);
            Assert.IsTrue(TuningNumbers.TryParse("1.500.000", true, out double b));
            Assert.AreEqual(1_500_000d, b);
            Assert.IsTrue(TuningNumbers.TryParse(" 2500 ", true, out double c));
            Assert.AreEqual(2_500d, c);
        }

        [Test]
        public void TryParse_DecimalWithCommaOrSingleDot_ReadsDecimal()
        {
            Assert.IsTrue(TuningNumbers.TryParse("0,005", false, out double a));
            Assert.AreEqual(0.005, a, 1e-9);
            Assert.IsTrue(TuningNumbers.TryParse("0.005", false, out double b));
            Assert.AreEqual(0.005, b, 1e-9);
            Assert.IsTrue(TuningNumbers.TryParse("1.234,5", false, out double c));
            Assert.AreEqual(1234.5, c, 1e-9);
        }

        [Test]
        public void TryParse_Garbage_ReturnsFalse()
        {
            Assert.IsFalse(TuningNumbers.TryParse("", true, out _));
            Assert.IsFalse(TuningNumbers.TryParse("abc", false, out _));
        }

        [Test]
        public void Box_ReturnsTheExactFieldType()
        {
            Assert.IsInstanceOf<long>(TuningNumbers.Box(typeof(long), 300000.4));
            Assert.AreEqual(300000L, TuningNumbers.Box(typeof(long), 300000.4));
            Assert.IsInstanceOf<int>(TuningNumbers.Box(typeof(int), 7.6));
            Assert.AreEqual(8, TuningNumbers.Box(typeof(int), 7.6));
            Assert.IsInstanceOf<float>(TuningNumbers.Box(typeof(float), 0.25));
            Assert.IsInstanceOf<double>(TuningNumbers.Box(typeof(double), 0.25));
        }

        [Test]
        public void Box_LongValue_CanBeAssignedToALongField()
        {
            var field = typeof(Holder).GetField(nameof(Holder.cost));
            var holder = new Holder();
            field.SetValue(holder, TuningNumbers.Box(field.FieldType, 1_500_000d));
            Assert.AreEqual(1_500_000L, holder.cost);
        }

        private sealed class Holder
        {
            public long cost;
        }
    }
}
