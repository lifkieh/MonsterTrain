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

        Image _sprite, _flash, _shadow, _hpFill, _hpDelayed; CanvasGroup _artGroup;
        RectTransform _rt, _artRt, _shadowRt; Text _name;
        Vector2 _basePos; int _mirror = 1;
        int _maxHp = 1; float _targetFrac = 1f, _dispFrac = 1f, _delayFrac = 1f;
        bool _dead, _victory; float _deadTime, _spawnT = 1f; Vector2 _knock;
        Vector2 _impulse; float _reserveScale = 1f, _reserveDim = 1f;   // cinematic push + reserve staging
        public Vector2 combatOffset;    // view-driven fight choreography (dash / launch / slam)

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
            _shadowRt.sizeDelta = new Vector2(150, 42); _shadowRt.anchoredPosition = anchoredPos + new Vector2(0, -FOOT);
            _shadow = sgo.GetComponent<Image>(); _shadow.sprite = ProceduralArt.Glow();
            _shadow.color = new Color(0f, 0f, 0f, 0.45f); _shadow.raycastTarget = false;

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

            // Floating HP bar above the head.
            var bg = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
            var bgrt = bg.GetComponent<RectTransform>(); bgrt.SetParent(_rt, false);
            bgrt.anchorMin = bgrt.anchorMax = new Vector2(0.5f, 0.5f);
            bgrt.sizeDelta = new Vector2(148, 14); bgrt.anchoredPosition = new Vector2(0, ART * 0.5f + 22f);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f); bg.GetComponent<Image>().raycastTarget = false;
            _hpDelayed = MakeFill(bgrt, new Color(0.95f, 0.85f, 0.3f, 0.9f));
            _hpFill = MakeFill(bgrt, new Color(0.3f, 0.9f, 0.3f, 1f));

            // Small floating name above the HP bar.
            var np = new GameObject("Name", typeof(RectTransform));
            var nprt = np.GetComponent<RectTransform>(); nprt.SetParent(_rt, false);
            nprt.anchorMin = nprt.anchorMax = new Vector2(0.5f, 0.5f);
            nprt.sizeDelta = new Vector2(240, 30); nprt.anchoredPosition = new Vector2(0, ART * 0.5f + 46f);
            _name = MakeText(nprt, font, displayName, 20, Vector2.zero, TextAnchor.MiddleCenter);
            _name.fontStyle = FontStyle.Bold;

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
        public void SetElement(Color c)
        {
            if (_rt == null) return;
            var go = new GameObject("Elem", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_rt, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(20, 20); rt.anchoredPosition = new Vector2(-86, ART * 0.5f + 22f);
            var img = go.GetComponent<Image>(); img.sprite = ProceduralArt.Disc(); img.color = c; img.raycastTarget = false;
        }
        public void SetBasePos(Vector2 p) { _basePos = p; }
        public void EnterFrom(Vector2 from, Vector2 to) { _basePos = to; _impulse = from - to; }   // slide in via decaying impulse

        void Update()
        {
            if (_rt == null) return;
            float dt = Time.deltaTime;

            _dispFrac = Mathf.MoveTowards(_dispFrac, _targetFrac, 2.5f * dt);
            _delayFrac = Mathf.MoveTowards(_delayFrac, _dispFrac, 0.8f * dt);
            if (_hpFill != null) { _hpFill.fillAmount = _dispFrac; _hpFill.color = _dispFrac > 0.5f ? new Color(0.3f, 0.9f, 0.3f) : _dispFrac > 0.25f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.95f, 0.3f, 0.25f); }
            if (_hpDelayed != null) _hpDelayed.fillAmount = _delayFrac;

            if (_spawnT < 1f) _spawnT = Mathf.Min(1f, _spawnT + dt / 0.3f);
            float spawnScale = Mathf.SmoothStep(0.2f, 1f, _spawnT) * _reserveScale;

            // Cinematic impulse (knockback / launch / slide-in) decays toward rest.
            _impulse = Vector2.Lerp(_impulse, Vector2.zero, 7f * dt);
            if (_impulse.sqrMagnitude < 0.25f) _impulse = Vector2.zero;

            if (_dead)
            {
                _deadTime += dt;
                float p = Mathf.Clamp01(_deadTime / 0.7f);
                Vector2 pos = _basePos + _impulse + _knock * (60f * p) + new Vector2(0, -50f * p);
                _rt.anchoredPosition = pos;
                _rt.localScale = Vector3.one * (1f - 0.3f * p) * spawnScale;
                _rt.localRotation = Quaternion.Euler(0, 0, _knock.x * 25f * p);
                if (_artGroup != null) _artGroup.alpha = (1f - p) * _reserveDim;      // dissolve
                if (_flash != null) _flash.color = new Color(0.05f, 0.05f, 0.08f, p * 0.7f);
                UpdateShadow(pos, spawnScale, (1f - p));
                return;
            }

            float t = Time.time;
            float bob = _victory ? 12f : 4f;
            Vector2 idle = new Vector2(0, Mathf.Abs(Mathf.Sin(t * (_victory ? 6f : 2.2f) + _basePos.x * 0.01f)) * bob);
            float breathe = 1f + Mathf.Sin(t * 3f + _basePos.y * 0.01f) * (_victory ? 0.08f : 0.03f);
            Vector2 animOff = Vector2.zero; float animScale = 1f;
            Color flashC = new Color(1f, 1f, 1f, 0f);

            if (_anim != Anim.None)
            {
                _animTime += dt;
                float p = Mathf.Clamp01(_animTime / _animDur);
                switch (_anim)
                {
                    case Anim.Attack:
                        animOff = _animDir * (Mathf.Sin(p * Mathf.PI) * _animDist);
                        if (_animUlt) animScale = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
                        break;
                    case Anim.Hit:
                        animOff = new Vector2(Mathf.Sin(p * 50f) * (1f - p) * 8f * _animMag, 0f);
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

            Vector2 apos = _basePos + idle + animOff + _impulse + combatOffset;
            _rt.anchoredPosition = apos;
            _rt.localScale = Vector3.one * (breathe * animScale * spawnScale);
            _rt.localRotation = Quaternion.identity;
            if (_artGroup != null) _artGroup.alpha = _reserveDim;
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
            _shadowRt.localScale = new Vector3(s, s, 1f);
            _shadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.45f, 0.15f, k) * _reserveDim * aliveAlpha);
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
