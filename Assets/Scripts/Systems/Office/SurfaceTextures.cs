using System.Collections.Generic;
using GameLike.Data.LevelBuilder;
using UnityEngine;

namespace GameLike.Systems.Office
{
    /// <summary>
    /// Texturas de chão e parede geradas por código, já na cor escolhida: tijolinho, tábuas,
    /// ripado, azulejo, xadrez, listras, carpete e concreto. Cores chapadas com leve variação
    /// por peça (estilo low-poly), sem arquivo de imagem. Cada textura representa
    /// <see cref="MetersPerTile"/> metros e é repetida pelo tamanho da superfície.
    /// </summary>
    public static class SurfaceTextures
    {
        /// <summary>Lado, em metros, que uma repetição da textura cobre.</summary>
        public const float MetersPerTile = 2f;

        private const int Size = 256;
        private const int MaxCached = 24;

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly List<string> Order = new List<string>();

        /// <summary>Textura do estilo na cor dada, ou null para <see cref="SurfaceStyle.Plain"/>.</summary>
        public static Texture2D Get(SurfaceStyle style, Color color)
        {
            if (style == SurfaceStyle.Plain) return null;
            string key = (int)style + "|" + ColorUtility.ToHtmlStringRGB(color);
            if (Cache.TryGetValue(key, out var cached) && cached != null)
            {
                Order.Remove(key);
                Order.Add(key);
                return cached;
            }

            var texture = Generate(style, color);
            Cache[key] = texture;
            Order.Add(key);
            while (Order.Count > MaxCached)
            {
                string oldest = Order[0];
                Order.RemoveAt(0);
                if (Cache.TryGetValue(oldest, out var old) && old != null) Destroy(old);
                Cache.Remove(oldest);
            }
            return texture;
        }

        private static void Destroy(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        private static Texture2D Generate(SurfaceStyle style, Color color)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                float v = (y + 0.5f) / Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size;
                    pixels[y * Size + x] = Sample(style, color, u, v);
                }
            }
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true)
            {
                name = "Surface_" + style,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 8,
                hideFlags = HideFlags.DontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static Color Sample(SurfaceStyle style, Color c, float u, float v)
        {
            switch (style)
            {
                case SurfaceStyle.Brick: return Brick(c, u, v);
                case SurfaceStyle.WoodPlanks: return Planks(c, u, v, 8, 2, 0.020f);
                case SurfaceStyle.WoodPanel: return Planks(c, v, u, 8, 1, 0.022f);
                case SurfaceStyle.Tiles: return Tiles(c, u, v, 4, false);
                case SurfaceStyle.Checker: return Tiles(c, u, v, 4, true);
                case SurfaceStyle.Stripes: return Stripes(c, u);
                case SurfaceStyle.Carpet: return Carpet(c, u, v);
                case SurfaceStyle.Concrete: return Concrete(c, u, v);
                default: return c;
            }
        }

        // 4 colunas x 8 fiadas por repetição (tijolo de 50 x 25 cm), fiadas desencontradas.
        private static Color Brick(Color c, float u, float v)
        {
            const int cols = 4, rows = 8;
            int row = Mathf.FloorToInt(v * rows);
            float shifted = u * cols + (row % 2 == 0 ? 0f : 0.5f);
            int col = Mathf.FloorToInt(shifted) % cols;
            float fu = shifted - Mathf.Floor(shifted), fv = v * rows - row;
            const float mortarU = 0.035f, mortarV = 0.075f;
            if (fu < mortarU || fu > 1f - mortarU || fv < mortarV || fv > 1f - mortarV)
                return Color.Lerp(c, new Color(0.86f, 0.84f, 0.80f), 0.62f);
            float shade = 0.84f + 0.20f * Hash(col, row, 11);
            float warm = Hash(col, row, 29) - 0.5f;
            return Clamp(new Color(c.r * shade + warm * 0.035f, c.g * shade, c.b * shade - warm * 0.03f));
        }

        // Tábuas ao longo de "along"; cada tábua tem leve variação e emendas desencontradas.
        private static Color Planks(Color c, float along, float across, int boards, int joints, float gap)
        {
            int board = Mathf.FloorToInt(across * boards);
            float fa = across * boards - board;
            float offset = Hash(board, 0, 5);
            float pos = along * joints + offset;
            int segment = Mathf.FloorToInt(pos);
            float fs = pos - segment;
            float jointGap = gap * joints / boards * 0.5f;
            if (fa < gap * 1.6f || fa > 1f - gap * 1.6f || fs < jointGap || fs > 1f - jointGap)
                return Scale(c, 0.62f);
            float shade = 0.88f + 0.18f * Hash(board, segment % joints, 17);
            float grain = 0.985f + 0.03f * Mathf.Sin((along * 34f + Hash(board, segment % joints, 3) * 6f) * Mathf.PI);
            return Scale(c, shade * grain);
        }

        private static Color Tiles(Color c, float u, float v, int count, bool checker)
        {
            int col = Mathf.FloorToInt(u * count), row = Mathf.FloorToInt(v * count);
            float fu = u * count - col, fv = v * count - row;
            const float grout = 0.03f;
            if (fu < grout || fu > 1f - grout || fv < grout || fv > 1f - grout)
                return checker ? Scale(c, 0.70f) : Color.Lerp(c, new Color(0.90f, 0.89f, 0.86f), 0.55f);
            if (checker)
            {
                bool dark = (col + row) % 2 == 0;
                return dark ? Scale(c, 0.74f) : Color.Lerp(c, Color.white, 0.22f);
            }
            return Scale(c, 0.93f + 0.09f * Hash(col, row, 7));
        }

        private static Color Stripes(Color c, float u)
        {
            int stripe = Mathf.FloorToInt(u * 12f);
            return stripe % 2 == 0 ? c : Color.Lerp(c, Color.white, 0.22f);
        }

        private static Color Carpet(Color c, float u, float v)
        {
            int x = Mathf.FloorToInt(u * Size), y = Mathf.FloorToInt(v * Size);
            float speck = Hash(x, y, 41);
            float weave = ((x / 2 + y / 2) % 2 == 0) ? 0.985f : 1.015f;
            return Scale(c, (0.90f + 0.16f * speck) * weave);
        }

        private static Color Concrete(Color c, float u, float v)
        {
            float blotch = ValueNoise(u * 5f, v * 5f, 5) * 0.6f + ValueNoise(u * 13f, v * 13f, 13) * 0.4f;
            float fine = Hash(Mathf.FloorToInt(u * Size), Mathf.FloorToInt(v * Size), 53);
            float seam = (u < 0.006f || v < 0.006f) ? 0.80f : 1f;
            return Scale(c, (0.90f + 0.12f * blotch + 0.03f * fine) * seam);
        }

        // Ruído de valor que fecha nas bordas (period = número de células por repetição).
        private static float ValueNoise(float x, float y, int period)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(Wrap(x0, period), Wrap(y0, period), 71);
            float b = Hash(Wrap(x0 + 1, period), Wrap(y0, period), 71);
            float c = Hash(Wrap(x0, period), Wrap(y0 + 1, period), 71);
            float d = Hash(Wrap(x0 + 1, period), Wrap(y0 + 1, period), 71);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static int Wrap(int value, int period) => ((value % period) + period) % period;

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        private static Color Scale(Color c, float k) => Clamp(new Color(c.r * k, c.g * k, c.b * k));

        private static Color Clamp(Color c) => new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
    }
}
