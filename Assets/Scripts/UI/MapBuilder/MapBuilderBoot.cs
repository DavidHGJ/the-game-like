using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLike.UI.MapBuilder
{
    /// <summary>
    /// Porta de entrada do Map Builder in-game — a ferramenta de produção em que o dev monta
    /// os cenários (escritórios) vendo o resultado em 3D, com a mesma câmera e luz do jogo.
    /// Não precisa de cena própria: <see cref="Open"/> cria uma cena vazia em runtime, monta
    /// tudo por código e descarrega a cena anterior. Disponível só no editor e em development
    /// build (<see cref="IsAvailable"/>); o jogador final nunca vê.
    /// </summary>
    public static class MapBuilderBoot
    {
        /// <summary>Rótulo da opção no menu inicial.</summary>
        public const string MenuLabel = "Map Builder";

        private const string SceneName = "MapBuilder";
        private const string MenuScene = "MainMenu";
        private const string OpenOnPlayKey = "GameLike.MapBuilder.OpenOnPlay";
        private const string AutoshotArg = "-glmapshot";

        /// <summary>true no editor e em development build.</summary>
        public static bool IsAvailable => GameLike.Core.DevTools.Enabled;

        /// <summary>Abre o Map Builder, saindo da cena atual.</summary>
        public static void Open()
        {
            if (!IsAvailable || Object.FindFirstObjectByType<MapBuilderController>() != null) return;

            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.CreateScene(SceneName);
            SceneManager.SetActiveScene(scene);
            new GameObject("[MapBuilder]").AddComponent<MapBuilderController>();
            if (previous.IsValid() && previous.isLoaded) SceneManager.UnloadSceneAsync(previous);
        }

        /// <summary>Fecha o Map Builder e volta ao menu inicial (ou encerra o Play se não houver menu).</summary>
        public static void Close()
        {
            if (Application.CanStreamedLevelBeLoaded(MenuScene))
            {
                SceneManager.LoadScene(MenuScene, LoadSceneMode.Single);
                return;
            }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Pasta pedida por <c>-glmapshot &lt;pasta&gt;</c> (roteiro de captura de validação), ou null.</summary>
        public static string AutoshotFolder()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == AutoshotArg) return args[i + 1];
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OpenIfRequested()
        {
            bool requested = AutoshotFolder() != null;
#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(OpenOnPlayKey, false))
            {
                UnityEditor.SessionState.EraseBool(OpenOnPlayKey);
                requested = true;
            }
#endif
            if (requested) Open();
        }

#if UNITY_EDITOR
        /// <summary>Pede que o próximo Play abra direto no Map Builder (usado pelo menu do editor).</summary>
        public static void RequestOpenOnNextPlay() => UnityEditor.SessionState.SetBool(OpenOnPlayKey, true);
#endif
    }
}
