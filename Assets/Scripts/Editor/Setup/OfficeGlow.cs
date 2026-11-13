using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Configura o <b>Bloom</b> (brilho/glow ao redor das superfícies acesas) do
    /// escritório: um VolumeProfile global com Bloom e o pós-processamento ligado
    /// na câmera. É o que faz as telas e luminárias "vazarem" luz em vez de serem
    /// retângulos chapados. Usado pela captura e pela cena de jogo.
    /// </summary>
    public static class OfficeGlow
    {
        private const string ProfilePath = "Assets/Settings/OfficeBloom.asset";

        public static VolumeProfile EnsureProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                Directory.CreateDirectory("Assets/Settings");
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            if (!profile.TryGet<Bloom>(out var bloom))
                bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.2f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(Color.white);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        /// <summary>
        /// Garante que o renderer 3D do URP tem o <c>PostProcessData</c> (sem ele o
        /// Bloom não roda). Copia o asset de dados usado pelo renderer 2D.
        /// </summary>
        public static void EnsurePostProcessData()
        {
            const string rendererPath = "Assets/Settings/Renderer3D.asset";
            var rd = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
            if (rd == null) return;
            var so = new SerializedObject(rd);
            var prop = so.FindProperty("m_PostProcessData");
            if (prop == null || prop.objectReferenceValue != null) return;

            string ppdPath = AssetDatabase.GUIDToAssetPath("41439944d30ece34e96484bdb6645b55");
            var ppd = !string.IsNullOrEmpty(ppdPath) ? AssetDatabase.LoadAssetAtPath<Object>(ppdPath) : null;
            if (ppd == null)
            {
                // Fallback: procura qualquer PostProcessData no projeto/pacote.
                foreach (var guid in AssetDatabase.FindAssets("t:PostProcessData"))
                {
                    ppd = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                    if (ppd != null) break;
                }
            }
            if (ppd != null)
            {
                prop.objectReferenceValue = ppd;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(rd);
                AssetDatabase.SaveAssets();
                Debug.Log("[OfficeGlow] PostProcessData atribuído ao Renderer3D.");
            }
        }

        /// <summary>
        /// Injeta o Bloom na cena GameOffice existente (abre, aplica na câmera
        /// principal, salva) sem reconstruir a cena — preserva managers.
        /// </summary>
        public static void AddBloomToGameScene()
        {
            const string scenePath = "Assets/Scenes/GameOffice.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) { cam = c; break; }
            }
            if (cam == null) { Debug.LogError("[OfficeGlow] Câmera não encontrada em GameOffice."); return; }
            ApplyBloom(cam);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[OfficeGlow] Bloom injetado em GameOffice.");
        }

        /// <summary>Liga o pós-processamento na câmera e garante um Volume global com Bloom.</summary>
        public static void ApplyBloom(Camera cam)
        {
            if (cam == null) return;
            EnsurePostProcessData();
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;

            var profile = EnsureProfile();
            var go = GameObject.Find("Global Volume (Bloom)");
            if (go == null) go = new GameObject("Global Volume (Bloom)");
            var vol = go.GetComponent<Volume>();
            if (vol == null) vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;
            vol.priority = 1f;
        }
    }
}
