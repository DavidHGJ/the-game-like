using System.IO;
using System.Text;
using GameLike.Data.Balance;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Traz para o projeto os ajustes feitos no jogo instalado (tela de Parâmetros e Map Builder),
    /// para irem na próxima versão. Os parâmetros são gravados direto nos assets de
    /// <c>Resources/Balance</c> (eles continuam sendo a fonte da verdade); os cenários vão para
    /// <c>Resources/OfficeMaps</c>. Lê da pasta do jogo no computador
    /// (<c>persistentDataPath</c>, a mesma do jogo instalado) ou da pasta dada por <c>-glfrom</c>.
    /// Grava um relatório com cada valor alterado.
    /// </summary>
    public static class ImportPlaytestSettings
    {
        [MenuItem("Tools/GameLike/Build/Importar ajustes do jogo instalado")]
        public static void ImportFromMenu()
        {
            string report = Import(Application.persistentDataPath);
            EditorUtility.DisplayDialog("Ajustes importados", report.Length > 1500 ? report.Substring(0, 1500) + "…" : report, "Ok");
        }

        /// <summary>Entrada para o modo batch (CI): <c>-glfrom &lt;pasta&gt;</c> opcional.</summary>
        public static void BatchImport()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-glfrom");
            string from = at >= 0 && at < args.Length - 1 ? args[at + 1] : Application.persistentDataPath;
            string report = Import(from);
            Debug.Log(report);
            Debug.Log("GAMELIKE_IMPORT_OK");
            EditorApplication.Exit(0);
        }

        /// <summary>Aplica os ajustes encontrados em <paramref name="root"/> e devolve o relatório.</summary>
        public static string Import(string root)
        {
            var report = new StringBuilder();
            report.AppendLine("Origem: " + root);
            ImportBalance(Path.Combine(root, "BalanceOverrides"), report);
            ImportMaps(Path.Combine(root, "OfficeMaps"), report);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return report.ToString();
        }

        private static void ImportBalance(string folder, StringBuilder report)
        {
            if (!Directory.Exists(folder)) { report.AppendLine("Parâmetros: nenhum ajuste salvo."); return; }
            BalanceProvider.InvalidateCache();
            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                string typeName = Path.GetFileNameWithoutExtension(file);
                var asset = FindBalanceAsset(typeName);
                if (asset == null) { report.AppendLine("  ignorado (tipo desconhecido): " + typeName); continue; }

                string before = JsonUtility.ToJson(asset);
                JsonUtility.FromJsonOverwrite(File.ReadAllText(file), asset);
                EditorUtility.SetDirty(asset);
                report.AppendLine("Parâmetros — " + typeName + ":");
                Diff(before, JsonUtility.ToJson(asset), report);
            }
        }

        private static BalanceAsset FindBalanceAsset(string typeName)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeName, new[] { "Assets/Resources/Balance" }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<BalanceAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.GetType().Name == typeName) return asset;
            }
            return null;
        }

        /// <summary>Lista campo a campo o que mudou entre dois JSON planos do JsonUtility.</summary>
        private static void Diff(string beforeJson, string afterJson, StringBuilder report)
        {
            var before = Flatten(beforeJson);
            int changes = 0;
            foreach (var pair in Flatten(afterJson))
            {
                if (before.TryGetValue(pair.Key, out string old) && old == pair.Value) continue;
                report.AppendLine("  " + pair.Key + ": " + (old ?? "—") + " → " + pair.Value);
                changes++;
            }
            if (changes == 0) report.AppendLine("  (sem mudanças)");
        }

        private static System.Collections.Generic.Dictionary<string, string> Flatten(string json)
        {
            var result = new System.Collections.Generic.Dictionary<string, string>();
            foreach (string raw in json.Trim('{', '}').Split(','))
            {
                int colon = raw.IndexOf(':');
                if (colon < 0) continue;
                result[raw.Substring(0, colon).Trim().Trim('"')] = raw.Substring(colon + 1).Trim();
            }
            return result;
        }

        private static void ImportMaps(string folder, StringBuilder report)
        {
            string[] files = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.json") : new string[0];
            if (files.Length == 0) { report.AppendLine("Cenários: nenhum salvo."); return; }
            string target = Path.Combine(Application.dataPath, "Resources", "OfficeMaps");
            Directory.CreateDirectory(target);
            foreach (string file in files)
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
                report.AppendLine("Cenário copiado: " + Path.GetFileName(file));
            }
        }
    }
}
