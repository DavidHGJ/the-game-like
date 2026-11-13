using System.Collections.Generic;
using System.Globalization;
using GameLike.Systems.Simulation;
using GameLike.UI.Gallery;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Tuning
{
    /// <summary>
    /// Painel "Simulação da economia" da tela de Parâmetros: roda a bateria padrão
    /// (<see cref="EconomyReport"/>) com os valores que estão na tela agora e mostra, por fase e
    /// faixa de nota, quanto cada jogo vende, custa e lucra, e se a meta foi cumprida.
    /// </summary>
    internal static class TuningSimulationView
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly string[] MarketingNames = { "Sem marketing", "Anúncio", "Completo" };
        private static readonly int[] Widths = { 210, 110, 125, 125, 125, 140 };
        private static SimMarketing _marketing = SimMarketing.None;

        /// <summary>Monta o painel já com a simulação rodada.</summary>
        public static VisualElement Build()
        {
            var root = new VisualElement();
            Fill(root);
            return root;
        }

        private static void Fill(VisualElement root)
        {
            root.Clear();
            var rows = EconomyReport.RunAll(_marketing);
            EconomyReport.Count(rows, out int passed, out int withGoal);

            var head = Kit.Row();
            head.Add(Kit.Text("Simulação da economia", 20, Kit.Ink, true));
            head.Add(Kit.Gap(14));
            head.Add(Kit.Chip(passed + " de " + withGoal + " metas cumpridas", passed == withGoal ? Kit.Teal : Kit.Orange, true,
                passed == withGoal ? Kit.IcCheck : Kit.IcWarn));
            head.Add(Kit.Spacer());
            head.Add(Widgets.Segmented(MarketingNames, (int)_marketing, i => { _marketing = (SimMarketing)i; Fill(root); }));
            head.Add(Kit.Gap(10));
            head.Add(Kit.Button("Rodar de novo", Kit.IcUndo, Kit.Sand, Kit.Ink, () => Fill(root), 34));
            root.Add(head);

            var help = Kit.Wrap(
                "Usa os valores que estão nesta tela agora (mude um número e rode de novo). Metas do GDD 23.14. " +
                "Chances (Garagem): dinheiro inicial real e regra de falência — dois fracassos seguidos têm de quebrar a empresa, " +
                "mas só depois do 2º lançamento; fracasso + acerto tem de sobreviver. " +
                "Lucro por jogo: começa do zero e sem falência — nota ruim dá prejuízo; média, boa e ótima dão lucro. " +
                "Lucro/jogo = venda completa prevista − tudo o que a empresa gastou enquanto o jogo era feito.", 12, Kit.Soft);
            help.style.marginTop = 8; help.style.marginBottom = 4;
            root.Add(help);

            string scenario = null, section = null;
            VisualElement card = null;
            foreach (var row in rows)
            {
                if (row.section != section)
                {
                    section = row.section;
                    var caption = Kit.Caps(section, 12, Kit.Purple);
                    caption.style.marginTop = 18;
                    root.Add(caption);
                    scenario = null;
                }
                if (row.scenario != scenario)
                {
                    scenario = row.scenario;
                    card = Kit.Tile(12);
                    card.style.marginTop = 12;
                    var s = row.result.scenario;
                    var title = Kit.Row();
                    title.Add(Kit.Text(scenario, 15, Kit.Ink, true));
                    title.Add(Kit.Gap(10));
                    title.Add(Kit.Text("fundador + " + s.employees + " · " + (row.MeasuresSurvival
                        ? "caixa inicial " + EconomyReport.Money(row.result.startingCash)
                        : "começa do zero"), 12, Kit.Soft));
                    card.Add(title);
                    card.Add(Line(true, "Notas", "Unid./jogo", "Receita/jogo", "Custo/jogo", "Lucro/jogo",
                        row.MeasuresSurvival ? "Caixa mínimo" : "Capital necessário", "Resultado", Kit.Soft, Kit.Soft));
                    root.Add(card);
                }

                string verdict = (row.informative ? "" : row.passed ? "✓ " : "✗ ") + row.verdict;
                card.Add(Line(false, row.band.label, row.AvgUnits.ToString("N0", Culture), EconomyReport.Money(row.AvgRevenue),
                    EconomyReport.Money(row.AvgCost), EconomyReport.Money(row.AvgProfit), EconomyReport.CashColumn(row),
                    verdict, row.AvgProfit >= 0 ? Kit.Cash : Kit.Red, row.informative ? Kit.Soft : row.passed ? Kit.Teal : Kit.Red));
            }
        }

        private static VisualElement Line(bool header, string score, string units, string revenue, string cost, string profit,
            string cash, string verdict, Color profitColor, Color verdictColor)
        {
            var line = Kit.Row();
            line.style.paddingTop = header ? 8 : 4; line.style.paddingBottom = 4;
            if (!header) { line.style.borderTopWidth = 1; line.style.borderTopColor = Kit.Track; }
            int size = header ? 11 : 13;
            string[] cells = { score, units, revenue, cost, profit, cash };
            for (int i = 0; i < cells.Length; i++)
            {
                Color color = header ? Kit.Soft : i == 4 ? profitColor : Kit.Ink;
                var cell = Kit.Text(cells[i], size, color, header || i == 4);
                cell.style.width = Widths[i]; cell.style.flexShrink = 0;
                cell.style.unityTextAlign = i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                cell.style.paddingRight = 12;
                line.Add(cell);
            }
            var last = Kit.Wrap(verdict, size, header ? Kit.Soft : verdictColor, true);
            last.style.flexGrow = 1; last.style.flexShrink = 1; last.style.marginLeft = 8;
            line.Add(last);
            return line;
        }
    }
}
