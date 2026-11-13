using System;
using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using UnityEngine;

namespace GameLike.UI.MapBuilder
{
    /// <summary>
    /// Gera, em runtime, a miniatura 3D de cada móvel para a paleta do Map Builder: instancia o
    /// prefab longe da cena, fotografa com uma câmera isométrica própria e guarda a textura.
    /// Uma miniatura por frame (<see cref="Tick"/>) para não travar a abertura da ferramenta.
    /// </summary>
    internal sealed class FurnitureThumbnails : IDisposable
    {
        private const int Size = 128;
        private static readonly Vector3 Studio = new Vector3(0f, -400f, 0f);

        private readonly Dictionary<FurnitureDef3DSO, Texture2D> _cache = new Dictionary<FurnitureDef3DSO, Texture2D>();
        private readonly Queue<(FurnitureDef3DSO def, Action<Texture2D> ready)> _queue = new Queue<(FurnitureDef3DSO, Action<Texture2D>)>();
        private readonly Color _background;
        private Camera _camera;
        private RenderTexture _target;

        public FurnitureThumbnails(Color background) => _background = background;

        /// <summary>Pede a miniatura; <paramref name="ready"/> é chamado quando ela existir.</summary>
        public void Request(FurnitureDef3DSO def, Action<Texture2D> ready)
        {
            if (def == null || def.Prefab == null) return;
            if (_cache.TryGetValue(def, out var cached)) { ready(cached); return; }
            _queue.Enqueue((def, ready));
        }

        /// <summary>Renderiza no máximo uma miniatura pendente.</summary>
        public void Tick()
        {
            if (_queue.Count == 0) return;
            var (def, ready) = _queue.Dequeue();
            if (!_cache.TryGetValue(def, out var texture))
            {
                texture = Render(def);
                _cache[def] = texture;
            }
            if (texture != null) ready(texture);
        }

        private Texture2D Render(FurnitureDef3DSO def)
        {
            EnsureCamera();
            var instance = UnityEngine.Object.Instantiate(def.Prefab, Studio, Quaternion.identity);
            foreach (var light in instance.GetComponentsInChildren<Light>()) light.enabled = false;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return null;
            }
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            _camera.orthographicSize = Mathf.Max(bounds.extents.magnitude * 0.82f, 0.25f);
            _camera.transform.position = bounds.center - _camera.transform.forward * 30f;
            _camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "Thumb_" + def.name };
            texture.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            UnityEngine.Object.DestroyImmediate(instance);
            return texture;
        }

        private void EnsureCamera()
        {
            if (_camera != null) return;
            _target = new RenderTexture(Size, Size, 24) { antiAliasing = 4, name = "FurnitureThumbnail" };
            var go = new GameObject("ThumbnailCamera");
            _camera = go.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = _background;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 80f;
            _camera.targetTexture = _target;
            _camera.transform.rotation = Quaternion.Euler(30f, 225f, 0f);
        }

        public void Dispose()
        {
            foreach (var texture in _cache.Values)
                if (texture != null) UnityEngine.Object.Destroy(texture);
            _cache.Clear();
            if (_camera != null) UnityEngine.Object.Destroy(_camera.gameObject);
            if (_target != null) _target.Release();
        }
    }
}
