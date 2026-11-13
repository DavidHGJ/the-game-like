using GameLike.Editor.Setup;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Remonta a cena da galeria de UI e entra em play mode para o <c>GalleryCapture</c>
    /// (disparado por <c>-glgallery &lt;pasta&gt;</c>) salvar um PNG por tela e encerrar o editor.
    /// Rodar o editor SEM <c>-batchmode</c> (precisa de Game view real). Uso:
    /// <c>Unity -projectPath ... -executeMethod GameLike.Editor.CI.GalleryShot.Capture -glgallery C:\...\shots</c>
    /// </summary>
    public static class GalleryShot
    {
        private const string ScenePath = "Assets/Scenes/UIGallery.unity";

        public static void Capture()
        {
            HudGallerySetup.Build();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();

            // watchdog: se algo travar o play mode, força a saída.
            double deadline = EditorApplication.timeSinceStartup + 420.0;
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup > deadline)
                    EditorApplication.Exit(2);
            };
        }
    }
}
