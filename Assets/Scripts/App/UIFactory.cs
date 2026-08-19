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
            // Real CC0 Kenney 9-slice button sprite (procedural fallback keeps a gloss child).
            var btn = UiSprite("btn");
            if (btn != null) { img.sprite = btn; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 2.4f; }
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

            if (btn == null)
            {
                // Fallback glossy top highlight when no real button sprite.
                var gloss = new GameObject("Gloss", typeof(RectTransform), typeof(Image));
                var grt = gloss.GetComponent<RectTransform>(); grt.SetParent(rt, false);
                grt.anchorMin = new Vector2(0, 0.55f); grt.anchorMax = new Vector2(1, 1);
                grt.offsetMin = new Vector2(3, 0); grt.offsetMax = new Vector2(-3, -3);
                var gi = gloss.GetComponent<Image>(); gi.color = new Color(1f, 1f, 1f, 0.07f); gi.raycastTarget = false;
            }

            var punch = go.AddComponent<ButtonPunch>();
            b.onClick.AddListener(punch.Punch);
            b.onClick.AddListener(MTA.Battle.AudioManager.PlayClick);
            if (onClick != null) b.onClick.AddListener(() => onClick());
            Label(rt, text, 32, Vector2.zero, size, font);
            return b;
        }

        public static void SetButtonColor(Button b, Color c)
        {
            if (b != null) b.GetComponent<Image>().color = c;
        }

        // Cached CC0 Kenney UI sprites (Resources/Ui). Null → procedural fallback.
        static Sprite _uiBtn, _uiPanel, _uiFrame; static bool _uiTried;
        public static Sprite UiSprite(string n)
        {
            if (!_uiTried) { _uiTried = true; _uiBtn = Resources.Load<Sprite>("Ui/btn"); _uiPanel = Resources.Load<Sprite>("Ui/panel"); _uiFrame = Resources.Load<Sprite>("Ui/frame"); }
            return n == "btn" ? _uiBtn : n == "panel" ? _uiPanel : _uiFrame;
        }

        // Minimal working slider (track + fill + round handle).
        public static Slider Slider(Transform parent, Vector2 pos, Vector2 size, float value, Action<float> onChange)
        {
            var root = new GameObject("Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
            var rt = root.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = size; rt.anchoredPosition = pos;
            var bg = root.GetComponent<Image>(); bg.sprite = MTA.Battle.ProceduralArt.RoundedRect(); bg.color = new Color(0, 0, 0, 0.5f);

            var fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(rt, false); fillArea.anchorMin = new Vector2(0, 0.3f); fillArea.anchorMax = new Vector2(1, 0.7f);
            fillArea.offsetMin = new Vector2(10, 0); fillArea.offsetMax = new Vector2(-10, 0);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            fill.SetParent(fillArea, false); fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(1, 1); fill.sizeDelta = new Vector2(10, 0);
            fill.GetComponent<Image>().color = new Color(0.3f, 0.72f, 1f);

            var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(rt, false); handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            handle.SetParent(handleArea, false); handle.sizeDelta = new Vector2(size.y * 0.9f, size.y * 0.9f);
            var hImg = handle.GetComponent<Image>(); hImg.sprite = MTA.Battle.ProceduralArt.Disc(); hImg.color = Color.white;

            var slider = root.GetComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = hImg;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = value;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return slider;
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

        // Row of 5 star icons (rarity gold-filled, rest dim) — replaces the ***** text.
        public static RectTransform StarRow(Transform parent, int rarity, Vector2 pos, float starSize)
        {
            var row = new GameObject("StarRow", typeof(RectTransform));
            var rrt = row.GetComponent<RectTransform>(); rrt.SetParent(parent, false);
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f);
            float step = starSize + 2f;
            rrt.sizeDelta = new Vector2(step * 5f, starSize); rrt.anchoredPosition = pos;
            float x0 = -(5 - 1) * step / 2f;
            for (int i = 0; i < 5; i++)
            {
                var s = new GameObject("Star", typeof(RectTransform), typeof(Image));
                var srt = s.GetComponent<RectTransform>(); srt.SetParent(rrt, false);
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(starSize, starSize); srt.anchoredPosition = new Vector2(x0 + i * step, 0);
                var img = s.GetComponent<Image>(); img.sprite = MTA.Battle.ProceduralArt.Star(); img.raycastTarget = false;
                img.color = i < rarity ? new Color(1f, 0.82f, 0.28f) : new Color(0.32f, 0.32f, 0.38f, 0.85f);
            }
            return rrt;
        }
    }
}
