using System.Collections.Generic;
using UnityEngine;

namespace MTA.Battle
{
    // Runtime-generated sprites (discs, glows, triangles, rings) so the game can
    // draw real shapes instead of flat rectangles — no art assets, works on Android.
    // Sprites are white and tinted per-use via Image.color; each is cached once.
    public static class ProceduralArt
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        static Sprite Make(string key, int size, System.Func<float, float, float> alphaAt)
        {
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - c) / c, ny = (y - c) / c;      // -1..1
                    float a = Mathf.Clamp01(alphaAt(nx, ny));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = s;
            return s;
        }

        // Filled disc with a soft 6% edge.
        public static Sprite Disc() => Make("disc", 128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return Mathf.SmoothStep(1f, 0f, (r - 0.92f) / 0.08f);
        });

        // Radial glow (bright center → transparent edge), for auras and rarity glow.
        public static Sprite Glow() => Make("glow", 128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = 1f - Mathf.Clamp01(r);
            return a * a;                                          // soft falloff
        });

        // Upward-pointing triangle (horns, claws, wings, arrows).
        public static Sprite Triangle() => Make("tri", 128, (x, y) =>
        {
            // inside triangle with apex at top (0,1), base y=-1, half-width shrinking upward
            float t = (y + 1f) * 0.5f;                             // 0 at base, 1 at apex
            float halfW = Mathf.Lerp(1f, 0f, t);
            bool inside = y >= -1f && y <= 1f && Mathf.Abs(x) <= halfW;
            return inside ? 1f : 0f;
        });

        // Ring / frame outline.
        public static Sprite Ring() => Make("ring", 128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float band = 1f - Mathf.Abs(r - 0.86f) / 0.14f;
            return Mathf.Clamp01(band);
        });

        // Soft rounded square (cards / panels).
        public static Sprite RoundedRect() => Make("rrect", 96, (x, y) =>
        {
            float corner = 0.28f;
            float dx = Mathf.Max(0f, Mathf.Abs(x) - (1f - corner));
            float dy = Mathf.Max(0f, Mathf.Abs(y) - (1f - corner));
            float d = Mathf.Sqrt(dx * dx + dy * dy) / corner;
            return Mathf.SmoothStep(1f, 0f, (d - 0.9f) / 0.1f);
        });

        // Vertical gradient (opaque top → transparent bottom) for backgrounds.
        public static Sprite VGradient() => Make("vgrad", 4, (x, y) => (y + 1f) * 0.5f);
    }
}
