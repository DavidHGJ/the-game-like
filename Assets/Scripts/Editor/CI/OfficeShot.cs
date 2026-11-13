using System.IO;
using GameLike.Data.LevelBuilder;
using GameLike.Systems.Office;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GameLike.Editor.CI
{
    /// <summary>
    /// Captura headless do escritório montado a partir de um OfficeLayoutSO,
    /// para revisão fora do editor. Monta a cena com a mesma câmera iso e luz do
    /// jogo, renderiza via URP (SubmitRenderRequest) e grava um PNG.
    ///
    /// Uso: Unity -batchmode -quit -executeMethod GameLike.Editor.CI.OfficeShot.Capture
    ///      -layout GarageLayout -shotOut C:\caminho\shot.png
    /// </summary>
    public static class OfficeShot
    {
        public static void Capture() => Run(null);

        /// <summary>
        /// Calibração de rotação: posiciona a mesma peça em rot 0/1/2/3 lado a
        /// lado para ler, numa foto, para onde cada passo faz a frente apontar.
        /// </summary>
        public static void CaptureCalib()
        {
            string key = ArgValue("-piece", "workstation");
            Run(temp =>
            {
                // Carregar o def DEPOIS do NewScene (senão o prefab vira fake-null).
                var def = AssetDatabase.LoadAssetAtPath<FurnitureDef3DSO>("Assets/Data/Furniture3D/" + key + ".asset");
                temp.EditorSetSize(12, 3);
                for (int r = 0; r < 4; r++)
                    temp.EditorPlacements.Add(new FurniturePlacement
                    {
                        furniture = def,
                        cell = new Vector2Int(r * 3, 1),
                        rotationSteps = r
                    });
            });
        }

        private static void Run(System.Action<OfficeLayoutSO> authorTemp)
        {
            string layoutName = ArgValue("-layout", "GarageLayout");
            string outPath = ArgValue("-shotOut", Path.Combine(Path.GetTempPath(), "office_shot.png"));
            int w = 1600, h = 900;

            // A cena nova precisa existir ANTES do Resources.Load: NewScene(Single)
            // descarrega assets não referenciados e deixaria o layout fake-null.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            OfficeLayoutSO layout;
            if (authorTemp != null)
            {
                layout = ScriptableObject.CreateInstance<OfficeLayoutSO>();
                authorTemp(layout);
            }
            else
            {
                layout = Resources.Load<OfficeLayoutSO>("Office/" + layoutName);
                if (layout == null)
                {
                    Debug.LogError("[OfficeShot] Layout não encontrado em Resources/Office/" + layoutName);
                    return;
                }
            }

            var camGo = new GameObject("ShotCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.89f, 0.80f);
            cam.farClipPlane = 400f;
            cam.transform.rotation = Quaternion.Euler(30f, 225f, 0f);

            OfficeLighting.Apply();
            GameLike.Editor.Setup.OfficeGlow.ApplyBloom(cam);

            var root = new GameObject("OfficeShotRoot");
            OfficeBuilder.Build(layout, root.transform);

            // Para calibração, enquadra só a mobília (ignora chão/paredes).
            var furniture = root.transform.Find("Furniture");
            var framingTarget = (authorTemp != null && furniture != null) ? furniture.gameObject : root;
            var bounds = ComputeBounds(framingTarget);
            cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z, bounds.extents.y) * 1.15f + 1.5f;
            cam.transform.position = bounds.center - cam.transform.forward * 80f;

            // HDR na RT para o Bloom ter faixa (valores > 1). cam.Render() no URP do
            // Unity 6 roda a pipeline completa (inclui pós-processamento/Bloom), o que
            // o SubmitRenderRequest/StandardRequest nem sempre faz.
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.DefaultHDR) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            var png = tex.EncodeToPNG();
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, png);
            Object.DestroyImmediate(tex);
            rt.Release();

            Debug.Log("[OfficeShot] GRAVADO " + outPath + " (" + png.Length + " bytes, peças=" + layout.EditorPlacements.Count + ")");
        }

        private static Bounds ComputeBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, new Vector3(8, 3, 8));
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static string ArgValue(string flag, string fallback)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == flag) return args[i + 1];
            return fallback;
        }
    }
}
