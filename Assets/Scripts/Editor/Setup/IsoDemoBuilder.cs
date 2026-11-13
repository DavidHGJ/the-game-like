using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Monta uma cena de demonstração da direção visual 3D isométrica low-poly
    /// (ART_STYLE.md): câmera ortográfica em ângulo iso clássico (30°/45°),
    /// iluminação direcional, e um mini-escritório feito de primitivas com
    /// materiais de cor chapada. <b>PROVISÓRIO</b> — é só um alvo visual para
    /// calibrar a câmera e o look; não é o escritório final nem usa o LevelBuilder.
    /// </summary>
    public static class IsoDemoBuilder
    {
        private const string MaterialsFolder = "Assets/Art/Materials";
        private const string ScenePath = "Assets/Scenes/IsoDemo.unity";

        [MenuItem("Tools/GameLike/Setup/4 - Build 3D Iso Demo")]
        public static void BuildIsoDemo()
        {
            EnsureFolder(MaterialsFolder);

            var floorMat = MakeMaterial("Iso_Floor", new Color(0.44f, 0.54f, 0.50f));
            var woodMat = MakeMaterial("Iso_Wood", new Color(0.72f, 0.45f, 0.25f));
            var darkMat = MakeMaterial("Iso_Dark", new Color(0.14f, 0.15f, 0.18f));
            var creamMat = MakeMaterial("Iso_Cream", new Color(0.90f, 0.87f, 0.78f));
            var plantMat = MakeMaterial("Iso_Plant", new Color(0.26f, 0.52f, 0.30f));
            var rugMat = MakeMaterial("Iso_Rug", new Color(0.30f, 0.38f, 0.43f));

            var devMats = new[]
            {
                MakeMaterial("Iso_Dev1", new Color(0.17f, 0.45f, 0.52f)),
                MakeMaterial("Iso_Dev2", new Color(0.62f, 0.32f, 0.36f)),
                MakeMaterial("Iso_Dev3", new Color(0.36f, 0.46f, 0.26f)),
                MakeMaterial("Iso_Dev4", new Color(0.47f, 0.42f, 0.62f)),
                MakeMaterial("Iso_Dev5", new Color(0.80f, 0.60f, 0.28f)),
                MakeMaterial("Iso_Dev6", new Color(0.30f, 0.55f, 0.58f)),
            };

            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetupCamera();
            SetupLight();

            var root = new GameObject("Office");

            // Chão (Plane = 10u; escala 2 → 20x20u).
            var floor = CreatePrimitive(PrimitiveType.Plane, "Floor", root, floorMat,
                new Vector3(0f, 0f, 0f), new Vector3(2f, 1f, 2f));

            // Tapete de acento sob a área das estações.
            CreatePrimitive(PrimitiveType.Cube, "Rug", root, rugMat,
                new Vector3(0f, 0.02f, 0.5f), new Vector3(13.5f, 0.04f, 9f));

            // Paredes baixas em duas bordas (cantos ao fundo, lendo o volume iso).
            CreatePrimitive(PrimitiveType.Cube, "Wall_Back", root, creamMat,
                new Vector3(0f, 1.5f, -9f), new Vector3(18f, 3f, 0.3f));
            CreatePrimitive(PrimitiveType.Cube, "Wall_Left", root, creamMat,
                new Vector3(-9f, 1.5f, 0f), new Vector3(0.3f, 3f, 18f));

            // Estações de trabalho em grade: mesa + monitor + "dev" + plantinha.
            float[] xs = { -4.5f, 0f, 4.5f };
            float[] zs = { -2.5f, 3f };
            int i = 0;
            foreach (var z in zs)
            foreach (var x in xs)
            {
                int idx = i++;
                var station = new GameObject($"Station_{idx}");
                station.transform.SetParent(root.transform, false);

                CreatePrimitive(PrimitiveType.Cube, "Desk", station, woodMat,
                    new Vector3(x, 0.45f, z), new Vector3(2.4f, 0.9f, 1.1f));
                CreatePrimitive(PrimitiveType.Cube, "Monitor", station, darkMat,
                    new Vector3(x, 1.25f, z - 0.35f), new Vector3(0.95f, 0.6f, 0.1f));
                CreatePrimitive(PrimitiveType.Capsule, "Dev", station, devMats[idx % devMats.Length],
                    new Vector3(x, 0.9f, z + 0.9f), new Vector3(0.6f, 0.6f, 0.6f));
                CreatePrimitive(PrimitiveType.Cylinder, "Plant", station, plantMat,
                    new Vector3(x + 1.0f, 0.5f, z), new Vector3(0.35f, 0.5f, 0.35f));
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Debug.Log("[GameLike Setup] Cena 3D iso de demonstração criada: " + ScenePath +
                      " — abra e veja na Scene/Game view.");
        }

        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.17f);
            cam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
            cam.transform.position = -cam.transform.forward * 40f;
        }

        private static void SetupLight()
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                light.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
                light.intensity = 1.15f;
                light.shadows = LightShadows.Soft;
            }
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, GameObject parent,
            Material material, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            return go;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.08f);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
                if (s.path == path) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
