using UnityEngine;

namespace GameLike.UI.MapBuilder
{
    /// <summary>
    /// Câmera do Map Builder: a mesma ortográfica isométrica do jogo (30° de inclinação,
    /// olhando do canto +X,+Z), com arrastar, zoom e giro em passos de 90° suavizados.
    /// <see cref="GameView"/> volta ao enquadramento exato da partida.
    /// </summary>
    internal sealed class MapBuilderCamera
    {
        private const float Pitch = 30f;
        private const float GameYaw = 225f;
        private const float DefaultGameSize = 6f;
        private const float Distance = 60f;
        private const float MinSize = 2f, MaxSize = 26f;

        private readonly Camera _camera;
        private Vector3 _focus, _focusTarget;
        private float _yaw = GameYaw, _yawTarget = GameYaw;
        private float _size = DefaultGameSize, _sizeTarget = DefaultGameSize;

        /// <summary>Zoom que a câmera do jogo usa para a sala aberta (ver <c>OfficeGrid.GameCameraSize</c>).</summary>
        public float GameSize { get; set; } = DefaultGameSize;

        public Camera Camera => _camera;

        /// <summary>Direção para a qual a câmera olha (já com o giro alvo), usada para esconder paredes.</summary>
        public Vector3 TargetForward => Quaternion.Euler(Pitch, _yawTarget, 0f) * Vector3.forward;

        public MapBuilderCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 300f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.93f, 0.89f, 0.80f);
            go.AddComponent<AudioListener>();
            Apply();
        }

        /// <summary>Enquadramento idêntico ao da cena de jogo.</summary>
        public void GameView()
        {
            _focusTarget = Vector3.zero;
            _yawTarget = GameYaw;
            _sizeTarget = GameSize;
        }

        /// <summary>Gira a vista em passos de 90° (+1 = horário visto de cima).</summary>
        public void Turn(int direction) => _yawTarget += direction * 90f;

        /// <summary>Aproxima (valor positivo) ou afasta a vista.</summary>
        public void ZoomBy(float notches) =>
            _sizeTarget = Mathf.Clamp(_sizeTarget * Mathf.Pow(0.88f, notches), MinSize, MaxSize);

        /// <summary>Desloca a vista pelo arrasto do ponteiro, em pixels de tela.</summary>
        public void DragPixels(Vector2 deltaPixels)
        {
            float unitsPerPixel = 2f * _size / Mathf.Max(1, Screen.height);
            MoveOnGround(-deltaPixels.x * unitsPerPixel, -deltaPixels.y * unitsPerPixel);
        }

        /// <summary>Desloca a vista pelo teclado (eixos -1..1 por segundo).</summary>
        public void PanAxes(Vector2 axes, float deltaTime)
        {
            float speed = _size * 1.4f * deltaTime;
            MoveOnGround(axes.x * speed, axes.y * speed);
        }

        private void MoveOnGround(float screenRight, float screenUp)
        {
            var rotation = Quaternion.Euler(0f, _yawTarget, 0f);
            Vector3 right = rotation * Vector3.right;
            Vector3 forward = rotation * Vector3.forward;
            // Na tela, 1 unidade "para cima" equivale a 1/sin(pitch) unidades no chão.
            _focusTarget += right * screenRight + forward * (screenUp / Mathf.Sin(Pitch * Mathf.Deg2Rad));
            _focusTarget.y = 0f;
        }

        /// <summary>Avança a suavização. Chamar uma vez por frame.</summary>
        public void Tick(float deltaTime)
        {
            float k = 1f - Mathf.Exp(-14f * deltaTime);
            _focus = Vector3.Lerp(_focus, _focusTarget, k);
            _yaw = Mathf.Lerp(_yaw, _yawTarget, k);
            _size = Mathf.Lerp(_size, _sizeTarget, k);
            Apply();
        }

        private void Apply()
        {
            var rotation = Quaternion.Euler(Pitch, _yaw, 0f);
            _camera.transform.SetPositionAndRotation(_focus - rotation * Vector3.forward * Distance, rotation);
            _camera.orthographicSize = _size;
        }
    }
}
