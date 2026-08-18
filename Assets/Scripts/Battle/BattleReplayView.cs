using System;
using System.Collections;
using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Cinematic fighting-game replay. Consumes the classified ReplayEvent stream
    // + the deterministic BattleCinematicDirector choreography for staging and
    // choreography; drives HP from BattlePlayback. NEVER re-simulates — winner and
    // logHash are the simulator's, untouched. Fighting-game 1v1 staging (active
    // fighters centered, reserves behind, next challenger runs in on death),
    // procedural combos, presentation dodges, knockback/launch, camera cues,
    // slow-mo finishers, and a parallax arena. Presentation only.
    public class BattleReplayView : MonoBehaviour
    {
        public float speedMultiplier = 1f;
        public event Action<int> OnFinished;

        static readonly Color CWhite = Color.white;
        static readonly Color CHeal = new Color(0.4f, 1f, 0.55f);
        static readonly Color CCrit = new Color(1f, 0.9f, 0.2f);
        static readonly Color COrange = new Color(1f, 0.6f, 0.15f);
        static readonly Color CDodge = new Color(0.7f, 0.9f, 1f);

        readonly BattlePlayback _pb = new BattlePlayback();
        readonly Dictionary<int, UnitView> _views = new Dictionary<int, UnitView>();
        readonly Dictionary<int, AttackStyle> _styleByKey = new Dictionary<int, AttackStyle>();
        readonly Dictionary<int, string> _speciesByKey = new Dictionary<int, string>();
        List<ReplayEvent> _replay; Choreography _cho; int _rIdx;
        RectTransform _root, _stage, _hud; Font _font; FloatingTextPool _texts; BattleFx _fx; BattleArena _arena;
        readonly List<Image> _pips0 = new List<Image>(), _pips1 = new List<Image>();
        Dictionary<string, AttackStyle> _styleMap;
        double _clock, _simPerReal; bool _playing, _finishedFired;
        float _shakeT, _shakeDur = 0.25f, _shakeMag, _zoom = 1f, _zoomTarget = 1f, _hitstop;
        float _slowmo = 1f, _slowmoT;

        static int Key(int t, int s) => t * 100 + s;
        public void SetSpeed(float m) => speedMultiplier = m;

        public void Play(BattleResult result, List<ReplayEvent> replay,
            Dictionary<string, AttackStyle> styleMap, RectTransform parent, Font font)
        {
            _font = font; _root = parent; _replay = replay; _styleMap = styleMap; _rIdx = 0;
            _clock = 0; _playing = true; _finishedFired = false;
            _zoom = 1f; _zoomTarget = 1f; _shakeT = _shakeMag = _hitstop = 0f; _slowmo = 1f; _slowmoT = 0f;
            _pb.Init(result);
            _cho = BattleCinematicDirector.Choreograph(result, replay);   // deterministic (seeded by logHash)

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
            if (_arena != null) _arena.Destroy();
            var go = new GameObject("Stage", typeof(RectTransform));
            _stage = go.GetComponent<RectTransform>(); _stage.SetParent(_root, false);
            _stage.anchorMin = _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.sizeDelta = new Vector2(1080, 1600); _stage.anchoredPosition = Vector2.zero;

            _arena = new BattleArena(); _arena.Build(_stage);   // procedural arena, behind fighters
            _texts = new FloatingTextPool(_stage, _font);
            _fx = new BattleFx(_stage);
            _views.Clear(); _styleByKey.Clear(); _speciesByKey.Clear();

            var size = new Vector2(240, 160);
            foreach (var u in _pb.Units)
            {
                var teamColor = u.team == 0 ? new Color(0.2f, 0.4f, 0.75f) : new Color(0.75f, 0.28f, 0.26f);
                var sc = SpeciesIdentity.ColorFor(u.speciesId);
                var speciesColor = new Color(sc.r, sc.g, sc.b);
                var v = new GameObject("UnitView").AddComponent<UnitView>();
                v.transform.SetParent(_stage, false);
                v.Build(_stage, Vector2.zero, size, teamColor, speciesColor,
                    u.speciesId, SpeciesIdentity.Initial(u.speciesId), _font);
                v.SetMaxHp(u.maxHp); v.SetHp(u.currentHp); v.PlaySpawn();
                int k = Key(u.team, u.slot);
                _views[k] = v;
                _speciesByKey[k] = u.speciesId;
                _styleByKey[k] = (_styleMap != null && _styleMap.TryGetValue(u.speciesId, out var st)) ? st : AttackStyle.MeleeLunge;
            }
            RelayoutTeam(0, false);
            RelayoutTeam(1, false);
            IntroApproach();
            BuildHud();
        }

        // Fighting-game round pips: player team left, enemy right, screen-fixed
        // (parented to _root, so camera zoom/shake never move it). Pips deplete as
        // monsters fall — a clear "who's winning" read over the arena.
        void BuildHud()
        {
            if (_hud != null) Destroy(_hud.gameObject);
            var go = new GameObject("Hud", typeof(RectTransform));
            _hud = go.GetComponent<RectTransform>(); _hud.SetParent(_root, false); _hud.SetAsLastSibling();
            _hud.anchorMin = _hud.anchorMax = new Vector2(0.5f, 0.5f);
            _hud.sizeDelta = new Vector2(1080, 120); _hud.anchoredPosition = new Vector2(0, 740);
            _pips0.Clear(); _pips1.Clear();
            HudLabel("VS", 40, Vector2.zero, new Vector2(200, 70));
            for (int i = 0; i < CountTeam(0); i++) _pips0.Add(Pip(new Vector2(-160 - i * 74, 0)));
            for (int i = 0; i < CountTeam(1); i++) _pips1.Add(Pip(new Vector2(160 + i * 74, 0)));
        }

        Text HudLabel(string s, int size, Vector2 pos, Vector2 sz)
        {
            var go = new GameObject("T", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_hud, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = sz; rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>(); t.font = _font; t.text = s; t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        Image Pip(Vector2 pos)
        {
            var go = new GameObject("Pip", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_hud, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(50, 50); rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>(); img.raycastTarget = false; return img;
        }

        int CountTeam(int t) { int n = 0; foreach (var u in _pb.Units) if (u.team == t) n++; return n; }

        void UpdatePips()
        {
            SetPipRow(_pips0, 0, new Color(0.32f, 0.6f, 1f));
            SetPipRow(_pips1, 1, new Color(1f, 0.42f, 0.36f));
        }

        void SetPipRow(List<Image> pips, int team, Color live)
        {
            int alive = _pb.AliveCount(team);
            var dead = new Color(0.2f, 0.2f, 0.24f, 0.85f);
            for (int i = 0; i < pips.Count; i++) if (pips[i] != null) pips[i].color = i < alive ? live : dead;
        }

        // Fighting-game intro: the two active fighters rush in from their edges.
        void IntroApproach()
        {
            for (int team = 0; team < 2; team++)
            {
                var v = ActiveView(team); if (v == null) continue;
                var anchor = ActiveAnchor(team);
                float far = team == 0 ? -900f : 900f;
                v.EnterFrom(new Vector2(far, anchor.y), anchor);
            }
        }

        UnitView ActiveView(int team)
        {
            foreach (var u in _pb.Units)
            {
                if (u.team != team) continue;
                var v = View(u.team, u.slot);
                if (v != null && !v.IsDead) return v;
            }
            return null;
        }

        // ---- Fighting-game staging: front-most alive = active (centered, big),
        //      the rest wait behind (small, dim). On a death the next runs in. ----
        static Vector2 ActiveAnchor(int team) => new Vector2(team == 0 ? -250f : 250f, -40f);
        static Vector2 ReserveAnchor(int team, int rank)
        {
            float side = team == 0 ? -1f : 1f;
            return new Vector2(side * (470f + (rank - 1) * 80f), 250f + (rank - 1) * 70f);
        }

        void RelayoutTeam(int team, bool animate)
        {
            int rank = 0;
            foreach (var u in _pb.Units)
            {
                if (u.team != team) continue;
                var v = View(u.team, u.slot); if (v == null || v.IsDead) continue;   // dead stay where they fell
                Vector2 target = rank == 0 ? ActiveAnchor(team) : ReserveAnchor(team, rank);
                v.SetReserve(rank > 0);
                if (animate && (v.BasePos - target).sqrMagnitude > 1600f) v.EnterFrom(v.BasePos, target);
                else v.SetBasePos(target);
                rank++;
            }
        }

        void Update()
        {
            if (_playing)
            {
                if (_slowmoT > 0f) { _slowmoT -= Time.deltaTime; if (_slowmoT <= 0f) _slowmo = 1f; }

                if (_hitstop > 0f)
                {
                    _hitstop -= Time.deltaTime;                       // combo / impact freeze
                }
                else
                {
                    _clock += Time.deltaTime * _simPerReal * speedMultiplier * _slowmo;
                    while (_replay != null && _rIdx < _replay.Count && _replay[_rIdx].t <= _clock)
                    {
                        Apply(_replay[_rIdx], _cho.beats[_rIdx]);
                        _rIdx++;
                    }
                    _pb.ProcessUpTo(_clock);
                }

                foreach (var u in _pb.Units)
                    if (_views.TryGetValue(Key(u.team, u.slot), out var v)) v.SetHp(u.currentHp);
                UpdatePips();

                bool eventsDone = _replay == null || _rIdx >= _replay.Count;
                if (!_finishedFired && eventsDone && _clock >= _pb.Duration && _hitstop <= 0f)
                {
                    _finishedFired = true; _playing = false; _zoomTarget = 1.12f;
                    foreach (var wu in _pb.Units)
                        if (wu.team == _pb.WinnerTeam && wu.Alive && _views.TryGetValue(Key(wu.team, wu.slot), out var wv))
                            wv.PlayVictory();
                    AudioManager.Play(Sfx.Victory);
                    OnFinished?.Invoke(_pb.WinnerTeam);
                }
            }
            UpdateCamera();
        }

        UnitView View(int t, int s) => _views.TryGetValue(Key(t, s), out var v) ? v : null;
        AttackStyle StyleOf(int team, int slot) =>
            _styleByKey.TryGetValue(Key(team, slot), out var s) ? s : AttackStyle.MeleeLunge;
        Vector2 PosOf(int team, int slot) => View(team, slot) is UnitView v ? v.BasePos : Vector2.zero;

        void Apply(ReplayEvent e, ChoreoBeat b)
        {
            switch (e.kind)
            {
                case ReplayEventKind.Attack:
                case ReplayEventKind.Skill:
                case ReplayEventKind.Ultimate:
                {
                    bool ult = e.kind == ReplayEventKind.Ultimate;
                    var st = StyleOf(e.actorTeam, e.actorSlot);
                    string actorSp = _speciesByKey.TryGetValue(Key(e.actorTeam, e.actorSlot), out var asp) ? asp : "";
                    var av = View(e.actorTeam, e.actorSlot);

                    // Skill / ultimate banners.
                    if (av != null && e.kind == ReplayEventKind.Skill)
                        _texts.Spawn(av.BasePos + new Vector2(0, 100), SpeciesIdentity.SkillWord(actorSp), COrange, 30);
                    if (av != null && ult)
                    {
                        _texts.Spawn(av.BasePos + new Vector2(0, 110), "ULTIMATE", COrange, 44);
                        _texts.Spawn(av.BasePos + new Vector2(0, 62), SpeciesIdentity.SkillWord(actorSp), COrange, 26);
                        _fx.Burst(av.BasePos, BurstKind.Ultimate);
                    }
                    if (e.kind == ReplayEventKind.Skill) AudioManager.Play(Sfx.Skill);
                    else if (ult) AudioManager.Play(Sfx.Ultimate);

                    if (e.isBuff)
                    {
                        av?.PlayAttack(new Vector2(e.actorTeam == 0 ? 1f : -1f, 0f), DashDist(st, ult) * 0.5f, ult);
                        ApplyCam(b.cam);
                        break;
                    }

                    _zoomTarget = ult ? 1.16f : 1.05f;   // wind-up; the impact punch lands on the connecting hit
                    int tt = e.targetTeam, ts = e.targetSlot;
                    if (AttackStyles.IsRanged(st))
                    {
                        Vector2 from = PosOf(e.actorTeam, e.actorSlot), to = PosOf(tt, ts);
                        av?.PlayAttack(new Vector2(e.actorTeam == 0 ? 1f : -1f, 0f), DashDist(st, ult) * 0.5f, false);
                        _fx.Projectile(from, to, ProjColor(st),
                            () => StartCoroutine(Combo(e.actorTeam, e.actorSlot, tt, ts, b, st, actorSp, ult)));
                    }
                    else
                    {
                        StartCoroutine(Combo(e.actorTeam, e.actorSlot, tt, ts, b, st, actorSp, ult));
                    }
                    break;
                }
                case ReplayEventKind.Heal:
                    if (View(e.targetTeam, e.targetSlot) is UnitView hv)
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
                    var dv = View(e.targetTeam, e.targetSlot);
                    if (dv != null) { dv.Knock(knock, b.knockback); if (b.launch) dv.Launch(140f); dv.PlayDeath(knock); }
                    if (b.endsBattle)
                    {
                        _texts.Spawn(ActiveAnchor(1 - e.targetTeam) + new Vector2(0, 150), FinisherWord(b.finisher), CCrit, 40);
                        ApplyCam(ChoreoCam.SlowMoFinisher);
                    }
                    else ApplyCam(ChoreoCam.ShakeCrit);
                    HitStop(b.hitStop);
                    AudioManager.Play(Sfx.Death);
                    RelayoutTeam(e.targetTeam, true);   // next challenger runs in
                    break;
                }
                case ReplayEventKind.Victory:
                    ApplyCam(ChoreoCam.ZoomWinner);
                    break;
            }
        }

        // Procedural combo chain: staggered hits with hit-stop, connecting hit
        // carries the (single, sim-accurate) damage number, knockback + camera.
        IEnumerator Combo(int at, int as_, int tt, int ts, ChoreoBeat b, AttackStyle st, string actorSp, bool ult)
        {
            var actor = View(at, as_); var target = View(tt, ts);
            Vector2 dir = new Vector2(at == 0 ? 1f : -1f, 0f);

            if (b.dodge && target != null)
            {
                float sp0 = Mathf.Clamp(speedMultiplier, 0.5f, 4f);
                target.Dodge(new Vector2(-dir.x, 0.3f));
                _texts.Spawn(target.BasePos + new Vector2(0, 72), "DODGE", CDodge, 26);
                AudioManager.Play(Sfx.Click);
                HitStop(0.16f / sp0 + 0.02f);   // keep the sim frozen through the dodge beat
                yield return new WaitForSecondsRealtime(0.16f / sp0);
            }

            int n = Mathf.Clamp(b.hits, 1, 15);
            int denom = Mathf.Max(1, n - 1);
            float baseStep = ult ? 0.055f : b.crit ? 0.05f : 0.045f;
            for (int i = 0; i < n; i++)
            {
                // Respect the 0.5×–4× playback buttons; combo accelerates into the finish.
                float sp = Mathf.Clamp(speedMultiplier, 0.5f, 4f);
                float ramp = Mathf.Lerp(1.15f, 0.68f, i / (float)denom);
                float step = baseStep * ramp / sp;
                bool last = i == n - 1;
                Vector2 tpos = target != null ? target.BasePos : PosOf(tt, ts);
                actor?.PlayAttack(dir, DashDist(st, ult) * (0.5f + 0.5f * (i / (float)n)), ult && last);
                target?.PlayHit(b.crit && last);
                _fx.Burst(tpos + ComboJit(i), last ? (ult ? BurstKind.Ultimate : b.crit ? BurstKind.Crit : MeleeBurst(st)) : BurstKind.Slash);
                AudioManager.Play((b.crit || ult) && last ? Sfx.Crit : Sfx.Hit);
                if (!last)
                {
                    Shake(ult ? 5f : 2.5f);
                    HitStop(step + 0.02f);                                    // hold the sim clock across the whole combo
                }
                else
                {
                    _texts.Spawn(tpos + Jitter(), b.amount.ToString(), (b.crit || ult) ? CCrit : CWhite, (b.crit || ult) ? 44 : 30);
                    if (b.crit) _texts.Spawn(tpos + new Vector2(0, -70), SpeciesIdentity.CritWord(actorSp), CCrit, 34);
                    if (target != null) { target.Knock(dir, b.knockback); if (b.launch) target.Launch(130f); }
                    float impact = ult ? 0.10f : b.crit ? 0.085f : 0.055f;    // heavier freeze on the connecting hit
                    HitStop(impact / sp);
                    Shake(ult ? 22f : b.crit ? 15f : 8f);
                    ZoomPunch(ult ? 0.12f : b.crit ? 0.07f : 0.03f);
                }
                yield return new WaitForSecondsRealtime(step);
            }
        }

        static string FinisherWord(FinisherKind f)
        {
            switch (f)
            {
                case FinisherKind.TotalDomination: return "TOTAL DOMINATION";
                case FinisherKind.CloseComeback: return "COMEBACK!";
                case FinisherKind.ClutchSlowMo: return "CLUTCH FINISH";
                default: return "K.O.";
            }
        }

        void ApplyCam(ChoreoCam c)
        {
            switch (c)
            {
                case ChoreoCam.ZoomCombo: _zoomTarget = 1.06f; break;
                case ChoreoCam.ShakeCrit: _zoomTarget = 1.09f; Shake(12f); ZoomPunch(0.05f); break;
                case ChoreoCam.CinematicZoom: _zoomTarget = 1.15f; ZoomPunch(0.1f); Shake(16f); break;
                case ChoreoCam.SlowMoFinisher: _zoomTarget = 1.24f; Shake(22f); StartSlowMo(); break;
                case ChoreoCam.ZoomWinner: _zoomTarget = 1.12f; break;
            }
        }

        void StartSlowMo() { _slowmo = 0.22f; _slowmoT = 1.4f; }

        static float DashDist(AttackStyle s, bool ult)
        {
            switch (s)
            {
                case AttackStyle.HeavySmash: return ult ? 170f : 120f;
                case AttackStyle.AssassinDash: return ult ? 210f : 155f;
                case AttackStyle.MeleeLunge: return ult ? 120f : 80f;
                default: return ult ? 70f : 40f;
            }
        }

        static BurstKind MeleeBurst(AttackStyle s) => s == AttackStyle.HeavySmash ? BurstKind.Impact : BurstKind.Slash;
        static Color ProjColor(AttackStyle s) => s == AttackStyle.MageCast ? new Color(0.6f, 0.5f, 1f) : new Color(1f, 0.9f, 0.4f);
        static Vector2 Jitter() => new Vector2(UnityEngine.Random.Range(-24f, 24f), 40f);
        static Vector2 ComboJit(int i) => new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-30f, 40f));

        void HitStop(float d) => _hitstop = Mathf.Max(_hitstop, d);
        void Shake(float mag) { float dur = 0.18f + mag * 0.006f; if (dur > _shakeT) { _shakeT = dur; _shakeDur = dur; } _shakeMag = Mathf.Max(_shakeMag, mag); }
        void ZoomPunch(float amt) { _zoom = Mathf.Min(_zoom + amt, 1.35f); }

        void UpdateCamera()
        {
            if (_stage == null) return;
            Vector2 camOffset = _stage.anchoredPosition;
            if (_shakeT > 0f)
            {
                _shakeT -= Time.deltaTime;
                float m = _shakeMag * Mathf.Clamp01(_shakeT / _shakeDur);
                _stage.anchoredPosition = new Vector2(UnityEngine.Random.Range(-m, m), UnityEngine.Random.Range(-m, m));
                if (_shakeT <= 0f) { _shakeMag = 0f; _stage.anchoredPosition = Vector2.zero; }
            }
            else _stage.anchoredPosition = Vector2.Lerp(_stage.anchoredPosition, Vector2.zero, 12f * Time.deltaTime);

            float restZoom = _finishedFired ? 1.1f : 1f;
            _zoomTarget = Mathf.Lerp(_zoomTarget, restZoom, 1.2f * Time.deltaTime);   // hold the punch a touch longer
            _zoom = Mathf.Clamp(Mathf.Lerp(_zoom, _zoomTarget, 5.5f * Time.deltaTime), 0.9f, 1.35f);
            _stage.localScale = Vector3.one * _zoom;

            _arena?.SetParallax(camOffset);
        }
    }
}
