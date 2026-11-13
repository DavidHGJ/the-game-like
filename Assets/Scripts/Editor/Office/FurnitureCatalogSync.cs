using System.Collections.Generic;
using System.IO;
using GameLike.Data.LevelBuilder;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Office
{
    /// <summary>
    /// Mantém o <see cref="FurnitureCatalogSO"/> (Resources/Office/FurnitureCatalog) em dia com
    /// todos os <see cref="FurnitureDef3DSO"/> de <c>Assets/Data/Furniture3D</c>. Roda sozinho
    /// quando o editor carrega e sempre que um móvel é criado, movido ou apagado — é o que
    /// permite ao Map Builder in-game listar a mobília sem depender do AssetDatabase.
    /// </summary>
    public sealed class FurnitureCatalogSync : AssetPostprocessor
    {
        private const string DefFolder = "Assets/Data/Furniture3D";
        private const string CatalogPath = "Assets/Resources/" + FurnitureCatalogSO.ResourcePath + ".asset";
        private const string CharacterPath = "Assets/Art/Characters/Dev/Dev.fbx";

        [InitializeOnLoadMethod]
        private static void OnEditorLoad() => EditorApplication.delayCall += () => Sync();

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom))
                EditorApplication.delayCall += () => Sync();
        }

        private static bool Touches(string[] paths)
        {
            foreach (string path in paths)
                if (path.StartsWith(DefFolder, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Cria/atualiza o catálogo. Só grava se a lista mudou. Devolve o catálogo.</summary>
        [MenuItem("Tools/GameLike/Office 3D/8 - Sincronizar catálogo de mobília")]
        public static FurnitureCatalogSO Sync()
        {
            if (!AssetDatabase.IsValidFolder(DefFolder)) return null;

            var defs = new List<FurnitureDef3DSO>();
            foreach (string guid in AssetDatabase.FindAssets("t:FurnitureDef3DSO", new[] { DefFolder }))
            {
                var def = AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null) defs.Add(def);
            }
            defs.Sort((a, b) => a.Category != b.Category
                ? a.Category.CompareTo(b.Category)
                : string.Compare(a.EditorName, b.EditorName, System.StringComparison.OrdinalIgnoreCase));
            var character = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);

            var catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
                catalog = ScriptableObject.CreateInstance<FurnitureCatalogSO>();
                catalog.SetContents(defs, character);
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                AssetDatabase.SaveAssets();
                return catalog;
            }

            if (SameContents(catalog, defs, character)) return catalog;
            catalog.SetContents(defs, character);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        private static bool SameContents(FurnitureCatalogSO catalog, List<FurnitureDef3DSO> defs, GameObject character)
        {
            if (catalog.DefaultCharacter != character || catalog.Furniture.Count != defs.Count) return false;
            for (int i = 0; i < defs.Count; i++)
                if (catalog.Furniture[i] != defs[i]) return false;
            return true;
        }
    }
}
