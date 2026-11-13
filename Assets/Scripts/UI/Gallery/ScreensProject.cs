using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>Novo projeto — Conceito (GDD 5.2 fase 1): nome, gênero, subgênero, tema, plataforma, escopo, engine.</summary>
    internal sealed class NewProjectScreen : GalleryScreen
    {
        protected override void Build()
        {
            Scrim();
            var body = Window("Novo projeto", Kit.IcPlus, Kit.Blue, 1260, 780, out _, "Conceito — o que vamos fazer?");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            var form = Kit.Col(); form.style.flexGrow = 1; form.style.flexShrink = 1;
            row.Add(form);

            var top = Kit.Row(); top.style.marginBottom = 14;
            var nameCol = Kit.Col();
            Section(nameCol, "Nome do jogo");
            nameCol.Add(Widgets.Field("Lendas de Aethel II", 380));
            top.Add(nameCol);
            top.Add(Kit.Gap(24));
            var seqCol = Kit.Col();
            Section(seqCol, "Sequência de");
            seqCol.Add(Widgets.Select(new[] { "Lendas de Aethel (nota 8,1)", "Nenhuma — IP novo", "Turbo Kart 64 (nota 6,4)" }, 0, 320));
            top.Add(seqCol);
            form.Add(Enter(top));

            Section(form, "Gênero");
            form.Add(Enter(TileRow(Kit.Blue, 0,
                OptionTile(Kit.IcDragon, Kit.Blue, "Action RPG", null),
                OptionTile(Kit.IcGamepad, Kit.Blue, "Plataforma", null),
                OptionTile(Kit.IcCubes, Kit.Blue, "Puzzle", null),
                OptionTile(Kit.IcPlane, Kit.Blue, "Simulador", null),
                OptionTile(Kit.IcFlag, Kit.Blue, "Estratégia RTS", null),
                OptionTile(Kit.IcBuilding, Kit.Blue, "Tycoon", null),
                OptionTile(Kit.IcLock, Kit.Faint, "Roguelite", "P&D", true))));

            var subRow = Kit.Row(); subRow.style.marginTop = 12; subRow.style.marginBottom = 14;
            var subLbl = Kit.Caps("Subgênero", 11, Kit.Soft); subLbl.style.marginRight = 14;
            subRow.Add(subLbl);
            subRow.Add(Widgets.Segmented(new[] { "Nenhum", "Souls-like", "Isométrico ARPG" }, 1, null, Kit.Blue));
            form.Add(Enter(subRow));

            Section(form, "Tema");
            form.Add(Enter(TileRow(Kit.Pink, 0,
                ThemeTile(Kit.IcHat, Kit.Purple, "Fantasia Medieval"),
                ThemeTile(Kit.IcChip, Kit.Pink, "Cyberpunk"),
                ThemeTile(Kit.IcGhost, Kit.RGB(70, 76, 96), "Horror Sobrenatural"),
                ThemeTile(Kit.IcFutbol, Kit.Green, "Futebol"),
                ThemeTile(Kit.IcShuttle, Kit.Sky, "Espaço Sideral"),
                ThemeTile(Kit.IcCar, Kit.Orange, "Corrida de Carros"),
                ThemeTile(Kit.IcLock, Kit.Faint, "Pirataria", true))));

            var bottom = Kit.Row(Align.FlexStart); bottom.style.marginTop = 14;
            var platCol = Kit.Col();
            Section(platCol, "Plataforma");
            platCol.Add(TileRow(Kit.Teal, 0,
                OptionTile(Kit.IcDesktop, Kit.Teal, "PC", "R$ 5.000"),
                OptionTile(Kit.IcGamepad, Kit.Teal, "Game Poy", "R$ 3.000"),
                OptionTile(Kit.IcLock, Kit.Faint, "MegaDrive Pro", "Ano 5", true)));
            bottom.Add(platCol);
            bottom.Add(Kit.Gap(28));
            var scopeCol = Kit.Col();
            Section(scopeCol, "Escopo");
            scopeCol.Add(Widgets.Segmented(new[] { "Pequeno", "Médio", "Grande", "AAA" }, 0, null, Kit.Teal, new[] { false, false, true, true }));
            var engLbl = Kit.Caps("Engine", 11, Kit.Soft); engLbl.style.marginTop = 14; engLbl.style.marginBottom = 8;
            scopeCol.Add(engLbl);
            scopeCol.Add(Widgets.Select(new[] { "Engine Básica · 200 EB", "Forja 2D · 600 EB" }, 0, 300));
            bottom.Add(scopeCol);
            form.Add(Enter(bottom));

            row.Add(Kit.Gap(24));
            row.Add(Enter(Summary(), 0.45f));
        }

        private VisualElement Summary()
        {
            var s = Kit.Tile(16); s.style.width = 310; s.style.flexShrink = 0; Kit.Pad(s, 18, 18);
            s.Add(Kit.Caps("Resumo", 11, Kit.Soft));
            var name = Kit.Wrap("Lendas de Aethel II", 20, Kit.Ink, true); name.style.marginTop = 6;
            s.Add(name);
            s.Add(Kit.Text("Action RPG · Souls-like", 13, Kit.Soft));
            s.Add(Kit.Text("Fantasia Medieval · PC", 13, Kit.Soft));
            s.Add(Kit.Divider(12));
            s.Add(Line(Kit.IcUsers, "Equipe", "3 pessoas"));
            s.Add(Line(Kit.IcSliders, "Teto por parâmetro", "300 pts"));
            s.Add(Line(Kit.IcClock, "Duração estimada", "~20 semanas"));
            s.Add(Line(Kit.IcTag, "Licença (PC)", "R$ 5.000"));
            s.Add(Line(Kit.IcCoins, "Custo por semana", "R$ 7.800"));
            s.Add(Kit.Divider(12));

            var hype = Kit.Row(Align.FlexStart);
            hype.Add(Kit.Badge(Kit.IcFire, Kit.Pink, 28));
            var ht = Kit.Col(); ht.style.marginLeft = 10; ht.style.flexShrink = 1;
            ht.Add(Kit.Text("Sequência: hype inicial +18", 13, Kit.Ink, true));
            ht.Add(Kit.Wrap("Média da franquia: 8,1", 11, Kit.Soft));
            hype.Add(ht);
            s.Add(hype);

            var compat = Kit.Row(Align.FlexStart); compat.style.marginTop = 10;
            compat.Add(Kit.Badge(Kit.IcQuestion, Kit.Faint, 28));
            var ctx = Kit.Col(); ctx.style.marginLeft = 10; ctx.style.flexShrink = 1;
            ctx.Add(Kit.Text("Combinação: ?", 13, Kit.Ink, true));
            ctx.Add(Kit.Wrap("Você descobre durante o desenvolvimento.", 11, Kit.Soft));
            compat.Add(ctx);
            s.Add(compat);

            s.Add(Kit.Spacer());
            s.Add(Kit.Text("Custo inicial", 11, Kit.Soft, true));
            var total = Kit.Text("R$ 0", 28, Kit.Cash, true);
            s.Add(total);
            CountUp(total, 0, 5000, 0.8f, 0.7f, Kit.Money);
            var start = Kit.Primary("Iniciar projeto", Kit.IcRocket); start.style.marginTop = 12;
            s.Add(start);
            var cancel = Kit.Ghost("Cancelar"); cancel.style.marginTop = 8;
            s.Add(cancel);
            return s;
        }

        private static VisualElement Line(string icon, string label, string value)
        {
            var r = Kit.Row(); r.style.marginBottom = 7;
            var i = Kit.Icon(icon, 11, Kit.Soft); i.style.width = 18;
            r.Add(i);
            var l = Kit.Text(label, 12, Kit.Soft); l.style.marginLeft = 6;
            r.Add(l);
            r.Add(Kit.Spacer());
            r.Add(Kit.Text(value, 13, Kit.Ink, true));
            return r;
        }

        private static VisualElement TileRow(Color accent, int selected, params VisualElement[] tiles)
        {
            var row = Kit.Row(Align.Stretch);
            var selectable = new List<VisualElement>();
            foreach (var t in tiles)
            {
                t.style.marginRight = 10;
                row.Add(t);
                if (t.userData == null) selectable.Add(t);
            }
            Widgets.SelectGroup(selectable, selected, accent);
            return row;
        }

        private static VisualElement OptionTile(string icon, Color color, string label, string sub, bool locked = false)
        {
            var t = Kit.Outline(14);
            t.style.width = 112; t.style.alignItems = Align.Center; Kit.Pad(t, 12, 6);
            t.Add(Kit.Badge(icon, locked ? Kit.Track : Kit.A(color, 0.16f), 44, locked ? Kit.Faint : color));
            var l = Kit.Wrap(label, 12, locked ? Kit.Faint : Kit.Ink, true);
            l.style.marginTop = 8; l.style.unityTextAlign = TextAnchor.MiddleCenter;
            t.Add(l);
            if (sub != null) t.Add(Kit.Text(sub, 11, locked ? Kit.Faint : Kit.Cash, true));
            if (locked) { t.userData = "locked"; t.style.opacity = 0.75f; }
            return t;
        }

        private static VisualElement ThemeTile(string icon, Color color, string label, bool locked = false)
        {
            var t = Kit.Outline(14);
            t.style.width = 112; Kit.Pad(t, 0, 0); t.style.overflow = Overflow.Hidden;
            var art = new VisualElement();
            art.style.height = 58; art.style.backgroundColor = locked ? Kit.Track : color;
            art.style.alignItems = Align.Center; art.style.justifyContent = Justify.Center;
            art.Add(Kit.Icon(icon, 26, locked ? Kit.Faint : Kit.A(Color.white, 0.95f)));
            t.Add(art);
            var l = Kit.Wrap(label, 12, locked ? Kit.Faint : Kit.Ink, true);
            l.style.unityTextAlign = TextAnchor.MiddleCenter; Kit.Pad(l, 7, 4);
            t.Add(l);
            if (locked) { t.userData = "locked"; t.style.opacity = 0.75f; }
            return t;
        }
    }

    /// <summary>Alocação de pontos por fase de produção (GDD 5.2 sliders, 5.3 pontos, 5.4 EB, 5.6 crunch).</summary>
    internal sealed class PhaseAllocationScreen : GalleryScreen
    {
        private static readonly (string name, string icon)[] Phases =
        {
            ("Pré-produção", Kit.IcBulb), ("Arte e Visual", Kit.IcBrush), ("Áudio", Kit.IcMusic),
            ("Tecnologia", Kit.IcCode), ("Conteúdo", Kit.IcLayers)
        };

        private readonly float[] _values = { 0.34f, 0.2f, 0.26f };
        private Label _sumLabel;
        private VisualElement _sumFill, _activePhase;
        private const int Budget = 300;

        protected override void Build()
        {
            Scrim();
            Color col = Kit.PhaseColors[0];
            var body = Window("Pré-produção", Kit.IcBulb, col, 1120, 720, out _, "Fase 1 de 5 — onde a equipe coloca o esforço?");
            body.Add(Enter(PhaseStrip()));

            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1; row.style.marginTop = 16;
            body.Add(row);

            var left = Kit.Col(); left.style.flexGrow = 1;
            row.Add(left);

            var sumRow = Kit.Row();
            sumRow.Add(Kit.Caps("Pontos da equipe", 11, Kit.Soft));
            sumRow.Add(Kit.Spacer());
            _sumLabel = Kit.Text("", 15, Kit.Ink, true);
            sumRow.Add(_sumLabel);
            left.Add(Enter(sumRow));
            var bar = Kit.Bar(12, col, out _sumFill); bar.style.marginTop = 6; bar.style.marginBottom = 16;
            left.Add(Enter(bar));

            left.Add(Enter(AttrRow(0, Kit.IcSliders, "Game Design", "Regras, sistemas e o loop central", col)));
            left.Add(Enter(AttrRow(1, Kit.IcBook, "Narrativa", "História, mundo e personagens", col)));
            left.Add(Enter(AttrRow(2, Kit.IcBrush, "Direção de Arte", "Identidade visual do jogo", col)));
            left.Add(Enter(LockedRow(Kit.IcGamepad, "Game Feel", "Projetos Médio ou maior")));
            left.Add(Enter(LockedRow(Kit.IcExpand, "Escopo", "Projetos Grande ou maior")));
            UpdateSum();

            row.Add(Kit.Gap(24));
            var right = Kit.Col(); right.style.width = 330; right.style.flexShrink = 0;
            row.Add(right);

            var eb = Kit.Tile();
            var ebHead = Kit.Row();
            ebHead.Add(Kit.Badge(Kit.IcChip, Kit.Sky, 28));
            var ebT = Kit.Text("Engine Budget", 14, Kit.Ink, true); ebT.style.marginLeft = 10;
            ebHead.Add(ebT); ebHead.Add(Kit.Spacer());
            ebHead.Add(Kit.Text("200 / 200 EB", 13, Kit.Sky, true));
            eb.Add(ebHead);
            var ebBar = Kit.Bar(8, Kit.Sky, out var ebFill); ebBar.style.marginTop = 8; ebBar.style.marginBottom = 10;
            eb.Add(ebBar);
            Grow(ebFill, 1f, 0.6f);
            eb.Add(Feature("Renderizador 2D básico", "100 EB · R$ 100/sem", true));
            eb.Add(Feature("Animação spritesheet", "100 EB · R$ 100/sem", true));
            eb.Add(Feature("Física 2D básica", "100 EB · R$ 100/sem", false));
            right.Add(Enter(eb));

            var crunch = Kit.Tile(); crunch.style.marginTop = 12; crunch.style.backgroundColor = Kit.A(Kit.Red, 0.10f);
            var cr = Kit.Row();
            cr.Add(Kit.Badge(Kit.IcBolt, Kit.Red, 28));
            var crT = Kit.Col(); crT.style.marginLeft = 10; crT.style.flexGrow = 1;
            crT.Add(Kit.Text("Crunch", 14, Kit.Ink, true));
            crT.Add(Kit.Wrap("Acelera a fase · mais burnout e bugs · pode vazar para a imprensa", 11, Kit.Soft));
            cr.Add(crT);
            cr.Add(Widgets.Toggle(false));
            crunch.Add(cr);
            right.Add(Enter(crunch));

            var sum = Kit.Tile(); sum.style.marginTop = 12;
            sum.Add(StatLine(Kit.IcClock, "Duração da fase", "3 semanas"));
            sum.Add(StatLine(Kit.IcCoins, "Custo por semana", "R$ 7.800"));
            sum.Add(StatLine(Kit.IcBug, "Risco de bugs", "Baixo"));
            right.Add(Enter(sum));

            right.Add(Kit.Spacer());
            var go = Kit.Button("Começar fase", Kit.IcPlay, col, Color.white); go.style.marginTop = 12;
            right.Add(Enter(go));
        }

        private VisualElement PhaseStrip()
        {
            var strip = Kit.Row(); strip.style.justifyContent = Justify.Center;
            for (int i = 0; i < Phases.Length; i++)
            {
                bool on = i == 0;
                Color c = Kit.PhaseColors[i];
                var item = Kit.Row();
                var circle = Kit.Badge(Phases[i].icon, on ? c : Kit.Sand, on ? 44 : 34, on ? Color.white : Kit.A(c, 0.6f));
                if (on) _activePhase = circle;
                item.Add(circle);
                var l = Kit.Text(Phases[i].name, on ? 14 : 12, on ? Kit.Ink : Kit.Soft, on); l.style.marginLeft = 8;
                item.Add(l);
                strip.Add(item);
                if (i < Phases.Length - 1)
                {
                    var dash = new VisualElement();
                    dash.style.width = 38; dash.style.height = 3; Kit.Radius(dash, 2);
                    dash.style.backgroundColor = Kit.Track; dash.style.marginLeft = 12; dash.style.marginRight = 12;
                    strip.Add(dash);
                }
            }
            return strip;
        }

        private VisualElement AttrRow(int index, string icon, string name, string desc, Color col)
        {
            var row = Kit.Outline(14); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.style.marginBottom = 10; Kit.Pad(row, 12, 16);
            row.Add(Kit.Badge(icon, col, 44));
            var t = Kit.Col(); t.style.marginLeft = 12; t.style.width = 220; t.style.flexShrink = 0;
            t.Add(Kit.Text(name, 15, Kit.Ink, true));
            t.Add(Kit.Wrap(desc, 11, Kit.Soft));
            row.Add(t);
            var val = Kit.Text("", 20, col, true); val.style.width = 60; val.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(Widgets.Slider(_values[index], col, v =>
            {
                _values[index] = v;
                val.text = Mathf.RoundToInt(v * Budget).ToString();
                UpdateSum();
            }));
            val.text = Mathf.RoundToInt(_values[index] * Budget).ToString();
            row.Add(val);
            return row;
        }

        private static VisualElement LockedRow(string icon, string name, string reason)
        {
            var row = Kit.Tile(14); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.style.marginBottom = 10; Kit.Pad(row, 10, 16); row.style.opacity = 0.8f;
            row.Add(Kit.Badge(icon, Kit.Track, 36, Kit.Faint));
            var n = Kit.Text(name, 14, Kit.Faint, true); n.style.marginLeft = 12;
            row.Add(n);
            row.Add(Kit.Spacer());
            row.Add(Kit.Chip(reason, Kit.Soft, false, Kit.IcLock));
            return row;
        }

        private static VisualElement Feature(string name, string cost, bool on)
        {
            var r = Kit.Row(); r.style.marginTop = 6;
            var t = Kit.Col(); t.style.flexGrow = 1;
            t.Add(Kit.Text(name, 12, Kit.Ink, true));
            t.Add(Kit.Text(cost, 10, Kit.Soft));
            r.Add(t);
            r.Add(Widgets.Toggle(on));
            return r;
        }

        private static VisualElement StatLine(string icon, string label, string value)
        {
            var r = Kit.Row(); r.style.marginBottom = 5; r.style.marginTop = 2;
            var i = Kit.Icon(icon, 12, Kit.Soft); i.style.width = 18;
            r.Add(i);
            var l = Kit.Text(label, 12, Kit.Soft); l.style.marginLeft = 6;
            r.Add(l); r.Add(Kit.Spacer());
            r.Add(Kit.Text(value, 13, Kit.Ink, true));
            return r;
        }

        private void UpdateSum()
        {
            float total = 0f;
            foreach (float v in _values) total += v * Budget;
            int pts = Mathf.RoundToInt(total);
            bool over = pts > Budget;
            _sumLabel.text = pts + " / " + Budget + " pts";
            _sumLabel.style.color = over ? Kit.Red : Kit.Ink;
            _sumFill.style.width = Length.Percent(Mathf.Clamp01(total / Budget) * 100f);
            _sumFill.style.backgroundColor = over ? Kit.Red : Kit.PhaseColors[0];
        }

        protected override void OnTick(float dt)
        {
            if (_activePhase == null) return;
            float s = 1f + 0.06f * Mathf.Sin(Clock * 4f);
            _activePhase.style.scale = new Scale(new Vector2(s, s));
        }
    }

    /// <summary>Marketing do projeto em andamento (GDD 5.5 e 10.1/10.2): hype, wishlists e campanhas.</summary>
    internal sealed class MarketingScreen : GalleryScreen
    {
        private Label _flame;
        private VisualElement _activeFill;

        protected override void Build()
        {
            Scrim();
            var body = Window("Marketing", Kit.IcBullhorn, Kit.Pink, 1100, 700, out _, "Lendas de Aethel II — em desenvolvimento");

            var top = Kit.Row(Align.Stretch);
            body.Add(top);

            var hype = Kit.Tile(16); hype.style.flexGrow = 1; Kit.Pad(hype, 16, 18);
            var hr = Kit.Row();
            _flame = Kit.Icon(Kit.IcFire, 30, Kit.Pink);
            hr.Add(_flame);
            var hv = Kit.Text("0", 34, Kit.Pink, true); hv.style.marginLeft = 10;
            hr.Add(hv);
            var hc = Kit.Col(); hc.style.marginLeft = 10;
            hc.Add(Kit.Text("HYPE", 12, Kit.Soft, true));
            hc.Add(Kit.Text("Vendas boas no lançamento", 12, Kit.Ink, true));
            hr.Add(hc);
            hr.Add(Kit.Spacer());
            hr.Add(Kit.Chip("cai 2 por semana sem campanha", Kit.Soft, false, Kit.IcArrowDown));
            hype.Add(hr);
            CountUp(hv, 0, 64, 0.4f, 0.9f, v => Mathf.RoundToInt(v).ToString());

            var zones = Kit.Row(Align.Stretch); zones.style.marginTop = 12; zones.style.height = 14;
            (string n, float w, Color c)[] zs = { ("Fracas", 20, Kit.Faint), ("Medianas", 30, Kit.Orange), ("Boas", 30, Kit.Pink), ("Excelentes", 20, Kit.Red) };
            foreach (var z in zs)
            {
                var seg = new VisualElement(); seg.style.flexGrow = z.w; seg.style.flexBasis = 0;
                seg.style.backgroundColor = Kit.A(z.c, 0.3f); seg.style.marginRight = 2; Kit.Radius(seg, 4);
                zones.Add(seg);
            }
            var marker = new VisualElement();
            marker.style.position = Position.Absolute; marker.style.top = -4; marker.style.width = 6; marker.style.height = 22;
            marker.style.backgroundColor = Kit.Ink; Kit.Radius(marker, 3);
            zones.Add(marker);
            Animate(0.4f, 0.9f, k => marker.style.left = Length.Percent(64f * k));
            hype.Add(zones);
            var zl = Kit.Row(); zl.style.marginTop = 4;
            foreach (var z in zs)
            {
                var t = Kit.Text(z.n, 10, Kit.Soft, true); t.style.flexGrow = z.w; t.style.flexBasis = 0;
                t.style.unityTextAlign = TextAnchor.MiddleCenter;
                zl.Add(t);
            }
            hype.Add(zl);
            top.Add(Enter(hype));
            top.Add(Kit.Gap(14));

            var wish = Kit.Tile(16); wish.style.width = 270; Kit.Pad(wish, 16, 18);
            var wr = Kit.Row();
            wr.Add(Kit.Badge(Kit.IcHeart, Kit.Red, 34));
            var wc = Kit.Col(); wc.style.marginLeft = 10;
            wc.Add(Kit.Text("WISHLISTS", 12, Kit.Soft, true));
            var wv = Kit.Text("0", 26, Kit.Ink, true);
            wc.Add(wv);
            wr.Add(wc);
            wish.Add(wr);
            CountUp(wv, 0, 8400, 0.5f, 1.1f, v => Kit.Short(v));
            var wn = Kit.Wrap("≈ 6.700 vendas na semana de lançamento", 12, Kit.Soft); wn.style.marginTop = 10;
            wish.Add(wn);
            top.Add(Enter(wish));

            var sec = Kit.Caps("Ações de marketing", 11, Kit.Soft); sec.style.marginTop = 18; sec.style.marginBottom = 10;
            body.Add(sec);
            var grid = Kit.Row(Align.Stretch); grid.style.flexWrap = Wrap.Wrap;
            body.Add(grid);
            grid.Add(Enter(Campaign(Kit.IcBullhorn, "Anúncio oficial", "R$ 2.000", "+10 hype", 2)));
            grid.Add(Enter(Campaign(Kit.IcHeart, "Campanha de wishlist", "R$ 4.500", "+2.000 wishlists", 1)));
            grid.Add(Enter(Campaign(Kit.IcVideo, "Devlog público", "R$ 800", "+4 hype por 4 sem", 0)));
            grid.Add(Enter(Campaign(Kit.IcNews, "Press kit", "R$ 1.500", "+6 hype", 0)));
            grid.Add(Enter(Campaign(Kit.IcGamepad, "Demo jogável", "R$ 6.000", "+15 hype · revela bugs", 0)));
            grid.Add(Enter(Campaign(Kit.IcUsers, "Beta fechado", "R$ 3.000", "−30% bugs · +5 hype", 0)));
            grid.Add(Enter(Campaign(Kit.IcStar, "Influencers", "R$ 12.000", "+25 hype", 3)));
            grid.Add(Enter(Campaign(Kit.IcTrophy, "Game show", "R$ 20.000", "+35 hype", 3)));
        }

        // state: 0 disponível, 1 em andamento, 2 concluída, 3 bloqueada (P&D)
        private VisualElement Campaign(string icon, string name, string cost, string effect, int state)
        {
            bool locked = state == 3;
            var c = Kit.Outline(14); c.style.width = 250; c.style.marginRight = 12; c.style.marginBottom = 12; Kit.Pad(c, 14, 14);
            if (!locked) Kit.Hover(c, 1.03f, 0.97f);
            var r = Kit.Row();
            r.Add(Kit.Badge(locked ? Kit.IcLock : icon, locked ? Kit.Track : Kit.A(Kit.Pink, 0.16f), 38, locked ? Kit.Faint : Kit.Pink));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexShrink = 1;
            t.Add(Kit.Text(name, 14, locked ? Kit.Faint : Kit.Ink, true));
            t.Add(Kit.Text(effect, 11, locked ? Kit.Faint : Kit.Soft));
            r.Add(t);
            c.Add(r);
            var f = Kit.Row(); f.style.marginTop = 12;
            switch (state)
            {
                case 0:
                    f.Add(Kit.Text(cost, 14, Kit.Cash, true)); f.Add(Kit.Spacer());
                    f.Add(Kit.Button("Lançar", null, Kit.Pink, Color.white, null, 30));
                    break;
                case 1:
                    var col = Kit.Col(); col.style.flexGrow = 1;
                    col.Add(Kit.Text("Em andamento · 2 de 3 semanas", 11, Kit.Pink, true));
                    var bar = Kit.Bar(8, Kit.Pink, out _activeFill); bar.style.marginTop = 5;
                    col.Add(bar);
                    f.Add(col);
                    Kit.BorderColor(c, Kit.Pink);
                    break;
                case 2:
                    f.Add(Kit.Chip("Concluída", Kit.Green, false, Kit.IcCheck));
                    break;
                default:
                    f.Add(Kit.Chip("Requer P&D: Influencer Network", Kit.Soft, false, Kit.IcFlask));
                    c.style.opacity = 0.8f;
                    break;
            }
            c.Add(f);
            return c;
        }

        protected override void OnTick(float dt)
        {
            float s = 1f + 0.07f * Mathf.Sin(Clock * 6f);
            _flame.style.scale = new Scale(new Vector2(s, s));
            if (_activeFill != null) _activeFill.style.width = Length.Percent(60f + 12f * Mathf.PingPong(Clock * 0.3f, 1f));
        }
    }

    /// <summary>Lançamento (GDD 5.7): preço, budget de marketing, data/janela e aviso de bugs.</summary>
    internal sealed class LaunchScreen : GalleryScreen
    {
        private VisualElement _launchIcon;
        private int _price = 39;

        protected override void Build()
        {
            Scrim();
            var body = Window("Lançamento", Kit.IcRocket, Kit.Blue, 1100, 700, out _, "Lendas de Aethel II está pronto. Como vamos lançar?");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);
            var left = Kit.Col(); left.style.flexGrow = 1;
            row.Add(left);

            // preço
            Section(left, "Preço");
            var priceRow = Kit.Row(); priceRow.style.marginBottom = 18;
            var priceLbl = Kit.Text("R$ " + _price, 34, Kit.Ink, true);
            priceLbl.style.width = 150; priceLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            priceRow.Add(Kit.RoundButton(Kit.IcMinus, Kit.Sand, Kit.Ink, 40, () => { _price = Mathf.Max(5, _price - 5); priceLbl.text = "R$ " + _price; }));
            priceRow.Add(priceLbl);
            priceRow.Add(Kit.RoundButton(Kit.IcPlus, Kit.Sand, Kit.Ink, 40, () => { _price = Mathf.Min(99, _price + 5); priceLbl.text = "R$ " + _price; }));
            var hint = Kit.Chip("Faixa comum para Pequeno: R$ 29 – 49", Kit.Blue, false, Kit.IcInfo); hint.style.marginLeft = 18;
            priceRow.Add(hint);
            left.Add(Enter(priceRow));

            // marketing de lançamento
            Section(left, "Budget de marketing de lançamento");
            var mk = Kit.Row(); mk.style.marginBottom = 18;
            var mkVal = Kit.Text(Kit.Money(8000), 16, Kit.Cash, true); mkVal.style.width = 110; mkVal.style.unityTextAlign = TextAnchor.MiddleRight;
            mk.Add(Widgets.Slider(0.4f, Kit.Pink, v => mkVal.text = Kit.Money(Mathf.Round(v * 20f) * 1000f)));
            mk.Add(mkVal);
            left.Add(Enter(mk));

            // janela
            Section(left, "Data de lançamento");
            var weeks = Kit.Row(Align.Stretch); weeks.style.marginBottom = 8;
            var cells = new List<VisualElement>();
            (string tag, Color c)[] marks =
            {
                ("Agora", Kit.Soft), (null, Kit.Soft), ("Rival lança RPG", Kit.Red), (null, Kit.Soft),
                ("Janela ideal", Kit.Green), ("Janela ideal", Kit.Green), (null, Kit.Soft), ("Feira de games", Kit.Purple)
            };
            for (int i = 0; i < marks.Length; i++)
            {
                var cell = Kit.Outline(12); cell.style.flexGrow = 1; cell.style.flexBasis = 0; cell.style.marginRight = 6;
                cell.style.alignItems = Align.Center; Kit.Pad(cell, 8, 2); cell.style.minHeight = 78;
                cell.Add(Kit.Text("Sem " + (37 + i), 12, Kit.Ink, true));
                cell.Add(Kit.Text(i == 0 ? "hoje" : "+" + i + " sem", 10, Kit.Soft));
                if (marks[i].tag != null)
                {
                    var dot = new VisualElement(); dot.style.width = 8; dot.style.height = 8; Kit.Radius(dot, 4);
                    dot.style.backgroundColor = marks[i].c; dot.style.marginTop = 6;
                    cell.Add(dot);
                    var tg = Kit.Wrap(marks[i].tag, 9, marks[i].c, true); tg.style.unityTextAlign = TextAnchor.MiddleCenter; tg.style.marginTop = 2;
                    cell.Add(tg);
                }
                cells.Add(cell); weeks.Add(cell);
            }
            Widgets.SelectGroup(cells, 4, Kit.Blue);
            left.Add(Enter(weeks));
            left.Add(Enter(Kit.Wrap("Esperar custa: o hype cai a cada semana parada e a equipe continua recebendo.", 11, Kit.Soft)));

            var dlc = Widgets.FormRow("Anunciar DLC no lançamento", Widgets.Toggle(false), "Mantém as vendas por mais tempo");
            dlc.style.marginTop = 16;
            left.Add(Enter(dlc));

            row.Add(Kit.Gap(24));
            var right = Kit.Col(); right.style.width = 320; right.style.flexShrink = 0;
            row.Add(right);

            var bugs = Kit.Tile(); bugs.style.backgroundColor = Kit.A(Kit.Red, 0.10f);
            var br = Kit.Row();
            br.Add(Kit.Badge(Kit.IcBug, Kit.Red, 34));
            var bt = Kit.Col(); bt.style.marginLeft = 10; bt.style.flexShrink = 1;
            bt.Add(Kit.Text("12 bugs conhecidos", 14, Kit.Ink, true));
            bt.Add(Kit.Wrap("Lançar com bugs derruba a nota da crítica.", 11, Kit.Soft));
            br.Add(bt);
            bugs.Add(br);
            var fix = Kit.Ghost("Corrigir antes (+2 semanas)", Kit.IcWrench); fix.style.marginTop = 10;
            bugs.Add(fix);
            right.Add(Enter(bugs));

            var fore = Kit.Tile(); fore.style.marginTop = 12;
            fore.Add(Kit.Caps("Previsão", 11, Kit.Soft));
            fore.Add(Forecast(Kit.IcFire, Kit.Pink, "Hype no lançamento", "58", "Boas"));
            fore.Add(Forecast(Kit.IcHeart, Kit.Red, "Wishlists", "8,4K", "≈ 6,7K vendas"));
            fore.Add(Forecast(Kit.IcDesktop, Kit.Teal, "Plataforma", "PC", "Popularidade média"));
            right.Add(Enter(fore));

            right.Add(Kit.Spacer());
            var go = Kit.Button("Lançar jogo", Kit.IcRocket, Kit.Blue, Color.white, null, 56); go.style.marginTop = 12;
            _launchIcon = go[0];
            right.Add(Enter(go));
        }

        private static VisualElement Forecast(string icon, Color c, string label, string value, string note)
        {
            var r = Kit.Row(); r.style.marginTop = 10;
            r.Add(Kit.Badge(icon, Kit.A(c, 0.16f), 30, c));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexGrow = 1;
            t.Add(Kit.Text(label, 11, Kit.Soft));
            t.Add(Kit.Text(note, 11, Kit.Ink, true));
            r.Add(t);
            r.Add(Kit.Text(value, 18, Kit.Ink, true));
            return r;
        }

        protected override void OnTick(float dt)
        {
            if (_launchIcon == null) return;
            _launchIcon.style.translate = new Translate(Mathf.Sin(Clock * 9f) * 1.5f, Mathf.Sin(Clock * 3f) * -3f, 0);
            _launchIcon.style.rotate = new Rotate(new Angle(Mathf.Sin(Clock * 3f) * 6f));
        }
    }

    /// <summary>Notas da crítica reveladas uma a uma + vendas por semana (GDD 5.7, 19.6, 19.7).</summary>
    internal sealed class ReviewsScreen : GalleryScreen
    {
        private sealed class Reviewer { public Label score; public VisualElement card; public float value, revealAt; public bool done; }
        private sealed class Confetti { public float x, y, vx, vy, rot, spin, size; public Color c; }

        private readonly List<Reviewer> _reviewers = new List<Reviewer>();
        private readonly List<Confetti> _confetti = new List<Confetti>();
        private readonly List<VisualElement> _bars = new List<VisualElement>();
        private VisualElement _confettiLayer, _finalDisc;
        private Label _finalScore, _verdict;
        private bool _finalDone;
        private const float FinalAt = 4.6f;

        public override float SettleSeconds => 7.4f;
        private static readonly float[] Sales = { 1f, 0.72f, 0.5f, 0.38f, 0.3f, 0.22f, 0.17f, 0.13f, 0.1f, 0.08f, 0.06f, 0.05f };

        protected override void Build()
        {
            Scrim(0.55f);
            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(1080, 22); card.style.borderBottomWidth = 6; Kit.Pad(card, 26, 30);
            card.style.alignItems = Align.Center;
            center.Add(Enter(card, 0.05f, 30f));

            card.Add(Kit.Caps("As notas chegaram", 12, Kit.Soft, 3f));
            var title = Kit.Text("Lendas de Aethel II", 30, Kit.Ink, true); title.style.marginTop = 4;
            card.Add(title);
            card.Add(Kit.Text("Action RPG · Souls-like · Fantasia Medieval · PC", 13, Kit.Soft));

            var row = Kit.Row(Align.Stretch); row.style.marginTop = 22;
            card.Add(row);
            (string outlet, string quote, float score)[] data =
            {
                ("GameSpotted", "“Combate afiado, mundo memorável.”", 9f),
                ("IGM", "“Faltou polimento, sobrou alma.”", 8f),
                ("Polígono", "“Os bugs incomodam no fim.”", 7f),
                ("Kotaco", "“A melhor sequência do ano.”", 9f)
            };
            for (int i = 0; i < data.Length; i++)
            {
                var rc = Kit.Tile(16); rc.style.width = 210; rc.style.marginLeft = 8; rc.style.marginRight = 8;
                rc.style.alignItems = Align.Center; Kit.Pad(rc, 16, 12);
                rc.Add(Kit.Text(data[i].outlet, 14, Kit.Ink, true));
                var sc = Kit.Text("?", 50, Kit.Faint, true); sc.style.marginTop = 4;
                rc.Add(sc);
                var q = Kit.Wrap(data[i].quote, 11, Kit.Soft); q.style.unityTextAlign = TextAnchor.MiddleCenter; q.style.marginTop = 4;
                q.style.opacity = 0f;
                rc.Add(q);
                row.Add(Enter(rc, 0.3f + i * 0.08f));
                _reviewers.Add(new Reviewer { score = sc, card = rc, value = data[i].score, revealAt = 1.0f + i * 0.85f });
                float at = 1.0f + i * 0.85f + 0.75f;
                Animate(at, 0.4f, k => q.style.opacity = k, Linear);
            }

            var lower = Kit.Row(Align.Stretch); lower.style.marginTop = 24; lower.style.alignSelf = Align.Stretch;
            card.Add(lower);

            // nota final
            var finalCol = Kit.Col(); finalCol.style.alignItems = Align.Center; finalCol.style.width = 230;
            _finalDisc = new VisualElement();
            _finalDisc.style.width = 132; _finalDisc.style.height = 132; Kit.Radius(_finalDisc, 66);
            _finalDisc.style.backgroundColor = Kit.Green; Kit.Border(_finalDisc, Kit.A(Color.white, 0.6f), 6);
            _finalDisc.style.alignItems = Align.Center; _finalDisc.style.justifyContent = Justify.Center;
            _finalScore = Kit.Text("8,3", 52, Color.white, true);
            _finalDisc.Add(_finalScore);
            finalCol.Add(_finalDisc);
            _verdict = Kit.Text("Sucesso de crítica!", 16, Kit.Ink, true); _verdict.style.marginTop = 10;
            finalCol.Add(_verdict);
            lower.Add(finalCol);
            Pop(_finalDisc, FinalAt);
            Animate(FinalAt + 0.3f, 0.4f, k => _verdict.style.opacity = k, Linear);

            // vendas por semana
            var salesCol = Kit.Col(); salesCol.style.flexGrow = 1; salesCol.style.marginLeft = 24;
            salesCol.Add(Kit.Caps("Vendas por semana", 11, Kit.Soft));
            var chart = Kit.Row(Align.FlexEnd); chart.style.height = 120; chart.style.marginTop = 8;
            for (int i = 0; i < Sales.Length; i++)
            {
                var col = Kit.Col(); col.style.flexGrow = 1; col.style.flexBasis = 0; col.style.alignItems = Align.Center;
                col.style.justifyContent = Justify.FlexEnd; col.style.height = Length.Percent(100);
                var bar = new VisualElement();
                bar.style.width = Length.Percent(70); bar.style.height = 0; Kit.Radius(bar, 5);
                bar.style.backgroundColor = i == 0 ? Kit.Blue : Kit.A(Kit.Blue, 0.55f);
                col.Add(bar);
                chart.Add(col);
                float h = Sales[i];
                Animate(FinalAt + 0.6f + i * 0.07f, 0.45f, k => bar.style.height = Length.Percent(h * 100f * k), OutBack);
            }
            salesCol.Add(chart);
            var stats = Kit.Row(); stats.style.marginTop = 12; stats.style.justifyContent = Justify.SpaceBetween;
            stats.Add(Stat("Unidades (1ª semana)", 6700, FinalAt + 0.6f, v => Kit.Short(v), Kit.Ink));
            stats.Add(Stat("Receita prevista", 412000, FinalAt + 0.8f, Kit.Money, Kit.Cash));
            stats.Add(Stat("Pesquisa", 5, FinalAt + 1.0f, v => "+" + Mathf.RoundToInt(v) + " PP", Kit.Teal));
            stats.Add(Stat("Franquia Aethel", 8.2f, FinalAt + 1.2f, v => "média " + v.ToString("0.0").Replace('.', ','), Kit.Purple));
            salesCol.Add(stats);
            lower.Add(salesCol);

            var ok = Kit.Confirm("Continuar", Kit.IcArrowRight); ok.style.marginTop = 22; ok.style.alignSelf = Align.FlexEnd;
            card.Add(ok);
            Animate(FinalAt + 1.4f, 0.4f, k => ok.style.opacity = k, Linear);

            _confettiLayer = Kit.Fill(); _confettiLayer.pickingMode = PickingMode.Ignore;
            _confettiLayer.generateVisualContent += DrawConfetti;
            Root.Add(_confettiLayer);
        }

        private VisualElement Stat(string cap, float value, float at, System.Func<float, string> fmt, Color color)
        {
            var c = Kit.Col();
            c.Add(Kit.Text(cap, 11, Kit.Soft, true));
            var label = Kit.Text(fmt(0), 20, color, true);
            c.Add(label);
            CountUp(label, 0, value, at, 0.9f, fmt);
            return c;
        }

        private static Color ScoreColor(float v) => v >= 8f ? Kit.Green : v >= 6f ? Kit.Gold : Kit.Red;

        protected override void OnTick(float dt)
        {
            foreach (var r in _reviewers)
            {
                if (r.done || Clock < r.revealAt) continue;
                if (Clock < r.revealAt + 0.7f)
                {
                    r.score.text = Random.Range(1, 10).ToString();
                    r.score.style.color = Kit.Soft;
                }
                else
                {
                    r.done = true;
                    r.score.text = Mathf.RoundToInt(r.value).ToString();
                    r.score.style.color = ScoreColor(r.value);
                    var sc = r.score;
                    Animate(0f, 0.35f, k =>
                    {
                        float s = 1f + 0.35f * Mathf.Sin(k * Mathf.PI);
                        sc.style.scale = new Scale(new Vector2(s, s));
                    }, Linear);
                }
            }

            if (!_finalDone && Clock >= FinalAt)
            {
                _finalDone = true;
                Color[] cols = { Kit.Teal, Kit.Pink, Kit.Gold, Kit.Blue, Kit.Purple, Kit.Green };
                var rect = Root.contentRect;
                for (int i = 0; i < 90; i++)
                {
                    _confetti.Add(new Confetti
                    {
                        x = rect.width * Random.Range(0.25f, 0.75f), y = rect.height * 0.45f,
                        vx = Random.Range(-420f, 420f), vy = Random.Range(-720f, -220f),
                        rot = Random.Range(0f, 360f), spin = Random.Range(-360f, 360f),
                        size = Random.Range(6f, 12f), c = cols[i % cols.Length]
                    });
                }
            }
            if (_finalDone)
            {
                float s = 1f + 0.03f * Mathf.Sin(Clock * 4f);
                if (Clock > FinalAt + 0.6f) _finalDisc.style.scale = new Scale(new Vector2(s, s));
            }

            if (_confetti.Count > 0)
            {
                float h = Root.contentRect.height;
                for (int i = _confetti.Count - 1; i >= 0; i--)
                {
                    var c = _confetti[i];
                    c.vy += 900f * dt; c.vx *= 1f - dt * 0.8f;
                    c.x += c.vx * dt; c.y += c.vy * dt; c.rot += c.spin * dt;
                    if (c.y > h + 20f) _confetti.RemoveAt(i);
                }
                _confettiLayer.MarkDirtyRepaint();
            }
        }

        private void DrawConfetti(MeshGenerationContext mgc)
        {
            if (_confetti.Count == 0) return;
            var p = mgc.painter2D;
            foreach (var c in _confetti)
            {
                Vector2 pos = new Vector2(c.x, c.y);
                Vector2 a = Kit.OnCircle(pos, c.size, c.rot), b = Kit.OnCircle(pos, c.size * 0.5f, c.rot + 90f);
                Vector2 d = Kit.OnCircle(pos, c.size, c.rot + 180f), e = Kit.OnCircle(pos, c.size * 0.5f, c.rot + 270f);
                p.fillColor = c.c;
                p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.LineTo(d); p.LineTo(e); p.ClosePath(); p.Fill();
            }
        }
    }
}
