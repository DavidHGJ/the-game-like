using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State.Staff;
using UnityEngine;

namespace GameLike.Systems.Simulation
{
    /// <summary>Uma faixa de nota (ou sequência de notas) testada e a meta que ela precisa cumprir.</summary>
    public sealed class ScoreBand
    {
        public string label;
        public float[] scores;
        /// <summary>Lucro por jogo esperado: true = lucro, false = prejuízo, null = sem meta.</summary>
        public bool? shouldProfit;
        /// <summary>Falência esperada: true = tem de falir, false = não pode falir, null = sem meta.</summary>
        public bool? shouldGoBankrupt;
        /// <summary>Quantos jogos têm de chegar a ser lançados (as "chances" do jogador).</summary>
        public int mustLaunch;
        /// <summary>true = a run inteira tem de terminar com mais dinheiro do que começou.</summary>
        public bool mustGrow;

        /// <summary>true se a faixa tem alguma meta (senão é só informativa).</summary>
        public bool HasGoal => shouldProfit.HasValue || shouldGoBankrupt.HasValue || mustLaunch > 0 || mustGrow;
    }

    /// <summary>Uma linha do relatório: um cenário numa faixa de nota, com o veredito.</summary>
    public sealed class ReportRow
    {
        public string scenario;
        public string section;
        public ScoreBand band;
        public SimResult result;
        public bool passed;
        /// <summary>true = linha sem meta (só para consulta).</summary>
        public bool informative;
        public string verdict;

        public long AvgRevenue => Average(g => g.forecastRevenue);
        public long AvgCost => Average(g => g.devCost);
        public long AvgProfit => Average(g => g.ForecastProfit);
        public long AvgUnits => Average(g => g.forecastUnits);

        /// <summary>true se o cenário mede sobrevivência (com falência); false se mede só se os jogos se pagam.</summary>
        public bool MeasuresSurvival => !result.scenario.ignoreBankruptcy;

        private long Average(Func<SimGame, long> pick)
        {
            if (result.games.Count == 0) return 0;
            long sum = 0;
            foreach (var g in result.games) sum += pick(g);
            return sum / result.games.Count;
        }
    }

    /// <summary>Um cenário da bateria com as faixas que ele roda.</summary>
    public sealed class ReportScenario
    {
        public string section;
        public SimScenario template;
        public ScoreBand[] bands;
        public bool informative;
    }

    /// <summary>
    /// Bateria padrão de simulações da economia (GDD 23.14), em três partes:
    /// (1) Garagem — as "chances" do jogador: com o dinheiro inicial real e a regra de falência,
    /// dois fracassos seguidos quebram a empresa (mas só depois do 2º lançamento) e um fracasso
    /// seguido de acerto sobrevive; (2) lucro por jogo no tamanho principal de cada fase (sem
    /// falência, caixa começando em zero); (3) outros tamanhos, só informativos.
    /// </summary>
    public static class EconomyReport
    {
        public const string SectionChances = "Garagem — chances do jogador";
        public const string SectionProfit = "Lucro por jogo no tamanho principal da fase";
        public const string SectionInfo = "Outros tamanhos (informativo, sem meta)";
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

        /// <summary>Faixas de lucro por jogo (3 jogos seguidos com a mesma nota).</summary>
        public static readonly ScoreBand[] ProfitBands =
        {
            new ScoreBand { label = "Ruim (3)", scores = Repeat(3f), shouldProfit = false },
            new ScoreBand { label = "Fraca (5)", scores = Repeat(5f) },
            new ScoreBand { label = "Média (6,5)", scores = Repeat(6.5f), shouldProfit = true },
            new ScoreBand { label = "Boa (8)", scores = Repeat(8f), shouldProfit = true },
            new ScoreBand { label = "Ótima (9,5)", scores = Repeat(9.5f), shouldProfit = true },
            new ScoreBand { label = "Variada (5 · 7 · 6)", scores = new[] { 5f, 7f, 6f } },
        };

        /// <summary>
        /// Sequências de jogos da Garagem que testam as chances do jogador (GDD 23.14). O jogador
        /// continua tentando: a simulação vai até o lançamento do último jogo da sequência.
        /// Dois fracassos seguidos têm de quebrar a empresa antes do 3º lançamento (mas depois do 2º).
        /// </summary>
        public static readonly ScoreBand[] ChanceBands =
        {
            new ScoreBand { label = "Fracasso, fracasso, tenta de novo (3 · 3 · 8)", scores = new[] { 3f, 3f, 8f }, shouldGoBankrupt = true, mustLaunch = 2 },
            new ScoreBand { label = "Fraco, fraco, tenta de novo (5 · 5 · 8)", scores = new[] { 5f, 5f, 8f }, shouldGoBankrupt = true, mustLaunch = 2 },
            new ScoreBand { label = "Fracasso, acerto, segue (3 · 8 · 6,5 · 6,5)", scores = new[] { 3f, 8f, 6.5f, 6.5f }, shouldGoBankrupt = false, mustLaunch = 4 },
            new ScoreBand { label = "Fraco, acerto, segue (5 · 7 · 6,5 · 6,5)", scores = new[] { 5f, 7f, 6.5f, 6.5f }, shouldGoBankrupt = false, mustLaunch = 4 },
            new ScoreBand { label = "Acerta de cara (8 · 8 · 6,5)", scores = new[] { 8f, 8f, 6.5f }, shouldGoBankrupt = false, mustLaunch = 3, mustGrow = true },
            new ScoreBand { label = "Só médios (6,5 ×4)", scores = new[] { 6.5f, 6.5f, 6.5f, 6.5f } },
        };

        private static float[] Repeat(float score) => new[] { score, score, score };

        /// <summary>A bateria completa, com equipe e caixa coerentes com o balanceamento atual.</summary>
        public static List<ReportScenario> StandardScenarios()
        {
            return new List<ReportScenario>
            {
                new ReportScenario
                {
                    section = SectionChances,
                    template = Scenario("Garagem · Pequeno (começo real)", CompanyPhase.Garage, ProjectSize.Small, Seniority.Junior, true),
                    bands = ChanceBands
                },
                Profit("Garagem · Pequeno", CompanyPhase.Garage, ProjectSize.Small, Seniority.Junior, false),
                Profit("Indie · Médio", CompanyPhase.Indie, ProjectSize.Medium, Seniority.Junior, false),
                Profit("Escritório · Grande", CompanyPhase.Office, ProjectSize.Large, Seniority.Mid, false),
                Profit("Estúdio · AAA", CompanyPhase.Studio, ProjectSize.AAA, Seniority.Senior, false),
                Profit("Indie · Pequeno", CompanyPhase.Indie, ProjectSize.Small, Seniority.Junior, true),
                Profit("Escritório · Médio", CompanyPhase.Office, ProjectSize.Medium, Seniority.Mid, true),
                Profit("Estúdio · Grande", CompanyPhase.Studio, ProjectSize.Large, Seniority.Mid, true),
            };
        }

        private static ReportScenario Profit(string name, CompanyPhase phase, ProjectSize size, Seniority seniority, bool informative) =>
            new ReportScenario
            {
                section = informative ? SectionInfo : SectionProfit,
                template = Scenario(name, phase, size, seniority, false),
                bands = ProfitBands,
                informative = informative
            };

        /// <summary>
        /// Monta o cenário: equipe = o que o tamanho pede, limitado ao que cabe na fase (menos o
        /// fundador). Com <paramref name="realStart"/> vale o dinheiro inicial do jogo e a regra de
        /// falência; sem ele a falência é ignorada e o caixa começa em zero (o caixa mínimo vira o
        /// capital que a fase exige).
        /// </summary>
        private static SimScenario Scenario(string name, CompanyPhase phase, ProjectSize size, Seniority seniority, bool realStart)
        {
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            int team = Mathf.Min(loop.TeamFor(size), loop.MaxTeamForPhase(phase));
            return new SimScenario
            {
                name = name,
                phase = phase,
                size = size,
                employees = Mathf.Max(0, team - 1),
                employeeSeniority = seniority,
                startingCash = realStart ? -1 : 0,
                ignoreBankruptcy = !realStart,
                stopAtLastLaunch = realStart
            };
        }

        /// <summary>Roda a bateria inteira com os valores atuais do balanceamento.</summary>
        public static List<ReportRow> RunAll(SimMarketing marketing = SimMarketing.None)
        {
            var rows = new List<ReportRow>();
            foreach (var item in StandardScenarios())
            {
                foreach (var band in item.bands)
                {
                    var scenario = Copy(item.template);
                    scenario.scores = band.scores;
                    scenario.marketing = marketing;
                    rows.Add(Judge(item, band, EconomySimulator.Run(scenario)));
                }
            }
            return rows;
        }

        /// <summary>Quantas linhas com meta passaram, e quantas linhas têm meta.</summary>
        public static void Count(List<ReportRow> rows, out int passed, out int withGoal)
        {
            passed = withGoal = 0;
            foreach (var row in rows)
            {
                if (row.informative) continue;
                withGoal++;
                if (row.passed) passed++;
            }
        }

        private static SimScenario Copy(SimScenario s) => new SimScenario
        {
            name = s.name, phase = s.phase, size = s.size, employees = s.employees,
            employeeSeniority = s.employeeSeniority, startingCash = s.startingCash,
            licenseCost = s.licenseCost, lightEngineModules = s.lightEngineModules,
            costVariation = s.costVariation, maxWeeks = s.maxWeeks, ignoreBankruptcy = s.ignoreBankruptcy,
            stopAtLastLaunch = s.stopAtLastLaunch
        };

        private static ReportRow Judge(ReportScenario item, ScoreBand band, SimResult result)
        {
            var row = new ReportRow
            {
                scenario = item.template.name, section = item.section, band = band, result = result,
                informative = item.informative || !band.HasGoal
            };
            var problems = new List<string>();
            if (!row.informative)
            {
                if (band.mustLaunch > 0 && result.games.Count < band.mustLaunch)
                    problems.Add("faliu na semana " + result.bankruptWeek + " antes de lançar o " + (result.games.Count + 1) + "º jogo");
                else if (band.shouldGoBankrupt == true && !result.Bankrupt) problems.Add("deveria falir e não faliu");
                else if (band.shouldGoBankrupt == false && result.Bankrupt) problems.Add("faliu na semana " + result.bankruptWeek);
                if (band.mustGrow && result.Net <= 0) problems.Add("a empresa não cresceu");
                if (band.shouldProfit == true && row.AvgProfit <= 0) problems.Add("deveria dar lucro");
                if (band.shouldProfit == false && row.AvgProfit > 0) problems.Add("jogo ruim não deveria dar lucro");
            }
            row.passed = problems.Count == 0;

            var notes = new List<string>();
            if (row.informative) notes.Add("—");
            else if (row.passed) notes.Add("ok");
            else notes.AddRange(problems);
            if (row.passed && row.MeasuresSurvival)
                notes.Add(result.Bankrupt ? "faliu na sem. " + result.bankruptWeek : "terminou com " + Money(result.finalCash));
            row.verdict = string.Join(" · ", notes);
            return row;
        }

        /// <summary>Formata dinheiro como no jogo (R$ 12.500 / -R$ 3.000).</summary>
        public static string Money(long value) =>
            (value < 0 ? "-R$ " : "R$ ") + Math.Abs(value).ToString("N0", Culture);

        /// <summary>
        /// Valor da coluna de caixa: nos cenários de sobrevivência, o menor caixa da run; nos de
        /// lucratividade (que começam do zero), o capital que a fase exige até os jogos se pagarem.
        /// </summary>
        public static string CashColumn(ReportRow row) =>
            row.MeasuresSurvival ? Money(row.result.minCash) : Money(Math.Max(0, -row.result.minCash));

        /// <summary>Relatório em Markdown (seções e tabela por cenário), para o editor e para documentação.</summary>
        public static string ToMarkdown(List<ReportRow> rows, string title)
        {
            var sb = new StringBuilder();
            Count(rows, out int passed, out int withGoal);
            sb.AppendLine("# " + title);
            sb.AppendLine();
            sb.AppendLine("Metas cumpridas: **" + passed + " de " + withGoal + "** (GDD 23.14). Valores atuais do balanceamento.");
            sb.AppendLine();
            sb.AppendLine("- **Chances (Garagem):** dinheiro inicial real e regra de falência; o jogador continua tentando e a simulação vai até o último lançamento. Dois fracassos seguidos têm de quebrar a empresa antes do 3º lançamento (mas depois do 2º); fracasso + acerto tem de seguir lançando jogos.");
            sb.AppendLine("- **Lucro por jogo:** sem falência e caixa começando em zero; a coluna de caixa vira o capital que a fase exige. Nota ruim dá prejuízo; média, boa e ótima dão lucro.");
            sb.AppendLine("- Receita e unidades são a venda completa prevista de cada jogo. Lucro/jogo = receita prevista − tudo o que a empresa gastou enquanto o jogo era produzido.");
            sb.AppendLine();
            string section = null;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.section != section)
                {
                    section = row.section;
                    sb.AppendLine("# " + section);
                    sb.AppendLine();
                }
                if (i == 0 || rows[i - 1].scenario != row.scenario || rows[i - 1].section != row.section)
                {
                    var s = row.result.scenario;
                    var loop = BalanceProvider.Get<GameLoopBalanceSO>();
                    sb.AppendLine("## " + row.scenario);
                    sb.AppendLine();
                    sb.AppendLine("Equipe: fundador + " + s.employees + " · jogos de " + EconomySimulator.DevelopmentWeeks(s.size, loop) + " semanas"
                                  + (row.MeasuresSurvival ? " · caixa inicial " + Money(row.result.startingCash) : " · começa com caixa zero"));
                    sb.AppendLine();
                    sb.AppendLine("| Notas | Unid. previstas/jogo | Receita/jogo | Custo/jogo | Lucro/jogo | " + (row.MeasuresSurvival ? "Caixa mínimo" : "Capital necessário") + " | Resultado |");
                    sb.AppendLine("|---|---:|---:|---:|---:|---:|---|");
                }
                string mark = row.informative ? "" : row.passed ? "✅ " : "❌ ";
                sb.AppendLine("| " + row.band.label + " | " + row.AvgUnits.ToString("N0", Culture) + " | " + Money(row.AvgRevenue)
                              + " | " + Money(row.AvgCost) + " | " + Money(row.AvgProfit) + " | " + CashColumn(row) + " | " + mark + row.verdict + " |");
                bool lastOfScenario = i == rows.Count - 1 || rows[i + 1].scenario != row.scenario || rows[i + 1].section != row.section;
                if (lastOfScenario) sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
