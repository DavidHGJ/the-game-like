using GameLike.Editor.Office;
using GameLike.UI.MapBuilder;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Abre o Map Builder in-game. Pelo menu, entra em Play direto na ferramenta. Em modo de
    /// validação (<c>-executeMethod GameLike.Editor.CI.MapBuilderShot.Run -glmapshot &lt;pasta&gt;</c>,
    /// SEM <c>-batchmode</c>), o <c>MapBuilderAutoshot</c> dirige a ferramenta, tira prints e encerra.
    /// </summary>
    public static class MapBuilderShot
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Tools/GameLike/★ Map Builder in-game (Play)", priority = -1)]
        public static void OpenInPlay()
        {
            if (EditorApplication.isPlaying)
            {
                MapBuilderBoot.Open();
                return;
            }
            FurnitureCatalogSync.Sync();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            MapBuilderBoot.RequestOpenOnNextPlay();
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Entrada do modo de validação (linha de comando).</summary>
        public static void Run()
        {
            FurnitureCatalogSync.Sync();
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();

            double deadline = EditorApplication.timeSinceStartup + 180.0;
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup > deadline) EditorApplication.Exit(2);
            };
        }
    }
}
