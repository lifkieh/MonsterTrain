using MTA.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Procedurally assembles a recognizable monster portrait from species + element
    // + role using ProceduralArt shapes. Deterministic per species (no two alike),
    // element-themed palette + aura, role-shaped body. Presentation only.
    public static class MonsterArt
    {
        static ulong Hash(string s)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < s.Length; i++) h = (h ^ (byte)s[i]) * 1099511628211UL;
            return h;
        }

        static void Palette(string element, ulong h, out Color body, out Color aura, out Color accent)
        {
            Color baseCol, auraCol;
            switch (element)
            {
                case "Fire": baseCol = new Color(0.90f, 0.34f, 0.20f); auraCol = new Color(1f, 0.55f, 0.18f); break;
                case "Water": baseCol = new Color(0.24f, 0.55f, 0.92f); auraCol = new Color(0.35f, 0.72f, 1f); break;
                case "Nature": baseCol = new Color(0.34f, 0.70f, 0.36f); auraCol = new Color(0.45f, 0.92f, 0.42f); break;
                default: baseCol = new Color(0.6f, 0.6f, 0.66f); auraCol = new Color(0.7f, 0.7f, 0.8f); break;
            }
            // Blend toward the species' unique hue so every monster differs within its element.
            var sc = SpeciesIdentity.ColorFor(element + s(h));
            var spHue = new Color(sc.r, sc.g, sc.b);
            body = Color.Lerp(baseCol, spHue, 0.32f);
            aura = auraCol;
            accent = new Color(body.r * 0.55f, body.g * 0.55f, body.b * 0.55f);
        }

        static string s(ulong h) => (h % 997).ToString();

        static Image Img(Transform parent, Sprite sp, Color col, Vector2 size, Vector2 pos, float rot = 0f)
        {
            var go = new GameObject("part", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos; rt.localRotation = Quaternion.Euler(0, 0, rot);
            var img = go.GetComponent<Image>(); img.sprite = sp; img.color = col; img.raycastTarget = false;
            return img;
        }

        public static RectTransform Build(RectTransform parent, string speciesId, string element, string role, float size)
        {
            var go = new GameObject("MonsterArt", typeof(RectTransform));
            var root = go.GetComponent<RectTransform>(); root.SetParent(parent, false);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(size, size); root.anchoredPosition = Vector2.zero;

            ulong h = Hash(speciesId);
            Palette(element, h, out var body, out var aura, out var accent);

            int horns = (int)(h % 3);                 // 0..2
            bool wings = ((h >> 3) & 1) == 1;
            bool tail = ((h >> 5) & 1) == 1;
            bool claws = ((h >> 7) & 1) == 1;
            bool oneEye = ((h >> 9) & 3) == 0;        // ~25% cyclops
            float bodyW = 0.62f, bodyH = 0.62f;
            switch (role)
            {
                case "Tank": bodyW = 0.74f; bodyH = 0.66f; break;
                case "Assassin": bodyW = 0.50f; bodyH = 0.66f; break;
                case "Mage": bodyW = 0.60f; bodyH = 0.60f; break;
                case "Support": bodyW = 0.58f; bodyH = 0.58f; break;
            }

            var disc = ProceduralArt.Disc();
            var glow = ProceduralArt.Glow();
            var tri = ProceduralArt.Triangle();

            // aura (behind everything)
            Img(root, glow, new Color(aura.r, aura.g, aura.b, 0.35f), new Vector2(size * 1.05f, size * 1.05f), Vector2.zero);

            // wings (behind body)
            if (wings)
            {
                var wc = new Color(aura.r, aura.g, aura.b, 0.85f);
                Img(root, tri, wc, new Vector2(size * 0.42f, size * 0.5f), new Vector2(-size * 0.28f, size * 0.05f), 55f);
                Img(root, tri, wc, new Vector2(size * 0.42f, size * 0.5f), new Vector2(size * 0.28f, size * 0.05f), -55f);
            }

            // tail
            if (tail)
                Img(root, tri, accent, new Vector2(size * 0.16f, size * 0.34f), new Vector2(size * 0.26f, -size * 0.28f), 200f);

            // body + belly
            Img(root, disc, body, new Vector2(size * bodyW, size * bodyH), new Vector2(0, -size * 0.02f));
            Img(root, disc, new Color(body.r * 1.25f, body.g * 1.25f, body.b * 1.25f, 0.55f),
                new Vector2(size * bodyW * 0.55f, size * bodyH * 0.5f), new Vector2(0, -size * 0.08f));

            // horns
            var hornCol = new Color(0.95f, 0.92f, 0.85f);
            if (horns >= 1) Img(root, tri, hornCol, new Vector2(size * 0.12f, size * 0.2f), new Vector2(-size * 0.13f, size * 0.22f), -18f);
            if (horns >= 2) Img(root, tri, hornCol, new Vector2(size * 0.12f, size * 0.2f), new Vector2(size * 0.13f, size * 0.22f), 18f);

            // eyes
            float eyeY = size * 0.05f;
            if (oneEye)
            {
                Img(root, disc, Color.white, new Vector2(size * 0.2f, size * 0.2f), new Vector2(0, eyeY));
                Img(root, disc, new Color(0.1f, 0.1f, 0.14f), new Vector2(size * 0.09f, size * 0.09f), new Vector2(0, eyeY));
            }
            else
            {
                float ex = size * 0.13f;
                Img(root, disc, Color.white, new Vector2(size * 0.16f, size * 0.16f), new Vector2(-ex, eyeY));
                Img(root, disc, Color.white, new Vector2(size * 0.16f, size * 0.16f), new Vector2(ex, eyeY));
                Img(root, disc, new Color(0.1f, 0.1f, 0.14f), new Vector2(size * 0.07f, size * 0.07f), new Vector2(-ex, eyeY));
                Img(root, disc, new Color(0.1f, 0.1f, 0.14f), new Vector2(size * 0.07f, size * 0.07f), new Vector2(ex, eyeY));
            }

            // claws (bottom)
            if (claws)
            {
                Img(root, tri, hornCol, new Vector2(size * 0.09f, size * 0.14f), new Vector2(-size * 0.18f, -size * 0.26f), 180f);
                Img(root, tri, hornCol, new Vector2(size * 0.09f, size * 0.14f), new Vector2(size * 0.18f, -size * 0.26f), 180f);
            }

            return root;
        }
    }
}
