using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameLike.LevelBuilder.Editor
{
    /// <summary>
    /// Utilitário de produtividade (LEVEL_BUILDER.md §13): gera assets
    /// <see cref="Tile"/> a partir de todos os Sprites de uma pasta escolhida,
    /// espelhando a estrutura de subpastas em <c>Assets/Data/Tiles/</c>. Cobre
    /// sprite sheets (todos os sub-sprites viram tiles). Pula tiles já existentes.
    ///
    /// O papel do tile (chão ou parede) é decidido na <b>camada</b> onde ele é
    /// pintado, nunca pelo nome — este gerador não tipa nem prefixa tiles
    /// (princípio inegociável §2).
    /// </summary>
    public static class TilePaletteGenerator
    {
        private const string OutputRoot = "Assets/Data/Tiles";

        /// <summary>Abre um seletor de pasta e gera os Tiles dos sprites encontrados.</summary>
        public static void GenerateFromFolderDialog()
        {
            string abs = EditorUtility.OpenFolderPanel("Pasta de sprites (origem)", Application.dataPath, "");
            if (string.IsNullOrEmpty(abs)) return;

            string sourceRoot = ToAssetsRelative(abs);
            if (sourceRoot == null)
            {
                EditorUtility.DisplayDialog("Gerar Tiles", "A pasta precisa estar dentro de Assets/.", "OK");
                return;
            }

            GenerateFromFolder(sourceRoot);
        }

        /// <summary>Gera Tiles de todos os sprites sob <paramref name="sourceRoot"/> (caminho Assets-relativo).</summary>
        public static void GenerateFromFolder(string sourceRoot)
        {
            EnsureFolder(OutputRoot);
            int created = 0, skipped = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { sourceRoot }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>();

                foreach (var sprite in sprites)
                {
                    string rel    = assetPath.Substring(sourceRoot.Length).TrimStart('/');
                    string subDir = Path.GetDirectoryName(rel)?.Replace("\\", "/") ?? "";
                    string outDir = string.IsNullOrEmpty(subDir) ? OutputRoot : $"{OutputRoot}/{subDir}";
                    EnsureFolder(outDir);

                    string tilePath = $"{outDir}/{sprite.name}.asset";
                    if (AssetDatabase.LoadAssetAtPath<Tile>(tilePath) != null) { skipped++; continue; }

                    var tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.colliderType = Tile.ColliderType.None;
                    AssetDatabase.CreateAsset(tile, tilePath);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TilePaletteGenerator] Concluído — Criados: {created} | Já existiam: {skipped} | Saída: {OutputRoot}/");
            var folder = AssetDatabase.LoadAssetAtPath<Object>(OutputRoot);
            if (folder != null) EditorGUIUtility.PingObject(folder);
        }

        private static string ToAssetsRelative(string absolutePath)
        {
            absolutePath = absolutePath.Replace("\\", "/");
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (absolutePath == dataPath) return "Assets";
            if (absolutePath.StartsWith(dataPath + "/")) return "Assets" + absolutePath.Substring(dataPath.Length);
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
