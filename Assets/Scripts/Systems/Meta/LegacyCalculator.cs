using System.Collections.Generic;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.State;

namespace GameLike.Systems.Meta
{
    /// <summary>Uma linha do extrato de Pontos de Legado mostrado no fim da run.</summary>
    public readonly struct LegacyLine
    {
        public readonly string Label;
        public readonly int Points;
        public LegacyLine(string label, int points) { Label = label; Points = points; }
    }

    /// <summary>
    /// Calcula os Pontos de Legado ganhos ao fim de uma run (GDD 19.5): fase alcançada, jogos
    /// com nota acima de 7, franquias com 3+ jogos, run sem crise grave e valuation máximo.
    /// Lógica pura; os valores vêm do <see cref="LegacyProgressionBalanceSO"/>.
    /// </summary>
    public static class LegacyCalculator
    {
        /// <summary>Total de PL da run (compatibilidade: sem informação de crise, conta como sem crise).</summary>
        public static int Compute(CompanyState company, IReadOnlyList<ProjectState> released, bool bankrupt)
        {
            int total = 0;
            var lines = Breakdown(company, released, bankrupt, false);
            for (int i = 0; i < lines.Count; i++) total += lines[i].Points;
            return total;
        }

        /// <summary>Total de PL da run.</summary>
        public static int Compute(RunState run, bool bankrupt)
        {
            if (run == null) return 0;
            int total = 0;
            var lines = Breakdown(run.company, run.releasedProjects, bankrupt, run.hadMajorCrisis);
            for (int i = 0; i < lines.Count; i++) total += lines[i].Points;
            return total;
        }

        /// <summary>Extrato linha a linha dos PL ganhos (para a tela de resultado, GDD 20.4).</summary>
        public static List<LegacyLine> Breakdown(CompanyState company, IReadOnlyList<ProjectState> released,
            bool bankrupt, bool hadMajorCrisis)
        {
            var lines = new List<LegacyLine>();
            if (company == null) return lines;
            var b = BalanceProvider.Get<LegacyProgressionBalanceSO>();

            switch (company.phase)
            {
                case CompanyPhase.Studio: lines.Add(new LegacyLine("Chegou à fase Estúdio", b != null ? b.PlForReachingStudio : 30)); break;
                case CompanyPhase.Office: lines.Add(new LegacyLine("Chegou à fase Escritório", b != null ? b.PlForReachingOffice : 15)); break;
                case CompanyPhase.Indie: lines.Add(new LegacyLine("Chegou à fase Indie", b != null ? b.PlForReachingIndie : 5)); break;
            }

            int goodGames = 0;
            var franchiseCounts = new Dictionary<string, int>();
            if (released != null)
            {
                for (int i = 0; i < released.Count; i++)
                {
                    if (released[i].finalScore > 7f) goodGames++;
                    string fid = released[i].franchiseId;
                    if (string.IsNullOrEmpty(fid)) continue;
                    franchiseCounts.TryGetValue(fid, out int n);
                    franchiseCounts[fid] = n + 1;
                }
            }
            if (goodGames > 0)
                lines.Add(new LegacyLine(goodGames == 1 ? "1 jogo com nota acima de 7" : goodGames + " jogos com nota acima de 7",
                    goodGames * (b != null ? b.PlPerGameAboveScoreSeven : 2)));

            int bigFranchises = 0;
            foreach (var kv in franchiseCounts) if (kv.Value >= 3) bigFranchises++;
            if (bigFranchises > 0)
                lines.Add(new LegacyLine(bigFranchises == 1 ? "1 franquia com 3+ jogos" : bigFranchises + " franquias com 3+ jogos",
                    bigFranchises * (b != null ? b.PlForFranchiseWithThreeOrMoreGames : 10)));

            if (company.peakValuation >= 10_000_000)
                lines.Add(new LegacyLine("Valuation máximo acima de 10 milhões", b != null ? b.PlForValuationAboveTenMillion : 15));
            else if (company.peakValuation >= 1_000_000)
                lines.Add(new LegacyLine("Valuation máximo acima de 1 milhão", b != null ? b.PlForValuationAboveOneMillion : 5));

            if (!bankrupt && !hadMajorCrisis)
                lines.Add(new LegacyLine("Run sem crise grave", b != null ? b.PlForRunWithoutMajorCrisis : 5));
            return lines;
        }
    }
}
