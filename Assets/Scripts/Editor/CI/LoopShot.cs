using UnityEditor;
using UnityEditor.SceneManagement;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Abre a cena de gameplay e entra em play mode para o <c>LoopAutoplay</c> (disparado por
    /// <c>-glplay &lt;pasta&gt;</c>) jogar o loop completo tirando prints. Rodar o editor SEM
    /// <c>-batchmode</c> (precisa de Game view real). Uso:
    /// <c>Unity -projectPath ... -executeMethod GameLike.Editor.CI.LoopShot.Run -glplay C:\...\shots</c>
    /// </summary>
    public static class LoopShot
    {
        private const string ScenePath = "Assets/Scenes/GameOffice.unity";

        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();

            // watchdog: se algo travar o play mode, força a saída.
            double deadline = EditorApplication.timeSinceStartup + 540.0;
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    EditorApplication.Exit(2);
            };
        }
    }
}
