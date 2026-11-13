using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using UnityEngine;

namespace GameLike.Systems.Projects
{
    /// <summary>
    /// Converte a nota de qualidade, o hype e as wishlists acumuladas em vendas,
    /// seguindo o GDD seção 19.6. As vendas de lançamento vêm das wishlists reais
    /// construídas pelo Marketing (§10) durante a produção (≈80 vendas por 100
    /// wishlists); soma-se uma cauda orgânica dependente da nota e da banda de hype.
    /// </summary>
    public static class SalesCalculator
    {
        public readonly struct Result
        {
            public readonly long units;
            public readonly long revenue;
            public readonly float hype01;
            public readonly long wishlists;

            public Result(long units, long revenue, float hype01, long wishlists)
            {
                this.units = units;
                this.revenue = revenue;
                this.hype01 = hype01;
                this.wishlists = wishlists;
            }
        }

        /// <summary>
        /// Calcula vendas e receita de um lançamento a partir da nota, do hype acumulado
        /// (0..1) e das wishlists reais construídas durante a produção.
        /// </summary>
        /// <param name="platformReach01">
        /// Multiplicador de alcance de mercado da plataforma de lançamento (GDD 15.2),
        /// derivado da popularidade do console. 1 = neutro (sem console escolhido).
        /// </param>
        public static Result Compute(float score, ProjectSize size, float hype01, long wishlists,
            GameLoopBalanceSO loop, QualityFormulaBalanceSO qf, float platformReach01 = 1f)
        {
            float scoreFraction = Mathf.Clamp01(score / 10f);
            hype01 = Mathf.Clamp01(hype01);
            int hype100 = Mathf.RoundToInt(hype01 * 100f);
            if (platformReach01 <= 0f) platformReach01 = 1f;

            if (wishlists < 0) wishlists = 0;
            int salesPer100 = qf != null ? qf.SalesPer100Wishlists : 80;
            long launchSales = wishlists * salesPer100 / 100;

            long baseUnits = loop != null ? loop.BaseUnitsFor(size) : 90_000;
            float exponent = loop != null ? loop.SalesScoreExponent : 2.4f;
            float bandMultiplier = HypeBandMultiplier(hype100, qf);
            long organicSales = (long)(baseUnits * Mathf.Pow(scoreFraction, exponent) * bandMultiplier);

            // A base instalada da plataforma amplia (ou reduz) o mercado total alcançável.
            long units = (long)((launchSales + organicSales) * platformReach01);
            int price = loop != null ? loop.PriceFor(size) : 20;
            long revenue = units * price;

            return new Result(units, revenue, hype01, wishlists);
        }

        /// <summary>
        /// Converte a popularidade (0..1) de um console no fator de alcance de mercado
        /// usado nas vendas. Popularidade média (~0.5) ≈ neutro; consoles dominantes
        /// ampliam, nichos reduzem. GDD 15.2 (valores a balancear).
        /// </summary>
        public static float PlatformReach(float popularity01)
        {
            return Mathf.Lerp(0.5f, 1.55f, Mathf.Clamp01(popularity01));
        }

        /// <summary>Multiplicador de vendas por banda de hype (GDD 19.6).</summary>
        /// <summary>
        /// Efeito do preço escolhido sobre as unidades (adendo 23.3): cobrar acima do preço
        /// de referência do escopo vende menos; abaixo, vende mais.
        /// </summary>
        public static float PriceFactor(int price, int referencePrice, float elasticity)
        {
            if (price <= 0 || referencePrice <= 0) return 1f;
            return Mathf.Clamp(Mathf.Pow(referencePrice / (float)price, elasticity), 0.2f, 2.5f);
        }

        /// <summary>
        /// Retenção semanal das vendas (adendo 23.4): quanto melhor a nota, mais devagar as
        /// vendas caem depois do pico de lançamento.
        /// </summary>
        public static float TailRetention(float score, GameLoopBalanceSO loop)
        {
            float worst = loop != null ? loop.TailRetentionWorst : 0.55f;
            float best = loop != null ? loop.TailRetentionBest : 0.88f;
            return Mathf.Lerp(worst, best, Mathf.Clamp01(score / 10f));
        }

        /// <summary>
        /// Unidades vendidas na semana <paramref name="weekIndex"/> (0 = lançamento) de uma
        /// cauda geométrica que soma <paramref name="totalUnits"/> em <paramref name="totalWeeks"/>.
        /// </summary>
        public static long TailUnitsForWeek(long totalUnits, int weekIndex, int totalWeeks, float retention)
        {
            if (totalUnits <= 0 || totalWeeks <= 0 || weekIndex < 0 || weekIndex >= totalWeeks) return 0;
            retention = Mathf.Clamp(retention, 0.05f, 0.99f);
            double norm = (1.0 - System.Math.Pow(retention, totalWeeks)) / (1.0 - retention);
            double share = System.Math.Pow(retention, weekIndex) / norm;
            return (long)System.Math.Round(totalUnits * share);
        }

        /// <summary>Hype (0..1) comprado com o budget de lançamento, com retorno decrescente.</summary>
        public static float LaunchBudgetHype(long budget, GameLoopBalanceSO loop)
        {
            if (budget <= 0) return 0f;
            float per10k = loop != null ? loop.LaunchBudgetHypePer10k : 0.12f;
            return Mathf.Clamp(per10k * Mathf.Sqrt(budget / 10_000f), 0f, 0.5f);
        }

        private static float HypeBandMultiplier(int hype100, QualityFormulaBalanceSO qf)
        {
            int weak = qf != null ? qf.WeakHypeUpperBound : 20;
            int medium = qf != null ? qf.MediumHypeUpperBound : 50;
            int good = qf != null ? qf.GoodHypeUpperBound : 80;

            if (hype100 <= weak) return 0.5f;     // vendas fracas
            if (hype100 <= medium) return 0.8f;    // medianas
            if (hype100 <= good) return 1.0f;      // boas
            return 1.25f;                          // excelentes — pico de lançamento
        }
    }
}
