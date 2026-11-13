using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>
    /// Moldura comum dos painéis de gestão (GDD 16.2): janela grande com trilho lateral de
    /// navegação entre Financeiro / Projetos / RH / Mercado / P&D / Relações / Fundador / Legado.
    /// </summary>
    internal abstract class ManagementScreen : GalleryScreen
    {
        private static readonly (string name, string icon, Color color)[] Panels =
        {
            ("Financeiro", Kit.IcChartLine, Kit.Cash), ("Projetos", Kit.IcGamepad, Kit.Blue),
            ("RH", Kit.IcUsers, Kit.Orange), ("Mercado", Kit.IcGlobe, Kit.Purple),
            ("P&D", Kit.IcFlask, Kit.Teal), ("Relações", Kit.IcHandshake, Kit.Pink),
            ("Fundador", Kit.IcUserTie, Kit.Sky), ("Legado", Kit.IcHourglass, Kit.Gold)
        };

        protected abstract int PanelIndex { get; }

        /// <summary>Chamado ao clicar em outro painel no trilho lateral (jogo real).</summary>
        public System.Action<int> OnNavigate { get; set; }
        protected Color Accent => Panels[PanelIndex].color;
        protected VisualElement Header { get; private set; }

        /// <summary>Monta a moldura e devolve a área de conteúdo do painel.</summary>
        protected VisualElement Shell(string subtitle)
        {
            Scrim();
            var center = Kit.Centered();
            Root.Add(center);

            var window = Kit.Card(1400, 20);
            window.style.height = 860; Kit.Pad(window, 0, 0);
            window.style.borderBottomWidth = 6; window.style.overflow = Overflow.Hidden;
            window.style.flexDirection = FlexDirection.Row;
            center.Add(window);
            Animate(0.05f, 0.45f, k =>
            {
                window.style.opacity = Mathf.Clamp01(k * 1.6f);
                float s = 0.92f + 0.08f * k;
                window.style.scale = new Scale(new Vector2(s, s));
                window.style.translate = new Translate(0, (1f - k) * 26f, 0);
            }, OutBack);

            var rail = Kit.Col();
            rail.style.width = 96; rail.style.flexShrink = 0; rail.style.backgroundColor = Kit.Sand;
            rail.style.alignItems = Align.Center; rail.style.paddingTop = 16;
            for (int i = 0; i < Panels.Length; i++)
            {
                bool on = i == PanelIndex;
                var item = Kit.Col(); item.style.alignItems = Align.Center; item.style.marginBottom = 12;
                var b = Kit.Badge(Panels[i].icon, on ? Panels[i].color : Kit.Paper, 46, on ? Color.white : Kit.Soft);
                Kit.Hover(b, 1.12f, 0.92f);
                int panel = i;
                b.RegisterCallback<ClickEvent>(_ => OnNavigate?.Invoke(panel));
                item.Add(b);
                var l = Kit.Text(Panels[i].name, 10, on ? Kit.Ink : Kit.Soft, on); l.style.marginTop = 3;
                item.Add(l);
                rail.Add(item);
                int idx = i;
                Animate(0.12f + idx * 0.04f, 0.35f, k =>
                {
                    item.style.opacity = k;
                    item.style.translate = new Translate(-14f * (1f - k), 0, 0);
                });
            }
            window.Add(rail);

            var main = Kit.Col(); main.style.flexGrow = 1; Kit.Pad(main, 20, 26);
            window.Add(main);

            Header = Kit.Row(); Header.style.marginBottom = 16;
            var titles = Kit.Col();
            titles.Add(Kit.Text(Panels[PanelIndex].name, 26, Kit.Ink, true));
            titles.Add(Kit.Text(subtitle, 13, Kit.Soft));
            Header.Add(titles);
            Header.Add(Kit.Spacer());
            main.Add(Header);
            var close = Kit.RoundButton(Kit.IcClose, Kit.Sand, Kit.Soft, 34, RequestClose);
            close.style.position = Position.Absolute; close.style.top = 18; close.style.right = 20;
            main.Add(close);

            var content = Kit.Col(); content.style.flexGrow = 1;
            main.Add(content);
            ResetCascade(0.25f);
            return content;
        }

        private string[] _tabNames;
        private System.Action<int> _tabShow;
        private VisualElement _tabEl;

        /// <summary>Adiciona abas ao cabeçalho do painel.</summary>
        protected void AddTabs(string[] names, System.Action<int> show)
        {
            _tabNames = names; _tabShow = show;
            _tabEl = Widgets.Segmented(names, 0, show, Accent);
            Header.Add(_tabEl);
            Header.Add(Kit.Gap(54));
        }

        /// <summary>Seleciona uma aba por código (mantém o destaque sincronizado).</summary>
        protected void SelectTab(int index)
        {
            int at = Header.IndexOf(_tabEl);
            Header.RemoveAt(at);
            _tabEl = Widgets.Segmented(_tabNames, index, _tabShow, Accent);
            Header.Insert(at, _tabEl);
            _tabShow(index);
        }

        protected static VisualElement StatCard(string icon, Color color, string caption, out Label value, string note = null, Color? noteColor = null)
        {
            var c = Kit.Tile(14); c.style.flexGrow = 1; c.style.flexBasis = 0; c.style.marginRight = 12; Kit.Pad(c, 14, 16);
            var r = Kit.Row();
            r.Add(Kit.Badge(icon, Kit.A(color, 0.16f), 32, color));
            var cap = Kit.Text(caption, 12, Kit.Soft, true); cap.style.marginLeft = 10;
            r.Add(cap);
            c.Add(r);
            value = Kit.Text("", 24, Kit.Ink, true); value.style.marginTop = 6;
            c.Add(value);
            if (note != null) c.Add(Kit.Text(note, 11, noteColor ?? Kit.Soft, true));
            return c;
        }

        protected static Label HeadCell(string text, float width, bool grow = false)
        {
            var l = Kit.Caps(text, 10, Kit.Soft, 1f);
            if (grow) { l.style.flexGrow = 1; l.style.flexBasis = 0; } else l.style.width = width;
            return l;
        }
    }

    /// <summary>Painel Financeiro (GDD 4.2): gráfico histórico de valuation, rivais, despesas e fatores.</summary>
    internal sealed class FinanceScreen : ManagementScreen
    {
        private static readonly float[] Mine = { 50, 58, 55, 70, 92, 88, 120, 150, 142, 190, 240, 228, 300, 372, 412 };
        private static readonly float[] RivalA = { 80, 95, 110, 118, 140, 170, 200, 215, 250, 262, 300, 340, 352, 390, 430 };
        private static readonly float[] RivalB = { 30, 34, 40, 52, 60, 58, 75, 96, 110, 104, 130, 150, 171, 180, 205 };
        private const float MaxValue = 520f, Threshold = 500f;

        private VisualElement _chart;
        private float _draw;

        protected override int PanelIndex => 0;

        protected override void Build()
        {
            var content = Shell("Semana 37 · Ano 4 — fase Indie");

            var stats = Kit.Row(Align.Stretch);
            content.Add(stats);
            stats.Add(Enter(StatCard(Kit.IcCoins, Kit.Cash, "Caixa", out var cash, "8 semanas de fôlego")));
            stats.Add(Enter(StatCard(Kit.IcChartLine, Kit.Teal, "Valuation", out var val, "▲ 10,8% no mês", Kit.Green)));
            stats.Add(Enter(StatCard(Kit.IcArrowUp, Kit.Green, "Receita / semana", out var rev, "3 jogos vendendo")));
            var last = StatCard(Kit.IcArrowDown, Kit.Red, "Despesas / semana", out var exp, "salários são 62%");
            last.style.marginRight = 0;
            stats.Add(Enter(last));
            CountUp(cash, 0, 150984, 0.4f, 0.9f, Kit.Money);
            CountUp(val, 0, 412000, 0.45f, 0.9f, Kit.Money);
            CountUp(rev, 0, 21400, 0.5f, 0.9f, Kit.Money);
            CountUp(exp, 0, 17900, 0.55f, 0.9f, Kit.Money);

            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1; row.style.marginTop = 16;
            content.Add(row);

            var chartCard = Kit.Outline(16); chartCard.style.flexGrow = 1; Kit.Pad(chartCard, 16, 18);
            var ch = Kit.Row();
            ch.Add(Kit.Text("Histórico de valuation", 15, Kit.Ink, true));
            ch.Add(Kit.Spacer());
            ch.Add(Legend("Pixel Forja", Kit.Teal));
            ch.Add(Legend("MegaByte Corp", Kit.Red));
            ch.Add(Legend("Lua Nova Games", Kit.Purple));
            chartCard.Add(ch);
            _chart = new VisualElement(); _chart.style.flexGrow = 1; _chart.style.marginTop = 10;
            _chart.generateVisualContent += DrawChart;
            chartCard.Add(_chart);
            var axis = Kit.Row(); axis.style.justifyContent = Justify.SpaceBetween; axis.style.marginTop = 4;
            foreach (string y in new[] { "Ano 1", "Ano 2", "Ano 3", "Ano 4" }) axis.Add(Kit.Text(y, 10, Kit.Soft, true));
            chartCard.Add(axis);
            row.Add(Enter(chartCard));

            row.Add(Kit.Gap(16));
            var side = Kit.Col(); side.style.width = 380; side.style.flexShrink = 0;
            row.Add(side);

            var expCard = Kit.Tile(16);
            expCard.Add(Kit.Text("Para onde vai o dinheiro", 14, Kit.Ink, true));
            expCard.Add(Expense("Salários", 11100, 1f, Kit.Orange, 0.7f));
            expCard.Add(Expense("Aluguel e contas", 3200, 0.29f, Kit.Soft, 0.76f));
            expCard.Add(Expense("Pesquisa (P&D)", 1600, 0.14f, Kit.Teal, 0.82f));
            expCard.Add(Expense("Marketing", 1500, 0.135f, Kit.Pink, 0.88f));
            expCard.Add(Expense("Engine", 500, 0.045f, Kit.Sky, 0.94f));
            side.Add(Enter(expCard));

            var facCard = Kit.Tile(16); facCard.style.marginTop = 12; facCard.style.flexGrow = 1;
            facCard.Add(Kit.Text("O que forma o valuation", 14, Kit.Ink, true));
            facCard.Add(Factor(Kit.IcGamepad, "Receita dos jogos", "+ R$ 168K", true));
            facCard.Add(Factor(Kit.IcStar, "Reputação e nota média", "+ R$ 96K", true));
            facCard.Add(Factor(Kit.IcUsers, "Equipe", "+ R$ 54K", true));
            facCard.Add(Factor(Kit.IcCrown, "IPs e franquias", "+ R$ 61K", true));
            facCard.Add(Factor(Kit.IcCoins, "Liquidez (caixa)", "+ R$ 45K", true));
            facCard.Add(Factor(Kit.IcHourglass, "Estagnação de fase", "− R$ 12K", false));
            side.Add(Enter(facCard));
        }

        private static VisualElement Legend(string name, Color c)
        {
            var r = Kit.Row(); r.style.marginLeft = 14;
            var d = new VisualElement(); d.style.width = 10; d.style.height = 10; Kit.Radius(d, 5); d.style.backgroundColor = c;
            r.Add(d);
            var l = Kit.Text(name, 11, Kit.Soft, true); l.style.marginLeft = 5;
            r.Add(l);
            return r;
        }

        private VisualElement Expense(string name, float value, float pct, Color color, float delay)
        {
            var c = Kit.Col(); c.style.marginTop = 10;
            var r = Kit.Row();
            r.Add(Kit.Text(name, 12, Kit.Ink));
            r.Add(Kit.Spacer());
            r.Add(Kit.Text(Kit.Money(value), 12, Kit.Ink, true));
            c.Add(r);
            var bar = Kit.Bar(8, color, out var fill); bar.style.marginTop = 4;
            c.Add(bar);
            Grow(fill, pct, delay);
            return c;
        }

        private static VisualElement Factor(string icon, string name, string value, bool positive)
        {
            var r = Kit.Row(); r.style.marginTop = 9;
            var i = Kit.Icon(icon, 12, Kit.Soft); i.style.width = 20;
            r.Add(i);
            var l = Kit.Text(name, 12, Kit.Ink); l.style.marginLeft = 6;
            r.Add(l);
            r.Add(Kit.Spacer());
            r.Add(Kit.Text(value, 12, positive ? Kit.Green : Kit.Red, true));
            return r;
        }

        protected override void OnTick(float dt)
        {
            _draw = Mathf.Clamp01((Clock - 0.5f) / 1.6f);
            _chart.MarkDirtyRepaint();
        }

        private void DrawChart(MeshGenerationContext mgc)
        {
            var r = _chart.contentRect;
            if (r.width < 20f || r.height < 20f) return;
            var p = mgc.painter2D;
            float w = r.width, h = r.height;
            p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;

            p.strokeColor = Kit.Track; p.lineWidth = 1.5f;
            for (int g = 0; g <= 4; g++)
            {
                float y = h * g / 4f;
                p.BeginPath(); p.MoveTo(new Vector2(0, y)); p.LineTo(new Vector2(w, y)); p.Stroke();
            }

            // limite da próxima fase (tracejado)
            float ty = h - Threshold / MaxValue * h;
            p.strokeColor = Kit.A(Kit.Gold, 0.9f); p.lineWidth = 2f;
            for (float x = 0; x < w; x += 14f)
            {
                p.BeginPath(); p.MoveTo(new Vector2(x, ty)); p.LineTo(new Vector2(Mathf.Min(x + 8f, w), ty)); p.Stroke();
            }

            Line(p, RivalA, Kit.A(Kit.Red, 0.55f), 2f, w, h, false);
            Line(p, RivalB, Kit.A(Kit.Purple, 0.55f), 2f, w, h, false);
            Line(p, Mine, Kit.Teal, 4f, w, h, true);
        }

        private void Line(Painter2D p, float[] data, Color color, float width, float w, float h, bool area)
        {
            float span = (data.Length - 1) * _draw;
            int full = Mathf.FloorToInt(span);
            if (span <= 0.001f) return;
            Vector2 At(float i)
            {
                int a = Mathf.Clamp(Mathf.FloorToInt(i), 0, data.Length - 1), b = Mathf.Min(a + 1, data.Length - 1);
                float v = Mathf.Lerp(data[a], data[b], i - a);
                return new Vector2(i / (data.Length - 1) * w, h - v / MaxValue * h);
            }
            Vector2 tip = At(span);
            if (area)
            {
                p.fillColor = Kit.A(color, 0.12f);
                p.BeginPath(); p.MoveTo(new Vector2(0, h));
                for (int i = 0; i <= full; i++) p.LineTo(At(i));
                p.LineTo(tip); p.LineTo(new Vector2(tip.x, h)); p.ClosePath(); p.Fill();
            }
            p.strokeColor = color; p.lineWidth = width;
            p.BeginPath(); p.MoveTo(At(0));
            for (int i = 1; i <= full; i++) p.LineTo(At(i));
            p.LineTo(tip); p.Stroke();
            if (area)
            {
                float pulse = 6f + 2f * Mathf.Sin(Clock * 5f);
                Kit.Disc(p, tip, pulse + 4f, Kit.A(color, 0.25f));
                Kit.Disc(p, tip, 5f, color);
            }
        }
    }

    /// <summary>Painel Projetos (GDD 5.8, 10.4, 13): projeto atual, catálogo, franquias e ações de pós-lançamento.</summary>
    internal sealed class ProjectsScreen : ManagementScreen
    {
        private readonly List<(VisualElement el, float[] data, Color c)> _sparks = new List<(VisualElement, float[], Color)>();

        protected override int PanelIndex => 1;

        protected override void Build()
        {
            var content = Shell("1 em desenvolvimento · 5 lançados · 2 franquias");

            var current = Kit.Tile(16); current.style.flexDirection = FlexDirection.Row; current.style.alignItems = Align.Center;
            Kit.Pad(current, 14, 18);
            current.Add(Kit.SquareBadge(Kit.IcDragon, Kit.Blue, 52));
            var ct = Kit.Col(); ct.style.marginLeft = 14; ct.style.width = 300;
            ct.Add(Kit.Chip("Em desenvolvimento", Kit.Blue, true));
            var cn = Kit.Text("Lendas de Aethel II", 18, Kit.Ink, true); cn.style.marginTop = 4;
            ct.Add(cn);
            ct.Add(Kit.Text("Action RPG · Fantasia Medieval · PC · Pequeno", 11, Kit.Soft));
            current.Add(ct);
            var pc = Kit.Col(); pc.style.flexGrow = 1; pc.style.marginLeft = 10; pc.style.marginRight = 20;
            var pl = Kit.Row();
            pl.Add(Kit.Text("Fase 2 de 5 — Arte e Visual", 12, Kit.Ink, true)); pl.Add(Kit.Spacer());
            pl.Add(Kit.Text("semana 7 de 20", 12, Kit.Soft));
            pc.Add(pl);
            var bar = Kit.Bar(12, Kit.Pink, out var fill); bar.style.marginTop = 6;
            pc.Add(bar);
            Grow(fill, 0.35f, 0.5f);
            current.Add(pc);
            current.Add(Kit.Ghost("Marketing", Kit.IcBullhorn));
            content.Add(Enter(current));

            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1; row.style.marginTop = 16;
            content.Add(row);

            var list = Kit.Col(); list.style.flexGrow = 1;
            row.Add(list);
            var head = Kit.Row(); Kit.Pad(head, 0, 14); head.style.marginBottom = 6;
            head.Add(HeadCell("Jogo", 0, true)); head.Add(HeadCell("Nota", 64)); head.Add(HeadCell("Unidades", 90));
            head.Add(HeadCell("Receita", 110)); head.Add(HeadCell("Vendas", 110)); head.Add(HeadCell("Pós-lançamento", 250));
            list.Add(head);
            list.Add(Enter(Game("Lendas de Aethel", "Action RPG · Fantasia Medieval · PC", Kit.IcDragon, 8.1f, "42,3K", "R$ 1,2M", new[] { 1f, 0.7f, 0.5f, 0.4f, 0.32f, 0.3f, 0.26f, 0.2f }, true, 0)));
            list.Add(Enter(Game("Turbo Kart 64", "Simulador · Corrida de Carros · Game Poy", Kit.IcCar, 6.4f, "18,9K", "R$ 380K", new[] { 1f, 0.55f, 0.3f, 0.18f, 0.1f, 0.07f, 0.05f, 0.03f }, true, 1)));
            list.Add(Enter(Game("Bloquinhos", "Puzzle · Cozy · PC", Kit.IcCubes, 7.3f, "25,0K", "R$ 290K", new[] { 0.8f, 0.6f, 0.55f, 0.5f, 0.42f, 0.4f, 0.36f, 0.3f }, true, 2)));
            list.Add(Enter(Game("Castelo Sombrio", "Plataforma · Horror Sobrenatural · PC", Kit.IcGhost, 4.2f, "3,1K", "R$ 41K", new[] { 0.5f, 0.2f, 0.1f, 0.05f, 0.02f, 0.01f, 0f, 0f }, false, 3)));
            list.Add(Enter(Game("Fazendinha Feliz", "Tycoon · Fazenda · PC", Kit.IcBuilding, 7.8f, "31,2K", "R$ 512K", new[] { 0.7f, 0.66f, 0.6f, 0.52f, 0.5f, 0.45f, 0.4f, 0.38f }, true, 4)));

            row.Add(Kit.Gap(16));
            var side = Kit.Col(); side.style.width = 300; side.style.flexShrink = 0;
            row.Add(side);
            side.Add(Kit.Caps("Franquias", 11, Kit.Soft));
            side.Add(Enter(Franchise("Aethel", 1, 8.1f, "Hype inicial +18 na próxima", Kit.Green)));
            side.Add(Enter(Franchise("Turbo Kart", 2, 5.9f, "Em risco: abaixo de 6 vira peso", Kit.Red)));
            var tip = Kit.Tile(14); tip.style.marginTop = 12; tip.style.flexDirection = FlexDirection.Row;
            tip.Add(Kit.Badge(Kit.IcInfo, Kit.Blue, 26));
            var tt = Kit.Wrap("Sequências herdam o hype da franquia — e uma sequência ruim derruba a média.", 11, Kit.Soft);
            tt.style.marginLeft = 10; tt.style.flexShrink = 1;
            tip.Add(tt);
            side.Add(Enter(tip));
        }

        private VisualElement Game(string name, string sub, string icon, float score, string units, string revenue, float[] sales, bool alive, int tone)
        {
            Color[] tones = { Kit.Blue, Kit.Orange, Kit.Teal, Kit.Purple, Kit.Green };
            var r = Kit.Outline(14); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center;
            r.style.marginBottom = 8; Kit.Pad(r, 10, 14);
            Kit.Smooth(r);
            r.RegisterCallback<PointerEnterEvent>(_ => Kit.BorderColor(r, Kit.Blue));
            r.RegisterCallback<PointerLeaveEvent>(_ => Kit.BorderColor(r, Kit.Track));

            var nameCell = Kit.Row(); nameCell.style.flexGrow = 1; nameCell.style.flexBasis = 0;
            nameCell.Add(Kit.SquareBadge(icon, tones[tone % tones.Length], 40, 10));
            var nt = Kit.Col(); nt.style.marginLeft = 10; nt.style.flexShrink = 1;
            nt.Add(Kit.Text(name, 14, Kit.Ink, true));
            nt.Add(Kit.Text(sub, 10, Kit.Soft));
            nameCell.Add(nt);
            r.Add(nameCell);

            Color sc = score >= 8f ? Kit.Green : score >= 6f ? Kit.Gold : Kit.Red;
            var scoreCell = Kit.Col(); scoreCell.style.width = 64;
            var pill = Kit.Text(score.ToString("0.0").Replace('.', ','), 14, Color.white, true);
            pill.style.backgroundColor = sc; Kit.Radius(pill, 9); Kit.Pad(pill, 4, 9); pill.style.alignSelf = Align.FlexStart;
            scoreCell.Add(pill);
            r.Add(scoreCell);

            var u = Kit.Text(units, 13, Kit.Ink, true); u.style.width = 90; r.Add(u);
            var rv = Kit.Text(revenue, 13, Kit.Cash, true); rv.style.width = 110; r.Add(rv);

            var spark = new VisualElement(); spark.style.width = 96; spark.style.height = 30; spark.style.marginRight = 14;
            var entry = (spark, sales, alive ? Kit.Blue : Kit.Faint);
            spark.generateVisualContent += m => DrawSpark(m, entry.spark, entry.sales, entry.Item3);
            _sparks.Add(entry);
            r.Add(spark);

            var act = Kit.Row(); act.style.width = 250;
            if (alive)
            {
                act.Add(Mini("DLC", Kit.IcPlus)); act.Add(Mini("Port", Kit.IcDesktop)); act.Add(Mini("Promoção", Kit.IcTag));
            }
            else act.Add(Kit.Chip("Vendas encerradas", Kit.Soft));
            r.Add(act);
            return r;
        }

        private static VisualElement Mini(string label, string icon)
        {
            var b = Kit.Button(label, icon, Kit.Sand, Kit.Ink, null, 30);
            b.style.paddingLeft = 10; b.style.paddingRight = 10; b.style.marginRight = 6;
            return b;
        }

        private VisualElement Franchise(string name, int games, float avg, string note, Color c)
        {
            var f = Kit.Outline(14); f.style.marginTop = 10; Kit.Pad(f, 12, 14);
            var r = Kit.Row();
            r.Add(Kit.Badge(Kit.IcCrown, Kit.A(Kit.Gold, 0.2f), 34, Kit.Gold));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 15, Kit.Ink, true));
            t.Add(Kit.Text(games + (games == 1 ? " jogo" : " jogos"), 11, Kit.Soft));
            r.Add(t);
            r.Add(Kit.Text(avg.ToString("0.0").Replace('.', ','), 22, c, true));
            f.Add(r);
            var bar = Kit.Bar(8, c, out var fill); bar.style.marginTop = 8;
            f.Add(bar);
            Grow(fill, avg / 10f, 0.8f);
            var n = Kit.Text(note, 11, c, true); n.style.marginTop = 6;
            f.Add(n);
            return f;
        }

        private float _sparkDraw;

        protected override void OnTick(float dt)
        {
            if (_sparkDraw >= 1f) return;
            _sparkDraw = Mathf.Clamp01((Clock - 0.7f) / 1.0f);
            foreach (var s in _sparks) s.el.MarkDirtyRepaint();
        }

        private void DrawSpark(MeshGenerationContext mgc, VisualElement el, float[] data, Color color)
        {
            var r = el.contentRect;
            if (r.width < 4f || _sparkDraw <= 0f) return;
            var p = mgc.painter2D;
            p.strokeColor = color; p.lineWidth = 2.5f; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            int n = Mathf.Max(2, Mathf.CeilToInt(data.Length * _sparkDraw));
            p.BeginPath();
            for (int i = 0; i < n && i < data.Length; i++)
            {
                var pt = new Vector2(i / (float)(data.Length - 1) * r.width, r.height - 2f - data[i] * (r.height - 4f));
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.Stroke();
        }
    }

    /// <summary>Painel RH (GDD 7): equipe, traços, burnout, treino e mercado de talentos.</summary>
    internal sealed class StaffScreen : ManagementScreen
    {
        private VisualElement _host;

        protected override int PanelIndex => 2;
        public override void SetState(int index) => SelectTab(index);

        protected override void Build()
        {
            var content = Shell("Equipe 2 de 2 + fundador · folha R$ 5.500 / semana");
            AddTabs(new[] { "Equipe", "Mercado de talentos" }, Show);
            _host = Kit.Col(); _host.style.flexGrow = 1;
            content.Add(_host);
            Show(0);
        }

        private void Show(int tab)
        {
            _host.Clear();
            ResetCascade(0.1f);
            if (tab == 0) BuildTeam(); else BuildMarket();
        }

        private void BuildTeam()
        {
            var grid = Kit.Row(Align.Stretch); grid.style.flexWrap = Wrap.Wrap;
            _host.Add(grid);
            grid.Add(Enter(Employee("David", "Fundador · faz de tudo", 0, "Fundador", Kit.Sky, 0.0f, 0.82f, 0.25f, "sem salário", new[] { "Técnica 3", "Carisma 1" }, true)));
            grid.Add(Enter(Employee("Lia Tanaka", "Programação", 3, "Pleno", Kit.Blue, 0.64f, 0.7f, 0.42f, "R$ 3.500 / sem", new[] { "Perfeccionista", "Leal" }, false)));
            grid.Add(Enter(Employee("Beto Ramos", "Design", 4, "Júnior", Kit.Pink, 0.31f, 0.38f, 0.81f, "R$ 2.000 / sem", new[] { "Criativo", "Propenso a burnout" }, false)));
            grid.Add(Enter(EmptySeat()));

            var cap = Kit.Tile(14); cap.style.flexDirection = FlexDirection.Row; cap.style.alignItems = Align.Center; cap.style.marginTop = 4;
            cap.Add(Kit.Badge(Kit.IcBuilding, Kit.Orange, 30));
            var ct = Kit.Col(); ct.style.marginLeft = 12; ct.style.flexGrow = 1;
            ct.Add(Kit.Text("Capacidade da fase Indie: 2 funcionários + fundador", 13, Kit.Ink, true));
            ct.Add(Kit.Text("Expanda para Escritório para chegar a 5. O custo de RH cresce com o tamanho da equipe.", 11, Kit.Soft));
            cap.Add(ct);
            cap.Add(Kit.Ghost("Festa da empresa · R$ 1.500", Kit.IcGift));
            _host.Add(Kit.Spacer());
            _host.Add(Enter(cap));
        }

        private VisualElement Employee(string name, string role, int tone, string level, Color roleColor, float xp, float mood, float burnout, string salary, string[] traits, bool founder)
        {
            var c = Kit.Outline(16); c.style.width = 296; c.style.marginRight = 14; c.style.marginBottom = 14; Kit.Pad(c, 16, 16);
            var top = Kit.Row();
            if (founder) { var art = new FounderArt(56) { HairStyle = 1, Accessory = 1 }; var w = Kit.Avatar(3, 56); w.Clear(); w.Add(art); top.Add(w); }
            else top.Add(Kit.Avatar(tone, 56));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexShrink = 1;
            t.Add(Kit.Text(name, 16, Kit.Ink, true));
            t.Add(Kit.Text(role, 11, Kit.Soft));
            var lv = Kit.Chip(level, roleColor, true); lv.style.marginTop = 4; lv.style.alignSelf = Align.FlexStart;
            t.Add(lv);
            top.Add(t);
            c.Add(top);

            var tr = Kit.Row(); tr.style.marginTop = 10; tr.style.flexWrap = Wrap.Wrap;
            foreach (string s in traits)
            {
                var chip = Kit.Chip(s, s.StartsWith("Propenso") ? Kit.Red : Kit.Soft); chip.style.marginRight = 6; chip.style.marginBottom = 4;
                tr.Add(chip);
            }
            c.Add(tr);

            if (!founder) c.Add(Meter("Experiência", xp, Kit.Blue, "próximo nível"));
            c.Add(Meter("Humor", mood, mood > 0.5f ? Kit.Green : Kit.Gold, mood > 0.5f ? "satisfeito" : "desanimado"));
            c.Add(Meter("Burnout", burnout, burnout > 0.7f ? Kit.Red : Kit.Orange, burnout > 0.7f ? "no limite!" : "sob controle"));

            c.Add(Kit.Divider(10));
            var f = Kit.Row();
            f.Add(Kit.Text(salary, 12, founder ? Kit.Soft : Kit.Ink, true));
            f.Add(Kit.Spacer());
            if (!founder)
            {
                f.Add(IconAction(Kit.IcGradCap, Kit.Blue)); f.Add(IconAction(Kit.IcPlane, Kit.Teal));
                f.Add(IconAction(Kit.IcGift, Kit.Gold)); f.Add(IconAction(Kit.IcClose, Kit.Red));
            }
            c.Add(f);
            if (!founder)
            {
                var legend = Kit.Text("treinar · férias · bônus · demitir", 9, Kit.Faint);
                legend.style.alignSelf = Align.FlexEnd; legend.style.marginTop = 3;
                c.Add(legend);
            }
            return c;
        }

        private static VisualElement IconAction(string icon, Color c)
        {
            var b = Kit.RoundButton(icon, Kit.A(c, 0.15f), c, 30); b.style.marginLeft = 6;
            return b;
        }

        private VisualElement Meter(string label, float value, Color color, string note)
        {
            var c = Kit.Col(); c.style.marginTop = 8;
            var r = Kit.Row();
            r.Add(Kit.Text(label, 11, Kit.Soft, true)); r.Add(Kit.Spacer());
            r.Add(Kit.Text(note, 10, color, true));
            c.Add(r);
            var bar = Kit.Bar(7, color, out var fill); bar.style.marginTop = 3;
            c.Add(bar);
            Grow(fill, value, 0.5f);
            return c;
        }

        private static VisualElement EmptySeat()
        {
            var c = Kit.Tile(16); c.style.width = 296; c.style.marginBottom = 14; c.style.alignItems = Align.Center; c.style.justifyContent = Justify.Center;
            c.style.minHeight = 250;
            c.Add(Kit.Badge(Kit.IcLock, Kit.Track, 52, Kit.Faint));
            var t = Kit.Text("Vaga bloqueada", 14, Kit.Soft, true); t.style.marginTop = 10;
            c.Add(t);
            c.Add(Kit.Text("Disponível na fase Escritório", 11, Kit.Faint));
            return c;
        }

        private void BuildMarket()
        {
            var info = Kit.Row(); info.style.marginBottom = 12;
            info.Add(Kit.Icon(Kit.IcHistory, 12, Kit.Soft));
            var it = Kit.Text("Novos candidatos em 3 semanas", 12, Kit.Soft, true); it.style.marginLeft = 6;
            info.Add(it);
            info.Add(Kit.Spacer());
            info.Add(Kit.Chip("Equipe cheia — demita ou expanda para contratar", Kit.Red, false, Kit.IcWarn));
            _host.Add(Enter(info));

            var grid = Kit.Row(Align.Stretch);
            _host.Add(grid);
            grid.Add(Enter(Candidate("Caio Melo", "Programação", 5, "Sênior", "R$ 6.000 / sem", 4, new[] { "Workaholic", "Egocêntrico" }, Kit.Blue)));
            grid.Add(Enter(Candidate("Nina Prado", "Design", 1, "Pleno", "R$ 3.500 / sem", 3, new[] { "Mentora", "Criativa" }, Kit.Pink)));
            grid.Add(Enter(Candidate("Jô Farias", "Programação", 6, "Júnior", "R$ 2.000 / sem", 2, new[] { "Ambicioso" }, Kit.Blue)));

            var hh = Kit.Tile(16); hh.style.width = 296; hh.style.alignItems = Align.Center; hh.style.justifyContent = Justify.Center;
            hh.Add(Kit.Badge(Kit.IcBinoculars, Kit.Track, 52, Kit.Faint));
            var ht = Kit.Text("Headhunting", 15, Kit.Soft, true); ht.style.marginTop = 10;
            hh.Add(ht);
            var hd = Kit.Wrap("Talentos raros (Estrela). Libera com a habilidade Contatos no Mercado.", 11, Kit.Faint);
            hd.style.unityTextAlign = TextAnchor.MiddleCenter; hd.style.marginTop = 4; hd.style.maxWidth = 230;
            hh.Add(hd);
            grid.Add(Enter(hh));
        }

        private VisualElement Candidate(string name, string role, int tone, string level, string salary, int stars, string[] traits, Color roleColor)
        {
            var c = Kit.Outline(16); c.style.width = 296; c.style.marginRight = 14; Kit.Pad(c, 16, 16); c.style.alignItems = Align.Center;
            Kit.Hover(c, 1.03f, 0.98f);
            c.Add(Kit.Avatar(tone, 72));
            var n = Kit.Text(name, 16, Kit.Ink, true); n.style.marginTop = 10;
            c.Add(n);
            c.Add(Kit.Text(role, 11, Kit.Soft));
            var lv = Kit.Chip(level, roleColor, true); lv.style.marginTop = 6;
            c.Add(lv);
            var st = Kit.Stars(stars, 5, 14); st.style.marginTop = 10;
            c.Add(st);
            var tr = Kit.Row(); tr.style.marginTop = 10; tr.style.flexWrap = Wrap.Wrap; tr.style.justifyContent = Justify.Center;
            foreach (string s in traits) { var chip = Kit.Chip(s, Kit.Soft); chip.style.marginRight = 4; chip.style.marginLeft = 4; chip.style.marginBottom = 4; tr.Add(chip); }
            c.Add(tr);
            c.Add(Kit.Divider(12));
            c.Add(Kit.Text(salary, 14, Kit.Ink, true));
            var hire = Kit.Button("Contratar", Kit.IcUserPlus, Kit.Orange, Color.white, null, 36); hire.style.marginTop = 10; hire.style.alignSelf = Align.Stretch;
            c.Add(hire);
            return c;
        }
    }
}
