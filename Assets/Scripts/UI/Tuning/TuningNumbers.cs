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
