using System;
using System.IO;
using GameLike.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Content
{
    /// <summary>
    /// Lê os JSONs de eventos narrativos (forma <see cref="NarrativeEventContentDto"/>)
    /// e cria/atualiza os <see cref="NarrativeEventSO"/> em Resources/Events, casando
    /// pelo <c>key</c> e preservando o Id estável. Nunca edite um .asset à mão.
    /// </summary>
    public static class NarrativeEventContentImporter
    {
        private const string SourceFolder = "Assets/Content/Events";
        private const string TargetFolder = "Assets/Resources/Events";

        [MenuItem("Tools/GameLike/Import Content/Narrative Events")]
        public static void ImportEvents()
        {
            if (!Directory.Exists(SourceFolder))
            {
                Debug.LogWarning($"[NarrativeEventContentImporter] Source folder not found: {SourceFolder}");
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
                    var dto = JsonUtility.FromJson<NarrativeEventContentDto>(File.ReadAllText(filePath));
                    if (string.IsNullOrWhiteSpace(dto.key))
                    {
                        Debug.LogError($"[NarrativeEventContentImporter] \"{filePath}\" missing \"key\". Skipped.");
                        failed++;
                        continue;
                    }

                    var assetPath = $"{TargetFolder}/{dto.key}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<NarrativeEventSO>(assetPath);
                    if (existing != null)
                    {
                        ApplyDto(existing, dto);
                        EditorUtility.SetDirty(existing);
                        updated++;
                    }
                    else
                    {
                        var asset = ScriptableObject.CreateInstance<NarrativeEventSO>();
                        ApplyDto(asset, dto);
                        AssetDatabase.CreateAsset(asset, assetPath);
                        created++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NarrativeEventContentImporter] Failed to import \"{filePath}\": {ex.Message}");
                    failed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NarrativeEventContentImporter] Done. Created: {created}, Updated: {updated}, Failed: {failed}.");
        }

        private static void ApplyDto(NarrativeEventSO asset, NarrativeEventContentDto dto)
        {
            var so = new SerializedObject(asset);
            so.FindProperty("title").stringValue = dto.title ?? "";
            so.FindProperty("body").stringValue = dto.body ?? "";
            so.FindProperty("weight").floatValue = dto.weight <= 0f ? 1f : dto.weight;
            so.FindProperty("requiresActiveProject").boolValue = dto.requiresActiveProject;

            if (Enum.TryParse<NarrativeCategory>(dto.category, true, out var cat))
                so.FindProperty("category").enumValueIndex = (int)cat;

            var optionsProp = so.FindProperty("options");
            optionsProp.ClearArray();
            if (dto.options != null)
            {
                for (int i = 0; i < dto.options.Length; i++)
                {
                    var od = dto.options[i];
                    optionsProp.InsertArrayElementAtIndex(i);
                    var opt = optionsProp.GetArrayElementAtIndex(i);
                    opt.FindPropertyRelative("text").stringValue = od.text ?? "";

                    var skill = FounderSkill.None;
                    if (!string.IsNullOrEmpty(od.requiredSkill))
                        Enum.TryParse(od.requiredSkill, true, out skill);
                    opt.FindPropertyRelative("requiredSkill").enumValueIndex = (int)skill;

                    var effectsProp = opt.FindPropertyRelative("effects");
                    effectsProp.ClearArray();
                    if (od.effects != null)
                    {
                        for (int j = 0; j < od.effects.Length; j++)
                        {
                            effectsProp.InsertArrayElementAtIndex(j);
                            var fx = effectsProp.GetArrayElementAtIndex(j);
                            var type = EventEffectType.Cash;
                            Enum.TryParse(od.effects[j].type, true, out type);
                            fx.FindPropertyRelative("type").enumValueIndex = (int)type;
                            fx.FindPropertyRelative("amount").floatValue = od.effects[j].amount;
                        }
                    }
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
