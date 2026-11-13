using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>
    /// Controles interativos das telas da galeria (slider, toggle, seletor, campo, abas,
    /// grupo selecionável) e a arte caricata do fundador desenhada em Painter2D.
    /// </summary>
    internal static class Widgets
    {
        /// <summary>
        /// Slider arrastável. <paramref name="onChange"/> recebe 0..1. <paramref name="limit"/>, se
        /// dado, recebe o valor pedido e devolve o permitido — o controle para ali (não passa).
        /// </summary>
        public static VisualElement Slider(float value01, Color color, Action<float> onChange = null, Func<float, float> limit = null)
        {
            var wrap = new VisualElement();
            wrap.style.height = 24; wrap.style.flexGrow = 1; wrap.style.justifyContent = Justify.Center;

            var track = Kit.Bar(10, color, out var fill, value01);
            wrap.Add(track);

            var knob = new VisualElement();
            knob.style.position = Position.Absolute;
            knob.style.width = 22; knob.style.height = 22; Kit.Radius(knob, 11);
            knob.style.backgroundColor = Color.white; Kit.Border(knob, color, 4);
            knob.style.top = 1; knob.style.left = Length.Percent(value01 * 100f);
            knob.style.translate = new Translate(-11, 0, 0);
            knob.pickingMode = PickingMode.Ignore;
            wrap.Add(knob);

            void Set(float x)
            {
                float w = wrap.contentRect.width;
                if (w < 1f) return;
                float v = Mathf.Clamp01(x / w);
                if (limit != null) v = Mathf.Clamp01(limit(v));
                fill.style.width = Length.Percent(v * 100f);
                knob.style.left = Length.Percent(v * 100f);
                onChange?.Invoke(v);
            }

            wrap.RegisterCallback<PointerDownEvent>(e =>
            {
                wrap.CapturePointer(e.pointerId);
                Kit.Press();
                knob.style.scale = new Scale(new Vector2(1.2f, 1.2f));
                Set(e.localPosition.x);
            });
            wrap.RegisterCallback<PointerMoveEvent>(e => { if (wrap.HasPointerCapture(e.pointerId)) Set(e.localPosition.x); });
            wrap.RegisterCallback<PointerUpEvent>(e =>
            {
                if (wrap.HasPointerCapture(e.pointerId)) wrap.ReleasePointer(e.pointerId);
                knob.style.scale = new Scale(Vector2.one);
            });
            return wrap;
        }

        public static VisualElement Toggle(bool on, Action<bool> onChange = null)
        {
            var pill = new VisualElement();
            pill.style.width = 46; pill.style.height = 26; pill.style.flexShrink = 0; Kit.Radius(pill, 13);
            var knob = new VisualElement();
            knob.style.position = Position.Absolute; knob.style.top = 3; knob.style.left = 3;
            knob.style.width = 20; knob.style.height = 20; Kit.Radius(knob, 10);
            knob.style.backgroundColor = Color.white;
            Kit.Smooth(knob, 0.16f); Kit.Smooth(pill, 0.16f);
            pill.Add(knob);

            bool state = on;
            void Apply()
            {
                pill.style.backgroundColor = state ? Kit.Teal : Kit.Faint;
                knob.style.translate = new Translate(state ? 20 : 0, 0, 0);
            }
            Apply();
            pill.RegisterCallback<ClickEvent>(_ => { state = !state; Apply(); Kit.Press(); onChange?.Invoke(state); });
            return pill;
        }

        /// <summary>Seletor ◀ valor ▶ que percorre as opções.</summary>
        public static VisualElement Select(string[] options, int index = 0, float width = 220, Action<int> onChange = null)
        {
            var row = Kit.Row();
            row.style.width = width; row.style.height = 36; row.style.backgroundColor = Color.white;
            Kit.Radius(row, 10); Kit.Border(row, Kit.Track, 2);
            var label = Kit.Text(options[index], 13, Kit.Ink, true);
            label.style.flexGrow = 1; label.style.unityTextAlign = TextAnchor.MiddleCenter;
            int i = index;
            VisualElement Arrow(string icon, int dir)
            {
                var a = Kit.Icon(icon, 11, Kit.Soft);
                a.style.width = 30; a.style.height = 32;
                a.RegisterCallback<ClickEvent>(_ =>
                {
                    i = (i + dir + options.Length) % options.Length;
                    Kit.Press();
                    label.text = options[i];
                    onChange?.Invoke(i);
                });
                return a;
            }
            row.Add(Arrow(Kit.IcChevLeft, -1)); row.Add(label); row.Add(Arrow(Kit.IcChevRight, 1));
            return row;
        }

        /// <summary>Campo de texto (visual) com cursor piscando.</summary>
        public static VisualElement Field(string value, float width = 0, int fontSize = 15)
        {
            var box = Kit.Row();
            if (width > 0) box.style.width = width;
            box.style.height = 40; box.style.backgroundColor = Color.white;
            Kit.Radius(box, 10); Kit.Border(box, Kit.Teal, 2);
            box.style.paddingLeft = 12; box.style.paddingRight = 12;
            box.Add(Kit.Text(value, fontSize, Kit.Ink, true));
            var caret = new VisualElement();
            caret.style.width = 2; caret.style.height = fontSize + 4; caret.style.marginLeft = 2;
            caret.style.backgroundColor = Kit.Teal;
            box.Add(caret);
            bool vis = true;
            box.schedule.Execute(() => { vis = !vis; caret.style.opacity = vis ? 1f : 0f; }).Every(520);
            return box;
        }

        /// <summary>Campo de texto de verdade (digitável), no visual do kit.</summary>
        public static TextField Input(string value, float width = 0, int fontSize = 15, int maxLength = 28)
        {
            var field = new TextField { value = value ?? "", maxLength = maxLength };
            if (width > 0) field.style.width = width;
            field.style.height = 40; field.style.marginLeft = 0; field.style.marginRight = 0;
            field.style.marginTop = 0; field.style.marginBottom = 0;
            var input = field.Q(className: "unity-base-text-field__input");
            if (input != null)
            {
                input.style.backgroundColor = Color.white;
                Kit.Radius(input, 10); Kit.Border(input, Kit.Teal, 2);
                input.style.paddingLeft = 12; input.style.paddingRight = 12;
                input.style.color = Kit.Ink; input.style.fontSize = fontSize;
                input.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            return field;
        }

        /// <summary>Controle segmentado / abas. O item ativo fica preenchido.</summary>
        public static VisualElement Segmented(string[] options, int selected, Action<int> onChange = null, Color? accent = null, bool[] locked = null)
        {
            Color col = accent ?? Kit.Teal;
            var row = Kit.Row();
            row.style.backgroundColor = Kit.Sand; Kit.Radius(row, 11); Kit.Pad(row, 4, 4);
            row.style.alignSelf = Align.FlexStart;
            var items = new List<VisualElement>();
            var labels = new List<Label>();
            int current = selected;
            void Apply()
            {
                for (int k = 0; k < items.Count; k++)
                {
                    bool on = k == current;
                    bool isLocked = locked != null && locked[k];
                    items[k].style.backgroundColor = on ? col : new Color(0, 0, 0, 0);
                    labels[k].style.color = on ? Color.white : (isLocked ? Kit.Faint : Kit.Soft);
                }
            }
            for (int k = 0; k < options.Length; k++)
            {
                int idx = k;
                bool isLocked = locked != null && locked[k];
                var it = Kit.Row();
                Kit.Pad(it, 7, 16); Kit.Radius(it, 8); Kit.Smooth(it);
                if (isLocked) { var lk = Kit.Icon(Kit.IcLock, 10, Kit.Faint); lk.style.marginRight = 6; it.Add(lk); }
                var lb = Kit.Text(options[k], 13, Kit.Soft, true);
                it.Add(lb);
                if (!isLocked) it.RegisterCallback<ClickEvent>(_ => { current = idx; Apply(); Kit.Press(); onChange?.Invoke(idx); });
                items.Add(it); labels.Add(lb); row.Add(it);
            }
            Apply();
            return row;
        }

        /// <summary>Faz um conjunto de tiles se comportar como seleção única (borda + leve zoom).</summary>
        public static void SelectGroup(IList<VisualElement> tiles, int selected, Color accent, Action<int> onChange = null)
        {
            void Apply(int sel)
            {
                for (int k = 0; k < tiles.Count; k++)
                {
                    bool on = k == sel;
                    Kit.BorderColor(tiles[k], on ? accent : Kit.Track);
                    tiles[k].style.backgroundColor = on ? Kit.A(accent, 0.10f) : Color.white;
                    tiles[k].style.scale = new Scale(on ? new Vector2(1.04f, 1.04f) : Vector2.one);
                }
            }
            for (int k = 0; k < tiles.Count; k++)
            {
                int idx = k;
                Kit.Smooth(tiles[k]);
                tiles[k].RegisterCallback<ClickEvent>(_ => { Apply(idx); Kit.Press(); onChange?.Invoke(idx); });
            }
            Apply(selected);
        }

        /// <summary>Linha "rótulo ........ controle" usada em formulários.</summary>
        public static VisualElement FormRow(string label, VisualElement control, string hint = null)
        {
            var row = Kit.Row();
            row.style.marginBottom = 14; row.style.minHeight = 40;
            var texts = Kit.Col(); texts.style.width = 230; texts.style.flexShrink = 0;
            texts.Add(Kit.Text(label, 14, Kit.Ink, true));
            if (hint != null) texts.Add(Kit.Wrap(hint, 11, Kit.Soft));
            row.Add(texts);
            row.Add(control);
            return row;
        }
    }

    /// <summary>
    /// Fundador caricato desenhado em vetor (placeholder da arte final): cabeça, cabelo,
    /// camisa e acessório trocáveis, com respiração/piscada em idle.
    /// </summary>
    internal sealed class FounderArt : VisualElement
    {
        private static readonly Color[] Skins = { Kit.RGB(244, 204, 170), Kit.RGB(222, 170, 130), Kit.RGB(176, 122, 88), Kit.RGB(120, 80, 58) };
        private static readonly Color[] Hairs = { Kit.RGB(60, 44, 36), Kit.RGB(150, 92, 48), Kit.RGB(228, 190, 96), Kit.RGB(200, 80, 70), Kit.RGB(120, 120, 130) };
        public static readonly Color[] Shirts = { Kit.Teal, Kit.Blue, Kit.Pink, Kit.Orange, Kit.Purple, Kit.Green };

        public int Skin, Hair, HairStyle, Shirt, Accessory;
        private float _t;

        public FounderArt(float size)
        {
            style.width = size; style.height = size; style.flexShrink = 0;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void Tick(float dt) { _t += dt; MarkDirtyRepaint(); }

        private void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect;
            if (r.width < 8f) return;
            var p = mgc.painter2D;
            float s = r.width;
            float breath = Mathf.Sin(_t * 2.2f) * s * 0.008f;
            Vector2 c = new Vector2(s * 0.5f, s * 0.44f + breath);
            float head = s * 0.21f;
            Color skin = Skins[Mathf.Abs(Skin) % Skins.Length];
            Color hair = Hairs[Mathf.Abs(Hair) % Hairs.Length];
            Color shirt = Shirts[Mathf.Abs(Shirt) % Shirts.Length];

            // ombros / camisa
            p.fillColor = shirt;
            p.BeginPath();
            p.MoveTo(new Vector2(s * 0.14f, s));
            p.BezierCurveTo(new Vector2(s * 0.14f, s * 0.70f), new Vector2(s * 0.30f, s * 0.66f), new Vector2(s * 0.5f, s * 0.66f + breath));
            p.BezierCurveTo(new Vector2(s * 0.70f, s * 0.66f), new Vector2(s * 0.86f, s * 0.70f), new Vector2(s * 0.86f, s));
            p.ClosePath(); p.Fill();
            // gola
            p.fillColor = Color.Lerp(shirt, Color.white, 0.35f);
            p.BeginPath();
            p.MoveTo(new Vector2(s * 0.40f, s * 0.665f + breath)); p.LineTo(new Vector2(s * 0.5f, s * 0.76f + breath));
            p.LineTo(new Vector2(s * 0.60f, s * 0.665f + breath)); p.ClosePath(); p.Fill();
            // pescoço
            p.fillColor = Color.Lerp(skin, Kit.Ink, 0.12f);
            p.BeginPath();
            p.MoveTo(new Vector2(s * 0.44f, s * 0.58f)); p.LineTo(new Vector2(s * 0.56f, s * 0.58f));
            p.LineTo(new Vector2(s * 0.56f, s * 0.68f + breath)); p.LineTo(new Vector2(s * 0.5f, s * 0.72f + breath));
            p.LineTo(new Vector2(s * 0.44f, s * 0.68f + breath)); p.ClosePath(); p.Fill();

            // cabelo de trás (estilo longo)
            if (HairStyle % 4 == 2)
            {
                p.fillColor = hair;
                p.BeginPath(); p.Arc(new Vector2(c.x, c.y + head * 0.25f), head * 1.18f, new Angle(0f), new Angle(360f)); p.Fill();
            }
            // cabeça + orelhas
            Kit.Disc(p, new Vector2(c.x - head * 0.98f, c.y + head * 0.08f), head * 0.2f, skin);
            Kit.Disc(p, new Vector2(c.x + head * 0.98f, c.y + head * 0.08f), head * 0.2f, skin);
            Kit.Disc(p, c, head, skin);

            // cabelo
            p.fillColor = hair;
            switch (HairStyle % 4)
            {
                case 0: // curto
                    p.BeginPath(); p.Arc(new Vector2(c.x, c.y - head * 0.12f), head * 1.02f, new Angle(180f), new Angle(360f)); p.ClosePath(); p.Fill();
                    break;
                case 1: // espetado
                    for (int k = 0; k < 5; k++)
                    {
                        float x = c.x - head * 0.9f + k * head * 0.45f;
                        p.BeginPath(); p.MoveTo(new Vector2(x - head * 0.28f, c.y - head * 0.45f));
                        p.LineTo(new Vector2(x + head * 0.05f, c.y - head * (1.35f + 0.12f * (k % 2))));
                        p.LineTo(new Vector2(x + head * 0.34f, c.y - head * 0.45f)); p.ClosePath(); p.Fill();
                    }
                    p.BeginPath(); p.Arc(new Vector2(c.x, c.y - head * 0.2f), head * 1.0f, new Angle(190f), new Angle(350f)); p.ClosePath(); p.Fill();
                    break;
                case 2: // longo (franja)
                    p.BeginPath(); p.Arc(new Vector2(c.x, c.y - head * 0.08f), head * 1.04f, new Angle(175f), new Angle(365f)); p.ClosePath(); p.Fill();
                    break;
                default: // boné
                    p.fillColor = shirt;
                    p.BeginPath(); p.Arc(new Vector2(c.x, c.y - head * 0.22f), head * 1.02f, new Angle(180f), new Angle(360f)); p.ClosePath(); p.Fill();
                    p.fillColor = Color.Lerp(shirt, Kit.Ink, 0.3f);
                    p.BeginPath();
                    p.MoveTo(new Vector2(c.x - head * 0.2f, c.y - head * 0.26f)); p.LineTo(new Vector2(c.x + head * 1.5f, c.y - head * 0.26f));
                    p.LineTo(new Vector2(c.x + head * 1.4f, c.y - head * 0.08f)); p.LineTo(new Vector2(c.x - head * 0.2f, c.y - head * 0.08f));
                    p.ClosePath(); p.Fill();
                    break;
            }

            // olhos (piscam) + sorriso
            bool blink = (_t % 3.4f) < 0.12f;
            float ey = c.y + head * 0.12f;
            if (blink)
            {
                p.strokeColor = Kit.Ink; p.lineWidth = Mathf.Max(1.5f, s * 0.012f); p.lineCap = LineCap.Round;
                p.BeginPath(); p.MoveTo(new Vector2(c.x - head * 0.46f, ey)); p.LineTo(new Vector2(c.x - head * 0.22f, ey)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(c.x + head * 0.22f, ey)); p.LineTo(new Vector2(c.x + head * 0.46f, ey)); p.Stroke();
            }
            else
            {
                Kit.Disc(p, new Vector2(c.x - head * 0.34f, ey), head * 0.1f, Kit.Ink);
                Kit.Disc(p, new Vector2(c.x + head * 0.34f, ey), head * 0.1f, Kit.Ink);
            }
            p.strokeColor = Kit.Ink; p.lineWidth = Mathf.Max(1.5f, s * 0.013f); p.lineCap = LineCap.Round;
            p.BeginPath(); p.Arc(new Vector2(c.x, c.y + head * 0.3f), head * 0.34f, new Angle(25f), new Angle(155f)); p.Stroke();

            // acessório: 1 = óculos, 2 = headset
            if (Accessory % 3 == 1)
            {
                p.lineWidth = Mathf.Max(1.5f, s * 0.014f); p.strokeColor = Kit.Ink;
                p.BeginPath(); p.Arc(new Vector2(c.x - head * 0.34f, ey), head * 0.24f, new Angle(0f), new Angle(360f)); p.Stroke();
                p.BeginPath(); p.Arc(new Vector2(c.x + head * 0.34f, ey), head * 0.24f, new Angle(0f), new Angle(360f)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(c.x - head * 0.1f, ey)); p.LineTo(new Vector2(c.x + head * 0.1f, ey)); p.Stroke();
            }
            else if (Accessory % 3 == 2)
            {
                p.lineWidth = Mathf.Max(2f, s * 0.022f); p.strokeColor = Kit.Ink;
                p.BeginPath(); p.Arc(c, head * 1.1f, new Angle(200f), new Angle(340f)); p.Stroke();
                Kit.Disc(p, new Vector2(c.x - head * 1.04f, c.y + head * 0.1f), head * 0.24f, Kit.Ink);
                Kit.Disc(p, new Vector2(c.x + head * 1.04f, c.y + head * 0.1f), head * 0.24f, Kit.Ink);
            }
        }
    }

    /// <summary>Fundo "sunburst" pastel girando — o mesmo do menu inicial aprovado (v1).</summary>
    internal sealed class SunburstBackdrop : VisualElement
    {
        private readonly Color _a, _b;
        private float _t;

        public SunburstBackdrop(Color background, Color rayA, Color rayB)
        {
            _a = rayA; _b = rayB;
            style.position = Position.Absolute;
            style.left = 0; style.right = 0; style.top = 0; style.bottom = 0;
            style.backgroundColor = background;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public static SunburstBackdrop Lilac() => new SunburstBackdrop(Kit.RGB(214, 216, 240), Kit.RGB(206, 208, 236), Kit.RGB(224, 225, 246));
        public static SunburstBackdrop Mint() => new SunburstBackdrop(Kit.RGB(212, 234, 226), Kit.RGB(202, 228, 219), Kit.RGB(224, 241, 234));
        public static SunburstBackdrop Rose() => new SunburstBackdrop(Kit.RGB(240, 216, 214), Kit.RGB(234, 204, 202), Kit.RGB(246, 226, 224));
        public static SunburstBackdrop Gold() => new SunburstBackdrop(Kit.RGB(244, 232, 200), Kit.RGB(240, 224, 184), Kit.RGB(249, 240, 214));

        public void Tick(float dt) { _t += dt; MarkDirtyRepaint(); }

        private void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect;
            if (r.width < 8f || r.height < 8f) return;
            var p = mgc.painter2D;
            float w = r.width, h = r.height;
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
            float maxR = Mathf.Sqrt(w * w + h * h);
            const int slices = 28;
            float rot = _t * 4f;
            for (int k = 0; k < slices; k++)
            {
                float a0 = rot + k * (360f / slices);
                p.fillColor = (k % 2 == 0) ? _a : _b;
                p.BeginPath(); p.MoveTo(c);
                p.LineTo(Kit.OnCircle(c, maxR, a0)); p.LineTo(Kit.OnCircle(c, maxR, a0 + 360f / slices));
                p.ClosePath(); p.Fill();
            }
            for (int k = 0; k < 7; k++)
            {
                float bx = (0.5f + 0.42f * Mathf.Sin(_t * 0.25f + k * 1.3f)) * w;
                float by = ((k * 0.17f + _t * 0.025f) % 1f) * h;
                Kit.Disc(p, new Vector2(bx, by), w * (0.04f + 0.03f * (k % 3)), new Color(1f, 1f, 1f, 0.09f));
            }
        }
    }
}
