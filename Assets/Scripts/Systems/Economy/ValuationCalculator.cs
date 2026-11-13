using System.Collections.Generic;
using GameLike.State;
using GameLike.State.Staff;
using UnityEngine;

namespace GameLike.Systems.Economy
{
    /// <summary>
    /// Calcula o valuation da empresa (GDD 4) como soma de fatores: liquidez (caixa),
    /// receita acumulada, catálogo de jogos, reputação, qualidade média do catálogo,
    /// valor da equipe e diversidade de gêneros. Puro e determinístico, testável fora
    /// do Unity. Os pesos são do slice — calibrar em playtesting.
    /// </summary>
    public static class ValuationCalculator
    {
        public static long Compute(CompanyState company, IReadOnlyList<ProjectState> released)
        {
            if (company == null) return 0;

            double v = 0;
            v += Mathf.Max(0, company.cash);                 // liquidez
            v += company.lifetimeRevenue * 0.5;              // receita histórica
            v += company.gamesReleased * 10_000.0;           // catálogo
            v += company.reputation01 * 100_000.0;           // reputação/sentimento público

            v += TeamValue(company.employees);               // qualidade/tamanho da equipe
            v += AverageScore(released) * 8_000.0;           // nota média da crítica
            v += (company.genresReleased?.Count ?? 0) * 8_000.0; // diversidade de IP
            v += FranchiseValue(released);                   // franquias ativas (GDD 13.5)
            v += company.acquiredIpValue;                    // IPs absorvidos em aquisições (GDD 12.4)

            return (long)System.Math.Max(0, v);
        }

        /// <summary>Parcelas do valuation, rotuladas, para o painel financeiro (GDD 4.1).</summary>
        public static List<KeyValuePair<string, long>> Breakdown(CompanyState company, IReadOnlyList<ProjectState> released)
        {
            var parts = new List<KeyValuePair<string, long>>();
            if (company == null) return parts;
            parts.Add(new KeyValuePair<string, long>("Liquidez (caixa)", System.Math.Max(0, company.cash)));
            parts.Add(new KeyValuePair<string, long>("Receita dos jogos", (long)(company.lifetimeRevenue * 0.5)));
            parts.Add(new KeyValuePair<string, long>("Catálogo e diversidade",
                company.gamesReleased * 10_000L + (company.genresReleased?.Count ?? 0) * 8_000L));
            parts.Add(new KeyValuePair<string, long>("Reputação e nota média",
                (long)(company.reputation01 * 100_000.0 + AverageScore(released) * 8_000.0)));
            parts.Add(new KeyValuePair<string, long>("Equipe", (long)TeamValue(company.employees)));
            parts.Add(new KeyValuePair<string, long>("IPs e franquias", (long)FranchiseValue(released) + company.acquiredIpValue));
            return parts;
        }

        private static double TeamValue(List<EmployeeInstance> staff)
        {
            if (staff == null) return 0;
            double sum = 0;
            for (int i = 0; i < staff.Count; i++)
            {
                sum += staff[i].seniority switch
                {
                    Seniority.Junior => 15_000.0,
                    Seniority.Mid => 30_000.0,
                    Seniority.Senior => 60_000.0,
                    Seniority.Star => 120_000.0,
                    _ => 15_000.0
                };
            }
            return sum;
        }

        private static double FranchiseValue(IReadOnlyList<ProjectState> released)
        {
            if (released == null) return 0;
            // Conta jogos por franquia; franquias com 2+ jogos valem como ativo (GDD 13.5),
            // proporcional à média da franquia.
            var counts = new Dictionary<string, int>();
            var scoreSum = new Dictionary<string, float>();
            for (int i = 0; i < released.Count; i++)
            {
                var fid = released[i].franchiseId;
                if (string.IsNullOrEmpty(fid)) continue;
                counts.TryGetValue(fid, out int c);
                counts[fid] = c + 1;
                scoreSum.TryGetValue(fid, out float s);
                scoreSum[fid] = s + released[i].finalScore;
            }

            double value = 0;
            foreach (var kv in counts)
            {
                if (kv.Value < 2) continue;
                float avg = scoreSum[kv.Key] / kv.Value;
                value += 40_000.0 + kv.Value * 15_000.0 + avg * 8_000.0;
            }
            return value;
        }

        private static float AverageScore(IReadOnlyList<ProjectState> released)
        {
            if (released == null || released.Count == 0) return 0f;
            float sum = 0f;
            for (int i = 0; i < released.Count; i++) sum += released[i].finalScore;
            return sum / released.Count;
        }
    }
}
