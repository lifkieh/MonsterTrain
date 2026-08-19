using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Pooled floating combat text: pops in (scale 1.4→1.0), rises, holds then fades,
    // recycles itself. Crits are bigger (caller-set size), shake, and live longer;
    // light hits are smaller and shorter-lived so crits visually dominate.
    public class FloatingTextPool
    {
        readonly RectTransform _parent;
        readonly Font _font;
        readonly Stack<FloatingCombatText> _free = new Stack<FloatingCombatText>();

        public FloatingTextPool(RectTransform parent, Font font) { _parent = parent; _font = font; }

        public void Spawn(Vector2 pos, string text, Color color, int size) => Spawn(pos, text, color, size, 0.9f, false);

        public void Spawn(Vector2 pos, string text, Color color, int size, float life, bool crit)
        {
            var f = _free.Count > 0 ? _free.Pop() : FloatingCombatText.Create(_parent, _font);
            f.Show(pos, text, color, size, life, crit, Recycle);
        }

        void Recycle(FloatingCombatText f) => _free.Push(f);
    }

    public class FloatingCombatText : MonoBehaviour
    {
        Text _t; RectTransform _rt; float _age, _life; Vector2 _start; bool _active, _crit; Color _baseColor;
        Action<FloatingCombatText> _onDone;

        public static FloatingCombatText Create(RectTransform parent, Font font)
        {
            var go = new GameObject("FloatText", typeof(RectTransform), typeof(Text), typeof(FloatingCombatText));
            var f = go.GetComponent<FloatingCombatText>();
            f._rt = go.GetComponent<RectTransform>();
            f._rt.SetParent(parent, false);
            f._rt.sizeDelta = new Vector2(240, 44);
            f._t = go.GetComponent<Text>();
            f._t.font = font; f._t.alignment = TextAnchor.MiddleCenter; f._t.fontStyle = FontStyle.Bold;
            f._t.horizontalOverflow = HorizontalWrapMode.Overflow; f._t.verticalOverflow = VerticalWrapMode.Overflow;
            f._t.raycastTarget = false;
            go.SetActive(false);
            return f;
        }

        public void Show(Vector2 pos, string text, Color color, int size, Action<FloatingCombatText> onDone)
            => Show(pos, text, color, size, 0.9f, false, onDone);

        public void Show(Vector2 pos, string text, Color color, int size, float life, bool crit, Action<FloatingCombatText> onDone)
        {
            _start = pos; _rt.anchoredPosition = pos;
            _t.text = text; _baseColor = color; _t.color = color; _t.fontSize = size;
            _age = 0; _life = Mathf.Max(0.2f, life); _crit = crit; _onDone = onDone; _active = true;
            _rt.localScale = Vector3.one * 1.4f;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Update()
        {
            if (!_active) return;
            _age += Time.deltaTime;
            float p = _age / _life;
            float pop = Mathf.Lerp(1.4f, 1f, Mathf.Clamp01(_age / 0.15f));   // pop-in scale
            float shakeX = _crit ? Mathf.Sin(_age * 70f) * 3f * (1f - p) : 0f;
            _rt.localScale = Vector3.one * pop;
            _rt.anchoredPosition = _start + new Vector2(shakeX, 60f * p);      // rise ~60 px
            var c = _baseColor; c.a = Mathf.Clamp01(1f - p * p); _t.color = c; // hold, then fade out
            if (_age >= _life)
            {
                _active = false;
                gameObject.SetActive(false);
                _onDone?.Invoke(this);
            }
        }
    }
}
