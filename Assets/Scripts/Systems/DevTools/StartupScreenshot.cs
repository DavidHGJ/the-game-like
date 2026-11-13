#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Collections;
using GameLike.Data.Definitions;
using GameLike.Systems.Core;
using GameLike.Systems.Marketing;
using UnityEngine;

namespace GameLike.Systems.DevTools
{
    /// <summary>
    /// Ferramenta de dev: quando o player é iniciado com <c>-glshot &lt;caminho&gt;</c>,
    /// inicia uma run de demonstração, deixa a produção avançar alguns segundos e captura
    /// um screenshot do HUD + escritório 3D no caminho dado, encerrando em seguida. Só
    /// existe em development build/editor — custo zero no release. Auto-instala via
    /// <see cref="RuntimeInitializeOnLoadMethod"/>, sem precisar estar na cena.
    /// </summary>
    public sealed class StartupScreenshot : MonoBehaviour
    {
        private string _path;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string path = ArgValue("-glshot");
            if (string.IsNullOrEmpty(path)) return;
            var go = new GameObject("[StartupScreenshot]");
            DontDestroyOnLoad(go);
            go.AddComponent<StartupScreenshot>()._path = path;
        }

        private IEnumerator Start()
        {
            GameController gc = null;
            float t = 0f;
            while (t < 2.5f)
            {
                gc = FindFirstObjectByType<GameController>();
                if (gc != null && gc.Run != null) break;
                t += UnityEngine.Time.unscaledDeltaTime;
                yield return null;
            }

            if (gc != null && gc.Run != null)
            {
                // ---- escritório/HUD: inicia uma run de demonstração ----
                if (gc.Projects != null && gc.Projects.CanStartProject)
                {
                    var genre = gc.AvailableGenres.Count > 0 ? gc.AvailableGenres[0] : null;
                    var theme = gc.AvailableThemes.Count > 0 ? gc.AvailableThemes[0] : null;
                    if (genre != null)
                        gc.StartProject("Jogo Demo", genre, ProjectSize.Small, new float[] { 6, 5, 3, 6, 5 }, theme);
                    if (gc.Time != null) { gc.Time.Resume(); gc.Time.SetSpeedIndex(2); }
                }
                yield return new WaitForSeconds(3f);
                if (gc.Marketing != null) gc.Marketing.RunCampaign(MarketingCampaign.Announcement);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // ---- menu: sem mouse, emite rajadas p/ o fogo aparecer cheio no print ----
                var fireGo = GameObject.Find("Fire");
                var fire = fireGo != null ? fireGo.GetComponent<ParticleSystem>() : null;
                for (int i = 0; i < 45; i++)
                {
                    if (fire != null) fire.Emit(9);
                    yield return null;
                }
                yield return new WaitForSeconds(0.3f);
            }

            ScreenCapture.CaptureScreenshot(_path);
            Debug.Log($"[StartupScreenshot] Capturado em {_path}");
            yield return new WaitForSeconds(1.5f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        private static string ArgValue(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return null;
        }
    }
}
#endif
