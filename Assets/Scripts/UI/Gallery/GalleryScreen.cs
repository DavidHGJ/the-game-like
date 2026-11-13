using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>
    /// Base das telas da galeria (showcase de direção visual, dados fictícios). Cada tela monta
    /// sua árvore em <see cref="Build"/> e ganha um mini-sistema de tweens: entrada em cascata
    /// dos blocos, números contando, barras enchendo. Animação contínua vai em <see cref="OnTick"/>.
    /// </summary>
    internal abstract class GalleryScreen
    {
        private sealed class Tween
        {
            public float delay, dur, age;
            public Action<float> apply;
            public Func<float, float> ease;
        }

        private readonly List<Tween> _tweens = new List<Tween>();
        private float _enterCursor;

        protected VisualElement Root { get; private set; }

        /// <summary>UI do jogo real que abriu esta tela; nulo na galeria (dados fictícios).</summary>
        public GameLike.UI.Game.GameUi Ui { get; set; }

        /// <summary>Chamado quando a tela pede para fechar (X, Cancelar, ação concluída).</summary>
        public Action OnClose { get; set; }

        /// <summary>Se verdadeiro, as animações de entrada são puladas (remontagem após uma ação).</summary>
        protected bool Instant { get; set; }

        protected void RequestClose() => OnClose?.Invoke();

        /// <summary>Remonta a tela no lugar, sem repetir a animação de entrada.</summary>
        protected void Rebuild()
        {
            Instant = true;
            _tweens.Clear();
            _enterCursor = 0f;
            Root.Clear();
            Build();
        }
        protected float Clock { get; private set; }

        /// <summary>Segundos até a animação de entrada assentar; usado pela captura automática.</summary>
        public virtual float SettleSeconds => 2.4f;

        /// <summary>Troca o estado visível (aba/passo). Padrão: tela de estado único.</summary>
        public virtual void SetState(int index) { }

        public void Init(VisualElement root)
        {
            Root = root;
            root.Clear();
            root.style.flexGrow = 1;
            Build();
        }

        public void Tick(float dt)
        {
            Clock += dt;
            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                var tw = _tweens[i];
                tw.age += dt;
                if (tw.age < tw.delay) continue;
                float k = tw.dur <= 0f ? 1f : Mathf.Clamp01((tw.age - tw.delay) / tw.dur);
                tw.apply(tw.ease(k));
                if (k >= 1f) _tweens.RemoveAt(i);
            }
            OnTick(dt);
        }

        protected abstract void Build();
        protected virtual void OnTick(float dt) { }

        // ---------------- easing ----------------
        protected static float OutCubic(float k) => 1f - Mathf.Pow(1f - k, 3f);
        protected static float Linear(float k) => k;

        protected static float OutBack(float k)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
        }

        // ---------------- tweens ----------------
        protected void Animate(float delay, float dur, Action<float> apply, Func<float, float> ease = null)
        {
            if (Instant) { apply(1f); return; }
            apply(0f);
            _tweens.Add(new Tween { delay = delay, dur = dur, apply = apply, ease = ease ?? OutCubic });
        }

        /// <summary>Entrada padrão: sobe, cresce e aparece. Sem <paramref name="delay"/>, entra em cascata.</summary>
        protected T Enter<T>(T el, float delay = -1f, float rise = 22f) where T : VisualElement
        {
            if (delay < 0f) { delay = _enterCursor; _enterCursor += 0.055f; }
            Animate(delay, 0.42f, k =>
            {
                el.style.opacity = Mathf.Clamp01(k * 1.4f);
                el.style.translate = new Translate(0, (1f - k) * rise, 0);
                float s = 0.94f + 0.06f * k;
                el.style.scale = new Scale(new Vector2(s, s));
            }, OutBack);
            return el;
        }

        /// <summary>Entrada com "pop" (para selos, notas, badges).</summary>
        protected T Pop<T>(T el, float delay) where T : VisualElement
        {
            Animate(delay, 0.5f, k =>
            {
                el.style.opacity = Mathf.Clamp01(k * 2f);
                el.style.scale = new Scale(new Vector2(k, k));
            }, OutBack);
            return el;
        }

        protected void SlideIn(VisualElement el, float fromX, float fromY, float delay, float dur = 0.5f)
        {
            Animate(delay, dur, k =>
            {
                el.style.opacity = Mathf.Clamp01(k * 1.5f);
                el.style.translate = new Translate(fromX * (1f - k), fromY * (1f - k), 0);
            }, OutCubic);
        }

        protected void CountUp(Label label, float from, float to, float delay, float dur, Func<float, string> format)
        {
            Animate(delay, dur, k => label.text = format(Mathf.Lerp(from, to, k)));
        }

        protected void Grow(VisualElement fill, float pct01, float delay, float dur = 0.7f)
        {
            Animate(delay, dur, k => fill.style.width = Length.Percent(pct01 * 100f * k));
        }

        protected void ResetCascade(float start = 0f) => _enterCursor = start;

        // ---------------- molduras ----------------
        /// <summary>Véu escuro sobre o jogo (o escritório/HUD continuam visíveis por trás).</summary>
        protected VisualElement Scrim(float alpha = 0.42f)
        {
            var s = Kit.Fill();
            s.style.backgroundColor = Kit.A(Kit.Ink, alpha);
            Root.Add(s);
            Animate(0f, 0.25f, k => s.style.opacity = k, Linear);
            return s;
        }

        /// <summary>Janela centralizada com cabeçalho (ícone + título + fechar). Devolve o corpo.</summary>
        protected VisualElement Window(string title, string icon, Color accent, float width, float height, out VisualElement window, string subtitle = null)
        {
            var center = Kit.Centered();
            Root.Add(center);

            window = Kit.Card(width, 20);
            if (height > 0) window.style.height = height;
            Kit.Pad(window, 0, 0);
            window.style.borderBottomWidth = 6;
            window.style.overflow = Overflow.Hidden;
            center.Add(window);

            var head = Kit.Row();
            Kit.Pad(head, 14, 20);
            head.style.backgroundColor = Kit.Sand;
            head.Add(Kit.Badge(icon, accent, 36));
            var titles = Kit.Col(); titles.style.marginLeft = 12;
            titles.Add(Kit.Text(title, 20, Kit.Ink, true));
            if (subtitle != null) titles.Add(Kit.Text(subtitle, 12, Kit.Soft));
            head.Add(titles);
            head.Add(Kit.Spacer());
            head.Add(Kit.RoundButton(Kit.IcClose, Kit.Paper, Kit.Soft, 32, RequestClose));
            window.Add(head);

            var body = Kit.Col();
            body.style.flexGrow = 1;
            Kit.Pad(body, 18, 22);
            window.Add(body);

            var w = window;
            Animate(0.05f, 0.45f, k =>
            {
                w.style.opacity = Mathf.Clamp01(k * 1.6f);
                float s = 0.9f + 0.1f * k;
                w.style.scale = new Scale(new Vector2(s, s));
                w.style.translate = new Translate(0, (1f - k) * 26f, 0);
            }, OutBack);
            ResetCascade(0.22f);
            return body;
        }

        protected static VisualElement Footer(VisualElement body)
        {
            var f = Kit.Row();
            f.style.justifyContent = Justify.FlexEnd;
            f.style.marginTop = 16;
            body.Add(f);
            return f;
        }

        protected static VisualElement Section(VisualElement parent, string title)
        {
            var l = Kit.Caps(title, 11, Kit.Soft);
            l.style.marginBottom = 8;
            parent.Add(l);
            return l;
        }
    }
}
