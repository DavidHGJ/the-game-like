using System;
using System.Globalization;

namespace GameLike.UI.Tuning
{
    /// <summary>
    /// Leitura e conversão dos números digitados na tela de Parâmetros. Aceita o formato
    /// brasileiro ("1.500.000", "0,005") e o com ponto decimal ("0.005"), e devolve o valor já
    /// no tipo exato do campo (int, long, float ou double).
    /// </summary>
    public static class TuningNumbers
    {
        /// <summary>
        /// Interpreta o texto. Para campos inteiros, pontos são sempre separador de milhar. Para
        /// campos decimais, a vírgula é o decimal; sem vírgula, um único ponto é decimal e mais de
        /// um ponto é separador de milhar.
        /// </summary>
        public static bool TryParse(string text, bool wholeNumber, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string clean = text.Trim().Replace(" ", "").Replace(" ", "");

            if (wholeNumber)
            {
                int comma = clean.IndexOf(',');
                if (comma >= 0) clean = clean.Substring(0, comma);
                clean = clean.Replace(".", "");
            }
            else if (clean.Contains(","))
            {
                clean = clean.Replace(".", "").Replace(',', '.');
            }
            else if (clean.IndexOf('.') != clean.LastIndexOf('.'))
            {
                clean = clean.Replace(".", "");
            }

            return double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private const int MaxIntegerDigits = 15;
        private const int MaxDecimalDigits = 6;

        /// <summary>
        /// Máscara de digitação: devolve o texto já com ponto de milhar (e vírgula decimal nos
        /// campos decimais). Só os algarismos, o sinal de menos no início e — em campo decimal —
        /// uma vírgula são aproveitados; o resto é ignorado. Num campo decimal sem vírgula, um
        /// ponto digitado no fim vira a vírgula decimal.
        /// </summary>
        public static string Mask(string text, bool wholeNumber)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string raw = text.Trim();
            bool negative = raw.StartsWith("-");
            if (!wholeNumber && raw.IndexOf(',') < 0 && raw.EndsWith(".")) raw = raw.Substring(0, raw.Length - 1) + ",";

            var integer = new System.Text.StringBuilder();
            var fraction = new System.Text.StringBuilder();
            bool inFraction = false;
            foreach (char c in raw)
            {
                if (c == ',' && !wholeNumber && !inFraction) { inFraction = true; continue; }
                if (!char.IsDigit(c)) continue;
                if (inFraction) { if (fraction.Length < MaxDecimalDigits) fraction.Append(c); }
                else if (integer.Length < MaxIntegerDigits) integer.Append(c);
            }

            string digits = integer.ToString().TrimStart('0');
            if (digits.Length == 0 && (integer.Length > 0 || inFraction)) digits = "0";

            var result = new System.Text.StringBuilder();
            if (negative) result.Append('-');
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0) result.Append('.');
                result.Append(digits[i]);
            }
            if (inFraction) result.Append(',').Append(fraction);
            return result.ToString();
        }

        /// <summary>
        /// Onde o cursor deve ficar no texto mascarado para continuar depois dos mesmos
        /// caracteres digitados (algarismos, vírgula e sinal) que estavam antes dele no texto original.
        /// </summary>
        public static int CaretAfterMask(string original, int caret, string masked)
        {
            if (string.IsNullOrEmpty(masked)) return 0;
            if (original == null || caret >= original.Length) return masked.Length;
            caret = Math.Max(0, caret);
            int significant = 0;
            for (int i = 0; i < caret; i++)
                if (IsSignificant(original[i])) significant++;
            if (significant == 0) return 0;

            int seen = 0;
            for (int i = 0; i < masked.Length; i++)
            {
                if (!IsSignificant(masked[i])) continue;
                if (++seen == significant) return i + 1;
            }
            return masked.Length;
        }

        private static bool IsSignificant(char c) => char.IsDigit(c) || c == ',' || c == '-';

        /// <summary>Converte o número para o tipo exato do campo, arredondando inteiros.</summary>
        public static object Box(Type fieldType, double value)
        {
            if (fieldType == typeof(int)) return (int)Math.Round(Math.Max(int.MinValue, Math.Min(int.MaxValue, value)));
            if (fieldType == typeof(long)) return (long)Math.Round(value);
            if (fieldType == typeof(float)) return (float)value;
            return value;
        }
    }
}
