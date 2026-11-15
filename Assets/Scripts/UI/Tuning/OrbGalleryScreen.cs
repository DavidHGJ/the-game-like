using System.Collections.Generic;
using GameLike.Systems.Core;
using GameLike.UI.Gallery;
using GameLike.UI.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Tuning
{
    /// <summary>
    /// Galeria de validação das bolinhas de ponto (adendo 23.16) — ferramenta do dev, só no editor e
    /// em development build. Usa o mesmo <see cref="PointOrbs"/> do HUD: cada botão faz um personagem
    /// gerar um ponto daquele tipo, a bolinha sai de cima dele com "plup", voa até o item e o contador
    /// sobe "+1" na chegada.
    /// </summary>
    public sealed class OrbGalleryScreen : MonoBehaviour
    {
        private const string PanelResource = "HudPanelSettings";
        private const int KindCount = 7;

        private sealed class Target
        {
            public VisualElement element;
            public Label counter;
            public int value;
        }

        private VisualElement _root, _stage, _layer;
        private PointOrbs _orbs;
        private readonly Target[] _targets = new Target[KindCount];
        private readonly List<FounderArt> _crew = new List<FounderArt>();
        private readonly List<VisualElement> _crewSlots = new List<VisualElement>();
        private int _crewSize = 1, _cursor;
        private float _rainLeft, _rainTimer;
        private bool _resumeTime;
        private Label _status;

        public static bool IsAvailable => GameLike.Core.DevTools.Enabled;

        /// <summary>Abre a galeria por cima do que estiver na tela.</summary>
        public static void Open()
        {
            if (!IsAvailable || FindFirstObjectByType<OrbGalleryScreen>() != null) return;
            new GameObject("[OrbGallery]").AddComponent<OrbGalleryScreen>();
        }

        /// <summary>Dispara uma bolinha de cada tipo (usado pelo print automático).</summary>
        public void FireAll()
        {
            for (int k = 0; k < KindCount; k++) Fire((OrbKind)k, k * 0.18f);
        }

        /// <summary>Liga a "chuva" de pontos aleatórios por alguns segundos.</summary>
        public void StartRain(float seconds) => _rainLeft = seconds;

        private void Awake()
        {
            var panel = Resources.Load<PanelSettings>(PanelResource);
            if (panel == null) { Destroy(gameObject); return; }
            var gc = FindFirstObjectByType<GameController>();
            if (gc != null && gc.Time != null && !gc.Time.IsPaused) { gc.Time.Pause(); _resumeTime = true; }

            Sfx.Ensure();
            PointOrbs.Sound = kind => Sfx.Play(kind == 0 ? SfxId.Plup : SfxId.Absorb, 0.92f + Random.value * 0.16f);

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.sortingOrder = 60;
            _root = document.rootVisualElement;
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = Kit.RGB(238, 232, 219);
            Build();
        }

        private void OnDestroy()
        {
            if (FindFirstObjectByType<GameUi>() == null) PointOrbs.Sound = null;
        }

        private void Close()
        {
            if (_resumeTime)
            {
                var gc = FindFirstObjectByType<GameController>();
                if (gc != null && gc.Time != null) gc.Time.Resume();
            }
            Destroy(gameObject);
        }

        private void Build()
        {
            var page = Kit.Col(); page.style.flexGrow = 1; Kit.Pad(page, 22, 28);
            _root.Add(page);

            var top = Kit.Row();
            top.Add(Kit.Badge(Kit.IcMagic, Kit.Purple, 40));
            var titles = Kit.Col(); titles.style.marginLeft = 12; titles.style.flexGrow = 1;
            titles.Add(Kit.Text("Bolinhas de ponto", 22, Kit.Ink, true));
            titles.Add(Kit.Text("Validação dos efeitos: o ponto sai de cima do personagem, voa até o item e o contador sobe +1.", 12, Kit.Soft));
            top.Add(titles);
            top.Add(Kit.Button("Fechar", Kit.IcClose, Kit.Sand, Kit.Ink, Close, 38));
            page.Add(top);

            var buttons = Kit.Row(); buttons.style.marginTop = 16; buttons.style.flexWrap = Wrap.Wrap;
            for (int k = 0; k < KindCount; k++)
            {
                var kind = (OrbKind)k;
                var b = Kit.Button(PointOrbs.NameOf(kind), PointOrbs.IconOf(kind), PointOrbs.ColorOf(kind), Color.white, () => Fire(kind, 0f), 36);
                b.style.marginRight = 8; b.style.marginBottom = 8;
                buttons.Add(b);
            }
            var all = Kit.Button("Um de cada", Kit.IcLayers, Kit.Ink, Color.white, FireAll, 36);
            all.style.marginRight = 8; all.style.marginBottom = 8;
            buttons.Add(all);
            var rain = Kit.Button("Chuva (4 s)", Kit.IcBolt, Kit.Orange, Color.white, () => StartRain(4f), 36);
            rain.style.marginRight = 8; rain.style.marginBottom = 8;
            buttons.Add(rain);
            var team = Kit.Button("Equipe: 1 / 3 / 6", Kit.IcUsers, Kit.Sand, Kit.Ink, CycleCrew, 36);
            team.style.marginBottom = 8;
            buttons.Add(team);
            page.Add(buttons);

            _stage = Kit.Card(0, 18); _stage.style.flexGrow = 1; _stage.style.marginTop = 8;
            _stage.style.backgroundColor = Kit.RGB(250, 246, 237);
            page.Add(_stage);

            var targets = Kit.Row(); targets.style.justifyContent = Justify.Center; targets.style.marginTop = 26;
            targets.style.flexWrap = Wrap.Wrap;
            for (int k = 0; k < KindCount; k++) targets.Add(BuildTarget((OrbKind)k));
            _stage.Add(targets);

            _stage.Add(Kit.Spacer());
            var crewRow = Kit.Row(); crewRow.style.justifyContent = Justify.Center; crewRow.style.marginBottom = 24;
            _stage.Add(crewRow);
            for (int i = 0; i < 6; i++)
            {
                var slot = Kit.Col(); slot.style.alignItems = Align.Center; slot.style.marginLeft = 14; slot.style.marginRight = 14;
                var art = new FounderArt(120) { Skin = i % 4, Hair = (i * 2) % 5, HairStyle = 1 + i % 3, Shirt = i % 6, Accessory = i % 3 };
                slot.Add(art);
                var floor = new VisualElement(); floor.style.width = 92; floor.style.height = 10; Kit.Radius(floor, 5);
                floor.style.backgroundColor = Kit.A(Kit.Ink, 0.08f);
                slot.Add(floor);
                slot.Add(Kit.Text(i == 0 ? "Fundador" : "Funcionário " + i, 11, Kit.Soft, true));
                crewRow.Add(slot);
                _crew.Add(art);
                _crewSlots.Add(slot);
            }
            _status = Kit.Text("", 11, Kit.Faint, true);
            _status.style.alignSelf = Align.Center; _status.style.marginBottom = 10;
            _stage.Add(_status);
            ApplyCrew();

            _layer = new VisualElement();
            _layer.style.position = Position.Absolute;
            _layer.style.left = 0; _layer.style.top = 0; _layer.style.right = 0; _layer.style.bottom = 0;
            _root.Add(_layer);
            _orbs = new PointOrbs(_layer);
        }

        private VisualElement BuildTarget(OrbKind kind)
        {
            Color color = PointOrbs.ColorOf(kind);
            var box = Kit.Col(); box.style.alignItems = Align.Center; box.style.marginLeft = 16; box.style.marginRight = 16; box.style.marginBottom = 12;
            var disc = Kit.Badge(PointOrbs.IconOf(kind), color, 64);
            box.Add(disc);
            var counter = Kit.Text("0", 22, color, true); counter.style.marginTop = 6;
            box.Add(counter);
            box.Add(Kit.Text(PointOrbs.NameOf(kind) + (kind == OrbKind.Research ? " (contador)" : kind == OrbKind.Bug ? " (contador)" : " (item da fase)"), 11, Kit.Soft, true));
            _targets[(int)kind] = new Target { element = disc, counter = counter };
            return box;
        }

        private void CycleCrew()
        {
            _crewSize = _crewSize == 1 ? 3 : _crewSize == 3 ? 6 : 1;
            ApplyCrew();
        }

        private void ApplyCrew()
        {
            for (int i = 0; i < _crewSlots.Count; i++)
                _crewSlots[i].style.display = i < _crewSize ? DisplayStyle.Flex : DisplayStyle.None;
            _status.text = _crewSize == 1 ? "Só o fundador produz" : _crewSize + " pessoas produzindo — as bolinhas saem de cada uma em rodízio";
        }

        private void Fire(OrbKind kind, float delay)
        {
            if (_orbs == null) return;
            var target = _targets[(int)kind];
            var source = _crew[_cursor++ % _crewSize];
            var wb = source.worldBound;
            Vector2 head = _layer.WorldToLocal(new Vector2(wb.center.x, wb.yMin + wb.height * 0.12f));
            _orbs.Launch(kind, head, target.element, () =>
            {
                target.value++;
                target.counter.text = target.value.ToString();
            }, 1, delay);
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            foreach (var art in _crew) art.Tick(dt);
            if (_rainLeft > 0f)
            {
                _rainLeft -= dt;
                _rainTimer -= dt;
                if (_rainTimer <= 0f)
                {
                    _rainTimer = 0.11f;
                    Fire((OrbKind)Random.Range(0, KindCount), 0f);
                }
            }
            _orbs?.Tick(dt);
        }
    }
}
