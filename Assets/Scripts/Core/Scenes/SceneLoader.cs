using System.Collections;
using GameLike.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLike.Core.Scenes
{
    /// <summary>
    /// Single choke point for switching the active content scene
    /// (MainMenu, Gameplay, SkillTree, ...) while the Boot scene and its
    /// persistent managers stay alive for the entire application lifetime.
    ///
    /// Content scenes are loaded additively and the previous one is unloaded
    /// after the new one finishes loading, avoiding a black frame between them.
    /// Lives in GameLike.Core so any layer can request a scene switch without
    /// depending on Systems.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        private static SceneLoader _instance;

        private string _currentContentScene;
        private bool _isTransitioning;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        /// <summary>
        /// Loads <paramref name="sceneName"/> additively and unloads the
        /// previously active content scene, if any. Ignored if a transition
        /// is already in progress or the requested scene is already active.
        /// </summary>
        public void SwitchTo(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] SwitchTo called with an empty scene name.");
                return;
            }

            if (_isTransitioning)
            {
                Debug.LogWarning($"[SceneLoader] Ignored SwitchTo(\"{sceneName}\") — a transition is already running.");
                return;
            }

            if (_currentContentScene == sceneName)
            {
                Debug.LogWarning($"[SceneLoader] Scene \"{sceneName}\" is already active.");
                return;
            }

            StartCoroutine(SwitchRoutine(sceneName));
        }

        private IEnumerator SwitchRoutine(string sceneName)
        {
            _isTransitioning = true;

            var previousScene = _currentContentScene;

            EventBus.Publish(new SceneLoadStartedEvent(sceneName));

            var loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogError($"[SceneLoader] Scene \"{sceneName}\" could not be found. Check Build Settings.");
                _isTransitioning = false;
                yield break;
            }

            yield return loadOperation;

            var loadedScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(loadedScene);
            _currentContentScene = sceneName;

            EventBus.Publish(new SceneLoadCompletedEvent(sceneName));

            if (!string.IsNullOrEmpty(previousScene))
            {
                yield return SceneManager.UnloadSceneAsync(previousScene);
                EventBus.Publish(new SceneUnloadedEvent(previousScene));
            }

            _isTransitioning = false;
        }
    }
}
