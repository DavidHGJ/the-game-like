using GameLike.Data.LevelBuilder;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// A "atmosfera" do escritório — o que tira o cenário do visual chapado: pós-processamento
    /// (bloom nas telas e janelas, tonemapping, leve contraste e vinheta), sombras suaves bem
    /// definidas, luz ambiente em três tons com o rebote da cor do piso, reflexo do ambiente
    /// nos materiais lisos (sonda de reflexão da própria sala) e um chão externo que recebe a
    /// sombra da sala. Tudo montado em runtime; jogo e Map Builder chamam o mesmo
    /// <see cref="Apply"/>, então os dois mostram exatamente a mesma imagem.
    /// </summary>
    public static class OfficeAtmosphere
    {
        private const string RootName = "[OfficeAtmosphere]";
        private const string GroundName = "Ground";
        private const string ProbeName = "RoomReflection";

        private static VolumeProfile _profile;
        private static Material _groundMaterial;

        /// <summary>Aplica luz, pós-processamento, chão externo e reflexo para o cenário dado.</summary>
        public static void Apply(OfficeLayoutSO layout, Camera camera)
        {
            if (layout == null) return;
            OfficeLighting.Apply(layout.FloorColor);

            var root = GameObject.Find(RootName);
            if (root == null) root = new GameObject(RootName);

            EnsureVolume(root);
            EnsureGround(root, camera);
            ConfigureCamera(camera, layout);
            RefreshReflection(root, layout, camera);
        }

        /// <summary>Refaz o reflexo do ambiente (chamar depois de mudar móveis, cores ou luzes).</summary>
        public static void RefreshReflection(OfficeLayoutSO layout, Camera camera)
        {
            var root = GameObject.Find(RootName);
            if (root != null && layout != null) RefreshReflection(root, layout, camera);
        }

        private static void EnsureVolume(GameObject root)
        {
            var volume = root.GetComponent<Volume>();
            if (volume == null) volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = Profile();
        }

        private static VolumeProfile Profile()
        {
            if (_profile != null) return _profile;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "OfficeAtmosphere";
            _profile.hideFlags = HideFlags.DontSave;

            var bloom = _profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.85f);
            bloom.scatter.Override(0.68f);

            var tone = _profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var color = _profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.18f);
            color.contrast.Override(12f);
            color.saturation.Override(8f);

            var white = _profile.Add<WhiteBalance>(true);
            white.temperature.Override(6f);

            var vignette = _profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.55f);
            return _profile;
        }

        private static void ConfigureCamera(Camera camera, OfficeLayoutSO layout)
        {
            QualitySettings.realtimeReflectionProbes = true;
            float roomDiagonal = new Vector2(layout.Cols, layout.Rows).magnitude * layout.CellSize;

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                // A sombra é calculada até essa distância a partir da câmera: o bastante para
                // cobrir a sala inteira e nada além, para não desperdiçar resolução.
                float toRoom = camera != null ? camera.transform.position.magnitude : 40f;
                pipeline.shadowDistance = toRoom + roomDiagonal;
            }

            if (camera == null) return;
            camera.allowHDR = true;
            var data = camera.GetUniversalAdditionalCameraData();
            if (data == null) return;
            data.renderPostProcessing = true;
            data.renderShadows = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        /// <summary>
        /// Plano enorme logo abaixo da laje, na cor do fundo: recebe a sombra da sala, como
        /// uma maquete pousada numa mesa.
        /// </summary>
        private static void EnsureGround(GameObject root, Camera camera)
        {
            var existing = root.transform.Find(GroundName);
            GameObject ground;
            if (existing != null) ground = existing.gameObject;
            else
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = GroundName;
                ground.transform.SetParent(root.transform, false);
                var collider = ground.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
                ground.transform.localPosition = new Vector3(0f, -0.105f, 0f);
                ground.transform.localScale = new Vector3(60f, 1f, 60f);
                ground.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            if (_groundMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                _groundMaterial = new Material(shader) { name = "OfficeGround", hideFlags = HideFlags.DontSave };
                if (_groundMaterial.HasProperty("_Smoothness")) _groundMaterial.SetFloat("_Smoothness", 0.05f);
                if (_groundMaterial.HasProperty("_EnvironmentReflections")) _groundMaterial.SetFloat("_EnvironmentReflections", 0f);
            }
            // A luz clareia o plano; escurecer um pouco a cor mantém o tom do fundo original.
            Color background = camera != null ? camera.backgroundColor : new Color(0.93f, 0.89f, 0.80f);
            Color albedo = background * 0.78f;
            albedo.a = 1f;
            if (_groundMaterial.HasProperty("_BaseColor")) _groundMaterial.SetColor("_BaseColor", albedo);
            if (_groundMaterial.HasProperty("_Color")) _groundMaterial.SetColor("_Color", albedo);
            ground.GetComponent<MeshRenderer>().sharedMaterial = _groundMaterial;
        }

        private static void RefreshReflection(GameObject root, OfficeLayoutSO layout, Camera camera)
        {
            var existing = root.transform.Find(ProbeName);
            ReflectionProbe probe;
            if (existing != null) probe = existing.GetComponent<ReflectionProbe>();
            else
            {
                var go = new GameObject(ProbeName);
                go.transform.SetParent(root.transform, false);
                probe = go.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                probe.resolution = 256;
                probe.hdr = true;
                probe.boxProjection = true;
                probe.nearClipPlane = 0.05f;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.intensity = 1f;
            }

            float width = layout.Cols * layout.CellSize, depth = layout.Rows * layout.CellSize;
            probe.transform.localPosition = new Vector3(0f, layout.WallHeight * 0.5f, 0f);
            probe.size = new Vector3(width + 1.5f, layout.WallHeight + 1.5f, depth + 1.5f);
            probe.backgroundColor = camera != null ? camera.backgroundColor : new Color(0.93f, 0.89f, 0.80f);
            probe.RenderProbe();
        }

        private static void Destroy(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
