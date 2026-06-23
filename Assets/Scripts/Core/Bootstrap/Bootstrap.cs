using GameLike.Core.Scenes;
using UnityEngine;

namespace GameLike.Core.Bootstrap
{
    /// <summary>
    /// Entry point of the persistent Boot scene. Boot holds every
    /// application-lifetime manager (TimeManager, EconomyManager, SceneLoader,
    /// and future ones such as SaveManager) marked to survive scene loads, then
    /// hands off to <see cref="SceneLoader"/> to load the first content scene
    /// additively (MainMenu by default).
    ///
    /// The Boot scene itself is never unloaded: only content scenes
    /// (MainMenu, Gameplay, SkillTree, ...) load and unload on top of it.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [Header("Cena inicial")]
        [Tooltip("Nome exato da cena de conteúdo a carregar primeiro. Deve estar em Build Settings.")]
        [SerializeField] private string firstContentScene = "MainMenu";

        [Header("Serviços persistentes")]
        [Tooltip("Raiz que contém todos os managers que devem sobreviver à troca de cenas.")]
        [SerializeField] private GameObject persistentServicesRoot;

        private SceneLoader _sceneLoader;

        private void Awake()
        {
            if (persistentServicesRoot != null)
            {
                DontDestroyOnLoad(persistentServicesRoot);
            }

            _sceneLoader = GetComponentInChildren<SceneLoader>();
            if (_sceneLoader == null)
            {
                Debug.LogError("[Bootstrap] Nenhum SceneLoader encontrado em Boot. " +
                               "Adicione um GameObject com SceneLoader dentro de persistentServicesRoot.");
            }
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(firstContentScene))
            {
                Debug.LogError("[Bootstrap] firstContentScene não configurada no Inspector.");
                return;
            }

            _sceneLoader?.SwitchTo(firstContentScene);
        }
    }
}
