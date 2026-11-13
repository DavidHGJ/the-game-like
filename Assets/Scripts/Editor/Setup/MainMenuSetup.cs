using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// (Re)cria a cena <c>MainMenu</c> (GDD 20.1): só uma câmera de fundo. O menu em si não
    /// fica na cena — o <c>MainMenuLive</c> nasce em runtime quando ela carrega. Também deixa
    /// o Build Settings só com as duas cenas do jogo: MainMenu (primeira) e GameOffice.
    /// </summary>
    public static class MainMenuSetup
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/Scenes/GameOffice.unity";

        [MenuItem("Tools/GameLike/Menu/Build Main Menu")]
        public static void Build()
        {
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.84f, 0.85f, 0.94f);
            camGo.AddComponent<AudioListener>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
            Debug.Log("[MainMenu] Cena MainMenu recriada; Build Settings = MainMenu + GameOffice.");
        }

        /// <summary>Entrada para o modo batch (CI).</summary>
        public static void BatchBuild()
        {
            Build();
            Debug.Log("GAMELIKE_MAINMENU_OK");
            EditorApplication.Exit(0);
        }
    }
}
