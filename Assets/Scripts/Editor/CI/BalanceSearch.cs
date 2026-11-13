using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameLike.Core.Events;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.Systems.Simulation;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Busca automática de balanceamento da RECEITA (GDD 23.14): testa combinações de
    /// unidades-base, preço, curva nota → vendas (expoente, piso, nota de virada) e cauda de
    /// vendas no simulador de economia, com os custos intocados. Entre as combinações que cumprem
    /// mais metas, prefere a que deixa o jogo médio (6,5) com lucro perto de 30% do custo (para não
    /// ficar fácil) e, depois, a que muda menos os valores atuais. As metas de chances são testadas
    /// com a variação de custo por run (−5%, 0, +5%). Não grava nos assets: gera o ajuste em JSON
    /// (formato da tela de Parâmetros) e o relatório; quem aplica é o <see cref="ImportPlaytestSettings"/>.
    /// Uso: <c>-executeMethod GameLike.Editor.CI.BalanceSearch.BatchRun -glout &lt;pasta&gt;</c>.
    /// </summary>
    public static class BalanceSearch
    {
        private const double TargetMediumMargin = 0.30;
        private const string MediumBand = "Média (6,5)";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private sealed class Candidate
        {
            public Dictionary<string, double> values = new Dictionary<string, double>();
            public int passed, total;
            public double margin;
            public double distance;
        }

        public static void BatchRun()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-glout");
            string outFolder = at >= 0 && at < args.Length - 1 ? args[at + 1] : Path.Combine(Application.temporaryCachePath, "balance-search");

            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            string original = JsonUtility.ToJson(loop);
            var report = new StringBuilder();
            var chosen = new Dictionary<string, double>();
            try
            {
                var garage = Search(loop, "Garagem (jogo Pequeno)", s => s.template.phase == CompanyPhase.Garage, new Dictionary<string, double[]>
                {
                    { "smallBaseUnits", new double[] { 20000, 30000, 40000, 60000, 80000, 100000 } },
                    { "smallPrice", new double[] { 15, 20, 25, 30 } },
                    { "salesScoreExponent", new double[] { 1.5, 2, 2.4, 3 } },
                    { "salesFloorShare", new double[] { 0, 0.1, 0.2, 0.3, 0.4, 0.5 } },
                    { "salesScoreThreshold", new double[] { 0, 3, 5, 6 } },
                    { "tailRetentionWorst", new double[] { 0.55, 0.4, 0.25 } },
                    { "tailRetentionBest", new double[] { 0.93, 0.8, 0.65 } },
                    { "salesTailWeeks", new double[] { 26, 16 } },
                }, new[] { 0.95f, 1f, 1.05f }, report);
                Merge(chosen, garage);
                Apply(loop, chosen);

                SearchSize(loop, "Indie · Médio", "mediumBaseUnits", "mediumPrice", chosen, report);
                SearchSize(loop, "Escritório · Grande", "largeBaseUnits", "largePrice", chosen, report);
                SearchSize(loop, "Estúdio · AAA", "aaaBaseUnits", "aaaPrice", chosen, report);
                Apply(loop, chosen);

                var rows = EconomyReport.RunAll();
                EconomyReport.Count(rows, out int passed, out int withGoal);
                report.AppendLine();
                report.AppendLine("RESULTADO FINAL: " + passed + " de " + withGoal + " metas cumpridas.");
                Directory.CreateDirectory(Path.Combine(outFolder, "BalanceOverrides"));
                File.WriteAllText(Path.Combine(outFolder, "BalanceOverrides", "GameLoopBalanceSO.json"), ToJson(chosen), new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(outFolder, "relatorio_com_proposta.md"), EconomyReport.ToMarkdown(rows, "Simulação com a proposta de receita"), new UTF8Encoding(false));
                Debug.Log("GAMELIKE_BALANCE_SEARCH passed=" + passed + " total=" + withGoal);
            }
            finally
            {
                JsonUtility.FromJsonOverwrite(original, loop); // nada fica alterado no asset
            }
            File.WriteAllText(Path.Combine(outFolder, "busca.txt"), report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Busca guiada pela SIMULAÇÃO LONGA (GDD 23.15): mantém a venda de um fracasso na Garagem
        /// (piso × unidades-base do Pequeno) e varia quanto um acerto rende e quanto vendem Médio,
        /// Grande e AAA. Toda combinação tem de manter as metas de chances da Garagem (GDD 23.14);
        /// entre elas, vence a que cumpre mais metas de carreira e, depois, a que muda menos.
        /// Uso: <c>-executeMethod GameLike.Editor.CI.BalanceSearch.BatchCareerSearch -glout &lt;pasta&gt;</c>.
        /// </summary>
        public static void BatchCareerSearch()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-glout");
            string outFolder = at >= 0 && at < args.Length - 1 ? args[at + 1] : Path.Combine(Application.temporaryCachePath, "career-search");
            const int careers = 30;

            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            string original = JsonUtility.ToJson(loop);
            var current = Read(loop, "smallBaseUnits", "salesFloorShare", "salesScoreExponent", "salesScoreThreshold",
                "mediumBaseUnits", "largeBaseUnits", "aaaBaseUnits");
            double flopUnits = current["smallBaseUnits"] * current["salesFloorShare"];
            var profiles = CareerReport.StandardProfiles().FindAll(p => p.name.Contains("equipe enxuta"));
            var chanceScenarios = EconomyReport.StandardScenarios().FindAll(s => s.section == EconomyReport.SectionChances || s.section == EconomyReport.SectionProfit);
            var report = new StringBuilder();
            report.AppendLine("Busca guiada pela simulação longa (" + careers + " carreiras por perfil). Fracasso na Garagem fixo em "
                              + flopUnits.ToString("0", Inv) + " unidades-base de piso.");

            Dictionary<string, double> best = null;
            int bestGoals = -1, evaluated = 0, kept = 0;
            double bestDistance = double.MaxValue;
            string bestMeasured = "";
            try
            {
                var grid = new Dictionary<string, double[]>
                {
                    { "smallBaseUnits", new double[] { 40000, 50000, 60000 } },
                    { "salesScoreThreshold", new double[] { 4, 5, 6 } },
                    { "salesScoreExponent", new double[] { 0.8, 1, 1.1, 1.2, 1.3, 1.4 } },
                    { "mediumBaseUnits", Scale(current["mediumBaseUnits"], 0.75, 1, 1.25, 1.5) },
                    { "largeBaseUnits", Scale(current["largeBaseUnits"], 0.5, 0.6, 0.75, 0.9, 1, 1.25) },
                    { "aaaBaseUnits", Scale(current["aaaBaseUnits"], 1, 1.5) },
                };
                foreach (var combo in Combinations(grid))
                {
                    evaluated++;
                    combo["salesFloorShare"] = Math.Round(flopUnits / combo["smallBaseUnits"], 4);
                    Apply(loop, combo);

                    var chances = new Candidate { values = combo };
                    Evaluate(chanceScenarios, chances, new[] { 1f });
                    if (chances.passed < chances.total) continue;
                    kept++;

                    var results = new List<CareerProfileResult>();
                    int seed = 1;
                    foreach (var profile in profiles) results.Add(CareerReport.RunProfile(profile, careers, seed++ * 1000));
                    var goals = CareerReport.Goals(results);
                    int passed = goals.FindAll(g => g.passed).Count;
                    double distance = Distance(combo, current);
                    if (passed > bestGoals || (passed == bestGoals && distance < bestDistance))
                    {
                        best = combo;
                        bestGoals = passed;
                        bestDistance = distance;
                        var measured = new StringBuilder();
                        foreach (var g in goals) measured.AppendLine("  " + (g.passed ? "ok  " : "NÃO ") + g.text + " — " + g.measured);
                        bestMeasured = measured.ToString();
                    }
                }
            }
            finally
            {
                JsonUtility.FromJsonOverwrite(original, loop);
            }

            report.AppendLine(evaluated + " combinações; " + kept + " mantêm as chances da Garagem e o lucro por jogo de cada fase.");
            report.AppendLine("Atual:     " + Describe(current));
            if (best != null)
            {
                report.AppendLine("Escolhida: " + Describe(best) + "  → metas de carreira " + bestGoals + "/" + 5);
                report.Append(bestMeasured);
                Directory.CreateDirectory(Path.Combine(outFolder, "BalanceOverrides"));
                File.WriteAllText(Path.Combine(outFolder, "BalanceOverrides", "GameLoopBalanceSO.json"), ToJson(best), new UTF8Encoding(false));
            }
            File.WriteAllText(Path.Combine(outFolder, "busca_carreira.txt"), report.ToString(), new UTF8Encoding(false));
            Debug.Log(report.ToString());
            Debug.Log("GAMELIKE_CAREER_SEARCH goals=" + bestGoals);
            EditorApplication.Exit(0);
        }

        private static void SearchSize(GameLoopBalanceSO loop, string scenarioName, string unitsKey, string priceKey,
            Dictionary<string, double> chosen, StringBuilder report)
        {
            var current = Read(loop, unitsKey, priceKey);
            var best = Search(loop, scenarioName, s => s.template.name == scenarioName, new Dictionary<string, double[]>
            {
                { unitsKey, Scale(current[unitsKey], 0.5, 0.75, 1, 1.25, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10, 12, 15, 20, 25, 30) },
                { priceKey, Scale(current[priceKey], 1, 1.25, 1.5, 2) },
            }, new[] { 1f }, report);
            Merge(chosen, best);
            Apply(loop, chosen);
        }

        private static Candidate Search(GameLoopBalanceSO loop, string title, Predicate<ReportScenario> which,
            Dictionary<string, double[]> grid, float[] variations, StringBuilder report)
        {
            var current = Read(loop, new List<string>(grid.Keys).ToArray());
            var scenarios = EconomyReport.StandardScenarios().FindAll(which);
            Candidate best = null;
            int evaluated = 0;
            foreach (var combo in Combinations(grid))
            {
                evaluated++;
                Apply(loop, combo);
                var candidate = new Candidate { values = combo, distance = Distance(combo, current) };
                Evaluate(scenarios, candidate, variations);
                if (Better(candidate, best)) best = candidate;
            }
            report.AppendLine();
            report.AppendLine("== " + title + ": " + evaluated + " combinações testadas ==");
            report.AppendLine("Atual:     " + Describe(current));
            if (best != null)
                report.AppendLine("Escolhida: " + Describe(best.values) + "  → metas " + best.passed + "/" + best.total
                                  + " · jogo médio lucra " + (best.margin * 100).ToString("0", Inv) + "% do custo");
            return best;
        }

        private static double[] Scale(double basis, params double[] factors)
        {
            var values = new double[factors.Length];
            for (int i = 0; i < factors.Length; i++) values[i] = Math.Round(basis * factors[i]);
            return values;
        }

        // ------------------------------------------------------------------ avaliação

        private static void Evaluate(List<ReportScenario> scenarios, Candidate candidate, float[] variations)
        {
            candidate.margin = double.NaN;
            foreach (var item in scenarios)
            {
                if (item.informative) continue;
                foreach (var band in item.bands)
                {
                    if (band.HasGoal)
                    {
                        // metas de sobrevivência dependem do caixa: testadas com a variação de custo
                        var runs = item.template.ignoreBankruptcy ? new[] { 1f } : variations;
                        foreach (float variation in runs)
                        {
                            var result = EconomySimulator.Run(Prepare(item.template, band, variation));
                            candidate.total++;
                            if (Passes(band, result)) candidate.passed++;
                            if (band.label == MediumBand && variation == 1f) candidate.margin = Margin(result);
                        }
                    }
                }
            }
        }

        private static SimScenario Prepare(SimScenario template, ScoreBand band, float variation)
        {
            var scenario = Clone(template);
            scenario.scores = band.scores;
            scenario.costVariation = variation;
            return scenario;
        }

        /// <summary>Lucro médio por jogo dividido pelo custo médio por jogo.</summary>
        private static double Margin(SimResult result)
        {
            long profit = 0, cost = 0;
            foreach (var g in result.games) { profit += g.ForecastProfit; cost += g.devCost; }
            return cost > 0 ? profit / (double)cost : double.NaN;
        }

        /// <summary>Mesmas metas do relatório (<see cref="EconomyReport"/>), aplicadas a um resultado.</summary>
        private static bool Passes(ScoreBand band, SimResult result)
        {
            if (band.mustLaunch > 0 && result.games.Count < band.mustLaunch) return false;
            if (band.shouldGoBankrupt == true && !result.Bankrupt) return false;
            if (band.shouldGoBankrupt == false && result.Bankrupt) return false;
            if (band.mustGrow && result.Net <= 0) return false;
            if (band.shouldProfit.HasValue)
            {
                long profit = 0;
                foreach (var g in result.games) profit += g.ForecastProfit;
                if (result.games.Count > 0) profit /= result.games.Count;
                if (band.shouldProfit.Value && profit <= 0) return false;
                if (!band.shouldProfit.Value && profit > 0) return false;
            }
            return true;
        }

        /// <summary>Mais metas cumpridas; empate → jogo médio com lucro mais perto de 30%; empate → menos mudança.</summary>
        private static bool Better(Candidate candidate, Candidate best)
        {
            if (best == null) return true;
            if (candidate.passed != best.passed) return candidate.passed > best.passed;
            double a = MarginGap(candidate), b = MarginGap(best);
            if (Math.Abs(a - b) > 0.05) return a < b;
            return candidate.distance < best.distance;
        }

        private static double MarginGap(Candidate c) => double.IsNaN(c.margin) ? 99 : Math.Abs(c.margin - TargetMediumMargin);

        /// <summary>Quanto a combinação se afasta dos valores atuais (soma das variações relativas).</summary>
        private static double Distance(Dictionary<string, double> combo, Dictionary<string, double> current)
        {
            double sum = 0;
            foreach (var pair in combo)
            {
                double old = current.TryGetValue(pair.Key, out double v) ? v : 0;
                sum += old > 0 && pair.Value > 0 ? Math.Abs(Math.Log(pair.Value / old)) : Math.Abs(pair.Value - old);
            }
            return sum;
        }

        private static SimScenario Clone(SimScenario s) => new SimScenario
        {
            name = s.name, phase = s.phase, size = s.size, employees = s.employees,
            employeeSeniority = s.employeeSeniority, startingCash = s.startingCash,
            licenseCost = s.licenseCost, lightEngineModules = s.lightEngineModules,
            costVariation = s.costVariation, maxWeeks = s.maxWeeks, ignoreBankruptcy = s.ignoreBankruptcy,
            stopAtLastLaunch = s.stopAtLastLaunch
        };

        // ------------------------------------------------------------------ valores

        private static IEnumerable<Dictionary<string, double>> Combinations(Dictionary<string, double[]> grid)
        {
            var keys = new List<string>(grid.Keys);
            var index = new int[keys.Count];
            while (true)
            {
                var combo = new Dictionary<string, double>();
                for (int i = 0; i < keys.Count; i++) combo[keys[i]] = grid[keys[i]][index[i]];
                yield return combo;
                int k = 0;
                while (k < keys.Count && ++index[k] >= grid[keys[k]].Length) { index[k] = 0; k++; }
                if (k == keys.Count) yield break;
            }
        }

        private static void Merge(Dictionary<string, double> into, Candidate from)
        {
            if (from == null) return;
            foreach (var pair in from.values) into[pair.Key] = pair.Value;
        }

        private static void Apply(GameLoopBalanceSO loop, Dictionary<string, double> values)
        {
            if (values.Count > 0) JsonUtility.FromJsonOverwrite(ToJson(values), loop);
        }

        private static Dictionary<string, double> Read(GameLoopBalanceSO loop, params string[] keys)
        {
            var so = new SerializedObject(loop);
            var result = new Dictionary<string, double>();
            foreach (string key in keys)
            {
                var p = so.FindProperty(key);
                if (p == null) continue;
                result[key] = p.propertyType == SerializedPropertyType.Float ? p.floatValue : p.longValue;
            }
            return result;
        }

        private static string ToJson(Dictionary<string, double> values)
        {
            var sb = new StringBuilder("{\n");
            int i = 0;
            foreach (var pair in values)
                sb.Append("    \"").Append(pair.Key).Append("\": ").Append(pair.Value.ToString("0.####", Inv)).Append(++i < values.Count ? ",\n" : "\n");
            return sb.Append("}\n").ToString();
        }

        private static string Describe(Dictionary<string, double> values)
        {
            var parts = new List<string>();
            foreach (var pair in values) parts.Add(pair.Key + "=" + pair.Value.ToString("0.###", Inv));
            return string.Join(", ", parts);
        }
    }
}
