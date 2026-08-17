using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    public enum BurstKind { Slash, Impact, Heal, Crit, Ultimate }

    // Pooled procedural VFX (expanding/fading quads) + pooled projectiles. No assets.
    public class BattleFx
    {
        readonly RectTransform _parent;
        readonly Stack<FxBurst> _bursts = new Stack<FxBurst>();
        readonly Stack<FxProjectile> _projs = new Stack<FxProjectile>();

        public BattleFx(RectTransform parent) { _parent = parent; }

        public void Burst(Vector2 pos, BurstKind kind)
        {
            var b = _bursts.Count > 0 ? _bursts.Pop() : FxBurst.Create(_parent);
            b.Play(pos, kind, x => _bursts.Push(x));
        }

        public void Projectile(Vector2 from, Vector2 to, Color color, Action onImpact)
        {
            var p = _projs.Count > 0 ? _projs.Pop() : FxProjectile.Create(_parent);
            p.Play(from, to, color, onImpact, x => _projs.Push(x));
        }
    }

    public class FxBurst : MonoBehaviour
    {
        Image _img; RectTransform _rt; float _age, _life, _s0, _s1; Color _col; bool _active;
        Action<FxBurst> _done;

        public static FxBurst Create(RectTransform parent)
        {
            var go = new GameObject("Fx", typeof(RectTransform), typeof(Image), typeof(FxBurst));
            var f = go.GetComponent<FxBurst>();
            f._rt = go.GetComponent<RectTransform>(); f._rt.SetParent(parent, false);
            f._img = go.GetComponent<Image>(); f._img.raycastTarget = false;
            go.SetActive(false); return f;
        }

        public void Play(Vector2 pos, BurstKind kind, Action<FxBurst> done)
        {
            _rt.anchoredPosition = pos; _done = done; _age = 0; _active = true;
            _rt.localRotation = Quaternion.identity;
            switch (kind)
            {
                case BurstKind.Slash: _col = new Color(1f, 1f, 1f, 0.9f); _rt.sizeDelta = new Vector2(170, 16); _rt.localRotation = Quaternion.Euler(0, 0, -35); _s0 = 0.3f; _s1 = 1.5f; _life = 0.22f; break;
                case BurstKind.Impact: _col = new Color(1f, 0.9f, 0.6f, 0.9f); _rt.sizeDelta = new Vector2(120, 120); _s0 = 0.2f; _s1 = 1.7f; _life = 0.25f; break;
                case BurstKind.Heal: _col = new Color(0.4f, 1f, 0.55f, 0.85f); _rt.sizeDelta = new Vector2(140, 140); _s0 = 0.2f; _s1 = 1.6f; _life = 0.4f; break;
                case BurstKind.Crit: _col = new Color(1f, 0.9f, 0.2f, 0.95f); _rt.sizeDelta = new Vector2(170, 170); _s0 = 0.2f; _s1 = 2.1f; _life = 0.3f; break;
                default: _col = new Color(1f, 0.55f, 0.12f, 0.95f); _rt.sizeDelta = new Vector2(240, 240); _s0 = 0.2f; _s1 = 2.7f; _life = 0.45f; break; // Ultimate
            }
            _img.color = _col; gameObject.SetActive(true); transform.SetAsLastSibling();
        }

        void Update()
        {
            if (!_active) return;
            _age += Time.deltaTime;
            float p = _age / _life;
            _rt.localScale = Vector3.one * Mathf.Lerp(_s0, _s1, p);
            var c = _col; c.a = _col.a * (1f - p); _img.color = c;
            if (_age >= _life) { _active = false; gameObject.SetActive(false); _done?.Invoke(this); }
        }
    }

    public class FxProjectile : MonoBehaviour
    {
        Image _img; RectTransform _rt; Vector2 _from, _to; float _age; const float Dur = 0.22f;
        Action _impact; Action<FxProjectile> _done; bool _active;

        public static FxProjectile Create(RectTransform parent)
        {
            var go = new GameObject("Proj", typeof(RectTransform), typeof(Image), typeof(FxProjectile));
            var f = go.GetComponent<FxProjectile>();
            f._rt = go.GetComponent<RectTransform>(); f._rt.SetParent(parent, false); f._rt.sizeDelta = new Vector2(34, 34);
            f._img = go.GetComponent<Image>(); f._img.raycastTarget = false;
            go.SetActive(false); return f;
        }

        public void Play(Vector2 from, Vector2 to, Color color, Action impact, Action<FxProjectile> done)
        {
            _from = from; _to = to; _impact = impact; _done = done; _age = 0; _active = true;
            _img.color = color; _rt.anchoredPosition = from; _rt.localScale = Vector3.one;
            gameObject.SetActive(true); transform.SetAsLastSibling();
        }

        void Update()
        {
            if (!_active) return;
            _age += Time.deltaTime;
            float p = Mathf.Clamp01(_age / Dur);
            _rt.anchoredPosition = Vector2.Lerp(_from, _to, p);
            if (p >= 1f) { _active = false; gameObject.SetActive(false); _impact?.Invoke(); _done?.Invoke(this); }
        }
    }
}
