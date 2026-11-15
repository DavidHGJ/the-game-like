using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State.Staff;
using UnityEngine;

namespace GameLike.Systems.Simulation
{
    /// <summary>Resultado médio de um cenário de desenvolvimento.</summary>
    public sealed class DevScenarioResult
    {
        public DevScenario scenario;
        public string group;
        /// <summary>Linha da matriz (equipe × divisão × combinação): só entra no resumo, sem tabela jogo a jogo.</summary>
        public bool matrix;
        public string team, division, combo;
        public List<DevGameResult> games;
    }

    /// <summary>
    /// Bateria padrão do simulador de desenvolvimento (adendos 23.16/23.17) e o relatório em Markdown
    /// (<c>Docs/Simulacao/desenvolvimento.md</c>): matriz equipe fraca/média/forte × divisão nas fases
    /// boa/média/ruim × combinação gênero-tema boa/neutra/ruim na Garagem, equipes típicas de cada fase,
    /// treino e crunch — cada cenário por 10 jogos seguidos.
    /// </summary>
    public static class DevelopmentReport
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly string[] PhaseShort = { "Pré", "Arte", "Áudio", "Tec", "Cont" };

        public static readonly float[] Beginner = { 3.5f, 3f, 2.5f, 3f, 3f };
        public static readonly float[] Specialist = { 8f, 3f, 2f, 4f, 3f };
        public static readonly float[] AllRounder = { 6f, 6f, 6f, 6f, 6f };
        public static readonly float[] Expert = { 8f, 8f, 8f, 8f, 8f };

        private sealed class Entry
        {
            public string group, team, division, combo;
            public bool matrix;
            public DevScenario scenario;
        }

        private static readonly (string name, float[] aptitude)[] Teams =
        {
            ("Fraca (iniciante, todas ~3)", Beginner),
            ("Média (fundador sorteado)", null),
            ("Forte (expert, todas 8)", Expert)
        };

        private static readonly (string name, DevAllocation allocation)[] Divisions =
        {
            ("Boa (= pesos do gênero)", DevAllocation.Ideal),
            ("Média (divide igual)", DevAllocation.Even),
            ("Ruim (pesos invertidos)", DevAllocation.Inverted)
        };

        private static readonly (string name, DevCombo combo)[] Combos =
        {
            ("Boa (+++)", DevCombo.Good),
            ("Neutra", DevCombo.Neutral),
            ("Ruim (---)", DevCombo.Bad)
        };

        /// <summary>Cenários padrão (a matriz da Garagem e os cenários de fase, treino e crunch).</summary>
        public static List<(string group, DevScenario scenario)> StandardScenarios()
        {
            var list = new List<(string, DevScenario)>();
            foreach (var e in Entries()) list.Add((e.group, e.scenario));
            return list;
        }

        private static List<Entry> Entries()
        {
            var list = new List<Entry>();
            const string matrix = "1. Matriz na Garagem (fundador sozinho, jogo Pequeno)";
            foreach (var team in Teams)
                foreach (var division in Divisions)
                    foreach (var combo in Combos)
                        list.Add(new Entry
                        {
                            group = matrix, matrix = true, team = team.name, division = division.name, combo = combo.name,
                            scenario = new DevScenario
                            {
                                name = team.name + " · " + division.name + " · " + combo.name,
                                founderAptitude = team.aptitude, allocation = division.allocation, combo = combo.combo
                            }
                        });

            const string phases = "2. Equipes típicas de cada fase, no tamanho principal (combinação neutra)";
            foreach (var division in new[] { Divisions[0], Divisions[2] })
            {
                list.Add(Phase(phases, "Indie: fundador + 2 júnior (Arte, Programação) — Médio · divisão " + division.name, ProjectSize.Medium, division.allocation,
                    new DevHire(Specialty.Art, Seniority.Junior), new DevHire(Specialty.Programming, Seniority.Junior)));
                list.Add(Phase(phases, "Escritório: fundador + 5 pleno (um por área) — Grande · divisão " + division.name, ProjectSize.Large, division.allocation,
                    new DevHire(Specialty.Design, Seniority.Mid), new DevHire(Specialty.Art, Seniority.Mid), new DevHire(Specialty.Audio, Seniority.Mid),
                    new DevHire(Specialty.Programming, Seniority.Mid), new DevHire(Specialty.Content, Seniority.Mid)));
                list.Add(Phase(phases, "Estúdio: fundador + 8 sênior — AAA · divisão " + division.name, ProjectSize.AAA, division.allocation,
                    new DevHire(Specialty.Design, Seniority.Senior), new DevHire(Specialty.Art, Seniority.Senior), new DevHire(Specialty.Art, Seniority.Senior),
                    new DevHire(Specialty.Audio, Seniority.Senior), new DevHire(Specialty.Programming, Seniority.Senior),
                    new DevHire(Specialty.Programming, Seniority.Senior), new DevHire(Specialty.Content, Seniority.Senior),
                    new DevHire(Specialty.Content, Seniority.Senior)));
            }
            list.Add(Phase(phases, "Indie sem contratar (fundador sozinho) — Médio · divisão Boa", ProjectSize.Medium, DevAllocation.Ideal));

            const string engine = "3. Engine: fundador médio na Garagem, combinação neutra";
            foreach (var division in new[] { Divisions[0], Divisions[2] })
                foreach (var (label, kind) in new[] { ("sem engine", DevEngine.None), ("engine inicial", DevEngine.Starter), ("engine completa", DevEngine.Full) })
                    list.Add(new Entry { group = engine, scenario = new DevScenario { name = "Divisão " + division.name + " · " + label, allocation = division.allocation, engine = kind } });

            const string extra = "4. Fundador médio na Garagem: treino e crunch (divisão boa, combinação neutra)";
            list.Add(new Entry { group = extra, scenario = new DevScenario { name = "Sem treino (referência)" } });
            list.Add(new Entry { group = extra, scenario = new DevScenario { name = "Treina a área mais fraca a cada jogo", training = DevTraining.WeakestEachGame } });
            list.Add(new Entry { group = extra, scenario = new DevScenario { name = "Treina a área da pior fase a cada jogo", training = DevTraining.WorstPhaseEachGame } });
            list.Add(new Entry { group = extra, scenario = new DevScenario { name = "Crunch em todas as fases", crunch = true } });
            list.Add(new Entry { group = extra, scenario = new DevScenario { name = "Especialista (Design 8, resto fraco)", founderAptitude = Specialist } });
            return list;
        }

        private static Entry Phase(string group, string name, ProjectSize size, DevAllocation allocation, params DevHire[] hires)
        {
            var s = new DevScenario { name = name, size = size, allocation = allocation };
            s.hires.AddRange(hires);
            return new Entry { group = group, scenario = s };
        }

        /// <summary>Roda todos os cenários (média de várias sementes e gêneros).</summary>
        public static List<DevScenarioResult> RunAll(IReadOnlyList<GenreSO> genres, int repetitions)
        {
            var results = new List<DevScenarioResult>();
            foreach (var e in Entries())
            {
                int reps = e.scenario.founderAptitude == null || e.scenario.hires.Count > 0 ? repetitions : 1;
                results.Add(new DevScenarioResult
                {
                    group = e.group, scenario = e.scenario, matrix = e.matrix, team = e.team, division = e.division, combo = e.combo,
                    games = DevelopmentSimulator.RunAverage(e.scenario, genres, reps)
                });
            }
            return results;
        }

        public static string ToMarkdown(List<DevScenarioResult> results, int genreCount, int repetitions)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var sb = new StringBuilder();
            sb.AppendLine("# Simulação do desenvolvimento (nota quão perto do perfeito — GDD 23.16/23.18)");
            sb.AppendLine();
            sb.AppendLine("Cada cenário faz 10 jogos seguidos com as mesmas regras do jogo: pontos por semana pela aptidão, prática a cada " +
                          "ponto, treino entre jogos. Sem dinheiro nem vendas — só produção e nota.");
            sb.AppendLine();
            sb.AppendLine("- **Valores:** " + Num(loop != null ? loop.BasePointsPerWeek : 3f) + " pts-base/semana · prática " +
                          Num(loop != null ? loop.PracticePerPoint : 0.02f, "0.###") + "/ponto · tema " + Pct(loop != null ? loop.ThemeShare : 0.5f) +
                          " da nota · contraste do perfil " + Num(loop != null ? loop.GenreWeightContrast : 2f) + " · tolerância " + Pct(loop != null ? loop.BlockToleranceRelative : 0.1f) + " do ideal · bônus de pontos " +
                          Num(loop != null ? loop.PointsBonusFactor : 0.5f) + " · engine " + Pct(loop != null ? loop.EngineReliefPerLevel : 0.1f) +
                          "/nível até " + Pct(loop != null ? loop.EngineReliefMax : 0.4f) + " · bugs " + Num(loop != null ? loop.BugsPerPoint : 0.1f) +
                          "/ponto, desconto até " + Pct(loop != null ? loop.BugPenaltyMax : 0.5f) + ".");
            sb.AppendLine("- **Divisão** = acerto só da divisão em % nos blocos (decisão; a aptidão não muda). **Fases** = acerto final das fases " +
                          "com o bônus dos pontos e a amenização da engine. **Nota** = 10 × (metade tema + metade fases) × (1 − desconto dos bugs).");
            sb.AppendLine("- **Divisão boa** = pesos do gênero; **média** = divide igual; **ruim** = pesos invertidos. " +
                          "**Combinação** = gênero × tema (componente de 20%).");
            sb.AppendLine("- Média de " + genreCount + " gênero(s)" + (repetitions > 1 ? " × " + repetitions + " sorteios de aptidão" : "") +
                          ". Nota estimada sem sorteio da crítica, mercado 50%, engine 60%, com a penalidade dos bugs esperados.");
            sb.AppendLine();

            string currentGroup = null;
            bool matrixHeader = false;
            foreach (var r in results)
            {
                if (r.group != currentGroup)
                {
                    currentGroup = r.group;
                    sb.AppendLine("## " + r.group);
                    sb.AppendLine();
                    matrixHeader = false;
                }
                var first = r.games[0];
                var last = r.games[r.games.Count - 1];
                if (r.matrix)
                {
                    if (!matrixHeader)
                    {
                        sb.AppendLine("| Equipe | Divisão nas fases | Gênero × tema | Divisão | Pontos / esperado | Fases | Bugs | **Nota jogo 1** | Nota jogo 10 |");
                        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
                        matrixHeader = true;
                    }
                    sb.AppendLine("| " + r.team + " | " + r.division + " | " + r.combo + " | " + Pct(first.mix) +
                                  " | " + Num(first.points, "0") + " / " + Num(first.expectedPoints, "0") +
                                  " | " + Pct(first.phasesAccuracy) + " | −" + Pct(first.bugPenalty) + " | **" + Num(first.score) + "** | " + Num(last.score) + " |");
                    continue;
                }
                if (matrixHeader) { sb.AppendLine(); matrixHeader = false; }
                long training = 0;
                foreach (var x in r.games) training += x.trainingCost;
                sb.AppendLine("### " + r.scenario.name);
                sb.AppendLine();
                sb.AppendLine("Nota " + Num(first.score) + " → " + Num(last.score) + " em 10 jogos" +
                              (training > 0 ? " · treino total R$ " + training.ToString("N0", Culture) : "") + ".");
                sb.AppendLine();
                sb.AppendLine("| Jogo | Semanas | Aptidão média (fundador) | Pior área | Pontos | Volume por fase (" + string.Join(" · ", PhaseShort) +
                              ") | Divisão | Fases | Bugs (desconto) | Nota |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
                foreach (var g in r.games)
                {
                    var vol = new string[g.phaseVolume.Length];
                    for (int i = 0; i < vol.Length; i++) vol[i] = Pct(g.phaseVolume[i]);
                    sb.AppendLine("| " + g.game + " | " + g.weeks + " | " + Num(g.founderAptitude) + " | " + Num(g.founderWeakest) +
                                  " | " + Num(g.points, "0") + " / " + Num(g.expectedPoints, "0") + " | " + string.Join(" · ", vol) +
                                  " | " + Pct(g.mix) + " | " + Pct(g.phasesAccuracy) + " | " + Num(g.bugs, "0") + " (−" + Pct(g.bugPenalty) + ") | **" + Num(g.score) + "** |");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string Num(float v, string format = "0.0") => v.ToString(format, Culture);
        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
    }
}
