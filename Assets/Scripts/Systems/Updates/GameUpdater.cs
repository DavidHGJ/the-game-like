using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameLike.Systems.Updates
{
    /// <summary>Uma versão publicada mais nova que a instalada.</summary>
    public sealed class AvailableUpdate
    {
        public string version;
        public string notes;
        public string downloadUrl;
        public long sizeBytes;
    }

    /// <summary>
    /// Atualização automática pelo GitHub Releases. <see cref="Check"/> consulta a última
    /// release do repositório e compara com a versão instalada; <see cref="DownloadAndInstall"/>
    /// baixa o .zip da release, fecha o jogo e um script troca os arquivos e reabre. Só faz
    /// algo num build para Windows — no editor é inerte.
    /// </summary>
    public static class GameUpdater
    {
        /// <summary>Repositório de onde vêm as releases (dono/nome).</summary>
        public const string Repository = "DavidHGJ/the-game-like";
        private const string LatestUrl = "https://api.github.com/repos/" + Repository + "/releases/latest";
        private const string ExeName = "TheGameLike.exe";

        [Serializable] private sealed class ReleaseAsset { public string name; public string browser_download_url; public long size; }
        [Serializable] private sealed class ReleaseInfo { public string tag_name; public string body; public bool draft; public bool prerelease; public ReleaseAsset[] assets; }

        /// <summary>true onde a atualização automática funciona (build de Windows).</summary>
        public static bool Supported => !Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer;

        /// <summary>
        /// Consulta a última release. Chama <paramref name="done"/> com a atualização disponível,
        /// ou null se a instalada já é a mais nova, se não há release ou se a consulta falhou.
        /// </summary>
        public static IEnumerator Check(Action<AvailableUpdate> done)
        {
            using (var request = UnityWebRequest.Get(LatestUrl))
            {
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                request.SetRequestHeader("User-Agent", "TheGameLike-Updater");
                request.timeout = 10;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.Log("[Updater] Sem resposta do servidor de releases: " + request.error);
                    done(null);
                    yield break;
                }
                done(Parse(request.downloadHandler.text, Application.version));
            }
        }

        /// <summary>Interpreta a resposta da API e devolve a atualização se ela for mais nova que <paramref name="installed"/>.</summary>
        public static AvailableUpdate Parse(string json, string installed)
        {
            ReleaseInfo info;
            try { info = JsonUtility.FromJson<ReleaseInfo>(json); }
            catch (ArgumentException) { return null; }
            if (info == null || info.draft || string.IsNullOrEmpty(info.tag_name) || info.assets == null) return null;
            if (CompareVersions(info.tag_name, installed) <= 0) return null;
            foreach (var asset in info.assets)
            {
                if (asset == null || string.IsNullOrEmpty(asset.name) || !asset.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                return new AvailableUpdate
                {
                    version = info.tag_name.TrimStart('v', 'V'),
                    notes = info.body ?? "",
                    downloadUrl = asset.browser_download_url,
                    sizeBytes = asset.size
                };
            }
            return null;
        }

        /// <summary>
        /// Compara versões no formato "1.2.3" (aceita prefixo "v"). Positivo se
        /// <paramref name="a"/> é mais nova que <paramref name="b"/>.
        /// </summary>
        public static int CompareVersions(string a, string b)
        {
            string[] pa = (a ?? "").TrimStart('v', 'V').Split('.');
            string[] pb = (b ?? "").TrimStart('v', 'V').Split('.');
            for (int i = 0; i < Mathf.Max(pa.Length, pb.Length); i++)
            {
                int na = i < pa.Length ? LeadingNumber(pa[i]) : 0;
                int nb = i < pb.Length ? LeadingNumber(pb[i]) : 0;
                if (na != nb) return na.CompareTo(nb);
            }
            return 0;
        }

        private static int LeadingNumber(string part)
        {
            int value = 0;
            foreach (char c in part)
            {
                if (!char.IsDigit(c)) break;
                value = value * 10 + (c - '0');
            }
            return value;
        }

        /// <summary>
        /// Baixa a atualização (informando o progresso 0..1) e, ao terminar, fecha o jogo e
        /// dispara o script que substitui os arquivos e reabre. <paramref name="failed"/> recebe
        /// a mensagem se algo der errado — nesse caso nada é alterado.
        /// </summary>
        public static IEnumerator DownloadAndInstall(AvailableUpdate update, Action<float> progress, Action<string> failed)
        {
            if (!Supported) { failed("A atualização automática só funciona no jogo instalado (Windows)."); yield break; }

            string work = Path.Combine(Application.temporaryCachePath, "update");
            string zip = Path.Combine(work, "TheGameLike-" + update.version + ".zip");
            try
            {
                if (Directory.Exists(work)) Directory.Delete(work, true);
                Directory.CreateDirectory(work);
            }
            catch (IOException e) { failed("Não consegui preparar a pasta temporária: " + e.Message); yield break; }

            using (var request = new UnityWebRequest(update.downloadUrl, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerFile(zip) { removeFileOnAbort = true };
                request.SetRequestHeader("User-Agent", "TheGameLike-Updater");
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    progress(request.downloadProgress);
                    yield return null;
                }
                if (request.result != UnityWebRequest.Result.Success) { failed("O download falhou: " + request.error); yield break; }
            }
            progress(1f);

            string installFolder = Path.GetDirectoryName(Application.dataPath);
            string script = Path.Combine(work, "apply_update.cmd");
            try
            {
                File.WriteAllText(script, BuildScript(Process.GetCurrentProcess().Id, zip, installFolder), new UTF8Encoding(false));
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"\"" + script + "\"\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = work
                });
            }
            catch (Exception e) when (e is IOException || e is System.ComponentModel.Win32Exception)
            {
                failed("Não consegui iniciar a instalação: " + e.Message);
                yield break;
            }
            Application.Quit();
        }

        /// <summary>
        /// Script que roda depois que o jogo fecha: espera o processo sair, extrai o .zip por
        /// cima da pasta de instalação e reabre o jogo.
        /// </summary>
        private static string BuildScript(int processId, string zip, string installFolder)
        {
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("chcp 65001 >nul");
            sb.AppendLine(":wait");
            sb.AppendLine("tasklist /FI \"PID eq " + processId + "\" 2>nul | find \"" + processId + "\" >nul");
            sb.AppendLine("if not errorlevel 1 (timeout /t 1 /nobreak >nul & goto wait)");
            sb.AppendLine("powershell -NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -LiteralPath '" + zip.Replace("'", "''")
                          + "' -DestinationPath '" + installFolder.Replace("'", "''") + "' -Force\"");
            sb.AppendLine("start \"\" \"" + Path.Combine(installFolder, ExeName) + "\"");
            return sb.ToString();
        }
    }
}
