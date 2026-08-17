using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // One monster's placeholder visual with PROCEDURAL animation (no sprites):
    // idle float + breathe · dash attack (bigger for ultimate) · hit shake + flash
    // (stronger on crit) · heal green pulse + bounce · death fade + sink.
    // Renders only — never computes outcomes.
    public class UnitView : MonoBehaviour
    {
        Image _panel; Image _hpFill; Text _name; RectTransform _rt;
        Vector2 _basePos; Color _baseColor;
        int _maxHp = 1; bool _dead; float _deadTime;

        enum Anim { None, Attack, Hit, Heal }
        Anim _anim = Anim.None; float _animTime, _animDur; Vector2 _animDir; float _animMag = 1f; bool _animUlt;

        public Vector2 BasePos => _basePos;
        public Color TeamColor => _baseColor;
        public bool IsDead => _dead;

        public void Build(RectTransform parent, Vector2 anchoredPos, Vector2 size, Color color, Font font, string label)
        {
            var go = new GameObject("Unit_" + label, typeof(RectTransform), typeof(Image));
            _rt = go.GetComponent<RectTransform>();
            _rt.SetParent(parent, false);
            _rt.sizeDelta = size;
            _rt.anchoredPosition = anchoredPos;
            _basePos = anchoredPos;
            _panel = go.GetComponent<Image>();
            _panel.color = color; _baseColor = color;

            _name = MakeText(_rt, font, label, 17, new Vector2(0, size.y * 0.5f - 2), TextAnchor.UpperCenter);

            var bg = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
            var bgrt = bg.GetComponent<RectTransform>();
            bgrt.SetParent(_rt, false);
            bgrt.sizeDelta = new Vector2(size.x - 12, 12);
            bgrt.anchoredPosition = new Vector2(0, -size.y * 0.5f + 12);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var fill = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            _hpFill = fill.GetComponent<Image>();
            _hpFill.color = new Color(0.3f, 0.9f, 0.3f, 1f);
            var frt = _hpFill.rectTransform; frt.SetParent(bgrt, false);
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            _hpFill.type = Image.Type.Filled; _hpFill.fillMethod = Image.FillMethod.Horizontal; _hpFill.fillOrigin = 0; _hpFill.fillAmount = 1f;
        }

        public void SetMaxHp(int m) => _maxHp = Mathf.Max(1, m);

        public void SetHp(int cur)
        {
            if (_hpFill != null)
            {
                float f = Mathf.Clamp01(cur / (float)_maxHp);
                _hpFill.fillAmount = f;
                _hpFill.color = f > 0.5f ? new Color(0.3f, 0.9f, 0.3f) : f > 0.25f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.95f, 0.3f, 0.25f);
            }
            if (cur <= 0 && !_dead) PlayDeath();
        }

        public void PlayAttack(Vector2 dir, bool ult) { if (_dead) return; _anim = Anim.Attack; _animTime = 0; _animDur = ult ? 0.5f : 0.32f; _animDir = dir.normalized; _animUlt = ult; }
        public void PlayHit(bool crit) { if (_dead) return; _anim = Anim.Hit; _animTime = 0; _animDur = 0.3f; _animMag = crit ? 2.2f : 1f; }
        public void PlayHeal() { if (_dead) return; _anim = Anim.Heal; _animTime = 0; _animDur = 0.45f; }
        public void PlayDeath() { if (_dead) return; _dead = true; _deadTime = 0; if (_name != null) _name.color = new Color(1, 1, 1, 0.5f); }

        void Update()
        {
            if (_rt == null) return;
            float dt = Time.deltaTime;

            if (_dead)
            {
                _deadTime += dt;
                float p = Mathf.Clamp01(_deadTime / 0.6f);
                _rt.anchoredPosition = _basePos + new Vector2(0, -45f * p);
                _rt.localScale = Vector3.one * (1f - 0.25f * p);
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
                        animOff = _animDir * (Mathf.Sin(p * Mathf.PI) * (_animUlt ? 150f : 90f));
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
            if (_panel != null) _panel.color = panelC;
        }

        static Text MakeText(RectTransform parent, Font font, string s, int size, Vector2 pos, TextAnchor anchor)
        {
            var go = new GameObject("Txt", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false); rt.sizeDelta = new Vector2(200, 28); rt.anchoredPosition = pos;
            var tx = go.GetComponent<Text>();
            tx.font = font; tx.text = s; tx.fontSize = size; tx.alignment = anchor; tx.color = Color.white;
            tx.horizontalOverflow = HorizontalWrapMode.Overflow; tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.raycastTarget = false;
            return tx;
        }
    }
}
