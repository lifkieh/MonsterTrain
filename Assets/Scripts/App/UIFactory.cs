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
            go.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.95f);
            var b = go.GetComponent<Button>();
            b.onClick.AddListener(MTA.Battle.AudioManager.PlayClick);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            Label(rt, text, 30, Vector2.zero, size, font);
            return b;
        }

        public static void SetButtonColor(Button b, Color c)
        {
            if (b != null) b.GetComponent<Image>().color = c;
        }
    }
}
