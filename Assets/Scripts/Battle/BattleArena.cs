using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    public enum ArenaReact { Dust, Crack, Debris, Shockwave, KO }

    // Procedural element-themed arena (Phase R): gradient sky, three parallax layers
    // (far / near / foreground), ground, per-element ambient motion (fire embers + heat
    // shimmer, water mist + bubbles + wave, nature leaves + pollen + wind), a reactive
    // impact system (dust / crack / debris / shockwave / KO), and an arena flash. Every
    // effect draws from PRE-WARMED pools — no Instantiate/Destroy or allocation during
    // combat. Generated shapes only, presentation only.
    public class BattleArena
    {
        RectTransform _root, _far, _near, _fore, _backdrop, _floor, _mist;
        Vector2 _farHome, _nearHome, _foreHome, _backdropHome, _floorHome;
        RectTransform[] _parts; Image[] _partImg; Vector2[] _vel; float _drift;
        string _element; Color _accent; float _partDir;
        Image _flash; float _flashA;

        // Reactive impact pool (parallel arrays = cache-friendly, zero per-spawn alloc).
        const int RP = 40;
        Image[] _rp; RectTransform[] _rpRt;
        float[] _rpAge, _rpLife, _rpS0, _rpS1, _rpGrav, _rpRot, _rpRotVel;
        Vector2[] _rpVel; Color[] _rpCol; int[] _rpMode; int _rpCur;

        public void Build(RectTransform parent, string element)
        {
            _element = element;
            Theme(element, out var skyTop, out var skyHor, out var mtnCol, out var groundCol, out var floorCol, out var partCol, out var partDir);
            _accent = partCol; _partDir = partDir;

            _root = Panel(parent, "Arena", skyTop, new Vector2(1200, 1700), Vector2.zero);
            _root.SetAsFirstSibling();

            // Real CC0 forest panorama backdrop, tinted per element; procedural fallback.
            var backdrop = Resources.Load<Texture2D>("Arena/forest");
            if (backdrop != null)
            {
                var go = new GameObject("Backdrop", typeof(RectTransform), typeof(RawImage));
                _backdrop = go.GetComponent<RectTransform>(); _backdrop.SetParent(_root, false);
                _backdrop.anchorMin = _backdrop.anchorMax = new Vector2(0.5f, 0.5f);
                _backdrop.sizeDelta = new Vector2(1320, 1240); _backdrop.anchoredPosition = new Vector2(0, 200);
                var img = go.GetComponent<RawImage>(); img.texture = backdrop; img.raycastTarget = false;
                img.uvRect = new Rect(0.12f, 0f, 0.42f, 1f);
                img.color = element == "Fire" ? new Color(1f, 0.72f, 0.55f)
                          : element == "Water" ? new Color(0.7f, 0.85f, 1.05f)
                          : element == "Nature" ? new Color(0.92f, 1f, 0.9f)
                          : new Color(0.85f, 0.8f, 0.95f);
            }
            else
            {
                const int bands = 12;
                for (int i = 0; i < bands; i++)
                {
                    float f = i / (float)(bands - 1);
                    Panel(_root, "Sky", Color.Lerp(skyTop, skyHor, f), new Vector2(1200, 165), new Vector2(0, 780 - i * 145));
                }
                _far = Layer("Far");
                var mtn = new Color(mtnCol.r, mtnCol.g, mtnCol.b, 0.9f);
                float[] mx = { -430, -210, 40, 250, 470 };
                float[] ms = { 360, 520, 430, 600, 380 };
                for (int i = 0; i < mx.Length; i++) Diamond(_far, mtn, ms[i], new Vector2(mx[i], -430));
            }

            _near = Layer("Near");
            var pil = new Color(floorCol.r * 0.6f, floorCol.g * 0.6f, floorCol.b * 0.6f, 0.95f);
            Panel(_near, "PillarL", pil, new Vector2(120, 900), new Vector2(-520, -180));
            Panel(_near, "PillarR", pil, new Vector2(120, 900), new Vector2(520, -180));

            Panel(_root, "Ground", groundCol, new Vector2(1200, 520), new Vector2(0, -700));
            _floor = Panel(_root, "Floor", floorCol, new Vector2(980, 10), new Vector2(0, -430));
            _floor.GetComponent<Image>().color = new Color(floorCol.r * 1.6f, floorCol.g * 1.6f, floorCol.b * 1.6f);

            // Foreground silhouette layer — darkest, strongest parallax (depth).
            _fore = Layer("Fore");
            var fg = new Color(groundCol.r * 0.4f, groundCol.g * 0.4f, groundCol.b * 0.4f, 0.96f);
            Diamond(_fore, fg, 260, new Vector2(-470, -560));
            Diamond(_fore, fg, 200, new Vector2(430, -580));
            Panel(_fore, "FgBar", fg, new Vector2(1300, 120), new Vector2(0, -690));

            // Water gets a drifting mist band; other elements skip it.
            if (element == "Water")
            {
                _mist = Panel(_root, "Mist", new Color(0.7f, 0.85f, 1f, 0.06f), new Vector2(1500, 320), new Vector2(0, -320));
                _mist.GetComponent<Image>().sprite = ProceduralArt.Glow();
            }

            // Ambient particles (embers / bubbles / leaves).
            int n = 18;
            _parts = new RectTransform[n]; _partImg = new Image[n]; _vel = new Vector2[n];
            var seed = new System.Random(element == null ? 0 : element.GetHashCode());
            for (int i = 0; i < n; i++)
            {
                float sz = 12f + (float)seed.NextDouble() * 26f;
                var p = Panel(_root, "Ambient", new Color(partCol.r, partCol.g, partCol.b, 0.28f), new Vector2(sz, sz),
                    new Vector2((float)(seed.NextDouble() * 1100 - 550), (float)(seed.NextDouble() * 1400 - 700)));
                var pimg = p.GetComponent<Image>(); pimg.sprite = element == "Nature" ? ProceduralArt.Triangle() : ProceduralArt.Glow();
                _parts[i] = p; _partImg[i] = pimg;
                _vel[i] = new Vector2(((float)seed.NextDouble() - 0.5f) * 22f, partDir * (10f + (float)seed.NextDouble() * 26f));
            }

            // Arena flash overlay (ult / finisher) — above the scene, below the fighters.
            var flashRt = Panel(_root, "ArenaFlash", new Color(1, 1, 1, 0), new Vector2(1600, 2000), Vector2.zero);
            _flash = flashRt.GetComponent<Image>(); _flash.raycastTarget = false;

            BuildReactPool();

            _farHome = _far != null ? _far.anchoredPosition : Vector2.zero;
            _nearHome = _near.anchoredPosition;
            _foreHome = _fore.anchoredPosition;
            _backdropHome = _backdrop != null ? _backdrop.anchoredPosition : Vector2.zero;
            _floorHome = _floor != null ? _floor.anchoredPosition : Vector2.zero;
        }

        void BuildReactPool()
        {
            _rp = new Image[RP]; _rpRt = new RectTransform[RP];
            _rpAge = new float[RP]; _rpLife = new float[RP]; _rpS0 = new float[RP]; _rpS1 = new float[RP];
            _rpGrav = new float[RP]; _rpRot = new float[RP]; _rpRotVel = new float[RP];
            _rpVel = new Vector2[RP]; _rpCol = new Color[RP]; _rpMode = new int[RP];
            for (int i = 0; i < RP; i++)
            {
                var p = Panel(_root, "React", new Color(1, 1, 1, 0), new Vector2(48, 48), Vector2.zero);
                var img = p.GetComponent<Image>(); img.raycastTarget = false;
                p.gameObject.SetActive(false);
                _rp[i] = img; _rpRt[i] = p;
            }
        }

        // Impact reaction at a ground position (in arena/stage-local space).
        public void React(ArenaReact kind, Vector2 pos)
        {
            var dust = new Color(_accent.r * 0.7f + 0.3f, _accent.g * 0.7f + 0.3f, _accent.b * 0.7f + 0.3f, 0.5f);
            var dark = new Color(0.06f, 0.05f, 0.05f, 0.7f);
            switch (kind)
            {
                case ArenaReact.Dust:
                    for (int k = 0; k < 3; k++)
                        Emit(pos + R2(28, 12), ProceduralArt.Glow(), dust, new Vector2(R1(46), 40f + R0(50f)), 70f, 0.5f, 1.3f, 0.5f, 0f, 0);
                    break;
                case ArenaReact.Crack:
                    for (int k = 0; k < 3; k++) { var c = _rpRt.Length > 0 ? EmitR(pos + R2(60, 4), ProceduralArt.Triangle(), dark, Vector2.zero, 0f, 1.6f + R0(0.8f), 1.6f, 1.1f, 0f, 2) : -1; if (c >= 0) _rpRt[c].localRotation = Quaternion.Euler(0, 0, R1(180)); }
                    for (int k = 0; k < 2; k++) Emit(pos + R2(30, 8), ProceduralArt.Glow(), dust, new Vector2(R1(40), 30f + R0(40f)), 70f, 0.5f, 1.2f, 0.45f, 0f, 0);
                    break;
                case ArenaReact.Debris:
                    for (int k = 0; k < 6; k++) Emit(pos + R2(20, 10), ProceduralArt.Triangle(), dark, new Vector2(R1(130), 130f + R0(150f)), 240f, 0.5f, 1.0f, 0.7f, R1(540), 0);
                    for (int k = 0; k < 2; k++) Emit(pos, ProceduralArt.Glow(), dust, new Vector2(R1(60), 60f + R0(60f)), 70f, 0.6f, 1.4f, 0.5f, 0f, 0);
                    break;
                case ArenaReact.Shockwave:
                    Emit(pos, ProceduralArt.Ring(), new Color(_accent.r, _accent.g, _accent.b, 0.7f), Vector2.zero, 0f, 0.3f, 9f, 0.45f, 0f, 1);
                    for (int k = 0; k < 4; k++) Emit(pos, ProceduralArt.Glow(), dust, new Vector2(R1(120), 30f + R0(70f)), 60f, 0.5f, 1.5f, 0.5f, 0f, 0);
                    break;
                case ArenaReact.KO:
                    Emit(pos, ProceduralArt.Ring(), new Color(_accent.r, _accent.g, _accent.b, 0.85f), Vector2.zero, 0f, 0.3f, 14f, 0.6f, 0f, 1);
                    Emit(pos, ProceduralArt.Ring(), new Color(1, 1, 1, 0.6f), Vector2.zero, 0f, 0.3f, 8f, 0.4f, 0f, 1);
                    for (int k = 0; k < 8; k++) Emit(pos + R2(24, 12), ProceduralArt.Triangle(), dark, new Vector2(R1(200), 160f + R0(220f)), 260f, 0.5f, 1.1f, 0.8f, R1(680), 0);
                    for (int k = 0; k < 4; k++) { int c = EmitR(pos + R2(70, 4), ProceduralArt.Triangle(), dark, Vector2.zero, 0f, 2.0f + R0(1f), 2.0f, 1.3f, 0f, 2); if (c >= 0) _rpRt[c].localRotation = Quaternion.Euler(0, 0, R1(180)); }
                    Flash(_accent, 0.45f);
                    break;
            }
        }

        public void Flash(Color c, float amt)
        {
            if (_flash == null) return;
            _flashA = Mathf.Max(_flashA, amt);
            _flash.color = new Color(c.r, c.g, c.b, _flashA);
        }

        void Emit(Vector2 pos, Sprite spr, Color col, Vector2 vel, float grav, float s0, float s1, float life, float rotVel, int mode)
            => EmitR(pos, spr, col, vel, grav, s0, s1, life, rotVel, mode);

        int EmitR(Vector2 pos, Sprite spr, Color col, Vector2 vel, float grav, float s0, float s1, float life, float rotVel, int mode)
        {
            int i = _rpCur; _rpCur = (_rpCur + 1) % RP;
            var rt = _rpRt[i];
            rt.anchoredPosition = pos; rt.localRotation = Quaternion.identity; rt.localScale = Vector3.one * s0;
            _rp[i].sprite = spr; _rp[i].color = col;
            _rpVel[i] = vel; _rpGrav[i] = grav; _rpS0[i] = s0; _rpS1[i] = s1; _rpLife[i] = life; _rpAge[i] = 0f;
            _rpRot[i] = 0f; _rpRotVel[i] = rotVel; _rpCol[i] = col; _rpMode[i] = mode;
            rt.gameObject.SetActive(true);
            return i;
        }

        public void Tick(float dt)
        {
            _drift += dt;
            // arena flash decay
            if (_flash != null && _flashA > 0f)
            {
                _flashA = Mathf.Max(0f, _flashA - dt * 2.5f);
                var c = _flash.color; c.a = _flashA; _flash.color = c;
            }
            // per-element ambient motion + environment life
            AmbientTick(dt);
            // reactive particles
            if (_rp == null) return;
            for (int i = 0; i < RP; i++)
            {
                if (!_rpRt[i].gameObject.activeSelf) continue;
                _rpAge[i] += dt; float p = _rpAge[i] / _rpLife[i];
                if (p >= 1f) { _rpRt[i].gameObject.SetActive(false); continue; }
                var rt = _rpRt[i]; var col = _rpCol[i]; float a;
                if (_rpMode[i] == 1)               // shockwave ring: ease-out scale, fade
                {
                    float e = 1f - (1f - p) * (1f - p);
                    rt.localScale = Vector3.one * Mathf.Lerp(_rpS0[i], _rpS1[i], e);
                    a = (1f - p) * col.a;
                }
                else if (_rpMode[i] == 2)          // crack shard: quick in, slow fade, static
                {
                    a = (p < 0.15f ? p / 0.15f : (1f - p) / 0.85f) * col.a;
                }
                else                                // dust / debris: move + gravity + spin
                {
                    _rpVel[i] = new Vector2(_rpVel[i].x, _rpVel[i].y - _rpGrav[i] * dt);
                    rt.anchoredPosition += _rpVel[i] * dt;
                    rt.localScale = Vector3.one * Mathf.Lerp(_rpS0[i], _rpS1[i], p);
                    if (_rpRotVel[i] != 0f) { _rpRot[i] += _rpRotVel[i] * dt; rt.localRotation = Quaternion.Euler(0, 0, _rpRot[i]); }
                    a = (1f - p) * col.a;
                }
                _rp[i].color = new Color(col.r, col.g, col.b, a);
            }
        }

        void AmbientTick(float dt)
        {
            if (_parts == null) return;
            bool nature = _element == "Nature", water = _element == "Water", fire = _element == "Fire";
            float gust = nature ? Mathf.Sin(_drift * 0.6f) * 30f + Mathf.Sin(_drift * 1.7f) * 12f : 0f;   // wind
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null) continue;
                var rt = _parts[i];
                var p = rt.anchoredPosition + _vel[i] * dt;
                p.x += Mathf.Sin(_drift + i) * 6f * dt + gust * dt;                     // sway + wind gust
                if (water) p.x += Mathf.Sin(_drift * 2f + i) * 14f * dt;                // bubble wobble
                if (p.y > 760f) p.y = -700f; else if (p.y < -700f) p.y = 760f;
                if (p.x > 620f) p.x = -620f; else if (p.x < -620f) p.x = 620f;
                rt.anchoredPosition = p;
                if (nature) rt.localRotation = Quaternion.Euler(0, 0, (_drift * 60f + i * 40f) % 360f);   // tumbling leaves
                else if (fire && _partImg[i] != null)                                   // ember flicker
                {
                    var c = _partImg[i].color;
                    c.a = 0.2f + 0.12f * Mathf.Abs(Mathf.Sin(_drift * 5f + i)); _partImg[i].color = c;
                }
            }
            // water surface wave + fire heat shimmer + drifting mist
            if (water && _floor != null) _floor.anchoredPosition = _floorHome + new Vector2(Mathf.Sin(_drift * 1.5f) * 8f, 0f);
            if (fire && _backdrop != null) _backdrop.anchoredPosition = _backdropHome + new Vector2(Mathf.Sin(_drift * 3.2f) * 4f, 0f);   // heat shimmer
            if (_mist != null) _mist.anchoredPosition = new Vector2(Mathf.Sin(_drift * 0.4f) * 60f, -320f + Mathf.Sin(_drift * 0.7f) * 20f);
        }

        static void Theme(string e, out Color skyTop, out Color skyHor, out Color mtn, out Color ground, out Color floor, out Color part, out float partDir)
        {
            switch (e)
            {
                case "Fire":
                    skyTop = new Color(0.18f, 0.06f, 0.05f); skyHor = new Color(0.52f, 0.17f, 0.09f);
                    mtn = new Color(0.32f, 0.11f, 0.09f); ground = new Color(0.17f, 0.07f, 0.05f);
                    floor = new Color(0.20f, 0.10f, 0.08f); part = new Color(1f, 0.55f, 0.2f); partDir = 1f; break;    // embers rise
                case "Water":
                    skyTop = new Color(0.04f, 0.10f, 0.22f); skyHor = new Color(0.09f, 0.30f, 0.46f);
                    mtn = new Color(0.10f, 0.21f, 0.36f); ground = new Color(0.06f, 0.11f, 0.18f);
                    floor = new Color(0.10f, 0.15f, 0.22f); part = new Color(0.5f, 0.8f, 1f); partDir = 0.7f; break;   // bubbles rise
                case "Nature":
                    skyTop = new Color(0.07f, 0.14f, 0.09f); skyHor = new Color(0.17f, 0.33f, 0.17f);
                    mtn = new Color(0.13f, 0.26f, 0.14f); ground = new Color(0.09f, 0.14f, 0.09f);
                    floor = new Color(0.13f, 0.19f, 0.11f); part = new Color(0.55f, 0.9f, 0.45f); partDir = -1f; break; // leaves fall
                default:
                    skyTop = new Color(0.10f, 0.12f, 0.22f); skyHor = new Color(0.28f, 0.17f, 0.26f);
                    mtn = new Color(0.17f, 0.16f, 0.27f); ground = new Color(0.11f, 0.10f, 0.13f);
                    floor = new Color(0.22f, 0.20f, 0.26f); part = new Color(0.7f, 0.7f, 0.85f); partDir = 0.5f; break;
            }
        }

        public void SetParallax(Vector2 cam)
        {
            if (_far != null) _far.anchoredPosition = _farHome - cam * 0.03f;
            if (_near != null) _near.anchoredPosition = _nearHome - cam * 0.09f;
            if (_fore != null) _fore.anchoredPosition = _foreHome - cam * 0.17f;   // foreground moves most
            if (_backdrop != null && _element != "Fire") _backdrop.anchoredPosition = _backdropHome - cam * 0.02f;
        }

        public void Destroy() { if (_root != null) Object.Destroy(_root.gameObject); }

        // deterministic-free cosmetic scatter (no allocation)
        static float R0(float m) => UnityEngine.Random.value * m;
        static float R1(float m) => (UnityEngine.Random.value * 2f - 1f) * m;
        static Vector2 R2(float mx, float my) => new Vector2(R1(mx), R1(my));

        RectTransform Layer(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_root, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        static RectTransform Panel(RectTransform parent, string name, Color c, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = size; rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
            return rt;
        }

        static void Diamond(RectTransform parent, Color c, float size, Vector2 pos)
        {
            var rt = Panel(parent, "Mtn", c, new Vector2(size, size), pos);
            rt.localRotation = Quaternion.Euler(0, 0, 45f);
        }
    }
}
