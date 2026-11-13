using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Volume de cada móvel, medido uma vez a partir das malhas do prefab (no espaço do pivô).
    /// É o que permite encostar a peça numa borda da célula sem atravessar a parede e saber
    /// a altura do tampo de um móvel para pôr outra peça em cima.
    /// </summary>
    public static class FurnitureBounds
    {
        private static readonly Dictionary<FurnitureDef3DSO, Bounds?> Cache = new Dictionary<FurnitureDef3DSO, Bounds?>();

        /// <summary>Volume local da peça (pivô na origem). False se ela não tem malha.</summary>
        public static bool TryGet(FurnitureDef3DSO def, out Bounds bounds)
        {
            bounds = default;
            if (def == null) return false;
            if (!Cache.TryGetValue(def, out Bounds? cached))
            {
                cached = Measure(def.Prefab);
                Cache[def] = cached;
            }
            if (!cached.HasValue) return false;
            bounds = cached.Value;
            return true;
        }

        /// <summary>
        /// Extensão da peça no plano do chão depois de girada em passos de 90°:
        /// <paramref name="min"/>/<paramref name="max"/> em (x, z) relativos ao pivô.
        /// </summary>
        public static bool TryGetRotatedFootprint(FurnitureDef3DSO def, int rotationSteps, out Vector2 min, out Vector2 max)
        {
            min = max = default;
            if (!TryGet(def, out Bounds bounds)) return false;
            var rotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
            Vector3 a = rotation * new Vector3(bounds.min.x, 0f, bounds.min.z);
            Vector3 b = rotation * new Vector3(bounds.max.x, 0f, bounds.max.z);
            min = new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z));
            max = new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
            return true;
        }

        /// <summary>
        /// Altura em que uma peça apoiada neste móvel fica: o tampo declarado na definição ou,
        /// se não houver, o topo da malha.
        /// </summary>
        public static float SurfaceHeight(FurnitureDef3DSO def)
        {
            if (def == null) return 0f;
            if (def.SurfaceHeight > 0f) return def.SurfaceHeight;
            return TryGet(def, out Bounds bounds) ? bounds.max.y : 0f;
        }

        private static Bounds? Measure(GameObject prefab)
        {
            if (prefab == null) return null;
            var root = prefab.transform;
            Matrix4x4 toRoot = Matrix4x4.TRS(Vector3.zero, root.localRotation, root.localScale) * root.worldToLocalMatrix;
            bool any = false;
            var bounds = new Bounds();
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;
                Bounds mesh = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? mesh.min.x : mesh.max.x,
                        (i & 2) == 0 ? mesh.min.y : mesh.max.y,
                        (i & 4) == 0 ? mesh.min.z : mesh.max.z);
                    Vector3 point = matrix.MultiplyPoint3x4(corner);
                    if (!any) { bounds = new Bounds(point, Vector3.zero); any = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return any ? bounds : (Bounds?)null;
        }
    }
}
