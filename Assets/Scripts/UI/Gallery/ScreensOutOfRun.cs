using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>Slots de save (GDD 20.6): vários jogadores na mesma instalação.</summary>
    internal sealed class SaveSlotsScreen : GalleryScreen
    {
        private SunburstBackdrop _bg;
        private readonly List<FounderArt> _arts = new List<FounderArt>();

        protected override void Build()
        {
            _bg = SunburstBackdrop.Lilac();
            Root.Add(_bg);

            var center = Kit.Centered();
            Root.Add(center);

            var title = Kit.Text("Quem está jogando?", 34, Kit.Ink, true);
            center.Add(Enter(title, 0.05f));
            var sub = Kit.Text("Cada slot guarda a run atual, a árvore de habilidades e o histórico de empresas.", 14, Kit.Soft);
            sub.style.marginTop = 6; sub.style.marginBottom = 30;
            center.Add(Enter(sub, 0.1f));

            var row = Kit.Row(Align.Stretch);
            center.Add(row);
            var cards = new List<VisualElement>
            {
                Slot("Pixel Forja", "David", "Run 4 · Ano 12 · Estúdio", "R$ 6,2M", "38h 12min", "Autosave há 2 min", 0, 0, 1, 1),
                Slot("Bit Brasa Games", "Marina", "Run 1 · Ano 3 · Garagem", "R$ 84K", "2h 40min", "Salvo ontem, 22:14", 1, 2, 2, 2),
                EmptySlot()
            };
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].style.marginLeft = 12; cards[i].style.marginRight = 12;
                row.Add(Enter(cards[i], 0.2f + i * 0.09f, 40f));
            }
            Widgets.SelectGroup(cards, 0, Kit.Teal);

            var actions = Kit.Row();
            actions.style.marginTop = 30;
            actions.Add(Kit.Ghost("Voltar", Kit.IcArrowLeft));
            actions.Add(Kit.Gap(12));
            actions.Add(Kit.Danger("Apagar slot", Kit.IcTrash));
            actions.Add(Kit.Gap(12));
            actions.Add(Kit.Confirm("Continuar", Kit.IcPlay));
            center.Add(Enter(actions, 0.55f));
        }

        private VisualElement Slot(string company, string founder, string run, string valuation, string time, string saved, int skin, int hairStyle, int shirt, int accessory)
        {
            var card = Kit.Card(300, 18);
            Kit.Border(card, Kit.Track, 3); card.style.borderBottomWidth = 6;
            card.style.alignItems = Align.Center;
            Kit.Pad(card, 22, 20);

            var art = new FounderArt(130) { Skin = skin, HairStyle = hairStyle, Shirt = shirt, Accessory = accessory, Hair = skin };
            var halo = new VisualElement();
            halo.style.width = 150; halo.style.height = 150; Kit.Radius(halo, 75);
            halo.style.backgroundColor = Kit.A(FounderArt.Shirts[shirt], 0.16f);
            halo.style.alignItems = Align.Center; halo.style.justifyContent = Justify.FlexEnd; halo.style.overflow = Overflow.Hidden;
            halo.Add(art); _arts.Add(art);
            card.Add(halo);

            var name = Kit.Text(company, 20, Kit.Ink, true); name.style.marginTop = 14;
            card.Add(name);
            card.Add(Kit.Text("Fundador(a): " + founder, 12, Kit.Soft));
            var chip = Kit.Chip(run, Kit.Teal); chip.style.marginTop = 10;
            card.Add(chip);
            card.Add(Kit.Divider(14));

            var stats = Kit.Row(); stats.style.alignSelf = Align.Stretch; stats.style.justifyContent = Justify.SpaceBetween;
            stats.Add(MiniStat(Kit.IcChartLine, "Valuation", valuation, Kit.Cash));
            stats.Add(MiniStat(Kit.IcClock, "Tempo", time, Kit.Soft));
            card.Add(stats);
            var sv = Kit.Row(); sv.style.marginTop = 12;
            sv.Add(Kit.Icon(Kit.IcSave, 10, Kit.Faint)); sv.Add(Kit.Text("  " + saved, 11, Kit.Faint));
            card.Add(sv);
            return card;
        }

        private static VisualElement MiniStat(string icon, string cap, string val, Color color)
        {
            var c = Kit.Col();
            var r = Kit.Row(); r.Add(Kit.Icon(icon, 10, Kit.Soft)); r.Add(Kit.Text("  " + cap, 10, Kit.Soft, true));
            c.Add(r); c.Add(Kit.Text(val, 16, color, true));
            return c;
        }

        private static VisualElement EmptySlot()
        {
            var card = Kit.Card(300, 18);
            Kit.Border(card, Kit.Track, 3); card.style.borderBottomWidth = 6;
            card.style.alignItems = Align.Center; card.style.justifyContent = Justify.Center;
            card.Add(Kit.Badge(Kit.IcPlus, Kit.Blue, 72));
            var t = Kit.Text("Novo jogo", 20, Kit.Ink, true); t.style.marginTop = 16;
            card.Add(t);
            var d = Kit.Wrap("Começa do zero, incluindo a meta-progressão.", 12, Kit.Soft);
            d.style.unityTextAlign = TextAnchor.MiddleCenter; d.style.marginTop = 4; d.style.maxWidth = 220;
            card.Add(d);
            return card;
        }

        protected override void OnTick(float dt)
        {
            _bg.Tick(dt);
            foreach (var a in _arts) a.Tick(dt);
        }
    }

    /// <summary>Opções (GDD 20.1): áudio, vídeo, idioma e acessibilidade (+ dicas do tutorial, 20.3).</summary>
    internal sealed class OptionsScreen : GalleryScreen
    {
        private static readonly string[] Tabs = { "Áudio", "Vídeo", "Idioma", "Acessibilidade" };
        private static readonly string[] TabIcons = { Kit.IcVolume, Kit.IcTv, Kit.IcLanguage, Kit.IcAccess };

        private SunburstBackdrop _bg;
        private VisualElement _content;
        private readonly List<VisualElement> _tabEls = new List<VisualElement>();
        private readonly List<Label> _valueLabels = new List<Label>();

        public override void SetState(int index) => ShowTab(index);

        protected override void Build()
        {
            _bg = SunburstBackdrop.Lilac();
            Root.Add(_bg);
            var body = Window("Opções", Kit.IcGear, Kit.Soft, 900, 600, out _);
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            var rail = Kit.Col(); rail.style.width = 210; rail.style.flexShrink = 0;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int idx = i;
                var t = Kit.Row(); Kit.Pad(t, 11, 14); Kit.Radius(t, 11); t.style.marginBottom = 6; Kit.Smooth(t);
                t.Add(Kit.Icon(TabIcons[i], 14, Kit.Soft));
                var l = Kit.Text(Tabs[i], 15, Kit.Ink, true); l.style.marginLeft = 12;
                t.Add(l);
                t.RegisterCallback<ClickEvent>(_ => ShowTab(idx));
                rail.Add(Enter(t));
                _tabEls.Add(t);
            }
            row.Add(rail);
            row.Add(Kit.VDivider(20));

            _content = Kit.Col(); _content.style.flexGrow = 1;
            row.Add(_content);

            var foot = Footer(body);
            foot.Add(Kit.Ghost("Restaurar padrão", Kit.IcUndo));
            foot.Add(Kit.Gap(12));
            foot.Add(Kit.Confirm("Aplicar", Kit.IcCheck));
            ShowTab(0);
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < _tabEls.Count; i++)
            {
                bool on = i == index;
                _tabEls[i].style.backgroundColor = on ? Kit.Teal : new Color(0, 0, 0, 0);
                ((Label)_tabEls[i][0]).style.color = on ? Color.white : Kit.Soft;
                ((Label)_tabEls[i][1]).style.color = on ? Color.white : Kit.Ink;
            }
            _content.Clear();
            ResetCascade(0.05f);
            switch (index)
            {
                case 0:
                    Add(SliderRow("Volume geral", 0.8f, Kit.Teal));
                    Add(SliderRow("Música", 0.6f, Kit.Purple, "Trilha lo-fi / jazzy"));
                    Add(SliderRow("Efeitos sonoros", 0.9f, Kit.Blue));
                    Add(SliderRow("Interface", 0.7f, Kit.Orange, "Cliques, pontos e jingles"));
                    Add(Widgets.FormRow("Silenciar em segundo plano", Widgets.Toggle(true)));
                    break;
                case 1:
                    Add(Widgets.FormRow("Resolução", Widgets.Select(new[] { "1920 × 1080", "2560 × 1440", "1600 × 900", "1280 × 720" })));
                    Add(Widgets.FormRow("Modo de janela", Widgets.Segmented(new[] { "Tela cheia", "Sem borda", "Janela" }, 0)));
                    Add(Widgets.FormRow("Qualidade", Widgets.Segmented(new[] { "Baixa", "Média", "Alta" }, 2)));
                    Add(Widgets.FormRow("VSync", Widgets.Toggle(true)));
                    Add(Widgets.FormRow("Limite de FPS", Widgets.Select(new[] { "60", "120", "144", "Sem limite" })));
                    Add(Widgets.FormRow("Escala da interface", Widgets.Segmented(new[] { "90%", "100%", "115%", "130%" }, 1), "Aumenta HUD e painéis"));
                    break;
                case 2:
                    var langs = new List<VisualElement> { Lang("Português (Brasil)", "pt-BR", true), Lang("English (US)", "en-US", false) };
                    var lr = Kit.Row();
                    foreach (var l in langs) { l.style.marginRight = 14; lr.Add(l); }
                    Widgets.SelectGroup(langs, 0, Kit.Teal);
                    Add(lr);
                    var note = Kit.Wrap("Todo o texto do jogo vem das tabelas de localização — novos idiomas entram sem refatoração.", 12, Kit.Soft);
                    note.style.marginTop = 18; note.style.maxWidth = 520;
                    Add(note);
                    break;
                default:
                    Add(Widgets.FormRow("Tamanho do texto", Widgets.Segmented(new[] { "Normal", "Grande", "Enorme" }, 0)));
                    Add(Widgets.FormRow("Alto contraste", Widgets.Toggle(false)));
                    Add(Widgets.FormRow("Reduzir animações", Widgets.Toggle(false), "Desliga partículas e pulsos do HUD"));
                    Add(Widgets.FormRow("Modo daltônico", Widgets.Select(new[] { "Desligado", "Protanopia", "Deuteranopia", "Tritanopia" })));
                    Add(Widgets.FormRow("Pausar ao perder o foco", Widgets.Toggle(true)));
                    Add(Widgets.FormRow("Dicas do tutorial", Widgets.Toggle(true), "Tooltips na primeira vez em cada sistema"));
                    var again = Kit.Ghost("Rever todas as dicas", Kit.IcQuestion); again.style.alignSelf = Align.FlexStart;
                    Add(again);
                    break;
            }
        }

        private void Add(VisualElement el) => _content.Add(Enter(el));

        private VisualElement SliderRow(string label, float value, Color color, string hint = null)
        {
            var val = Kit.Text(Mathf.RoundToInt(value * 100f) + "%", 14, Kit.Ink, true);
            val.style.width = 52; val.style.unityTextAlign = TextAnchor.MiddleRight;
            var ctl = Kit.Row(); ctl.style.flexGrow = 1;
            ctl.Add(Widgets.Slider(value, color, v => val.text = Mathf.RoundToInt(v * 100f) + "%"));
            ctl.Add(val);
            _valueLabels.Add(val);
            return Widgets.FormRow(label, ctl, hint);
        }

        private static VisualElement Lang(string name, string code, bool ready)
        {
            var c = Kit.Outline(14); c.style.width = 250; Kit.Pad(c, 18, 18);
            c.Add(Kit.Badge(Kit.IcGlobe, ready ? Kit.Teal : Kit.Faint, 40));
            var n = Kit.Text(name, 16, Kit.Ink, true); n.style.marginTop = 12;
            c.Add(n);
            c.Add(Kit.Text(code, 12, Kit.Soft));
            var chip = Kit.Chip(ready ? "Completo" : "No lançamento Steam", ready ? Kit.Green : Kit.Orange);
            chip.style.marginTop = 10; chip.style.alignSelf = Align.FlexStart;
            c.Add(chip);
            return c;
        }

        protected override void OnTick(float dt) => _bg.Tick(dt);
    }

    /// <summary>Criação do fundador e da empresa (GDD 20.2) ao iniciar um novo jogo/run.</summary>
    internal sealed class FounderCreateScreen : GalleryScreen
    {
        private SunburstBackdrop _bg;
        private FounderArt _art;
        private VisualElement _stage;

        protected override void Build()
        {
            _bg = SunburstBackdrop.Lilac();
            Root.Add(_bg);
            var body = Window("Novo fundador", Kit.IcUserPlus, Kit.Teal, 980, 620, out _, "Quem vai tocar essa empresa?");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            // palco com o personagem
            _stage = Kit.Tile(18);
            _stage.style.width = 340; _stage.style.alignItems = Align.Center; _stage.style.justifyContent = Justify.FlexEnd;
            _stage.style.overflow = Overflow.Hidden; Kit.Pad(_stage, 0, 0);
            _stage.style.backgroundColor = Kit.A(Kit.Teal, 0.14f);
            _art = new FounderArt(320) { Skin = 0, HairStyle = 1, Shirt = 0, Accessory = 1 };
            _stage.Add(_art);
            var plate = Kit.Card(0, 12); plate.style.position = Position.Absolute; plate.style.bottom = 16;
            plate.style.alignItems = Align.Center; Kit.Pad(plate, 8, 18);
            plate.Add(Kit.Text("David", 18, Kit.Ink, true));
            plate.Add(Kit.Text("CEO · Pixel Forja", 11, Kit.Soft));
            _stage.Add(plate);
            row.Add(Enter(_stage, 0.2f));
            row.Add(Kit.Gap(26));

            var form = Kit.Col(); form.style.flexGrow = 1;
            row.Add(form);
            form.Add(Enter(Widgets.FormRow("Nome do fundador", Widgets.Field("David", 300))));
            form.Add(Enter(Widgets.FormRow("Nome da empresa", Widgets.Field("Pixel Forja", 300))));
            form.Add(Kit.Divider(6));
            form.Add(Enter(Widgets.FormRow("Rosto", Widgets.Select(new[] { "Rosto 1", "Rosto 2", "Rosto 3", "Rosto 4" }, 0, 300, i => { _art.Skin = i; Bounce(); }))));
            form.Add(Enter(Widgets.FormRow("Cabelo", Widgets.Select(new[] { "Espetado", "Longo", "Boné", "Curto" }, 0, 300, i => { _art.HairStyle = i + 1; Bounce(); }))));
            form.Add(Enter(Widgets.FormRow("Cor do cabelo", Widgets.Select(new[] { "Castanho escuro", "Castanho", "Loiro", "Ruivo", "Grisalho" }, 0, 300, i => { _art.Hair = i; Bounce(); }))));
            form.Add(Enter(Widgets.FormRow("Acessório", Widgets.Select(new[] { "Óculos", "Headset", "Nenhum" }, 0, 300, i => { _art.Accessory = i + 1; Bounce(); }))));

            var swatches = Kit.Row();
            var sw = new List<VisualElement>();
            for (int i = 0; i < FounderArt.Shirts.Length; i++)
            {
                int idx = i;
                var s = new VisualElement();
                s.style.width = 34; s.style.height = 34; Kit.Radius(s, 17); s.style.marginRight = 10;
                s.style.backgroundColor = FounderArt.Shirts[i]; Kit.Border(s, Color.white, 3);
                Kit.Hover(s, 1.15f, 0.9f);
                s.RegisterCallback<ClickEvent>(_ =>
                {
                    _art.Shirt = idx; Bounce();
                    _stage.style.backgroundColor = Kit.A(FounderArt.Shirts[idx], 0.14f);
                    for (int k = 0; k < sw.Count; k++) Kit.BorderColor(sw[k], k == idx ? Kit.Ink : Color.white);
                });
                sw.Add(s); swatches.Add(s);
            }
            Kit.BorderColor(sw[0], Kit.Ink);
            form.Add(Enter(Widgets.FormRow("Roupa", swatches, "Novas roupas liberam conforme as runs")));

            var capital = Kit.Tile(); capital.style.flexDirection = FlexDirection.Row; capital.style.alignItems = Align.Center;
            capital.style.marginTop = 4;
            capital.Add(Kit.Badge(Kit.IcPiggy, Kit.Cash, 34));
            var ct = Kit.Col(); ct.style.marginLeft = 12;
            ct.Add(Kit.Text("Capital inicial", 11, Kit.Soft, true));
            var cv = Kit.Text("R$ 0", 20, Kit.Cash, true);
            ct.Add(cv);
            capital.Add(ct);
            capital.Add(Kit.Spacer());
            capital.Add(Kit.Wrap("Seu dinheiro pessoal vira o caixa da empresa.", 11, Kit.Soft));
            form.Add(Enter(capital));
            CountUp(cv, 0, 50000, 0.9f, 0.9f, v => Kit.Money(v));

            var foot = Footer(body);
            foot.Add(Kit.Ghost("Aleatório", Kit.IcDice));
            foot.Add(Kit.Gap(12));
            foot.Add(Kit.Confirm("Fundar empresa", Kit.IcRocket));
        }

        private void Bounce()
        {
            Animate(0f, 0.35f, k =>
            {
                float s = 1f + 0.08f * Mathf.Sin(k * Mathf.PI);
                _art.style.scale = new Scale(new Vector2(s, s));
            }, Linear);
        }

        protected override void OnTick(float dt)
        {
            _bg.Tick(dt);
            _art.Tick(dt);
        }
    }
}
