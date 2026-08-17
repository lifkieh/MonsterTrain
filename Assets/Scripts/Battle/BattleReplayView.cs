using System;
using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Visual battle replay: consumes the classified ReplayEvent stream for
    // animation triggers and BattlePlayback for HP — never re-simulates.
    // Species attack styles, pooled projectiles/VFX, hit-stop, camera shake/zoom,
    // auto-pacing. Presentation only; determinism untouched.
    public class BattleReplayView : MonoBehaviour
    {
        public float speedMultiplier = 1f;
        public event Action<int> OnFinished;

        static readonly Color CWhite = Color.white;
        static readonly Color CHeal = new Color(0.4f, 1f, 0.55f);
        static readonly Color CCrit = new Color(1f, 0.9f, 0.2f);
        static readonly Color COrange = new Color(1f, 0.6f, 0.15f);

        readonly BattlePlayback _pb = new BattlePlayback();
        readonly Dictionary<int, UnitView> _views = new Dictionary<int, UnitView>();
        readonly Dictionary<int, AttackStyle> _styleByKey = new Dictionary<int, AttackStyle>();
        readonly Dictionary<int, string> _speciesByKey = new Dictionary<int, string>();
        List<ReplayEvent> _replay; int _rIdx;
        RectTransform _root, _stage; Font _font; FloatingTextPool _texts; BattleFx _fx;
        Dictionary<string, AttackStyle> _styleMap;
        double _clock, _simPerReal; bool _playing, _finishedFired;
        float _shakeT, _shakeMag, _zoom = 1f, _zoomTarget = 1f, _hitstop;

        static int Key(int t, int s) => t * 100 + s;
        public void SetSpeed(float m) => speedMultiplier = m;

        public void Play(BattleResult result, List<ReplayEvent> replay,
            Dictionary<string, AttackStyle> styleMap, RectTransform parent, Font font)
        {
            _font = font; _root = parent; _replay = replay; _styleMap = styleMap; _rIdx = 0;
            _clock = 0; _playing = true; _finishedFired = false;
            _zoom = 1f; _zoomTarget = 1f; _shakeT = _shakeMag = _hitstop = 0f;
            _pb.Init(result);

            // Auto-pace: 15–60 s window; close matches longer, stomps faster.
            double sim = Math.Max(1.0, result.duration);
            double target = Math.Min(60.0, Math.Max(15.0, sim * 1.5));
            var d = BattleDrama.Compute(result);
            bool close = d.winnerAlive <= 1 || d.leadChanges >= 2;
            bool stomp = d.winnerAlive >= 3 && d.leadChanges == 0;
            if (close) target = Math.Min(60.0, target * 1.25);
            else if (stomp) target = Math.Max(12.0, target * 0.7);
            _simPerReal = sim / target;

            BuildStage();
        }

        void BuildStage()
        {
            if (_stage != null) Destroy(_stage.gameObject);
            var go = new GameObject("Stage", typeof(RectTransform));
            _stage = go.GetComponent<RectTransform>(); _stage.SetParent(_root, false);
            _stage.anchorMin = _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.sizeDelta = new Vector2(1080, 1600); _stage.anchoredPosition = Vector2.zero;
            _texts = new FloatingTextPool(_stage, _font);
            _fx = new BattleFx(_stage);
            _views.Clear(); _styleByKey.Clear(); _speciesByKey.Clear();

            var size = new Vector2(230, 150);
            float[] rowY = { 380f, 0f, -380f };
            foreach (var u in _pb.Units)
            {
                int slot = Mathf.Clamp(u.slot, 0, 2);
                float depth = slot * 30f;
                float x = u.team == 0 ? -280f - depth : 280f + depth;
                var teamColor = u.team == 0 ? new Color(0.2f, 0.4f, 0.75f) : new Color(0.75f, 0.28f, 0.26f);
                var sc = SpeciesIdentity.ColorFor(u.speciesId);
                var speciesColor = new Color(sc.r, sc.g, sc.b);
                var v = new GameObject("UnitView").AddComponent<UnitView>();
                v.transform.SetParent(_stage, false);
                v.Build(_stage, new Vector2(x, rowY[slot]), size, teamColor, speciesColor,
                    u.speciesId, SpeciesIdentity.Initial(u.speciesId), _font);
                v.SetMaxHp(u.maxHp); v.SetHp(u.currentHp); v.PlaySpawn();
                int k = Key(u.team, u.slot);
                _views[k] = v;
                _speciesByKey[k] = u.speciesId;
                _styleByKey[k] = (_styleMap != null && _styleMap.TryGetValue(u.speciesId, out var st)) ? st : AttackStyle.MeleeLunge;
            }
        }

        void Update()
        {
            if (_playing)
            {
                if (_hitstop > 0f)
                {
                    _hitstop -= Time.deltaTime;                       // dramatic freeze
                }
                else
                {
                    _clock += Time.deltaTime * _simPerReal * speedMultiplier;
                    while (_replay != null && _rIdx < _replay.Count && _replay[_rIdx].t <= _clock)
                        Apply(_replay[_rIdx++]);
                    _pb.ProcessUpTo(_clock);
                }

                foreach (var u in _pb.Units)
                    if (_views.TryGetValue(Key(u.team, u.slot), out var v)) v.SetHp(u.currentHp);

                bool eventsDone = _replay == null || _rIdx >= _replay.Count;
                if (!_finishedFired && eventsDone && _clock >= _pb.Duration)
                {
                    _finishedFired = true; _playing = false; _zoomTarget = 1.08f;
                    foreach (var wu in _pb.Units)
                        if (wu.team == _pb.WinnerTeam && wu.Alive && _views.TryGetValue(Key(wu.team, wu.slot), out var wv))
                            wv.PlayVictory();
                    AudioManager.Play(Sfx.Victory);
                    OnFinished?.Invoke(_pb.WinnerTeam);
                }
            }
            UpdateCamera();
        }

        AttackStyle StyleOf(int team, int slot) =>
            _styleByKey.TryGetValue(Key(team, slot), out var s) ? s : AttackStyle.MeleeLunge;

        Vector2 PosOf(int team, int slot) =>
            _views.TryGetValue(Key(team, slot), out var v) ? v.BasePos : Vector2.zero;

        void Apply(ReplayEvent e)
        {
            switch (e.kind)
            {
                case ReplayEventKind.Attack:
                case ReplayEventKind.Skill:
                case ReplayEventKind.Ultimate:
                {
                    bool ult = e.kind == ReplayEventKind.Ultimate;
                    var st = StyleOf(e.actorTeam, e.actorSlot);
                    Vector2 dir = new Vector2(e.actorTeam == 0 ? 1f : -1f, 0f);
                    Vector2 actorPos = PosOf(e.actorTeam, e.actorSlot);

                    string actorSp = _speciesByKey.TryGetValue(Key(e.actorTeam, e.actorSlot), out var asp) ? asp : "";
                    if (_views.TryGetValue(Key(e.actorTeam, e.actorSlot), out var av))
                    {
                        av.PlayAttack(dir, DashDist(st, ult), ult);
                        // Unique per-species skill banner.
                        if (e.kind == ReplayEventKind.Skill)
                            _texts.Spawn(av.BasePos + new Vector2(0, 95), SpeciesIdentity.SkillWord(actorSp), COrange, 30);
                        if (ult)
                        {
                            _texts.Spawn(av.BasePos + new Vector2(0, 100), "ULTIMATE", COrange, 42);
                            _texts.Spawn(av.BasePos + new Vector2(0, 55), SpeciesIdentity.SkillWord(actorSp), COrange, 26);
                            _fx.Burst(av.BasePos, BurstKind.Ultimate); ZoomPunch(0.10f); Shake(16f);
                        }
                    }

                    if (e.kind == ReplayEventKind.Skill) AudioManager.Play(Sfx.Skill);
                    else if (ult) AudioManager.Play(Sfx.Ultimate);

                    if (!e.isBuff)
                    {
                        int tt = e.targetTeam, ts = e.targetSlot, amt = e.amount; bool crit = e.crit;
                        if (AttackStyles.IsRanged(st))
                            _fx.Projectile(actorPos, PosOf(tt, ts), ProjColor(st), () => DoHit(tt, ts, amt, crit, ult, st, actorSp));
                        else
                            DoHit(tt, ts, amt, crit, ult, st, actorSp);
                    }
                    break;
                }
                case ReplayEventKind.Heal:
                    if (_views.TryGetValue(Key(e.targetTeam, e.targetSlot), out var hv))
                    {
                        hv.PlayHeal();
                        _texts.Spawn(hv.BasePos + Jitter(), "+" + e.amount, CHeal, 30);
                        _fx.Burst(hv.BasePos, BurstKind.Heal);
                        AudioManager.Play(Sfx.Heal);
                    }
                    break;
                case ReplayEventKind.Death:
                {
                    Vector2 knock = new Vector2(e.targetTeam == 0 ? -1f : 1f, 0f);   // away from enemy
                    if (_views.TryGetValue(Key(e.targetTeam, e.targetSlot), out var dv)) dv.PlayDeath(knock);
                    Shake(9f); AudioManager.Play(Sfx.Death);
                    break;
                }
                case ReplayEventKind.Victory:
                    _zoomTarget = 1.1f;
                    break;
            }
        }

        void DoHit(int tt, int ts, int amt, bool crit, bool ult, AttackStyle st, string actorSp)
        {
            Vector2 tpos = PosOf(tt, ts);
            if (_views.TryGetValue(Key(tt, ts), out var tv)) tv.PlayHit(crit);
            _texts.Spawn(tpos + Jitter(), amt.ToString(), crit ? CCrit : CWhite, crit ? 42 : 30);
            AudioManager.Play(crit ? Sfx.Crit : Sfx.Hit);
            if (crit) _texts.Spawn(tpos + new Vector2(0, -70), SpeciesIdentity.CritWord(actorSp), CCrit, 34);
            _fx.Burst(tpos, ult ? BurstKind.Ultimate : crit ? BurstKind.Crit : MeleeBurst(st));
            HitStop(crit || ult ? 0.08f : 0.04f);
            Shake(ult ? 18f : crit ? 12f : 6f);
            if (crit) ZoomPunch(0.05f);
        }

        static float DashDist(AttackStyle s, bool ult)
        {
            switch (s)
            {
                case AttackStyle.HeavySmash: return ult ? 170f : 120f;
                case AttackStyle.AssassinDash: return ult ? 210f : 155f;
                case AttackStyle.MeleeLunge: return ult ? 120f : 80f;
                default: return ult ? 70f : 40f;   // ranged/mage: small step, projectile does the travel
            }
        }

        static BurstKind MeleeBurst(AttackStyle s) => s == AttackStyle.HeavySmash ? BurstKind.Impact : BurstKind.Slash;
        static Color ProjColor(AttackStyle s) => s == AttackStyle.MageCast ? new Color(0.6f, 0.5f, 1f) : new Color(1f, 0.9f, 0.4f);
        static Vector2 Jitter() => new Vector2(UnityEngine.Random.Range(-24f, 24f), 40f);

        void HitStop(float d) => _hitstop = Mathf.Max(_hitstop, d);
        void Shake(float mag) { _shakeT = 0.25f; _shakeMag = Mathf.Max(_shakeMag, mag); }
        void ZoomPunch(float amt) { _zoom += amt; }

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
            else _stage.anchoredPosition = Vector2.Lerp(_stage.anchoredPosition, Vector2.zero, 12f * Time.deltaTime);

            _zoom = Mathf.Lerp(_zoom, _zoomTarget, 4f * Time.deltaTime);
            _stage.localScale = Vector3.one * _zoom;
        }
    }
}
