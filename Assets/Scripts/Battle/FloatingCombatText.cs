using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Pooled floating combat text: rises, fades, recycles itself back to the pool.
    public class FloatingTextPool
    {
        readonly RectTransform _parent;
        readonly Font _font;
        readonly Stack<FloatingCombatText> _free = new Stack<FloatingCombatText>();

        public FloatingTextPool(RectTransform parent, Font font) { _parent = parent; _font = font; }

        public void Spawn(Vector2 pos, string text, Color color, int size)
        {
            var f = _free.Count > 0 ? _free.Pop() : FloatingCombatText.Create(_parent, _font);
            f.Show(pos, text, color, size, Recycle);
        }

        void Recycle(FloatingCombatText f) => _free.Push(f);
    }

    public class FloatingCombatText : MonoBehaviour
    {
        Text _t; RectTransform _rt; float _age, _life; Vector2 _start; bool _active;
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
        {
            _start = pos; _rt.anchoredPosition = pos;
            _t.text = text; _t.color = color; _t.fontSize = size;
            _age = 0; _life = 0.9f; _onDone = onDone; _active = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        void Update()
        {
            if (!_active) return;
            _age += Time.deltaTime;
            float p = _age / _life;
            _rt.anchoredPosition = _start + new Vector2(0f, 70f * p);
            var c = _t.color; c.a = Mathf.Clamp01(1f - p); _t.color = c;
            if (_age >= _life)
            {
                _active = false;
                gameObject.SetActive(false);
                _onDone?.Invoke(this);
            }
        }
    }
}
