using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Un-boxed battle fighter (Phase O-0): a free-standing creature in the arena —
    // NO card panel, frame, rarity border or backing rect. Just a bare front sprite
    // (player side mirrored), a floating HP bar above the head, and a soft ground
    // shadow that shrinks/fades as the unit rises (so launchers & air combos read as
    // airborne). Hit/heal/death flash tints the sprite SILHOUETTE (a white copy of the
    // same sprite), never a rectangle. Renders only — no simulation.
    public class UnitView : MonoBehaviour
    {
        const float ART = 336f;        // sprite display size (bigger = fighters dominate the frame, V1 reframing)
        const float FOOT = 140f;       // sprite center -> ground line (feet) offset (scales with ART)
        const float MAX_JUMP = 300f;   // launch height at which the shadow is smallest

        Image _shadow, _hpFill, _hpDelayed; CanvasGroup _artGroup, _barGroup;
        DeformSprite _dBody, _dOutline, _dFlash; Graphic _flash; Sprite _spriteRef;   // mesh-deform body + outline + flash (the "animator")
        RectTransform _rt, _artRt, _shadowRt, _barsRt, _hpBgRt, _nameRt, _elemDotRt, _levelRt; Text _name; Font _font;
        Vector2 _basePos; int _mirror = 1;
        int _maxHp = 1; float _targetFrac = 1f, _dispFrac = 1f, _delayFrac = 1f;
        float _ghostHold, _lastTarget = 1f;   // HP ghost bar: delay before the lost-HP chunk drains
        bool _dead, _victory; float _deadTime, _spawnT = 1f; Vector2 _knock;
        Vector2 _impulse; float _reserveScale = 1f, _reserveDim = 1f;   // cinematic push + reserve staging
        public Vector2 combatOffset;    // view-driven fight choreography (dash / launch / slam)

        // --- Phase O deform layer (applied to the ART child only, independent of
        // combatOffset / choreography math): squash & stretch, lean/spin, vibrate. ---
        Vector2 _lastPos; Vector2 _vel;
        Vector2 _sqCur = Vector2.one; float _sqT, _sqDur;   // explicit squash impulse (eases back to 1)
        float _spinT, _spinSpeed, _spinAngle;               // launcher/slam spin + lean settle
        float _vibT, _vibMag;                               // hit-stop vibrate
        float _impSilT;                                     // impact-frame white silhouette
        float _extraTilt;                                   // hit head-snap rotation (decays)
        float _roamFactor;                                  // idle-wander blend (0 busy → 1 at rest)
        float _weight = 1f;                                 // heft: heavy = ponderous/plodding, light = springy/darty

        // Personality motion (Phase P1): role sets the idle character, element adds a signature tremor.
        float _pFreq = 1f, _pBob = 1f, _pSway = 1f, _pJitter = 0.6f, _pHover = 0f, _pLimb = 1f, _pTremor = 0f, _pTremorFreq = 12f;

        // Species character (Character Direction pass): per-creature stance/lean/pace/elasticity/etc.
        string _speciesId;
        float _cLean = 0f, _cStance = 0f, _cElastic = 1f, _cSettle = 4.5f, _cAntic = 1f;
        HitStyle _cHit = HitStyle.Recoil; DeathStyle _cDeath = DeathStyle.LaunchSpin;

        enum Anim { None, Attack, Hit, Heal }
        Anim _anim = Anim.None; float _animTime, _animDur, _animDist, _animMag = 1f; Vector2 _animDir; bool _animUlt;

        public Vector2 BasePos => _basePos;
        public bool IsDead => _dead;

        public void Build(RectTransform parent, Vector2 anchoredPos, Vector2 size,
            Color teamColor, Color speciesColor, string speciesId, string displayName, Font font,
            string element = "", string role = "Bruiser", bool playerSide = false)
        {
            _mirror = playerSide ? -1 : 1;
            _font = font;
            _speciesId = speciesId;
            Personality(role, element);

            // Soft ground shadow — a SIBLING under the stage (not a child of the fighter),
            // so it stays on the ground line while the fighter jumps. Behind the fighter.
            var sgo = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            _shadowRt = sgo.GetComponent<RectTransform>(); _shadowRt.SetParent(parent, false);
            _shadowRt.anchorMin = _shadowRt.anchorMax = new Vector2(0.5f, 0.5f);
            _shadowRt.sizeDelta = new Vector2(178, 52); _shadowRt.anchoredPosition = anchoredPos + new Vector2(0, -FOOT);
            _shadow = sgo.GetComponent<Image>(); _shadow.sprite = ProceduralArt.Glow();
            _shadow.color = new Color(0f, 0f, 0f, 0.52f); _shadow.raycastTarget = false;

            // Fighter root (the moving transform).
            var go = new GameObject("Unit_" + speciesId, typeof(RectTransform));
            _rt = go.GetComponent<RectTransform>(); _rt.SetParent(parent, false);
            _rt.sizeDelta = new Vector2(ART, ART + 130f); _rt.anchoredPosition = anchoredPos; _basePos = anchoredPos;

            // Bare sprite (mirrored on the player side) — no frame, no panel, no badge.
            var art = new GameObject("Art", typeof(RectTransform));
            _artRt = art.GetComponent<RectTransform>(); _artRt.SetParent(_rt, false);
            _artRt.anchorMin = _artRt.anchorMax = new Vector2(0.5f, 0.5f);
            _artRt.sizeDelta = new Vector2(ART, ART); _artRt.anchoredPosition = Vector2.zero;
            _artRt.localScale = new Vector3(_mirror, 1f, 1f);

            var sprite = MonsterVisual.For(speciesId, false);   // FRONT sprite for BOTH sides (KOF staging)
            _spriteRef = sprite;
            if (sprite != null)
            {
                // Mesh-deform body: the same sprite on a grid whose vertices breathe / sway / bend
                // every frame (the "animator"). Outline (dark, behind) + flash (white, on top) are
                // deform copies sharing the SAME shape, so they wobble together with the body.
                _dOutline = MakeDeform("Outline", sprite, new Color(0.03f, 0.03f, 0.05f, 0.62f), 1.075f);
                _dBody = MakeDeform("Body", sprite, Color.white, 1f);
                _dFlash = MakeDeform("Flash", sprite, new Color(1, 1, 1, 0), 1f); _flash = _dFlash;
                _dBody.phase = _dOutline.phase = _dFlash.phase = anchoredPos.x * 0.05f + anchoredPos.y * 0.03f;
                _dBody.shadeLo = 0.66f; _dBody.lightTint = LightTint(element);   // top-light form + biome tint (only the body)
            }
            else
            {
                // Procedural fallback (rare — all shipped species have real sprites).
                var proc = MonsterArt.Build(_artRt, speciesId, element, role, ART * 0.9f);
                proc.anchorMin = proc.anchorMax = new Vector2(0.5f, 0.5f); proc.anchoredPosition = Vector2.zero;
                var fl = new GameObject("Flash", typeof(RectTransform), typeof(Image));
                var frt = fl.GetComponent<RectTransform>(); frt.SetParent(_artRt, false);
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
                var flImg = fl.GetComponent<Image>(); flImg.sprite = ProceduralArt.Disc(); flImg.color = new Color(1, 1, 1, 0); flImg.raycastTarget = false; _flash = flImg;
            }
            _artGroup = _artRt.gameObject.AddComponent<CanvasGroup>();

            // Bars container (HP + name + element dot) — fades out on KO so the HP bar despawns.
            var bars = new GameObject("Bars", typeof(RectTransform));
            _barsRt = bars.GetComponent<RectTransform>(); _barsRt.SetParent(_rt, false);
            _barsRt.anchorMin = Vector2.zero; _barsRt.anchorMax = Vector2.one; _barsRt.offsetMin = Vector2.zero; _barsRt.offsetMax = Vector2.zero;
            _barGroup = bars.AddComponent<CanvasGroup>();

            // Floating HP bar above the head.
            var bg = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
            var bgrt = bg.GetComponent<RectTransform>(); bgrt.SetParent(_barsRt, false);
            bgrt.anchorMin = bgrt.anchorMax = new Vector2(0.5f, 0.5f);
            bgrt.sizeDelta = new Vector2(148, 14); bgrt.anchoredPosition = new Vector2(0, ART * 0.5f + 22f); _hpBgRt = bgrt;
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f); bg.GetComponent<Image>().raycastTarget = false;
            _hpDelayed = MakeFill(bgrt, new Color(0.95f, 0.3f, 0.28f, 0.92f));   // red "recently lost HP" ghost
            _hpFill = MakeFill(bgrt, new Color(0.3f, 0.9f, 0.3f, 1f));

            // Small floating name above the HP bar.
            var np = new GameObject("Name", typeof(RectTransform));
            var nprt = np.GetComponent<RectTransform>(); nprt.SetParent(_barsRt, false);
            nprt.anchorMin = nprt.anchorMax = new Vector2(0.5f, 0.5f);
            nprt.sizeDelta = new Vector2(196, 26); nprt.anchoredPosition = new Vector2(0, ART * 0.5f + 44f); _nameRt = nprt;
            _name = MakeText(nprt, font, displayName, 17, Vector2.zero, TextAnchor.MiddleCenter);
            _name.fontStyle = FontStyle.Bold;

            _lastPos = anchoredPos;
            _spawnT = 0f;   // spawn-pop
        }

        // Biome light colour the body is lit by, so fighters sit in the scene's lighting.
        static Color LightTint(string element)
        {
            switch (element)
            {
                case "Fire": return new Color(1f, 0.93f, 0.84f);
                case "Water": return new Color(0.86f, 0.93f, 1f);
                case "Nature": return new Color(0.9f, 1f, 0.88f);
                case "Lightning": return new Color(1f, 1f, 0.9f);
                default: return Color.white;
            }
        }

        DeformSprite MakeDeform(string name, Sprite sp, Color col, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(DeformSprite));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_artRt, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = new Vector3(scale, scale, 1f);
            var d = go.GetComponent<DeformSprite>(); d.sprite = sp; d.color = col; d.raycastTarget = false;
            return d;
        }

        void PushDeform(float sway, float lean, float breathe, float wobble, float limb)
        {
            SetD(_dBody, sway, lean, breathe, wobble, limb);
            SetD(_dOutline, sway, lean, breathe, wobble, limb);
            SetD(_dFlash, sway, lean, breathe, wobble, limb);
        }
        static void SetD(DeformSprite d, float sway, float lean, float breathe, float wobble, float limb)
        {
            if (d == null) return;
            d.sway = sway; d.lean = lean; d.breathe = breathe; d.wobbleAmp = wobble; d.limb = limb;
        }

        static Image MakeFill(RectTransform parent, Color c)
        {
            var go = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
            var rt = img.rectTransform; rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal; img.fillOrigin = 0; img.fillAmount = 1f;
            return img;
        }

        public void SetMaxHp(int m) => _maxHp = Mathf.Max(1, m);
        public void SetHp(int cur) { _targetFrac = Mathf.Clamp01(cur / (float)_maxHp); if (cur <= 0 && !_dead) PlayDeath(Vector2.zero); }

        public void PlaySpawn() => _spawnT = 0f;
        public void PlayVictory() { if (!_dead) _victory = true; }
        public void PlayAttack(Vector2 dir, float dist, bool ult) { if (_dead) return; _anim = Anim.Attack; _animTime = 0; _animDur = (ult ? 0.5f : 0.32f) * Mathf.Lerp(1f, _cAntic, 0.6f); _animDir = dir.normalized; _animDist = dist; _animUlt = ult; }   // species wind-up pace (Phase 5)
        public void PlayHit(bool crit) { if (_dead) return; _anim = Anim.Hit; _animTime = 0; _animDur = 0.3f; _animMag = crit ? 2.4f : 1f; }
        public void PlayHeal() { if (_dead) return; _anim = Anim.Heal; _animTime = 0; _animDur = 0.45f; }
        public void PlayDeath(Vector2 knock) { if (_dead) return; _dead = true; _victory = false; _deadTime = 0; _knock = knock; if (_name != null) _name.color = new Color(1, 1, 1, 0.5f); }

        // --- Cinematic presentation (visual only) ---
        public void Knock(Vector2 dir, float strength) { if (_dead) return; _impulse += dir.normalized * strength; }
        public void Launch(float strength) { if (_dead) return; _impulse += new Vector2(0f, strength); }
        public void Dodge(Vector2 dir) { if (_dead) return; _impulse += dir.normalized * 80f; _anim = Anim.Hit; _animTime = 0; _animDur = 0.24f; _animMag = 0.4f; }
        public void SetReserve(bool r)
        {
            _reserveScale = r ? 0.62f : 1f; _reserveDim = r ? 0.55f : 1f;
            // Benched reserves drop their Lv + element badges — declutters the flanks and stops the
            // badges clipping the screen edge. Shown again if promoted to the front. (V9)
            if (_levelRt != null) _levelRt.gameObject.SetActive(!r);
            if (_elemDotRt != null) _elemDotRt.gameObject.SetActive(!r);
        }
        public void SetWeight(float w) { _weight = Mathf.Clamp(w, 0.6f, 1.6f); }   // role-driven heft (presentation only)

        // Per-role + per-element idle personality (Phase P1). Tanks plod low & slow; assassins are
        // fast, restless, leaning; mages float; supports bounce; fire shivers; lightning twitches;
        // water flows smooth; nature drifts gently. Presentation only.
        void Personality(string role, string element)
        {
            switch (role)
            {
                case "Tank":     _pFreq = 0.72f; _pBob = 0.7f;  _pSway = 0.5f; _pJitter = 0.3f; _pHover = 0f; _pLimb = 0.7f; break;
                case "Assassin": _pFreq = 1.55f; _pBob = 1.1f;  _pSway = 1.5f; _pJitter = 2.6f; _pHover = 0f; _pLimb = 1.45f; break;
                case "Mage":     _pFreq = 0.9f;  _pBob = 0.6f;  _pSway = 0.7f; _pJitter = 0.4f; _pHover = 6f; _pLimb = 0.9f; break;
                case "Support":  _pFreq = 1.15f; _pBob = 1.15f; _pSway = 1.0f; _pJitter = 0.8f; _pHover = 2.4f; _pLimb = 1.0f; break;
                default:         _pFreq = 1.0f;  _pBob = 1.0f;  _pSway = 1.0f; _pJitter = 0.6f; _pHover = 0f; _pLimb = 1.0f; break;   // Bruiser
            }
            switch (element)
            {
                case "Fire":      _pTremor = 1.6f; _pTremorFreq = 17f; break;
                case "Lightning": _pTremor = 2.2f; _pTremorFreq = 25f; break;
                case "Nature":    _pTremor = 0.6f; _pTremorFreq = 6f;  break;
                case "Water":     _pTremor = 0f;   _pJitter *= 0.5f;   break;   // smooth / flowing
                default:          _pTremor = 0f; break;
            }
            // Species character layer (Character Direction): each creature moves like itself.
            if (CharacterProfile.TryGet(_speciesId, out var ct))
            {
                _cLean = ct.lean; _cStance = ct.stance; _pFreq *= ct.freq;
                _cElastic = ct.elastic; _cSettle = ct.settle; _cAntic = ct.antic; _cHit = ct.hit; _cDeath = ct.death;
            }
            else
            {
                int h = _speciesId == null ? 0 : _speciesId.GetHashCode();   // no two species identical
                _pFreq *= 0.9f + ((h & 255) / 255f) * 0.3f;
                _cLean = ((h >> 8) & 15) - 7;
                _cStance = ((h >> 12) & 15) - 7;
            }
        }
        public void SetElement(Color c, string element)
        {
            if (_rt == null) return;
            var go = new GameObject("Elem", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_barsRt != null ? _barsRt : _rt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(26, 26); rt.anchoredPosition = new Vector2(-92, ART * 0.5f + 22f + _barRaise); _elemDotRt = rt;
            var img = go.GetComponent<Image>(); img.sprite = ElemIcon(element); img.color = c; img.raycastTarget = false;   // element-shaped icon, not a plain dot
        }

        static Sprite ElemIcon(string e)
        {
            switch (e)
            {
                case "Fire": return ProceduralArt.Flame();
                case "Water": return ProceduralArt.Droplet();
                case "Nature": return ProceduralArt.Leaf();
                case "Lightning": return ProceduralArt.Bolt();
                default: return ProceduralArt.Disc();
            }
        }

        // Level badge (Lv{n}) on a dark pill, to the right of the HP bar — surfaces the monster's level.
        public void SetLevel(int lvl)
        {
            if (_barsRt == null || _font == null || lvl <= 0) return;
            var go = new GameObject("Lv", typeof(RectTransform));
            _levelRt = go.GetComponent<RectTransform>(); _levelRt.SetParent(_barsRt, false);
            _levelRt.anchorMin = _levelRt.anchorMax = new Vector2(0.5f, 0.5f);
            _levelRt.sizeDelta = new Vector2(58, 24); _levelRt.anchoredPosition = new Vector2(96, ART * 0.5f + 22f + _barRaise);
            var bg = new GameObject("bg", typeof(RectTransform), typeof(Image));
            var brt = bg.GetComponent<RectTransform>(); brt.SetParent(_levelRt, false); brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
            var bi = bg.GetComponent<Image>(); bi.sprite = ProceduralArt.RoundedRect(); bi.color = new Color(0.1f, 0.11f, 0.14f, 0.92f); bi.raycastTarget = false;
            var t = MakeText(_levelRt, _font, "Lv" + lvl, 15, Vector2.zero, TextAnchor.MiddleCenter); t.fontStyle = FontStyle.Bold; t.color = new Color(1f, 0.88f, 0.5f);
        }

        // Raise this fighter's HP bar + name by a per-slot amount so stacked teammates' HUD does not
        // collide into one unreadable soup in a brawl scrum (V4 label de-collision). Presentation only.
        float _barRaise;
        public void SetBarRaise(float dy)
        {
            _barRaise = dy;
            if (_hpBgRt != null) _hpBgRt.anchoredPosition = new Vector2(_hpBgRt.anchoredPosition.x, ART * 0.5f + 22f + dy);
            if (_nameRt != null) _nameRt.anchoredPosition = new Vector2(_nameRt.anchoredPosition.x, ART * 0.5f + 44f + dy);
            if (_elemDotRt != null) _elemDotRt.anchoredPosition = new Vector2(_elemDotRt.anchoredPosition.x, ART * 0.5f + 22f + dy);
            if (_levelRt != null) _levelRt.anchoredPosition = new Vector2(_levelRt.anchoredPosition.x, ART * 0.5f + 22f + dy);
        }
        public void SetBasePos(Vector2 p) { _basePos = p; }
        public void EnterFrom(Vector2 from, Vector2 to) { _basePos = to; _impulse = from - to; }   // slide in via decaying impulse

        // --- Phase O deform hooks (visual only) ---
        public void Squash(float sx, float sy, float dur) { if (_dead) return; _sqCur = new Vector2(sx, sy); _sqT = _sqDur = Mathf.Max(0.01f, dur); }
        public void Spin(float degPerSec, float dur) { if (_dead) return; _spinSpeed = degPerSec; _spinT = Mathf.Max(_spinT, dur); }
        public void Vibrate(float mag, float dur) { if (_dead) return; _vibMag = Mathf.Max(_vibMag, mag); _vibT = Mathf.Max(_vibT, dur); }
        public void ImpactSilhouette(float dur) { _impSilT = Mathf.Max(0.01f, dur); }
        public Sprite CurrentSprite => _spriteRef;
        public Vector2 RenderPos => _rt != null ? _rt.anchoredPosition : _basePos;
        public float RenderScale => _rt != null ? _rt.localScale.x : 1f;
        public int Mirror => _mirror;

        // Deform layer on the ART child: explicit squash impulse × velocity stretch,
        // launcher/slam spin or velocity lean, and hit-stop vibrate. Kept off _rt so the
        // choreography position math (combatOffset / BasePos) is never touched.
        void ApplyDeform(float dt)
        {
            if (_artRt == null) return;
            Vector2 sq = Vector2.one;
            if (_sqT > 0f) { _sqT -= dt; float e = Mathf.Clamp01(_sqT / _sqDur); sq = Vector2.Lerp(Vector2.one, _sqCur, e); }
            float ax = Mathf.Clamp(Mathf.Abs(_vel.x) * 0.0005f, 0f, 0.30f);    // stretch along motion (motion smear)
            float ay = Mathf.Clamp(Mathf.Abs(_vel.y) * 0.0005f, 0f, 0.30f);
            float scx = sq.x * (1f + ax - ay * 0.5f);
            float scy = sq.y * (1f + ay - ax * 0.5f);
            float rot;
            if (_spinT > 0f) { _spinT -= dt; _spinAngle += _spinSpeed * dt; rot = _spinAngle; }
            else
            {
                _spinAngle = Mathf.Lerp(_spinAngle, 0f, 10f * dt);
                float idleSway = Mathf.Sin(Time.time * 0.9f + _lastPos.x * 0.02f) * 2.4f * _roamFactor;   // gentle at-rest sway (liveliness)
                rot = _spinAngle + Mathf.Clamp(-_vel.x * 0.02f * _weight, -16f, 16f) * _mirror + idleSway;   // heavier bodies lean harder into motion
            }
            rot += _extraTilt; _extraTilt = Mathf.MoveTowards(_extraTilt, 0f, 120f * dt);   // hit head-snap settles
            Vector2 vib = Vector2.zero;
            if (_vibT > 0f) { _vibT -= dt; float tt = Time.time; vib = new Vector2(Mathf.Sin(tt * 90f) * _vibMag, Mathf.Cos(tt * 78f) * _vibMag * 0.5f); }
            _artRt.localScale = new Vector3(_mirror * scx, scy, 1f);
            _artRt.localRotation = Quaternion.Euler(0f, 0f, rot);
            _artRt.anchoredPosition = vib;
        }

        // Attack motion: anticipation crouch → explosive lunge → follow-through overshoot → settle.
        static float AttackCurve(float p)
        {
            if (p < 0.16f) return -0.25f * (p / 0.16f);
            if (p < 0.42f) return Mathf.Lerp(-0.25f, 1f, (p - 0.16f) / 0.26f);
            float q = (p - 0.42f) / 0.58f;
            return Mathf.Lerp(1f, 0f, q) - Mathf.Sin(q * Mathf.PI) * 0.14f;
        }

        void Update()
        {
            if (_rt == null) return;
            float dt = Time.deltaTime;

            // HP ghost bar: the main fill drops INSTANTLY; the red ghost fill holds for
            // 0.4 s then drains down to it (classic fighting-game "recently lost HP").
            if (_targetFrac < _lastTarget - 0.0001f) _ghostHold = 0.4f;   // took damage → refresh the hold
            _lastTarget = _targetFrac;
            _dispFrac = _targetFrac;
            if (_ghostHold > 0f) _ghostHold -= dt;
            else _delayFrac = Mathf.MoveTowards(_delayFrac, _dispFrac, 1.4f * dt);
            if (_delayFrac < _dispFrac) _delayFrac = _dispFrac;           // heal: ghost snaps up
            if (_hpFill != null)
            {
                _hpFill.fillAmount = _dispFrac;
                if (_dispFrac > 0.5f) _hpFill.color = new Color(0.3f, 0.9f, 0.3f);
                else if (_dispFrac > 0.25f) _hpFill.color = new Color(0.95f, 0.8f, 0.2f);
                else
                {
                    // Near-death danger pulse — instant "this one is about to die" read (readability target).
                    float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 7f));
                    _hpFill.color = new Color(1f, 0.16f * pulse, 0.12f * pulse);
                    if (_barGroup != null) _barGroup.alpha = (0.75f + 0.25f * pulse) * _reserveDim;   // whole bar throbs
                }
            }
            // Healthy/hurt bars follow the reserve dim so benched fighters' HUD recedes (declutter flanks).
            if (_barGroup != null && !_dead && _dispFrac > 0.25f) _barGroup.alpha = _reserveDim;
            if (_hpDelayed != null) _hpDelayed.fillAmount = _delayFrac;

            if (_spawnT < 1f) _spawnT = Mathf.Min(1f, _spawnT + dt / 0.3f);
            float spawnScale = Mathf.SmoothStep(0.2f, 1f, _spawnT) * _reserveScale;

            // Cinematic impulse (knockback / launch / slide-in) — slower ease-out (~0.25 s)
            // so victim knockback reads heavy; the engagement system re-closes the gap.
            _impulse = Vector2.Lerp(_impulse, Vector2.zero, _cSettle * dt);   // species settle: heavy = slow, agile = snappy
            if (_impulse.sqrMagnitude < 0.25f) _impulse = Vector2.zero;

            if (_dead)
            {
                _deadTime += dt;
                float p = Mathf.Clamp01(_deadTime / 0.7f);
                // Species/element death language (Phase 4): Collapse (heavy drop+flatten), Tumble (agile
                // roll), Dissolve (water/fire sink + quick fade), Scatter (nature gentle lift), else the
                // default launch-spin. Reads WHO died without the name.
                float side = _knock.x >= 0f ? 1f : -1f;
                float rot, dropY, scX = 1f, scY = 1f, aFade = 1f - p;
                switch (_cDeath)
                {
                    case DeathStyle.Collapse: rot = 0f; dropY = -95f * p; scY = 1f - 0.5f * p; scX = 1f + 0.16f * p; break;
                    case DeathStyle.Tumble:   rot = side * 430f * p; dropY = -55f * p; break;
                    case DeathStyle.Dissolve: rot = side * 90f * p; dropY = -30f * p; aFade = Mathf.Clamp01(1f - p * 1.5f); break;
                    case DeathStyle.Scatter:  rot = side * 120f * p; dropY = 16f * p; break;
                    default:                  rot = side * 210f * p; dropY = -50f * p; break;
                }
                Vector2 pos = _basePos + _impulse + _knock * (60f * p) + new Vector2(0, dropY);
                _rt.anchoredPosition = pos;
                _rt.localScale = new Vector3(scX, scY, 1f) * (1f - 0.3f * p) * spawnScale;
                _rt.localRotation = Quaternion.Euler(0, 0, rot);
                if (_artRt != null) { _artRt.localScale = new Vector3(_mirror, 1f, 1f); _artRt.localRotation = Quaternion.identity; _artRt.anchoredPosition = Vector2.zero; }
                if (_artGroup != null) _artGroup.alpha = aFade * _reserveDim;
                if (_barGroup != null) _barGroup.alpha = 1f - p;                      // HP bar despawns
                if (_flash != null) _flash.color = new Color(0.05f, 0.05f, 0.08f, p * 0.7f);
                UpdateShadow(pos, spawnScale, aFade);
                return;
            }

            float t = Time.time;
            float ph = _basePos.x * 0.013f + _basePos.y * 0.017f;
            // Personality idle (P1): role sets pace/spring/restlessness; element adds a signature tremor.
            // A tank plods low & slow, an assassin is fast/restless/leaning, a mage floats; fire shivers,
            // lightning twitches, water flows, nature drifts. Blended out the moment combat claims it.
            float bob = _victory ? 12f : 5f * _pBob;
            float step = _victory ? 0f : Mathf.Sin(t * 1.15f * _pFreq + ph) * 3.2f * _pSway;
            float jitter = _victory ? 0f : Mathf.Sin(t * 11f + ph * 3f) * _pJitter;              // restless micro-motion
            float tremor = (_victory || _pTremor <= 0f) ? 0f : Mathf.Sin(t * _pTremorFreq + ph) * _pTremor;  // element shiver
            float hover = _victory ? 0f : Mathf.Sin(t * 1.6f + ph) * _pHover;                    // mages/supports float
            Vector2 idle = new Vector2(step + jitter * 0.4f + tremor,
                                       Mathf.Abs(Mathf.Sin(t * (_victory ? 6f : 2.3f * _pFreq) + ph)) * bob + hover + jitter * 0.5f + (_victory ? 0f : _cStance));
            float breathe = 1f + Mathf.Sin(t * 3f * _pFreq + _basePos.y * 0.01f) * (_victory ? 0.08f : 0.045f);
            Vector2 animOff = Vector2.zero; float animScale = 1f;
            float meshLean = 0f, hitWobble = 0f;
            Color flashC = new Color(1f, 1f, 1f, 0f);

            if (_anim != Anim.None)
            {
                _animTime += dt;
                float p = Mathf.Clamp01(_animTime / _animDur);
                switch (_anim)
                {
                    case Anim.Attack:
                        // anticipation crouch → explosive lunge → follow-through overshoot → settle
                        animOff = _animDir * (AttackCurve(p) * _animDist);
                        if (_animUlt) animScale = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
                        animScale *= 1f + Mathf.Sin(p * Mathf.PI) * 0.06f * (_weight - 1f);   // heavy bodies pop harder on the swing
                        meshLean = animOff.x * 0.16f;   // the body bends into the swing (mesh)
                        break;
                    case Anim.Hit:
                        // Species-specific hit reaction (Phase 3): Golem barely flinches (Stiff), Jelly
                        // ripples hard, Turtle takes it (Slide), Phoenix wobbles in the air, others recoil.
                        float hm = _animMag * (_cHit == HitStyle.Stiff ? 0.4f : _cHit == HitStyle.Ripple ? 1.35f : _cHit == HitStyle.Slide ? 0.7f : 1f);
                        animOff = new Vector2(Mathf.Sin(p * 50f) * (1f - p) * 8f * hm, 0f);
                        if (_cHit == HitStyle.AirWobble) animOff.y = Mathf.Sin(p * 30f) * (1f - p) * 7f * _animMag;
                        _extraTilt = Mathf.Sin(p * 46f) * (1f - p) * 11f * hm;   // head-snap away from the blow
                        flashC = new Color(1f, 1f, 1f, (1f - p) * 0.85f);
                        animScale = 1f + (1f - p) * 0.06f * hm;
                        hitWobble = (1f - p) * 7f * hm * _cElastic;   // elastic bodies ripple more (Jelly), stiff barely (Golem)
                        break;
                    case Anim.Heal:
                        animScale = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
                        flashC = new Color(0.4f, 1f, 0.55f, Mathf.Sin(p * Mathf.PI) * 0.7f);
                        break;
                }
                if (p >= 1f) _anim = Anim.None;
            }

            // Idle roam (Final Combat Presentation pass): when a fighter is NOT mid-attack,
            // knocked, or choreographed, it slowly wanders around its home — sidestep, drift,
            // gentle circling — so the arena feels alive and units never plant on one spot.
            // Blended out the instant combat claims the unit; never touches BasePos, so the
            // choreography/VFX anchoring is unaffected. Presentation only.
            bool atRest = _anim == Anim.None && !_victory && _impulse.sqrMagnitude < 4f && combatOffset.sqrMagnitude < 4f;
            _roamFactor = Mathf.MoveTowards(_roamFactor, atRest ? 1f : 0f, dt * (atRest ? 0.7f : 4f));
            float rph = _basePos.x * 0.017f + _basePos.y * 0.011f;
            Vector2 roam = new Vector2(Mathf.Sin(t * 0.5f + rph) * 40f + Mathf.Sin(t * 0.21f + rph * 2f) * 18f,
                                       Mathf.Sin(t * 0.37f + rph * 1.7f) * 20f) * _roamFactor;

            Vector2 apos = _basePos + idle + animOff + _impulse + combatOffset + roam;
            _vel = (apos - _lastPos) / Mathf.Max(dt, 1e-4f); _lastPos = apos;   // for lean & auto-stretch
            _rt.anchoredPosition = apos;
            _rt.localScale = Vector3.one * (animScale * spawnScale);   // breathing now lives in the mesh, not a uniform scale
            _rt.localRotation = Quaternion.identity;
            ApplyDeform(dt);
            // Feed the mesh-deform animator: chest-rise breathing, idle upper-body sway, attack bend,
            // impact ripple, and idle limb-ripple (only at rest). Makes the flat sprite read animated.
            float breathePx = (breathe - 1f) * ART * 0.5f;
            PushDeform(step * 0.7f, meshLean + (_victory ? 0f : _cLean), breathePx, hitWobble, _roamFactor * _pLimb);
            if (_artGroup != null) _artGroup.alpha = _reserveDim;
            if (_impSilT > 0f) { _impSilT -= dt; flashC = new Color(1f, 1f, 1f, 1f); }   // impact-frame silhouette
            if (_flash != null) _flash.color = flashC;
            UpdateShadow(apos, spawnScale, 1f);
        }

        // Shadow stays on the ground line under the fighter's X; shrinks & fades with
        // height above the ground so jumps/launchers read as airborne.
        void UpdateShadow(Vector2 rootPos, float scale, float aliveAlpha)
        {
            if (_shadowRt == null) return;
            float height = Mathf.Max(0f, rootPos.y - _basePos.y);
            float k = Mathf.Clamp01(height / MAX_JUMP);
            float s = Mathf.Lerp(1f, 0.55f, k) * scale;
            _shadowRt.anchoredPosition = new Vector2(rootPos.x, _basePos.y - FOOT);
            _shadowRt.localScale = new Vector3(s, s * 0.9f, 1f);
            _shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.52f, 0.14f, k) * _reserveDim * aliveAlpha);
        }

        static Text MakeText(RectTransform parent, Font font, string s, int size, Vector2 pos, TextAnchor anchor)
        {
            var go = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>(); tx.font = font; tx.text = s; tx.fontSize = size; tx.alignment = anchor; tx.color = Color.white;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow; tx.verticalOverflow = VerticalWrapMode.Overflow; tx.raycastTarget = false;
            return tx;
        }
    }
}
