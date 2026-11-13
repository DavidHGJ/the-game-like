#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLike.UI.Gallery
{
    /// <summary>
    /// Ferramenta de dev: com <c>-glgallery &lt;pasta&gt;</c>, liga cada tela da galeria por vez,
    /// espera a animação de entrada assentar e salva um PNG por tela na pasta dada, encerrando
    /// em seguida. Só existe em editor/development build — custo zero no release.
    /// </summary>
    public sealed class GalleryCapture : MonoBehaviour
    {
        private string _dir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string dir = ArgValue("-glgallery");
            if (string.IsNullOrEmpty(dir)) return;
            var go = new GameObject("[GalleryCapture]");
            DontDestroyOnLoad(go);
            go.AddComponent<GalleryCapture>()._dir = dir;
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(_dir);
            yield return new WaitForSecondsRealtime(1.0f);

            var hosts = new List<GalleryScreenHost>(FindObjectsByType<GalleryScreenHost>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            hosts.Sort((a, b) => Order(a.transform).CompareTo(Order(b.transform)));
            var hud = FindFirstObjectByType<AnimatedHud>(FindObjectsInactive.Include);
            var hudDoc = hud != null ? hud.GetComponent<UIDocument>() : null;
            foreach (var menu in FindObjectsByType<AnimatedMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                menu.gameObject.SetActive(false);
            foreach (var h in hosts) h.gameObject.SetActive(false);

            string only = ArgValue("-glonly");
            int index = 0;
            foreach (var host in hosts)
            {
                index++;
                if (!string.IsNullOrEmpty(only) && !host.name.ToLowerInvariant().Contains(only.ToLowerInvariant())) continue;
                if (hudDoc != null && hudDoc.rootVisualElement != null)
                    hudDoc.rootVisualElement.style.display = host.OverGameplay ? DisplayStyle.Flex : DisplayStyle.None;
                host.gameObject.SetActive(true);
                yield return null;
                yield return null;
                yield return new WaitForSecondsRealtime(host.SettleSeconds);
                string file = Path.Combine(_dir, index.ToString("00") + "_" + Slug(host.name) + ".png");
                ScreenCapture.CaptureScreenshot(file);
                yield return new WaitForSecondsRealtime(0.35f);
                host.gameObject.SetActive(false);
                yield return null;
            }

            Debug.Log("[GalleryCapture] Telas capturadas em " + _dir);
            yield return new WaitForSecondsRealtime(0.8f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        private static int Order(Transform t)
        {
            int parent = t.parent != null ? t.parent.GetSiblingIndex() : 0;
            return parent * 1000 + t.GetSiblingIndex();
        }

        private static string Slug(string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in name.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (char.IsLetterOrDigit(c) && c < 128) sb.Append(char.ToLowerInvariant(c));
                else if ((c == ' ' || c == '-' || c == '—') && sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
            }
            return sb.ToString().Trim('_');
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
