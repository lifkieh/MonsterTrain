using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // One monster's placeholder visual: colored panel + name + HP bar. Built in
    // code by BattleReplayView; no prefab wiring. Renders only — never computes.
    public class UnitView : MonoBehaviour
    {
        Image _panel;
        Image _hpFill;
        Text _name;
        RectTransform _rt;
        int _maxHp = 1;
        bool _dead;

        public void Build(RectTransform parent, Vector2 anchoredPos, Vector2 size,
            Color color, Font font, string label)
        {
            var go = new GameObject("Unit_" + label, typeof(RectTransform), typeof(Image));
            _rt = go.GetComponent<RectTransform>();
            _rt.SetParent(parent, false);
            _rt.sizeDelta = size;
            _rt.anchoredPosition = anchoredPos;
            _panel = go.GetComponent<Image>();
            _panel.color = color;

            _name = MakeText(_rt, font, label, 18, new Vector2(0, size.y * 0.5f - 4), TextAnchor.UpperCenter);

            // HP bar background + fill along the bottom.
            var bg = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
            var bgrt = bg.GetComponent<RectTransform>();
            bgrt.SetParent(_rt, false);
            bgrt.sizeDelta = new Vector2(size.x - 12, 12);
            bgrt.anchoredPosition = new Vector2(0, -size.y * 0.5f + 12);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var fill = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            _hpFill = fill.GetComponent<Image>();
            _hpFill.color = new Color(0.3f, 0.9f, 0.3f, 1f);
            var frt = _hpFill.rectTransform;
            frt.SetParent(bgrt, false);
            frt.anchorMin = new Vector2(0, 0);
            frt.anchorMax = new Vector2(1, 1);
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillOrigin = 0;
            _hpFill.fillAmount = 1f;
        }

        public void SetMaxHp(int maxHp) => _maxHp = Mathf.Max(1, maxHp);

        public void SetHp(int currentHp)
        {
            if (_hpFill != null) _hpFill.fillAmount = Mathf.Clamp01(currentHp / (float)_maxHp);
            if (currentHp <= 0 && !_dead) MarkDead();
        }

        public void FlashDamage(int amount, bool crit, bool heal, Font font)
        {
            if (_rt == null) return;
            var t = MakeText(_rt, font, (heal ? "+" : "-") + amount + (crit ? "!" : ""),
                crit ? 24 : 18, new Vector2(0, 0), TextAnchor.MiddleCenter);
            t.color = heal ? new Color(0.4f, 1f, 0.4f) : (crit ? new Color(1f, 0.85f, 0.2f) : Color.white);
            var fl = t.gameObject.AddComponent<FloatingText>();
            fl.Init(t);
        }

        public void MarkDead()
        {
            _dead = true;
            if (_panel != null) _panel.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            if (_name != null) _name.color = new Color(1f, 1f, 1f, 0.4f);
        }

        static Text MakeText(RectTransform parent, Font font, string s, int size,
            Vector2 pos, TextAnchor anchor)
        {
            var go = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(180, 30);
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = s;
            t.fontSize = size;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }
    }

    // Tiny float-up-and-fade for damage numbers. Self-destructs.
    public class FloatingText : MonoBehaviour
    {
        Text _t;
        float _age;
        const float Life = 0.8f;

        public void Init(Text t) => _t = t;

        void Update()
        {
            _age += Time.deltaTime;
            if (_t != null)
            {
                _t.rectTransform.anchoredPosition += new Vector2(0, 40f * Time.deltaTime);
                var c = _t.color; c.a = Mathf.Clamp01(1f - _age / Life); _t.color = c;
            }
            if (_age >= Life) Destroy(gameObject);
        }
    }
}
