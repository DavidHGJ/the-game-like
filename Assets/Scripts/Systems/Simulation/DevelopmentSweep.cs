using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State.Staff;
using UnityEngine;

namespace GameLike.Systems.Simulation
{
    /// <summary>Uma linha da varredura: a combinação e a nota média do 1º jogo.</summary>
    public sealed class SweepRow
    {
        public int size, team, division, theme, engine;
        public float score, division01, phases01, bugPenalty, points, expected;
    }

    /// <summary>
    /// Varredura completa do desenvolvimento (adendo 23.18): todos os tamanhos (com a equipe típica de cada fase) ×
    /// força da equipe × 9 jeitos de dividir as fases (perfeita → péssima, igual, invertida e mistas) × os 7 níveis
    /// de gênero × tema × 3 engines. Mede a nota do 1º jogo (média de gêneros e sorteios) e checa a coerência:
    /// melhorar uma decisão nunca pode baixar a nota.
    /// </summary>
    public static class DevelopmentSweep
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

        public static readonly string[] Sizes = { "Pequeno (Garagem)", "Médio (Indie)", "Grande (Escritório)", "AAA (Estúdio)" };
        public static readonly string[] Teams = { "Fraca", "Média", "Forte" };
        public static readonly string[] Themes = { "−−−", "−−", "−", "Neutro", "+", "++", "+++" };
        public static readonly string[] Engines = { "Sem engine", "Engine inicial", "Engine completa" };

        /// <summary>Jeitos de dividir as fases: nome e erro por fase (nulo = estratégia especial).</summary>
        public static readonly (string name, float[] error, DevAllocation special)[] Divisions =
        {
            ("Perfeita", new[] { 0f, 0f, 0f, 0f, 0f }, DevAllocation.Ideal),
            ("Boa (erro 25%)", new[] { 0.25f, 0.25f, 0.25f, 0.25f, 0.25f }, DevAllocation.Ideal),
            ("Média (erro 50%)", new[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f }, DevAllocation.Ideal),
            ("Ruim (erro 75%)", new[] { 0.75f, 0.75f, 0.75f, 0.75f, 0.75f }, DevAllocation.Ideal),
            ("Péssima (tudo no bloco menos importante)", new[] { 1f, 1f, 1f, 1f, 1f }, DevAllocation.Ideal),
            ("Divide igual", null, DevAllocation.Even),
            ("Invertida", null, DevAllocation.Inverted),
            ("Mista: 3 fases certas, 2 péssimas", new[] { 0f, 1f, 0f, 1f, 0f }, DevAllocation.Ideal),
            ("Mista: só a Pré-produção péssima", new[] { 1f, 0f, 0f, 0f, 0f }, DevAllocation.Ideal)
        };

        /// <summary>Ordem de qualidade para a checagem de coerência (só as divisões uniformes, perfeita → péssima).</summary>
        private static readonly int[] DivisionOrder = { 0, 1, 2, 3, 4 };

        private static readonly ProjectSize[] SizeValues = { ProjectSize.Small, ProjectSize.Medium, ProjectSize.Large, ProjectSize.AAA };

        /// <summary>Monta o cenário de uma combinação.</summary>
        public static DevScenario Scenario(int size, int team, int division, int theme, int engine)
        {
            var d = Divisions[division];
            var s = new DevScenario
            {
                name = Sizes[size] + " · " + Teams[team] + " · " + d.name + " · " + Themes[theme] + " · " + Engines[engine],
                size = SizeValues[size],
                games = 1,
                themeWeight = theme - 3,
                engine = (DevEngine)engine,
                phaseError = d.error,
                allocation = d.special,
                founderAptitude = team == 0 ? DevelopmentReport.Beginner : team == 2 ? DevelopmentReport.Expert : null
            };
            var seniority = team == 0 ? Seniority.Junior : team == 1 ? Seniority.Mid : Seniority.Senior;
            Specialty[] hires = size switch
            {
                1 => new[] { Specialty.Art, Specialty.Programming },
                2 => new[] { Specialty.Design, Specialty.Art, Specialty.Audio, Specialty.Programming, Specialty.Content },
                3 => new[] { Specialty.Design, Specialty.Art, Specialty.Art, Specialty.Audio, Specialty.Programming,
                             Specialty.Programming, Specialty.Content, Specialty.Content },
                _ => new Specialty[0]
            };
            foreach (var h in hires) s.hires.Add(new DevHire(h, seniority));
            return s;
        }

        /// <summary>Roda todas as combinações (média de gêneros e sorteios; o 1º jogo de cada).</summary>
        public static List<SweepRow> RunAll(IReadOnlyList<GenreSO> genres, int repetitions)
        {
            var rows = new List<SweepRow>();
            for (int size = 0; size < Sizes.Length; size++)
                for (int team = 0; team < Teams.Length; team++)
                    for (int division = 0; division < Divisions.Length; division++)
                        for (int theme = 0; theme < Themes.Length; theme++)
                            for (int engine = 0; engine < Engines.Length; engine++)
                            {
                                var scenario = Scenario(size, team, division, theme, engine);
                                int reps = scenario.founderAptitude == null || scenario.hires.Count > 0 ? repetitions : 1;
                                var g = DevelopmentSimulator.RunAverage(scenario, genres, reps)[0];
                                rows.Add(new SweepRow
                                {
                                    size = size, team = team, division = division, theme = theme, engine = engine,
                                    score = g.score, division01 = g.mix, phases01 = g.phasesAccuracy, bugPenalty = g.bugPenalty,
                                    points = g.points, expected = g.expectedPoints
                                });
                            }
            return rows;
        }

        /// <summary>
        /// Checagem de coerência: com o resto igual, divisão melhor, tema melhor, equipe mais forte e engine maior
        /// nunca podem dar nota menor. Devolve as violações (vazio = coerente).
        /// </summary>
        public static List<string> Violations(List<SweepRow> rows, float tolerance = 0.05f)
        {
            var index = rows.ToDictionary(r => (r.size, r.team, r.division, r.theme, r.engine));
            var problems = new List<string>();
            foreach (var r in rows)
            {
                void Check(string what, (int, int, int, int, int) betterKey)
                {
                    if (index.TryGetValue(betterKey, out var better) && better.score + tolerance < r.score)
                        problems.Add(what + " baixou a nota: " + Describe(r) + " = " + Num(r.score) + " → " + Num(better.score));
                }
                int pos = System.Array.IndexOf(DivisionOrder, r.division);
                if (pos > 0) Check("Divisão melhor", (r.size, r.team, DivisionOrder[pos - 1], r.theme, r.engine));
                if (r.theme < Themes.Length - 1) Check("Tema melhor", (r.size, r.team, r.division, r.theme + 1, r.engine));
                if (r.team < Teams.Length - 1) Check("Equipe mais forte", (r.size, r.team + 1, r.division, r.theme, r.engine));
                if (r.engine < Engines.Length - 1) Check("Engine maior", (r.size, r.team, r.division, r.theme, r.engine + 1));
            }
            return problems;
        }

        public static string Describe(SweepRow r) =>
            Sizes[r.size] + " · equipe " + Teams[r.team] + " · " + Divisions[r.division].name + " · tema " + Themes[r.theme] + " · " + Engines[r.engine];

        public static string ToMarkdown(List<SweepRow> rows, int genreCount, int repetitions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Varredura completa do desenvolvimento (GDD 23.18)");
            sb.AppendLine();
            sb.AppendLine(rows.Count + " combinações: " + Sizes.Length + " tamanhos × " + Teams.Length + " equipes × " + Divisions.Length +
                          " divisões × " + Themes.Length + " temas × " + Engines.Length + " engines. Nota do 1º jogo, média de " + genreCount +
                          " gêneros" + (repetitions > 1 ? " × " + repetitions + " sorteios do fundador médio" : "") +
                          ", sem o sorteio da crítica e sem corrigir bugs antes de lançar. Tabela inteira em `desenvolvimento_varredura.csv`.");
            sb.AppendLine();
            sb.AppendLine("- **Equipes:** Fraca = fundador iniciante (~3) + júniores; Média = fundador sorteado + plenos; Forte = fundador expert (8) + sêniores. " +
                          "Pequeno = só o fundador; Médio +2; Grande +5; AAA +8.");
            sb.AppendLine("- **Divisões:** erro 0 = igual aos pesos do gênero; erro 1 = tudo no bloco que o gênero menos quer (fase de bloco único não tem escolha).");
            sb.AppendLine("- **Engine:** inicial = renderizador e áudio básicos; completa = amenização máxima em todo bloco que tem módulo.");
            sb.AppendLine();

            var all = rows.Select(r => r.score).ToList();
            sb.AppendLine("## Visão geral");
            sb.AppendLine();
            sb.AppendLine("- Menor nota: **" + Num(all.Min()) + "** — " + Describe(rows.OrderBy(r => r.score).First()));
            sb.AppendLine("- Maior nota: **" + Num(all.Max()) + "** — " + Describe(rows.OrderByDescending(r => r.score).First()));
            sb.AppendLine("- Média: " + Num(all.Average()));
            sb.AppendLine();
            sb.AppendLine("| Faixa de nota | Combinações | % |");
            sb.AppendLine("|---|---|---|");
            for (int b = 0; b < 10; b++)
            {
                int count = rows.Count(r => r.score >= b && (b == 9 ? r.score <= 10f : r.score < b + 1));
                sb.AppendLine("| " + b + " a " + (b + 1) + " | " + count + " | " + Pct(count / (float)rows.Count) + " |");
            }
            sb.AppendLine();

            var problems = Violations(rows);
            sb.AppendLine("## Coerência (melhorar nunca pode baixar a nota)");
            sb.AppendLine();
            if (problems.Count == 0) sb.AppendLine("Nenhuma violação: divisão, tema, equipe e engine melhores nunca baixam a nota.");
            else
            {
                sb.AppendLine("**" + problems.Count + " violações.** Primeiras:");
                sb.AppendLine();
                foreach (var p in problems.Take(15)) sb.AppendLine("- " + p);
            }
            sb.AppendLine();

            sb.AppendLine("## Efeito de cada eixo (nota média, com todo o resto variando)");
            sb.AppendLine();
            Axis(sb, "Gênero × tema", Themes, i => rows.Where(r => r.theme == i));
            Axis(sb, "Divisão nas fases", Divisions.Select(d => d.name).ToArray(), i => rows.Where(r => r.division == i));
            Axis(sb, "Equipe", Teams, i => rows.Where(r => r.team == i));
            Axis(sb, "Engine", Engines, i => rows.Where(r => r.engine == i));
            Axis(sb, "Tamanho", Sizes, i => rows.Where(r => r.size == i));

            for (int size = 0; size < Sizes.Length; size++)
            {
                sb.AppendLine("## " + Sizes[size] + ": divisão × tema (equipe média, engine inicial)");
                sb.AppendLine();
                sb.AppendLine("| Divisão | " + string.Join(" | ", Themes) + " |");
                sb.AppendLine("|---|" + string.Concat(Enumerable.Repeat("---|", Themes.Length)));
                for (int d = 0; d < Divisions.Length; d++)
                {
                    var cells = new List<string>();
                    for (int t = 0; t < Themes.Length; t++)
                    {
                        var r = rows.FirstOrDefault(x => x.size == size && x.team == 1 && x.engine == 1 && x.division == d && x.theme == t);
                        cells.Add(r != null ? Num(r.score) : "—");
                    }
                    sb.AppendLine("| " + Divisions[d].name + " | " + string.Join(" | ", cells) + " |");
                }
                sb.AppendLine();
                sb.AppendLine("Equipe fraca → forte com divisão Ruim e tema neutro: " +
                              string.Join(" → ", Enumerable.Range(0, Teams.Length).Select(team =>
                                  Num(rows.First(x => x.size == size && x.team == team && x.engine == 1 && x.division == 3 && x.theme == 3).score))) +
                              " · sem → com engine completa (mesmo caso, equipe média): " +
                              string.Join(" → ", Enumerable.Range(0, Engines.Length).Select(e =>
                                  Num(rows.First(x => x.size == size && x.team == 1 && x.engine == e && x.division == 3 && x.theme == 3).score))) + ".");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static void Axis(StringBuilder sb, string title, string[] labels, System.Func<int, IEnumerable<SweepRow>> select)
        {
            sb.AppendLine("### " + title);
            sb.AppendLine();
            sb.AppendLine("| " + title + " | Nota média | Menor | Maior | Acerto das fases | Desconto de bugs |");
            sb.AppendLine("|---|---|---|---|---|---|");
            for (int i = 0; i < labels.Length; i++)
            {
                var set = select(i).ToList();
                if (set.Count == 0) continue;
                sb.AppendLine("| " + labels[i] + " | **" + Num(set.Average(r => r.score)) + "** | " + Num(set.Min(r => r.score)) + " | " +
                              Num(set.Max(r => r.score)) + " | " + Pct(set.Average(r => r.phases01)) + " | −" + Pct(set.Average(r => r.bugPenalty)) + " |");
            }
            sb.AppendLine();
        }

        public static string ToCsv(List<SweepRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("tamanho;equipe;divisao;tema;engine;nota;acerto_divisao;acerto_fases;desconto_bugs;pontos;esperado");
            foreach (var r in rows)
                sb.AppendLine(string.Join(";", Sizes[r.size], Teams[r.team], Divisions[r.division].name, Themes[r.theme], Engines[r.engine],
                    Num(r.score, "0.00"), Pct(r.division01), Pct(r.phases01), Pct(r.bugPenalty), Num(r.points, "0"), Num(r.expected, "0")));
            return sb.ToString();
        }

        private static string Num(float v, string format = "0.0") => v.ToString(format, Culture);
        private static string Num(double v) => v.ToString("0.0", Culture);
        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
        private static string Pct(double v) => Mathf.RoundToInt((float)v * 100f) + "%";
    }
}
