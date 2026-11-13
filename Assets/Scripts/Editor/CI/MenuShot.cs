using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Captura um screenshot do menu rodando NO EDITOR (play mode), sem precisar
    /// gerar um build standalone. Abre a MainMenu, entra em play mode e deixa o
    /// <c>StartupScreenshot</c> (disparado por <c>-glshot &lt;caminho&gt;</c>) capturar
    /// e encerrar o editor. Uso:
    /// <c>Unity -batchmode -executeMethod GameLike.Editor.CI.MenuShot.Capture -glshot C:\...\menu.png</c>
    /// (sem <c>-quit</c>; o StartupScreenshot chama EditorApplication.Exit ao terminar).
    /// </summary>
    public static class MenuShot
    {
        public static void Capture()
        {
            string scene = ArgValue("-glscene") ?? "Assets/Scenes/MainMenu.unity";
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();

            // watchdog: se algo travar o play mode, força a saída depois de 60s.
            double deadline = EditorApplication.timeSinceStartup + 60.0;
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    EditorApplication.Exit(2);
            };
        }

        private static string ArgValue(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }
    }
}
