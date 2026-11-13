using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

namespace GameLike.Editor.Setup
{
    /// <summary>
    /// Gera um <see cref="FontAsset"/> dinamico de UI Toolkit a partir do TTF de icones
    /// (Font Awesome Solid) para a HUD usar glifos de icone nitidos. Salvo em Resources
    /// para o runtime carregar via <c>Resources.Load&lt;FontAsset&gt;("IconFont")</c>.
    /// Ferramenta de producao.
    /// </summary>
    public static class IconFontSetup
    {
        private const string Ttf = "Assets/UI/Fonts/fa-solid-900.ttf";
        private const string OutDir = "Assets/UI/Fonts/Resources";
        private const string OutPath = "Assets/UI/Fonts/Resources/IconFont.asset";

        [MenuItem("Tools/GameLike/UI Gallery/1 - Create Icon Font Asset")]
        public static void Create()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(Ttf);
            if (font == null)
            {
                Debug.LogError("[IconFont] TTF nao encontrado em " + Ttf);
                return;
            }

            var fa = FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            if (fa == null)
            {
                Debug.LogError("[IconFont] CreateFontAsset falhou.");
                return;
            }
            fa.name = "IconFont";

            Directory.CreateDirectory(OutDir);
            AssetDatabase.DeleteAsset(OutPath);
            AssetDatabase.CreateAsset(fa, OutPath);

            if (fa.material != null)
            {
                fa.material.name = "IconFont Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            if (fa.atlasTextures != null)
            {
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                {
                    var tex = fa.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = "IconFont Atlas " + i;
                    AssetDatabase.AddObjectToAsset(tex, fa);
                }
            }

            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[IconFont] FontAsset criado: " + OutPath);
        }
    }
}
