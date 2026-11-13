using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>
    /// Menu inicial animado — 3 direções num só script (campo <see cref="variant"/>):
    /// 0 = Console creme (handheld + sunburst), 1 = Estúdio (cards + partículas),
    /// 2 = Arcade (blobs + pills). Constrói tudo em código sobre o UIDocument e anima
    /// por frame (fundo em Painter2D, hover nos itens, seleção). Ferramenta de design.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class AnimatedMenu : MonoBehaviour
    {
        [SerializeField] private int variant;

        private const string IcPlay = "", IcFolder = "", IcGear = "", IcPower = "";

        private static Color RGB(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
        private static readonly Color Ink = RGB(36, 31, 23);
        private static readonly Color Soft = RGB(110, 100, 84);
        private static readonly Color Paper = RGB(251, 248, 241);
        private static readonly Color Teal = RGB(42, 157, 143);
        private static readonly Color Screen = RGB(19, 48, 41);
        private static readonly Color Mint = RGB(207, 224, 214);

        private System.Action<int> _onChoose;

        /// <summary>
        /// Liga o menu ao jogo real: escolhe a variante, troca os itens (rótulo + ícone) e recebe
        /// o índice do item clicado. Chamar logo após o AddComponent, antes do primeiro frame.
        /// </summary>
        public void Configure(int menuVariant, (string label, string icon)[] items, System.Action<int> onChoose)
        {
            variant = menuVariant;
            if (items != null && items.Length > 0) Items = items;
            _onChoose = onChoose;
        }

        private (string label, string icon)[] Items =
        {
            ("Novo jogo", IcPlay), ("Continuar", IcFolder), ("Opções", IcGear), ("Sair", IcPower)
        };

        private UIDocument _doc; private FontAsset _iconFont; private bool _built; private float _t;
        private VisualElement _root, _bg, _console;
        private readonly List<VisualElement> _itemEls = new List<VisualElement>();
        private readonly List<VisualElement> _itemHi = new List<VisualElement>();
        private int _selected;
        private float _tiltX, _tiltTargetX, _tiltY, _tiltTargetY;

        private void Awake() => _doc = GetComponent<UIDocument>();

        private void Update()
        {
            if (!_built)
            {
                if (_doc != null && _doc.rootVisualElement != null) Build();
                return;
            }
            _t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (_bg != null) _bg.MarkDirtyRepaint();
            Animate();
        }

        private void Build()
        {
            _iconFont = Resources.Load<FontAsset>("IconFont");
            _root = _doc.rootVisualElement;
            _root.Clear();
            _root.style.flexGrow = 1;

            _bg = new VisualElement();
            _bg.style.position = Position.Absolute;
            _bg.style.left = 0; _bg.style.right = 0; _bg.style.top = 0; _bg.style.bottom = 0;
            _bg.pickingMode = PickingMode.Ignore;
            _bg.style.backgroundColor = variant == 0 ? RGB(214, 216, 240)
                : variant == 1 ? RGB(236, 240, 238) : RGB(244, 239, 229);
            _bg.generateVisualContent += DrawBg;
            _root.Add(_bg);

            _root.RegisterCallback<PointerMoveEvent>(e =>
            {
                var rc = _root.contentRect;
                if (rc.width < 1f || rc.height < 1f) return;
                _tiltTargetX = Mathf.Clamp((e.position.x - rc.width * 0.5f) / (rc.width * 0.5f), -1f, 1f);
                _tiltTargetY = Mathf.Clamp((e.position.y - rc.height * 0.5f) / (rc.height * 0.5f), -1f, 1f);
            });

            if (variant == 0) BuildConsole();
            else if (variant == 1) BuildStudio();
            else BuildArcade();

            _built = true;
        }

        // ================= VARIANTE 0 — CONSOLE CREME =================
        private void BuildConsole()
        {
            var center = Centered();
            var body = new VisualElement();
            _console = body;
            body.style.width = 320; Radius(body, 28);
            body.style.backgroundColor = RGB(236, 229, 217);
            body.style.borderBottomWidth = 6; SetBorderColor(body, new Color(0, 0, 0, 0.12f));
            body.style.paddingTop = 16; body.style.paddingBottom = 24; body.style.paddingLeft = 18; body.style.paddingRight = 18;

            var brand = new VisualElement(); brand.style.flexDirection = FlexDirection.Row;
            brand.style.justifyContent = Justify.SpaceBetween; brand.style.marginBottom = 8;
            brand.Add(Txt("THE GAME LIKE", 9, RGB(137, 127, 104), true, 2));
            var led = new VisualElement(); led.style.width = 8; led.style.height = 8; Radius(led, 4);
            led.style.backgroundColor = RGB(224, 103, 90); led.name = "led"; brand.Add(led);
            body.Add(brand);

            var screen = new VisualElement();
            screen.style.backgroundColor = Screen; Radius(screen, 12);
            screen.style.borderBottomWidth = 0; screen.style.borderTopWidth = 6; SetBorderColor(screen, RGB(17, 35, 29));
            screen.style.paddingTop = 16; screen.style.paddingBottom = 16; screen.style.paddingLeft = 14; screen.style.paddingRight = 14;
            var title = Txt("THE GAME LIKE", 20, RGB(234, 243, 236), true, 1); title.style.unityTextAlign = TextAnchor.MiddleCenter;
            screen.Add(title);
            var sub = Txt("FUNDE SUA DEV", 8, RGB(111, 154, 139), true, 4); sub.style.unityTextAlign = TextAnchor.MiddleCenter; sub.style.marginBottom = 12;
            screen.Add(sub);
            for (int i = 0; i < Items.Length; i++) screen.Add(MakeScreenItem(i));
            body.Add(screen);

            // controles decorativos
            var ctr = new VisualElement(); ctr.style.flexDirection = FlexDirection.Row; ctr.style.justifyContent = Justify.SpaceBetween;
            ctr.style.alignItems = Align.Center; ctr.style.marginTop = 14; ctr.style.paddingLeft = 6; ctr.style.paddingRight = 6;
            var dpad = new VisualElement(); dpad.style.width = 42; dpad.style.height = 42; Radius(dpad, 6); dpad.style.backgroundColor = RGB(58, 58, 58);
            ctr.Add(dpad);
            var ab = new VisualElement(); ab.style.flexDirection = FlexDirection.Row;
            ab.Add(RoundBtn(RGB(224, 103, 90))); ab.Add(RoundBtn(RGB(81, 163, 94)));
            ctr.Add(ab);
            body.Add(ctr);

            center.Add(body);
            _root.Add(center);
            SelectItem(0);
        }

        private VisualElement MakeScreenItem(int i)
        {
            var it = new VisualElement();
            it.style.flexDirection = FlexDirection.Row; it.style.alignItems = Align.Center;
            it.style.paddingTop = 8; it.style.paddingBottom = 8; it.style.paddingLeft = 11; it.style.paddingRight = 11;
            Radius(it, 8); it.style.marginBottom = 5;
            it.Add(Icon(Items[i].icon, 12, RGB(207, 224, 214)));
            it.Add(Txt("  " + Items[i].label, 13, RGB(207, 224, 214), true, 0));
            HookSelect(it, i);
            _itemEls.Add(it); _itemHi.Add(it);
            return it;
        }

        // ================= VARIANTE 1 — ESTÚDIO (cards) =================
        private void BuildStudio()
        {
            var center = Centered();
            center.Add(Txt("THE GAME LIKE", 40, Ink, true, 1));
            var sub = Txt("Funde sua dev", 14, Soft, false, 0); sub.style.marginBottom = 22; sub.style.unityTextAlign = TextAnchor.MiddleCenter;
            center.Add(sub);

            for (int i = 0; i < Items.Length; i++)
            {
                var card = new VisualElement();
                card.style.flexDirection = FlexDirection.Row; card.style.alignItems = Align.Center;
                card.style.width = 300; card.style.marginBottom = 10;
                card.style.backgroundColor = new Color(Paper.r, Paper.g, Paper.b, 0.98f);
                Radius(card, 12); card.style.borderBottomWidth = 3; SetBorderColor(card, new Color(0, 0, 0, 0.1f));
                card.style.paddingTop = 13; card.style.paddingBottom = 13; card.style.paddingLeft = 16; card.style.paddingRight = 16;
                var badge = new VisualElement(); badge.style.width = 30; badge.style.height = 30; Radius(badge, 15);
                badge.style.backgroundColor = Teal; badge.style.alignItems = Align.Center; badge.style.justifyContent = Justify.Center;
                badge.Add(Icon(Items[i].icon, 14, Color.white)); card.Add(badge);
                card.Add(Txt("   " + Items[i].label, 16, Ink, true, 0));
                HookSelect(card, i);
                center.Add(card);
                _itemEls.Add(card); _itemHi.Add(badge);
            }
            _root.Add(center);
            SelectItem(0);
        }

        // ================= VARIANTE 2 — ARCADE (pills) =================
        private void BuildArcade()
        {
            var center = Centered();
            var title = Txt("THE GAME LIKE", 46, Teal, true, 1); title.name = "arcTitle";
            center.Add(title);
            var sub = Txt("FUNDE SUA DEV", 12, Soft, true, 5); sub.style.marginBottom = 24; sub.style.unityTextAlign = TextAnchor.MiddleCenter;
            center.Add(sub);

            Color[] cols = { Teal, RGB(47, 116, 201), RGB(224, 145, 74), RGB(110, 100, 84) };
            for (int i = 0; i < Items.Length; i++)
            {
                var pill = new VisualElement();
                pill.style.flexDirection = FlexDirection.Row; pill.style.alignItems = Align.Center; pill.style.justifyContent = Justify.Center;
                pill.style.width = 260; pill.style.height = 46; Radius(pill, 23); pill.style.marginBottom = 11;
                pill.style.backgroundColor = cols[i % cols.Length];
                pill.Add(Icon(Items[i].icon, 15, Color.white));
                pill.Add(Txt("   " + Items[i].label, 16, Color.white, true, 0));
                HookSelect(pill, i);
                center.Add(pill);
                _itemEls.Add(pill); _itemHi.Add(pill);
            }
            _root.Add(center);
            SelectItem(0);
        }

        // ================= seleção / hover =================
        private void HookSelect(VisualElement el, int i)
        {
            el.RegisterCallback<PointerEnterEvent>(_ => SelectItem(i));
            el.RegisterCallback<ClickEvent>(_ => _onChoose?.Invoke(i));
        }

        private void SelectItem(int i)
        {
            _selected = i;
            for (int k = 0; k < _itemEls.Count; k++)
            {
                bool on = k == i;
                var hi = _itemHi[k];
                if (variant == 0) hi.style.backgroundColor = on ? Teal : new Color(0, 0, 0, 0);
                else if (variant == 1) hi.style.scale = new Scale(new Vector2(on ? 1.18f : 1f, on ? 1.18f : 1f));
                _itemEls[k].style.scale = new Scale(new Vector2(on && variant == 2 ? 1.06f : 1f, on && variant == 2 ? 1.06f : 1f));
                _itemEls[k].style.opacity = on ? 1f : (variant == 0 ? 0.8f : 0.92f);
            }
        }

        // ================= animação =================
        private void Animate()
        {
            if (variant == 0)
            {
                var led = _root.Q("led");
                if (led != null) led.style.opacity = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(_t * 2f));

                if (_console != null)   // leve flip/mexida seguindo o mouse
                {
                    float k = 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime);
                    _tiltX = Mathf.Lerp(_tiltX, _tiltTargetX, k);
                    _tiltY = Mathf.Lerp(_tiltY, _tiltTargetY, k);
                    _console.style.rotate = new Rotate(new Angle(_tiltX * 5f));
                    _console.style.translate = new Translate(_tiltX * 10f, _tiltY * 5f, 0);
                }
            }
            // pulso do item selecionado
            if (_selected >= 0 && _selected < _itemHi.Count)
            {
                float pulse = 1f + 0.03f * Mathf.Sin(_t * 5f);
                if (variant != 1) _itemHi[_selected].style.scale = new Scale(new Vector2(pulse * (variant == 2 ? 1.06f : 1f), pulse * (variant == 2 ? 1.06f : 1f)));
            }
            if (variant == 2)
            {
                var tt = _root.Q("arcTitle");
                if (tt != null) { float s = 1f + 0.02f * Mathf.Sin(_t * 2.5f); tt.style.scale = new Scale(new Vector2(s, s)); }
            }
        }

        // ================= fundo (Painter2D) =================
        private void DrawBg(MeshGenerationContext mgc)
        {
            var r = _bg.contentRect;
            if (r.width < 8f || r.height < 8f) return;
            var p = mgc.painter2D;
            float w = r.width, h = r.height;
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);

            if (variant == 0) // sunburst pastel girando + bokeh
            {
                float maxR = Mathf.Sqrt(w * w + h * h);
                int slices = 28; float rot = _t * 4f;
                Color a = RGB(206, 208, 236), b = RGB(224, 225, 246);
                for (int k = 0; k < slices; k++)
                {
                    float a0 = rot + k * (360f / slices);
                    p.fillColor = (k % 2 == 0) ? a : b;
                    p.BeginPath(); p.MoveTo(c);
                    p.LineTo(OnC(c, maxR, a0)); p.LineTo(OnC(c, maxR, a0 + 360f / slices));
                    p.ClosePath(); p.Fill();
                }
                for (int k = 0; k < 7; k++)
                {
                    float bx = (0.5f + 0.42f * Mathf.Sin(_t * 0.25f + k * 1.3f)) * w;
                    float by = ((k * 0.17f + _t * 0.025f) % 1f) * h;
                    p.fillColor = new Color(1f, 1f, 1f, 0.09f);
                    p.BeginPath(); p.Arc(new Vector2(bx, by), w * (0.04f + 0.03f * (k % 3)), new Angle(0f), new Angle(360f)); p.Fill();
                }
            }
            else if (variant == 1) // dust motes + grandes círculos suaves
            {
                for (int k = 0; k < 4; k++)
                {
                    float bx = (0.5f + 0.4f * Mathf.Sin(_t * 0.15f + k * 2.1f)) * w;
                    float by = (0.5f + 0.35f * Mathf.Cos(_t * 0.12f + k * 1.7f)) * h;
                    Color[] cs = { RGB(42, 157, 143), RGB(214, 86, 143), RGB(63, 134, 216), RGB(224, 145, 74) };
                    var cc = cs[k]; cc.a = 0.06f;
                    p.fillColor = cc;
                    p.BeginPath(); p.Arc(new Vector2(bx, by), w * 0.22f, new Angle(0f), new Angle(360f)); p.Fill();
                }
                for (int k = 0; k < 34; k++)
                {
                    float mx = ((k * 0.137f + Mathf.Sin(_t * 0.3f + k) * 0.02f) % 1f) * w;
                    float my = (1f - ((_t * 0.05f + k * 0.11f) % 1f)) * h;
                    float al = 0.12f * (0.5f + 0.5f * Mathf.Sin(_t * 2f + k));
                    p.fillColor = new Color(1f, 1f, 1f, al);
                    p.BeginPath(); p.Arc(new Vector2(mx, my), 1.6f, new Angle(0f), new Angle(360f)); p.Fill();
                }
            }
            else // blobs coloridos em movimento
            {
                Color[] cs = { RGB(42, 157, 143), RGB(214, 86, 143), RGB(240, 180, 60), RGB(63, 134, 216), RGB(139, 109, 214) };
                for (int k = 0; k < 5; k++)
                {
                    float bx = (0.5f + 0.4f * Mathf.Sin(_t * 0.25f + k * 1.7f)) * w;
                    float by = (0.5f + 0.4f * Mathf.Cos(_t * 0.2f + k * 2.1f)) * h;
                    float rad = w * (0.16f + 0.04f * Mathf.Sin(_t + k));
                    var cc = cs[k]; cc.a = 0.12f;
                    p.fillColor = cc;
                    p.BeginPath(); p.Arc(new Vector2(bx, by), rad, new Angle(0f), new Angle(360f)); p.Fill();
                }
            }
        }

        // ================= helpers =================
        private VisualElement Centered()
        {
            var e = new VisualElement();
            e.style.position = Position.Absolute; e.style.left = 0; e.style.right = 0; e.style.top = 0; e.style.bottom = 0;
            e.style.alignItems = Align.Center; e.style.justifyContent = Justify.Center;
            return e;
        }

        private VisualElement RoundBtn(Color c)
        {
            var b = new VisualElement(); b.style.width = 26; b.style.height = 26; Radius(b, 13);
            b.style.backgroundColor = c; b.style.marginLeft = 8; return b;
        }

        private Label Txt(string s, int size, Color c, bool bold, float spacing)
        {
            var l = new Label(s); l.style.fontSize = size; l.style.color = c;
            l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
            if (spacing > 0) l.style.letterSpacing = spacing;
            l.style.marginTop = 0; l.style.marginBottom = 0; l.style.marginLeft = 0; l.style.marginRight = 0;
            return l;
        }

        private Label Icon(string code, int size, Color c)
        {
            var l = new Label(code); l.style.fontSize = size; l.style.color = c;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (_iconFont != null) l.style.unityFontDefinition = new StyleFontDefinition(_iconFont);
            return l;
        }

        private static Vector2 OnC(Vector2 c, float r, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            return new Vector2(c.x + r * Mathf.Cos(a), c.y + r * Mathf.Sin(a));
        }

        private static void Radius(VisualElement v, float r)
        {
            v.style.borderTopLeftRadius = r; v.style.borderTopRightRadius = r;
            v.style.borderBottomLeftRadius = r; v.style.borderBottomRightRadius = r;
        }

        private static void SetBorderColor(VisualElement v, Color c)
        {
            v.style.borderTopColor = c; v.style.borderRightColor = c; v.style.borderBottomColor = c; v.style.borderLeftColor = c;
        }
    }
}
