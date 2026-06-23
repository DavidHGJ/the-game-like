#if UNITY_EDITOR
using System;
using GameLike.Data.Base;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Data
{
    /// <summary>
    /// Auto-assigns a stable unique Id to every <see cref="DefinitionAsset"/>
    /// right after it is created in the editor. Runs once per asset — once an
    /// Id is set, <see cref="DefinitionAsset.AssignIdIfEmpty"/> is a no-op, so
    /// the value never changes for the lifetime of the asset, regardless of
    /// renaming or moving the file.
    ///
    /// This is the only place in the codebase allowed to assign an Id.
    /// </summary>
    public sealed class DefinitionAssetIdAssigner : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;

                var asset = AssetDatabase.LoadAssetAtPath<DefinitionAsset>(path);
                if (asset == null) continue;
                if (!string.IsNullOrEmpty(asset.Id)) continue;

                asset.AssignIdIfEmpty(Guid.NewGuid().ToString("N"));
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);

                Debug.Log($"[DefinitionAssetIdAssigner] Assigned new Id to \"{path}\": {asset.Id}");
            }
        }
    }
}
#endif
