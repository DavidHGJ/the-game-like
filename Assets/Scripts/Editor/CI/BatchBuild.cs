using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Build standalone headless para validar o pipeline completo (AOT + cena + assets)
    /// e permitir uma captura visual do HUD + escritório 3D. Uso:
    /// <c>-executeMethod GameLike.Editor.CI.BatchBuild.BuildWindows</c>.
    /// </summary>
    public static class BatchBuild
    {
        private const string MenuScene = "Assets/Scenes/MainMenu.unity";
        private const string GameScene = "Assets/Scenes/GameOffice.unity";
        private const string OutDir = "Build/Win64";
        private const string Exe = "TheGameLike.exe";

        /// <summary>
        /// Build jogável (release) para Windows gravado na pasta Downloads do usuário,
        /// abrindo no menu do jogo. Conveniência para entregar um build ao dev sem
        /// navegar o diálogo de build. Roda pelo menu do editor (não encerra o editor).
        /// </summary>
        [MenuItem("Tools/GameLike/Build/Windows → Downloads (jogável)")]
        public static void BuildToDownloads()
        {
            string downloads = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "Downloads", "TheGameLike");
            Directory.CreateDirectory(downloads);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MenuScene, GameScene }, // abre no menu (experiência completa)
                locationPathName = Path.Combine(downloads, Exe),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None // release jogável (sem watermark de desenvolvimento)
            };

            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"GAMELIKE_BUILD_DOWNLOADS result={s.result} errors={s.totalErrors} size={s.totalSize} path={options.locationPathName}");
            // Nota: NÃO abrir o Explorer (RevealInFinder) aqui — ele rouba o foco do
            // editor e trava automação de tela quando o shell não está autorizado.
        }

        /// <summary>
        /// Build de DESENVOLVIMENTO que abre direto no gameplay (GameOffice primeiro),
        /// para capturar prints in-engine via <c>-glshot</c> (StartupScreenshot, que só
        /// existe em development build). Não é a entrega jogável — é só para screenshot.
        /// </summary>
        [MenuItem("Tools/GameLike/Build/Dev gameplay (p/ screenshot)")]
        public static void BuildDevForShot()
        {
            const string dir = "Build/Win64Shot";
            Directory.CreateDirectory(dir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { GameScene, MenuScene }, // gameplay primeiro → auto-inicia run demo
                locationPathName = Path.Combine(dir, Exe),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"GAMELIKE_DEVSHOT result={s.result} errors={s.totalErrors} path={options.locationPathName}");
        }

        /// <summary>
        /// Build de PLAYTEST para o dev (batch): abre no menu, development build — é o que
        /// mantém a opção "Map Builder" no menu e mostra erros na tela. Grava na pasta dada por
        /// <c>-glout &lt;pasta&gt;</c>. Uso: <c>-executeMethod GameLike.Editor.CI.BatchBuild.BuildPlaytest -glout C:\...</c>.
        /// </summary>
        public static void BuildPlaytest()
        {
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-glout");
            string dir = at >= 0 && at < args.Length - 1 ? args[at + 1] : "Build/Playtest";
            Directory.CreateDirectory(dir);
            GameLike.Editor.Office.FurnitureCatalogSync.Sync(); // o Map Builder do build lista a mobília por este catálogo

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MenuScene, GameScene },
                locationPathName = Path.Combine(dir, Exe),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"GAMELIKE_PLAYTEST_BUILD result={s.result} errors={s.totalErrors} size={s.totalSize} path={options.locationPathName}");
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>
        /// Build de RELEASE para publicar (batch): grava a versão dada por <c>-glversion 0.2.0</c>
        /// no projeto (é ela que o atualizador compara com a release do GitHub), gera o build
        /// final para Windows abrindo no menu e grava em <c>-glout &lt;pasta&gt;</c>. Sem ferramentas
        /// do dev: elas não existem em build sem DEVELOPMENT_BUILD (ver <c>GameLike.Core.DevTools</c>).
        /// Usado por <c>Tools/Release/release.sh</c>.
        /// </summary>
        public static void BuildRelease()
        {
            var args = System.Environment.GetCommandLineArgs();
            int outAt = System.Array.IndexOf(args, "-glout");
            int versionAt = System.Array.IndexOf(args, "-glversion");
            string dir = outAt >= 0 && outAt < args.Length - 1 ? args[outAt + 1] : "Build/Release";
            if (versionAt >= 0 && versionAt < args.Length - 1)
            {
                PlayerSettings.bundleVersion = args[versionAt + 1];
                AssetDatabase.SaveAssets();
            }
            Directory.CreateDirectory(dir);
            GameLike.Editor.Office.FurnitureCatalogSync.Sync();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MenuScene, GameScene },
                locationPathName = Path.Combine(dir, Exe),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"GAMELIKE_RELEASE_BUILD result={s.result} errors={s.totalErrors} version={PlayerSettings.bundleVersion} path={options.locationPathName}");
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }

        public static void BuildWindows()
        {
            Directory.CreateDirectory(OutDir);
            // -menuFirst abre no menu (p/ screenshot do menu); senão abre no escritório.
            bool menuFirst = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-menuFirst") >= 0;
            var scenes = menuFirst ? new[] { MenuScene, GameScene } : new[] { GameScene, MenuScene };

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutDir, Exe),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"GAMELIKE_BUILD result={summary.result} errors={summary.totalErrors} size={summary.totalSize}");
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
