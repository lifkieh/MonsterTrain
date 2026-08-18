using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Procedural monster visual with per-species identity: team-colored frame,
    // species-colored body, nameplate, icon badge. Animations: spawn pop, idle
    // float+breathe, style dash attack, hit shake+flash, heal pulse, death
    // knockback+fade+sink, victory bounce. Smooth + delayed HP bar. Renders only.
    public class UnitView : MonoBehaviour
    {
        Image _frame, _body, _hpFill, _hpDelayed; Text _name; RectTransform _rt;
        Vector2 _basePos; Color _bodyColor;
        int _maxHp = 1; float _targetFrac = 1f, _dispFrac = 1f, _delayFrac = 1f;
        bool _dead, _victory; float _deadTime, _spawnT = 1f; Vector2 _knock;
        Vector2 _impulse; float _reserveScale = 1f, _reserveDim = 1f;   // cinematic: physics push + reserve staging

        enum Anim { None, Attack, Hit, Heal }
        Anim _anim = Anim.None; float _animTime, _animDur, _animDist, _animMag = 1f; Vector2 _animDir; bool _animUlt;

        public Vector2 BasePos => _basePos;
        public bool IsDead => _dead;

        public void Build(RectTransform parent, Vector2 anchoredPos, Vector2 size,
            Color teamColor, Color speciesColor, string name, string initial, Font font)
        {
            var go = new GameObject("Unit_" + name, typeof(RectTransform), typeof(Image));
            _rt = go.GetComponent<RectTransform>(); _rt.SetParent(parent, false);
            _rt.sizeDelta = size; _rt.anchoredPosition = anchoredPos; _basePos = anchoredPos;
            _frame = go.GetComponent<Image>(); _frame.color = teamColor;                 // team frame

            var body = new GameObject("Body", typeof(RectTransform), typeof(Image));
            var brt = body.GetComponent<RectTransform>(); brt.SetParent(_rt, false);
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(7, 7); brt.offsetMax = new Vector2(-7, -7);
            _body = body.GetComponent<Image>(); _body.color = speciesColor; _bodyColor = speciesColor;

            // Nameplate bar at top.
            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            var prt = plate.GetComponent<RectTransform>(); prt.SetParent(_rt, false);
            prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(1, 1); prt.pivot = new Vector2(0.5f, 1);
            prt.sizeDelta = new Vector2(-6, 34); prt.anchoredPosition = new Vector2(0, -3);
            plate.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            _name = MakeText(prt, font, name, 18, Vector2.zero, TextAnchor.MiddleCenter);

            // Icon badge (species initial) top-left.
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var irt = icon.GetComponent<RectTransform>(); irt.SetParent(_rt, false);
            irt.anchorMin = irt.anchorMax = new Vector2(0, 1); irt.pivot = new Vector2(0, 1);
            irt.sizeDelta = new Vector2(44, 44); irt.anchoredPosition = new Vector2(6, -6);
            icon.GetComponent<Image>().color = new Color(speciesColor.r * 0.6f, speciesColor.g * 0.6f, speciesColor.b * 0.6f, 0.95f);
            MakeText(irt, font, initial, 22, Vector2.zero, TextAnchor.MiddleCenter);

            var bg = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
            var bgrt = bg.GetComponent<RectTransform>(); bgrt.SetParent(_rt, false);
            bgrt.sizeDelta = new Vector2(size.x - 14, 14); bgrt.anchoredPosition = new Vector2(0, -size.y * 0.5f + 13);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            _hpDelayed = MakeFill(bgrt, new Color(0.95f, 0.85f, 0.3f, 0.9f));
            _hpFill = MakeFill(bgrt, new Color(0.3f, 0.9f, 0.3f, 1f));

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
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(28, 28); rt.anchoredPosition = new Vector2(-6, -6);
            var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
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
                _rt.anchoredPosition = _basePos + _impulse + _knock * (60f * p) + new Vector2(0, -50f * p);
                _rt.localScale = Vector3.one * (1f - 0.3f * p) * spawnScale;
                _rt.localRotation = Quaternion.Euler(0, 0, _knock.x * 25f * p);
                if (_body != null) _body.color = new Color(0.15f, 0.15f, 0.15f, (1f - p) * 0.85f);
                return;
            }

            float t = Time.time;
            float bob = _victory ? 12f : 4f;
            Vector2 idle = new Vector2(0, Mathf.Abs(Mathf.Sin(t * (_victory ? 6f : 2.2f) + _basePos.x * 0.01f)) * bob);
            float breathe = 1f + Mathf.Sin(t * 3f + _basePos.y * 0.01f) * (_victory ? 0.08f : 0.03f);
            Vector2 animOff = Vector2.zero; float animScale = 1f; Color bodyC = _bodyColor;

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
                        bodyC = Color.Lerp(Color.white, _bodyColor, p);
                        animScale = 1f + (1f - p) * 0.06f * _animMag;
                        break;
                    case Anim.Heal:
                        animScale = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
                        bodyC = Color.Lerp(new Color(0.4f, 1f, 0.55f), _bodyColor, p);
                        break;
                }
                if (p >= 1f) _anim = Anim.None;
            }

            _rt.anchoredPosition = _basePos + idle + animOff + _impulse;
            _rt.localScale = Vector3.one * (breathe * animScale * spawnScale);
            _rt.localRotation = Quaternion.identity;
            if (_body != null) _body.color = new Color(bodyC.r, bodyC.g, bodyC.b, bodyC.a * _reserveDim);
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
