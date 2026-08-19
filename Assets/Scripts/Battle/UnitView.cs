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
        const float ART = 256f;        // sprite display size (4x the 64px source = crisp)
        const float FOOT = 108f;       // sprite center -> ground line (feet) offset
        const float MAX_JUMP = 300f;   // launch height at which the shadow is smallest

        Image _sprite, _flash, _shadow, _hpFill, _hpDelayed; CanvasGroup _artGroup, _barGroup;
        RectTransform _rt, _artRt, _shadowRt, _barsRt; Text _name;
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

        enum Anim { None, Attack, Hit, Heal }
        Anim _anim = Anim.None; float _animTime, _animDur, _animDist, _animMag = 1f; Vector2 _animDir; bool _animUlt;

        public Vector2 BasePos => _basePos;
        public bool IsDead => _dead;

        public void Build(RectTransform parent, Vector2 anchoredPos, Vector2 size,
            Color teamColor, Color speciesColor, string speciesId, string displayName, Font font,
            string element = "", string role = "Bruiser", bool playerSide = false)
        {
            _mirror = playerSide ? -1 : 1;

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
            if (sprite != null)
            {
                var img = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
                var irt = img.GetComponent<RectTransform>(); irt.SetParent(_artRt, false);
                irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;
                _sprite = img.GetComponent<Image>(); _sprite.sprite = sprite; _sprite.preserveAspect = true; _sprite.raycastTarget = false;

                // Silhouette flash: a white copy of the SAME sprite on top (matches the
                // creature outline exactly; alpha driven per-frame). No backing rectangle.
                var fl = new GameObject("Flash", typeof(RectTransform), typeof(Image));
                var frt = fl.GetComponent<RectTransform>(); frt.SetParent(_artRt, false);
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
                _flash = fl.GetComponent<Image>(); _flash.sprite = sprite; _flash.preserveAspect = true;
                _flash.color = new Color(1, 1, 1, 0); _flash.raycastTarget = false;
            }
            else
            {
                // Procedural fallback (rare — all shipped species have real sprites).
                var proc = MonsterArt.Build(_artRt, speciesId, element, role, ART * 0.9f);
                proc.anchorMin = proc.anchorMax = new Vector2(0.5f, 0.5f); proc.anchoredPosition = Vector2.zero;
                var fl = new GameObject("Flash", typeof(RectTransform), typeof(Image));
                var frt = fl.GetComponent<RectTransform>(); frt.SetParent(_artRt, false);
                frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
                _flash = fl.GetComponent<Image>(); _flash.sprite = ProceduralArt.Disc(); _flash.color = new Color(1, 1, 1, 0); _flash.raycastTarget = false;
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
            bgrt.sizeDelta = new Vector2(148, 14); bgrt.anchoredPosition = new Vector2(0, ART * 0.5f + 22f);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f); bg.GetComponent<Image>().raycastTarget = false;
            _hpDelayed = MakeFill(bgrt, new Color(0.95f, 0.3f, 0.28f, 0.92f));   // red "recently lost HP" ghost
            _hpFill = MakeFill(bgrt, new Color(0.3f, 0.9f, 0.3f, 1f));

            // Small floating name above the HP bar.
            var np = new GameObject("Name", typeof(RectTransform));
            var nprt = np.GetComponent<RectTransform>(); nprt.SetParent(_barsRt, false);
            nprt.anchorMin = nprt.anchorMax = new Vector2(0.5f, 0.5f);
            nprt.sizeDelta = new Vector2(240, 30); nprt.anchoredPosition = new Vector2(0, ART * 0.5f + 46f);
            _name = MakeText(nprt, font, displayName, 20, Vector2.zero, TextAnchor.MiddleCenter);
            _name.fontStyle = FontStyle.Bold;

            _lastPos = anchoredPos;
            _spawnT = 0f;   // spawn-pop
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
        public void PlayAttack(Vector2 dir, float dist, bool ult) { if (_dead) return; _anim = Anim.Attack; _animTime = 0; _animDur = ult ? 0.5f : 0.32f; _animDir = dir.normalized; _animDist = dist; _animUlt = ult; }
        public void PlayHit(bool crit) { if (_dead) return; _anim = Anim.Hit; _animTime = 0; _animDur = 0.3f; _animMag = crit ? 2.4f : 1f; }
        public void PlayHeal() { if (_dead) return; _anim = Anim.Heal; _animTime = 0; _animDur = 0.45f; }
        public void PlayDeath(Vector2 knock) { if (_dead) return; _dead = true; _victory = false; _deadTime = 0; _knock = knock; if (_name != null) _name.color = new Color(1, 1, 1, 0.5f); }

        // --- Cinematic presentation (visual only) ---
        public void Knock(Vector2 dir, float strength) { if (_dead) return; _impulse += dir.normalized * strength; }
        public void Launch(float strength) { if (_dead) return; _impulse += new Vector2(0f, strength); }
        public void Dodge(Vector2 dir) { if (_dead) return; _impulse += dir.normalized * 80f; _anim = Anim.Hit; _animTime = 0; _animDur = 0.24f; _animMag = 0.4f; }
        public void SetReserve(bool r) { _reserveScale = r ? 0.62f : 1f; _reserveDim = r ? 0.55f : 1f; }
        public void SetWeight(float w) { _weight = Mathf.Clamp(w, 0.6f, 1.6f); }   // role-driven heft (presentation only)
        public void SetElement(Color c)
        {
            if (_rt == null) return;
            var go = new GameObject("Elem", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_barsRt != null ? _barsRt : _rt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(20, 20); rt.anchoredPosition = new Vector2(-86, ART * 0.5f + 22f);
            var img = go.GetComponent<Image>(); img.sprite = ProceduralArt.Disc(); img.color = c; img.raycastTarget = false;
        }
        public void SetBasePos(Vector2 p) { _basePos = p; }
        public void EnterFrom(Vector2 from, Vector2 to) { _basePos = to; _impulse = from - to; }   // slide in via decaying impulse

        // --- Phase O deform hooks (visual only) ---
        public void Squash(float sx, float sy, float dur) { if (_dead) return; _sqCur = new Vector2(sx, sy); _sqT = _sqDur = Mathf.Max(0.01f, dur); }
        public void Spin(float degPerSec, float dur) { if (_dead) return; _spinSpeed = degPerSec; _spinT = Mathf.Max(_spinT, dur); }
        public void Vibrate(float mag, float dur) { if (_dead) return; _vibMag = Mathf.Max(_vibMag, mag); _vibT = Mathf.Max(_vibT, dur); }
        public void ImpactSilhouette(float dur) { _impSilT = Mathf.Max(0.01f, dur); }
        public Sprite CurrentSprite => _sprite != null ? _sprite.sprite : null;
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
            else { _spinAngle = Mathf.Lerp(_spinAngle, 0f, 10f * dt); rot = _spinAngle + Mathf.Clamp(-_vel.x * 0.02f * _weight, -16f, 16f) * _mirror; }   // heavier bodies lean harder into motion
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
            if (_hpFill != null) { _hpFill.fillAmount = _dispFrac; _hpFill.color = _dispFrac > 0.5f ? new Color(0.3f, 0.9f, 0.3f) : _dispFrac > 0.25f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.95f, 0.3f, 0.25f); }
            if (_hpDelayed != null) _hpDelayed.fillAmount = _delayFrac;

            if (_spawnT < 1f) _spawnT = Mathf.Min(1f, _spawnT + dt / 0.3f);
            float spawnScale = Mathf.SmoothStep(0.2f, 1f, _spawnT) * _reserveScale;

            // Cinematic impulse (knockback / launch / slide-in) — slower ease-out (~0.25 s)
            // so victim knockback reads heavy; the engagement system re-closes the gap.
            _impulse = Vector2.Lerp(_impulse, Vector2.zero, 4.5f * dt);
            if (_impulse.sqrMagnitude < 0.25f) _impulse = Vector2.zero;

            if (_dead)
            {
                _deadTime += dt;
                float p = Mathf.Clamp01(_deadTime / 0.7f);
                Vector2 pos = _basePos + _impulse + _knock * (60f * p) + new Vector2(0, -50f * p);
                _rt.anchoredPosition = pos;
                _rt.localScale = Vector3.one * (1f - 0.3f * p) * spawnScale;
                _rt.localRotation = Quaternion.Euler(0, 0, (_knock.x >= 0f ? 1f : -1f) * 210f * p);   // launched spin
                if (_artRt != null) { _artRt.localScale = new Vector3(_mirror, 1f, 1f); _artRt.localRotation = Quaternion.identity; _artRt.anchoredPosition = Vector2.zero; }
                if (_artGroup != null) _artGroup.alpha = (1f - p) * _reserveDim;      // dissolve
                if (_barGroup != null) _barGroup.alpha = 1f - p;                      // HP bar despawns
                if (_flash != null) _flash.color = new Color(0.05f, 0.05f, 0.08f, p * 0.7f);
                UpdateShadow(pos, spawnScale, (1f - p));
                return;
            }

            float t = Time.time;
            // Weight sell: light monsters bounce fast + springy, heavy ones plod slow + settled.
            float light = Mathf.Lerp(1.28f, 0.72f, Mathf.InverseLerp(0.6f, 1.6f, _weight));
            float bob = (_victory ? 12f : 4f) * (_victory ? 1f : light);
            Vector2 idle = new Vector2(0, Mathf.Abs(Mathf.Sin(t * (_victory ? 6f : 2.2f * light) + _basePos.x * 0.01f)) * bob);
            float breathe = 1f + Mathf.Sin(t * 3f * light + _basePos.y * 0.01f) * (_victory ? 0.08f : 0.03f * light);
            Vector2 animOff = Vector2.zero; float animScale = 1f;
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
                        break;
                    case Anim.Hit:
                        animOff = new Vector2(Mathf.Sin(p * 50f) * (1f - p) * 8f * _animMag, 0f);
                        _extraTilt = Mathf.Sin(p * 46f) * (1f - p) * 11f * _animMag;   // head-snap away from the blow
                        flashC = new Color(1f, 1f, 1f, (1f - p) * 0.85f);
                        animScale = 1f + (1f - p) * 0.06f * _animMag;
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
            _rt.localScale = Vector3.one * (breathe * animScale * spawnScale);
            _rt.localRotation = Quaternion.identity;
            ApplyDeform(dt);
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
