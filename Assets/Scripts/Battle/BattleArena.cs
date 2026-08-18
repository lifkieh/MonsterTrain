using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Procedural fighting-game arena: gradient sky, parallax silhouette layers,
    // and a ground band. Generated shapes only — no art assets. Renders behind the
    // fighters; SetParallax nudges the layers opposite the camera for depth.
    public class BattleArena
    {
        RectTransform _root, _far, _near;
        Vector2 _farHome, _nearHome;

        public void Build(RectTransform parent)
        {
            _root = Panel(parent, "Arena", new Color(0.09f, 0.10f, 0.16f), new Vector2(1200, 1700), Vector2.zero);
            _root.SetAsFirstSibling();

            // Sky gradient: stacked bands top → horizon.
            var top = new Color(0.10f, 0.12f, 0.22f);
            var hor = new Color(0.28f, 0.17f, 0.26f);
            const int bands = 10;
            for (int i = 0; i < bands; i++)
            {
                float f = i / (float)(bands - 1);
                var band = Panel(_root, "Sky", Color.Lerp(top, hor, f), new Vector2(1200, 190),
                    new Vector2(0, 760 - i * 150));
                band.GetComponent<Image>().raycastTarget = false;
            }

            // Far parallax layer: dim "mountains" (rotated diamonds on the horizon).
            _far = Layer("Far");
            var mtn = new Color(0.17f, 0.16f, 0.27f, 0.9f);
            float[] mx = { -430, -210, 40, 250, 470 };
            float[] ms = { 360, 520, 430, 600, 380 };
            for (int i = 0; i < mx.Length; i++)
                Diamond(_far, mtn, ms[i], new Vector2(mx[i], -430));

            // Near parallax layer: darker pillars flanking the arena.
            _near = Layer("Near");
            var pil = new Color(0.07f, 0.07f, 0.11f, 0.95f);
            Panel(_near, "PillarL", pil, new Vector2(120, 900), new Vector2(-520, -180));
            Panel(_near, "PillarR", pil, new Vector2(120, 900), new Vector2(520, -180));

            // Ground band + arena floor line.
            Panel(_root, "Ground", new Color(0.11f, 0.10f, 0.13f), new Vector2(1200, 520), new Vector2(0, -700));
            Panel(_root, "Floor", new Color(0.22f, 0.20f, 0.26f), new Vector2(980, 10), new Vector2(0, -430));

            _farHome = _far.anchoredPosition; _nearHome = _near.anchoredPosition;
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
