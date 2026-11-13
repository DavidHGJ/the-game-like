using System.Collections.Generic;
using GameLike.Data.Balance;
using GameLike.Data.Definitions;
using GameLike.State;
using GameLike.Systems.Core;
using GameLike.Systems.Marketing;
using GameLike.Systems.Projects;
using GameLike.UI.Gallery;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Game
{
    /// <summary>Pedaços de layout reutilizados pelas telas vivas.</summary>
    internal static class LiveBits
    {
        public static VisualElement Tile(string icon, Color color, string label, string sub, bool locked = false, float width = 118)
        {
            var t = Kit.Outline(14);
            t.style.width = width; t.style.alignItems = Align.Center; Kit.Pad(t, 12, 6);
            t.style.marginRight = 10; t.style.marginBottom = 10;
            t.Add(Kit.Badge(locked ? Kit.IcLock : icon, locked ? Kit.Track : Kit.A(color, 0.16f), 44, locked ? Kit.Faint : color));
            var l = Kit.Wrap(label, 12, locked ? Kit.Faint : Kit.Ink, true);
            l.style.marginTop = 8; l.style.unityTextAlign = TextAnchor.MiddleCenter;
            t.Add(l);
            if (sub != null) t.Add(Kit.Text(sub, 11, locked ? Kit.Faint : Kit.Cash, true));
            if (locked) { t.userData = "locked"; t.style.opacity = 0.7f; }
            return t;
        }

        public static VisualElement ThemeTile(string icon, Color color, string label, bool locked)
        {
            var t = Kit.Outline(14);
            t.style.width = 118; Kit.Pad(t, 0, 0); t.style.overflow = Overflow.Hidden;
            t.style.marginRight = 10; t.style.marginBottom = 10;
            var art = new VisualElement();
            art.style.height = 54; art.style.backgroundColor = locked ? Kit.Track : color;
            art.style.alignItems = Align.Center; art.style.justifyContent = Justify.Center;
            art.Add(Kit.Icon(locked ? Kit.IcLock : icon, 24, locked ? Kit.Faint : Kit.A(Color.white, 0.95f)));
            t.Add(art);
            var l = Kit.Wrap(label, 12, locked ? Kit.Faint : Kit.Ink, true);
            l.style.unityTextAlign = TextAnchor.MiddleCenter; Kit.Pad(l, 7, 4);
            t.Add(l);
            if (locked) { t.userData = "locked"; t.style.opacity = 0.7f; }
            return t;
        }

        /// <summary>Linha "ícone rótulo ........ valor"; devolve o Label do valor para atualizações.</summary>
        public static VisualElement Line(string icon, string label, string value, out Label valueLabel, Color? valueColor = null)
        {
            var r = Kit.Row(); r.style.marginBottom = 7;
            var i = Kit.Icon(icon, 11, Kit.Soft); i.style.width = 18;
            r.Add(i);
            var l = Kit.Text(label, 12, Kit.Soft); l.style.marginLeft = 6;
            r.Add(l);
            r.Add(Kit.Spacer());
            valueLabel = Kit.Text(value, 13, valueColor ?? Kit.Ink, true);
            r.Add(valueLabel);
            return r;
        }

        public static VisualElement Line(string icon, string label, string value, Color? valueColor = null) =>
            Line(icon, label, value, out _, valueColor);

        public static VisualElement Meter(string label, float value01, Color color, string note)
        {
            var c = Kit.Col(); c.style.marginTop = 8;
            var r = Kit.Row();
            r.Add(Kit.Text(label, 11, Kit.Soft, true)); r.Add(Kit.Spacer());
            if (note != null) r.Add(Kit.Text(note, 10, color, true));
            c.Add(r);
            var bar = Kit.Bar(7, color, out _, Mathf.Clamp01(value01)); bar.style.marginTop = 3;
            c.Add(bar);
            return c;
        }

        public static VisualElement Note(string icon, Color color, string text)
        {
            var n = Kit.Tile(12); n.style.flexDirection = FlexDirection.Row; n.style.alignItems = Align.Center;
            n.Add(Kit.Badge(icon, color, 26));
            var t = Kit.Wrap(text, 11, Kit.Soft); t.style.marginLeft = 10; t.style.flexShrink = 1; t.style.flexGrow = 1;
            n.Add(t);
            return n;
        }

        public static void SetEnabled(VisualElement button, bool enabled)
        {
            button.style.opacity = enabled ? 1f : 0.45f;
            button.pickingMode = enabled ? PickingMode.Position : PickingMode.Ignore;
            foreach (var child in button.Children()) child.pickingMode = PickingMode.Ignore;
        }

        public static VisualElement Wrap()
        {
            var r = Kit.Row(Align.Stretch); r.style.flexWrap = UnityEngine.UIElements.Wrap.Wrap;
            return r;
        }
    }

    /// <summary>Criação do fundador e da empresa (GDD 20.2) — primeira coisa de uma nova partida.</summary>
    internal sealed class FounderCreateLive : GalleryScreen
    {
        private FounderArt _art;
        private VisualElement _stage;
        private int _skin, _hair, _hairStyle = 1, _shirt, _accessory = 1;

        protected override void Build()
        {
            Scrim(0.6f);
            var gc = Ui.Gc;
            var body = Window("Novo fundador", Kit.IcUserPlus, Kit.Teal, 980, 620, out _, "Quem vai tocar essa empresa?");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);

            _stage = Kit.Tile(18);
            _stage.style.width = 340; _stage.style.alignItems = Align.Center; _stage.style.justifyContent = Justify.FlexEnd;
            _stage.style.overflow = Overflow.Hidden; Kit.Pad(_stage, 0, 0);
            _stage.style.backgroundColor = Kit.A(FounderArt.Shirts[_shirt], 0.14f);
            _art = new FounderArt(320) { Skin = _skin, Hair = _hair, HairStyle = _hairStyle, Shirt = _shirt, Accessory = _accessory };
            _stage.Add(_art);
            row.Add(Enter(_stage, 0.2f));
            row.Add(Kit.Gap(26));

            var form = Kit.Col(); form.style.flexGrow = 1;
            row.Add(form);
            var nameField = Widgets.Input(gc.Meta.founder.created ? gc.Meta.founder.name : "", 300);
            var companyField = Widgets.Input(gc.Run.company.name, 300);
            form.Add(Enter(Widgets.FormRow("Nome do fundador", nameField)));
            form.Add(Enter(Widgets.FormRow("Nome da empresa", companyField)));
            form.Add(Kit.Divider(6));
            form.Add(Enter(Widgets.FormRow("Rosto", Widgets.Select(new[] { "Rosto 1", "Rosto 2", "Rosto 3", "Rosto 4" }, 0, 300, i => { _skin = i; Apply(); }))));
            form.Add(Enter(Widgets.FormRow("Cabelo", Widgets.Select(new[] { "Espetado", "Longo", "Boné", "Curto" }, 0, 300, i => { _hairStyle = (i + 1) % 4; Apply(); }))));
            form.Add(Enter(Widgets.FormRow("Cor do cabelo", Widgets.Select(new[] { "Castanho escuro", "Castanho", "Loiro", "Ruivo", "Grisalho" }, 0, 300, i => { _hair = i; Apply(); }))));
            form.Add(Enter(Widgets.FormRow("Acessório", Widgets.Select(new[] { "Óculos", "Headset", "Nenhum" }, 0, 300, i => { _accessory = (i + 1) % 3; Apply(); }))));

            var swatches = Kit.Row();
            var sw = new List<VisualElement>();
            for (int i = 0; i < FounderArt.Shirts.Length; i++)
            {
                int idx = i;
                var s = new VisualElement();
                s.style.width = 34; s.style.height = 34; Kit.Radius(s, 17); s.style.marginRight = 10;
                s.style.backgroundColor = FounderArt.Shirts[i]; Kit.Border(s, i == 0 ? Kit.Ink : Color.white, 3);
                Kit.Hover(s, 1.15f, 0.9f);
                s.RegisterCallback<ClickEvent>(_ =>
                {
                    _shirt = idx; Apply();
                    _stage.style.backgroundColor = Kit.A(FounderArt.Shirts[idx], 0.14f);
                    for (int k = 0; k < sw.Count; k++) Kit.BorderColor(sw[k], k == idx ? Kit.Ink : Color.white);
                });
                sw.Add(s); swatches.Add(s);
            }
            form.Add(Enter(Widgets.FormRow("Roupa", swatches)));

            var capital = Kit.Tile(); capital.style.flexDirection = FlexDirection.Row; capital.style.alignItems = Align.Center;
            capital.Add(Kit.Badge(Kit.IcPiggy, Kit.Cash, 34));
            var ct = Kit.Col(); ct.style.marginLeft = 12;
            ct.Add(Kit.Text("Capital inicial", 11, Kit.Soft, true));
            ct.Add(Kit.Text(Kit.Money(gc.Run.company.cash), 20, Kit.Cash, true));
            capital.Add(ct);
            form.Add(Enter(capital));

            var foot = Footer(body);
            foot.Add(Kit.Confirm("Fundar empresa", Kit.IcRocket, () =>
            {
                string founder = string.IsNullOrWhiteSpace(nameField.value) ? "Fundador" : nameField.value;
                gc.SetFounder(founder, _skin, _hair, _hairStyle, _shirt, _accessory);
                gc.RenameCompany(companyField.value);
                Sfx.Play(SfxId.Success);
                RequestClose();
            }));
        }

        private void Apply()
        {
            _art.Skin = _skin; _art.Hair = _hair; _art.HairStyle = _hairStyle; _art.Shirt = _shirt; _art.Accessory = _accessory;
        }

        protected override void OnTick(float dt) => _art.Tick(dt);
    }

    /// <summary>Novo projeto — Conceito (GDD 5.2 fase 1) com o conteúdo e os desbloqueios reais da run.</summary>
    internal sealed class NewProjectLive : GalleryScreen
    {
        private GenreSO _genre;
        private ThemeSO _theme;
        private ConsoleSO _console;
        private ProjectSize _size = ProjectSize.Small;
        private string _parentId;
        private TextField _name;
        private Label _sumTitle, _sumCombo, _sumTeam, _sumPoints, _sumWeeks, _sumLicense, _sumBurn, _sumEngine, _sumTotal, _sumSequel;
        private VisualElement _start;

        protected override void Build()
        {
            Scrim();
            var gc = Ui.Gc;
            var body = Window("Novo projeto", Kit.IcPlus, Kit.Blue, 1300, 820, out _, "Conceito — o que vamos fazer?");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);
            var form = Kit.Col(); form.style.flexGrow = 1; form.style.flexShrink = 1;
            row.Add(form);

            // nome + sequência
            var top = Kit.Row(Align.FlexEnd); top.style.marginBottom = 12;
            var nameCol = Kit.Col();
            Section(nameCol, "Nome do jogo");
            _name = Widgets.Input("Jogo " + (gc.Run.company.gamesReleased + 1), 360);
            _name.RegisterValueChangedCallback(_ => RefreshSummary());
            nameCol.Add(_name);
            top.Add(nameCol);
            top.Add(Kit.Gap(24));
            var seqCol = Kit.Col();
            Section(seqCol, "Sequência de");
            var released = gc.Run.releasedProjects;
            if (gc.CanMakeSequels && released.Count > 0)
            {
                var options = new string[released.Count + 1];
                options[0] = "Nenhuma — IP novo";
                for (int i = 0; i < released.Count; i++) options[i + 1] = released[i].name + " (" + Labels.Score(released[i].finalScore) + ")";
                seqCol.Add(Widgets.Select(options, 0, 340, i => { _parentId = i == 0 ? null : released[i - 1].id; RefreshSummary(); }));
            }
            else
            {
                seqCol.Add(Kit.Chip(released.Count == 0 ? "Lance um jogo primeiro" : "Pesquise Gestão de Franquias (fase Escritório)", Kit.Soft, false, Kit.IcLock));
            }
            top.Add(seqCol);
            form.Add(Enter(top));

            // gênero
            Section(form, "Gênero");
            var genreRow = LiveBits.Wrap();
            var genreTiles = new List<VisualElement>();
            var genres = gc.AvailableGenres;
            for (int i = 0; i < genres.Count; i++)
            {
                var t = LiveBits.Tile(Labels.GenreIcon(genres[i].name), Kit.Blue, genres[i].DisplayLabel, null);
                genreTiles.Add(t); genreRow.Add(t);
            }
            if (genres.Count > 0) _genre = genres[0];
            Widgets.SelectGroup(genreTiles, 0, Kit.Blue, i => { _genre = genres[i]; RefreshSummary(); });
            form.Add(Enter(genreRow));

            // tema
            Section(form, "Tema");
            var themeRow = LiveBits.Wrap();
            var themeTiles = new List<VisualElement>();
            var unlockedThemes = new List<ThemeSO>();
            var themes = gc.AvailableThemes;
            for (int i = 0; i < themes.Count; i++)
            {
                bool unlocked = gc.Research == null || gc.Research.IsThemeUnlocked(themes[i].name);
                var style = Labels.ThemeStyle(themes[i].name);
                var t = LiveBits.ThemeTile(style.icon, style.color, themes[i].DisplayLabel, !unlocked);
                themeRow.Add(t);
                if (unlocked) { themeTiles.Add(t); unlockedThemes.Add(themes[i]); }
            }
            if (unlockedThemes.Count > 0) _theme = unlockedThemes[0];
            Widgets.SelectGroup(themeTiles, 0, Kit.Pink, i => { _theme = unlockedThemes[i]; RefreshSummary(); });
            form.Add(Enter(themeRow));

            // plataforma + escopo
            var bottom = Kit.Row(Align.FlexStart);
            var platCol = Kit.Col(); platCol.style.flexShrink = 1; platCol.style.maxWidth = 560;
            Section(platCol, "Plataforma");
            var platRow = LiveBits.Wrap();
            var platTiles = new List<VisualElement>();
            var consoles = gc.ConsolesAvailableNow();
            for (int i = 0; i < consoles.Count; i++)
            {
                var t = LiveBits.Tile(Labels.PlatformIcon(consoles[i].Category), Kit.Teal, consoles[i].DisplayLabel, Kit.Money(gc.LicenseCost(consoles[i])));
                platTiles.Add(t); platRow.Add(t);
            }
            if (consoles.Count > 0) _console = consoles[0];
            Widgets.SelectGroup(platTiles, 0, Kit.Teal, i => { _console = consoles[i]; RefreshSummary(); });
            platCol.Add(platRow);
            bottom.Add(platCol);
            bottom.Add(Kit.Gap(24));

            var scopeCol = Kit.Col();
            Section(scopeCol, "Escopo");
            var sizes = new[] { ProjectSize.Small, ProjectSize.Medium, ProjectSize.Large, ProjectSize.AAA };
            var names = new string[sizes.Length];
            var locked = new bool[sizes.Length];
            for (int i = 0; i < sizes.Length; i++)
            {
                names[i] = Labels.Size(sizes[i]);
                locked[i] = gc.Research != null && !gc.Research.IsSizeUnlocked(sizes[i]);
            }
            scopeCol.Add(Widgets.Segmented(names, 0, i => { _size = sizes[i]; RefreshSummary(); }, Kit.Teal, locked));
            var hint = Kit.Wrap("Escopos maiores e novos temas são liberados no painel de P&D.", 11, Kit.Soft);
            hint.style.marginTop = 8; hint.style.maxWidth = 300;
            scopeCol.Add(hint);
            bottom.Add(scopeCol);
            form.Add(Enter(bottom));

            row.Add(Kit.Gap(24));
            row.Add(Enter(Summary(), 0.4f));
            RefreshSummary();
        }

        private VisualElement Summary()
        {
            var s = Kit.Tile(16); s.style.width = 320; s.style.flexShrink = 0; Kit.Pad(s, 18, 18);
            s.Add(Kit.Caps("Resumo", 11, Kit.Soft));
            _sumTitle = Kit.Wrap("", 20, Kit.Ink, true); _sumTitle.style.marginTop = 6;
            s.Add(_sumTitle);
            _sumCombo = Kit.Wrap("", 13, Kit.Soft);
            s.Add(_sumCombo);
            s.Add(Kit.Divider(12));
            s.Add(LiveBits.Line(Kit.IcUsers, "Equipe", "", out _sumTeam));
            s.Add(LiveBits.Line(Kit.IcSliders, "Pontos por fase", "", out _sumPoints));
            s.Add(LiveBits.Line(Kit.IcClock, "Duração", "", out _sumWeeks));
            s.Add(LiveBits.Line(Kit.IcChip, "Engine Budget", "", out _sumEngine));
            s.Add(LiveBits.Line(Kit.IcTag, "Licença", "", out _sumLicense));
            s.Add(LiveBits.Line(Kit.IcCoins, "Custo por semana", "", out _sumBurn));
            s.Add(Kit.Divider(12));
            _sumSequel = Kit.Wrap("", 11, Kit.Soft);
            s.Add(_sumSequel);
            var q = Kit.Row(Align.FlexStart); q.style.marginTop = 8;
            q.Add(Kit.Badge(Kit.IcQuestion, Kit.Faint, 26));
            var qt = Kit.Wrap("Se gênero e tema combinam, você descobre durante o desenvolvimento.", 11, Kit.Soft);
            qt.style.marginLeft = 10; qt.style.flexShrink = 1;
            q.Add(qt);
            s.Add(q);
            s.Add(Kit.Spacer());
            s.Add(Kit.Text("Custo inicial", 11, Kit.Soft, true));
            _sumTotal = Kit.Text("", 28, Kit.Cash, true);
            s.Add(_sumTotal);
            _start = Kit.Primary("Iniciar projeto", Kit.IcRocket, StartProject); _start.style.marginTop = 12;
            s.Add(_start);
            var cancel = Kit.Ghost("Cancelar", null, RequestClose); cancel.style.marginTop = 8;
            s.Add(cancel);
            return s;
        }

        private void RefreshSummary()
        {
            if (_sumTitle == null) return;
            var gc = Ui.Gc;
            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            var engine = BalanceProvider.Get<EngineBalanceSO>();
            _sumTitle.text = string.IsNullOrWhiteSpace(_name.value) ? "Sem título" : _name.value;
            _sumCombo.text = (_genre != null ? _genre.DisplayLabel : "—") + " · " + (_theme != null ? _theme.DisplayLabel : "sem tema")
                             + "\n" + (_console != null ? _console.DisplayLabel : "—") + " · " + Labels.Size(_size);

            int maxTeam = loop != null ? loop.MaxTeamForPhase(gc.Run.company.phase) : 1;
            int team = Mathf.Clamp(gc.Run.company.EffectiveTeamSize, 1, maxTeam);
            var probe = new ProjectState("", "", "", _size, null) { teamSize = team };
            _sumTeam.text = team == 1 ? "só você" : team + " pessoas";
            _sumPoints.text = gc.Projects.PhaseBudget(probe) + " pts";
            _sumWeeks.text = "~" + (loop != null ? loop.WeeksFor(_size) : 0) + " semanas";
            int eb = gc.Research != null ? gc.Research.EngineBudget : 0;
            int reference = engine != null ? engine.ReferenceBudgetFor(_size) : 200;
            _sumEngine.text = eb + " / " + reference + " EB";
            _sumEngine.style.color = eb >= reference ? Kit.Green : Kit.Red;
            long license = gc.LicenseCost(_console);
            _sumLicense.text = Kit.Money(license);
            long burn = gc.WeeklyBurn + (gc.Research != null ? gc.Research.EngineWeeklyCost : 0);
            _sumBurn.text = Kit.Money(burn);
            _sumTotal.text = Kit.Money(license);
            _sumSequel.text = _parentId != null ? "Sequência: herda o hype da franquia." : "IP novo: começa sem hype.";

            bool ok = _genre != null && gc.Run.company.cash >= license;
            LiveBits.SetEnabled(_start, ok);
            _sumTotal.style.color = gc.Run.company.cash >= license ? Kit.Cash : Kit.Red;
        }

        private void StartProject()
        {
            if (_genre == null) return;
            if (Ui.Gc.StartProject(_name.value, _genre, _size, _theme, _parentId, _console))
            {
                Sfx.Play(SfxId.Success);
                RequestClose();
            }
            else Sfx.Play(SfxId.Fail);
        }
    }

    /// <summary>Distribuição dos pontos da fase atual (GDD 5.2 / 5.3 / adendo 23.2) + crunch (5.6).</summary>
    internal sealed class PhaseAllocationLive : GalleryScreen
    {
        private static readonly string[] PhaseIcons = { Kit.IcBulb, Kit.IcBrush, Kit.IcMusic, Kit.IcCode, Kit.IcLayers };
        private static readonly string[] ParamIcons =
        {
            Kit.IcSliders, Kit.IcBook, Kit.IcBrush, Kit.IcGamepad, Kit.IcExpand, Kit.IcImage, Kit.IcFilm, Kit.IcPointer, Kit.IcMagic,
            Kit.IcMusic, Kit.IcVolume, Kit.IcMic, Kit.IcCode, Kit.IcBolt, Kit.IcFlask, Kit.IcGamepad, Kit.IcCube, Kit.IcBookOpen, Kit.IcScale
        };

        private ProjectState _p;
        private List<GameParameter> _params;
        private float[] _points;
        private Label[] _valueLabels;
        private Label _sumLabel, _weeksLabel;
        private VisualElement _sumFill, _activePhase;
        private bool _crunch;
        private int _budget, _cap;

        protected override void Build()
        {
            Scrim();
            var gc = Ui.Gc;
            _p = gc.Run.currentProject;
            var phase = (ProductionPhase)_p.phaseIndex;
            Color col = Kit.PhaseColors[_p.phaseIndex];
            _params = ParameterCatalog.AvailableFor(phase, _p.size);
            _budget = _p.phasePointBudget;
            _cap = gc.Projects.ParameterCap(_p);
            if (_points == null)
            {
                _points = new float[_params.Count];
                for (int i = 0; i < _points.Length; i++) _points[i] = Mathf.Floor(Mathf.Min(_cap, _budget / (float)_params.Count));
            }
            _valueLabels = new Label[_params.Count];

            var body = Window(ParameterCatalog.PhaseLabel(phase), PhaseIcons[_p.phaseIndex], col, 1140, 740, out _,
                "Fase " + (_p.phaseIndex + 1) + " de 5 de \"" + _p.name + "\" — onde a equipe coloca o esforço?");
            body.Add(Enter(PhaseStrip()));

            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1; row.style.marginTop = 16;
            body.Add(row);
            var left = Kit.Col(); left.style.flexGrow = 1;
            row.Add(left);

            var sumRow = Kit.Row();
            sumRow.Add(Kit.Caps("Pontos da equipe nesta fase", 11, Kit.Soft));
            sumRow.Add(Kit.Spacer());
            _sumLabel = Kit.Text("", 15, Kit.Ink, true);
            sumRow.Add(_sumLabel);
            left.Add(Enter(sumRow));
            var bar = Kit.Bar(12, col, out _sumFill); bar.style.marginTop = 6; bar.style.marginBottom = 14;
            left.Add(Enter(bar));

            for (int i = 0; i < _params.Count; i++) left.Add(Enter(ParamRow(i, col)));
            foreach (var gp in ParameterCatalog.ParametersOf(phase))
                if (!_params.Contains(gp))
                    left.Add(Enter(LockedRow(ParamIcons[(int)gp], ParameterCatalog.Label(gp),
                        "Projetos " + Labels.Size(ParameterCatalog.MinSizeOf(gp)) + " ou maior")));

            row.Add(Kit.Gap(24));
            var right = Kit.Col(); right.style.width = 330; right.style.flexShrink = 0;
            row.Add(right);

            var info = Kit.Tile();
            info.Add(LiveBits.Line(Kit.IcClock, "Duração da fase", "", out _weeksLabel));
            info.Add(LiveBits.Line(Kit.IcSliders, "Teto por parâmetro", _cap + " pts"));
            info.Add(LiveBits.Line(Kit.IcBug, "Bugs até agora", _p.bugCount.ToString()));
            info.Add(LiveBits.Line(Kit.IcFire, "Hype", Mathf.RoundToInt(_p.hype01 * 100f).ToString()));
            right.Add(Enter(info));

            var crunch = Kit.Tile(); crunch.style.marginTop = 12; crunch.style.backgroundColor = Kit.A(Kit.Red, 0.10f);
            var cr = Kit.Row();
            cr.Add(Kit.Badge(Kit.IcBolt, Kit.Red, 28));
            var crT = Kit.Col(); crT.style.marginLeft = 10; crT.style.flexGrow = 1; crT.style.flexShrink = 1;
            crT.Add(Kit.Text("Crunch", 14, Kit.Ink, true));
            crT.Add(Kit.Wrap("Termina a fase mais rápido, mas gera mais bugs e burnout.", 11, Kit.Soft));
            cr.Add(crT);
            cr.Add(Widgets.Toggle(_crunch, v => { _crunch = v; RefreshSum(); }));
            crunch.Add(cr);
            right.Add(Enter(crunch));

            var tip = LiveBits.Note(Kit.IcQuestion, Kit.Blue, "Cada gênero pede uma mistura diferente. As notas da crítica dizem se você acertou.");
            tip.style.marginTop = 12;
            right.Add(Enter(tip));

            right.Add(Kit.Spacer());
            var even = Kit.Ghost("Dividir por igual", Kit.IcScale, () =>
            {
                for (int i = 0; i < _points.Length; i++) _points[i] = Mathf.Floor(Mathf.Min(_cap, _budget / (float)_points.Length));
                Rebuild();
            });
            right.Add(Enter(even));
            var go = Kit.Button("Começar fase", Kit.IcPlay, col, Color.white, Confirm); go.style.marginTop = 8;
            right.Add(Enter(go));
            RefreshSum();
        }

        private VisualElement PhaseStrip()
        {
            var strip = Kit.Row(); strip.style.justifyContent = Justify.Center;
            for (int i = 0; i < ParameterCatalog.PhaseCount; i++)
            {
                bool on = i == _p.phaseIndex, done = i < _p.phaseIndex;
                Color c = Kit.PhaseColors[i];
                var item = Kit.Row();
                var circle = Kit.Badge(done ? Kit.IcCheck : PhaseIcons[i], on || done ? c : Kit.Sand, on ? 44 : 34,
                    on || done ? Color.white : Kit.A(c, 0.6f));
                if (on) _activePhase = circle;
                item.Add(circle);
                var l = Kit.Text(ParameterCatalog.PhaseLabel((ProductionPhase)i), on ? 14 : 12, on ? Kit.Ink : Kit.Soft, on); l.style.marginLeft = 8;
                item.Add(l);
                strip.Add(item);
                if (i == ParameterCatalog.PhaseCount - 1) continue;
                var dash = new VisualElement();
                dash.style.width = 34; dash.style.height = 3; Kit.Radius(dash, 2);
                dash.style.backgroundColor = done ? c : Kit.Track; dash.style.marginLeft = 10; dash.style.marginRight = 10;
                strip.Add(dash);
            }
            return strip;
        }

        private VisualElement ParamRow(int index, Color col)
        {
            var gp = _params[index];
            var row = Kit.Outline(14); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center;
            row.style.marginBottom = 10; Kit.Pad(row, 12, 16);
            row.Add(Kit.Badge(ParamIcons[(int)gp], col, 44));
            var name = Kit.Text(ParameterCatalog.Label(gp), 15, Kit.Ink, true);
            name.style.marginLeft = 12; name.style.width = 190; name.style.flexShrink = 0;
            row.Add(name);
            float max = Mathf.Min(_cap, _budget);
            var val = Kit.Text(Mathf.RoundToInt(_points[index]).ToString(), 20, col, true);
            val.style.width = 60; val.style.unityTextAlign = TextAnchor.MiddleRight;
            _valueLabels[index] = val;
            row.Add(Widgets.Slider(max > 0 ? _points[index] / max : 0f, col, v =>
            {
                _points[index] = Mathf.Round(v * max);
                val.text = Mathf.RoundToInt(_points[index]).ToString();
                RefreshSum();
            }, v =>
            {
                // O total da fase não passa do orçamento: o controle para no que ainda sobra.
                float others = 0f;
                for (int i = 0; i < _points.Length; i++) if (i != index) others += _points[i];
                float allowed = Mathf.Max(0f, Mathf.Floor(_budget - others));
                return max > 0 ? Mathf.Min(v, allowed / max) : 0f;
            }));
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

        private void RefreshSum()
        {
            float total = 0f;
            foreach (float v in _points) total += v;
            int pts = Mathf.RoundToInt(total);
            bool over = pts > _budget;
            _sumLabel.text = pts + " / " + _budget + " pts" + (over ? "  (será ajustado)" : pts < _budget ? "  (sobrando " + (_budget - pts) + ")" : "");
            _sumLabel.style.color = over ? Kit.Red : Kit.Ink;
            _sumFill.style.width = Length.Percent(Mathf.Clamp01(total / Mathf.Max(1, _budget)) * 100f);
            _sumFill.style.backgroundColor = over ? Kit.Red : Kit.PhaseColors[_p.phaseIndex];
            int weeks = Ui.Gc.Projects.WeeksForPhase(_p, _p.phaseIndex, _crunch);
            _weeksLabel.text = weeks + (weeks == 1 ? " semana" : " semanas");
        }

        private void Confirm()
        {
            var all = new float[ParameterCatalog.ParameterCount];
            for (int i = 0; i < _params.Count; i++) all[(int)_params[i]] = _points[i];
            if (!Ui.Gc.Projects.ConfirmAllocation(all, _crunch)) { Sfx.Play(SfxId.Fail); return; }
            Ui.RefreshHudAttributes();
            RequestClose();
        }

        protected override void OnTick(float dt)
        {
            if (_activePhase == null) return;
            float s = 1f + 0.06f * Mathf.Sin(Clock * 4f);
            _activePhase.style.scale = new Scale(new Vector2(s, s));
        }
    }

    /// <summary>Marketing do projeto em andamento (GDD 5.5 / 10 / adendo 23.6).</summary>
    internal sealed class MarketingLive : GalleryScreen
    {
        private Label _flame;

        protected override void Build()
        {
            Scrim();
            var gc = Ui.Gc;
            var p = gc.Run.currentProject;
            var body = Window("Marketing", Kit.IcBullhorn, Kit.Pink, 1100, 700, out _, p != null ? p.name : "Sem projeto");
            if (p == null || !gc.Run.HasActiveProject)
            {
                body.Add(LiveBits.Note(Kit.IcInfo, Kit.Blue, "Marketing só existe com um jogo em produção. Crie um projeto primeiro."));
                return;
            }

            float hype100 = p.hype01 * 100f;
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
            hc.Add(Kit.Text(Labels.HypeBand(hype100) + " no lançamento", 12, Kit.Ink, true));
            hr.Add(hc);
            hr.Add(Kit.Spacer());
            hr.Add(Kit.Chip("cai um pouco toda semana", Kit.Soft, false, Kit.IcArrowDown));
            hype.Add(hr);
            CountUp(hv, 0, hype100, 0.3f, 0.7f, v => Mathf.RoundToInt(v).ToString());

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
            Animate(0.3f, 0.7f, k => marker.style.left = Length.Percent(Mathf.Clamp(hype100, 0f, 99f) * k));
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
            wc.Add(Kit.Text(Kit.Short(p.wishlists), 26, Kit.Ink, true));
            wr.Add(wc);
            wish.Add(wr);
            var wn = Kit.Wrap("≈ " + Kit.Short(p.wishlists * 0.8f) + " vendas na semana de lançamento", 12, Kit.Soft); wn.style.marginTop = 10;
            wish.Add(wn);
            top.Add(Enter(wish));

            var sec = Kit.Caps("Ações de marketing — uma vez por jogo", 11, Kit.Soft); sec.style.marginTop = 18; sec.style.marginBottom = 10;
            body.Add(sec);
            var grid = LiveBits.Wrap();
            body.Add(grid);
            foreach (var type in MarketingManager.AllCampaigns) grid.Add(Enter(Campaign(type)));
        }

        private VisualElement Campaign(MarketingCampaign type)
        {
            var mk = Ui.Gc.Marketing;
            var info = mk.InfoOf(type);
            bool locked = !mk.IsUnlocked(type), used = mk.IsUsed(type);
            bool affordable = Ui.Gc.Run.company.cash >= info.Cost;

            var c = Kit.Outline(14); c.style.width = 250; c.style.marginRight = 12; c.style.marginBottom = 12; Kit.Pad(c, 14, 14);
            var r = Kit.Row();
            r.Add(Kit.Badge(locked ? Kit.IcLock : Labels.CampaignIcon(type), locked ? Kit.Track : Kit.A(Kit.Pink, 0.16f), 38, locked ? Kit.Faint : Kit.Pink));
            var t = Kit.Col(); t.style.marginLeft = 10; t.style.flexShrink = 1;
            t.Add(Kit.Text(info.Label, 14, locked ? Kit.Faint : Kit.Ink, true));
            t.Add(Kit.Text(info.Effect, 11, locked ? Kit.Faint : Kit.Soft));
            r.Add(t);
            c.Add(r);
            var f = Kit.Row(); f.style.marginTop = 12;
            if (locked)
            {
                f.Add(Kit.Chip("Pesquise no P&D", Kit.Soft, false, Kit.IcFlask));
                c.style.opacity = 0.8f;
            }
            else if (used)
            {
                f.Add(Kit.Chip("Feita", Kit.Green, false, Kit.IcCheck));
            }
            else
            {
                f.Add(Kit.Text(Kit.Money(info.Cost), 14, affordable ? Kit.Cash : Kit.Red, true)); f.Add(Kit.Spacer());
                var run = Kit.Button("Lançar", null, Kit.Pink, Color.white, () =>
                {
                    if (mk.RunCampaign(type)) { Sfx.Play(SfxId.Success); Rebuild(); } else Sfx.Play(SfxId.Fail);
                }, 30);
                LiveBits.SetEnabled(run, affordable);
                f.Add(run);
            }
            c.Add(f);
            return c;
        }

        protected override void OnTick(float dt)
        {
            if (_flame == null) return;
            float s = 1f + 0.07f * Mathf.Sin(Clock * 6f);
            _flame.style.scale = new Scale(new Vector2(s, s));
        }
    }

    /// <summary>Lançamento (GDD 5.7 / adendo 23.3): preço, budget de lançamento, bugs e a decisão de lançar.</summary>
    internal sealed class LaunchLive : GalleryScreen
    {
        private int _price;
        private long _budget;
        private Label _hypeVal, _hypeNote, _wishNote, _priceNote;
        private VisualElement _launchIcon;

        protected override void Build()
        {
            Scrim();
            var gc = Ui.Gc;
            var p = gc.Run.currentProject;
            if (p == null) { RequestClose(); return; }
            int reference = gc.Projects.ReferencePrice(p);
            if (_price <= 0) _price = reference;
            long maxBudget = System.Math.Max(0, System.Math.Min(gc.Run.company.cash, reference * 3000L));

            var body = Window("Lançamento", Kit.IcRocket, Kit.Blue, 1060, 660, out _, "\"" + p.name + "\" está pronto. Como vamos lançar?");
            var row = Kit.Row(Align.Stretch); row.style.flexGrow = 1;
            body.Add(row);
            var left = Kit.Col(); left.style.flexGrow = 1;
            row.Add(left);

            Section(left, "Preço");
            var priceRow = Kit.Row(); priceRow.style.marginBottom = 6;
            var priceLbl = Kit.Text("R$ " + _price, 34, Kit.Ink, true);
            priceLbl.style.width = 150; priceLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            int step = Mathf.Max(1, reference / 10);
            priceRow.Add(Kit.RoundButton(Kit.IcMinus, Kit.Sand, Kit.Ink, 40, () => { _price = Mathf.Max(step, _price - step); priceLbl.text = "R$ " + _price; RefreshPreview(); }));
            priceRow.Add(priceLbl);
            priceRow.Add(Kit.RoundButton(Kit.IcPlus, Kit.Sand, Kit.Ink, 40, () => { _price = Mathf.Min(reference * 3, _price + step); priceLbl.text = "R$ " + _price; RefreshPreview(); }));
            var hint = Kit.Chip("Preço de referência do escopo: R$ " + reference, Kit.Blue, false, Kit.IcInfo); hint.style.marginLeft = 18;
            priceRow.Add(hint);
            left.Add(Enter(priceRow));
            _priceNote = Kit.Text("", 12, Kit.Soft, true); _priceNote.style.marginBottom = 18;
            left.Add(Enter(_priceNote));

            Section(left, "Budget de marketing de lançamento");
            var mk = Kit.Row(); mk.style.marginBottom = 18;
            var mkVal = Kit.Text(Kit.Money(_budget), 16, Kit.Cash, true); mkVal.style.width = 120; mkVal.style.unityTextAlign = TextAnchor.MiddleRight;
            mk.Add(Widgets.Slider(maxBudget > 0 ? _budget / (float)maxBudget : 0f, Kit.Pink, v =>
            {
                _budget = (long)(Mathf.Round(v * maxBudget / 500f) * 500f);
                mkVal.text = Kit.Money(_budget);
                RefreshPreview();
            }));
            mk.Add(mkVal);
            left.Add(Enter(mk));

            var fore = Kit.Tile(16);
            fore.Add(Kit.Caps("Previsão", 11, Kit.Soft));
            var hrow = Kit.Row(); hrow.style.marginTop = 10;
            hrow.Add(Kit.Badge(Kit.IcFire, Kit.A(Kit.Pink, 0.16f), 30, Kit.Pink));
            var ht = Kit.Col(); ht.style.marginLeft = 10; ht.style.flexGrow = 1;
            ht.Add(Kit.Text("Hype no lançamento", 11, Kit.Soft));
            _hypeNote = Kit.Text("", 11, Kit.Ink, true);
            ht.Add(_hypeNote);
            hrow.Add(ht);
            _hypeVal = Kit.Text("", 18, Kit.Ink, true);
            hrow.Add(_hypeVal);
            fore.Add(hrow);
            var wrow = Kit.Row(); wrow.style.marginTop = 10;
            wrow.Add(Kit.Badge(Kit.IcHeart, Kit.A(Kit.Red, 0.16f), 30, Kit.Red));
            var wt = Kit.Col(); wt.style.marginLeft = 10; wt.style.flexGrow = 1;
            wt.Add(Kit.Text("Wishlists", 11, Kit.Soft));
            _wishNote = Kit.Text("", 11, Kit.Ink, true);
            wt.Add(_wishNote);
            wrow.Add(wt);
            wrow.Add(Kit.Text(Kit.Short(p.wishlists), 18, Kit.Ink, true));
            fore.Add(wrow);
            left.Add(Enter(fore));
            if (p.weeksWaiting > 0)
            {
                var wait = Kit.Wrap("Parado há " + p.weeksWaiting + " semana(s): o hype cai e a folha continua correndo.", 11, Kit.Red);
                wait.style.marginTop = 10;
                left.Add(wait);
            }

            row.Add(Kit.Gap(24));
            var right = Kit.Col(); right.style.width = 330; right.style.flexShrink = 0;
            row.Add(right);

            var loop = BalanceProvider.Get<GameLoopBalanceSO>();
            float penalty = BugCalculator.ScorePenalty(p.bugCount, p.size, loop);
            var bugs = Kit.Tile(); bugs.style.backgroundColor = Kit.A(penalty > 0f ? Kit.Red : Kit.Green, 0.10f);
            var br = Kit.Row();
            br.Add(Kit.Badge(Kit.IcBug, penalty > 0f ? Kit.Red : Kit.Green, 34));
            var bt = Kit.Col(); bt.style.marginLeft = 10; bt.style.flexShrink = 1;
            bt.Add(Kit.Text(p.bugCount + (p.bugCount == 1 ? " bug conhecido" : " bugs conhecidos"), 14, Kit.Ink, true));
            bt.Add(Kit.Wrap(penalty > 0f ? "Lançar assim derruba a nota da crítica." : "Dentro do tolerável — não afeta a nota.", 11, Kit.Soft));
            br.Add(bt);
            bugs.Add(br);
            if (p.bugCount > 0)
            {
                int rate = gc.Projects.BugFixRate(p);
                var fix = Kit.Ghost(p.fixingBugs ? "Continuar corrigindo" : "Corrigir bugs (−" + rate + " por semana)", Kit.IcWrench, () =>
                {
                    gc.Projects.SetFixingBugs(true);
                    Ui.Notify(Kit.IcWrench, Kit.Blue, "Equipe corrigindo bugs", "Clique no projeto quando quiser lançar.");
                    RequestClose();
                });
                fix.style.marginTop = 10;
                bugs.Add(fix);
            }
            right.Add(Enter(bugs));

            var mkBtn = Kit.Ghost("Fazer mais marketing", Kit.IcBullhorn, Ui.OpenMarketing); mkBtn.style.marginTop = 12;
            right.Add(Enter(mkBtn));
            right.Add(Kit.Spacer());
            var go = Kit.Button("Lançar jogo", Kit.IcRocket, Kit.Blue, Color.white, () =>
            {
                if (gc.Projects.Launch(_price, _budget)) { Sfx.Play(SfxId.Launch); RequestClose(); } else Sfx.Play(SfxId.Fail);
            }, 56);
            go.style.marginTop = 12;
            _launchIcon = go[0];
            right.Add(Enter(go));
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            var preview = Ui.Gc.Projects.PreviewLaunch(_price, _budget);
            float hype100 = preview.Hype01 * 100f;
            _hypeVal.text = Mathf.RoundToInt(hype100).ToString();
            _hypeNote.text = Labels.HypeBand(hype100);
            _wishNote.text = "≈ " + Kit.Short(preview.WishlistSales) + " vendas na 1ª semana";
            int pct = Mathf.RoundToInt((preview.PriceFactor - 1f) * 100f);
            _priceNote.text = pct == 0 ? "Preço na média: procura normal." : pct > 0 ? "Mais barato: cerca de +" + pct + "% de unidades." : "Mais caro: cerca de " + pct + "% de unidades.";
            _priceNote.style.color = pct < 0 ? Kit.Red : pct > 0 ? Kit.Green : Kit.Soft;
        }

        protected override void OnTick(float dt)
        {
            if (_launchIcon == null) return;
            _launchIcon.style.translate = new Translate(Mathf.Sin(Clock * 9f) * 1.5f, Mathf.Sin(Clock * 3f) * -3f, 0);
            _launchIcon.style.rotate = new Rotate(new Angle(Mathf.Sin(Clock * 3f) * 6f));
        }
    }

    /// <summary>Notas da crítica reveladas uma a uma + previsão de vendas por semana (adendo 23.3 / 23.4).</summary>
    internal sealed class ReviewsLive : GalleryScreen
    {
        private sealed class Reviewer { public Label score; public float value, revealAt; public bool done; }
        private sealed class Confetti { public float x, y, vx, vy, rot, spin, size; public Color c; }

        private static readonly string[] Outlets = { "GameSpotted", "IGM", "Polígono", "Kotaco" };

        private readonly ProjectState _p;
        private readonly List<Reviewer> _reviewers = new List<Reviewer>();
        private readonly List<Confetti> _confetti = new List<Confetti>();
        private VisualElement _confettiLayer, _finalDisc;
        private bool _finalDone;
        private float _finalAt;

        public ReviewsLive(ProjectState project) { _p = project; }

        private static string Quote(float score) =>
            score >= 9f ? "“Obrigatório. Vai marcar época.”" : score >= 8f ? "“Muito bom — fácil de recomendar.”"
            : score >= 7f ? "“Divertido, com alguns tropeços.”" : score >= 5f ? "“Mediano. Faltou algo.”"
            : score >= 3f ? "“Decepcionante.”" : "“Difícil defender.”";

        private static string Verdict(float score) =>
            score >= 9f ? "Obra-prima!" : score >= 8f ? "Sucesso de crítica!" : score >= 7f ? "Bem recebido" : score >= 5f ? "Recepção morna" : "A crítica não perdoou";

        protected override void Build()
        {
            Scrim(0.55f);
            var center = Kit.Centered();
            Root.Add(center);
            var card = Kit.Card(1080, 22); card.style.borderBottomWidth = 6; Kit.Pad(card, 26, 30);
            card.style.alignItems = Align.Center;
            center.Add(Enter(card, 0.05f, 30f));

            card.Add(Kit.Caps("As notas chegaram", 12, Kit.Soft, 3f));
            var title = Kit.Text(_p.name, 30, Kit.Ink, true); title.style.marginTop = 4;
            card.Add(title);
            card.Add(Kit.Text(_p.genreDisplay + (string.IsNullOrEmpty(_p.themeDisplay) ? "" : " · " + _p.themeDisplay)
                              + (string.IsNullOrEmpty(_p.consoleDisplay) ? "" : " · " + _p.consoleDisplay), 13, Kit.Soft));

            var row = Kit.Row(Align.Stretch); row.style.marginTop = 22;
            card.Add(row);
            int n = _p.reviewScores.Length;
            for (int i = 0; i < n; i++)
            {
                var rc = Kit.Tile(16); rc.style.width = 210; rc.style.marginLeft = 8; rc.style.marginRight = 8;
                rc.style.alignItems = Align.Center; Kit.Pad(rc, 16, 12);
                rc.Add(Kit.Text(Outlets[i % Outlets.Length], 14, Kit.Ink, true));
                var sc = Kit.Text("?", 50, Kit.Faint, true); sc.style.marginTop = 4;
                rc.Add(sc);
                var q = Kit.Wrap(Quote(_p.reviewScores[i]), 11, Kit.Soft); q.style.unityTextAlign = TextAnchor.MiddleCenter; q.style.marginTop = 4;
                rc.Add(q);
                row.Add(Enter(rc, 0.3f + i * 0.08f));
                float at = 1.0f + i * 0.85f;
                _reviewers.Add(new Reviewer { score = sc, value = _p.reviewScores[i], revealAt = at });
                Animate(at + 0.75f, 0.4f, k => q.style.opacity = k, Linear);
            }
            _finalAt = 1.0f + n * 0.85f + 0.3f;

            var lower = Kit.Row(Align.Stretch); lower.style.marginTop = 24; lower.style.alignSelf = Align.Stretch;
            card.Add(lower);
            var finalCol = Kit.Col(); finalCol.style.alignItems = Align.Center; finalCol.style.width = 230;
            _finalDisc = new VisualElement();
            _finalDisc.style.width = 132; _finalDisc.style.height = 132; Kit.Radius(_finalDisc, 66);
            _finalDisc.style.backgroundColor = Labels.ScoreColor(_p.finalScore); Kit.Border(_finalDisc, Kit.A(Color.white, 0.6f), 6);
            _finalDisc.style.alignItems = Align.Center; _finalDisc.style.justifyContent = Justify.Center;
            _finalDisc.Add(Kit.Text(Labels.Score(_p.finalScore), 52, Color.white, true));
            finalCol.Add(_finalDisc);
            var verdict = Kit.Text(Verdict(_p.finalScore), 16, Kit.Ink, true); verdict.style.marginTop = 10;
            finalCol.Add(verdict);
            lower.Add(finalCol);
            Pop(_finalDisc, _finalAt);
            Animate(_finalAt + 0.3f, 0.4f, k => verdict.style.opacity = k, Linear);

            // As vendas NÃO acontecem aqui: começam na semana seguinte e aparecem, semana a semana,
            // no gráfico sob o dinheiro no HUD.
            var salesCol = Kit.Col(); salesCol.style.flexGrow = 1; salesCol.style.marginLeft = 24; salesCol.style.justifyContent = Justify.Center;
            salesCol.Add(Kit.Caps("E agora?", 11, Kit.Soft));
            var facts = Kit.Row(); facts.style.marginTop = 10; facts.style.justifyContent = Justify.SpaceBetween;
            facts.Add(Stat("Hype no lançamento", _p.hype01 * 100f, _finalAt + 0.6f, v => Mathf.RoundToInt(v).ToString(), Kit.Pink));
            facts.Add(Stat("Wishlists", _p.wishlists, _finalAt + 0.8f, v => Kit.Short(v), Kit.Ink));
            facts.Add(Stat("Preço", _p.price, _finalAt + 1.0f, v => "R$ " + Mathf.RoundToInt(v), Kit.Cash));
            salesCol.Add(facts);
            var next = LiveBits.Note(Kit.IcChartBar, Kit.Blue,
                "As vendas começam na próxima semana. Acompanhe o gráfico embaixo do seu dinheiro: uma barra nova a cada semana, até o jogo parar de vender.");
            next.style.marginTop = 16;
            salesCol.Add(next);
            Animate(_finalAt + 0.9f, 0.4f, k => next.style.opacity = k, Linear);
            lower.Add(salesCol);

            var ok = Kit.Confirm("Continuar", Kit.IcArrowRight, RequestClose); ok.style.marginTop = 22; ok.style.alignSelf = Align.FlexEnd;
            card.Add(ok);
            Animate(_finalAt + 1.2f, 0.4f, k => ok.style.opacity = k, Linear);

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

        protected override void OnTick(float dt)
        {
            foreach (var r in _reviewers)
            {
                if (r.done || Clock < r.revealAt) continue;
                if (Clock < r.revealAt + 0.7f)
                {
                    r.score.text = Random.Range(1, 10).ToString();
                    r.score.style.color = Kit.Soft;
                    continue;
                }
                r.done = true;
                r.score.text = Mathf.RoundToInt(r.value).ToString();
                r.score.style.color = Labels.ScoreColor(r.value);
                Sfx.Play(SfxId.Review, 0.8f + r.value * 0.05f);
                var sc = r.score;
                Animate(0f, 0.35f, k =>
                {
                    float s = 1f + 0.35f * Mathf.Sin(k * Mathf.PI);
                    sc.style.scale = new Scale(new Vector2(s, s));
                }, Linear);
            }

            if (!_finalDone && Clock >= _finalAt)
            {
                _finalDone = true;
                bool good = _p.finalScore >= 7f;
                Sfx.Play(good ? SfxId.Fanfare : _p.finalScore >= 5f ? SfxId.Success : SfxId.Fail);
                if (good)
                {
                    Color[] cols = { Kit.Teal, Kit.Pink, Kit.Gold, Kit.Blue, Kit.Purple, Kit.Green };
                    var rect = Root.contentRect;
                    int count = _p.finalScore >= 9f ? 160 : 90;
                    for (int i = 0; i < count; i++)
                        _confetti.Add(new Confetti
                        {
                            x = rect.width * Random.Range(0.25f, 0.75f), y = rect.height * 0.45f,
                            vx = Random.Range(-420f, 420f), vy = Random.Range(-720f, -220f),
                            rot = Random.Range(0f, 360f), spin = Random.Range(-360f, 360f),
                            size = Random.Range(6f, 12f), c = cols[i % cols.Length]
                        });
                }
            }
            if (_finalDone && Clock > _finalAt + 0.6f)
            {
                float s = 1f + 0.03f * Mathf.Sin(Clock * 4f);
                _finalDisc.style.scale = new Scale(new Vector2(s, s));
            }

            if (_confetti.Count == 0) return;
            float height = Root.contentRect.height;
            for (int i = _confetti.Count - 1; i >= 0; i--)
            {
                var c = _confetti[i];
                c.vy += 900f * dt; c.vx *= 1f - dt * 0.8f;
                c.x += c.vx * dt; c.y += c.vy * dt; c.rot += c.spin * dt;
                if (c.y > height + 20f) _confetti.RemoveAt(i);
            }
            _confettiLayer.MarkDirtyRepaint();
        }

        private void DrawConfetti(MeshGenerationContext mgc)
        {
            if (_confetti.Count == 0) return;
            var p = mgc.painter2D;
            foreach (var c in _confetti)
            {
                Vector2 pos = new Vector2(c.x, c.y);
                p.fillColor = c.c;
                p.BeginPath();
                p.MoveTo(Kit.OnCircle(pos, c.size, c.rot)); p.LineTo(Kit.OnCircle(pos, c.size * 0.5f, c.rot + 90f));
                p.LineTo(Kit.OnCircle(pos, c.size, c.rot + 180f)); p.LineTo(Kit.OnCircle(pos, c.size * 0.5f, c.rot + 270f));
                p.ClosePath(); p.Fill();
            }
        }
    }
}
