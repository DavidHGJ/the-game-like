using System.Globalization;
using GameLike.Core.Events;
using GameLike.Systems.Simulation;
using GameLike.UI.Gallery;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Tuning
{
    /// <summary>
    /// Painel "Simulação de carreira" da tela de Parâmetros: roda a simulação longa
    /// (<see cref="CareerReport"/>) com os valores da tela e mostra, por perfil de jogador, até
    /// onde ele chega em cada uma das 5 runs e se as metas de progressão foram cumpridas.
    /// </summary>
    internal static class TuningCareerView
    {
        private const int Careers = 40;
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");
        private static readonly int[] Widths = { 60, 90, 100, 90, 80, 90, 70, 90, 150 };

        public static VisualElement Build()
        {
            var root = new VisualElement();
            Fill(root);
            return root;
        }

        private static void Fill(VisualElement root)
        {
            root.Clear();
            var results = CareerReport.RunAll(Careers);
            var goals = CareerReport.Goals(results);
            int passed = goals.FindAll(g => g.passed).Count;

            var head = Kit.Row();
            head.Add(Kit.Text("Simulação de carreira", 20, Kit.Ink, true));
            head.Add(Kit.Gap(14));
            head.Add(Kit.Chip(passed + " de " + goals.Count + " metas", passed == goals.Count ? Kit.Teal : Kit.Orange, true,
                passed == goals.Count ? Kit.IcCheck : Kit.IcWarn));
            head.Add(Kit.Spacer());
            head.Add(Kit.Button("Rodar de novo", Kit.IcUndo, Kit.Sand, Kit.Ink, () => Fill(root), 34));
            root.Add(head);

            var help = Kit.Wrap(
                Careers + " carreiras de " + CareerReport.RunsPerCareer + " runs por perfil, com os valores desta tela. O jogador simulado faz jogos " +
                "sem parar no maior tamanho que a fase usa e que já pesquisou, expande quando o valuation libera e tem caixa, contrata conforme a " +
                "estratégia e vende a empresa antes de falir (o dinheiro vira o capital da run seguinte).", 12, Kit.Soft);
            help.style.marginTop = 8;
            root.Add(help);

            var goalsCard = Kit.Tile(12);
            goalsCard.style.marginTop = 10;
            goalsCard.Add(Kit.Caps("Metas propostas — avança, mas não tudo", 11, Kit.Purple));
            foreach (var g in goals)
            {
                var line = Kit.Row();
                line.style.marginTop = 4;
                line.Add(Kit.Text(g.passed ? "✓" : "✗", 13, g.passed ? Kit.Teal : Kit.Red, true));
                line.Add(Kit.Gap(8));
                var text = Kit.Wrap(g.text + " — medido: " + g.measured, 12, Kit.Ink);
                text.style.flexShrink = 1;
                line.Add(text);
                goalsCard.Add(line);
            }
            root.Add(goalsCard);

            foreach (var r in results)
            {
                var card = Kit.Tile(12);
                card.style.marginTop = 12;
                var title = Kit.Row();
                title.Add(Kit.Text(r.profile.name, 15, Kit.Ink, true));
                title.Add(Kit.Gap(10));
                title.Add(Kit.Text("nota " + r.profile.skillMean.ToString("0.0", Culture) + " ± " + r.profile.skillSpread.ToString("0.0", Culture)
                                   + " · Escritório em alguma run: " + CareerReport.Pct(r.officeEver) + " · Estúdio: " + CareerReport.Pct(r.studioEver), 12, Kit.Soft));
                card.Add(title);
                card.Add(Line(true, "Run", "Indie", "Escritório", "Estúdio", "Faliu", "Semanas", "Jogos", "Acertos", "Leva p/ próxima"));
                foreach (var s in r.runs)
                {
                    card.Add(Line(false, s.runIndex.ToString(), CareerReport.Pct(s.ReachedShare(CompanyPhase.Indie)),
                        CareerReport.Pct(s.ReachedShare(CompanyPhase.Office)), CareerReport.Pct(s.ReachedShare(CompanyPhase.Studio)),
                        CareerReport.Pct(s.BankruptShare), s.medianWeeks.ToString(), s.medianGames.ToString("0", Culture),
                        CareerReport.Pct(s.hitRate), EconomyReport.Money(s.medianPayout)));
                }
                root.Add(card);
            }
        }

        private static VisualElement Line(bool header, params string[] cells)
        {
            var line = Kit.Row();
            line.style.paddingTop = header ? 8 : 3; line.style.paddingBottom = 3;
            if (!header) { line.style.borderTopWidth = 1; line.style.borderTopColor = Kit.Track; }
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = Kit.Text(cells[i], header ? 11 : 13, header ? Kit.Soft : Kit.Ink, header);
                cell.style.width = Widths[i]; cell.style.flexShrink = 0;
                cell.style.unityTextAlign = i == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                cell.style.paddingRight = 10;
                line.Add(cell);
            }
            return line;
        }
    }
}
