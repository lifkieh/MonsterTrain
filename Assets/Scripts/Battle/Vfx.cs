using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Plays a grid spritesheet (real CC0 VFX, Resources/Vfx) on a RawImage via
    // uvRect stepping. Pooled + reused; presentation only.
    public class VfxPlayer : MonoBehaviour
    {
        RawImage _img; RectTransform _rt;
        int _cols, _rows, _frames; float _fps, _t; int _cur; bool _playing;

        public bool Playing => _playing;

        public void Build(RectTransform parent)
        {
            _rt = GetComponent<RectTransform>();
            if (_rt == null) _rt = gameObject.AddComponent<RectTransform>();
            _rt.SetParent(parent, false);
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _img = gameObject.AddComponent<RawImage>(); _img.raycastTarget = false;
            gameObject.SetActive(false);
        }

        public void Play(Texture tex, int cols, int rows, int frames, float fps, Vector2 pos, float size, Color tint, float rot = 0f)
        {
            if (tex == null) return;
            _img.texture = tex; _cols = cols; _rows = rows; _frames = frames; _fps = fps;
            _img.color = tint;
            _rt.sizeDelta = new Vector2(size, size); _rt.anchoredPosition = pos; _rt.localRotation = Quaternion.Euler(0, 0, rot);
            _cur = 0; _t = 0f; _playing = true; ApplyFrame(0);
            gameObject.SetActive(true);
        }

        void ApplyFrame(int f)
        {
            int col = f % _cols, row = f / _cols;
            _img.uvRect = new Rect(col / (float)_cols, 1f - (row + 1) / (float)_rows, 1f / _cols, 1f / _rows);
        }

        void Update()
        {
            if (!_playing) return;
            _t += Time.deltaTime * _fps;
            int f = (int)_t;
            if (f >= _frames) { _playing = false; gameObject.SetActive(false); return; }
            if (f != _cur) { _cur = f; ApplyFrame(f); }
        }
    }

    // Catalog + texture cache + pool. One entry per downloaded effect sheet.
    public static class VfxCatalog
    {
        public struct Sheet { public int cols, rows, frames; public float fps; }
        static readonly Dictionary<string, Sheet> Sheets = new Dictionary<string, Sheet>
        {
            { "hit_small",  new Sheet{cols=6,rows=5,frames=30,fps=45f} },
            { "hit_impact", new Sheet{cols=6,rows=5,frames=30,fps=40f} },
            { "hit_big",    new Sheet{cols=6,rows=5,frames=30,fps=38f} },
            { "explosion",  new Sheet{cols=6,rows=5,frames=30,fps=34f} },
            { "speedlines", new Sheet{cols=6,rows=5,frames=30,fps=42f} },
            { "fire",       new Sheet{cols=6,rows=5,frames=30,fps=34f} },
            { "electric",   new Sheet{cols=6,rows=5,frames=30,fps=40f} },
            { "puff",       new Sheet{cols=7,rows=6,frames=42,fps=48f} },
        };
        static readonly Dictionary<string, Texture2D> _tex = new Dictionary<string, Texture2D>();

        public static bool Has(string id) => Sheets.ContainsKey(id) && Tex(id) != null;
        public static Sheet Info(string id) => Sheets[id];
        public static Texture2D Tex(string id)
        {
            if (_tex.TryGetValue(id, out var t)) return t;
            t = Resources.Load<Texture2D>("Vfx/" + id);
            _tex[id] = t; return t;
        }
    }

    public class VfxPool
    {
        readonly List<VfxPlayer> _pool = new List<VfxPlayer>();
        readonly RectTransform _parent; int _next;

        public VfxPool(RectTransform parent, int size)
        {
            _parent = parent;
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject("Vfx", typeof(RectTransform));
                var p = go.AddComponent<VfxPlayer>();
                p.Build(parent); _pool.Add(p);
            }
        }

        public void Play(string id, Vector2 pos, float size, Color tint, float rot = 0f)
        {
            if (!VfxCatalog.Has(id) || _pool.Count == 0) return;
            var info = VfxCatalog.Info(id);
            var p = _pool[_next]; _next = (_next + 1) % _pool.Count;
            p.transform.SetAsLastSibling();
            p.Play(VfxCatalog.Tex(id), info.cols, info.rows, info.frames, info.fps, pos, size, tint, rot);
        }

        public bool Available => VfxCatalog.Has("hit_small");
    }
}
