using GameLike.Systems.Core;
using GameLike.Systems.Economy;
using GameLike.Systems.Events;
using GameLike.Systems.Market;
using GameLike.Systems.Marketing;
using GameLike.Systems.Office;
using GameLike.Systems.Projects;
using GameLike.Systems.Staff;
using GameLike.Systems.Time;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Garante, de forma não-destrutiva, que a cena de jogo <c>GameOffice</c> tenha
    /// todos os managers (incluindo os novos: Staff, Marketing, Market, Event), o
    /// <see cref="OfficeView"/> e a câmera iso. A UI do jogo não fica na cena: nasce em runtime (GameUi).
    /// Preserva o resto da cena. Ferramenta de produção; roda por menu ou em batch.
    /// </summary>
    public static class GameSceneEnsure
    {
        private const string ScenePath = "Assets/Scenes/GameOffice.unity";

        [MenuItem("Tools/GameLike/Office 3D/★ Ensure Game Scene (managers)")]
        public static void Ensure()
        {
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var managers = GameObject.Find("[GameLike]") ?? new GameObject("[GameLike]");
            EnsureComponent<TimeManager>(managers);
            EnsureComponent<EconomyManager>(managers);
            EnsureComponent<ProjectManager>(managers);
            EnsureComponent<StaffManager>(managers);
            EnsureComponent<MarketingManager>(managers);
            EnsureComponent<MarketManager>(managers);
            EnsureComponent<EventManager>(managers);
            EnsureComponent<GameLike.Systems.Research.ResearchManager>(managers);
            EnsureComponent<GameController>(managers);

            if (GameObject.Find("Office") == null)
                EnsureComponent<OfficeView>(new GameObject("Office"));

            EnsureCamera();
            OfficeLighting.Apply();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameSceneEnsure] Cena GameOffice garantida com todos os managers.");
        }

        private static void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            if (!cam.orthographic)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.93f, 0.89f, 0.80f);
                cam.transform.rotation = Quaternion.Euler(30f, 225f, 0f);
                cam.transform.position = -cam.transform.forward * 40f;
            }
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }
    }
}
