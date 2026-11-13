using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Troca o pipeline URP do <b>Renderer 2D</b> para um <b>Universal Renderer 3D</b>
    /// (forward), sem recriar o projeto. O jogo é isométrico 3D, então o renderer
    /// 3D é o correto — habilita iluminação/sombra 3D de verdade, que o Renderer 2D
    /// não faz. Cria o asset do renderer 3D se faltar e o aponta como default no
    /// <c>UniversalRP.asset</c>. Reversível.
    /// </summary>
    public static class Switch3DRenderer
    {
        private const string PipelinePath = "Assets/Settings/UniversalRP.asset";
        private const string RendererPath = "Assets/Settings/Renderer3D.asset";

        [MenuItem("Tools/GameLike/Setup/Switch to 3D Renderer (URP)")]
        public static void SwitchTo3D()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PipelinePath);
            if (pipeline == null)
            {
                Debug.LogError("[3D Renderer] UniversalRP.asset não encontrado em " + PipelinePath);
                return;
            }

            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererPath);
            if (renderer == null) renderer = CreateUniversalRenderer3D();
            if (renderer == null)
            {
                Debug.LogError("[3D Renderer] Não foi possível criar o Universal Renderer 3D. " +
                               "Crie manualmente: Project → Create → Rendering → URP Universal Renderer, " +
                               "salve como Renderer3D.asset e rode este menu de novo.");
                return;
            }

            var so = new SerializedObject(pipeline);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null)
            {
                Debug.LogError("[3D Renderer] Propriedade m_RendererDataList não encontrada no pipeline.");
                return;
            }

            list.arraySize = Mathf.Max(1, list.arraySize);
            list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;

            var defaultIdx = so.FindProperty("m_DefaultRendererIndex");
            if (defaultIdx != null) defaultIdx.intValue = 0;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[3D Renderer] Pipeline agora usa o Universal Renderer 3D (" + RendererPath +
                      "). Reabra a cena/entre em Play para ver a iluminação 3D.");
        }

        private static ScriptableObject CreateUniversalRenderer3D()
        {
            // UniversalRendererData vive na assembly de runtime da URP; resolvemos por nome
            // para não exigir referência de assembly no asmdef do editor.
            var type = FindType("UnityEngine.Rendering.Universal.UniversalRendererData");
            if (type == null)
            {
                Debug.LogError("[3D Renderer] Tipo UniversalRendererData não encontrado (URP instalada?).");
                return null;
            }

            var data = ScriptableObject.CreateInstance(type);
            if (data == null) return null;
            data.name = "Renderer3D";

            var dir = Path.GetDirectoryName(RendererPath);
            if (!AssetDatabase.IsValidFolder(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(data, RendererPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(RendererPath);
            return AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererPath);
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
