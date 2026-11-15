using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>Tipo de bolinha de ponto (adendo 23.16): uma por área, pesquisa e bug.</summary>
    public enum OrbKind
    {
        Design = 0,
        Art = 1,
        Audio = 2,
        Tech = 3,
        Content = 4,
        Research = 5,
        Bug = 6
    }

    /// <summary>
    /// Bolinhas de ponto (adendo 23.16): nascem em cima do personagem com um "plup", voam até o item
    /// que recebe o ponto com rastro próprio de cada tipo, entram nele com um respingo e um "+1".
    /// Só desenho — quem chama decide quando lançar e o que acontece na chegada (o contador subir).
    /// Usado pelo HUD do jogo e pela galeria de validação de efeitos.
    /// </summary>
    public sealed class PointOrbs
    {
        private const float PopTime = 0.30f, ArriveTime = 0.42f, OrbSize = 30f;

        /// <summary>Gancho de som: 0 = plup (nasceu), 1 = entrou no item.</summary>
        public static Action<int> Sound;

        private sealed class Flight
        {
            public OrbKind kind;
            public VisualElement root, glow, core;
            public Label icon;
            public Vector2 from, to, control, last;
            public VisualElement target;
            public Action onArrive;
            public int amount;
            public float age, delay, flightTime, trailTimer, seed;
            public bool popped, landed;
        }

        private sealed class Particle
        {
            public VisualElement ve;
            public Vector2 pos, vel;
            public float age, life, size, spin, gravity;
            public bool fadeGrow, rise;
        }

        private sealed class Pulse
        {
            public VisualElement ve;
            public float age;
        }

        private readonly VisualElement _layer;
        private readonly List<Flight> _flights = new List<Flight>();
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly List<Pulse> _pulses = new List<Pulse>();
        private readonly System.Random _rng = new System.Random();

        /// <param name="layer">Camada absoluta que cobre a tela (coordenadas locais dela), sem receber clique.</param>
        public PointOrbs(VisualElement layer)
        {
            _layer = layer;
            _layer.pickingMode = PickingMode.Ignore;
        }

        public int InFlight => _flights.Count;

        public static Color ColorOf(OrbKind kind) => kind switch
        {
            OrbKind.Design => Kit.Teal,
            OrbKind.Art => Kit.Pink,
            OrbKind.Audio => Kit.Purple,
            OrbKind.Tech => Kit.Sky,
            OrbKind.Content => Kit.Green,
            OrbKind.Research => Kit.RGB(38, 166, 196),
            _ => Kit.Red
        };

        public static string IconOf(OrbKind kind) => kind switch
        {
            OrbKind.Design => Kit.IcBulb,
            OrbKind.Art => Kit.IcBrush,
            OrbKind.Audio => Kit.IcMusic,
            OrbKind.Tech => Kit.IcCode,
            OrbKind.Content => Kit.IcCube,
            OrbKind.Research => Kit.IcFlask,
            _ => Kit.IcBug
        };

        public static string NameOf(OrbKind kind) => kind switch
        {
            OrbKind.Design => "Design",
            OrbKind.Art => "Arte",
            OrbKind.Audio => "Áudio",
            OrbKind.Tech => "Tecnologia",
            OrbKind.Content => "Conteúdo",
            OrbKind.Research => "Pesquisa",
            _ => "Bug"
        };

        /// <summary>
        /// Lança uma bolinha de <paramref name="from"/> (coordenadas da camada) até o centro de
        /// <paramref name="target"/>. <paramref name="onArrive"/> roda quando ela entra no item.
        /// </summary>
        public void Launch(OrbKind kind, Vector2 from, VisualElement target, Action onArrive, int amount = 1, float delay = 0f)
        {
            var f = new Flight
            {
                kind = kind, from = from, target = target, onArrive = onArrive, amount = Mathf.Max(1, amount),
                delay = Mathf.Max(0f, delay), seed = (float)_rng.NextDouble() * 100f,
                flightTime = 0.75f + (float)_rng.NextDouble() * 0.25f
            };
            Color color = ColorOf(kind);
            f.root = new VisualElement { pickingMode = PickingMode.Ignore };
            f.root.style.position = Position.Absolute;
            f.root.style.width = OrbSize; f.root.style.height = OrbSize;
            f.root.style.alignItems = Align.Center; f.root.style.justifyContent = Justify.Center;
            f.root.style.opacity = 0f;

            f.glow = Circle(OrbSize * 1.9f, Kit.A(color, 0.28f));
            f.glow.style.position = Position.Absolute;
            f.glow.style.left = -OrbSize * 0.45f; f.glow.style.top = -OrbSize * 0.45f;
            f.root.Add(f.glow);

            f.core = Circle(OrbSize, color);
            f.core.style.borderTopWidth = f.core.style.borderBottomWidth = f.core.style.borderLeftWidth = f.core.style.borderRightWidth = 2.5f;
            var rim = Color.Lerp(color, Color.white, 0.65f);
            f.core.style.borderTopColor = f.core.style.borderBottomColor = f.core.style.borderLeftColor = f.core.style.borderRightColor = rim;
            f.core.style.alignItems = Align.Center; f.core.style.justifyContent = Justify.Center;
            f.icon = Kit.Icon(IconOf(kind), 14, Color.white);
            f.icon.pickingMode = PickingMode.Ignore;
            f.core.Add(f.icon);
            f.root.Add(f.core);
            _layer.Add(f.root);
            Place(f.root, from, OrbSize);
            _flights.Add(f);
        }

        /// <summary>Avança as animações (chamar todo frame, com tempo real).</summary>
        public void Tick(float dt)
        {
            for (int i = _flights.Count - 1; i >= 0; i--)
                if (TickFlight(_flights[i], dt)) { _flights[i].root.RemoveFromHierarchy(); _flights.RemoveAt(i); }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.age += dt;
                float k = Mathf.Clamp01(p.age / p.life);
                p.vel.y += p.gravity * dt;
                p.pos += p.vel * dt;
                Place(p.ve, p.pos, p.size);
                float s = p.fadeGrow ? 0.4f + 2.2f * k : 1f - 0.6f * k;
                p.ve.style.scale = new Scale(new Vector2(s, s));
                if (p.spin != 0f) p.ve.style.rotate = new Rotate(new Angle(p.spin * p.age));
                p.ve.style.opacity = p.fadeGrow ? (1f - k) * 0.85f : 1f - k * k;
                if (k >= 1f) { p.ve.RemoveFromHierarchy(); _particles.RemoveAt(i); }
            }

            for (int i = _pulses.Count - 1; i >= 0; i--)
            {
                var p = _pulses[i];
                p.age += dt;
                float k = Mathf.Clamp01(p.age / 0.35f);
                float s = 1f + 0.22f * Mathf.Sin(k * Mathf.PI) * (1f - 0.4f * k);
                p.ve.style.scale = new Scale(new Vector2(s, s));
                if (k >= 1f) { p.ve.style.scale = new Scale(Vector2.one); _pulses.RemoveAt(i); }
            }
        }

        /// <summary>Remove tudo que está no ar (troca de tela).</summary>
        public void Clear()
        {
            foreach (var f in _flights) f.root.RemoveFromHierarchy();
            foreach (var p in _particles) p.ve.RemoveFromHierarchy();
            foreach (var p in _pulses) p.ve.style.scale = new Scale(Vector2.one);
            _flights.Clear(); _particles.Clear(); _pulses.Clear();
        }

        // ---------------------------------------------------------------- voo

        private bool TickFlight(Flight f, float dt)
        {
            if (f.delay > 0f) { f.delay -= dt; return false; }
            f.age += dt;
            Color color = ColorOf(f.kind);

            if (!f.popped)
            {
                f.popped = true;
                Sound?.Invoke(0);
                for (int k = 0; k < 6; k++)
                {
                    float ang = k / 6f * Mathf.PI * 2f;
                    Spawn(f.from, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 70f, 0.32f, 5f, Kit.A(color, 0.9f), true, 0f);
                }
            }

            // 1) plup: sobe do personagem com escala que passa do ponto e volta.
            if (f.age < PopTime)
            {
                float k = f.age / PopTime;
                float s = k < 0.6f ? Mathf.Lerp(0f, 1.35f, k / 0.6f) : Mathf.Lerp(1.35f, 1f, (k - 0.6f) / 0.4f);
                Vector2 pos = f.from + new Vector2(0f, -30f * Mathf.Sin(k * Mathf.PI * 0.5f));
                f.root.style.opacity = 1f;
                f.root.style.scale = new Scale(new Vector2(s, s));
                Place(f.root, pos, OrbSize);
                f.last = pos;
                return false;
            }

            // 2) voo: curva até o item, com o jeito de cada tipo.
            f.to = TargetPoint(f);
            Vector2 start = f.from + new Vector2(0f, -30f);
            if (f.control == Vector2.zero)
            {
                Vector2 mid = (start + f.to) * 0.5f;
                float lift = 90f + Vector2.Distance(start, f.to) * 0.25f;
                f.control = mid + new Vector2(((float)_rng.NextDouble() - 0.5f) * 160f, -lift);
            }
            float t = Mathf.Clamp01((f.age - PopTime) / f.flightTime);
            if (t < 1f)
            {
                float e = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                Vector2 pos = Bezier(start, f.control, f.to, e) + Wobble(f, t, start);
                Place(f.root, pos, OrbSize);
                float breathe = 1f + 0.08f * Mathf.Sin(f.age * 18f);
                f.root.style.scale = new Scale(new Vector2(breathe, breathe));
                if (f.kind == OrbKind.Content || f.kind == OrbKind.Research)
                    f.icon.style.rotate = new Rotate(new Angle(f.age * (f.kind == OrbKind.Content ? 360f : -220f)));
                f.glow.style.opacity = 0.7f + 0.3f * Mathf.Sin(f.age * 12f + f.seed);
                f.trailTimer -= dt;
                if (f.trailTimer <= 0f)
                {
                    f.trailTimer = 0.028f;
                    Trail(f, pos, pos - f.last);
                }
                f.last = pos;
                return false;
            }

            // 3) chegada: entra no item, respingo e "+1".
            if (!f.landed)
            {
                f.landed = true;
                Sound?.Invoke(1);
                Arrive(f);
                f.onArrive?.Invoke();
                if (f.target != null) _pulses.Add(new Pulse { ve = f.target });
            }
            float a = Mathf.Clamp01((f.age - PopTime - f.flightTime) / ArriveTime);
            float shrink = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0f, 1f, a * 2.2f));
            f.root.style.scale = new Scale(new Vector2(shrink, shrink));
            Place(f.root, f.to, OrbSize);
            return a >= 1f;
        }

        private Vector2 TargetPoint(Flight f)
        {
            if (f.target == null || f.target.panel == null) return f.to;
            var wb = f.target.worldBound;
            if (float.IsNaN(wb.x) || wb.width < 1f) return f.to;
            return _layer.WorldToLocal(wb.center);
        }

        private Vector2 Wobble(Flight f, float t, Vector2 start)
        {
            Vector2 dir = (f.to - start).normalized;
            Vector2 n = new Vector2(-dir.y, dir.x);
            float fade = Mathf.Sin(t * Mathf.PI);
            switch (f.kind)
            {
                case OrbKind.Audio:
                    return n * Mathf.Sin(t * Mathf.PI * 6f) * 16f * fade;
                case OrbKind.Tech:
                    return n * (Mathf.Round(Mathf.Sin(t * Mathf.PI * 5f) * 2f) / 2f) * 18f * fade;
                case OrbKind.Research:
                    float ang = t * Mathf.PI * 7f;
                    return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 14f * fade;
                case OrbKind.Bug:
                    return new Vector2(Mathf.PerlinNoise(f.seed, t * 9f) - 0.5f, Mathf.PerlinNoise(t * 9f, f.seed) - 0.5f) * 44f * fade;
                default:
                    return Vector2.zero;
            }
        }

        private void Trail(Flight f, Vector2 pos, Vector2 velocity)
        {
            Color color = ColorOf(f.kind);
            Vector2 back = -velocity.normalized * 30f;
            switch (f.kind)
            {
                case OrbKind.Design:
                    if (Chance(0.6f)) SpawnGlyph(pos + Jitter(8f), back + Jitter(20f), 0.5f, Kit.IcStar, 9, Color.Lerp(color, Color.white, 0.5f), 0f, 0f);
                    else Spawn(pos, back, 0.35f, 6f, Kit.A(color, 0.7f), false, 0f);
                    break;
                case OrbKind.Art:
                    var paint = new[] { Kit.Pink, Kit.Gold, Kit.Teal, Kit.Purple };
                    Spawn(pos + Jitter(6f), back * 0.3f + new Vector2(0f, 20f), 0.55f, 4f + (float)_rng.NextDouble() * 5f,
                        paint[_rng.Next(paint.Length)], false, 260f);
                    break;
                case OrbKind.Audio:
                    if (Chance(0.35f)) SpawnGlyph(pos, new Vector2(0f, -40f) + Jitter(16f), 0.7f, Kit.IcMusic, 10, Kit.A(color, 0.9f), 0f, 0f);
                    else Spawn(pos, back * 0.5f, 0.3f, 5f, Kit.A(color, 0.6f), false, 0f);
                    break;
                case OrbKind.Tech:
                    var px = Spawn(pos + Jitter(6f), Vector2.zero, 0.4f, 5f, Kit.A(Color.Lerp(color, Color.white, 0.3f), 0.95f), false, 0f);
                    px.ve.style.borderTopLeftRadius = px.ve.style.borderTopRightRadius = px.ve.style.borderBottomLeftRadius = px.ve.style.borderBottomRightRadius = 0f;
                    if (Chance(0.2f)) SpawnText(pos + Jitter(10f), new Vector2(0f, -24f), 0.6f, Chance(0.5f) ? "0" : "1", 10, color);
                    break;
                case OrbKind.Content:
                    var cube = Spawn(pos + Jitter(7f), back * 0.4f + Jitter(10f), 0.5f, 6f, Kit.A(color, 0.9f), false, 120f);
                    cube.ve.style.borderTopLeftRadius = cube.ve.style.borderTopRightRadius = cube.ve.style.borderBottomLeftRadius = cube.ve.style.borderBottomRightRadius = 1.5f;
                    cube.spin = 280f;
                    break;
                case OrbKind.Research:
                    var bubble = Spawn(pos + Jitter(8f), new Vector2(0f, -36f) + Jitter(10f), 0.6f, 7f, new Color(0, 0, 0, 0), false, 0f);
                    Ring(bubble.ve, Kit.A(color, 0.85f), 1.5f);
                    break;
                default:
                    Spawn(pos + Jitter(5f), Jitter(25f), 0.35f, 4f, Kit.A(Kit.RGB(120, 60, 50), 0.8f), false, 0f);
                    break;
            }
        }

        private void Arrive(Flight f)
        {
            Color color = ColorOf(f.kind);
            var ring = Spawn(f.to, Vector2.zero, 0.45f, 26f, new Color(0, 0, 0, 0), true, 0f);
            Ring(ring.ve, color, 3f);
            if (f.kind == OrbKind.Audio)
            {
                var ring2 = Spawn(f.to, Vector2.zero, 0.65f, 18f, new Color(0, 0, 0, 0), true, 0f);
                Ring(ring2.ve, Kit.A(color, 0.7f), 2f);
            }

            int count = f.kind == OrbKind.Content || f.kind == OrbKind.Art ? 10 : 7;
            for (int k = 0; k < count; k++)
            {
                float a = (k + (float)_rng.NextDouble() * 0.6f) / count * Mathf.PI * 2f;
                Vector2 v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (90f + (float)_rng.NextDouble() * 70f);
                switch (f.kind)
                {
                    case OrbKind.Design:
                        SpawnGlyph(f.to, v, 0.5f, Kit.IcStar, 10, Color.Lerp(color, Kit.Gold, 0.5f), 0f, 0f);
                        break;
                    case OrbKind.Art:
                        Spawn(f.to, v * 0.8f, 0.6f, 5f + (float)_rng.NextDouble() * 6f, new[] { Kit.Pink, Kit.Gold, Kit.Teal, Kit.Purple }[k % 4], false, 300f);
                        break;
                    case OrbKind.Tech:
                        var px = Spawn(f.to, v, 0.4f, 5f, Color.Lerp(color, Color.white, 0.4f), false, 0f);
                        px.ve.style.borderTopLeftRadius = px.ve.style.borderTopRightRadius = px.ve.style.borderBottomLeftRadius = px.ve.style.borderBottomRightRadius = 0f;
                        break;
                    case OrbKind.Content:
                        var cube = Spawn(f.to, v, 0.6f, 6f, new[] { Kit.Green, Kit.Gold, Kit.Sky }[k % 3], false, 280f);
                        cube.spin = 420f;
                        break;
                    case OrbKind.Research:
                        var b = Spawn(f.to, v * 0.5f + new Vector2(0f, -40f), 0.7f, 6f, new Color(0, 0, 0, 0), false, 0f);
                        Ring(b.ve, Kit.A(color, 0.9f), 1.5f);
                        break;
                    case OrbKind.Audio:
                        if (k % 2 == 0) SpawnGlyph(f.to, v * 0.6f, 0.6f, Kit.IcMusic, 10, color, 0f, 0f);
                        break;
                    default:
                        Spawn(f.to, v * 0.6f, 0.4f, 5f, Kit.A(Kit.RGB(120, 60, 50), 0.85f), false, 200f);
                        break;
                }
            }

            string plus = "+" + f.amount;
            var label = SpawnText(f.to + new Vector2(0f, -18f), new Vector2(0f, -55f), 0.9f, plus, 20, color);
            label.ve.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        // ---------------------------------------------------------------- partículas

        private Particle Spawn(Vector2 pos, Vector2 vel, float life, float size, Color color, bool fadeGrow, float gravity)
        {
            var ve = Circle(size, color);
            ve.style.position = Position.Absolute;
            _layer.Add(ve);
            var p = new Particle { ve = ve, pos = pos, vel = vel, life = life, size = size, fadeGrow = fadeGrow, gravity = gravity };
            Place(ve, pos, size);
            _particles.Add(p);
            return p;
        }

        private Particle SpawnGlyph(Vector2 pos, Vector2 vel, float life, string icon, int size, Color color, float gravity, float spin)
        {
            var l = Kit.Icon(icon, size, color);
            return AddLabel(l, pos, vel, life, size, gravity, spin);
        }

        private Particle SpawnText(Vector2 pos, Vector2 vel, float life, string text, int size, Color color)
        {
            var l = new Label(text);
            l.style.fontSize = size; l.style.color = color;
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            return AddLabel(l, pos, vel, life, size, 0f, 0f);
        }

        private Particle AddLabel(Label l, Vector2 pos, Vector2 vel, float life, int size, float gravity, float spin)
        {
            l.pickingMode = PickingMode.Ignore;
            l.style.position = Position.Absolute;
            l.style.width = size * 2.4f; l.style.height = size * 1.6f;
            l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0;
            l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0;
            _layer.Add(l);
            var p = new Particle { ve = l, pos = pos, vel = vel, life = life, size = size * 2.4f, gravity = gravity, spin = spin, rise = true };
            Place(l, pos, size * 2.4f);
            _particles.Add(p);
            return p;
        }

        private static VisualElement Circle(float size, Color color)
        {
            var ve = new VisualElement { pickingMode = PickingMode.Ignore };
            ve.style.width = size; ve.style.height = size;
            float r = size * 0.5f;
            ve.style.borderTopLeftRadius = ve.style.borderTopRightRadius = ve.style.borderBottomLeftRadius = ve.style.borderBottomRightRadius = r;
            ve.style.backgroundColor = color;
            return ve;
        }

        private static void Ring(VisualElement ve, Color color, float width)
        {
            ve.style.borderTopWidth = ve.style.borderBottomWidth = ve.style.borderLeftWidth = ve.style.borderRightWidth = width;
            ve.style.borderTopColor = ve.style.borderBottomColor = ve.style.borderLeftColor = ve.style.borderRightColor = color;
        }

        private static void Place(VisualElement ve, Vector2 center, float size)
        {
            ve.style.left = center.x - size * 0.5f;
            ve.style.top = center.y - size * 0.5f;
        }

        private static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }

        private Vector2 Jitter(float r) => new Vector2(((float)_rng.NextDouble() - 0.5f) * 2f * r, ((float)_rng.NextDouble() - 0.5f) * 2f * r);

        private bool Chance(float p) => _rng.NextDouble() < p;
    }
}
