using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Monta a cena 3D do escritório da fase <b>Garage</b> (GDD 3.4 — "espaço
    /// minúsculo e caótico, mesa improvisada, pôsteres na parede") usando os
    /// prefabs/FBX low-poly gerados em <c>Assets/Art/Furniture</c>, com câmera
    /// ortográfica isométrica e luz direcional (ART_STYLE.md). Substitui a
    /// <c>IsoDemo</c> de primitivas por uma cena com os móveis reais.
    ///
    /// É uma ferramenta de autoria sua (roda no editor, uma vez por fase), não
    /// uma mecânica de runtime. Rodar de novo recria a cena do zero.
    /// </summary>
    public static class GarageOfficeBuilder
    {
        private const string FurnitureFolder = "Assets/Art/Furniture";
        private const string MaterialsFolder = "Assets/Art/Materials";
        private const string ScenePath = "Assets/Scenes/GarageOffice.unity";

        [MenuItem("Tools/GameLike/Setup/5 - Build 3D Garage Office")]
        public static void BuildGarage()
        {
            EnsureFolder(MaterialsFolder);
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetupCamera();
            SetupLight();

            var root = new GameObject("GarageOffice");

            var floorMat = MakeMaterial("Garage_Floor", new Color(0.40f, 0.36f, 0.33f));
            var wallMat = MakeMaterial("Garage_Wall", new Color(0.78f, 0.74f, 0.66f));

            // Chão (Plane = 10u; escala 0.7 → 7x7u, garagem pequena).
            Primitive(PrimitiveType.Plane, "Floor", root, floorMat,
                new Vector3(0, 0, 0), new Vector3(0.7f, 1, 0.7f));

            // Duas paredes baixas ao fundo (cantos lendo o volume iso).
            Primitive(PrimitiveType.Cube, "Wall_Back", root, wallMat,
                new Vector3(0, 1.4f, -3.3f), new Vector3(7f, 2.8f, 0.2f));
            Primitive(PrimitiveType.Cube, "Wall_Left", root, wallMat,
                new Vector3(-3.3f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 7f));

            // Layout caótico de garagem: mesa improvisada + monitor + cadeira,
            // caixas empilhadas num canto, pôster na parede, tapete e plantinha.
            Place(root, "Rug", new Vector3(-0.3f, 0f, 0.2f), 0f);
            Place(root, "Desk", new Vector3(-0.6f, 0f, -1.1f), 0f);
            Place(root, "Monitor", new Vector3(-0.6f, 0.78f, -1.3f), 0f);
            Place(root, "OfficeChair", new Vector3(-0.6f, 0f, 0.1f), 180f);
            Place(root, "CardboardBox", new Vector3(2.0f, 0f, -2.4f), 20f);
            Place(root, "CardboardBox", new Vector3(2.3f, 0.47f, -2.5f), -10f);
            Place(root, "CardboardBox", new Vector3(1.4f, 0f, -2.6f), 50f);
            Place(root, "Poster", new Vector3(0.9f, 0f, -3.18f), 0f);
            Place(root, "Poster", new Vector3(-3.18f, 0f, -0.6f), 90f);
            Place(root, "Plant", new Vector3(2.4f, 0f, 1.8f), 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Debug.Log("[GameLike Setup] Cena 3D da Garage criada: " + ScenePath + " — abra e veja na Game view.");
        }

        private static void Place(GameObject root, string pieceName, Vector3 position, float yaw)
        {
            var path = $"{FurnitureFolder}/{pieceName}/{pieceName}.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                Debug.LogWarning($"[GarageOfficeBuilder] FBX não encontrado: {path}. " +
                                 "Rode o gerador de mobília (Tools/Blender/gen_furniture.py) primeiro.");
                return;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            inst.transform.SetParent(root.transform, false);
            inst.transform.localPosition = position;
            inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
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

        private static GameObject Primitive(PrimitiveType type, string name, GameObject parent,
            Material material, Vector3 position, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null && material != null) r.sharedMaterial = material;
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
