using System;
using System.IO;
using GameLike.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Content
{
    /// <summary>
    /// Lê os JSONs de consoles (forma <see cref="ConsoleContentDto"/>) e cria/atualiza
    /// os <see cref="ConsoleSO"/> correspondentes, casando pelo <c>key</c> (slug) e
    /// preservando o Id estável. Mesmo molde do <see cref="GenreContentImporter"/>.
    /// Grava em <c>Resources/Consoles</c> para o runtime do slice carregar via
    /// <c>Resources.LoadAll&lt;ConsoleSO&gt;("Consoles")</c>. Nunca edite um .asset à mão.
    /// </summary>
    public static class ConsoleContentImporter
    {
        private const string SourceFolder = "Assets/Content/Consoles";
        private const string TargetFolder = "Assets/Resources/Consoles";

        [MenuItem("Tools/GameLike/Import Content/Consoles")]
        public static void ImportConsoles()
        {
            if (!Directory.Exists(SourceFolder))
            {
                Debug.LogWarning($"[ConsoleContentImporter] Source folder not found: {SourceFolder}");
                return;
            }

            EnsureFolderExists(TargetFolder);

            var jsonFiles = Directory.GetFiles(SourceFolder, "*.json", SearchOption.TopDirectoryOnly);
            int created = 0, updated = 0, failed = 0;

            foreach (var filePath in jsonFiles)
            {
                if (filePath.EndsWith(".example.json", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    var dto = JsonUtility.FromJson<ConsoleContentDto>(File.ReadAllText(filePath));
                    if (string.IsNullOrWhiteSpace(dto.key))
                    {
                        Debug.LogError($"[ConsoleContentImporter] \"{filePath}\" missing \"key\". Skipped.");
                        failed++;
                        continue;
                    }

                    var assetPath = $"{TargetFolder}/{dto.key}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<ConsoleSO>(assetPath);
                    if (existing != null)
                    {
                        ApplyDto(existing, dto);
                        EditorUtility.SetDirty(existing);
                        updated++;
                    }
                    else
                    {
                        var asset = ScriptableObject.CreateInstance<ConsoleSO>();
                        ApplyDto(asset, dto);
                        AssetDatabase.CreateAsset(asset, assetPath);
                        created++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ConsoleContentImporter] Failed to import \"{filePath}\": {ex.Message}");
                    failed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ConsoleContentImporter] Done. Created: {created}, Updated: {updated}, Failed: {failed}.");
        }

        private static void ApplyDto(ConsoleSO asset, ConsoleContentDto dto)
        {
            var so = new SerializedObject(asset);

            var displayNameProp = so.FindProperty("displayNamePt");
            if (displayNameProp != null) displayNameProp.stringValue = dto.displayNamePtBr;

            SetString(so, "manufacturer", dto.manufacturer);

            if (!string.IsNullOrWhiteSpace(dto.category) &&
                Enum.TryParse<PlatformCategory>(dto.category, true, out var category))
            {
                var catProp = so.FindProperty("category");
                if (catProp != null) catProp.enumValueIndex = (int)category;
            }

            SetInt(so, "releaseYear", dto.releaseYear);
            SetLong(so, "licenseCost", dto.licenseCost);
            SetFloat(so, "initialPopularity", Mathf.Clamp01(dto.initialPopularity));
            SetFloat(so, "peakPopularity", Mathf.Clamp01(dto.peakPopularity));
            SetInt(so, "lifespanYears", Mathf.Max(1, dto.lifespanYears));

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(SerializedObject so, string field, string value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.stringValue = value ?? string.Empty;
        }

        private static void SetInt(SerializedObject so, string field, int value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.intValue = value;
        }

        private static void SetLong(SerializedObject so, string field, long value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.longValue = value;
        }

        private static void SetFloat(SerializedObject so, string field, float value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.floatValue = value;
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
