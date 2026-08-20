using System.Collections.Generic;
using UnityEngine;

namespace MTA.Battle
{
    // Procedurally PAINTED battle backdrop (the "environment artist" — code, no new art).
    // Bakes one texture per biome: a graded sky, soft Perlin clouds, a hazy sun, and two
    // atmospheric mountain ridges (far ridge fades toward the sky = aerial perspective),
    // over a ground band at the horizon. Looks painted/layered instead of flat panels.
    // Cached once per element. Cosmetic only.
    public static class PaintedBackdrop
    {
        static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        public static Texture2D For(string element, Color skyTop, Color skyHor, Color mtn, Color ground)
        {
            string key = element ?? "def";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            const int W = 384, H = 660;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H];

            float seed = (Mathf.Abs((key.GetHashCode() % 997)) * 0.013f) + 3.1f;
            const float horizon = 0.42f;                       // sky/ground split (fraction up)
            Color cloud = Lerp(skyHor, Color.white, 0.55f);
            Color sun = SunColor(element);
            Vector2 sunPos = new Vector2(0.26f, 0.80f);        // upper-left sun
            Color mtnFar = Lerp(mtn, skyHor, 0.55f);           // hazed far ridge (aerial perspective)
            Color mtnNear = mtn;
            Color groundLow = Lerp(ground, Color.black, 0.28f);
            bool drawRidges = key != "Water";                  // Water = flat sea horizon, not mountains

            for (int y = 0; y < H; y++)
            {
                float v = y / (H - 1f);
                for (int x = 0; x < W; x++)
                {
                    float u = x / (W - 1f);
                    Color c;

                    if (v >= horizon)
                    {
                        float sv = (v - horizon) / (1f - horizon);          // 0 horizon → 1 top
                        c = Lerp(skyHor, skyTop, Mathf.SmoothStep(0f, 1f, sv));

                        // hazy sun glow
                        float sd = Vector2.Distance(new Vector2(u, v), sunPos);
                        c = Lerp(c, sun, Mathf.Clamp01(1f - sd / 0.42f) * 0.5f);

                        // soft clouds (Perlin), thicker toward the horizon band
                        float n = Perlin(u * 2.6f + seed, v * 3.4f + seed * 0.7f);
                        float band = Mathf.Clamp01(1f - Mathf.Abs(sv - 0.28f) / 0.5f);
                        float cl = Mathf.SmoothStep(0.55f, 0.82f, n) * 0.55f * band;
                        c = Lerp(c, cloud, cl);

                        if (drawRidges)
                        {
                            // two mountain ridges (near overrides far), atmospheric fade
                            float farH = horizon + 0.20f + Ridge(u, 3.1f, seed) * 0.10f;
                            float nearH = horizon + 0.10f + Ridge(u, 5.7f, seed + 9f) * 0.13f;
                            if (v < nearH) c = Shade(mtnNear, u, v, nearH);
                            else if (v < farH) c = Lerp(Shade(mtnFar, u, v, farH), c, 0.15f);
                        }
                        else if (sv < 0.06f)   // Water: a brighter sea-horizon glow line
                        {
                            c = Lerp(Lerp(skyHor, Color.white, 0.3f), c, sv / 0.06f);
                        }
                    }
                    else
                    {
                        float gv = v / horizon;                              // 0 bottom → 1 horizon
                        c = Lerp(groundLow, ground, Mathf.SmoothStep(0f, 1f, gv));
                        // faint ground undulation
                        float n = Perlin(u * 4f + seed, v * 6f);
                        c = Lerp(c, Lerp(c, Color.black, 0.12f), n * 0.4f);
                    }
                    px[y * W + x] = c;
                }
            }
            tex.SetPixels32(px); tex.Apply(false, false);
            _cache[key] = tex;
            return tex;
        }

        // A ridge silhouette: value in 0..1 from layered sines (cheap, smooth).
        static float Ridge(float u, float freq, float seed)
        {
            float a = Mathf.Sin((u * freq + seed) * 3.14159f);
            float b = Mathf.Sin((u * freq * 2.3f + seed * 1.7f) * 3.14159f);
            return (a * 0.6f + b * 0.4f) * 0.5f + 0.5f;
        }

        // Slight vertical shading inside a ridge so it isn't a flat cut-out.
        static Color Shade(Color baseC, float u, float v, float top)
        {
            float depth = Mathf.Clamp01((top - v) / 0.22f);    // deeper down the slope = darker
            return Lerp(Lerp(baseC, Color.white, 0.06f), Lerp(baseC, Color.black, 0.35f), depth);
        }

        static Color SunColor(string e)
        {
            switch (e)
            {
                case "Fire": return new Color(1f, 0.62f, 0.28f);
                case "Water": return new Color(1f, 0.95f, 0.78f);
                case "Nature": return new Color(1f, 0.97f, 0.7f);
                default: return new Color(0.9f, 0.85f, 1f);
            }
        }

        static float Perlin(float x, float y) => Mathf.PerlinNoise(x, y);
        static Color Lerp(Color a, Color b, float t) => Color.Lerp(a, b, t);
    }
}
