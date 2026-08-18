using System;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.App
{
    // Tiny code-first uGUI builder so the whole game is one scripted scene — no
    // manual prefab/canvas wiring. Placeholder look; Kenney art swaps in later.
    public static class UIFactory
    {
        public static Font DefaultFont() =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas Canvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);   // portrait
            scaler.matchWidthOrHeight = 0.5f;
            return c;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static Text Label(Transform parent, string text, int size, Vector2 pos,
            Vector2 sizeDelta, Font font, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button Button(Transform parent, string text, Vector2 pos, Vector2 size,
            Font font, Action onClick)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>(); img.color = new Color(0.2f, 0.55f, 0.95f);
            var b = go.GetComponent<Button>();
            // Modern press feedback: tint on hover/press instead of a static block.
            b.transition = Selectable.Transition.ColorTint;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
            cb.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            b.colors = cb;

            // Glossy top highlight for a little depth.
            var gloss = new GameObject("Gloss", typeof(RectTransform), typeof(Image));
            var grt = gloss.GetComponent<RectTransform>(); grt.SetParent(rt, false);
            grt.anchorMin = new Vector2(0, 0.55f); grt.anchorMax = new Vector2(1, 1);
            grt.offsetMin = new Vector2(3, 0); grt.offsetMax = new Vector2(-3, -3);
            var gi = gloss.GetComponent<Image>(); gi.color = new Color(1f, 1f, 1f, 0.07f); gi.raycastTarget = false;

            b.onClick.AddListener(MTA.Battle.AudioManager.PlayClick);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            Label(rt, text, 32, Vector2.zero, size, font);
            return b;
        }

        public static void SetButtonColor(Button b, Color c)
        {
            if (b != null) b.GetComponent<Image>().color = c;
        }

        // Elemental-triangle colors (Fire/Water/Nature) for UI indicators.
        public static Color ElementColor(string element)
        {
            switch (element)
            {
                case "Fire": return new Color(0.95f, 0.45f, 0.25f);
                case "Water": return new Color(0.30f, 0.62f, 0.95f);
                case "Nature": return new Color(0.40f, 0.80f, 0.42f);
                default: return new Color(0.6f, 0.6f, 0.65f);
            }
        }

        // Small element pill (colored chip + initial). Returns its RectTransform.
        public static RectTransform ElementBadge(Transform parent, string element, Vector2 pos, float size, Font font)
        {
            var go = new GameObject("ElemBadge", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>(); img.color = ElementColor(element); img.raycastTarget = false;
            var lbl = Label(rt, string.IsNullOrEmpty(element) ? "?" : element.Substring(0, 1),
                (int)(size * 0.62f), Vector2.zero, new Vector2(size, size), font);
            lbl.color = new Color(0.08f, 0.08f, 0.1f); lbl.raycastTarget = false;
            return rt;
        }
    }
}
