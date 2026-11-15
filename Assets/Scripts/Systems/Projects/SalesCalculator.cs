using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Vendas semana a semana (adendo 23.19). A 1ª semana sai da nota, do hype, do preço, do alcance da
    /// plataforma e das wishlists; cada semana seguinte sai da anterior pela queda semanal (hype segura,
    /// bugs aceleram) com uma variação aleatória. Não existe total nem duração fixa.
    /// </summary>
    public static class SalesCalculator
    {
        /// <summary>Fator da nota sobre a 1ª semana: a curva com piso e virada (23.14), 1 na nota 10.</summary>
        public static float ScoreFactor(float score, GameLoopBalanceSO loop) =>
            loop != null ? loop.SalesShareForScore(score) : Mathf.Pow(Mathf.Clamp01(score / 10f), 2f);

        /// <summary>
        /// "Entrega" da nota (−1..+1) em relação à nota de virada: +1 na nota 10, 0 na virada, −1 na nota 0.
        /// Decide para que lado o hype empurra (efeito rebote).
        /// </summary>
        public static float Delivery(float score, GameLoopBalanceSO loop)
        {
            float threshold = loop != null ? Mathf.Clamp(loop.SalesScoreThreshold, 0f, 9.5f) : 5f;
            if (score >= threshold) return Mathf.Clamp01((score - threshold) / Mathf.Max(0.5f, 10f - threshold));
            return threshold > 0f ? Mathf.Clamp((score - threshold) / threshold, -1f, 0f) : 0f;
        }

        /// <summary>
        /// Fator do hype (0..1) sobre a 1ª semana, pela entrega da nota: jogo excelente vai de 0,5 (sem hype) a 1,5
        /// (hype máximo); jogo ruim quase não ganha comprador com o hype. Nunca abaixo de 0,2.
        /// </summary>
        public static float HypeFactor(float hype01, float delivery, GameLoopBalanceSO loop)
        {
            float min = loop != null ? loop.HypeSalesFactorMin : 0.5f;
            float max = loop != null ? loop.HypeSalesFactorMax : 1.5f;
            float weight = 0.25f + 0.75f * Mathf.Clamp(delivery, -1f, 1f);
            return Mathf.Max(0.2f, min + Mathf.Clamp01(hype01) * (max - min) * weight);
        }

        /// <summary>Intensidade do rebote (0..1): hype alto com nota abaixo da virada.</summary>
        public static float Backlash01(float hype01, float delivery) => Mathf.Clamp01(hype01) * Mathf.Clamp01(-delivery);

        /// <summary>Fator do preço sobre a 1ª semana: barato, na média ou caro em relação ao preço de referência.</summary>
        public static float PriceFactor(int price, int referencePrice, GameLoopBalanceSO loop)
        {
            if (price <= 0 || referencePrice <= 0) return 1f;
            float ratio = price / (float)referencePrice;
            float cheap = loop != null ? loop.CheapPriceRatio : 0.8f;
            float expensive = loop != null ? loop.ExpensivePriceRatio : 1.2f;
            if (ratio <= cheap) return loop != null ? loop.CheapPriceFactor : 1.25f;
            if (ratio >= expensive) return loop != null ? loop.ExpensivePriceFactor : 0.7f;
            return 1f;
        }

        /// <summary>Multiplicador de vendas pelo alcance da plataforma (base instalada do console).</summary>
        public static float PlatformReach(float popularity01) => Mathf.Lerp(0.5f, 1.55f, Mathf.Clamp01(popularity01));

        /// <summary>Unidades vendidas pelas wishlists no lançamento (≈80 por 100 wishlists).</summary>
        public static long WishlistUnits(long wishlists, QualityFormulaBalanceSO qf)
        {
            int per100 = qf != null ? qf.SalesPer100Wishlists : 80;
            return System.Math.Max(0L, wishlists) * per100 / 100;
        }

        /// <summary>
        /// Unidades da 1ª semana: (base do tamanho × nota × hype × preço × alcance da plataforma + wishlists) × estouro (9+).
        /// </summary>
        public static long FirstWeekUnits(float score, ProjectSize size, float hype01, long wishlists, int price, int referencePrice,
            float platformReach01, GameLoopBalanceSO loop, QualityFormulaBalanceSO qf, float hitScore = -1f)
        {
            if (platformReach01 <= 0f) platformReach01 = 1f;
            long baseUnits = loop != null ? loop.FirstWeekUnitsFor(size) : 5_000;
            double organic = baseUnits * (double)ScoreFactor(score, loop) * HypeFactor(hype01, Delivery(score, loop), loop)
                             * PriceFactor(price, referencePrice, loop) * platformReach01;
            float hit = loop != null ? loop.HitMultiplier(hitScore >= 0f ? hitScore : score, size) : 1f;
            return (long)System.Math.Round((organic + WishlistUnits(wishlists, qf)) * hit);
        }


        /// <summary>
        /// Nota que conta para o estouro (adendo 23.19): a média das críticas, mas só se TODAS as casas deram 9 ou mais;
        /// senão 0 (sem estouro).
        /// </summary>
        public static float HitScore(float[] reviewScores, float finalScore)
        {
            if (reviewScores == null || reviewScores.Length == 0) return finalScore;
            foreach (float r in reviewScores) if (r < 9f) return 0f;
            return finalScore;
        }
        /// <summary>
        /// Queda semanal das vendas (0..0,95). Jogo acima da virada: o hype segura a queda (mais quanto melhor a nota).
        /// Jogo abaixo: o hype acelera a queda (rebote). Bugs (bugs ÷ pontos) sempre aceleram.
        /// </summary>
        public static float WeeklyDecay(float hype01, float delivery, float bugRatio, GameLoopBalanceSO loop)
        {
            float decayBase = loop != null ? loop.WeeklyDecayBase : 0.15f;
            float reduction = loop != null ? loop.HypeDecayReductionMax : 0.6f;
            float backlash = loop != null ? loop.HypeBacklashWeight : 1.5f;
            float bugWeight = loop != null ? loop.BugDecayWeight : 0.3f;
            float hype = Mathf.Clamp01(hype01);
            float d = Mathf.Clamp(delivery, -1f, 1f);
            float decay = d >= 0f
                ? decayBase * (1f - hype * reduction * d)
                : decayBase * (1f + hype * backlash * -d);
            decay += bugWeight * Mathf.Max(0f, bugRatio);
            return Mathf.Clamp(decay, 0.005f, 0.95f);
        }

        /// <summary>
        /// Vendas da semana seguinte a partir da semana atual: aplica a queda e a variação aleatória
        /// (<paramref name="random01"/> de 0 = vende bem menos, a 1 = vende um pouco mais).
        /// </summary>
        public static float NextWeekUnits(float currentUnits, float decay, float random01, GameLoopBalanceSO loop)
        {
            float down = loop != null ? loop.WeeklyVariationDown : 0.25f;
            float up = loop != null ? loop.WeeklyVariationUp : 0.15f;
            float variation = Mathf.Lerp(1f - down, 1f + up, Mathf.Clamp01(random01));
            return Mathf.Max(0f, currentUnits * (1f - decay) * variation);
        }

        /// <summary>As vendas acabam quando a semana cai abaixo da fração mínima da 1ª semana (ou abaixo de 1 unidade).</summary>
        public static bool SalesEnded(float nextUnits, long firstWeekUnits, GameLoopBalanceSO loop)
        {
            float endShare = loop != null ? loop.SalesEndShare : 0.02f;
            return nextUnits < 1f || nextUnits < firstWeekUnits * endShare;
        }

        /// <summary>Total esperado (média da variação) a partir da 1ª semana — só para simulações e relatórios.</summary>
        public static double ExpectedTotalUnits(long firstWeekUnits, float decay, GameLoopBalanceSO loop)
        {
            float down = loop != null ? loop.WeeklyVariationDown : 0.25f;
            float up = loop != null ? loop.WeeklyVariationUp : 0.15f;
            double keep = (1.0 - decay) * (1.0 + (up - down) / 2.0);
            double end = firstWeekUnits * (loop != null ? loop.SalesEndShare : 0.02f);
            double total = 0, week = firstWeekUnits;
            for (int i = 0; i < 1000 && week >= System.Math.Max(1.0, end); i++) { total += week; week *= keep; }
            return total;
        }

        /// <summary>Hype extra no lançamento pelo tema em alta (tendência acima do neutro).</summary>
        public static float TrendingThemeHype(float themeTrend01, GameLoopBalanceSO loop)
        {
            float bonus = loop != null ? loop.TrendingThemeHypeBonus : 0.2f;
            return bonus * Mathf.Clamp01((themeTrend01 - 0.5f) * 2f);
        }

        /// <summary>Hype (0..1) comprado com o budget de lançamento, com retorno decrescente.</summary>
        public static float LaunchBudgetHype(long budget, GameLoopBalanceSO loop)
        {
            if (budget <= 0) return 0f;
            float per10k = loop != null ? loop.LaunchBudgetHypePer10k : 0.12f;
            return Mathf.Clamp(per10k * Mathf.Sqrt(budget / 10_000f), 0f, 0.5f);
        }

        /// <summary>
        /// LEGADO (só para converter saves antigos, v5 → v6): unidades da semana numa cauda geométrica de
        /// duração fixa do modelo anterior (23.4).
        /// </summary>
        public static long LegacyTailUnitsForWeek(long totalUnits, int weekIndex, int totalWeeks, float retention)
        {
            if (totalUnits <= 0 || totalWeeks <= 0 || weekIndex < 0 || weekIndex >= totalWeeks) return 0;
            retention = Mathf.Clamp(retention, 0.05f, 0.99f);
            double norm = (1.0 - System.Math.Pow(retention, totalWeeks)) / (1.0 - retention);
            double share = System.Math.Pow(retention, weekIndex) / norm;
            return (long)System.Math.Round(totalUnits * share);
        }
    }
}
