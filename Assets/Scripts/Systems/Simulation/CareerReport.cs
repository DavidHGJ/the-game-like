using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameLike.Core.Events;
using GameLike.State.Staff;

namespace GameLike.Systems.Simulation
{
    /// <summary>Números de um perfil numa posição da carreira (1ª run, 2ª run...), somando todas as carreiras simuladas.</summary>
    public sealed class CareerRunStats
    {
        public int runIndex;
        public int careers;
        /// <summary>Quantas carreiras chegaram a cada fase nesta run (índice = fase).</summary>
        public int[] reached = new int[4];
        public int bankrupt;
        public long medianPayout;
        public int medianWeeks;
        public float medianGames;
        public float hitRate;

        public float ReachedShare(CompanyPhase phase) => careers > 0 ? reached[(int)phase] / (float)careers : 0f;
        public float BankruptShare => careers > 0 ? bankrupt / (float)careers : 0f;
    }

    /// <summary>Resultado de um perfil: estatísticas por run e em quantas runs ele chega a cada fase.</summary>
    public sealed class CareerProfileResult
    {
        public CareerProfile profile;
        public readonly List<CareerRunStats> runs = new List<CareerRunStats>();
        /// <summary>Fração das carreiras que chegaram ao Estúdio em alguma run.</summary>
        public float studioEver;
        /// <summary>Fração das carreiras que chegaram ao Escritório em alguma run.</summary>
        public float officeEver;
    }

    /// <summary>Uma meta da simulação longa (proposta de design para "avança, mas não tudo").</summary>
    public sealed class CareerGoal
    {
        public string text;
        public bool passed;
        public string measured;
    }

    /// <summary>
    /// Bateria da simulação longa (GDD 23.14 — o jogador avança, mas o reset é necessário):
    /// perfis de jogador (iniciante, médio, bom, expert) × estratégias de equipe, cada um com
    /// muitas carreiras de várias runs, e metas propostas de progressão.
    /// </summary>
    public static class CareerReport
    {
        public const int RunsPerCareer = 5;
        public const int WeeksPerRun = 520;
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly string[] PhaseNames = { "Garagem", "Indie", "Escritório", "Estúdio" };

        /// <summary>Os perfis padrão.</summary>
        public static List<CareerProfile> StandardProfiles()
        {
            var list = new List<CareerProfile>();
            foreach (var (label, mean, spread) in new[] { ("Iniciante", 5.0f, 1.5f), ("Médio", 6.3f, 1.3f), ("Bom", 7.3f, 1.1f), ("Expert", 8.3f, 0.8f) })
            {
                list.Add(new CareerProfile { name = label + " · equipe enxuta (júnior)", skillMean = mean, skillSpread = spread, team = TeamStrategy.Lean, hireSeniority = Seniority.Junior });
                list.Add(new CareerProfile { name = label + " · equipe cheia (pleno)", skillMean = mean, skillSpread = spread, team = TeamStrategy.Full, hireSeniority = Seniority.Mid });
            }
            list.Add(new CareerProfile { name = "Médio · não vende antes de quebrar", skillMean = 6.3f, skillSpread = 1.3f, sellsBeforeBankruptcy = false });
            list.Add(new CareerProfile { name = "Médio · fundador nível 3 em tudo", skillMean = 6.3f, skillSpread = 1.3f, businessLevel = 3, marketingLevel = 3, charismaLevel = 3, technicalLevel = 3 });
            return list;
        }

        /// <summary>Roda todos os perfis com <paramref name="careers"/> carreiras cada (sorteio fixo: resultados repetíveis).</summary>
        public static List<CareerProfileResult> RunAll(int careers)
        {
            var results = new List<CareerProfileResult>();
            int seed = 1;
            foreach (var profile in StandardProfiles()) results.Add(RunProfile(profile, careers, seed++ * 1000));
            return results;
        }

        public static CareerProfileResult RunProfile(CareerProfile profile, int careers, int seed)
        {
            var all = new List<List<CareerRun>>();
            for (int c = 0; c < careers; c++)
                all.Add(CareerSimulator.RunCareer(profile, RunsPerCareer, WeeksPerRun, new System.Random(seed + c)));

            var result = new CareerProfileResult { profile = profile };
            for (int r = 0; r < RunsPerCareer; r++)
            {
                var stats = new CareerRunStats { runIndex = r + 1, careers = careers };
                var payouts = new List<long>();
                var weeks = new List<int>();
                var games = new List<int>();
                int hits = 0, totalGames = 0;
                foreach (var career in all)
                {
                    var run = career[r];
                    for (int p = 0; p <= (int)run.maxPhase; p++) stats.reached[p]++;
                    if (run.end == CareerRunEnd.Bankrupt) stats.bankrupt++;
                    payouts.Add(run.payout);
                    weeks.Add(run.weeks);
                    games.Add(run.games);
                    hits += run.hits;
                    totalGames += run.games;
                }
                payouts.Sort(); weeks.Sort(); games.Sort();
                stats.medianPayout = payouts[payouts.Count / 2];
                stats.medianWeeks = weeks[weeks.Count / 2];
                stats.medianGames = games[games.Count / 2];
                stats.hitRate = totalGames > 0 ? hits / (float)totalGames : 0f;
                result.runs.Add(stats);
            }
            int office = 0, studio = 0;
            foreach (var career in all)
            {
                bool o = false, s = false;
                foreach (var run in career) { o |= run.maxPhase >= CompanyPhase.Office; s |= run.maxPhase >= CompanyPhase.Studio; }
                if (o) office++;
                if (s) studio++;
            }
            result.officeEver = office / (float)careers;
            result.studioEver = studio / (float)careers;
            return result;
        }

        /// <summary>
        /// Metas propostas para "avança, mas não tudo": ninguém fecha o jogo na primeira run, o
        /// jogador médio avança ao longo das runs e quem joga bem chega ao fim depois de alguns resets.
        /// </summary>
        public static List<CareerGoal> Goals(List<CareerProfileResult> results)
        {
            var goals = new List<CareerGoal>();
            float maxStudioRun1 = 0f;
            foreach (var r in results) maxStudioRun1 = Math.Max(maxStudioRun1, r.runs[0].ReachedShare(CompanyPhase.Studio));
            goals.Add(Goal("Na 1ª run, nenhum perfil chega ao Estúdio em mais de 10% das carreiras", maxStudioRun1 <= 0.10f, Pct(maxStudioRun1) + " no melhor perfil"));

            var medium = results.Find(r => r.profile.name.StartsWith("Médio · equipe enxuta"));
            if (medium != null)
            {
                goals.Add(Goal("Jogador médio sai da Garagem na 1ª run em pelo menos 40% das vezes", medium.runs[0].ReachedShare(CompanyPhase.Indie) >= 0.40f,
                    Pct(medium.runs[0].ReachedShare(CompanyPhase.Indie))));
                goals.Add(Goal("Jogador médio chega ao Escritório em alguma das 5 runs em pelo menos 50% das carreiras", medium.officeEver >= 0.50f, Pct(medium.officeEver)));
            }
            var expert = results.Find(r => r.profile.name.StartsWith("Expert · equipe enxuta"));
            if (expert != null)
                goals.Add(Goal("Expert chega ao Estúdio em alguma das 5 runs em pelo menos 60% das carreiras", expert.studioEver >= 0.60f, Pct(expert.studioEver)));
            var novice = results.Find(r => r.profile.name.StartsWith("Iniciante · equipe enxuta"));
            if (novice != null)
                goals.Add(Goal("Iniciante não chega ao Escritório na 1ª run (no máximo 5%)", novice.runs[0].ReachedShare(CompanyPhase.Office) <= 0.05f,
                    Pct(novice.runs[0].ReachedShare(CompanyPhase.Office))));
            return goals;
        }

        private static CareerGoal Goal(string text, bool passed, string measured) => new CareerGoal { text = text, passed = passed, measured = measured };

        public static string Pct(float share) => Math.Round(share * 100f).ToString("0", Culture) + "%";

        public static string PhaseName(CompanyPhase phase) => PhaseNames[(int)phase];

        /// <summary>Relatório em Markdown.</summary>
        public static string ToMarkdown(List<CareerProfileResult> results, int careers)
        {
            var sb = new StringBuilder();
            var goals = Goals(results);
            int passed = goals.FindAll(g => g.passed).Count;
            sb.AppendLine("# Simulação longa — carreiras de " + RunsPerCareer + " runs");
            sb.AppendLine();
            sb.AppendLine(careers + " carreiras por perfil, até " + WeeksPerRun + " semanas por run (10 anos). O dinheiro da venda vira o capital da run seguinte.");
            sb.AppendLine("O jogador simulado faz jogos sem parar no maior tamanho da fase que já pesquisou, expande quando o valuation libera e tem caixa,");
            sb.AppendLine("contrata conforme a estratégia e (salvo indicado) vende a empresa quando está prestes a falir.");
            sb.AppendLine();
            sb.AppendLine("## Metas propostas — " + passed + " de " + goals.Count);
            sb.AppendLine();
            foreach (var g in goals) sb.AppendLine("- " + (g.passed ? "✅ " : "❌ ") + g.text + " — medido: " + g.measured);
            sb.AppendLine();
            foreach (var r in results)
            {
                sb.AppendLine("## " + r.profile.name);
                sb.AppendLine();
                sb.AppendLine("Nota média " + r.profile.skillMean.ToString("0.0", Culture) + " ± " + r.profile.skillSpread.ToString("0.0", Culture)
                              + " · chegou ao Escritório em alguma run: " + Pct(r.officeEver) + " · ao Estúdio: " + Pct(r.studioEver));
                sb.AppendLine();
                sb.AppendLine("| Run | Chegou ao Indie | Escritório | Estúdio | Faliu | Semanas (mediana) | Jogos | Acertos (nota 7+) | Leva para a próxima (mediana) |");
                sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
                foreach (var s in r.runs)
                {
                    sb.AppendLine("| " + s.runIndex + " | " + Pct(s.ReachedShare(CompanyPhase.Indie)) + " | " + Pct(s.ReachedShare(CompanyPhase.Office))
                                  + " | " + Pct(s.ReachedShare(CompanyPhase.Studio)) + " | " + Pct(s.BankruptShare) + " | " + s.medianWeeks
                                  + " | " + s.medianGames.ToString("0", Culture) + " | " + Pct(s.hitRate) + " | " + EconomyReport.Money(s.medianPayout) + " |");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
