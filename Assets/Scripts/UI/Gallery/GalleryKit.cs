using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>
    /// Kit visual compartilhado pelas telas da galeria: tokens de cor aprovados no HUD animado
    /// (card creme, borda inferior, teal/azul), ícones Font Awesome e construtores de componentes
    /// (card, botão, chip, barra, avatar). Mantém todas as telas com a mesma linguagem.
    /// </summary>
    internal static class Kit
    {
        public static Color RGB(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
        public static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static readonly Color Ink = RGB(36, 31, 23);
        public static readonly Color Soft = RGB(110, 100, 84);
        public static readonly Color Faint = RGB(168, 158, 140);
        public static readonly Color Paper = RGB(251, 248, 241);
        public static readonly Color Sand = RGB(241, 236, 224);
        public static readonly Color Track = RGB(232, 225, 210);
        public static readonly Color Teal = RGB(42, 157, 143);
        public static readonly Color Blue = RGB(47, 116, 201);
        public static readonly Color Red = RGB(200, 90, 74);
        public static readonly Color Green = RGB(76, 158, 86);
        public static readonly Color Cash = RGB(30, 120, 60);
        public static readonly Color Pink = RGB(214, 86, 143);
        public static readonly Color Purple = RGB(139, 109, 214);
        public static readonly Color Sky = RGB(63, 134, 216);
        public static readonly Color Orange = RGB(224, 145, 74);
        public static readonly Color Gold = RGB(232, 170, 48);
        public static readonly Color Screen = RGB(19, 48, 41);

        /// <summary>Cores das 5 fases de produção (GDD 5.2), iguais às do HUD animado.</summary>
        public static readonly Color[] PhaseColors = { Teal, Pink, Purple, Sky, Green };

        // --- Font Awesome 6 Solid ---
        public const string IcUsers = "", IcUser = "", IcUserPlus = "", IcUserTie = "",
            IcPlus = "", IcMinus = "", IcPause = "", IcPlay = "", IcForward = "",
            IcBug = "", IcFire = "", IcCoins = "", IcClock = "", IcFlask = "",
            IcGear = "", IcPower = "", IcFolder = "", IcClose = "", IcCheck = "",
            IcStar = "", IcTrophy = "", IcChartLine = "", IcChartBar = "",
            IcGamepad = "", IcBriefcase = "", IcHandshake = "", IcBullhorn = "",
            IcBuilding = "", IcBolt = "", IcCode = "", IcBrush = "", IcMusic = "",
            IcVolume = "", IcMic = "", IcImage = "", IcFilm = "", IcMagic = "",
            IcBook = "", IcBookOpen = "", IcScale = "", IcCube = "", IcBulb = "",
            IcSliders = "", IcExpand = "", IcPointer = "", IcLock = "",
            IcSave = "", IcTrash = "", IcPencil = "", IcEye = "", IcFlag = "",
            IcRocket = "", IcGlobe = "", IcDesktop = "", IcMobile = "",
            IcHouse = "", IcDollar = "", IcArrowUp = "", IcArrowDown = "",
            IcArrowRight = "", IcArrowLeft = "", IcChevRight = "", IcChevLeft = "",
            IcTag = "", IcCalendar = "", IcBell = "", IcWarn = "", IcInfo = "",
            IcQuestion = "", IcHeart = "", IcPulse = "", IcWrench = "",
            IcChip = "", IcServer = "", IcCloud = "", IcRobot = "", IcLaptop = "",
            IcComments = "", IcNews = "", IcVideo = "", IcCrown = "", IcGem = "",
            IcGift = "", IcMedal = "", IcShield = "", IcBullseye = "", IcSkull = "",
            IcGhost = "", IcDragon = "", IcFutbol = "", IcGradCap = "",
            IcContract = "", IcSignature = "", IcTasks = "", IcSearch = "",
            IcUndo = "", IcHistory = "", IcHourglass = "", IcLayers = "",
            IcCubes = "", IcPie = "", IcSignal = "", IcPiggy = "", IcDice = "",
            IcLink = "", IcStop = "", IcBrain = "", IcAtom = "", IcLanguage = "",
            IcAccess = "", IcTv = "", IcHeadphones = "", IcKeyboard = "",
            IcSmile = "", IcFrown = "", IcMeh = "", IcThumbUp = "", IcSecret = "",
            IcStore = "", IcPlane = "", IcMug = "", IcBed = "", IcVr = "",
            IcCar = "", IcMoney = "", IcCircle = "", IcShuttle = "", IcHat = "",
            IcNetwork = "", IcBinoculars = "", IcDumbbell = "", IcPaperPlane = "";

        private static FontAsset _iconFont;
        public static FontAsset IconFont => _iconFont != null ? _iconFont : (_iconFont = Resources.Load<FontAsset>("IconFont"));

        // ---------------- texto / ícone ----------------
        public static Label Text(string s, int size, Color c, bool bold = false)
        {
            var l = new Label(s);
            l.style.fontSize = size; l.style.color = c;
            l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            Flat(l);
            return l;
        }

        public static Label Wrap(string s, int size, Color c, bool bold = false)
        {
            var l = Text(s, size, c, bold);
            l.style.whiteSpace = WhiteSpace.Normal;
            return l;
        }

        public static Label Caps(string s, int size, Color c, float spacing = 1.5f)
        {
            var l = Text(s.ToUpperInvariant(), size, c, true);
            l.style.letterSpacing = spacing;
            return l;
        }

        public static Label Icon(string code, int size, Color c)
        {
            var l = new Label(code);
            l.style.fontSize = size; l.style.color = c;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            Flat(l);
            if (IconFont != null) l.style.unityFontDefinition = new StyleFontDefinition(IconFont);
            return l;
        }

        private static void Flat(VisualElement l)
        {
            l.style.marginTop = 0; l.style.marginBottom = 0; l.style.marginLeft = 0; l.style.marginRight = 0;
            l.style.paddingTop = 0; l.style.paddingBottom = 0; l.style.paddingLeft = 0; l.style.paddingRight = 0;
        }

        // ---------------- layout ----------------
        public static VisualElement Row(Align align = Align.Center)
        {
            var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = align;
            return r;
        }

        public static VisualElement Col() => new VisualElement();

        public static VisualElement Spacer(float min = 0)
        {
            var s = new VisualElement(); s.style.flexGrow = 1; if (min > 0) s.style.minWidth = min; return s;
        }

        public static VisualElement Gap(float w, float h = 0)
        {
            var s = new VisualElement(); s.style.width = w; s.style.height = h; s.style.flexShrink = 0; return s;
        }

        public static VisualElement Fill()
        {
            var e = new VisualElement();
            e.style.position = Position.Absolute;
            e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
            return e;
        }

        public static VisualElement Centered()
        {
            var e = Fill();
            e.style.alignItems = Align.Center; e.style.justifyContent = Justify.Center;
            return e;
        }

        public static T Pad<T>(T v, float vertical, float horizontal) where T : VisualElement
        {
            v.style.paddingTop = vertical; v.style.paddingBottom = vertical;
            v.style.paddingLeft = horizontal; v.style.paddingRight = horizontal;
            return v;
        }

        public static T Margin<T>(T v, float top, float right, float bottom, float left) where T : VisualElement
        {
            v.style.marginTop = top; v.style.marginRight = right; v.style.marginBottom = bottom; v.style.marginLeft = left;
            return v;
        }

        public static void Radius(VisualElement v, float r)
        {
            v.style.borderTopLeftRadius = r; v.style.borderTopRightRadius = r;
            v.style.borderBottomLeftRadius = r; v.style.borderBottomRightRadius = r;
        }

        public static void BorderColor(VisualElement v, Color c)
        {
            v.style.borderTopColor = c; v.style.borderRightColor = c;
            v.style.borderBottomColor = c; v.style.borderLeftColor = c;
        }

        public static void Border(VisualElement v, Color c, float w)
        {
            v.style.borderTopWidth = w; v.style.borderRightWidth = w;
            v.style.borderBottomWidth = w; v.style.borderLeftWidth = w;
            BorderColor(v, c);
        }

        /// <summary>Transições suaves (hover/seleção) sem precisar animar por frame.</summary>
        public static void Smooth(VisualElement v, float seconds = 0.14f)
        {
            v.style.transitionProperty = new List<StylePropertyName>
            {
                new StylePropertyName("scale"), new StylePropertyName("background-color"),
                new StylePropertyName("border-color"), new StylePropertyName("opacity"),
                new StylePropertyName("translate")
            };
            v.style.transitionDuration = new List<TimeValue> { new TimeValue(seconds, TimeUnit.Second) };
            v.style.transitionTimingFunction = new List<EasingFunction> { new EasingFunction(EasingMode.EaseOut) };
        }

        public static void Hover(VisualElement v, float up = 1.04f, float down = 0.96f)
        {
            Smooth(v);
            v.RegisterCallback<PointerEnterEvent>(_ => { v.style.scale = new Scale(new Vector2(up, up)); UiSound?.Invoke(0); });
            v.RegisterCallback<PointerLeaveEvent>(_ => v.style.scale = new Scale(Vector2.one));
            v.RegisterCallback<PointerDownEvent>(_ => { v.style.scale = new Scale(new Vector2(down, down)); UiSound?.Invoke(1); });
            v.RegisterCallback<PointerUpEvent>(_ => v.style.scale = new Scale(new Vector2(up, up)));
        }

        /// <summary>Gancho de som da interface: 0 = passar o mouse, 1 = pressionar. Ligado pelo jogo real.</summary>
        public static Action<int> UiSound;

        /// <summary>Toca o som de "pressionar" (para controles que não usam <see cref="Hover"/>).</summary>
        public static void Press() => UiSound?.Invoke(1);

        // ---------------- componentes ----------------
        public static VisualElement Card(float width = 0, float radius = 14)
        {
            var c = new VisualElement();
            if (width > 0) c.style.width = width;
            c.style.backgroundColor = A(Paper, 0.985f);
            Radius(c, radius);
            c.style.borderBottomWidth = 3; BorderColor(c, new Color(0, 0, 0, 0.12f));
            Pad(c, 12, 16);
            return c;
        }

        /// <summary>Bloco interno (dentro de uma janela): fundo areia, sem sombra.</summary>
        public static VisualElement Tile(float radius = 12)
        {
            var c = new VisualElement();
            c.style.backgroundColor = Sand;
            Radius(c, radius);
            Pad(c, 12, 14);
            return c;
        }

        public static VisualElement Outline(float radius = 12)
        {
            var c = new VisualElement();
            c.style.backgroundColor = Color.white;
            Radius(c, radius);
            Border(c, Track, 2);
            Pad(c, 10, 12);
            return c;
        }

        public static VisualElement Badge(string icon, Color color, int size = 30, Color? iconColor = null)
        {
            var b = new VisualElement();
            b.style.width = size; b.style.height = size; b.style.flexShrink = 0; Radius(b, size / 2f);
            b.style.backgroundColor = color;
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            b.Add(Icon(icon, Mathf.RoundToInt(size * 0.48f), iconColor ?? Color.white));
            return b;
        }

        public static VisualElement SquareBadge(string icon, Color color, int size = 44, float radius = 12)
        {
            var b = Badge(icon, color, size);
            Radius(b, radius);
            return b;
        }

        public static VisualElement Chip(string text, Color color, bool filled = false, string icon = null)
        {
            var c = Row();
            c.style.backgroundColor = filled ? color : A(color, 0.14f);
            Radius(c, 10); Pad(c, 3, 9);
            c.style.flexShrink = 0;
            Color fg = filled ? Color.white : Color.Lerp(color, Ink, 0.25f);
            if (icon != null) { var i = Icon(icon, 10, fg); i.style.marginRight = 5; c.Add(i); }
            c.Add(Text(text, 11, fg, true));
            return c;
        }

        public static VisualElement Button(string label, string icon, Color bg, Color fg, Action onClick = null, int height = 42)
        {
            var b = Row();
            b.style.height = height; b.style.justifyContent = Justify.Center; b.style.flexShrink = 0;
            b.style.paddingLeft = 18; b.style.paddingRight = 18;
            b.style.backgroundColor = bg; Radius(b, height * 0.28f);
            b.style.borderBottomWidth = 3; BorderColor(b, new Color(0, 0, 0, 0.18f));
            if (icon != null)
            {
                var i = Icon(icon, Mathf.RoundToInt(height * 0.36f), fg);
                if (!string.IsNullOrEmpty(label)) i.style.marginRight = 8;
                b.Add(i);
            }
            if (!string.IsNullOrEmpty(label)) b.Add(Text(label, Mathf.RoundToInt(height * 0.36f), fg, true));
            Hover(b);
            if (onClick != null) b.RegisterCallback<ClickEvent>(_ => onClick());
            return b;
        }

        public static VisualElement Primary(string label, string icon = null, Action onClick = null) => Button(label, icon, Blue, Color.white, onClick);
        public static VisualElement Confirm(string label, string icon = null, Action onClick = null) => Button(label, icon, Teal, Color.white, onClick);
        public static VisualElement Danger(string label, string icon = null, Action onClick = null) => Button(label, icon, Red, Color.white, onClick);
        public static VisualElement Ghost(string label, string icon = null, Action onClick = null) => Button(label, icon, Sand, Ink, onClick);

        public static VisualElement RoundButton(string icon, Color bg, Color fg, int size = 38, Action onClick = null)
        {
            var b = Badge(icon, bg, size, fg);
            Hover(b, 1.1f, 0.92f);
            if (onClick != null) b.RegisterCallback<ClickEvent>(_ => onClick());
            return b;
        }

        /// <summary>Barra de progresso; devolve o trilho e entrega o preenchimento em <paramref name="fill"/>.</summary>
        public static VisualElement Bar(float height, Color color, out VisualElement fill, float pct = 0f)
        {
            var track = new VisualElement();
            track.style.height = height; track.style.backgroundColor = Track; Radius(track, height / 2f);
            track.style.overflow = Overflow.Hidden;
            fill = new VisualElement();
            fill.style.position = Position.Absolute; fill.style.left = 0; fill.style.top = 0; fill.style.bottom = 0;
            fill.style.width = Length.Percent(pct * 100f);
            fill.style.backgroundColor = color; Radius(fill, height / 2f);
            track.Add(fill);
            return track;
        }

        public static VisualElement Divider(float marginV = 10)
        {
            var d = new VisualElement();
            d.style.height = 2; d.style.backgroundColor = Track;
            d.style.marginTop = marginV; d.style.marginBottom = marginV; Radius(d, 1);
            return d;
        }

        public static VisualElement VDivider(float marginH = 12)
        {
            var d = new VisualElement();
            d.style.width = 2; d.style.backgroundColor = Track; d.style.alignSelf = Align.Stretch;
            d.style.marginLeft = marginH; d.style.marginRight = marginH; Radius(d, 1);
            return d;
        }

        /// <summary>Avatar caricato: círculo de cor + silhueta. <paramref name="tone"/> escolhe a cor.</summary>
        public static VisualElement Avatar(int tone, int size = 44, string icon = IcUser)
        {
            Color[] tones = { Teal, Pink, Purple, Sky, Orange, Green, Gold, Red };
            var c = tones[Mathf.Abs(tone) % tones.Length];
            var b = new VisualElement();
            b.style.width = size; b.style.height = size; b.style.flexShrink = 0; Radius(b, size / 2f);
            b.style.backgroundColor = A(c, 0.22f); Border(b, c, 2);
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.FlexEnd; b.style.overflow = Overflow.Hidden;
            var i = Icon(icon, Mathf.RoundToInt(size * 0.62f), c);
            i.style.marginBottom = -size * 0.08f;
            b.Add(i);
            return b;
        }

        /// <summary>Peso do GDD (-3..+3) desenhado como pílulas: vermelho à esquerda, verde à direita.</summary>
        public static VisualElement Weight(int level)
        {
            var r = Row();
            for (int i = -3; i <= 3; i++)
            {
                if (i == 0) continue;
                bool on = level < 0 ? (i < 0 && i >= level) : (i > 0 && i <= level);
                var p = new VisualElement();
                p.style.width = 9; p.style.height = 9; Radius(p, 3); p.style.marginRight = 2;
                p.style.backgroundColor = on ? (i < 0 ? Red : Green) : Track;
                r.Add(p);
            }
            return r;
        }

        public static VisualElement Stars(float value, int max = 5, int size = 12)
        {
            var r = Row();
            for (int i = 0; i < max; i++)
            {
                var s = Icon(IcStar, size, i < Mathf.RoundToInt(value) ? Gold : Track);
                s.style.marginRight = 2; r.Add(s);
            }
            return r;
        }

        // ---------------- formatação ----------------
        public static string Money(float value)
        {
            long n = (long)Mathf.Round(Mathf.Abs(value));
            string s = n.ToString();
            var sb = new System.Text.StringBuilder();
            int c = 0;
            for (int i = s.Length - 1; i >= 0; i--)
            {
                sb.Insert(0, s[i]);
                if (++c % 3 == 0 && i > 0) sb.Insert(0, '.');
            }
            return (value < 0 ? "-R$ " : "R$ ") + sb;
        }

        public static string Short(float value)
        {
            float a = Mathf.Abs(value);
            string sign = value < 0 ? "-" : "";
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (a >= 1_000_000f) return sign + (a / 1_000_000f).ToString("0.##", inv).Replace('.', ',') + "M";
            if (a >= 1_000f) return sign + (a / 1_000f).ToString("0.#", inv).Replace('.', ',') + "K";
            return sign + Mathf.RoundToInt(a);
        }

        public static Vector2 OnCircle(Vector2 c, float r, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector2(c.x + r * Mathf.Cos(a), c.y + r * Mathf.Sin(a));
        }

        public static void Disc(Painter2D p, Vector2 pos, float r, Color c)
        {
            p.fillColor = c; p.BeginPath(); p.Arc(pos, r, new Angle(0f), new Angle(360f)); p.Fill();
        }
    }
}
