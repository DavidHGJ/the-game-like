using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>Telas disponíveis na galeria (uma por GameObject, ligadas/desligadas na Hierarchy).</summary>
    public enum GalleryScreenId
    {
        SaveSlots, Options, FounderCreate,
        NewProject, PhaseAllocation, Marketing, Launch, Reviews,
        Finance, Projects, Staff, Market, Research, Relations, Founder,
        NarrativeEvent, PhaseExpansion, Acquisition, SellCompany, PauseMenu, Toasts,
        RunResult, SkillTree
    }

    /// <summary>
    /// Hospeda uma tela da galeria sobre o <see cref="UIDocument"/> do mesmo GameObject.
    /// Reativar o GameObject remonta a tela e toca a animação de entrada de novo.
    /// Ferramenta de design (dados fictícios) — a lógica real entra depois, tela a tela.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class GalleryScreenHost : MonoBehaviour
    {
        [SerializeField] private GalleryScreenId screen;
        [SerializeField] private bool overGameplay = true;
        [SerializeField] private int initialState;

        private UIDocument _doc;
        private GalleryScreen _screen;

        /// <summary>True quando a tela aparece por cima do jogo (a captura mostra o HUD atrás).</summary>
        public bool OverGameplay => overGameplay;

        /// <summary>Segundos até a entrada da tela assentar (para a captura automática).</summary>
        public float SettleSeconds => _screen != null ? _screen.SettleSeconds : 2.4f;

        private void Awake() => _doc = GetComponent<UIDocument>();

        private void OnEnable() => _screen = null;

        private void Update()
        {
            if (_screen == null)
            {
                if (_doc == null || _doc.rootVisualElement == null) return;
                _screen = GalleryScreens.Create(screen);
                _screen.Init(_doc.rootVisualElement);
                if (initialState > 0) _screen.SetState(initialState);
                return;
            }
            _screen.Tick(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        }
    }
}
