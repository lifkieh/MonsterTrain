using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    public enum ElemFx { Cast, Impact, Ultimate, Heal }

    // Element-signature battle VFX (Final Combat Presentation pass).
    //
    // Emits SHAPE-DISTINCT procedural particles so a player reads "fire / water /
    // nature / lightning / heal" from the silhouette alone — never from a coloured box
    // or a plain flash. Fire = rising flame tongues + embers + scorch; Water = a
    // wave-slash crescent + arcing droplets + mist; Nature = tumbling leaves + pollen;
    // Lightning = jagged bolts + sparks; Heal = rising plus-crosses + an aura ring.
    //
    // Pooled parallel arrays, zero per-spawn allocation. Cosmetic RNG only
    // (UnityEngine.Random) — never touches the deterministic sim. Presentation only.
    public class ElementVfx
    {
        const int N = 128;
        readonly Image[] _img = new Image[N];
        readonly RectTransform[] _rt = new RectTransform[N];
        readonly float[] _age = new float[N], _life = new float[N], _s0 = new float[N], _s1 = new float[N];
        readonly float[] _grav = new float[N], _rot = new float[N], _rotV = new float[N];
        readonly Vector2[] _vel = new Vector2[N];
        readonly Color[] _col = new Color[N];
        readonly int[] _mode = new int[N];
        int _cur;

        public ElementVfx(RectTransform parent)
        {
            for (int i = 0; i < N; i++)
            {
                var go = new GameObject("EVfx", typeof(RectTransform), typeof(Image));
                var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                var img = go.GetComponent<Image>(); img.raycastTarget = false; img.color = new Color(1, 1, 1, 0);
                go.SetActive(false);
                _img[i] = img; _rt[i] = rt;
            }
        }

        // Element/kind → a composed burst of signature particles at a stage-local position.
        public void Burst(string element, Vector2 pos, float size, ElemFx kind)
        {
            float u = kind == ElemFx.Ultimate ? 1.7f : kind == ElemFx.Cast ? 0.9f : 1f;   // energy scale
            switch (Norm(element, kind))
            {
                case "Fire":    Fire(pos, size, u, kind); break;
                case "Water":   Water(pos, size, u, kind); break;
                case "Nature":  Nature(pos, size, u, kind); break;
                case "Lightning": Lightning(pos, size, u, kind); break;
                case "Heal":    Heal(pos, size, u); break;
                default:        Neutral(pos, size, u); break;
            }
        }

        static string Norm(string element, ElemFx kind)
        {
            if (kind == ElemFx.Heal) return "Heal";
            if (element == "Fire" || element == "Water" || element == "Nature" || element == "Lightning") return element;
            return "Neutral";
        }

        // ---- per-element compositions ----

        void Fire(Vector2 p, float sz, float u, ElemFx k)
        {
            var core = new Color(1f, 0.86f, 0.34f); var mid = new Color(1f, 0.5f, 0.15f);
            var scorch = new Color(0.14f, 0.06f, 0.04f, 0.7f);
            // scorch mark on the ground (static, fades)
            Emit(ProceduralArt.Disc(), p, sz * 0.9f * u, Vector2.zero, 0f, 1.0f, 1.3f, 0.5f, 0f, 2, scorch);
            int tongues = k == ElemFx.Ultimate ? 6 : 4;
            for (int i = 0; i < tongues; i++)                                   // flame tongues rise & grow
                Emit(ProceduralArt.Flame(), p + R2(24 * u, 6), sz * 0.7f * u,
                    new Vector2(R1(30), 120f + R0(90f)), -20f, 0.7f, 1.5f, 0.34f, R1(60), 3, Lerp(core, mid, R0(1f)));
            int embers = k == ElemFx.Ultimate ? 12 : 7;
            for (int i = 0; i < embers; i++)                                    // embers spray up + out, gravity
                Emit(ProceduralArt.Triangle(), p + R2(16, 8), sz * 0.18f,
                    new Vector2(R1(150), 120f + R0(180f)), 210f, 0.6f, 1.0f, 0.55f, R1(400), 0, core);
            if (k == ElemFx.Ultimate) Emit(ProceduralArt.Ring(), p, sz * 0.6f, Vector2.zero, 0f, 0.3f, 7f, 0.4f, 0f, 1, mid);
        }

        void Water(Vector2 p, float sz, float u, ElemFx k)
        {
            var foam = new Color(0.92f, 0.97f, 1f); var mid = new Color(0.4f, 0.72f, 1f);
            var deep = new Color(0.3f, 0.55f, 0.95f, 0.5f);
            // wave-slash crescent (quick), random tilt
            int c = Emit(ProceduralArt.Crescent(), p, sz * 1.5f * u, Vector2.zero, 0f, 0.5f, 1.5f, 0.24f, 0f, 2, foam);
            if (c >= 0) _rt[c].localRotation = Quaternion.Euler(0, 0, R1(70));
            int drops = k == ElemFx.Ultimate ? 12 : 7;
            for (int i = 0; i < drops; i++)                                     // droplets arc out & fall
            {
                int d = Emit(ProceduralArt.Droplet(), p + R2(12, 8), sz * 0.24f,
                    new Vector2(R1(180), 140f + R0(170f)), 300f, 0.7f, 1.0f, 0.5f, 0f, 4, Lerp(foam, mid, R0(1f)));
                if (d >= 0) _rt[d].localRotation = Quaternion.Euler(0, 0, R1(30));
            }
            for (int i = 0; i < 3; i++)                                          // mist bloom
                Emit(ProceduralArt.Glow(), p + R2(20, 10), sz * 0.7f * u, new Vector2(R1(40), R0(30f)), 0f, 0.6f, 1.8f, 0.4f, 0f, 1, deep);
        }

        void Nature(Vector2 p, float sz, float u, ElemFx k)
        {
            var leafA = new Color(0.5f, 0.9f, 0.4f); var leafB = new Color(0.3f, 0.7f, 0.25f);
            var pollen = new Color(0.92f, 1f, 0.55f);
            Emit(ProceduralArt.Ring(), p, sz * 0.55f * u, Vector2.zero, 0f, 0.3f, 6.5f, 0.32f, 0f, 1, leafA);   // vine burst ring
            int leaves = k == ElemFx.Ultimate ? 12 : 7;
            for (int i = 0; i < leaves; i++)                                    // leaves spin outward, float down
            {
                int l = Emit(ProceduralArt.Leaf(), p + R2(18, 10), sz * 0.34f,
                    new Vector2(R1(150), 90f + R0(120f)), 150f, 0.7f, 1.1f, 0.75f, R1(420), 0, Lerp(leafA, leafB, R0(1f)));
                if (l >= 0) _rt[l].localRotation = Quaternion.Euler(0, 0, R1(180));
            }
            for (int i = 0; i < 5; i++)                                          // pollen drifts up
                Emit(ProceduralArt.Glow(), p + R2(22, 12), sz * 0.12f, new Vector2(R1(40), 30f + R0(50f)), -30f, 0.6f, 1.0f, 0.7f, 0f, 3, pollen);
        }

        void Lightning(Vector2 p, float sz, float u, ElemFx k)
        {
            var hot = new Color(1f, 1f, 0.65f); var arc = new Color(0.7f, 0.85f, 1f);
            int bolts = k == ElemFx.Ultimate ? 4 : 2;
            for (int i = 0; i < bolts; i++)                                     // jagged bolts flash
            {
                int b = Emit(ProceduralArt.Bolt(), p + R2(30 * u, 20), sz * 0.9f * u, Vector2.zero, 0f, 0.9f, 1.15f, 0.16f, 0f, 2, hot);
                if (b >= 0) _rt[b].localRotation = Quaternion.Euler(0, 0, R1(28));
            }
            int sparks = k == ElemFx.Ultimate ? 10 : 6;
            for (int i = 0; i < sparks; i++)                                    // sparks scatter fast
                Emit(ProceduralArt.Star(), p + R2(10, 8), sz * 0.14f, new Vector2(R1(260), R1(240)), 0f, 0.7f, 0.9f, 0.25f, R1(500), 0, Lerp(hot, arc, R0(1f)));
            if (k == ElemFx.Ultimate) Emit(ProceduralArt.Ring(), p, sz * 0.6f, Vector2.zero, 0f, 0.3f, 7f, 0.35f, 0f, 1, arc);
        }

        void Heal(Vector2 p, float sz, float u)
        {
            var green = new Color(0.5f, 1f, 0.6f); var gold = new Color(1f, 0.9f, 0.42f);
            Emit(ProceduralArt.Ring(), p, sz * 0.6f, Vector2.zero, 0f, 0.3f, 6f, 0.5f, 0f, 1, gold);            // aura pulse
            int crosses = 5;
            for (int i = 0; i < crosses; i++)                                  // plus-crosses rise
                Emit(ProceduralArt.Plus(), p + R2(30, 14), sz * 0.24f, new Vector2(R1(24), 70f + R0(70f)), -20f, 0.6f, 1.0f, 0.72f, 0f, 3, i % 2 == 0 ? green : gold);
            for (int i = 0; i < 4; i++)                                          // soft motes
                Emit(ProceduralArt.Glow(), p + R2(24, 12), sz * 0.16f, new Vector2(R1(30), 40f + R0(50f)), -20f, 0.6f, 1.0f, 0.6f, 0f, 3, green);
        }

        void Neutral(Vector2 p, float sz, float u)
        {
            var w = new Color(1f, 0.95f, 0.85f);
            for (int i = 0; i < 6; i++)
                Emit(ProceduralArt.Triangle(), p + R2(14, 8), sz * 0.2f, new Vector2(R1(160), R1(160)), 120f, 0.6f, 1.0f, 0.45f, R1(360), 0, w);
            Emit(ProceduralArt.Glow(), p, sz * 0.7f, Vector2.zero, 0f, 0.3f, 1.6f, 0.28f, 0f, 1, w);
        }

        // ---- pool / sim ----

        int Emit(Sprite spr, Vector2 pos, float px, Vector2 vel, float grav, float s0, float s1, float life, float rotV, int mode, Color col)
        {
            int i = _cur; _cur = (_cur + 1) % N;
            var rt = _rt[i];
            rt.sizeDelta = new Vector2(px, px);
            rt.anchoredPosition = pos; rt.localRotation = Quaternion.identity; rt.localScale = Vector3.one * s0;
            rt.SetAsLastSibling();
            _img[i].sprite = spr; _img[i].color = col;
            _vel[i] = vel; _grav[i] = grav; _s0[i] = s0; _s1[i] = s1; _life[i] = life; _age[i] = 0f;
            _rot[i] = 0f; _rotV[i] = rotV; _col[i] = col; _mode[i] = mode;
            rt.gameObject.SetActive(true);
            return i;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < N; i++)
            {
                if (!_rt[i].gameObject.activeSelf) continue;
                _age[i] += dt; float p = _age[i] / _life[i];
                if (p >= 1f) { _rt[i].gameObject.SetActive(false); continue; }
                var rt = _rt[i]; var col = _col[i]; float a;
                switch (_mode[i])
                {
                    case 1:                                                     // ring / bloom expand, ease-out
                        {
                            float e = 1f - (1f - p) * (1f - p);
                            rt.localScale = Vector3.one * Mathf.Lerp(_s0[i], _s1[i], e);
                            a = (1f - p) * col.a;
                            break;
                        }
                    case 2:                                                     // static flash: quick-in, slow-out
                        rt.localScale = Vector3.one * Mathf.Lerp(_s0[i], _s1[i], p);
                        a = (p < 0.18f ? p / 0.18f : (1f - p) / 0.82f) * col.a;
                        break;
                    case 3:                                                     // buoyant rise: decel, sway, fade late
                        {
                            _vel[i] = new Vector2(_vel[i].x, _vel[i].y - _grav[i] * dt);
                            var pp = rt.anchoredPosition + _vel[i] * dt;
                            pp.x += Mathf.Sin((_age[i] + i) * 6f) * 12f * dt;
                            rt.anchoredPosition = pp;
                            rt.localScale = Vector3.one * Mathf.Lerp(_s0[i], _s1[i], Mathf.Min(1f, p * 2f));
                            a = Mathf.Clamp01(1f - (p - 0.55f) / 0.45f) * col.a;
                            if (_rotV[i] != 0f) { _rot[i] += _rotV[i] * dt; rt.localRotation = Quaternion.Euler(0, 0, _rot[i]); }
                            break;
                        }
                    default:                                                    // 0: ballistic move + gravity + spin
                        _vel[i] = new Vector2(_vel[i].x, _vel[i].y - _grav[i] * dt);
                        rt.anchoredPosition += _vel[i] * dt;
                        rt.localScale = Vector3.one * Mathf.Lerp(_s0[i], _s1[i], p);
                        if (_rotV[i] != 0f) { _rot[i] += _rotV[i] * dt; rt.localRotation = Quaternion.Euler(0, 0, _rot[i]); }
                        a = (1f - p) * col.a;
                        break;
                }
                _img[i].color = new Color(col.r, col.g, col.b, a);
            }
        }

        // cosmetic scatter (non-deterministic, never fed back to the sim)
        static float R0(float m) => UnityEngine.Random.value * m;
        static float R1(float m) => (UnityEngine.Random.value * 2f - 1f) * m;
        static Vector2 R2(float mx, float my) => new Vector2(R1(mx), R1(my));
        static Color Lerp(Color a, Color b, float t) => Color.Lerp(a, b, t);
    }
}
