using GameLike.Core.Events;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Core;
using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Mostra o escritório 3D da fase atual como cenário de gameplay. Lê a fase do
    /// <see cref="GameController"/> (fallback Garage), carrega o
    /// <see cref="OfficeLayoutSO"/> correspondente de <c>Resources/Office</c> e o
    /// constrói via <see cref="OfficeBuilder"/>. Reconstrói ao mudar de fase
    /// (<see cref="PhaseChangedEvent"/>), dando a evolução visual do escritório.
    /// </summary>
    public sealed class OfficeView : MonoBehaviour
    {
        [Tooltip("Layout usado se não houver um em Resources/Office para a fase atual.")]
        [SerializeField] private OfficeLayoutSO fallbackLayout;

        private GameController _gc;
        private CompanyPhase _shownPhase;
        private bool _built;

        private void OnEnable() => EventBus.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
        private void OnDisable() => EventBus.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);

        private void Start()
        {
            _gc = FindFirstObjectByType<GameController>();
            Rebuild();
        }

        private void OnPhaseChanged(PhaseChangedEvent e) => Rebuild();

        /// <summary>
        /// Carregar um save ou começar outra run troca a fase sem passar pelo evento de mudança
        /// de fase; conferir aqui garante que o cenário na tela é sempre o da fase atual.
        /// </summary>
        private void LateUpdate()
        {
            if (_built && ResolvePhase() != _shownPhase) Rebuild();
        }

        /// <summary>Reconstrói o escritório para a fase atual.</summary>
        public void Rebuild()
        {
            var phase = ResolvePhase();
            _shownPhase = phase;
            _built = true;
            var layout = LoadLayoutFor(phase) ?? fallbackLayout;
            if (layout == null) return;
            OfficeBuilder.Build(layout, transform);
            var camera = Camera.main;
            if (camera != null && camera.orthographic) camera.orthographicSize = OfficeGrid.GameCameraSize(layout.Cols, layout.Rows);
            OfficeAtmosphere.Apply(layout, camera);
            var crew = GetComponent<OfficeCrew>();
            if (crew == null) crew = gameObject.AddComponent<OfficeCrew>();
            crew.Show(layout);
        }

        private CompanyPhase ResolvePhase()
        {
            if (_gc == null) _gc = FindFirstObjectByType<GameController>();
            var run = _gc != null ? _gc.Run : null;
            return run != null && run.company != null ? run.company.phase : CompanyPhase.Garage;
        }

        private static OfficeLayoutSO LoadLayoutFor(CompanyPhase phase)
        {
            // Cenário salvo pelo Map Builder in-game e marcado para esta fase tem prioridade.
            var map = OfficeMapStore.FindForPhase(phase);
            if (map != null)
            {
                var catalog = OfficeMapStore.Catalog;
                if (catalog != null) return OfficeLayoutSO.FromMapData(map, catalog);
                Debug.LogWarning("[OfficeView] Cenário '" + map.name + "' ignorado: catálogo de mobília ausente em Resources/"
                                 + FurnitureCatalogSO.ResourcePath + ".");
            }

            // Sem cenário para a fase: usa o da fase anterior mais próxima (a empresa "continua no
            // mesmo lugar" até existir o cenário novo), nunca um qualquer.
            var all = Resources.LoadAll<OfficeLayoutSO>("Office");
            OfficeLayoutSO best = null, any = null;
            foreach (var l in all)
            {
                if (l == null) continue;
                if (l.Phase == phase) return l;
                if (any == null || l.Phase < any.Phase) any = l;
                if (l.Phase < phase && (best == null || l.Phase > best.Phase)) best = l;
            }
            for (int lower = (int)phase - 1; lower >= 0; lower--)
            {
                var lowerMap = OfficeMapStore.FindForPhase((CompanyPhase)lower);
                if (lowerMap == null || OfficeMapStore.Catalog == null) continue;
                if (best != null && (int)best.Phase > lower) break;
                return OfficeLayoutSO.FromMapData(lowerMap, OfficeMapStore.Catalog);
            }
            return best != null ? best : any;
        }
    }
}
