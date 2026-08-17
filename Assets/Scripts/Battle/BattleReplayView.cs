using System;
using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Visual battle replay. Consumes the classified ReplayEvent stream for
    // animation triggers and BattlePlayback for HP bars — never re-simulates.
    // Stages 3v3 in lanes, paces the replay into a watchable window, drives unit
    // animations + floating text + camera shake. Presentation only.
    public class BattleReplayView : MonoBehaviour
    {
        public float speedMultiplier = 1f;             // 0.5 / 1 / 2 / 4
        public event Action<int> OnFinished;           // winnerTeam (0 = player)

        static readonly Color CWhite = Color.white;
        static readonly Color CHeal = new Color(0.4f, 1f, 0.55f);
        static readonly Color CCrit = new Color(1f, 0.9f, 0.2f);
        static readonly Color COrange = new Color(1f, 0.6f, 0.15f);

        readonly BattlePlayback _pb = new BattlePlayback();
        readonly Dictionary<int, UnitView> _views = new Dictionary<int, UnitView>();
        List<ReplayEvent> _replay; int _rIdx;
        RectTransform _root, _stage; Font _font; FloatingTextPool _texts;
        double _clock, _simPerReal; bool _playing, _finishedFired;
        float _shakeT, _shakeMag, _zoom = 1f, _zoomTarget = 1f;

        static int Key(int t, int s) => t * 100 + s;

        public void SetSpeed(float m) => speedMultiplier = m;

        public void Play(BattleResult result, List<ReplayEvent> replay, RectTransform parent, Font font)
        {
            _font = font; _root = parent; _replay = replay; _rIdx = 0;
            _clock = 0; _playing = true; _finishedFired = false;
            _zoom = 1f; _zoomTarget = 1f; _shakeT = 0f; _shakeMag = 0f;
            _pb.Init(result);
            // Pace: play the whole sim across a 15–60 s window (short→long).
            double sim = Math.Max(1.0, result.duration);
            double target = Math.Min(60.0, Math.Max(15.0, sim * 1.5));
            _simPerReal = sim / target;
            BuildStage();
        }

        void BuildStage()
        {
            if (_stage != null) Destroy(_stage.gameObject);
            var go = new GameObject("Stage", typeof(RectTransform));
            _stage = go.GetComponent<RectTransform>();
            _stage.SetParent(_root, false);
            _stage.anchorMin = _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.sizeDelta = new Vector2(1080, 1600);
            _stage.anchoredPosition = Vector2.zero;
            _texts = new FloatingTextPool(_stage, _font);
            _views.Clear();

            var size = new Vector2(230, 150);
            float[] rowY = { 380f, 0f, -380f };            // 3 lanes, portrait-safe
            foreach (var u in _pb.Units)
            {
                int slot = Mathf.Clamp(u.slot, 0, 2);
                float depth = slot * 30f;                  // front (slot0) nearest centre
                float x = u.team == 0 ? -280f - depth : 280f + depth;
                float y = rowY[slot];
                var color = u.team == 0 ? new Color(0.25f, 0.5f, 0.85f) : new Color(0.85f, 0.35f, 0.32f);
                var v = new GameObject("UnitView").AddComponent<UnitView>();
                v.transform.SetParent(_stage, false);
                v.Build(_stage, new Vector2(x, y), size, color, _font, u.speciesId);
                v.SetMaxHp(u.maxHp); v.SetHp(u.currentHp);
                _views[Key(u.team, u.slot)] = v;
            }
        }

        void Update()
        {
            if (_playing)
            {
                _clock += Time.deltaTime * _simPerReal * speedMultiplier;

                while (_replay != null && _rIdx < _replay.Count && _replay[_rIdx].t <= _clock)
                    Apply(_replay[_rIdx++]);

                _pb.ProcessUpTo(_clock);
                foreach (var u in _pb.Units)
                    if (_views.TryGetValue(Key(u.team, u.slot), out var v)) v.SetHp(u.currentHp);

                bool eventsDone = _replay == null || _rIdx >= _replay.Count;
                if (!_finishedFired && eventsDone && _clock >= _pb.Duration)
                {
                    _finishedFired = true; _playing = false; _zoomTarget = 1.08f;   // victory zoom
                    OnFinished?.Invoke(_pb.WinnerTeam);
                }
            }
            UpdateCamera();
        }

        void Apply(ReplayEvent e)
        {
            switch (e.kind)
            {
                case ReplayEventKind.Attack:
                case ReplayEventKind.Skill:
                case ReplayEventKind.Ultimate:
                {
                    bool ult = e.kind == ReplayEventKind.Ultimate;
                    if (_views.TryGetValue(Key(e.actorTeam, e.actorSlot), out var av))
                    {
                        av.PlayAttack(new Vector2(e.actorTeam == 0 ? 1f : -1f, 0f), ult);
                        if (e.kind == ReplayEventKind.Skill) _texts.Spawn(av.BasePos + new Vector2(0, 90), "SKILL", COrange, 30);
                        if (ult) _texts.Spawn(av.BasePos + new Vector2(0, 90), "ULTIMATE", COrange, 40);
                    }
                    if (!e.isBuff && _views.TryGetValue(Key(e.targetTeam, e.targetSlot), out var tv))
                    {
                        tv.PlayHit(e.crit);
                        _texts.Spawn(tv.BasePos + Jitter(), e.amount.ToString(), e.crit ? CCrit : CWhite, e.crit ? 42 : 30);
                        if (e.crit) _texts.Spawn(tv.BasePos + new Vector2(0, -70), "CRIT!", CCrit, 34);
                    }
                    Shake(ult ? 18f : e.crit ? 11f : 5f);   // camera punch on ultimate
                    break;
                }
                case ReplayEventKind.Heal:
                    if (_views.TryGetValue(Key(e.targetTeam, e.targetSlot), out var hv))
                    {
                        hv.PlayHeal();
                        _texts.Spawn(hv.BasePos + Jitter(), "+" + e.amount, CHeal, 30);
                    }
                    break;
                case ReplayEventKind.Death:
                    if (_views.TryGetValue(Key(e.targetTeam, e.targetSlot), out var dv)) dv.PlayDeath();
                    Shake(8f);
                    break;
                case ReplayEventKind.Victory:
                    _zoomTarget = 1.1f;
                    break;
            }
        }

        static Vector2 Jitter() => new Vector2(UnityEngine.Random.Range(-24f, 24f), 40f);

        void Shake(float mag) { _shakeT = 0.25f; _shakeMag = Mathf.Max(_shakeMag, mag); }

        void UpdateCamera()
        {
            if (_stage == null) return;
            if (_shakeT > 0f)
            {
                _shakeT -= Time.deltaTime;
                float m = _shakeMag * Mathf.Clamp01(_shakeT / 0.25f);
                _stage.anchoredPosition = new Vector2(UnityEngine.Random.Range(-m, m), UnityEngine.Random.Range(-m, m));
                if (_shakeT <= 0f) { _shakeMag = 0f; _stage.anchoredPosition = Vector2.zero; }
            }
            else
            {
                _stage.anchoredPosition = Vector2.Lerp(_stage.anchoredPosition, Vector2.zero, 12f * Time.deltaTime);
            }
            _zoom = Mathf.Lerp(_zoom, _zoomTarget, 4f * Time.deltaTime);
            _stage.localScale = Vector3.one * _zoom;
        }
    }
}
