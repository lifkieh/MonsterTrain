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

        public void Projectile(Vector2 from, Vector2 to, Color color, Action onImpact, Sprite sprite = null)
        {
            var p = _projs.Count > 0 ? _projs.Pop() : FxProjectile.Create(_parent);
            p.Play(from, to, color, onImpact, x => _projs.Push(x), sprite);
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
            // Real generated shapes — never a bare rectangle. A slash reads as a crescent
            // arc, an impact/ultimate as a radial burst, a crit as a star, a heal as a plus.
            switch (kind)
            {
                case BurstKind.Slash: _img.sprite = ProceduralArt.Crescent(); _col = new Color(1f, 1f, 1f, 0.92f); _rt.sizeDelta = new Vector2(150, 120); _rt.localRotation = Quaternion.Euler(0, 0, -30f + UnityEngine.Random.value * 60f); _s0 = 0.3f; _s1 = 1.4f; _life = 0.2f; break;
                case BurstKind.Impact: _img.sprite = ProceduralArt.Glow(); _col = new Color(1f, 0.9f, 0.6f, 0.9f); _rt.sizeDelta = new Vector2(130, 130); _s0 = 0.25f; _s1 = 1.7f; _life = 0.25f; break;
                case BurstKind.Heal: _img.sprite = ProceduralArt.Plus(); _col = new Color(0.5f, 1f, 0.6f, 0.9f); _rt.sizeDelta = new Vector2(90, 90); _s0 = 0.3f; _s1 = 1.3f; _life = 0.4f; break;
                case BurstKind.Crit: _img.sprite = ProceduralArt.Star(); _col = new Color(1f, 0.9f, 0.2f, 0.95f); _rt.sizeDelta = new Vector2(150, 150); _rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.value * 72f); _s0 = 0.2f; _s1 = 2.0f; _life = 0.3f; break;
                default: _img.sprite = ProceduralArt.Glow(); _col = new Color(1f, 0.6f, 0.16f, 0.95f); _rt.sizeDelta = new Vector2(240, 240); _s0 = 0.2f; _s1 = 2.6f; _life = 0.45f; break; // Ultimate
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
        Image _img; RectTransform _rt; Vector2 _from, _to; float _age; float _ang; Color _color; const float Dur = 0.22f;
        Action _impact; Action<FxProjectile> _done; bool _active;

        // A short ribbon trail of fading element-shaped ghosts, so a shot reads as streaking
        // energy (fire/water/nature/lightning by shape) rather than a travelling dot.
        const int TRAIL = 9;
        Image[] _tImg; RectTransform[] _tRt; float[] _tA; int _tCur; float _dropT;

        public static FxProjectile Create(RectTransform parent)
        {
            var go = new GameObject("Proj", typeof(RectTransform), typeof(Image), typeof(FxProjectile));
            var f = go.GetComponent<FxProjectile>();
            // trail ghosts are siblings created FIRST so they render behind the head
            f._tImg = new Image[TRAIL]; f._tRt = new RectTransform[TRAIL]; f._tA = new float[TRAIL];
            for (int i = 0; i < TRAIL; i++)
            {
                var tg = new GameObject("PTrail", typeof(RectTransform), typeof(Image));
                var trt = tg.GetComponent<RectTransform>(); trt.SetParent(parent, false); trt.sizeDelta = new Vector2(40, 40);
                var ti = tg.GetComponent<Image>(); ti.raycastTarget = false; ti.color = new Color(1, 1, 1, 0);
                tg.SetActive(false); f._tImg[i] = ti; f._tRt[i] = trt;
            }
            f._rt = go.GetComponent<RectTransform>(); f._rt.SetParent(parent, false); f._rt.sizeDelta = new Vector2(54, 54);
            f._img = go.GetComponent<Image>(); f._img.raycastTarget = false; f._img.sprite = ProceduralArt.Droplet();
            go.SetActive(false); return f;
        }

        public void Play(Vector2 from, Vector2 to, Color color, Action impact, Action<FxProjectile> done, Sprite sprite = null)
        {
            _from = from; _to = to; _impact = impact; _done = done; _age = 0; _active = true; _color = color; _dropT = 0f;
            var spr = sprite != null ? sprite : ProceduralArt.Droplet();
            _img.sprite = spr; _img.color = color; _rt.anchoredPosition = from; _rt.localScale = Vector3.one;
            var d = to - from; _ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
            _rt.localRotation = Quaternion.Euler(0, 0, _ang);
            for (int i = 0; i < TRAIL; i++) { _tImg[i].sprite = spr; _tA[i] = 0f; _tRt[i].gameObject.SetActive(false); _tRt[i].localRotation = _rt.localRotation; }
            gameObject.SetActive(true); transform.SetAsLastSibling();
        }

        void Update()
        {
            if (!_active) return;
            float dt = Time.deltaTime;
            _age += dt;
            float p = Mathf.Clamp01(_age / Dur);
            Vector2 pos = Vector2.Lerp(_from, _to, p);
            pos.y += Mathf.Sin(p * Mathf.PI) * 40f;   // ballistic arc
            _rt.anchoredPosition = pos;
            _rt.localScale = Vector3.one * (1f + Mathf.Sin(p * Mathf.PI) * 0.3f);

            // drop a trail ghost at a fixed cadence, then fade all ghosts
            _dropT -= dt;
            if (_dropT <= 0f)
            {
                _dropT = 0.016f;
                var g = _tRt[_tCur]; _tImg[_tCur].color = new Color(_color.r, _color.g, _color.b, 0.55f);
                g.anchoredPosition = pos; g.localRotation = _rt.localRotation; g.localScale = _rt.localScale * 0.85f;
                g.gameObject.SetActive(true); _tA[_tCur] = 0.55f; _tCur = (_tCur + 1) % TRAIL;
            }
            for (int i = 0; i < TRAIL; i++)
            {
                if (_tA[i] <= 0f) continue;
                _tA[i] = Mathf.Max(0f, _tA[i] - dt * 4.5f);
                var c = _tImg[i].color; _tImg[i].color = new Color(c.r, c.g, c.b, _tA[i]);
                if (_tA[i] <= 0f) _tRt[i].gameObject.SetActive(false);
            }

            if (p >= 1f)
            {
                for (int i = 0; i < TRAIL; i++) { _tA[i] = 0f; _tRt[i].gameObject.SetActive(false); }   // clear the ribbon on impact
                _active = false; gameObject.SetActive(false); _impact?.Invoke(); _done?.Invoke(this);
            }
        }
    }
}
