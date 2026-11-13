using System;
using System.IO;
using GameLike.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Content
{
    /// <summary>
    /// Lê os JSONs de temas (forma <see cref="ThemeContentDto"/>) e cria/atualiza os
    /// <see cref="ThemeSO"/> correspondentes, casando pelo <c>key</c> (slug) e
    /// preservando o Id estável. Mesmo molde do <see cref="GenreContentImporter"/>.
    /// Nunca edite um .asset à mão — o fluxo é JSON → este menu → asset.
    /// </summary>
    public static class ThemeContentImporter
    {
        private const string SourceFolder = "Assets/Content/Themes";
        private const string TargetFolder = "Assets/Resources/Themes";

        [MenuItem("Tools/GameLike/Import Content/Themes")]
        public static void ImportThemes()
        {
            if (!Directory.Exists(SourceFolder))
            {
                Debug.LogWarning($"[ThemeContentImporter] Source folder not found: {SourceFolder}");
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
                    var dto = JsonUtility.FromJson<ThemeContentDto>(File.ReadAllText(filePath));
                    if (string.IsNullOrWhiteSpace(dto.key))
                    {
                        Debug.LogError($"[ThemeContentImporter] \"{filePath}\" missing \"key\". Skipped.");
                        failed++;
                        continue;
                    }

                    var assetPath = $"{TargetFolder}/{dto.key}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<ThemeSO>(assetPath);
                    if (existing != null)
                    {
                        ApplyDto(existing, dto);
                        EditorUtility.SetDirty(existing);
                        updated++;
                    }
                    else
                    {
                        var asset = ScriptableObject.CreateInstance<ThemeSO>();
                        ApplyDto(asset, dto);
                        AssetDatabase.CreateAsset(asset, assetPath);
                        created++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ThemeContentImporter] Failed to import \"{filePath}\": {ex.Message}");
                    failed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ThemeContentImporter] Done. Created: {created}, Updated: {updated}, Failed: {failed}.");
        }

        private static void ApplyDto(ThemeSO asset, ThemeContentDto dto)
        {
            var so = new SerializedObject(asset);

            // Nome amigável plano (a UI usa enquanto a ponte com o Localization Package não existe).
            var displayNameProp = so.FindProperty("displayNamePt");
            if (displayNameProp != null) displayNameProp.stringValue = dto.displayNamePtBr;

            if (Enum.TryParse<ThemeCategory>(dto.category, true, out var category))
                so.FindProperty("category").enumValueIndex = (int)category;

            var listProp = so.FindProperty("genreCompatibilities");
            listProp.ClearArray();
            if (dto.compatibilities != null)
            {
                for (int i = 0; i < dto.compatibilities.Length; i++)
                {
                    var c = dto.compatibilities[i];
                    listProp.InsertArrayElementAtIndex(i);
                    var element = listProp.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("genreKey").stringValue = c.genre;
                    var weight = WeightScaleParser.Parse(c.weight);
                    element.FindPropertyRelative("weight").enumValueIndex = (int)weight - (int)WeightLevel.StronglyNegative;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
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
