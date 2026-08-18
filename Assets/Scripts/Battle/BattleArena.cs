using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Procedural element-themed arena: gradient sky, parallax silhouette layers,
    // ground, and drifting ambient particles (fire embers / water motes / nature
    // leaves). Generated shapes only — no art assets. Presentation only.
    public class BattleArena
    {
        RectTransform _root, _far, _near;
        Vector2 _farHome, _nearHome;
        RectTransform[] _parts; Vector2[] _vel; float _drift;

        public void Build(RectTransform parent, string element)
        {
            Theme(element, out var skyTop, out var skyHor, out var mtnCol, out var groundCol, out var floorCol, out var partCol, out var partDir);

            _root = Panel(parent, "Arena", skyTop, new Vector2(1200, 1700), Vector2.zero);
            _root.SetAsFirstSibling();

            // Real CC0 forest panorama backdrop, tinted per element; procedural fallback.
            var backdrop = Resources.Load<Texture2D>("Arena/forest");
            if (backdrop != null)
            {
                var go = new GameObject("Backdrop", typeof(RectTransform), typeof(RawImage));
                var rt = go.GetComponent<RectTransform>(); rt.SetParent(_root, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(1320, 1240); rt.anchoredPosition = new Vector2(0, 200);
                var img = go.GetComponent<RawImage>(); img.texture = backdrop; img.raycastTarget = false;
                img.uvRect = new Rect(0.12f, 0f, 0.42f, 1f);   // frame a slice of the panorama
                img.color = element == "Fire" ? new Color(1f, 0.72f, 0.55f)
                          : element == "Water" ? new Color(0.7f, 0.85f, 1.05f)
                          : element == "Nature" ? new Color(1f, 1f, 1f)
                          : new Color(0.85f, 0.8f, 0.95f);
            }
            else
            {
                const int bands = 12;
                for (int i = 0; i < bands; i++)
                {
                    float f = i / (float)(bands - 1);
                    Panel(_root, "Sky", Color.Lerp(skyTop, skyHor, f), new Vector2(1200, 165), new Vector2(0, 780 - i * 145));
                }
                _far = Layer("Far");
                var mtn = new Color(mtnCol.r, mtnCol.g, mtnCol.b, 0.9f);
                float[] mx = { -430, -210, 40, 250, 470 };
                float[] ms = { 360, 520, 430, 600, 380 };
                for (int i = 0; i < mx.Length; i++) Diamond(_far, mtn, ms[i], new Vector2(mx[i], -430));
            }

            _near = Layer("Near");
            var pil = new Color(floorCol.r * 0.6f, floorCol.g * 0.6f, floorCol.b * 0.6f, 0.95f);
            Panel(_near, "PillarL", pil, new Vector2(120, 900), new Vector2(-520, -180));
            Panel(_near, "PillarR", pil, new Vector2(120, 900), new Vector2(520, -180));

            Panel(_root, "Ground", groundCol, new Vector2(1200, 520), new Vector2(0, -700));
            var floor = Panel(_root, "Floor", floorCol, new Vector2(980, 10), new Vector2(0, -430));
            floor.GetComponent<Image>().color = new Color(floorCol.r * 1.6f, floorCol.g * 1.6f, floorCol.b * 1.6f);

            // Ambient particles.
            int n = 16;
            _parts = new RectTransform[n]; _vel = new Vector2[n];
            var seed = new System.Random(element == null ? 0 : element.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float sz = 14f + (float)seed.NextDouble() * 26f;
                var p = Panel(_root, "Ambient", new Color(partCol.r, partCol.g, partCol.b, 0.28f), new Vector2(sz, sz),
                    new Vector2((float)(seed.NextDouble() * 1100 - 550), (float)(seed.NextDouble() * 1400 - 700)));
                p.GetComponent<Image>().sprite = ProceduralArt.Glow();
                _parts[i] = p;
                _vel[i] = new Vector2(((float)seed.NextDouble() - 0.5f) * 22f, partDir * (10f + (float)seed.NextDouble() * 26f));
            }

            _farHome = _far != null ? _far.anchoredPosition : Vector2.zero;
            _nearHome = _near != null ? _near.anchoredPosition : Vector2.zero;
        }

        static void Theme(string e, out Color skyTop, out Color skyHor, out Color mtn, out Color ground, out Color floor, out Color part, out float partDir)
        {
            switch (e)
            {
                case "Fire":
                    skyTop = new Color(0.16f, 0.07f, 0.06f); skyHor = new Color(0.45f, 0.16f, 0.10f);
                    mtn = new Color(0.30f, 0.12f, 0.10f); ground = new Color(0.16f, 0.08f, 0.06f);
                    floor = new Color(0.16f, 0.10f, 0.09f); part = new Color(1f, 0.55f, 0.2f); partDir = 1f; break;   // embers rise
                case "Water":
                    skyTop = new Color(0.05f, 0.10f, 0.20f); skyHor = new Color(0.10f, 0.28f, 0.42f);
                    mtn = new Color(0.11f, 0.20f, 0.34f); ground = new Color(0.07f, 0.11f, 0.17f);
                    floor = new Color(0.10f, 0.14f, 0.20f); part = new Color(0.5f, 0.8f, 1f); partDir = 0.6f; break;   // motes drift up
                case "Nature":
                    skyTop = new Color(0.07f, 0.13f, 0.09f); skyHor = new Color(0.16f, 0.30f, 0.16f);
                    mtn = new Color(0.13f, 0.24f, 0.14f); ground = new Color(0.09f, 0.13f, 0.09f);
                    floor = new Color(0.12f, 0.17f, 0.11f); part = new Color(0.5f, 0.9f, 0.45f); partDir = -1f; break; // leaves fall
                default:
                    skyTop = new Color(0.10f, 0.12f, 0.22f); skyHor = new Color(0.28f, 0.17f, 0.26f);
                    mtn = new Color(0.17f, 0.16f, 0.27f); ground = new Color(0.11f, 0.10f, 0.13f);
                    floor = new Color(0.22f, 0.20f, 0.26f); part = new Color(0.7f, 0.7f, 0.85f); partDir = 0.5f; break;
            }
        }

        public void Tick(float dt)
        {
            if (_parts == null) return;
            _drift += dt;
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null) continue;
                var p = _parts[i].anchoredPosition + _vel[i] * dt;
                p.x += Mathf.Sin(_drift + i) * 6f * dt;
                if (p.y > 760f) p.y = -700f; else if (p.y < -700f) p.y = 760f;
                _parts[i].anchoredPosition = p;
            }
        }

        public void SetParallax(Vector2 cam)
        {
            if (_far != null) _far.anchoredPosition = _farHome - cam * 0.03f;
            if (_near != null) _near.anchoredPosition = _nearHome - cam * 0.09f;
        }

        public void Destroy() { if (_root != null) Object.Destroy(_root.gameObject); }

        RectTransform Layer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_root, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        static RectTransform Panel(RectTransform parent, string name, Color c, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = size; rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
            return rt;
        }

        static void Diamond(RectTransform parent, Color c, float size, Vector2 pos)
        {
            var rt = Panel(parent, "Mtn", c, new Vector2(size, size), pos);
            rt.localRotation = Quaternion.Euler(0, 0, 45f);
        }
    }
}
