using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>Painel Mercado (GDD 8.2, 8.3): tendências de gênero/tema, plataformas, janelas e rivais.</summary>
    internal sealed class MarketScreen : ManagementScreen
    {
        private VisualElement _host;

        protected override int PanelIndex => 3;
        public override void SetState(int index) => SelectTab(index);

        protected override void Build()
        {
            var content = Shell("Ano 4 — o que está em alta e quem está na disputa");
            AddTabs(new[] { "Tendências", "Rivais" }, Show);
            _host = Kit.Col(); _host.style.flexGrow = 1;
            content.Add(_host);
            Show(0);
        }

        private void Show(int tab)
        {
            _host.Clear();
            ResetCascade(0.1f);
            if (tab == 0) BuildTrends(); else BuildRivals();
        }

        private void BuildTrends()
        {
            var row = Kit.Row(Align.Stretch);
            _host.Add(row);

            var genres = Column("Gêneros", Kit.IcGamepad);
            genres.Add(Trend("Action RPG", 2, 0.35f)); genres.Add(Trend("Plataforma", 1, 0.55f));
            genres.Add(Trend("Puzzle", 0, 0.4f)); genres.Add(Trend("Simulador", -1, 0.72f));
            genres.Add(Trend("Estratégia RTS", 0, 0.2f)); genres.Add(Trend("Tycoon", -2, 0.9f));
            row.Add(Enter(genres));

            var themes = Column("Temas", Kit.IcHat);
            themes.Add(Trend("Cyberpunk", 2, 0.3f)); themes.Add(Trend("Fantasia Medieval", 1, 0.62f));
            themes.Add(Trend("Espaço Sideral", 1, 0.25f)); themes.Add(Trend("Futebol", 0, 0.45f));
            themes.Add(Trend("Horror Sobrenatural", -1, 0.5f)); themes.Add(Trend("Corrida de Carros", -2, 0.85f));
            row.Add(Enter(themes));

            var plats = Column("Plataformas", Kit.IcDesktop);
            plats.style.marginRight = 0;
            plats.Add(Platform("PC", "Licença R$ 5.000", 0.5f, Kit.Teal, false));
            plats.Add(Platform("Game Poy", "Licença R$ 3.000", 0.42f, Kit.Blue, false));
            plats.Add(Platform("MegaDrive Pro", "Chega no Ano 5", 0f, Kit.Faint, true));
            var note = Kit.Tile(12); note.style.marginTop = 12; note.style.flexDirection = FlexDirection.Row;
            note.Add(Kit.Badge(Kit.IcNews, Kit.Purple, 26));
            var nt = Kit.Wrap("Boato: um novo console de mesa está a caminho. Quem pesquisar antes sai na frente.", 11, Kit.Soft);
            nt.style.marginLeft = 10; nt.style.flexShrink = 1;
            note.Add(nt);
            plats.Add(note);
            row.Add(Enter(plats));

            var windows = Kit.Tile(16); windows.style.marginTop = 14; Kit.Pad(windows, 14, 18);
            var wh = Kit.Row();
            wh.Add(Kit.Text("Janelas de lançamento — próximas 12 semanas", 14, Kit.Ink, true));
            wh.Add(Kit.Spacer());
            wh.Add(Kit.Chip("Sentimento público: Otimista", Kit.Green, false, Kit.IcSmile));
            windows.Add(wh);
            var strip = Kit.Row(Align.Stretch); strip.style.marginTop = 10; strip.style.height = 70;
            (int week, string label, Color c)[] marks = { (2, "MegaByte: RPG", Kit.Red), (7, "Feira de games", Kit.Purple), (9, "Lua Nova: Puzzle", Kit.Red) };
            for (int i = 0; i < 12; i++)
            {
                bool ideal = i == 4 || i == 5;
                var cell = new VisualElement(); cell.style.flexGrow = 1; cell.style.flexBasis = 0; cell.style.marginRight = 4;
                Kit.Radius(cell, 8); cell.style.backgroundColor = ideal ? Kit.A(Kit.Green, 0.22f) : Color.white;
                cell.style.alignItems = Align.Center; cell.style.paddingTop = 6;
                cell.Add(Kit.Text("S" + (38 + i), 10, Kit.Soft, true));
                foreach (var m in marks)
                {
                    if (m.week != i) continue;
                    var dot = new VisualElement(); dot.style.width = 10; dot.style.height = 10; Kit.Radius(dot, 5);
                    dot.style.backgroundColor = m.c; dot.style.marginTop = 5;
                    cell.Add(dot);
                    var t = Kit.Wrap(m.label, 9, m.c, true); t.style.unityTextAlign = TextAnchor.MiddleCenter;
                    cell.Add(t);
                }
                if (ideal) { var t = Kit.Text("ideal", 10, Kit.Green, true); t.style.marginTop = 8; cell.Add(t); }
                strip.Add(cell);
            }
            windows.Add(strip);
            _host.Add(Kit.Spacer());
            _host.Add(Enter(windows));
        }

        private static VisualElement Column(string title, string icon)
        {
            var c = Kit.Outline(16); c.style.flexGrow = 1; c.style.flexBasis = 0; c.style.marginRight = 14; Kit.Pad(c, 14, 16);
            var h = Kit.Row(); h.style.marginBottom = 4;
            h.Add(Kit.Icon(icon, 14, Kit.Purple));
            var t = Kit.Text(title, 15, Kit.Ink, true); t.style.marginLeft = 8;
            h.Add(t);
            h.Add(Kit.Spacer());
            h.Add(Kit.Caps("Saturação", 9, Kit.Faint, 1f));
            c.Add(h);
            return c;
        }

        private VisualElement Trend(string name, int trend, float saturation)
        {
            var r = Kit.Row(); r.style.marginTop = 10;
            string icon = trend > 0 ? Kit.IcArrowUp : trend < 0 ? Kit.IcArrowDown : Kit.IcMinus;
            Color c = trend > 0 ? Kit.Green : trend < 0 ? Kit.Red : Kit.Faint;
            r.Add(Kit.Badge(icon, Kit.A(c, 0.16f), 26, c));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 13, Kit.Ink, true));
            t.Add(Kit.Text(trend >= 2 ? "Explodindo" : trend == 1 ? "Em alta" : trend == 0 ? "Estável" : trend == -1 ? "Esfriando" : "Saturado", 10, c, true));
            r.Add(t);
            var bar = Kit.Bar(8, saturation > 0.7f ? Kit.Red : Kit.Purple, out var fill); bar.style.width = 110;
            r.Add(bar);
            Grow(fill, saturation, 0.5f);
            return r;
        }

        private VisualElement Platform(string name, string note, float pop, Color c, bool upcoming)
        {
            var col = Kit.Col(); col.style.marginTop = 10;
            var r = Kit.Row();
            r.Add(Kit.Badge(upcoming ? Kit.IcClock : Kit.IcGamepad, Kit.A(c, 0.18f), 26, c));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 13, upcoming ? Kit.Soft : Kit.Ink, true));
            t.Add(Kit.Text(note, 10, Kit.Soft));
            r.Add(t);
            if (!upcoming) r.Add(Kit.Text(Mathf.RoundToInt(pop * 100f) + "%", 13, c, true));
            col.Add(r);
            if (!upcoming)
            {
                var bar = Kit.Bar(8, c, out var fill); bar.style.marginTop = 5;
                col.Add(bar);
                Grow(fill, pop, 0.6f);
            }
            return col;
        }

        private void BuildRivals()
        {
            var grid = Kit.Row(Align.Stretch);
            _host.Add(grid);
            grid.Add(Enter(Rival("MegaByte Corp", "Estúdio", "R$ 4,3M", "Reino de Aço III · 8,7", 0.2f, Kit.Red, Kit.IcSkull, null)));
            grid.Add(Enter(Rival("Lua Nova Games", "Indie", "R$ 205K", "Peças do Céu · 7,1", 0.6f, Kit.Purple, Kit.IcStar, null)));
            grid.Add(Enter(Rival("Bit Brasa Games", "Escritório", "R$ 980K", "Fogo Cruzado · 6,8", 0.45f, Kit.Orange, Kit.IcFire, "Sua empresa da Run 2")));
            grid.Add(Enter(Rival("Garagem 42", "Garagem", "R$ 38K", "— nenhum ainda", 0.8f, Kit.Teal, Kit.IcRocket, "Sua empresa da Run 3")));

            var feed = Kit.Tile(16); feed.style.marginTop = 14; Kit.Pad(feed, 14, 18);
            feed.Add(Kit.Text("Movimentos recentes", 14, Kit.Ink, true));
            feed.Add(News(Kit.IcRocket, Kit.Red, "MegaByte Corp anunciou um Action RPG para a semana 40.", "há 1 semana"));
            feed.Add(News(Kit.IcUserPlus, Kit.Orange, "Bit Brasa Games sondou a Lia Tanaka com uma oferta de salário.", "há 2 semanas"));
            feed.Add(News(Kit.IcHandshake, Kit.Purple, "Lua Nova Games fechou contrato de publicação com a GigaPublish.", "há 4 semanas"));
            _host.Add(Kit.Spacer());
            _host.Add(Enter(feed));
        }

        private VisualElement Rival(string name, string phase, string valuation, string lastGame, float relation, Color c, string icon, string timeline)
        {
            var card = Kit.Outline(16); card.style.flexGrow = 1; card.style.flexBasis = 0; card.style.marginRight = 14; Kit.Pad(card, 16, 16);
            var top = Kit.Row();
            top.Add(Kit.SquareBadge(icon, c, 48));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexShrink = 1;
            t.Add(Kit.Text(name, 15, Kit.Ink, true));
            t.Add(Kit.Text("Fase " + phase, 11, Kit.Soft));
            top.Add(t);
            card.Add(top);
            if (timeline != null)
            {
                var chip = Kit.Chip(timeline, Kit.Gold, false, Kit.IcHourglass); chip.style.marginTop = 8; chip.style.alignSelf = Align.FlexStart;
                card.Add(chip);
            }
            else card.Add(Kit.Gap(0, 30));
            card.Add(Kit.Divider(10));
            card.Add(Kit.Text("Valuation estimado", 10, Kit.Soft, true));
            card.Add(Kit.Text(valuation, 20, Kit.Ink, true));
            var lg = Kit.Text("Último jogo", 10, Kit.Soft, true); lg.style.marginTop = 8;
            card.Add(lg);
            card.Add(Kit.Text(lastGame, 12, Kit.Ink));

            var rl = Kit.Row(); rl.style.marginTop = 10;
            rl.Add(Kit.Text("Relação", 10, Kit.Soft, true)); rl.Add(Kit.Spacer());
            Color rc = relation < 0.35f ? Kit.Red : relation < 0.65f ? Kit.Gold : Kit.Green;
            rl.Add(Kit.Text(relation < 0.35f ? "Hostil" : relation < 0.65f ? "Neutra" : "Amigável", 10, rc, true));
            card.Add(rl);
            var bar = Kit.Bar(7, rc, out var fill); bar.style.marginTop = 3;
            card.Add(bar);
            Grow(fill, relation, 0.6f);

            var actions = Kit.Row(); actions.style.marginTop = 14;
            var spy = Kit.Button("Espiar", Kit.IcSecret, Kit.Sand, Kit.Ink, null, 32); spy.style.flexGrow = 1; spy.style.marginRight = 6;
            actions.Add(spy);
            var buy = Kit.Button("Comprar", Kit.IcBriefcase, Kit.Purple, Color.white, null, 32); buy.style.flexGrow = 1;
            actions.Add(buy);
            card.Add(actions);
            return card;
        }

        private static VisualElement News(string icon, Color c, string text, string when)
        {
            var r = Kit.Row(); r.style.marginTop = 9;
            r.Add(Kit.Badge(icon, Kit.A(c, 0.16f), 26, c));
            var t = Kit.Text(text, 12, Kit.Ink); t.style.marginLeft = 10; t.style.flexGrow = 1;
            r.Add(t);
            r.Add(Kit.Text(when, 10, Kit.Faint, true));
            return r;
        }
    }

    /// <summary>Painel P&D (GDD 6, 19.3, 19.4): pontos de pesquisa, catálogo por categoria e montagem de engine.</summary>
    internal sealed class ResearchScreen : ManagementScreen
    {
        private static readonly string[] Categories = { "Engine", "Ferramentas", "Plataformas", "Negócios", "Marketing", "Infraestrutura" };

        // state: 0 disponível, 1 pesquisando, 2 concluída, 3 bloqueada pela fase
        private static readonly (string icon, string name, string cost, int state)[][] Nodes =
        {
            new[] { (Kit.IcImage, "Renderizador 2D básico", "10 PP · R$ 200/sem", 2), (Kit.IcFilm, "Animação spritesheet", "10 PP · R$ 200/sem", 2),
                    (Kit.IcImage, "Renderizador 2D avançado", "30 PP · R$ 800/sem", 1), (Kit.IcAtom, "Física 2D básica", "10 PP · R$ 200/sem", 0),
                    (Kit.IcVolume, "Motor de áudio básico", "10 PP · R$ 200/sem", 0), (Kit.IcNetwork, "Pathfinding", "30 PP · R$ 800/sem", 0),
                    (Kit.IcCube, "Renderizador 3D básico", "30 PP · R$ 800/sem", 3), (Kit.IcRobot, "Machine learning NPCs", "80 PP · R$ 2.000/sem", 3) },
            new[] { (Kit.IcCode, "GitGud", "10 PP · R$ 200/sem", 2), (Kit.IcLaptop, "VeseCode", "10 PP · R$ 200/sem", 0),
                    (Kit.IcBrush, "Asprite+", "10 PP · R$ 200/sem", 0), (Kit.IcBug, "BugZapper", "30 PP · R$ 800/sem", 0),
                    (Kit.IcTasks, "JirAAA", "30 PP · R$ 800/sem", 3), (Kit.IcMusic, "FMaud", "30 PP · R$ 800/sem", 3) },
            new[] { (Kit.IcDesktop, "PC (Steambox)", "30 PP · R$ 800/sem", 2), (Kit.IcGamepad, "Game Poy", "30 PP · R$ 800/sem", 0),
                    (Kit.IcMobile, "Mobile", "30 PP · R$ 800/sem", 3), (Kit.IcVr, "VR", "30 PP · R$ 800/sem", 3) },
            new[] { (Kit.IcBriefcase, "Outsourcing simples", "30 PP · R$ 800/sem", 0), (Kit.IcContract, "Publisher (terceiros)", "30 PP · R$ 800/sem", 0),
                    (Kit.IcCrown, "Gestão de Franquias", "80 PP · R$ 2.000/sem", 3), (Kit.IcHandshake, "M&A", "80 PP · R$ 2.000/sem", 3),
                    (Kit.IcChartLine, "IPO", "80 PP · R$ 2.000/sem", 3) },
            new[] { (Kit.IcNews, "Press Kit Pro", "10 PP · R$ 200/sem", 0), (Kit.IcHeart, "Wishlist Campaign", "30 PP · R$ 800/sem", 0),
                    (Kit.IcStar, "Influencer Network", "30 PP · R$ 800/sem", 3), (Kit.IcTrophy, "Launch Event", "80 PP · R$ 2.000/sem", 3) },
            new[] { (Kit.IcServer, "CI-CD", "30 PP · R$ 800/sem", 3), (Kit.IcUsers, "HR System", "30 PP · R$ 800/sem", 3),
                    (Kit.IcCloud, "Cloud farm", "80 PP · R$ 2.000/sem", 3) }
        };

        private VisualElement _host, _grid, _gauge, _activeFill;
        private float _gaugeFill;

        protected override int PanelIndex => 4;
        public override void SetState(int index) => SelectTab(index);

        protected override void Build()
        {
            var content = Shell("O P&D recomeça do zero a cada run");
            AddTabs(new[] { "Pesquisa", "Engines" }, Show);
            _host = Kit.Col(); _host.style.flexGrow = 1;
            content.Add(_host);
            Show(0);
        }

        private void Show(int tab)
        {
            _host.Clear(); _gauge = null; _activeFill = null;
            ResetCascade(0.1f);
            if (tab == 0) BuildResearch(); else BuildEngine();
        }

        private void BuildResearch()
        {
            var top = Kit.Row(Align.Stretch);
            _host.Add(top);

            var pp = Kit.Tile(16); pp.style.width = 250; pp.style.marginRight = 14; Kit.Pad(pp, 14, 16);
            var pr = Kit.Row();
            pr.Add(Kit.Badge(Kit.IcFlask, Kit.Teal, 40));
            var pv = Kit.Text("0", 34, Kit.Ink, true); pv.style.marginLeft = 12;
            pr.Add(pv);
            var pu = Kit.Text("PP", 14, Kit.Soft, true); pu.style.marginLeft = 6; pu.style.marginTop = 12;
            pr.Add(pu);
            pp.Add(pr);
            CountUp(pv, 0, 22, 0.4f, 0.8f, v => Mathf.RoundToInt(v).ToString());
            var pn = Kit.Wrap("+2 por semana pesquisando · +5 por jogo lançado", 11, Kit.Soft); pn.style.marginTop = 6;
            pp.Add(pn);
            top.Add(Enter(pp));

            var slot1 = Kit.Outline(16); slot1.style.flexGrow = 1; slot1.style.flexBasis = 0; slot1.style.marginRight = 14; Kit.Pad(slot1, 14, 16);
            Kit.BorderColor(slot1, Kit.Teal);
            var s1 = Kit.Row();
            s1.Add(Kit.Avatar(3, 44));
            var s1t = Kit.Col(); s1t.style.marginLeft = 12; s1t.style.flexGrow = 1;
            s1t.Add(Kit.Text("Lia está pesquisando", 11, Kit.Soft, true));
            s1t.Add(Kit.Text("Renderizador 2D avançado", 15, Kit.Ink, true));
            s1.Add(s1t);
            s1.Add(Kit.Text("R$ 800 / sem", 12, Kit.Red, true));
            slot1.Add(s1);
            var bar = Kit.Bar(10, Kit.Teal, out _activeFill); bar.style.marginTop = 10;
            slot1.Add(bar);
            var s1n = Kit.Text("faltam 4 semanas", 11, Kit.Soft); s1n.style.marginTop = 4;
            slot1.Add(s1n);
            top.Add(Enter(slot1));

            var slot2 = Kit.Tile(16); slot2.style.flexGrow = 1; slot2.style.flexBasis = 0; Kit.Pad(slot2, 14, 16);
            slot2.style.flexDirection = FlexDirection.Row; slot2.style.alignItems = Align.Center;
            slot2.Add(Kit.Avatar(4, 44));
            var s2t = Kit.Col(); s2t.style.marginLeft = 12; s2t.style.flexGrow = 1;
            s2t.Add(Kit.Text("Beto está ocioso", 15, Kit.Ink, true));
            s2t.Add(Kit.Wrap("Escolha uma pesquisa abaixo para ele. Uma por pessoa sem projeto.", 11, Kit.Soft));
            slot2.Add(s2t);
            top.Add(Enter(slot2));

            var cats = Widgets.Segmented(Categories, 0, FillGrid, Kit.Teal);
            cats.style.marginTop = 16; cats.style.marginBottom = 12;
            _host.Add(Enter(cats));

            _grid = Kit.Row(Align.Stretch); _grid.style.flexWrap = Wrap.Wrap;
            _host.Add(_grid);
            FillGrid(0);
        }

        private void FillGrid(int category)
        {
            _grid.Clear();
            ResetCascade(0.05f);
            foreach (var n in Nodes[category]) _grid.Add(Enter(Node(n.icon, n.name, n.cost, n.state)));
        }

        private static VisualElement Node(string icon, string name, string cost, int state)
        {
            bool locked = state == 3;
            var c = Kit.Outline(14); c.style.width = 292; c.style.marginRight = 12; c.style.marginBottom = 12; Kit.Pad(c, 12, 14);
            c.style.flexDirection = FlexDirection.Row; c.style.alignItems = Align.Center;
            Color col = state == 2 ? Kit.Green : locked ? Kit.Faint : Kit.Teal;
            if (state == 1) Kit.BorderColor(c, Kit.Teal);
            if (state == 0) Kit.Hover(c, 1.03f, 0.97f);
            c.Add(Kit.Badge(locked ? Kit.IcLock : icon, Kit.A(col, 0.16f), 40, col));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexGrow = 1; t.style.flexShrink = 1;
            t.Add(Kit.Text(name, 13, locked ? Kit.Faint : Kit.Ink, true));
            t.Add(Kit.Text(cost, 11, Kit.Soft));
            c.Add(t);
            switch (state)
            {
                case 0: c.Add(Kit.RoundButton(Kit.IcPlus, Kit.Teal, Color.white, 30)); break;
                case 1: c.Add(Kit.Chip("Pesquisando", Kit.Teal, true)); break;
                case 2: c.Add(Kit.Badge(Kit.IcCheck, Kit.Green, 26)); break;
                default: c.Add(Kit.Chip("Fase maior", Kit.Soft)); c.style.opacity = 0.75f; break;
            }
            return c;
        }

        private void BuildEngine()
        {
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            _host.Add(row);

            var left = Kit.Col(); left.style.flexGrow = 1;
            row.Add(left);
            left.Add(Kit.Caps("Módulos pesquisados", 11, Kit.Soft));
            left.Add(Enter(Module(Kit.IcImage, "Renderização", "Renderizador 2D básico", "Leve · 100 EB · R$ 100/sem", true)));
            left.Add(Enter(Module(Kit.IcFilm, "Animação", "Animação spritesheet", "Leve · 100 EB · R$ 100/sem", true)));
            left.Add(Enter(Module(Kit.IcAtom, "Física", "Física 2D básica", "Leve · 100 EB · R$ 100/sem", true)));
            left.Add(Enter(Module(Kit.IcVolume, "Áudio", "Motor de áudio básico", "Leve · 100 EB · R$ 100/sem", false)));
            left.Add(Enter(Module(Kit.IcNetwork, "IA", "Pathfinding", "Médio · 250 EB · R$ 300/sem", false)));
            var hint = Kit.Tile(12); hint.style.marginTop = 10; hint.style.flexDirection = FlexDirection.Row;
            hint.Add(Kit.Badge(Kit.IcInfo, Kit.Blue, 26));
            var ht = Kit.Wrap("Mais módulos = mais Engine Budget para gastar em cada fase do projeto — e mais custo por semana enquanto o jogo está em produção.", 11, Kit.Soft);
            ht.style.marginLeft = 10; ht.style.flexShrink = 1;
            hint.Add(ht);
            left.Add(Enter(hint));

            row.Add(Kit.Gap(20));
            var right = Kit.Tile(18); right.style.width = 420; right.style.flexShrink = 0; right.style.alignItems = Align.Center; Kit.Pad(right, 18, 20);
            right.Add(Kit.Caps("Nova engine", 11, Kit.Soft));
            var field = Widgets.Field("Forja 2D", 300); field.style.marginTop = 8;
            right.Add(field);

            var gaugeWrap = new VisualElement(); gaugeWrap.style.width = 210; gaugeWrap.style.height = 210; gaugeWrap.style.marginTop = 16;
            gaugeWrap.style.alignItems = Align.Center; gaugeWrap.style.justifyContent = Justify.Center;
            _gauge = Kit.Fill(); _gauge.generateVisualContent += DrawGauge;
            gaugeWrap.Add(_gauge);
            var gv = Kit.Text("0", 44, Kit.Ink, true);
            gaugeWrap.Add(gv);
            gaugeWrap.Add(Kit.Text("ENGINE BUDGET", 10, Kit.Soft, true));
            right.Add(gaugeWrap);
            CountUp(gv, 0, 300, 0.4f, 1.0f, v => Mathf.RoundToInt(v).ToString());
            Animate(0.4f, 1.0f, k => _gaugeFill = 0.5f * k);

            var stats = Kit.Row(); stats.style.alignSelf = Align.Stretch; stats.style.justifyContent = Justify.SpaceBetween; stats.style.marginTop = 14;
            stats.Add(Small("Módulos", "3"));
            stats.Add(Small("Gráficos", "2D"));
            stats.Add(Small("Custo em produção", "R$ 300/sem"));
            right.Add(stats);
            right.Add(Kit.Spacer());
            var build = Kit.Button("Montar engine", Kit.IcWrench, Kit.Teal, Color.white); build.style.alignSelf = Align.Stretch; build.style.marginTop = 14;
            right.Add(build);
            row.Add(Enter(right, 0.3f));
        }

        private static VisualElement Small(string cap, string val)
        {
            var c = Kit.Col(); c.style.alignItems = Align.Center;
            c.Add(Kit.Text(val, 18, Kit.Ink, true));
            c.Add(Kit.Text(cap, 10, Kit.Soft, true));
            return c;
        }

        private static VisualElement Module(string icon, string category, string name, string cost, bool installed)
        {
            var r = Kit.Outline(14); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 8; Kit.Pad(r, 10, 14);
            if (installed) { Kit.BorderColor(r, Kit.Teal); r.style.backgroundColor = Kit.A(Kit.Teal, 0.07f); }
            r.Add(Kit.Badge(icon, Kit.A(Kit.Sky, 0.16f), 38, Kit.Sky));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexGrow = 1;
            t.Add(Kit.Text(category.ToUpperInvariant(), 9, Kit.Soft, true));
            t.Add(Kit.Text(name, 14, Kit.Ink, true));
            r.Add(t);
            var c = Kit.Text(cost, 11, Kit.Soft, true); c.style.marginRight = 14;
            r.Add(c);
            r.Add(Widgets.Toggle(installed));
            return r;
        }

        protected override void OnTick(float dt)
        {
            if (_activeFill != null) _activeFill.style.width = Length.Percent(58f + 4f * Mathf.Sin(Clock * 2f));
            if (_gauge != null) _gauge.MarkDirtyRepaint();
        }

        private void DrawGauge(MeshGenerationContext mgc)
        {
            var r = _gauge.contentRect;
            if (r.width < 10f) return;
            var p = mgc.painter2D;
            Vector2 c = r.center; float rad = r.width * 0.5f - 12f;
            p.lineCap = LineCap.Round; p.lineWidth = 16f;
            p.strokeColor = Kit.Track;
            p.BeginPath(); p.Arc(c, rad, new Angle(135f), new Angle(405f)); p.Stroke();
            if (_gaugeFill > 0.005f)
            {
                p.strokeColor = Kit.Sky;
                p.BeginPath(); p.Arc(c, rad, new Angle(135f), new Angle(135f + 270f * _gaugeFill)); p.Stroke();
                Vector2 tip = Kit.OnCircle(c, rad, 135f + 270f * _gaugeFill);
                Kit.Disc(p, tip, 11f + 2f * Mathf.Sin(Clock * 5f), Kit.A(Kit.Sky, 0.3f));
                Kit.Disc(p, tip, 6f, Color.white);
            }
        }
    }

    /// <summary>Painel Relações (GDD 9 e 10.3): contratos/parcerias e declarações do CEO.</summary>
    internal sealed class RelationsScreen : ManagementScreen
    {
        private VisualElement _host, _gauge;
        private float _needle;

        protected override int PanelIndex => 5;
        public override void SetState(int index) => SelectTab(index);

        protected override void Build()
        {
            var content = Shell("Parcerias, contratos e a sua imagem pública");
            AddTabs(new[] { "Contratos", "Declarações do CEO" }, Show);
            _host = Kit.Col(); _host.style.flexGrow = 1;
            content.Add(_host);
            Show(0);
        }

        private void Show(int tab)
        {
            _host.Clear(); _gauge = null;
            ResetCascade(0.1f);
            if (tab == 0) BuildContracts(); else BuildStatements();
        }

        private void BuildContracts()
        {
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            _host.Add(row);
            var left = Kit.Col(); left.style.flexGrow = 1;
            row.Add(left);
            var lh = Kit.Row();
            lh.Add(Kit.Caps("Propostas na mesa", 11, Kit.Soft));
            lh.Add(Kit.Spacer());
            lh.Add(Kit.Chip("Termos fixos — é pegar ou largar", Kit.Soft, false, Kit.IcInfo));
            left.Add(lh);
            left.Add(Enter(Offer(Kit.IcBriefcase, Kit.Blue, "Outsourcing", "GigaPublish quer um Puzzle pequeno", "R$ 60.000",
                new[] { ("Prazo: 14 semanas", Kit.Orange), ("Multa: R$ 20.000", Kit.Red), ("Nota mínima 6", Kit.Blue) }, false)));
            left.Add(Enter(Offer(Kit.IcContract, Kit.Purple, "Publicação", "Orbital Games publica seu próximo jogo", "R$ 25.000 + 30% da receita",
                new[] { ("Exclusividade de gênero: 1 ano", Kit.Purple), ("Bônus se nota ≥ 8", Kit.Green) }, false)));
            left.Add(Enter(Offer(Kit.IcTrophy, Kit.Gold, "Patrocínio de evento", "Feira de games — estande da sua empresa", "− R$ 8.000 · +20 hype",
                new[] { ("Requer P&D: Patrocínio de eventos", Kit.Soft) }, true)));

            row.Add(Kit.Gap(18));
            var right = Kit.Col(); right.style.width = 400; right.style.flexShrink = 0;
            row.Add(right);
            right.Add(Kit.Caps("Contratos ativos", 11, Kit.Soft));
            var act = Kit.Tile(16); act.style.marginTop = 8; Kit.Pad(act, 14, 16);
            var ar = Kit.Row();
            ar.Add(Kit.Badge(Kit.IcDesktop, Kit.Teal, 36));
            var at = Kit.Col(); at.style.marginLeft = 12; at.style.flexGrow = 1;
            at.Add(Kit.Text("Exclusividade de plataforma", 14, Kit.Ink, true));
            at.Add(Kit.Text("Só PC por 6 meses · R$ 40.000 recebidos", 11, Kit.Soft));
            ar.Add(at);
            act.Add(ar);
            var bar = Kit.Bar(8, Kit.Teal, out var fill); bar.style.marginTop = 10;
            act.Add(bar);
            Grow(fill, 0.62f, 0.6f);
            var an = Kit.Row(); an.style.marginTop = 4;
            an.Add(Kit.Text("faltam 10 semanas", 11, Kit.Soft)); an.Add(Kit.Spacer());
            an.Add(Kit.Text("Rescindir: R$ 30.000", 11, Kit.Red, true));
            act.Add(an);
            right.Add(Enter(act));

            var net = Kit.Tile(16); net.style.marginTop = 12; Kit.Pad(net, 14, 16);
            net.Add(Kit.Text("O que libera mais propostas", 13, Kit.Ink, true));
            net.Add(Req(Kit.IcStar, "Reputação", "62 / 100", 0.62f, Kit.Gold));
            net.Add(Req(Kit.IcNetwork, "Networking do fundador", "nível 1", 0.2f, Kit.Sky));
            net.Add(Req(Kit.IcChartLine, "Valuation", "R$ 412K", 0.4f, Kit.Teal));
            right.Add(Enter(net));
        }

        private VisualElement Offer(string icon, Color c, string type, string title, string pay, (string text, Color color)[] clauses, bool locked)
        {
            var card = Kit.Outline(16); card.style.marginTop = 10; Kit.Pad(card, 14, 16);
            if (locked) card.style.opacity = 0.78f;
            var top = Kit.Row();
            top.Add(Kit.SquareBadge(locked ? Kit.IcLock : icon, locked ? Kit.Faint : c, 46));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.flexGrow = 1;
            t.Add(Kit.Text(type.ToUpperInvariant(), 10, locked ? Kit.Faint : c, true));
            t.Add(Kit.Text(title, 15, Kit.Ink, true));
            top.Add(t);
            top.Add(Kit.Text(pay, 15, pay.StartsWith("−") ? Kit.Red : Kit.Cash, true));
            card.Add(top);
            var bottom = Kit.Row(); bottom.style.marginTop = 10;
            foreach (var cl in clauses) { var chip = Kit.Chip(cl.text, cl.color); chip.style.marginRight = 6; bottom.Add(chip); }
            bottom.Add(Kit.Spacer());
            if (!locked)
            {
                var no = Kit.Button("Recusar", null, Kit.Sand, Kit.Ink, null, 34); no.style.marginRight = 8;
                bottom.Add(no);
                bottom.Add(Kit.Button("Aceitar", Kit.IcSignature, Kit.Teal, Color.white, null, 34));
            }
            card.Add(bottom);
            return card;
        }

        private VisualElement Req(string icon, string name, string value, float pct, Color c)
        {
            var col = Kit.Col(); col.style.marginTop = 9;
            var r = Kit.Row();
            var i = Kit.Icon(icon, 11, c); i.style.width = 18;
            r.Add(i);
            var l = Kit.Text(name, 12, Kit.Ink); l.style.marginLeft = 6;
            r.Add(l); r.Add(Kit.Spacer());
            r.Add(Kit.Text(value, 12, Kit.Ink, true));
            col.Add(r);
            var bar = Kit.Bar(6, c, out var fill); bar.style.marginTop = 3;
            col.Add(bar);
            Grow(fill, pct, 0.7f);
            return col;
        }

        private void BuildStatements()
        {
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            _host.Add(row);

            var left = Kit.Tile(18); left.style.width = 360; left.style.flexShrink = 0; left.style.alignItems = Align.Center; Kit.Pad(left, 18, 18);
            left.Add(Kit.Caps("Sentimento público", 11, Kit.Soft));
            var gw = new VisualElement(); gw.style.width = 280; gw.style.height = 160; gw.style.marginTop = 10;
            _gauge = Kit.Fill(); _gauge.generateVisualContent += DrawSentiment;
            gw.Add(_gauge);
            left.Add(gw);
            left.Add(Kit.Text("Otimista", 22, Kit.Green, true));
            var ln = Kit.Wrap("O público gosta de você — por enquanto. Declarações ruins viram crise.", 12, Kit.Soft);
            ln.style.unityTextAlign = TextAnchor.MiddleCenter; ln.style.marginTop = 6;
            left.Add(ln);
            left.Add(Kit.Divider(14));
            var cr = Kit.Row(); cr.style.alignSelf = Align.Stretch;
            cr.Add(Kit.Badge(Kit.IcComments, Kit.Pink, 30));
            var ct = Kit.Col(); ct.style.marginLeft = 10; ct.style.flexShrink = 1;
            ct.Add(Kit.Text("Carisma nível 1", 13, Kit.Ink, true));
            ct.Add(Kit.Wrap("Reduz a chance de a declaração sair pela culatra.", 11, Kit.Soft));
            cr.Add(ct);
            left.Add(cr);
            row.Add(Enter(left));
            Animate(0.5f, 1.2f, k => _needle = 0.72f * k, OutBack);

            row.Add(Kit.Gap(18));
            var right = Kit.Col(); right.style.flexGrow = 1;
            row.Add(right);
            right.Add(Kit.Caps("O que você quer dizer ao mundo?", 11, Kit.Soft));
            right.Add(Enter(Statement("Prometer o jogo do ano", "“Nosso próximo lançamento vai redefinir o gênero.”",
                new[] { ("Hype ▲▲", Kit.Pink), ("Risco 35%", Kit.Red), ("Efeito imediato", Kit.Soft) }, false)));
            right.Add(Enter(Statement("Elogiar a equipe", "“Tenho o melhor time que já trabalhei.”",
                new[] { ("Humor ▲", Kit.Green), ("Risco 5%", Kit.Green), ("Efeito imediato", Kit.Soft) }, false)));
            right.Add(Enter(Statement("Alfinetar um rival", "“Tem estúdio grande por aí que esqueceu como se faz jogo.”",
                new[] { ("Hype ▲", Kit.Pink), ("Rival fica hostil", Kit.Red), ("Risco 50%", Kit.Red), ("Efeito retardado", Kit.Soft) }, false)));
            right.Add(Enter(Statement("Discurso Afiado", "Declaração exclusiva de alto impacto.",
                new[] { ("Requer habilidade de Carisma", Kit.Soft) }, true)));
        }

        private static VisualElement Statement(string title, string quote, (string text, Color color)[] chips, bool locked)
        {
            var card = Kit.Outline(16); card.style.marginTop = 10; Kit.Pad(card, 14, 16);
            card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center;
            if (locked) card.style.opacity = 0.75f; else Kit.Hover(card, 1.02f, 0.98f);
            card.Add(Kit.Badge(locked ? Kit.IcLock : Kit.IcBullhorn, locked ? Kit.Track : Kit.A(Kit.Pink, 0.16f), 42, locked ? Kit.Faint : Kit.Pink));
            var t = Kit.Col(); t.style.marginLeft = 14; t.style.flexGrow = 1; t.style.flexShrink = 1;
            t.Add(Kit.Text(title, 15, Kit.Ink, true));
            t.Add(Kit.Wrap(quote, 12, Kit.Soft));
            var cr = Kit.Row(); cr.style.marginTop = 8;
            foreach (var c in chips) { var chip = Kit.Chip(c.text, c.color); chip.style.marginRight = 6; cr.Add(chip); }
            t.Add(cr);
            card.Add(t);
            if (!locked) card.Add(Kit.Button("Declarar", Kit.IcPaperPlane, Kit.Pink, Color.white, null, 36));
            return card;
        }

        protected override void OnTick(float dt)
        {
            if (_gauge != null) _gauge.MarkDirtyRepaint();
        }

        private void DrawSentiment(MeshGenerationContext mgc)
        {
            var r = _gauge.contentRect;
            if (r.width < 10f) return;
            var p = mgc.painter2D;
            Vector2 c = new Vector2(r.width * 0.5f, r.height - 14f);
            float rad = Mathf.Min(r.width * 0.5f, r.height) - 22f;
            p.lineCap = LineCap.Butt; p.lineWidth = 22f;
            Color[] cols = { Kit.Red, Kit.Orange, Kit.Gold, Kit.Green, Kit.Teal };
            for (int i = 0; i < cols.Length; i++)
            {
                p.strokeColor = cols[i];
                p.BeginPath(); p.Arc(c, rad, new Angle(180f + i * 36f + 1.5f), new Angle(180f + (i + 1) * 36f - 1.5f)); p.Stroke();
            }
            float wobble = Mathf.Sin(Clock * 2.4f) * 0.012f;
            float ang = 180f + 180f * Mathf.Clamp01(_needle + wobble);
            Vector2 tip = Kit.OnCircle(c, rad - 4f, ang);
            p.lineCap = LineCap.Round; p.lineWidth = 6f; p.strokeColor = Kit.Ink;
            p.BeginPath(); p.MoveTo(c); p.LineTo(tip); p.Stroke();
            Kit.Disc(p, c, 11f, Kit.Ink);
            Kit.Disc(p, c, 5f, Kit.Paper);
        }
    }

    /// <summary>Painel Fundador (GDD 14.1, 20.2): habilidades permanentes + temporárias da run e evolução visual.</summary>
    internal sealed class FounderScreen : ManagementScreen
    {
        private FounderArt _art;

        protected override int PanelIndex => 6;

        protected override void Build()
        {
            var content = Shell("Suas habilidades destravam opções nos eventos");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            content.Add(row);

            var left = Kit.Col(); left.style.width = 360; left.style.flexShrink = 0;
            row.Add(left);
            var stage = Kit.Tile(18); stage.style.height = 330; stage.style.alignItems = Align.Center; stage.style.justifyContent = Justify.FlexEnd;
            stage.style.overflow = Overflow.Hidden; Kit.Pad(stage, 0, 0); stage.style.backgroundColor = Kit.A(Kit.Sky, 0.16f);
            _art = new FounderArt(310) { HairStyle = 1, Accessory = 1 };
            stage.Add(_art);
            var plate = Kit.Card(0, 12); plate.style.position = Position.Absolute; plate.style.bottom = 14; plate.style.alignItems = Align.Center; Kit.Pad(plate, 8, 18);
            plate.Add(Kit.Text("David", 18, Kit.Ink, true));
            plate.Add(Kit.Text("CEO · Pixel Forja · Run 4", 11, Kit.Soft));
            stage.Add(plate);
            left.Add(Enter(stage));

            var ward = Kit.Tile(16); ward.style.marginTop = 12; Kit.Pad(ward, 14, 16);
            ward.Add(Kit.Text("Guarda-roupa", 13, Kit.Ink, true));
            var wr = Kit.Row(); wr.style.marginTop = 10;
            wr.Add(Item(Kit.IcUser, "Camiseta", true, true)); wr.Add(Item(Kit.IcHeadphones, "Headset", true, false));
            wr.Add(Item(Kit.IcUserTie, "Terno", false, false)); wr.Add(Item(Kit.IcCrown, "Coroa", false, false));
            ward.Add(wr);
            var wn = Kit.Text("O visual evolui conforme as runs e habilidades.", 11, Kit.Soft); wn.style.marginTop = 8;
            ward.Add(wn);
            left.Add(Enter(ward));

            var stats = Kit.Row(Align.Stretch); stats.style.marginTop = 12;
            stats.Add(MiniStat("Jogos nesta run", "5")); stats.Add(MiniStat("Melhor nota", "8,1")); stats.Add(MiniStat("Crises", "1"));
            left.Add(Enter(stats));

            row.Add(Kit.Gap(20));
            var right = Kit.Col(); right.style.flexGrow = 1;
            row.Add(right);
            var legend = Kit.Row(); legend.style.marginBottom = 4;
            legend.Add(Kit.Caps("Habilidades", 11, Kit.Soft)); legend.Add(Kit.Spacer());
            legend.Add(Pip(Kit.Ink)); var l1 = Kit.Text("permanente", 10, Kit.Soft, true); l1.style.marginLeft = 4; l1.style.marginRight = 12; legend.Add(l1);
            legend.Add(Pip(Kit.Gold)); var l2 = Kit.Text("ganho nesta run", 10, Kit.Soft, true); l2.style.marginLeft = 4; legend.Add(l2);
            right.Add(legend);
            right.Add(Enter(Skill(Kit.IcBriefcase, Kit.Cash, "Visão de Negócio", "Valuation, negociações e M&A", 2, 0)));
            right.Add(Enter(Skill(Kit.IcUsers, Kit.Orange, "Liderança", "Burnout, retenção e eficiência da equipe", 1, 1)));
            right.Add(Enter(Skill(Kit.IcCode, Kit.Sky, "Técnica", "Qualidade dos jogos, engines e P&D", 3, 0)));
            right.Add(Enter(Skill(Kit.IcBullhorn, Kit.Pink, "Marketing", "Hype, vendas e impacto das campanhas", 1, 0)));
            right.Add(Enter(Skill(Kit.IcComments, Kit.Purple, "Carisma", "Declarações públicas e imagem do CEO", 1, 2)));
            right.Add(Enter(Skill(Kit.IcNetwork, Kit.Teal, "Networking", "Contratos, talentos raros e informação", 0, 1)));
            right.Add(Kit.Spacer());
            var foot = Kit.Tile(14); foot.style.flexDirection = FlexDirection.Row; foot.style.alignItems = Align.Center;
            foot.Add(Kit.Badge(Kit.IcHourglass, Kit.Gold, 30));
            var ft = Kit.Wrap("Habilidades permanentes são compradas na árvore ao encerrar a run. As temporárias somem no reset.", 11, Kit.Soft);
            ft.style.marginLeft = 10; ft.style.flexShrink = 1; ft.style.flexGrow = 1;
            foot.Add(ft);
            right.Add(Enter(foot));
        }

        private static VisualElement Pip(Color c)
        {
            var p = new VisualElement(); p.style.width = 14; p.style.height = 8; Kit.Radius(p, 4); p.style.backgroundColor = c;
            return p;
        }

        private VisualElement Skill(string icon, Color c, string name, string desc, int permanent, int temporary)
        {
            var r = Kit.Outline(14); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginTop = 8; Kit.Pad(r, 11, 16);
            r.Add(Kit.Badge(icon, c, 42));
            var t = Kit.Col(); t.style.marginLeft = 14; t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 15, Kit.Ink, true));
            t.Add(Kit.Text(desc, 11, Kit.Soft));
            r.Add(t);
            var pips = Kit.Row();
            for (int i = 0; i < 6; i++)
            {
                var p = new VisualElement(); p.style.width = 26; p.style.height = 12; Kit.Radius(p, 6); p.style.marginLeft = 4;
                Color target = i < permanent ? c : i < permanent + temporary ? Kit.Gold : Kit.Track;
                p.style.backgroundColor = Kit.Track;
                pips.Add(p);
                if (i < permanent + temporary)
                    Animate(0.6f + i * 0.08f, 0.3f, k =>
                    {
                        p.style.backgroundColor = Color.Lerp(Kit.Track, target, k);
                        float s = 1f + 0.3f * Mathf.Sin(k * Mathf.PI);
                        p.style.scale = new Scale(new Vector2(s, s));
                    }, Linear);
            }
            r.Add(pips);
            var lv = Kit.Text((permanent + temporary).ToString(), 20, Kit.Ink, true); lv.style.width = 36; lv.style.unityTextAlign = TextAnchor.MiddleRight;
            r.Add(lv);
            return r;
        }

        private static VisualElement Item(string icon, string name, bool unlocked, bool equipped)
        {
            var c = Kit.Col(); c.style.alignItems = Align.Center; c.style.marginRight = 14;
            var b = Kit.SquareBadge(unlocked ? icon : Kit.IcLock, unlocked ? Color.white : Kit.Track, 54, 14);
            ((Label)b[0]).style.color = unlocked ? Kit.Sky : Kit.Faint;
            if (equipped) Kit.Border(b, Kit.Sky, 3);
            c.Add(b);
            var n = Kit.Text(name, 10, unlocked ? Kit.Ink : Kit.Faint, true); n.style.marginTop = 4;
            c.Add(n);
            return c;
        }

        private static VisualElement MiniStat(string cap, string val)
        {
            var c = Kit.Tile(12); c.style.flexGrow = 1; c.style.flexBasis = 0; c.style.marginRight = 8; c.style.alignItems = Align.Center; Kit.Pad(c, 10, 6);
            c.Add(Kit.Text(val, 20, Kit.Ink, true));
            c.Add(Kit.Text(cap, 10, Kit.Soft, true));
            return c;
        }

        protected override void OnTick(float dt) => _art.Tick(dt);
    }
}
