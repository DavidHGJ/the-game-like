using System;
using System.Collections.Generic;
using System.IO;
using GameLike.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.Content
{
    /// <summary>
    /// Reads every *.json file under <see cref="SourceFolder"/>, written by a
    /// content agent following <see cref="GenreContentDto"/>'s shape, and
    /// creates or updates the matching <see cref="GenreSO"/> assets under
    /// <see cref="TargetFolder"/>.
    ///
    /// Matching rule: the DTO's "key" field (a stable slug, e.g. "action_rpg")
    /// is used to find an existing asset by file name. If found, its fields
    /// are overwritten in place — its auto-generated Id (see DefinitionAsset)
    /// is preserved, so existing saves referencing that genre remain valid.
    /// If not found, a brand-new asset is created and gets its Id assigned by
    /// DefinitionAssetIdAssigner on the next asset database refresh.
    ///
    /// This is the only sanctioned way to turn agent-authored content into
    /// real ScriptableObject assets — never hand-author a .asset's YAML.
    /// </summary>
    public static class GenreContentImporter
    {
        private const string SourceFolder = "Assets/Content/Genres";
        private const string TargetFolder = "Assets/Data/Genres";

        /// <summary>
        /// Pasta de destino usada pelo vertical slice. O runtime do slice carrega
        /// os gêneros via <c>Resources.LoadAll&lt;GenreSO&gt;("Genres")</c>, então
        /// o import do slice grava aqui. No jogo completo os gêneros ficam em
        /// <see cref="TargetFolder"/> e são referenciados por Inspector (sem Resources).
        /// </summary>
        private const string SliceResourcesFolder = "Assets/Resources/Genres";

        [MenuItem("Tools/GameLike/Import Content/Genres")]
        public static void ImportGenres() => Import(TargetFolder);

        [MenuItem("Tools/GameLike/Import Content/Genres → Resources (slice)")]
        public static void ImportGenresToResources() => Import(SliceResourcesFolder);

        /// <summary>
        /// Lê todos os JSONs de <see cref="SourceFolder"/> e cria/atualiza os
        /// <see cref="GenreSO"/> em <paramref name="targetFolder"/>, casando pelo
        /// campo <c>key</c> e preservando o Id de assets existentes.
        /// </summary>
        public static void Import(string targetFolder)
        {
            if (!Directory.Exists(SourceFolder))
            {
                Debug.LogWarning($"[GenreContentImporter] Source folder not found: {SourceFolder}");
                return;
            }

            EnsureFolderExists(targetFolder);

            var jsonFiles = Directory.GetFiles(SourceFolder, "*.json", SearchOption.TopDirectoryOnly);
            var created = 0;
            var updated = 0;
            var failed = 0;

            foreach (var filePath in jsonFiles)
            {
                // Arquivos "*.example.json" são modelos de referência, não conteúdo real.
                if (filePath.EndsWith(".example.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var json = File.ReadAllText(filePath);
                    var dto = JsonUtility.FromJson<GenreContentDto>(json);

                    if (string.IsNullOrWhiteSpace(dto.key))
                    {
                        Debug.LogError($"[GenreContentImporter] \"{filePath}\" is missing required field \"key\". Skipped.");
                        failed++;
                        continue;
                    }

                    var assetPath = $"{targetFolder}/{dto.key}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<GenreSO>(assetPath);

                    if (existing != null)
                    {
                        ApplyDto(existing, dto);
                        EditorUtility.SetDirty(existing);
                        updated++;
                    }
                    else
                    {
                        var asset = ScriptableObject.CreateInstance<GenreSO>();
                        ApplyDto(asset, dto);
                        AssetDatabase.CreateAsset(asset, assetPath);
                        created++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GenreContentImporter] Failed to import \"{filePath}\": {ex.Message}");
                    failed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); // triggers DefinitionAssetIdAssigner for newly created assets

            Debug.Log($"[GenreContentImporter] Done. Created: {created}, Updated: {updated}, Failed: {failed}.");
        }

        private static void ApplyDto(GenreSO asset, GenreContentDto dto)
        {
            var so = new SerializedObject(asset);

            SetLocalizedStringPlaceholder(so, "displayName", dto.displayNamePtBr);
            SetLocalizedStringPlaceholder(so, "description", dto.descriptionPtBr);

            // Nome amigável plano (a UI usa enquanto a ponte com o Localization Package não existe).
            var displayNameProp = so.FindProperty("displayNamePt");
            if (displayNameProp != null) displayNameProp.stringValue = dto.displayNamePtBr;

            SetWeights(so, "gameDesign", dto.gameDesign, dto.key);
            SetWeights(so, "narrative", dto.narrative, dto.key);
            SetWeights(so, "artDirection", dto.artDirection, dto.key);
            SetWeights(so, "gameFeel", dto.gameFeel, dto.key);
            SetWeights(so, "scope", dto.scope, dto.key);

            SetWeights(so, "graphics", dto.graphics, dto.key);
            SetWeights(so, "animation", dto.animation, dto.key);
            SetWeights(so, "userInterface", dto.userInterface, dto.key);
            SetWeights(so, "vfx", dto.vfx, dto.key);

            SetWeights(so, "soundtrack", dto.soundtrack, dto.key);
            SetWeights(so, "soundDesign", dto.soundDesign, dto.key);
            SetWeights(so, "voiceActing", dto.voiceActing, dto.key);

            SetWeights(so, "programming", dto.programming, dto.key);
            SetWeights(so, "performanceOptimization", dto.performanceOptimization, dto.key);
            SetWeights(so, "stabilityTesting", dto.stabilityTesting, dto.key);

            SetWeights(so, "gameplay", dto.gameplay, dto.key);
            SetWeights(so, "content", dto.content, dto.key);
            SetWeights(so, "implementedNarrative", dto.implementedNarrative, dto.key);
            SetWeights(so, "balancing", dto.balancing, dto.key);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetWeights(SerializedObject so, string fieldName, ParameterWeightDto dto, string genreKey)
        {
            var parsed = WeightScaleParser.ParseOrDefault(dto, $"{genreKey}.{fieldName}");
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[GenreContentImporter] Field \"{fieldName}\" not found on GenreSO. Check for a rename.");
                return;
            }

            prop.FindPropertyRelative("small").enumValueIndex = WeightToEnumIndex(parsed.small);
            prop.FindPropertyRelative("medium").enumValueIndex = WeightToEnumIndex(parsed.medium);
            prop.FindPropertyRelative("large").enumValueIndex = WeightToEnumIndex(parsed.large);
            prop.FindPropertyRelative("aaa").enumValueIndex = WeightToEnumIndex(parsed.aaa);
        }

        private static int WeightToEnumIndex(WeightLevel level)
        {
            // WeightLevel values run -3..+3; SerializedProperty enumValueIndex
            // expects the declaration order index (0-based), which matches
            // (level - StronglyNegative) given the enum's explicit values.
            return (int)level - (int)WeightLevel.StronglyNegative;
        }

        private static void SetLocalizedStringPlaceholder(SerializedObject so, string fieldName, string rawText)
        {
            // LocalizedString itself is configured via the Localization Tables
            // in-editor; the importer only logs the source text for now so a
            // human can paste it into the pt-BR table. Wiring this fully to
            // the Localization package's table API is a follow-up task.
            if (string.IsNullOrEmpty(rawText)) return;
            Debug.Log($"[GenreContentImporter] pt-BR text for \"{fieldName}\": \"{rawText}\" — add to Localization Tables manually for now.");
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var folderName = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
