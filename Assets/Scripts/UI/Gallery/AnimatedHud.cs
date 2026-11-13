using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>
    /// HUD de demonstracao 100% animada (showcase de direcao visual). Constroi tudo em
    /// codigo sobre o <see cref="UIDocument"/> do mesmo GameObject e anima por frame:
    /// circulos de recurso que enchem (liquido subindo), brilho pulsante que cresce com o
    /// preenchimento, e a cada ponto ganho dispara "onda de choque" + "+N" flutuante + pop.
    /// Icones vem de uma FontAsset (Font Awesome) carregada de Resources. Clicar num circulo
    /// dispara um ponto na hora (interativo). Ferramenta de design — nao e o HUD final.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class AnimatedHud : MonoBehaviour
    {
        // --- codepoints Font Awesome 6 Solid ---
        private const string IcBug = "", IcDesign = "", IcArt = "",
            IcAudio = "", IcTech = "", IcContent = "", IcResearch = "",
            IcFire = "", IcCoins = "", IcClock = "", IcUsers = "",
            IcPlus = "", IcPause = "", IcPlay = "", IcForward = "";
        // fases + atributos (GDD 5.2)
        private const string IcBulb = "", IcSliders = "", IcBook = "",
            IcBrush = "", IcExpand = "", IcImage = "", IcFilm = "",
            IcPointer = "", IcMagic = "", IcVolume = "", IcMic = "",
            IcCode = "", IcBolt = "", IcGamepad = "", IcCube = "",
            IcBookOpen = "", IcScale = "";

        private static Color RGB(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
        private static readonly Color Ink = RGB(36, 31, 23);
        private static readonly Color Soft = RGB(110, 100, 84);
        private static readonly Color Paper = RGB(251, 248, 241);
        private static readonly Color Teal = RGB(42, 157, 143);
        private static readonly Color Blue = RGB(47, 116, 201);

        private sealed class Area
        {
            public string name, icon;
            public Color color;
            public float value, rate, pop, phase, fillNow;
            public int shown, kind, param = -1;
            public float burstCooldown;
            public VisualElement disc, liquid, aura, holder;
            public Label countPill;
        }

        private sealed class Fx
        {
            public VisualElement ve;
            public float age, dur;
            public int kind; // 0 shockwave, 1 plus
            public Color color;
        }

        private sealed class Insect { public VisualElement ve; public float x, y, ang, speed, turn; }
        private sealed class Ripple { public float x, y, age, dur; }
        private sealed class Orb { public VisualElement ve; public Vector2 start, target; public float age, dur; }
        private sealed class MoneyTick { public VisualElement ve; public float age, dur, y; }
        private sealed class FireP { public float x, y, vx, vy, life, maxLife, size, seed; }

        private UIDocument _doc;
        private FontAsset _iconFont;
        private bool _built;
        private float _t;
        private readonly List<Area> _areas = new List<Area>();
        private readonly List<Fx> _fx = new List<Fx>();
        private VisualElement _progSand;
        private Label _hypeNum, _hypeIcon;
        private VisualElement _hypeFlameFx, _hypeHolder;
        private float _hypeCount = 340f, _hypeMag;
        private readonly List<FireP> _fire = new List<FireP>();
        private float _fireEmit;
        private VisualElement _pauseBtn;
        private VisualElement _root;
        // bugs
        private VisualElement _bugCard, _bugBadge, _bugLayer; private Label _bugVal, _bugCap, _bugBadgeIcon;
        private int _bugs = 4; private float _bugTimer;
        private readonly List<Insect> _insects = new List<Insect>();
        private const int MaxInsects = 16;
        // pesquisa
        private VisualElement _resCard, _resBadge, _resLayer; private Label _resVal;
        private int _research = 22; private float _resFlash, _orbTimer;
        private readonly List<Ripple> _ripples = new List<Ripple>();
        private readonly List<Orb> _orbFx = new List<Orb>();
        // dinheiro + relógio
        private VisualElement _moneyCard, _clockEl; private Label _cashVal, _dateVal;
        private int _week = 5, _year = 1;
        private float _cash = 150984f, _clockAngle, _moneyTickTimer;
        private readonly List<MoneyTick> _moneyTicks = new List<MoneyTick>();
        // progresso + fases
        private Label _progRight; private float _phaseProg = 0.38f;
        private int _phaseIndex;
        private VisualElement _attrHost;
        private readonly List<Area> _attrAreas = new List<Area>();
        private PhaseDef[] _phases;
        private VisualElement[] _phaseCircles; private Label[] _phaseIcons, _phaseNames;
        private static readonly Color[] PhaseColors = {
            new Color(42f / 255f, 157f / 255f, 143f / 255f),   // Pré-produção (teal)
            new Color(214f / 255f, 86f / 255f, 143f / 255f),   // Arte (rosa)
            new Color(139f / 255f, 109f / 255f, 214f / 255f),  // Áudio (roxo)
            new Color(63f / 255f, 134f / 255f, 216f / 255f),   // Tecnologia (azul)
            new Color(76f / 255f, 158f / 255f, 86f / 255f) };  // Conteúdo (verde)

        /// <summary>Feed do jogo real. Nulo = HUD roda a simulação de vitrine (galeria).</summary>
        public HudLive Live { get; set; }

        /// <summary>Intenções disparadas pelos botões do HUD.</summary>
        public HudActions Actions { get; } = new HudActions();

        private static readonly int[] PhaseParamStart = { 0, 5, 9, 12, 15 };
        private bool IsLive => Live != null;
        private VisualElement[] _timeBtns;
        private Label[] _timeIcons;
        private Label _teamVal, _titleLbl, _subLbl, _progCaption, _valuationVal, _companyVal, _alertLbl;
        private VisualElement _phaseStrip, _flameWrap, _track, _progRow, _projectCard, _alertPill;
        private int _lastResearch = -1, _shownPhase = -1;
        private bool _shownHasProject = true, _hypeSynced;

        private void Awake() => _doc = GetComponent<UIDocument>();

        private void Update()
        {
            if (!_built)
            {
                if (_doc != null && _doc.rootVisualElement != null) Build();
                return;
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _t += dt;
            Animate(dt);
        }

        // ---------------- build ----------------
        private void Build()
        {
            _iconFont = Resources.Load<FontAsset>("IconFont");
            var root = _doc.rootVisualElement;
            _root = root;
            root.Clear();
            root.style.flexGrow = 1;

            // camada de efeitos (por cima, sem bloquear clique)
            // top-center: card do projeto + fileira de circulos
            var top = new VisualElement();
            top.style.position = Position.Absolute;
            top.style.top = 16; top.style.left = 0; top.style.right = 0;
            top.style.alignItems = Align.Center;
            root.Add(top);

            InitPhases();

            // card centralizado; foguinho do hype posicionado ao lado (sem deslocar o card)
            var cardRow = new VisualElement();
            cardRow.style.position = Position.Relative;
            cardRow.style.width = 300;
            cardRow.Add(BuildProjectCard());
            var flame = BuildHypeFlame();
            flame.style.position = Position.Absolute;
            flame.style.left = Length.Percent(100); flame.style.top = 10; flame.style.marginLeft = 12;
            cardRow.Add(flame);
            _flameWrap = flame;
            top.Add(cardRow);

            // subdivisões da fase ATIVA (cada uma com sua aura própria) — centralizadas sob o card
            _attrHost = new VisualElement();
            _attrHost.style.marginTop = 18;
            top.Add(_attrHost);
            BuildPhaseAttrs();
            UpdatePhaseHighlight();

            // top-left: bugs + pesquisa
            BuildCorner(root, true);
            // top-right: caixa + data
            BuildCorner(root, false);
            // bottom: barra de comando
            BuildCommandBar(root);

            _built = true;
        }

        private VisualElement BuildProjectCard()
        {
            var card = Card(300);
            card.style.alignItems = Align.Center;

            _projectCard = card;
            _titleLbl = Text("Lendas de Aethel", 17, Ink, true);
            _subLbl = Text("Action RPG · Terror", 11, Soft, false);
            card.Add(_titleLbl);
            card.Add(_subLbl);
            card.RegisterCallback<ClickEvent>(_ => Actions.Project?.Invoke());

            var lblRow = new VisualElement();
            _progRow = lblRow;
            lblRow.style.flexDirection = FlexDirection.Row; lblRow.style.justifyContent = Justify.SpaceBetween;
            lblRow.style.marginTop = 10; lblRow.style.width = Length.Percent(100);
            _progCaption = Text("Progresso da fase", 10, Soft, true);
            lblRow.Add(_progCaption);
            _progRight = Text("Semana 5 / 12", 10, Soft, true);
            lblRow.Add(_progRight);
            card.Add(lblRow);

            var track = new VisualElement();
            track.style.height = 15; track.style.width = Length.Percent(100); track.style.marginTop = 4;
            track.style.backgroundColor = RGB(232, 225, 210); Radius(track, 7); track.style.overflow = Overflow.Hidden;
            _progSand = new VisualElement();
            _progSand.style.position = Position.Absolute;
            _progSand.style.left = 0; _progSand.style.right = 0; _progSand.style.top = 0; _progSand.style.bottom = 0;
            _progSand.pickingMode = PickingMode.Ignore;
            _progSand.generateVisualContent += DrawSand;
            track.Add(_progSand); card.Add(track);
            _track = track;

            _phaseStrip = BuildPhaseStrip();
            card.Add(_phaseStrip);
            return card;
        }

        private void UpdatePhase(float dt)
        {
            if (IsLive) { UpdatePhaseLive(); return; }
            _phaseProg += dt * 0.055f;
            if (_phaseProg >= 1f)
            {
                _phaseProg = 0f;
                _phaseIndex = (_phaseIndex + 1) % _phases.Length;
                BuildPhaseAttrs();
                UpdatePhaseHighlight();
            }
            UpdateSand(dt);
            int weeks = _phases[_phaseIndex].weeks;
            int wk = Mathf.Clamp(1 + Mathf.FloorToInt(_phaseProg * weeks), 1, weeks);
            _progRight.text = "Semana " + wk + " / " + weeks;
        }

        private Area AddCircle(VisualElement parent, string name, string icon, Color color, float rate, int start, int kind, int size)
        {
            int border = size >= 56 ? 4 : 3;
            int inner = size - border * 2;
            int iconSz = Mathf.RoundToInt(size * 0.42f);
            int pad = kind >= 0 ? 16 : 10;

            var wrap = new VisualElement();
            wrap.style.alignItems = Align.Center;
            wrap.style.marginLeft = 20; wrap.style.marginRight = 20;

            var holder = new VisualElement();
            holder.style.width = size + pad; holder.style.height = size + pad;
            holder.style.alignItems = Align.Center; holder.style.justifyContent = Justify.Center;

            VisualElement aura = null;
            if (kind >= 0)
            {
                aura = new VisualElement();
                aura.style.position = Position.Absolute;
                aura.style.left = -12; aura.style.right = -12; aura.style.top = -12; aura.style.bottom = -12;
                aura.pickingMode = PickingMode.Ignore;
                holder.Add(aura);
            }

            var disc = new VisualElement();
            disc.style.width = size; disc.style.height = size; Radius(disc, size / 2f);
            disc.style.backgroundColor = Color.white; SetBorder(disc, color, border);
            disc.style.overflow = Overflow.Hidden;
            disc.style.alignItems = Align.Center; disc.style.justifyContent = Justify.Center;

            var ic = Icon(icon, iconSz, color);
            ic.style.position = Position.Absolute;
            ic.style.left = 0; ic.style.right = 0; ic.style.top = 0; ic.style.bottom = 0;
            disc.Add(ic);

            var liquid = new VisualElement();
            liquid.style.position = Position.Absolute;
            liquid.style.left = 0; liquid.style.right = 0; liquid.style.bottom = 0;
            liquid.style.height = Length.Percent(0);
            liquid.style.backgroundColor = new Color(color.r, color.g, color.b, 0.92f);
            liquid.style.overflow = Overflow.Hidden;
            disc.Add(liquid);

            var icW = Icon(icon, iconSz, Color.white);
            icW.style.position = Position.Absolute;
            icW.style.left = 0; icW.style.right = 0; icW.style.bottom = 0;
            icW.style.height = inner;   // área interna do disco → alinha com o ícone colorido
            liquid.Add(icW);
            holder.Add(disc);

            var pill = Text(start.ToString(), size >= 56 ? 11 : 9, Color.white, true);
            pill.style.position = Position.Absolute; pill.style.top = -4; pill.style.right = -4;
            pill.style.backgroundColor = color; Radius(pill, 9);
            pill.style.paddingLeft = 6; pill.style.paddingRight = 6;
            pill.style.paddingTop = 1; pill.style.paddingBottom = 1;
            pill.style.unityTextAlign = TextAnchor.MiddleCenter;
            holder.Add(pill);

            wrap.Add(holder);
            wrap.Add(Text(name, size >= 56 ? 10 : 9, Soft, true));
            parent.Add(wrap);

            var area = new Area
            {
                name = name, icon = icon, color = color, rate = rate, value = start,
                shown = start, kind = kind, disc = disc, liquid = liquid, aura = aura, holder = holder, countPill = pill
            };
            if (aura != null) aura.generateVisualContent += mgc => DrawAura(mgc, area);
            _areas.Add(area);

            disc.RegisterCallback<ClickEvent>(_ => { if (!IsLive) { area.value += 1f; Burst(area); } });
            return area;
        }

        // ---------- fases internalizadas + subdivisões da fase ativa ----------
        private sealed class PhaseDef
        {
            public string name, shortName, icon;
            public Color color; public int weeks;
            public (string n, string icon, int aura)[] attrs;
        }

        private void InitPhases()
        {
            _phases = new[]
            {
                new PhaseDef { name = "Pré-produção", shortName = "Pré-prod", icon = IcBulb, color = PhaseColors[0], weeks = 3, attrs = new[] {
                    ("Game Design", IcSliders, 0), ("Narrativa", IcBook, 5), ("Direção de Arte", IcBrush, 1), ("Game Feel", IcGamepad, 6), ("Escopo", IcExpand, 7) } },
                new PhaseDef { name = "Arte e Visual", shortName = "Arte", icon = IcArt, color = PhaseColors[1], weeks = 5, attrs = new[] {
                    ("Gráficos", IcImage, 8), ("Animação", IcFilm, 9), ("UI/UX", IcPointer, 0), ("VFX", IcMagic, 10) } },
                new PhaseDef { name = "Áudio", shortName = "Áudio", icon = IcAudio, color = PhaseColors[2], weeks = 3, attrs = new[] {
                    ("Trilha Sonora", IcAudio, 2), ("Sound Design", IcVolume, 11), ("Dublagem", IcMic, 12) } },
                new PhaseDef { name = "Tecnologia", shortName = "Tech", icon = IcTech, color = PhaseColors[3], weeks = 5, attrs = new[] {
                    ("Programação", IcCode, 13), ("Performance", IcBolt, 14), ("Estabilidade", IcResearch, 0) } },
                new PhaseDef { name = "Conteúdo e Design", shortName = "Conteúdo", icon = IcContent, color = PhaseColors[4], weeks = 4, attrs = new[] {
                    ("Gameplay", IcGamepad, 6), ("Conteúdo", IcCube, 15), ("Narrativa Impl.", IcBookOpen, 16), ("Balanceamento", IcScale, 17) } },
            };
        }

        private void UpdateSand(float dt)
        {
            if (_progSand != null) _progSand.MarkDirtyRepaint();
        }

        // Água enchendo na horizontal (esquerda→direita), com superfície ondulando.
        private void DrawSand(MeshGenerationContext mgc)
        {
            var r = _progSand.contentRect;
            if (r.width < 4f || r.height < 4f) return;
            float prog = Mathf.Clamp01(_phaseProg);
            if (prog <= 0.002f) return;
            var p = mgc.painter2D;
            float w = r.width, h = r.height;
            Color bc = _phases[_phaseIndex].color;
            Color body = new Color(bc.r, bc.g, bc.b, 0.92f);
            Color surf = Color.Lerp(new Color(bc.r, bc.g, bc.b, 1f), Color.white, 0.5f);

            // amplitude da ondulação (some quando está quase cheio p/ terminar reto)
            float amp = 2.6f * (1f - Mathf.Clamp01((prog - 0.9f) / 0.1f));
            float baseX = prog * w;
            float Surface(float y) => baseX
                + Mathf.Sin(y * 0.55f + _t * 4.2f) * amp
                + Mathf.Sin(y * 1.1f - _t * 2.7f) * amp * 0.5f;

            const int steps = 12;
            // corpo
            p.fillColor = body;
            p.BeginPath();
            p.MoveTo(new Vector2(0f, 0f));
            p.LineTo(new Vector2(0f, h));
            for (int i = 0; i <= steps; i++)
            {
                float y = h - h * i / steps;              // de baixo p/ cima pela superfície ondulada
                p.LineTo(new Vector2(Surface(y), y));
            }
            p.LineTo(new Vector2(0f, 0f));
            p.ClosePath(); p.Fill();

            // brilho na superfície (linha ondulada)
            p.strokeColor = surf; p.lineWidth = 2f;
            p.BeginPath();
            for (int i = 0; i <= steps; i++)
            {
                float y = h * i / steps;
                var pt = new Vector2(Surface(y), y);
                if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
            }
            p.Stroke();

            // bolhinhas subindo dentro da água
            for (int k = 0; k < 4; k++)
            {
                float bx = baseX * (0.2f + 0.7f * ((k * 0.27f + _t * 0.15f) % 1f));
                float by = h * (0.9f - ((_t * 0.5f + k * 0.3f) % 1f) * 0.8f);
                float a = 0.35f * (0.5f + 0.5f * Mathf.Sin(_t * 3f + k));
                p.fillColor = new Color(1f, 1f, 1f, a);
                p.BeginPath(); p.Arc(new Vector2(bx, by), 1.2f, new Angle(0f), new Angle(360f)); p.Fill();
            }
        }

        private VisualElement BuildPhaseStrip()
        {
            var strip = new VisualElement();
            strip.style.flexDirection = FlexDirection.Row; strip.style.justifyContent = Justify.SpaceBetween;
            strip.style.marginTop = 12; strip.style.width = Length.Percent(100);
            _phaseCircles = new VisualElement[_phases.Length];
            _phaseIcons = new Label[_phases.Length];
            _phaseNames = new Label[_phases.Length];
            for (int i = 0; i < _phases.Length; i++)
            {
                var it = new VisualElement(); it.style.alignItems = Align.Center; it.style.flexGrow = 1;
                var c = new VisualElement();
                c.style.width = 30; c.style.height = 30; Radius(c, 15);
                c.style.alignItems = Align.Center; c.style.justifyContent = Justify.Center;
                var icn = Icon(_phases[i].icon, 15, Color.white);
                c.Add(icn); it.Add(c);
                var nm = Text(_phases[i].shortName, 8, Soft, true); nm.style.marginTop = 3;
                it.Add(nm);
                strip.Add(it);
                _phaseCircles[i] = c; _phaseIcons[i] = icn; _phaseNames[i] = nm;
            }
            return strip;
        }

        private void UpdatePhaseHighlight()
        {
            for (int i = 0; i < _phaseCircles.Length; i++)
            {
                bool on = i == _phaseIndex;
                _phaseCircles[i].style.backgroundColor = on ? _phases[i].color : RGB(236, 231, 219);
                _phaseCircles[i].style.scale = new Scale(new Vector2(on ? 1.18f : 1f, on ? 1.18f : 1f));
                _phaseIcons[i].style.color = on ? Color.white : new Color(_phases[i].color.r, _phases[i].color.g, _phases[i].color.b, 0.55f);
                _phaseNames[i].style.color = on ? Ink : Soft;
                _phaseNames[i].style.unityFontStyleAndWeight = on ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void BuildPhaseAttrs()
        {
            foreach (var a in _attrAreas) _areas.Remove(a);
            _attrAreas.Clear();
            _attrHost.Clear();

            var ph = _phases[_phaseIndex];
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.Center;
            for (int ai = 0; ai < ph.attrs.Length; ai++)
            {
                var at = ph.attrs[ai];
                int param = PhaseParamStart[_phaseIndex] + ai;
                if (IsLive)
                {
                    if (!Live.paramVisible[param]) continue;
                    int start = Mathf.FloorToInt(Live.paramPoints[param]);
                    var live = AddCircle(row, at.n, at.icon, ph.color, 0f, start, at.aura, 56);
                    live.param = param; live.value = Live.paramPoints[param];
                    _attrAreas.Add(live);
                    continue;
                }
                var area = AddCircle(row, at.n, at.icon, ph.color, Random.Range(0.14f, 0.26f), Random.Range(1, 9), at.aura, 56);
                _attrAreas.Add(area);
            }
            _attrHost.Add(row);
        }

        private void BuildCorner(VisualElement root, bool left)
        {
            var col = new VisualElement();
            col.style.position = Position.Absolute;
            col.style.top = 16;
            if (left) col.style.left = 16; else col.style.right = 16;

            if (left)
            {
                col.Add(BuildBugCard());
                col.Add(BuildResearchCard());
            }
            else
            {
                col.style.alignItems = Align.FlexEnd;
                col.Add(BuildMoneyCard());
                col.Add(BuildSalesCard());
            }
            root.Add(col);
        }

        private VisualElement MiniStat(string icon, Color color, string val, string cap)
        {
            var card = Card(0);
            card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center;
            card.style.marginBottom = 8; card.style.paddingTop = 7; card.style.paddingBottom = 7;
            var badge = new VisualElement();
            badge.style.width = 26; badge.style.height = 26; Radius(badge, 13);
            badge.style.backgroundColor = color; badge.style.alignItems = Align.Center; badge.style.justifyContent = Justify.Center;
            badge.Add(Icon(icon, 14, Color.white));
            card.Add(badge);
            var col = new VisualElement(); col.style.marginLeft = 9;
            var v = Text(val, 15, Ink, true); v.name = "val"; col.Add(v);
            col.Add(Text(cap, 9, Soft, true));
            card.Add(col);
            return card;
        }

        private void BuildCommandBar(VisualElement root)
        {
            // selo de alerta (ex.: "Distribua os pontos da fase") logo acima da barra
            _alertPill = new VisualElement();
            _alertPill.style.position = Position.Absolute;
            _alertPill.style.bottom = 92; _alertPill.style.left = Length.Percent(50);
            _alertPill.style.translate = new Translate(Length.Percent(-50), 0, 0);
            _alertPill.style.backgroundColor = Blue; Radius(_alertPill, 16);
            _alertPill.style.paddingLeft = 16; _alertPill.style.paddingRight = 16;
            _alertPill.style.paddingTop = 7; _alertPill.style.paddingBottom = 7;
            _alertPill.style.display = DisplayStyle.None;
            _alertLbl = Text("", 13, Color.white, true);
            _alertPill.Add(_alertLbl);
            _alertPill.RegisterCallback<ClickEvent>(_ => Actions.Project?.Invoke());
            root.Add(_alertPill);

            var bar = Card(0);
            bar.style.position = Position.Absolute;
            bar.style.bottom = 16; bar.style.left = Length.Percent(50);
            bar.style.translate = new Translate(Length.Percent(-50), 0, 0);
            bar.style.flexDirection = FlexDirection.Row; bar.style.alignItems = Align.Center;
            bar.style.paddingLeft = 10; bar.style.paddingRight = 10;

            // controle de tempo: pausar / normal / rápido
            var tseg = new VisualElement();
            tseg.style.flexDirection = FlexDirection.Row; tseg.style.backgroundColor = RGB(241, 236, 224);
            Radius(tseg, 9); tseg.style.paddingTop = 3; tseg.style.paddingBottom = 3;
            tseg.style.paddingLeft = 3; tseg.style.paddingRight = 3;
            _timeBtns = new VisualElement[3];
            _timeIcons = new Label[3];
            string[] icons = { IcPause, IcPlay, IcForward };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                _timeBtns[i] = TimeBtn(icons[i], i == 0, out _timeIcons[i]);
                _timeBtns[i].RegisterCallback<ClickEvent>(_ =>
                {
                    if (idx == 0) Actions.Pause?.Invoke();
                    else if (idx == 1) Actions.Play?.Invoke();
                    else Actions.Fast?.Invoke();
                });
                tseg.Add(_timeBtns[i]);
            }
            _pauseBtn = _timeBtns[0];
            bar.Add(tseg);

            var team = CmdStat(IcUsers, "25", out _teamVal);
            bar.Add(team);
            bar.Add(Spacer());

            // painéis de gestão (GDD 16.2)
            (string icon, Color color)[] panels =
            {
                ("", Kit.Cash), ("", Kit.Blue), (IcUsers, Kit.Orange), ("", Kit.Purple),
                (IcResearch, Kit.Teal), ("", Kit.Pink), ("", Kit.Sky)
            };
            for (int i = 0; i < panels.Length; i++)
            {
                int idx = i;
                var b = IconButton(panels[i].icon, 15, Paper, panels[i].color, 36, true);
                b.style.marginLeft = 5;
                b.RegisterCallback<ClickEvent>(_ => Actions.Panel?.Invoke(idx));
                bar.Add(b);
            }

            var plus = IconButton(IcPlus, 25, Blue, Color.white, 46, true);  // Novo projeto (+ maior)
            plus.style.marginLeft = 12;
            plus.RegisterCallback<ClickEvent>(_ => Actions.NewProject?.Invoke());
            bar.Add(plus);

            var menu = IconButton("", 14, Paper, Soft, 34, true);
            menu.RegisterCallback<ClickEvent>(_ => Actions.Menu?.Invoke());
            bar.Add(menu);
            root.Add(bar);
        }

        private VisualElement TimeBtn(string icon, bool on, out Label iconLabel)
        {
            var b = new VisualElement();
            b.style.width = 34; b.style.height = 30; Radius(b, 7);
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.style.marginLeft = 2; b.style.marginRight = 2;
            if (on) b.style.backgroundColor = Teal;
            iconLabel = Icon(icon, 14, on ? Color.white : Soft);
            b.Add(iconLabel);
            return b;
        }

        // ---------------- modo vivo (feed do jogo real) ----------------

        // ---------------- gráfico de vendas semana a semana (sob o dinheiro) ----------------
        private VisualElement _salesCard, _salesChart;
        private Label _salesTitle, _salesCaption;
        private readonly List<float> _mockSales = new List<float>();
        private float _mockSalesTimer, _salesPop;
        private int _salesShown;
        private const int SalesSlots = 26;

        private VisualElement BuildSalesCard()
        {
            var card = Card(210);
            card.style.marginTop = 8; card.style.paddingTop = 8; card.style.paddingBottom = 8;
            _salesCard = card;
            _salesTitle = Text("Vendas · Lendas de Aethel", 10, Soft, true);
            _salesTitle.style.overflow = Overflow.Hidden;
            card.Add(_salesTitle);
            _salesChart = new VisualElement();
            _salesChart.style.height = 46; _salesChart.style.marginTop = 4;
            _salesChart.pickingMode = PickingMode.Ignore;
            _salesChart.generateVisualContent += DrawSales;
            card.Add(_salesChart);
            _salesCaption = Text("", 11, Ink, true);
            _salesCaption.style.marginTop = 3;
            card.Add(_salesCaption);
            card.RegisterCallback<ClickEvent>(_ => Actions.Panel?.Invoke(1));
            return card;
        }

        private IReadOnlyList<float> SalesData => IsLive ? Live.salesBars : _mockSales;

        private void UpdateSales(float dt)
        {
            if (_salesCard == null) return;
            if (IsLive)
            {
                bool show = Live.salesBars.Count > 0 || Live.salesAwaitingReview;
                _salesCard.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (!show) { _salesShown = 0; return; }
                _salesTitle.text = Live.salesTitle;
                _salesCaption.text = Live.salesCaption;
            }
            else
            {
                _mockSalesTimer += dt;
                if (_mockSalesTimer > 1.1f)
                {
                    _mockSalesTimer = 0f;
                    if (_mockSales.Count >= SalesSlots) _mockSales.Clear();
                    _mockSales.Add(9200f * Mathf.Pow(0.82f, _mockSales.Count) * Random.Range(0.9f, 1.1f));
                    _salesCaption.text = "Semana " + _mockSales.Count + " · " + FmtK(Mathf.RoundToInt(_mockSales[_mockSales.Count - 1])) + " un.";
                }
            }
            int count = SalesData.Count;
            if (count != _salesShown) { _salesPop = count > _salesShown ? 1f : 0f; _salesShown = count; }
            _salesPop = Mathf.Max(0f, _salesPop - dt * 3f);
            _salesChart.MarkDirtyRepaint();
        }

        private void DrawSales(MeshGenerationContext mgc)
        {
            var r = _salesChart.contentRect;
            if (r.width < 8f || r.height < 8f) return;
            var p = mgc.painter2D;
            var data = SalesData;
            float slot = r.width / SalesSlots;
            p.strokeColor = RGB(232, 225, 210); p.lineWidth = 1.5f;
            p.BeginPath(); p.MoveTo(new Vector2(0f, r.height - 0.75f)); p.LineTo(new Vector2(r.width, r.height - 0.75f)); p.Stroke();
            if (data.Count == 0) return;
            float max = 1f;
            for (int i = 0; i < data.Count; i++) if (data[i] > max) max = data[i];
            for (int i = 0; i < data.Count && i < SalesSlots; i++)
            {
                bool last = i == data.Count - 1;
                float h = Mathf.Max(2f, data[i] / max * (r.height - 4f));
                if (last) h *= 1f - 0.85f * _salesPop * _salesPop;
                float x = i * slot + 1f, w = Mathf.Max(2f, slot - 2f);
                p.fillColor = last ? Blue : new Color(Blue.r, Blue.g, Blue.b, 0.5f);
                p.BeginPath();
                p.MoveTo(new Vector2(x, r.height - 1.5f)); p.LineTo(new Vector2(x, r.height - 1.5f - h));
                p.LineTo(new Vector2(x + w, r.height - 1.5f - h)); p.LineTo(new Vector2(x + w, r.height - 1.5f));
                p.ClosePath(); p.Fill();
            }
        }

        /// <summary>Mostra uma variação de caixa flutuando sob o card de dinheiro (chamado pelo binder).</summary>
        public void ShowMoneyTick(long delta)
        {
            if (_built && delta != 0) SpawnMoneyTick(delta);
        }

        private void UpdatePhaseLive()
        {
            bool has = Live.hasProject;
            if (has != _shownHasProject)
            {
                _shownHasProject = has;
                var show = has ? DisplayStyle.Flex : DisplayStyle.None;
                _phaseStrip.style.display = show; _track.style.display = show; _progRow.style.display = show;
                _attrHost.style.display = show; _flameWrap.style.display = show;
                _shownPhase = -1;
            }
            _titleLbl.text = Live.title;
            _subLbl.text = Live.subtitle;
            if (!has) return;

            int phase = Mathf.Clamp(Live.phaseIndex, 0, _phases.Length - 1);
            if (phase != _shownPhase)
            {
                _shownPhase = phase;
                _phaseIndex = phase;
                BuildPhaseAttrs();
                UpdatePhaseHighlight();
            }
            _phaseProg = Live.phaseProgress01;
            _progCaption.text = Live.progressCaption;
            _progRight.text = Live.progressLabel;
            UpdateSand(0f);
        }

        /// <summary>Força a remontagem dos círculos (ex.: projeto novo na mesma fase).</summary>
        public void RefreshAttributes() => _shownPhase = -1;

        private void UpdateLiveChrome()
        {
            int active = Live.paused ? 0 : Live.speedIndex <= 0 ? 1 : 2;
            for (int i = 0; i < _timeBtns.Length; i++)
            {
                bool on = i == active;
                _timeBtns[i].style.backgroundColor = on ? (i == 0 ? RGB(200, 90, 74) : Teal) : new Color(0, 0, 0, 0);
                _timeIcons[i].style.color = on ? Color.white : Soft;
            }
            _teamVal.text = "  " + Live.team;

            bool alert = !string.IsNullOrEmpty(Live.alert);
            _alertPill.style.display = alert ? DisplayStyle.Flex : DisplayStyle.None;
            if (alert)
            {
                _alertLbl.text = Live.alert;
                float s = 1f + 0.04f * Mathf.Sin(_t * 5f);
                _alertPill.style.scale = new Scale(new Vector2(s, s));
            }
            if (_projectCard != null)
            {
                float glow = Live.needsAttention ? 0.5f + 0.5f * Mathf.Sin(_t * 5f) : 0f;
                _projectCard.style.borderBottomColor = Color.Lerp(new Color(0, 0, 0, 0.12f), Blue, glow);
            }
        }

        private void UpdateMoneyLive(float dt)
        {
            _clockAngle = Live.weekProgress01 * 360f;
            _clockEl.MarkDirtyRepaint();
            _dateVal.text = "  Semana " + Live.week + " · Ano " + Live.year;
            _companyVal.text = Live.companyLine;
            bool negative = Live.cash < 0;
            _cashVal.text = "  " + (negative ? "-R$ " : "R$ ") + FmtMoney((int)Mathf.Min(int.MaxValue, Mathf.Abs(Live.cash)));
            _cashVal.style.color = negative ? RGB(200, 70, 60) : RGB(30, 120, 60);
            _valuationVal.text = "  Valuation " + Kit.Money(Live.valuation) + (Live.valuationRising ? "  ▲" : "  ▼");
            _valuationVal.style.color = Live.valuationRising ? Teal : RGB(200, 90, 74);

            for (int i = _moneyTicks.Count - 1; i >= 0; i--)
            {
                var m = _moneyTicks[i]; m.age += dt;
                float t = Mathf.Clamp01(m.age / m.dur);
                m.ve.style.top = m.y - 34f * t;
                m.ve.style.opacity = t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f;
                if (t >= 1f) { m.ve.RemoveFromHierarchy(); _moneyTicks.RemoveAt(i); }
            }
        }


        private VisualElement CmdStat(string icon, string val, out Label value)
        {
            var c = new VisualElement(); c.style.flexDirection = FlexDirection.Row; c.style.alignItems = Align.Center;
            c.style.marginLeft = 12; c.style.marginRight = 6;
            c.Add(Icon(icon, 17, Soft));
            value = Text("  " + val, 15, Ink, true);
            c.Add(value);
            return c;
        }

        private VisualElement IconButton(string icon, int iconSz, Color bg, Color fg, int size, bool round)
        {
            var b = new VisualElement();
            b.style.width = size; b.style.height = size;
            Radius(b, round ? size / 2f : 12);
            b.style.backgroundColor = bg;
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.style.marginLeft = 8;
            b.Add(Icon(icon, iconSz, fg));

            Color hover = Color.Lerp(bg, fg, 0.14f);
            b.RegisterCallback<PointerEnterEvent>(_ => { b.style.backgroundColor = hover; b.style.scale = new Scale(new Vector2(1.1f, 1.1f)); });
            b.RegisterCallback<PointerLeaveEvent>(_ => { b.style.backgroundColor = bg; b.style.scale = new Scale(Vector2.one); });
            b.RegisterCallback<PointerDownEvent>(_ => b.style.scale = new Scale(new Vector2(0.92f, 0.92f)));
            b.RegisterCallback<PointerUpEvent>(_ => b.style.scale = new Scale(new Vector2(1.1f, 1.1f)));
            return b;
        }

        // ---------------- animate ----------------
        private void Animate(float dt)
        {
            foreach (var a in _areas)
            {
                a.burstCooldown -= dt;
                if (IsLive && a.param >= 0)
                {
                    float target = Live.paramPoints[a.param];
                    float speed = Mathf.Max(2f, Mathf.Abs(target - a.value) / Mathf.Max(0.4f, Live.secondsPerWeek * 0.9f));
                    if (!Live.paused) a.value = Mathf.MoveTowards(a.value, target, speed * dt);
                    if (target < a.value) a.value = target;
                }
                else
                {
                    a.value += a.rate * dt;
                }
                int ni = Mathf.FloorToInt(a.value);
                float fill = a.value - ni;
                if (ni != a.shown)
                {
                    bool up = ni > a.shown;
                    a.shown = ni; a.countPill.text = ni.ToString();
                    if (up && a.burstCooldown <= 0f) { Burst(a); a.burstCooldown = IsLive ? 0.22f : 0f; }
                }

                a.liquid.style.height = Length.Percent(fill * 100f);

                a.phase += dt;
                a.fillNow = fill;
                if (a.aura != null) a.aura.MarkDirtyRepaint();

                a.pop = Mathf.Max(0f, a.pop - dt * 4f);
                float discS = 1f + a.pop * 0.18f;
                a.disc.transform.scale = new Vector3(discS, discS, 1f);
            }

            // transientes
            for (int i = _fx.Count - 1; i >= 0; i--)
            {
                var f = _fx[i];
                f.age += dt;
                float k = Mathf.Clamp01(f.age / f.dur);
                if (f.kind == 0) // onda
                {
                    float s = 1f + k * 1.3f;
                    f.ve.transform.scale = new Vector3(s, s, 1f);
                    f.ve.style.opacity = (1f - k) * 0.7f;
                }
                else // +1
                {
                    f.ve.transform.position = new Vector3(0, -34f * k, 0);
                    f.ve.style.opacity = 1f - k;
                }
                if (k >= 1f) { f.ve.RemoveFromHierarchy(); _fx.RemoveAt(i); }
            }

            UpdatePhase(dt);

            UpdateHype(dt);

            UpdateBugs(dt);
            UpdateResearch(dt);
            UpdateFlyingOrbs(dt);
            UpdateMoney(dt);
            UpdateSales(dt);

            // pausa pulsando
            if (IsLive) UpdateLiveChrome();
            if (_pauseBtn != null)
            {
                float p = Mathf.Sin(_t * 4f) * 0.5f + 0.5f;
                float s = IsLive && !Live.paused ? 1f : 1f + 0.08f * p;
                _pauseBtn.transform.scale = new Vector3(s, s, 1f);
            }
        }

        private void Burst(Area a)
        {
            a.pop = 1f;

            var ring = new VisualElement();
            ring.style.position = Position.Absolute;
            ring.style.left = 0; ring.style.right = 0; ring.style.top = 0; ring.style.bottom = 0;
            Radius(ring, 36); SetBorder(ring, a.color, 3); ring.style.opacity = 0.7f;
            ring.pickingMode = PickingMode.Ignore;
            a.holder.Add(ring);
            _fx.Add(new Fx { ve = ring, dur = 0.55f, kind = 0, color = a.color });

            var plus = Text("+1", 15, a.color, true);
            plus.style.position = Position.Absolute; plus.style.top = -2; plus.style.left = 0; plus.style.right = 0;
            plus.style.unityTextAlign = TextAnchor.MiddleCenter;
            plus.pickingMode = PickingMode.Ignore;
            a.holder.Add(plus);
            _fx.Add(new Fx { ve = plus, dur = 0.75f, kind = 1, color = a.color });
        }

        // ---------------- card de bugs (insetinhos andando) ----------------
        private VisualElement BuildBugCard()
        {
            var card = Card(0);
            card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center;
            card.style.marginBottom = 8; card.style.paddingTop = 7; card.style.paddingBottom = 7;
            card.style.minWidth = 150; card.style.overflow = Overflow.Hidden;
            _bugCard = card;

            _bugLayer = new VisualElement();
            _bugLayer.style.position = Position.Absolute;
            _bugLayer.style.left = 0; _bugLayer.style.right = 0; _bugLayer.style.top = 0; _bugLayer.style.bottom = 0;
            _bugLayer.pickingMode = PickingMode.Ignore;
            card.Add(_bugLayer);

            _bugBadge = new VisualElement();
            _bugBadge.style.width = 26; _bugBadge.style.height = 26; Radius(_bugBadge, 13);
            _bugBadge.style.backgroundColor = RGB(200, 90, 74);
            _bugBadge.style.alignItems = Align.Center; _bugBadge.style.justifyContent = Justify.Center;
            _bugBadgeIcon = Icon(IcBug, 14, Color.white);
            _bugBadge.Add(_bugBadgeIcon);
            card.Add(_bugBadge);

            var colv = new VisualElement(); colv.style.marginLeft = 9;
            _bugVal = Text("4", 15, Ink, true); colv.Add(_bugVal);
            _bugCap = Text("Bugs", 9, Soft, true); colv.Add(_bugCap);
            card.Add(colv);
            return card;
        }

        private void SpawnInsect()
        {
            var l = Icon(IcBug, 12, RGB(120, 28, 20));
            l.style.position = Position.Absolute;
            l.pickingMode = PickingMode.Ignore;
            _bugLayer.Add(l);
            float w = Mathf.Max(_bugLayer.resolvedStyle.width, 44f);
            float h = Mathf.Max(_bugLayer.resolvedStyle.height, 32f);
            _insects.Add(new Insect
            {
                ve = l, x = Random.Range(6f, w - 14f), y = Random.Range(3f, h - 14f),
                ang = Random.Range(0f, 360f), speed = Random.Range(9f, 20f), turn = Random.Range(0.2f, 0.8f)
            });
        }

        private void UpdateBugs(float dt)
        {
            if (IsLive)
            {
                if (_bugs != Live.bugs) { _bugs = Live.bugs; _bugVal.text = _bugs.ToString(); }
            }
            else
            {
                _bugTimer += dt;
                if (_bugTimer > 3.2f) { _bugTimer = 0f; _bugs++; _bugVal.text = _bugs.ToString(); }
            }

            float fill = Mathf.Clamp01(_bugs / (float)MaxInsects);
            _bugCard.style.backgroundColor = Color.Lerp(new Color(Paper.r, Paper.g, Paper.b, 0.98f), RGB(188, 52, 44), fill);
            _bugVal.style.color = Color.Lerp(Ink, Color.white, fill);
            _bugCap.style.color = Color.Lerp(Soft, Color.white, fill);
            _bugBadge.style.backgroundColor = Color.Lerp(RGB(200, 90, 74), Color.white, fill);
            _bugBadgeIcon.style.color = Color.Lerp(Color.white, RGB(188, 52, 44), fill);

            int want = Mathf.Clamp(_bugs, 0, MaxInsects);
            while (_insects.Count < want) SpawnInsect();
            while (_insects.Count > want)
            {
                var last = _insects[_insects.Count - 1];
                last.ve.RemoveFromHierarchy(); _insects.RemoveAt(_insects.Count - 1);
            }

            float cw = _bugLayer.resolvedStyle.width, ch = _bugLayer.resolvedStyle.height;
            if (cw < 8f || ch < 8f) return;
            // insetos = textura clara de fundo (translúcida), não competem com o número branco
            Color insCol = Color.Lerp(new Color(0.59f, 0.23f, 0.20f, 0.75f), new Color(1f, 1f, 1f, 0.45f), fill);
            foreach (var ins in _insects)
            {
                ins.turn -= dt;
                if (ins.turn <= 0f) { ins.ang += Random.Range(-70f, 70f); ins.turn = Random.Range(0.3f, 0.9f); }
                float a = ins.ang * Mathf.Deg2Rad;
                ins.x += Mathf.Cos(a) * ins.speed * dt;
                ins.y += Mathf.Sin(a) * ins.speed * dt;
                if (ins.x < 4f) { ins.x = 4f; ins.ang = 180f - ins.ang; }
                if (ins.x > cw - 14f) { ins.x = cw - 14f; ins.ang = 180f - ins.ang; }
                if (ins.y < 2f) { ins.y = 2f; ins.ang = -ins.ang; }
                if (ins.y > ch - 14f) { ins.y = ch - 14f; ins.ang = -ins.ang; }
                ins.ve.style.translate = new Translate(ins.x, ins.y, 0);
                ins.ve.style.rotate = new Rotate(new Angle(ins.ang + 90f));
                ins.ve.style.color = insCol;
            }
        }

        // ---------------- card de pesquisa (gota d'agua) ----------------
        private VisualElement BuildResearchCard()
        {
            var card = Card(0);
            card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center;
            card.style.paddingTop = 7; card.style.paddingBottom = 7;
            card.style.minWidth = 150; card.style.overflow = Overflow.Hidden;
            _resCard = card;
            card.RegisterCallback<ClickEvent>(_ => Actions.Research?.Invoke());

            _resLayer = new VisualElement();
            _resLayer.style.position = Position.Absolute;
            _resLayer.style.left = 0; _resLayer.style.right = 0; _resLayer.style.top = 0; _resLayer.style.bottom = 0;
            _resLayer.pickingMode = PickingMode.Ignore;
            _resLayer.generateVisualContent += DrawRipples;
            card.Add(_resLayer);

            _resBadge = new VisualElement();
            _resBadge.style.width = 26; _resBadge.style.height = 26; Radius(_resBadge, 13);
            _resBadge.style.backgroundColor = Teal;
            _resBadge.style.alignItems = Align.Center; _resBadge.style.justifyContent = Justify.Center;
            _resBadge.Add(Icon(IcResearch, 14, Color.white));
            card.Add(_resBadge);

            var colv = new VisualElement(); colv.style.marginLeft = 9;
            _resVal = Text("22", 15, Ink, true); colv.Add(_resVal);
            colv.Add(Text("Pesquisa", 9, Soft, true));
            card.Add(colv);
            return card;
        }

        private void UpdateResearch(float dt)
        {
            _resFlash = Mathf.Max(0f, _resFlash - dt * 2.2f);
            _resCard.style.backgroundColor = Color.Lerp(
                new Color(Paper.r, Paper.g, Paper.b, 0.98f),
                new Color(Teal.r, Teal.g, Teal.b, 0.38f), _resFlash);

            bool any = _ripples.Count > 0;
            for (int i = _ripples.Count - 1; i >= 0; i--)
            {
                _ripples[i].age += dt;
                if (_ripples[i].age >= _ripples[i].dur) _ripples.RemoveAt(i);
            }
            if (any) _resLayer.MarkDirtyRepaint();
        }

        private void ResearchHit(float localX, float localY)
        {
            if (!IsLive) { _research++; _resVal.text = _research.ToString(); }
            _resFlash = 1f;
            _ripples.Add(new Ripple { x = localX, y = localY, age = 0f, dur = 0.9f });
            _resLayer.MarkDirtyRepaint();
        }

        private void DrawRipples(MeshGenerationContext mgc)
        {
            if (_ripples.Count == 0) return;
            var p = mgc.painter2D;
            var r = _resLayer.contentRect;
            float maxR = Mathf.Max(r.width, r.height) * 0.95f;
            p.lineCap = LineCap.Round;
            foreach (var rp in _ripples)
            {
                float t = Mathf.Clamp01(rp.age / rp.dur);
                for (int k = 0; k < 3; k++)
                {
                    float kt = t - k * 0.14f;
                    if (kt <= 0f) continue;
                    float rad = maxR * kt;
                    float alpha = (1f - kt) * 0.5f;
                    p.lineWidth = 2.5f;
                    p.strokeColor = new Color(Teal.r, Teal.g, Teal.b, alpha);
                    p.BeginPath(); p.Arc(new Vector2(rp.x, rp.y), rad, new Angle(0f), new Angle(360f)); p.Stroke();
                }
            }
        }

        private void SpawnResearchOrb()
        {
            if (_resCard == null || _root == null) return;
            var wb = _resCard.worldBound;
            if (float.IsNaN(wb.x) || wb.width < 1f) return;
            Vector2 target = wb.center;
            Vector2 start = new Vector2(wb.center.x + Random.Range(-30f, 160f), wb.yMax + Random.Range(140f, 260f));
            var dot = new VisualElement();
            dot.style.position = Position.Absolute;
            dot.style.width = 12; dot.style.height = 12; Radius(dot, 6);
            dot.style.backgroundColor = Teal;
            dot.pickingMode = PickingMode.Ignore;
            dot.style.opacity = 0f;
            _root.Add(dot);
            _orbFx.Add(new Orb { ve = dot, start = start, target = target, age = 0f, dur = 0.85f });
        }

        private void UpdateFlyingOrbs(float dt)
        {
            if (IsLive)
            {
                if (_lastResearch < 0) { _lastResearch = Live.research; _resVal.text = Live.research.ToString(); }
                if (Live.research > _lastResearch)
                {
                    int orbs = Mathf.Min(4, Live.research - _lastResearch);
                    for (int k = 0; k < orbs; k++) SpawnResearchOrb();
                }
                if (Live.research != _lastResearch) { _lastResearch = Live.research; _resVal.text = Live.research.ToString(); }
            }
            else
            {
                _orbTimer += dt;
                if (_orbTimer > 2.4f) { _orbTimer = 0f; SpawnResearchOrb(); }
            }
            for (int i = _orbFx.Count - 1; i >= 0; i--)
            {
                var o = _orbFx[i]; o.age += dt;
                float t = Mathf.Clamp01(o.age / o.dur);
                float ease = 1f - (1f - t) * (1f - t);
                Vector2 pos = Vector2.Lerp(o.start, o.target, ease);
                o.ve.style.left = pos.x - 6f; o.ve.style.top = pos.y - 6f;
                o.ve.style.opacity = t < 0.12f ? t / 0.12f : 1f;
                if (t >= 1f)
                {
                    o.ve.RemoveFromHierarchy(); _orbFx.RemoveAt(i);
                    var wb = _resCard.worldBound;
                    ResearchHit(o.target.x - wb.xMin, o.target.y - wb.yMin);
                }
            }
        }

        // ---------------- hype (foguinho + número que cresce) ----------------
        private VisualElement BuildHypeFlame()
        {
            var wrap = new VisualElement();
            wrap.style.alignItems = Align.Center; wrap.style.justifyContent = Justify.FlexEnd;
            wrap.style.marginLeft = 14; wrap.style.width = 72;

            _hypeHolder = new VisualElement();
            _hypeHolder.style.width = 60; _hypeHolder.style.height = 56;
            _hypeHolder.style.alignItems = Align.Center;
            _hypeHolder.style.justifyContent = Justify.FlexEnd;
            _hypeHolder.style.overflow = Overflow.Visible;
            _hypeHolder.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100), 0);

            _hypeFlameFx = new VisualElement();
            _hypeFlameFx.style.position = Position.Absolute;
            _hypeFlameFx.style.left = 0; _hypeFlameFx.style.right = 0; _hypeFlameFx.style.top = 0; _hypeFlameFx.style.bottom = 0;
            _hypeFlameFx.style.overflow = Overflow.Visible;
            _hypeFlameFx.pickingMode = PickingMode.Ignore;
            _hypeFlameFx.generateVisualContent += DrawFlames;
            _hypeHolder.Add(_hypeFlameFx);

            _hypeIcon = Icon(IcFire, 24, RGB(228, 95, 150)); // filho normal → fica no rodapé, perto do número
            _hypeHolder.Add(_hypeIcon);
            wrap.Add(_hypeHolder);

            _hypeNum = Text("340", 16, RGB(230, 120, 170), true);
            _hypeNum.style.marginTop = 1;
            wrap.Add(_hypeNum);
            var cap = Text("HYPE", 9, Soft, true); cap.style.marginTop = -2;
            wrap.Add(cap);
            return wrap;
        }

        private void UpdateHype(float dt)
        {
            if (IsLive)
            {
                if (!_hypeSynced) { _hypeSynced = true; _hypeCount = Live.hype100; }
                _hypeCount = Mathf.MoveTowards(_hypeCount, Live.hype100, dt * 30f);
                _hypeNum.text = Mathf.RoundToInt(_hypeCount).ToString();
                _hypeMag = Mathf.Clamp01(_hypeCount / 100f);
            }
            else
            {
                _hypeCount += dt * (6f + _hypeCount * 0.03f);   // cresce acelerando (bola de neve)
                _hypeNum.text = FmtK(Mathf.RoundToInt(_hypeCount));
                _hypeMag = Mathf.Clamp01((Mathf.Log10(Mathf.Max(_hypeCount, 10f)) - 2f) / 2.3f);
            }
            _hypeNum.style.color = Color.Lerp(RGB(230, 120, 170), RGB(232, 70, 140), _hypeMag);
            if (_hypeIcon != null)
            {
                float ip = 1f + 0.06f * Mathf.Sin(_t * 5f);
                _hypeIcon.style.scale = new Scale(new Vector2(ip, ip));
            }
            UpdateFire(dt);
            _hypeFlameFx.MarkDirtyRepaint();
        }

        // Sistema de partículas de fogo (desenhado no HUD via Painter2D).
        private void UpdateFire(float dt)
        {
            if (_hypeFlameFx == null) return;
            var r = _hypeFlameFx.contentRect;
            float w = r.width, h = r.height;
            if (w < 6f || h < 6f) return;
            float mag = _hypeMag;

            // emissão proporcional ao volume (fraca no começo, vira fogueira com o hype)
            _fireEmit += dt * (24f + mag * 72f);
            int n = (int)_fireEmit; _fireEmit -= n;
            for (int k = 0; k < n && _fire.Count < 150; k++)
            {
                float ml = Random.Range(0.7f, 1.25f);
                _fire.Add(new FireP
                {
                    x = w * 0.5f + Random.Range(-w * 0.09f, w * 0.09f),
                    y = h - 6f,
                    vx = Random.Range(-10f, 10f),
                    vy = -(Random.Range(30f, 48f) + mag * 44f),
                    life = ml, maxLife = ml,
                    size = Random.Range(4.5f, 8f) + mag * 5.5f,
                    seed = Random.Range(0f, 6.283f)
                });
            }

            for (int i = _fire.Count - 1; i >= 0; i--)
            {
                var f = _fire[i];
                f.life -= dt;
                if (f.life <= 0f) { _fire.RemoveAt(i); continue; }
                f.vy -= dt * 14f;                                       // acelera subindo
                f.x += (f.vx + Mathf.Sin(_t * 3f + f.seed) * 9f) * dt;  // turbulência lateral
                f.y += f.vy * dt;
            }
        }

        private void DrawFlames(MeshGenerationContext mgc)
        {
            if (_fire.Count == 0) return;
            var p = mgc.painter2D;
            for (int i = 0; i < _fire.Count; i++)
            {
                var f = _fire[i];
                float tt = Mathf.Clamp01(1f - f.life / f.maxLife);     // 0 nova .. 1 velha
                float rad = f.size * (1f - tt * 0.65f);
                float a = (1f - tt) * 0.8f;
                Color c = tt < 0.4f
                    ? Color.Lerp(new Color(1f, 0.93f, 0.97f, a), new Color(236f / 255f, 135f / 255f, 188f / 255f, a), tt / 0.4f)
                    : new Color(230f / 255f, 120f / 255f, 170f / 255f, a * 0.8f);
                var pos = new Vector2(f.x, f.y);
                p.fillColor = new Color(c.r, c.g, c.b, c.a * 0.4f);      // halo suave
                p.BeginPath(); p.Arc(pos, rad * 1.7f, new Angle(0f), new Angle(360f)); p.Fill();
                p.fillColor = c;                                        // núcleo
                p.BeginPath(); p.Arc(pos, rad, new Angle(0f), new Angle(360f)); p.Fill();
            }
        }

        private static string FmtK(int n)
        {
            if (n < 1000) return n.ToString();
            float k = n / 1000f;
            if (n >= 10000) return Mathf.RoundToInt(k) + "K";
            string s = k.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            if (s.EndsWith(".0")) s = s.Substring(0, s.Length - 2);
            return s + "K";
        }

        // ---------------- card de dinheiro (ticks + relógio) ----------------
        private VisualElement BuildMoneyCard()
        {
            var card = Card(0);
            card.style.alignItems = Align.FlexEnd;
            _moneyCard = card;

            _companyVal = Text("Pixel Forja · Garagem", 10, Soft, true);
            card.Add(_companyVal);
            card.RegisterCallback<ClickEvent>(_ => Actions.Money?.Invoke());
            var r1 = Row(); r1.Add(Icon(IcCoins, 15, RGB(30, 120, 60)));
            _cashVal = Text("  R$ 150.984", 18, RGB(30, 120, 60), true); r1.Add(_cashVal); card.Add(r1);

            var r2 = Row();
            _clockEl = new VisualElement();
            _clockEl.style.width = 15; _clockEl.style.height = 15;
            _clockEl.pickingMode = PickingMode.Ignore;
            _clockEl.generateVisualContent += DrawClock;
            r2.Add(_clockEl);
            _dateVal = Text("  Semana 5 · Ano 1", 12, Soft, false); r2.Add(_dateVal);
            var r3 = Row(); r3.Add(Icon("\uf201", 11, Teal));
            _valuationVal = Text("  Valuation R$ 412K", 12, Teal, true); r3.Add(_valuationVal);
            card.Add(r3);
            card.Add(r2);
            return card;
        }

        private void DrawClock(MeshGenerationContext mgc)
        {
            var r = _clockEl.contentRect;
            if (r.width < 4f) return;
            var p = mgc.painter2D;
            Vector2 c = r.center; float rad = Mathf.Min(r.width, r.height) * 0.5f - 1f;
            p.lineCap = LineCap.Round;
            p.lineWidth = 1.5f; p.strokeColor = new Color(Soft.r, Soft.g, Soft.b, 0.8f);
            p.BeginPath(); p.Arc(c, rad, new Angle(0f), new Angle(360f)); p.Stroke();
            float ang = (_clockAngle - 90f) * Mathf.Deg2Rad;         // ponteiro dos minutos
            Vector2 tip = new Vector2(c.x + Mathf.Cos(ang) * rad * 0.82f, c.y + Mathf.Sin(ang) * rad * 0.82f);
            p.lineWidth = 1.8f; p.strokeColor = Teal;
            p.BeginPath(); p.MoveTo(c); p.LineTo(tip); p.Stroke();
            float ang2 = (_clockAngle * 0.5f - 90f) * Mathf.Deg2Rad; // ponteiro das horas
            Vector2 tip2 = new Vector2(c.x + Mathf.Cos(ang2) * rad * 0.5f, c.y + Mathf.Sin(ang2) * rad * 0.5f);
            p.strokeColor = new Color(Teal.r, Teal.g, Teal.b, 0.6f);
            p.BeginPath(); p.MoveTo(c); p.LineTo(tip2); p.Stroke();
        }

        private void UpdateMoney(float dt)
        {
            if (IsLive) { UpdateMoneyLive(dt); return; }
            _clockAngle += dt * (360f / 6f);   // 6s = uma semana
            if (_clockAngle >= 360f)
            {
                _clockAngle -= 360f;
                _week++; if (_week > 52) { _week = 1; _year++; }
                _dateVal.text = "  Semana " + _week + " · Ano " + _year;
                SpawnMoneyTick(Random.Range(800f, 2200f));   // receita no fim da semana
            }
            _clockEl.MarkDirtyRepaint();

            _moneyTickTimer += dt;
            if (_moneyTickTimer > 0.9f)
            {
                _moneyTickTimer = 0f;
                float v = Random.value < 0.72f ? Random.Range(60f, 480f) : -Random.Range(40f, 260f);
                SpawnMoneyTick(v);
            }

            for (int i = _moneyTicks.Count - 1; i >= 0; i--)
            {
                var m = _moneyTicks[i]; m.age += dt;
                float t = Mathf.Clamp01(m.age / m.dur);
                m.ve.style.top = m.y - 34f * t;
                m.ve.style.opacity = t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f;
                if (t >= 1f) { m.ve.RemoveFromHierarchy(); _moneyTicks.RemoveAt(i); }
            }
        }

        private void SpawnMoneyTick(float v)
        {
            if (_moneyCard == null || _root == null) return;
            var wb = _moneyCard.worldBound;
            if (float.IsNaN(wb.x) || wb.width < 1f) return;

            if (!IsLive) { _cash += v; _cashVal.text = "  R$ " + FmtMoney(Mathf.RoundToInt(_cash)); }
            bool pos = v >= 0f;
            var lbl = Text((pos ? "+R$ " : "−R$ ") + FmtMoney(Mathf.RoundToInt(Mathf.Abs(v))),
                12, pos ? RGB(30, 150, 70) : RGB(200, 70, 60), true);
            lbl.style.position = Position.Absolute;
            lbl.pickingMode = PickingMode.Ignore;
            lbl.style.left = wb.xMax - 96f + Random.Range(-4f, 4f);
            float y0 = wb.yMax + 1f;
            lbl.style.top = y0;
            _root.Add(lbl);
            _moneyTicks.Add(new MoneyTick { ve = lbl, age = 0f, dur = 1.4f, y = y0 });
        }

        private static string FmtMoney(int n)
        {
            string s = Mathf.Abs(n).ToString();
            var sb = new System.Text.StringBuilder();
            int c = 0;
            for (int i = s.Length - 1; i >= 0; i--)
            {
                sb.Insert(0, s[i]);
                if (++c % 3 == 0 && i > 0) sb.Insert(0, '.');
            }
            return (n < 0 ? "-" : "") + sb.ToString();
        }

        // ---------------- auras por area (Painter2D) ----------------
        private static Vector2 OnC(Vector2 c, float r, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector2(c.x + r * Mathf.Cos(a), c.y + r * Mathf.Sin(a));
        }

        private static void Dot(Painter2D p, Vector2 pos, float r, Color c)
        {
            p.fillColor = c; p.BeginPath(); p.Arc(pos, r, new Angle(0f), new Angle(360f)); p.Fill();
        }

        private static void DrawAura(MeshGenerationContext mgc, Area area)
        {
            var rect = area.aura.contentRect;
            if (rect.width < 6f || rect.height < 6f) return;
            var p = mgc.painter2D;
            Vector2 c = rect.center;
            float R = Mathf.Min(rect.width, rect.height) * 0.5f;
            float ph = area.phase;
            float fill = area.fillNow;
            float energy = 0.6f + 0.4f * fill + area.pop;   // mais vivo quanto mais cheio
            Color col = area.color;
            p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;

            Color A(float alpha) => new Color(col.r, col.g, col.b, Mathf.Clamp01(alpha * energy));

            switch (area.kind)
            {
                case 0: // DESIGN — mira de alvo fechando
                {
                    p.lineWidth = 2f; p.strokeColor = A(0.22f);
                    p.BeginPath(); p.Arc(c, R * 0.90f, new Angle(0f), new Angle(360f)); p.Stroke();
                    for (int k = 0; k < 4; k++)
                    {
                        float ang = k * 90f;
                        Vector2 a0 = OnC(c, R * 0.90f, ang);
                        Vector2 a1 = OnC(c, R * 1.04f, ang);
                        p.lineWidth = 2.5f; p.strokeColor = A(0.5f);
                        p.BeginPath(); p.MoveTo(a0); p.LineTo(a1); p.Stroke();
                    }
                    float t = (ph * 0.85f) % 1f;              // anel que fecha e some
                    float rr = R * (1.12f - 0.55f * t);
                    p.lineWidth = 2.5f; p.strokeColor = A(0.8f * (1f - t));
                    p.BeginPath(); p.Arc(c, rr, new Angle(0f), new Angle(360f)); p.Stroke();
                    break;
                }
                case 1: // DIREÇÃO DE ARTE — pinceladas (splash) passando
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float a = ph * 1.4f + k * 2.1f;
                        float ang = (a * 55f) % 360f;
                        p.lineWidth = 5f; p.strokeColor = A(0.45f * (0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(a))));
                        p.BeginPath(); p.Arc(c, R * (0.88f + 0.07f * k), new Angle(ang), new Angle(ang + 42f)); p.Stroke();
                    }
                    Dot(p, OnC(c, R * 1.02f, ph * 70f), R * 0.06f, A(0.45f));
                    Dot(p, OnC(c, R * 0.96f, ph * 70f + 150f), R * 0.045f, A(0.4f));
                    break;
                }
                case 2: // TRILHA — equalizador radial (barrinhas)
                {
                    int m = 30; float baseR = R * 0.78f;
                    p.lineWidth = 2.5f;
                    for (int i = 0; i < m; i++)
                    {
                        float ang = i * (360f / m);
                        float h = R * 0.26f * (0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin(ph * 6f + i * 0.7f)));
                        p.strokeColor = A(0.55f);
                        p.BeginPath(); p.MoveTo(OnC(c, baseR, ang)); p.LineTo(OnC(c, baseR + h, ang)); p.Stroke();
                    }
                    break;
                }
                case 5: // NARRATIVA — letrinhas flutuando
                {
                    p.lineWidth = 2f;
                    for (int k = 0; k < 5; k++)
                    {
                        float ang = k * 72f + ph * 22f;
                        float rr = R * (0.85f + 0.14f * Mathf.Sin(ph * 2f + k));
                        Vector2 pos = OnC(c, rr, ang);
                        p.strokeColor = A(0.5f * (0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(ph * 3f + k))));
                        float s = R * 0.13f;
                        p.BeginPath();
                        p.MoveTo(new Vector2(pos.x - s * 0.5f, pos.y + s * 0.6f));
                        p.LineTo(new Vector2(pos.x, pos.y - s * 0.6f));
                        p.LineTo(new Vector2(pos.x + s * 0.5f, pos.y + s * 0.6f)); p.Stroke();
                        p.BeginPath(); p.MoveTo(new Vector2(pos.x - s * 0.25f, pos.y + s * 0.1f));
                        p.LineTo(new Vector2(pos.x + s * 0.25f, pos.y + s * 0.1f)); p.Stroke();
                    }
                    break;
                }
                case 6: // GAME FEEL / GAMEPLAY — controlezinhos girando
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float ang = ph * 50f + k * 120f;
                        Vector2 pos = OnC(c, R * 0.95f, ang);
                        float w = R * 0.30f, h = R * 0.17f;
                        p.lineWidth = 2f; p.strokeColor = A(0.55f);
                        p.BeginPath();
                        p.Arc(new Vector2(pos.x - w * 0.5f, pos.y), h, new Angle(90f), new Angle(270f));
                        p.LineTo(new Vector2(pos.x + w * 0.5f, pos.y - h));
                        p.Arc(new Vector2(pos.x + w * 0.5f, pos.y), h, new Angle(270f), new Angle(90f));
                        p.LineTo(new Vector2(pos.x - w * 0.5f, pos.y + h));
                        p.ClosePath(); p.Stroke();
                        Dot(p, new Vector2(pos.x + w * 0.35f, pos.y - h * 0.2f), R * 0.03f, A(0.6f));
                        Dot(p, new Vector2(pos.x - w * 0.35f, pos.y), R * 0.03f, A(0.6f));
                    }
                    break;
                }
                case 7: // ESCOPO — quadrados que expandem
                {
                    for (int j = 0; j < 2; j++)
                    {
                        float t = ((ph * 0.5f) + j * 0.5f) % 1f;
                        float d = R * (0.6f + 0.6f * t) * 1.414f;
                        float rot = ph * 14f;
                        p.lineWidth = 2f; p.strokeColor = A(0.5f * (1f - t));
                        Vector2 q0 = OnC(c, d, rot + 45f), q1 = OnC(c, d, rot + 135f), q2 = OnC(c, d, rot + 225f), q3 = OnC(c, d, rot + 315f);
                        p.BeginPath(); p.MoveTo(q0); p.LineTo(q1); p.LineTo(q2); p.LineTo(q3); p.ClosePath(); p.Stroke();
                    }
                    break;
                }
                case 8: // GRÁFICOS — quadradinhos de pixel cintilando
                {
                    for (int k = 0; k < 12; k++)
                    {
                        float ang = k * 30f;
                        Vector2 pos = OnC(c, R * (0.85f + 0.08f * Mathf.Sin(k * 1.3f)), ang);
                        float tw = 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin(ph * 4f + k * 1.3f));
                        float s = R * 0.07f;
                        p.fillColor = A(0.5f * tw);
                        p.BeginPath();
                        p.MoveTo(new Vector2(pos.x - s, pos.y - s)); p.LineTo(new Vector2(pos.x + s, pos.y - s));
                        p.LineTo(new Vector2(pos.x + s, pos.y + s)); p.LineTo(new Vector2(pos.x - s, pos.y + s));
                        p.ClosePath(); p.Fill();
                    }
                    break;
                }
                case 9: // ANIMAÇÃO — rolo de filme correndo
                {
                    p.lineWidth = 2f; p.strokeColor = A(0.28f);
                    p.BeginPath(); p.Arc(c, R * 0.98f, new Angle(0f), new Angle(360f)); p.Stroke();
                    p.BeginPath(); p.Arc(c, R * 0.80f, new Angle(0f), new Angle(360f)); p.Stroke();
                    for (int k = 0; k < 10; k++)
                    {
                        float ang = k * 36f + ph * 42f;
                        Vector2 pos = OnC(c, R * 0.89f, ang);
                        float s = R * 0.045f;
                        p.fillColor = A(0.5f);
                        p.BeginPath();
                        p.MoveTo(new Vector2(pos.x - s, pos.y - s)); p.LineTo(new Vector2(pos.x + s, pos.y - s));
                        p.LineTo(new Vector2(pos.x + s, pos.y + s)); p.LineTo(new Vector2(pos.x - s, pos.y + s));
                        p.ClosePath(); p.Fill();
                    }
                    break;
                }
                case 10: // VFX — estrelinhas brilhando
                {
                    for (int k = 0; k < 4; k++)
                    {
                        float ang = ph * 28f + k * 90f;
                        Vector2 pos = OnC(c, R * 0.95f, ang);
                        float tw = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(ph * 5f + k * 1.7f));
                        float s = R * 0.17f * tw;
                        p.lineWidth = 2f; p.strokeColor = A(0.75f * tw);
                        p.BeginPath(); p.MoveTo(new Vector2(pos.x, pos.y - s)); p.LineTo(new Vector2(pos.x, pos.y + s)); p.Stroke();
                        p.BeginPath(); p.MoveTo(new Vector2(pos.x - s, pos.y)); p.LineTo(new Vector2(pos.x + s, pos.y)); p.Stroke();
                        Dot(p, pos, R * 0.025f, A(0.9f * tw));
                    }
                    break;
                }
                case 11: // SOUND DESIGN — linha de onda contínua (sobe/desce)
                {
                    p.lineWidth = 2.5f; p.strokeColor = A(0.55f);
                    int n = 48;
                    p.BeginPath();
                    for (int i = 0; i <= n; i++)
                    {
                        float ang = i * (360f / n);
                        float wob = R * 0.13f * Mathf.Sin(ang * Mathf.Deg2Rad * 6f + ph * 5f);
                        Vector2 pt = OnC(c, R * 0.88f + wob, ang);
                        if (i == 0) p.MoveTo(pt); else p.LineTo(pt);
                    }
                    p.Stroke();
                    break;
                }
                case 12: // DUBLAGEM — balõezinhos de fala
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float t = ((ph * 0.6f) + k * 0.33f) % 1f;
                        float ang = k * 120f + ph * 20f;
                        Vector2 pos = OnC(c, R * (0.8f + 0.22f * t), ang);
                        float sc = t < 0.2f ? t / 0.2f : 1f - (t - 0.2f) / 0.8f;
                        float r2 = R * 0.17f * sc;
                        if (r2 < 0.5f) continue;
                        p.lineWidth = 2f; p.strokeColor = A(0.6f * sc);
                        p.BeginPath(); p.Arc(pos, r2, new Angle(0f), new Angle(360f)); p.Stroke();
                        p.BeginPath();
                        p.MoveTo(new Vector2(pos.x - r2 * 0.3f, pos.y + r2 * 0.8f));
                        p.LineTo(new Vector2(pos.x - r2 * 0.6f, pos.y + r2 * 1.5f));
                        p.LineTo(new Vector2(pos.x + r2 * 0.1f, pos.y + r2 * 0.9f)); p.Stroke();
                    }
                    break;
                }
                case 13: // PROGRAMAÇÃO — código binário 0/1 orbitando
                {
                    for (int k = 0; k < 10; k++)
                    {
                        float ang = k * 36f + ph * 32f;
                        Vector2 pos = OnC(c, R * 0.92f, ang);
                        float a = 0.45f * (0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(ph * 3f + k)));
                        p.lineWidth = 2f; p.strokeColor = A(a); float s = R * 0.1f;
                        if (k % 2 == 0)
                        { p.BeginPath(); p.MoveTo(new Vector2(pos.x, pos.y - s)); p.LineTo(new Vector2(pos.x, pos.y + s)); p.Stroke(); }
                        else
                        { p.BeginPath(); p.Arc(pos, s * 0.6f, new Angle(0f), new Angle(360f)); p.Stroke(); }
                    }
                    break;
                }
                case 14: // PERFORMANCE — raiozinhos passando
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float flash = Mathf.Sin(ph * 6f + k * 2.1f);
                        if (flash < 0.3f) continue;
                        float ang = (k * 120f + ph * 80f) % 360f;
                        Vector2 a0 = OnC(c, R * 0.7f, ang);
                        Vector2 a1 = OnC(c, R * 1.06f, ang);
                        Vector2 d = (a1 - a0); Vector2 perp = new Vector2(-d.y, d.x).normalized;
                        Vector2 m = Vector2.Lerp(a0, a1, 0.5f) + perp * (R * 0.1f);
                        p.lineWidth = 2.5f; p.strokeColor = A(0.85f * flash);
                        p.BeginPath(); p.MoveTo(a0); p.LineTo(m); p.LineTo(a1); p.Stroke();
                    }
                    break;
                }
                case 15: // CONTEÚDO — cubinhos iso orbitando
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float ang = ph * 40f + k * 120f;
                        Vector2 pos = OnC(c, R * 0.92f, ang);
                        float s = R * 0.14f;
                        p.lineWidth = 2f; p.strokeColor = A(0.55f);
                        Vector2 top = new Vector2(pos.x, pos.y - s);
                        Vector2 rr = new Vector2(pos.x + s, pos.y - s * 0.4f);
                        Vector2 bm = new Vector2(pos.x, pos.y + s * 0.2f);
                        Vector2 ll = new Vector2(pos.x - s, pos.y - s * 0.4f);
                        p.BeginPath(); p.MoveTo(top); p.LineTo(rr); p.LineTo(bm); p.LineTo(ll); p.ClosePath(); p.Stroke();
                        Vector2 bb = new Vector2(pos.x, pos.y + s);
                        Vector2 rb = new Vector2(pos.x + s, pos.y + s * 0.4f);
                        Vector2 lb = new Vector2(pos.x - s, pos.y + s * 0.4f);
                        p.BeginPath(); p.MoveTo(bm); p.LineTo(bb); p.Stroke();
                        p.BeginPath(); p.MoveTo(rr); p.LineTo(rb); p.LineTo(bb); p.Stroke();
                        p.BeginPath(); p.MoveTo(ll); p.LineTo(lb); p.LineTo(bb); p.Stroke();
                    }
                    break;
                }
                case 16: // NARRATIVA IMPL. — livrinhos abrindo/fechando
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float ang = ph * 24f + k * 120f;
                        Vector2 pos = OnC(c, R * 0.9f, ang);
                        float open = 0.3f + 0.4f * (0.5f + 0.5f * Mathf.Sin(ph * 2f + k));
                        float s = R * 0.2f;
                        p.lineWidth = 2f; p.strokeColor = A(0.55f);
                        Vector2 sTop = new Vector2(pos.x, pos.y - s * 0.2f);
                        Vector2 sBot = new Vector2(pos.x, pos.y + s * 0.2f);
                        Vector2 lT = new Vector2(pos.x - s * open, pos.y - s * 0.45f);
                        Vector2 lB = new Vector2(pos.x - s * open, pos.y + s * 0.1f);
                        Vector2 rT = new Vector2(pos.x + s * open, pos.y - s * 0.45f);
                        Vector2 rB = new Vector2(pos.x + s * open, pos.y + s * 0.1f);
                        p.BeginPath(); p.MoveTo(lB); p.LineTo(lT); p.LineTo(sTop); p.LineTo(sBot); p.ClosePath(); p.Stroke();
                        p.BeginPath(); p.MoveTo(rB); p.LineTo(rT); p.LineTo(sTop); p.LineTo(sBot); p.ClosePath(); p.Stroke();
                    }
                    break;
                }
                case 17: // BALANCEAMENTO — gangorra que vai e vem buscando o centro
                {
                    float tilt = Mathf.Sin(ph * 2f) * 20f * (0.5f + 0.5f * Mathf.Cos(ph * 0.6f));
                    float len = R * 0.98f;
                    Vector2 e1 = OnC(c, len, tilt);
                    Vector2 e2 = OnC(c, len, tilt + 180f);
                    p.lineWidth = 2.5f; p.strokeColor = A(0.6f);
                    p.BeginPath(); p.MoveTo(e2); p.LineTo(e1); p.Stroke();
                    Dot(p, e1, R * 0.08f, A(0.6f));
                    Dot(p, e2, R * 0.08f, A(0.6f));
                    Dot(p, c, R * 0.05f, A(0.75f));
                    break;
                }
                default: // CONTEÚDO/fallback — camadas se expandindo
                {
                    for (int j = 0; j < 3; j++)
                    {
                        float t = ((ph * 0.55f) + j / 3f) % 1f;
                        float rr = R * (0.58f + 0.52f * t);
                        p.lineWidth = 2.5f; p.strokeColor = A(0.55f * (1f - t));
                        p.BeginPath(); p.Arc(c, rr, new Angle(0f), new Angle(360f)); p.Stroke();
                    }
                    break;
                }
            }
        }

        // ---------------- helpers ----------------
        private VisualElement Card(float width)
        {
            var c = new VisualElement();
            if (width > 0) c.style.width = width;
            c.style.backgroundColor = new Color(Paper.r, Paper.g, Paper.b, 0.98f);
            Radius(c, 14);
            c.style.borderBottomWidth = 3; SetBorderColor(c, new Color(0, 0, 0, 0.12f));
            c.style.paddingTop = 10; c.style.paddingBottom = 10; c.style.paddingLeft = 14; c.style.paddingRight = 14;
            return c;
        }

        private Label Text(string s, int size, Color c, bool bold)
        {
            var l = new Label(s);
            l.style.fontSize = size; l.style.color = c;
            l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            l.style.marginTop = 0; l.style.marginBottom = 0; l.style.marginLeft = 0; l.style.marginRight = 0;
            l.style.paddingTop = 0; l.style.paddingBottom = 0; l.style.paddingLeft = 0; l.style.paddingRight = 0;
            return l;
        }

        private Label Icon(string code, int size, Color c)
        {
            var l = new Label(code);
            l.style.fontSize = size; l.style.color = c;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.marginTop = 0; l.style.marginBottom = 0; l.style.marginLeft = 0; l.style.marginRight = 0;
            l.style.paddingTop = 0; l.style.paddingBottom = 0; l.style.paddingLeft = 0; l.style.paddingRight = 0;
            if (_iconFont != null)
                l.style.unityFontDefinition = new StyleFontDefinition(_iconFont);
            return l;
        }

        private VisualElement Row()
        {
            var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center;
            return r;
        }

        private VisualElement Spacer() { var s = new VisualElement(); s.style.flexGrow = 1; s.style.minWidth = 16; return s; }

        private Label Seg(Color c, int grow, string label)
        {
            var l = new Label(label);
            l.style.flexGrow = grow; l.style.backgroundColor = c; l.style.color = Color.white;
            l.style.fontSize = 10; l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            return l;
        }

        private static void Radius(VisualElement v, float r)
        {
            v.style.borderTopLeftRadius = r; v.style.borderTopRightRadius = r;
            v.style.borderBottomLeftRadius = r; v.style.borderBottomRightRadius = r;
        }

        private static void SetBorderColor(VisualElement v, Color c)
        {
            v.style.borderTopColor = c; v.style.borderRightColor = c;
            v.style.borderBottomColor = c; v.style.borderLeftColor = c;
        }

        private static void SetBorder(VisualElement v, Color c, float w)
        {
            v.style.borderTopWidth = w; v.style.borderRightWidth = w;
            v.style.borderBottomWidth = w; v.style.borderLeftWidth = w;
            SetBorderColor(v, c);
        }
    }
}
