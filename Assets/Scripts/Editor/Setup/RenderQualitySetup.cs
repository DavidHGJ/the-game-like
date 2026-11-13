using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Liga, nos assets do URP, o que a atmosfera do escritório precisa e que não dá para ligar
    /// em runtime: sombras suaves em alta resolução, sombra das luzes dos abajures, MSAA,
    /// reflexo com projeção em caixa, o <c>PostProcessData</c> do renderer 3D (sem ele bloom e
    /// tonemapping simplesmente não rodam) e a oclusão de ambiente (SSAO) — a sombra de contato
    /// que "assenta" os móveis no chão. Idempotente.
    /// </summary>
    public static class RenderQualitySetup
    {
        private const string PipelinePath = "Assets/Settings/UniversalRP.asset";
        private const string RendererPath = "Assets/Settings/Renderer3D.asset";
        private const string SsaoType = "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion";
        private const string SsaoName = "ScreenSpaceAmbientOcclusion";

        [MenuItem("Tools/GameLike/Setup/★ Qualidade visual (sombras, SSAO, pós-processamento)")]
        public static void Apply()
        {
            ConfigurePipeline();
            OfficeGlow.EnsurePostProcessData();
            EnsureAmbientOcclusion();
            AssetDatabase.SaveAssets();
            Debug.Log("[RenderQuality] Pipeline e renderer 3D configurados.");
        }

        /// <summary>Entrada para o modo batch (CI).</summary>
        public static void BatchApply()
        {
            Apply();
            Debug.Log("GAMELIKE_RENDERQUALITY_OK");
            EditorApplication.Exit(0);
        }

        private static void ConfigurePipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                Debug.LogError("[RenderQuality] Pipeline não encontrado em " + PipelinePath);
                return;
            }
            var so = new SerializedObject(pipeline);
            Set(so, "m_SupportsHDR", true);
            Set(so, "m_MSAA", 4);
            Set(so, "m_MainLightShadowsSupported", true);
            Set(so, "m_MainLightShadowmapResolution", 4096);
            Set(so, "m_AdditionalLightsRenderingMode", 1);
            Set(so, "m_AdditionalLightsPerObjectLimit", 8);
            Set(so, "m_AdditionalLightShadowsSupported", true);
            Set(so, "m_AdditionalLightsShadowmapResolution", 2048);
            Set(so, "m_SoftShadowsSupported", true);
            Set(so, "m_SoftShadowQuality", 3);
            Set(so, "m_ShadowCascadeCount", 1);
            Set(so, "m_ReflectionProbeBlending", true);
            Set(so, "m_ReflectionProbeBoxProjection", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        private static void Set(SerializedObject so, string property, bool value)
        {
            var p = so.FindProperty(property);
            if (p == null) { Debug.LogWarning("[RenderQuality] Propriedade ausente: " + property); return; }
            if (p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value;
            else p.intValue = value ? 1 : 0;
        }

        private static void Set(SerializedObject so, string property, int value)
        {
            var p = so.FindProperty(property);
            if (p == null) { Debug.LogWarning("[RenderQuality] Propriedade ausente: " + property); return; }
            p.intValue = value;
        }

        private static void EnsureAmbientOcclusion()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            if (renderer == null)
            {
                Debug.LogError("[RenderQuality] Renderer 3D não encontrado em " + RendererPath);
                return;
            }
            Type type = typeof(UniversalRendererData).Assembly.GetType(SsaoType);
            if (type == null)
            {
                Debug.LogWarning("[RenderQuality] Tipo do SSAO não encontrado nesta versão do URP.");
                return;
            }

            ScriptableRendererFeature feature = renderer.rendererFeatures.Find(f => f != null && f.GetType() == type);
            if (feature == null)
            {
                feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
                feature.name = SsaoName;
                AssetDatabase.AddObjectToAsset(feature, renderer);
                renderer.rendererFeatures.Add(feature);

                // O renderer guarda, em paralelo, o id local de cada feature.
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
                var rendererSo = new SerializedObject(renderer);
                rendererSo.Update();
                var map = rendererSo.FindProperty("m_RendererFeatureMap");
                if (map != null)
                {
                    map.arraySize = renderer.rendererFeatures.Count;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    rendererSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var so = new SerializedObject(feature);
            SetFloat(so, "m_Settings.Intensity", 2.2f);
            SetFloat(so, "m_Settings.Radius", 0.32f);
            SetFloat(so, "m_Settings.DirectLightingStrength", 0.45f);
            SetFloat(so, "m_Settings.Falloff", 100f);
            so.ApplyModifiedPropertiesWithoutUndo();
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);
            renderer.SetDirty();
            EditorUtility.SetDirty(renderer);
        }

        private static void SetFloat(SerializedObject so, string property, float value)
        {
            var p = so.FindProperty(property);
            if (p != null) p.floatValue = value;
        }
    }
}
