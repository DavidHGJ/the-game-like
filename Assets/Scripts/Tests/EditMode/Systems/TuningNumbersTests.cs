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
        public void Mask_WholeNumber_AddsThousandDotsAsYouType()
        {
            Assert.AreEqual("1", TuningNumbers.Mask("1", true));
            Assert.AreEqual("1.000", TuningNumbers.Mask("1000", true));
            Assert.AreEqual("10.000.000", TuningNumbers.Mask("10000000", true));
            Assert.AreEqual("100.000.000", TuningNumbers.Mask("10.000.0000", true));
            Assert.AreEqual("15.000", TuningNumbers.Mask("1.5000", true), "ponto no lugar errado é refeito");
        }

        [Test]
        public void Mask_WholeNumber_IgnoresLettersLeadingZerosAndKeepsMinus()
        {
            Assert.AreEqual("2.500", TuningNumbers.Mask("R$ 2a500", true));
            Assert.AreEqual("50", TuningNumbers.Mask("0050", true));
            Assert.AreEqual("0", TuningNumbers.Mask("000", true));
            Assert.AreEqual("-1.200", TuningNumbers.Mask("-1200", true));
            Assert.AreEqual("", TuningNumbers.Mask("", true));
        }

        [Test]
        public void Mask_Decimal_KeepsOneCommaAndGroupsTheIntegerPart()
        {
            Assert.AreEqual("0,005", TuningNumbers.Mask("0,005", false));
            Assert.AreEqual("1.234,5", TuningNumbers.Mask("1234,5", false));
            Assert.AreEqual("0,", TuningNumbers.Mask(",", false));
            Assert.AreEqual("1,", TuningNumbers.Mask("1.", false), "ponto digitado no fim vira vírgula");
            Assert.AreEqual("2,55", TuningNumbers.Mask("2,5,5", false), "segunda vírgula é ignorada");
        }

        [Test]
        public void Mask_ResultIsReadBackAsTheSameNumber()
        {
            Assert.IsTrue(TuningNumbers.TryParse(TuningNumbers.Mask("100000000", true), true, out double whole));
            Assert.AreEqual(100_000_000d, whole);
            Assert.IsTrue(TuningNumbers.TryParse(TuningNumbers.Mask("1234,56", false), false, out double fraction));
            Assert.AreEqual(1234.56, fraction, 1e-9);
        }

        [Test]
        public void CaretAfterMask_KeepsTheCaretAfterTheSameDigits()
        {
            Assert.AreEqual(5, TuningNumbers.CaretAfterMask("1000", 4, "1.000"), "cursor no fim continua no fim");
            Assert.AreEqual(2, TuningNumbers.CaretAfterMask("15000", 2, "15.000"), "depois do 5, antes do ponto");
            Assert.AreEqual(0, TuningNumbers.CaretAfterMask("1000", 0, "1.000"));
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
