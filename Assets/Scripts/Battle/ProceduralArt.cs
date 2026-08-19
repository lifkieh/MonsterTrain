using System.Collections.Generic;
using UnityEngine;

namespace MTA.Battle
{
    // Runtime-generated PIXEL-ART shapes (discs, glows, triangles, rings, stars) so the
    // game draws real shapes that match the 64px pixel monster sprites — no smoothing.
    // Art direction A (Pokémon-style pixel art): low internal resolution, POINT filter,
    // and a hard 3-step alpha edge so every generated shape reads as chunky pixels. White,
    // tinted per-use via Image.color; cached once. (See docs/STYLE_GUIDE.md.)
    public static class ProceduralArt
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        static Sprite Make(string key, int size, System.Func<float, float, float> alphaAt)
        {
            if (_cache.TryGetValue(key, out var s) && s != null) return s;
            // Point filter + no mipmaps = crisp pixels, consistent with the pixel monsters.
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - c) / c, ny = (y - c) / c;      // -1..1
                    float a = Mathf.Clamp01(alphaAt(nx, ny));
                    a = a < 0.30f ? 0f : (a < 0.72f ? 0.5f : 1f);  // hard 3-step edge → chunky pixel outline
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = s;
            return s;
        }

        // Filled disc.
        public static Sprite Disc() => Make("disc", 32, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return Mathf.SmoothStep(1f, 0f, (r - 0.90f) / 0.10f);
        });

        // Radial "glow" — quantized to a hard disc with a one-step falloff (pixel-safe).
        public static Sprite Glow() => Make("glow", 32, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return 1f - Mathf.Clamp01(r);
        });

        // Upward-pointing triangle (horns, claws, debris).
        public static Sprite Triangle() => Make("tri", 32, (x, y) =>
        {
            float t = (y + 1f) * 0.5f;
            float halfW = Mathf.Lerp(1f, 0f, t);
            bool inside = y >= -1f && y <= 1f && Mathf.Abs(x) <= halfW;
            return inside ? 1f : 0f;
        });

        // Filled 5-point star (rarity icon), one point straight up.
        public static Sprite Star() => Make("star", 32, (x, y) =>
        {
            const float outer = 0.95f, inner = 0.42f;
            float ang = Mathf.Atan2(y, x) + Mathf.PI / 2f;
            float sector = Mathf.PI / 5f;
            float idx = Mathf.Floor(ang / sector);
            float aLocal = (ang - idx * sector) / sector;
            float r0 = Mathf.Repeat(idx, 2f) < 1f ? outer : inner;
            float r1 = Mathf.Repeat(idx + 1f, 2f) < 1f ? outer : inner;
            float edge = Mathf.Lerp(r0, r1, aLocal);
            float r = Mathf.Sqrt(x * x + y * y);
            return r <= edge ? 1f : 0f;
        });

        // Ring / frame outline.
        public static Sprite Ring() => Make("ring", 32, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float band = 1f - Mathf.Abs(r - 0.82f) / 0.18f;
            return Mathf.Clamp01(band);
        });

        // Rounded square (cards / panels) — chunky pixel-beveled corners.
        public static Sprite RoundedRect() => Make("rrect", 48, (x, y) =>
        {
            float corner = 0.30f;
            float dx = Mathf.Max(0f, Mathf.Abs(x) - (1f - corner));
            float dy = Mathf.Max(0f, Mathf.Abs(y) - (1f - corner));
            float d = Mathf.Sqrt(dx * dx + dy * dy) / corner;
            return d <= 1f ? 1f : 0f;
        });

        // Vertical gradient (opaque top → transparent bottom) — low-res, quantized to bands.
        public static Sprite VGradient() => Make("vgrad", 8, (x, y) => (y + 1f) * 0.5f);

        // ---- Element-signature silhouettes (Final Combat Presentation pass) ----
        // Each shape is recognisable from its outline alone, so the player reads the
        // element without relying on colour. Point-filtered chunky pixels like the rest.

        // FIRE: a flame tongue — rounded bulb at the base tapering to a wavy point.
        public static Sprite Flame() => Make("flame", 32, (x, y) =>
        {
            float bx = x, by = y + 0.34f;
            bool bulb = (bx * bx * 1.35f + by * by) <= 0.60f * 0.60f;          // base bulb
            float u = Mathf.InverseLerp(-0.34f, 1f, y);                        // 0 base → 1 tip
            float halfW = 0.58f * Mathf.Pow(Mathf.Clamp01(1f - u), 0.72f);
            halfW *= 1f + 0.13f * Mathf.Sin(u * 9f);                           // flame wiggle
            bool tongue = y >= -0.34f && Mathf.Abs(x) <= halfW;
            return (bulb || tongue) ? 1f : 0f;
        });

        // NATURE: a leaf — pointed lens, gently skewed so it reads organic.
        public static Sprite Leaf() => Make("leaf", 32, (x, y) =>
        {
            float halfW = 0.52f * (1f - y * y);                               // pointed at y = ±1
            float xs = x - y * 0.13f;                                          // slight skew
            return Mathf.Abs(xs) <= halfW ? 1f : 0f;
        });

        // LIGHTNING: a jagged vertical bolt (triangle-wave centreline).
        public static Sprite Bolt() => Make("bolt", 32, (x, y) =>
        {
            float zig = Mathf.PingPong((y + 1f) * 2.2f, 1f) * 0.9f - 0.45f;    // zigzag
            return Mathf.Abs(x - zig) <= 0.17f ? 1f : 0f;
        });

        // HEAL: a plus / cross — unmistakable "restore" glyph.
        public static Sprite Plus() => Make("plus", 32, (x, y) =>
        {
            bool v = Mathf.Abs(x) <= 0.27f && Mathf.Abs(y) <= 0.88f;
            bool h = Mathf.Abs(y) <= 0.27f && Mathf.Abs(x) <= 0.88f;
            return (v || h) ? 1f : 0f;
        });

        // WATER / slash: a crescent (big disc minus an offset disc) — reads as a
        // water wave-slash or a claw arc depending on rotation.
        public static Sprite Crescent() => Make("crescent", 40, (x, y) =>
        {
            bool outer = (x * x + y * y) <= 0.95f * 0.95f;
            float ix = x - 0.42f, iy = y + 0.06f;
            bool cut = (ix * ix + iy * iy) <= 0.82f * 0.82f;
            return (outer && !cut) ? 1f : 0f;
        });

        // WATER: a droplet — round belly, pointed top (teardrop).
        public static Sprite Droplet() => Make("droplet", 32, (x, y) =>
        {
            bool belly = (x * x * 1.2f + (y + 0.28f) * (y + 0.28f)) <= 0.58f * 0.58f;
            float u = Mathf.InverseLerp(-0.28f, 1f, y);
            bool tip = y >= -0.28f && Mathf.Abs(x) <= 0.52f * Mathf.Pow(Mathf.Clamp01(1f - u), 1.1f);
            return (belly || tip) ? 1f : 0f;
        });
    }
}
