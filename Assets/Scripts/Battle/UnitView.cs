using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Procedural monster visual: idle float+breathe · style-scaled dash attack ·
    // hit shake+flash (stronger crit) · heal pulse · death knockback+fade+sink.
    // HP bar: smooth main fill + delayed "damage" ghost bar. Renders only.
    public class UnitView : MonoBehaviour
    {
        Image _panel, _hpFill, _hpDelayed; Text _name; RectTransform _rt;
        Vector2 _basePos; Color _baseColor;
        int _maxHp = 1; float _targetFrac = 1f, _dispFrac = 1f, _delayFrac = 1f;
        bool _dead; float _deadTime; Vector2 _knock;

        enum Anim { None, Attack, Hit, Heal }
        Anim _anim = Anim.None; float _animTime, _animDur, _animDist, _animMag = 1f; Vector2 _animDir; bool _animUlt;

        public Vector2 BasePos => _basePos;
        public bool IsDead => _dead;

        public void Build(RectTransform parent, Vector2 anchoredPos, Vector2 size, Color color, Font font, string label)
        {
            var go = new GameObject("Unit_" + label, typeof(RectTransform), typeof(Image));
            _rt = go.GetComponent<RectTransform>(); _rt.SetParent(parent, false);
            _rt.sizeDelta = size; _rt.anchoredPosition = anchoredPos; _basePos = anchoredPos;
            _panel = go.GetComponent<Image>(); _panel.color = color; _baseColor = color;

            _name = MakeText(_rt, font, label, 17, new Vector2(0, size.y * 0.5f - 2), TextAnchor.UpperCenter);

            var bg = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
            var bgrt = bg.GetComponent<RectTransform>(); bgrt.SetParent(_rt, false);
            bgrt.sizeDelta = new Vector2(size.x - 12, 14); bgrt.anchoredPosition = new Vector2(0, -size.y * 0.5f + 13);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            _hpDelayed = MakeFill(bgrt, new Color(0.95f, 0.85f, 0.3f, 0.9f));   // delayed ghost (damage lag)
            _hpFill = MakeFill(bgrt, new Color(0.3f, 0.9f, 0.3f, 1f));          // main fill (on top)
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

        public void SetHp(int cur)
        {
            _targetFrac = Mathf.Clamp01(cur / (float)_maxHp);
            if (cur <= 0 && !_dead) PlayDeath(Vector2.zero);
        }

        public void PlayAttack(Vector2 dir, float dist, bool ult) { if (_dead) return; _anim = Anim.Attack; _animTime = 0; _animDur = ult ? 0.5f : 0.32f; _animDir = dir.normalized; _animDist = dist; _animUlt = ult; }
        public void PlayHit(bool crit) { if (_dead) return; _anim = Anim.Hit; _animTime = 0; _animDur = 0.3f; _animMag = crit ? 2.4f : 1f; }
        public void PlayHeal() { if (_dead) return; _anim = Anim.Heal; _animTime = 0; _animDur = 0.45f; }
        public void PlayDeath(Vector2 knock) { if (_dead) return; _dead = true; _deadTime = 0; _knock = knock; if (_name != null) _name.color = new Color(1, 1, 1, 0.5f); }

        void Update()
        {
            if (_rt == null) return;
            float dt = Time.deltaTime;

            // HP bars: main fill snaps smoothly, delayed ghost lags behind.
            _dispFrac = Mathf.MoveTowards(_dispFrac, _targetFrac, 2.5f * dt);
            _delayFrac = Mathf.MoveTowards(_delayFrac, _dispFrac, 0.8f * dt);
            if (_hpFill != null) { _hpFill.fillAmount = _dispFrac; _hpFill.color = _dispFrac > 0.5f ? new Color(0.3f, 0.9f, 0.3f) : _dispFrac > 0.25f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.95f, 0.3f, 0.25f); }
            if (_hpDelayed != null) _hpDelayed.fillAmount = _delayFrac;

            if (_dead)
            {
                _deadTime += dt;
                float p = Mathf.Clamp01(_deadTime / 0.7f);
                _rt.anchoredPosition = _basePos + _knock * (60f * p) + new Vector2(0, -50f * p);   // knockback + sink
                _rt.localScale = Vector3.one * (1f - 0.3f * p);
                _rt.localRotation = Quaternion.Euler(0, 0, _knock.x * 25f * p);
                if (_panel != null) _panel.color = new Color(0.15f, 0.15f, 0.15f, (1f - p) * 0.85f);
                return;
            }

            float t = Time.time;
            Vector2 idle = new Vector2(0, Mathf.Sin(t * 2.2f + _basePos.x * 0.01f) * 4f);
            float breathe = 1f + Mathf.Sin(t * 3f + _basePos.y * 0.01f) * 0.03f;
            Vector2 animOff = Vector2.zero; float animScale = 1f; Color panelC = _baseColor;

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
                        panelC = Color.Lerp(Color.white, _baseColor, p);
                        animScale = 1f + (1f - p) * 0.06f * _animMag;
                        break;
                    case Anim.Heal:
                        animScale = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
                        panelC = Color.Lerp(new Color(0.4f, 1f, 0.55f), _baseColor, p);
                        break;
                }
                if (p >= 1f) _anim = Anim.None;
            }

            _rt.anchoredPosition = _basePos + idle + animOff;
            _rt.localScale = Vector3.one * (breathe * animScale);
            _rt.localRotation = Quaternion.identity;
            if (_panel != null) _panel.color = panelC;
        }

        static Text MakeText(RectTransform parent, Font font, string s, int size, Vector2 pos, TextAnchor anchor)
        {
            var go = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); rt.sizeDelta = new Vector2(200, 28); rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>(); tx.font = font; tx.text = s; tx.fontSize = size; tx.alignment = anchor; tx.color = Color.white;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow; tx.verticalOverflow = VerticalWrapMode.Overflow; tx.raycastTarget = false;
            return tx;
        }
    }
}
