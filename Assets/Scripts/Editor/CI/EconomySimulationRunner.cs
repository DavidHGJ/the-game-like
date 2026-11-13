using System.IO;
using System.Text;
using GameLike.Systems.Simulation;
using UnityEditor;
using UnityEngine;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Roda a bateria padrão de simulação da economia com os valores atuais do balanceamento e
    /// grava o relatório em <c>Docs/Simulacao/economia.md</c> (sem marketing e com marketing
    /// completo). Pelo menu do editor ou em modo batch — leva segundos, sem abrir o jogo.
    /// </summary>
    public static class EconomySimulationRunner
    {
        private const string OutputFolder = "Docs/Simulacao";

        [MenuItem("Tools/GameLike/Balanceamento/Simular economia (relatório)")]
        public static void RunFromMenu()
        {
            string path = Write(out int passed, out int total);
            EditorUtility.DisplayDialog("Simulação da economia", passed + " de " + total + " cenários aprovados.\n\nRelatório: " + path, "Ok");
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>Entrada para o modo batch (CI).</summary>
        public static void BatchRun()
        {
            string path = Write(out int passed, out int total);
            Debug.Log("GAMELIKE_SIMULATION passed=" + passed + " total=" + total + " report=" + path);
            EditorApplication.Exit(0);
        }

        private static string Write(out int passed, out int total)
        {
            var plain = EconomyReport.RunAll(SimMarketing.None);
            var full = EconomyReport.RunAll(SimMarketing.Full);
            EconomyReport.Count(plain, out int p1, out int t1);
            EconomyReport.Count(full, out int p2, out int t2);
            passed = p1 + p2;
            total = t1 + t2;

            var sb = new StringBuilder();
            sb.AppendLine(EconomyReport.ToMarkdown(plain, "Simulação da economia — sem marketing"));
            sb.AppendLine(EconomyReport.ToMarkdown(full, "Simulação da economia — marketing completo (anúncio, wishlist, demo)"));

            Directory.CreateDirectory(OutputFolder);
            string path = Path.GetFullPath(Path.Combine(OutputFolder, "economia.md"));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }
    }
}
