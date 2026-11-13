using GameLike.Editor.Content;
using GameLike.Editor.Setup;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Executa, em batch mode, os passos de setup de dados que normalmente rodam por
    /// menu no editor: cria os <c>BalanceAsset</c> faltantes e importa o conteúdo JSON
    /// (temas). Uso: <c>-executeMethod GameLike.Editor.CI.BatchSetup.Run</c>.
    /// </summary>
    public static class BatchSetup
    {
        public static void Run()
        {
            BalanceAssetsSetup.CreateMissing();
            ThemeContentImporter.ImportThemes();
            NarrativeEventContentImporter.ImportEvents();
            GenreContentImporter.ImportGenresToResources();
            ConsoleContentImporter.ImportConsoles();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GAMELIKE_SETUP_OK");
            EditorApplication.Exit(0);
        }

        /// <summary>Garante a cena de jogo (managers + HUD) em batch.</summary>
        public static void EnsureScene()
        {
            GameSceneEnsure.Ensure();
            Debug.Log("GAMELIKE_SCENE_OK");
            EditorApplication.Exit(0);
        }
    }
}
