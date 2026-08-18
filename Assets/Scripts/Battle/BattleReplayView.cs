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
        public Dictionary<string, Color> elementColors;   // species -> element indicator color (set before Play)
        public Dictionary<string, string> elementNames, roleNames;   // species -> element / role, for portraits
        public Dictionary<string, string> displayNames;   // species -> Title Case name shown over the fighter
        List<ReplayEvent> _replay; Choreography _cho; int _rIdx;
        RectTransform _root, _stage, _hud; Font _font; FloatingTextPool _texts; BattleFx _fx; BattleArena _arena; VfxPool _vfx;
        readonly List<Image> _pips0 = new List<Image>(), _pips1 = new List<Image>();
        Dictionary<string, AttackStyle> _styleMap;
        double _clock, _simPerReal; bool _playing, _finishedFired;
        float _shakeT, _shakeDur = 0.25f, _shakeMag, _zoom = 1f, _zoomTarget = 1f, _hitstop;
        float _slowmo = 1f, _slowmoT;
        Image _screenFlash; float _flashT;

        static int Key(int t, int s) => t * 100 + s;
        public void SetSpeed(float m) => speedMultiplier = m;

        // Fallback humanizer: "mushroom_beast" -> "Mushroom Beast" (used only if no
        // displayName dict was supplied). Never shows raw snake_case ids.
        static string Humanize(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            var parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            return string.Join(" ", parts);
        }

        public void Play(BattleResult result, List<ReplayEvent> replay,
            Dictionary<string, AttackStyle> styleMap, RectTransform parent, Font font)
        {
            _font = font; _root = parent; _replay = replay; _styleMap = styleMap; _rIdx = 0;
            _clock = 0; _playing = true; _finishedFired = false;
            _zoom = 1f; _zoomTarget = 1f; _shakeT = _shakeMag = _hitstop = 0f; _slowmo = 1f; _slowmoT = 0f;
            _pb.Init(result);
            _cho = BattleCinematicDirector.Choreograph(result, replay);   // deterministic (seeded by logHash)
            AudioManager.PlayMusic(Music.Battle);

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

            // Arena themed by the enemy front-liner's element.
            string arenaElem = "";
            foreach (var u in _pb.Units) { if (u.team == 1) { if (elementNames != null) elementNames.TryGetValue(u.speciesId, out arenaElem); break; } }
            _arena = new BattleArena(); _arena.Build(_stage, arenaElem);   // procedural element arena
            _texts = new FloatingTextPool(_stage, _font);
            _fx = new BattleFx(_stage);
            _vfx = new VfxPool(_stage, 12);   // real CC0 impact VFX

            // Full-screen crit/ultimate flash overlay (over the fighters).
            var flashGo = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
            var frt = flashGo.GetComponent<RectTransform>(); frt.SetParent(_root, false);
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.sizeDelta = new Vector2(1400, 2000); frt.anchoredPosition = Vector2.zero;
            _screenFlash = flashGo.GetComponent<Image>(); _screenFlash.color = new Color(1, 1, 1, 0); _screenFlash.raycastTarget = false;
            _flashT = 0f;
            _views.Clear(); _styleByKey.Clear(); _speciesByKey.Clear();

            var size = new Vector2(240, 160);
            foreach (var u in _pb.Units)
            {
                var teamColor = u.team == 0 ? new Color(0.2f, 0.4f, 0.75f) : new Color(0.75f, 0.28f, 0.26f);
                var sc = SpeciesIdentity.ColorFor(u.speciesId);
                var speciesColor = new Color(sc.r, sc.g, sc.b);
                var v = new GameObject("UnitView").AddComponent<UnitView>();
                v.transform.SetParent(_stage, false);
                string elem = elementNames != null && elementNames.TryGetValue(u.speciesId, out var en) ? en : "";
                string role = roleNames != null && roleNames.TryGetValue(u.speciesId, out var rn) ? rn : "Bruiser";
                string dn = displayNames != null && displayNames.TryGetValue(u.speciesId, out var dnv) ? dnv : Humanize(u.speciesId);
                v.Build(_stage, Vector2.zero, size, teamColor, speciesColor,
                    u.speciesId, dn, _font, elem, role, u.team == 0);
                v.SetMaxHp(u.maxHp); v.SetHp(u.currentHp); v.PlaySpawn();
                if (elementColors != null && elementColors.TryGetValue(u.speciesId, out var ec)) v.SetElement(ec);
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
        static Vector2 ActiveAnchor(int team) => new Vector2(team == 0 ? -250f : 250f, -30f);
        // Reserves wait fully OFF-SCREEN (1v1 framing); the next runs in on a death.
        static Vector2 ReserveAnchor(int team, int rank)
        {
            float side = team == 0 ? -1f : 1f;
            return new Vector2(side * (820f + (rank - 1) * 60f), -30f);
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

                // Dynamic battle audio: intensify as it gets close / down to the wire.
                int aliveA = _pb.AliveCount(0), aliveB = _pb.AliveCount(1);
                float closeness = 1f - Mathf.Abs(aliveA - aliveB) * 0.34f;
                float climax = (aliveA + aliveB) <= 1 ? 1f : ((aliveA + aliveB) <= 2 ? 0.6f : 0f);
                AudioManager.SetBattleIntensity(Mathf.Clamp01(Mathf.Max(closeness * 0.5f, climax)));

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
                    // Elemental cast VFX on skills/ultimates.
                    if (av != null && (e.kind == ReplayEventKind.Skill || ult))
                    {
                        string ael = elementNames != null && elementNames.TryGetValue(actorSp, out var ae) ? ae : "";
                        string efx = ael == "Fire" ? "fire" : ael == "Water" ? "electric" : "";
                        if (efx != "") _vfx.Play(efx, av.BasePos + new Vector2(0, 10), ult ? 260f : 190f, Color.white);
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
                    if (dv != null) { dv.Knock(knock, b.knockback); if (b.launch) dv.Launch(140f); dv.PlayDeath(knock); _vfx.Play("explosion", dv.BasePos, b.endsBattle ? 340f : 240f, Color.white); }
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

        // Fighting-game choreography: dash-in → ground combo → launcher → air combo
        // → slam → recovery, plus dodge+counter. Transform-driven movement on the
        // sprites; the connecting hit carries the single sim-accurate damage number.
        // Presentation only — never re-simulates.
        IEnumerator Combo(int at, int as_, int tt, int ts, ChoreoBeat b, AttackStyle st, string actorSp, bool ult)
        {
            var A = View(at, as_); var T = View(tt, ts);
            Vector2 dir = new Vector2(at == 0 ? 1f : -1f, 0f);
            float sp = Mathf.Clamp(speedMultiplier, 0.5f, 4f);
            int n = Mathf.Clamp(b.hits, 1, 15);
            bool big = b.crit || ult;

            // ---- Dodge (sidestep + afterimage + MISS) then COUNTER ----
            if (b.dodge && T != null)
            {
                Afterimage(T);
                T.Dodge(new Vector2(-dir.x, 0.3f));
                _texts.Spawn(T.BasePos + new Vector2(0, 80), "MISS", CDodge, 34);
                _vfx.Play("puff", T.BasePos, 150f, new Color(1f, 1f, 1f, 0.9f));
                AudioManager.Play(Sfx.Hover);
                HitStop(0.16f / sp);
                yield return new WaitForSecondsRealtime(0.13f / sp);
                if (A != null)   // counter flick
                {
                    T.PlayAttack(new Vector2(-dir.x, 0f), 70f, false);
                    _texts.Spawn(A.BasePos + new Vector2(0, 74), "COUNTER", new Color(1f, 0.9f, 0.4f), 28);
                    _vfx.Play("hit_small", A.BasePos, 120f, Color.white);
                    A.PlayHit(false); A.Knock(dir, 40f); Shake(6f); AudioManager.Play(Sfx.Hit);
                    HitStop(0.14f / sp);
                    yield return new WaitForSecondsRealtime(0.12f / sp);
                }
            }

            // Ranged: no dash/air — quick hits at range (projectile already flew).
            if (AttackStyles.IsRanged(st) || A == null || T == null)
            {
                for (int i = 0; i < n; i++)
                {
                    bool last = i == n - 1;
                    Vector2 tp = T != null ? T.BasePos : PosOf(tt, ts);
                    T?.PlayHit(big && last);
                    _vfx.Play(last ? (ult ? "explosion" : b.crit ? "hit_big" : "hit_impact") : "hit_small", tp, last ? (ult ? 300f : 180f) : 120f, Color.white);
                    AudioManager.Play(big && last ? Sfx.Crit : Sfx.Hit);
                    Shake(last ? (ult ? 18f : b.crit ? 12f : 7f) : 3f);
                    if (last)
                    {
                        _texts.Spawn(tp + Jitter(), b.amount.ToString(), big ? CCrit : CWhite, big ? 44 : 30);
                        if (T != null) { T.Knock(dir, b.knockback); if (b.launch) T.Launch(120f); }
                        if (big) StartCoroutine(Shockwave(tp, ult ? new Color(1f, 0.6f, 0.2f) : CCrit));
                    }
                    HitStop(0.055f / sp + 0.02f);
                    yield return new WaitForSecondsRealtime(0.05f / sp);
                }
                yield break;
            }

            // ---- Melee fight choreography ----
            float gap = Mathf.Abs(T.BasePos.x - A.BasePos.x);
            Vector2 close = new Vector2(dir.x * (gap - 150f), 0f);

            // 1) DASH IN
            _vfx.Play("speedlines", A.BasePos + dir * 40f, 210f, new Color(1f, 1f, 1f, 0.9f));
            yield return MoveOffset(A, Vector2.zero, close, 0.10f / sp);
            Shake(4f);

            // 2) GROUND COMBO
            int ground = big ? Mathf.Max(2, n / 3) : n;
            for (int i = 0; i < ground; i++)
            {
                A.PlayAttack(dir, 34f, false); T.PlayHit(false);
                T.combatOffset = new Vector2(dir.x * 10f, 0f);
                _vfx.Play("hit_small", T.BasePos + ComboJit(i), 130f, Color.white);
                _fx.Burst(T.BasePos + ComboJit(i), BurstKind.Slash);
                Shake(4f); AudioManager.Play(Sfx.Hit);
                HitStop(0.055f / sp + 0.02f);
                yield return new WaitForSecondsRealtime(0.05f / sp);
            }

            if (big)
            {
                // 3) LAUNCHER — target flies up, attacker jumps after
                AudioManager.Play(Sfx.Crit);
                _vfx.Play("hit_big", T.BasePos, 230f, Color.white); Shake(14f); FlashScreen(0.4f);
                _texts.Spawn(T.BasePos + new Vector2(0, 46), "LAUNCH!", CCrit, 30);
                StartCoroutine(MoveOffset(T, T.combatOffset, new Vector2(dir.x * 24f, 300f), 0.16f / sp));
                yield return MoveOffset(A, close, close + new Vector2(dir.x * 60f, 260f), 0.16f / sp);
                ApplyCam(ChoreoCam.ZoomCombo);

                // 4) AIR COMBO
                int air = Mathf.Max(2, n - ground - 1);
                for (int i = 0; i < air; i++)
                {
                    A.PlayAttack(dir, 26f, ult && i == air - 1); T.PlayHit(true);
                    T.combatOffset += new Vector2(dir.x * 6f, 14f);
                    A.combatOffset = new Vector2(A.combatOffset.x, T.combatOffset.y - 20f);
                    _vfx.Play("hit_impact", T.BasePos, 150f, Color.white);
                    Shake(6f); AudioManager.Play(Sfx.Hit);
                    HitStop(0.05f / sp + 0.02f);
                    yield return new WaitForSecondsRealtime(0.045f / sp);
                }

                // 5) SLAM DOWN
                AudioManager.Play(ult ? Sfx.Ultimate : Sfx.Crit);
                _texts.Spawn(T.BasePos + new Vector2(0, 34), "SLAM!", COrange, 32);
                yield return MoveOffset(T, T.combatOffset, new Vector2(dir.x * 40f, -30f), 0.11f / sp);
                _vfx.Play(ult ? "explosion" : "hit_big", T.BasePos, ult ? 330f : 250f, Color.white);
                _texts.Spawn(T.BasePos + Jitter(), b.amount.ToString(), CCrit, 46);
                if (b.crit) _texts.Spawn(T.BasePos + new Vector2(0, -70), SpeciesIdentity.CritWord(actorSp), CCrit, 34);
                T.Knock(dir, b.knockback);
                Shake(ult ? 24f : 18f); FlashScreen(ult ? 0.7f : 0.5f); ZoomPunch(ult ? 0.12f : 0.08f);
                StartCoroutine(Shockwave(T.BasePos, ult ? new Color(1f, 0.6f, 0.2f) : CCrit));
                HitStop(0.10f / sp);
                yield return new WaitForSecondsRealtime(0.1f / sp);
            }
            else
            {
                // Light finish: last strike + knockback
                A.PlayAttack(dir, 42f, false); T.PlayHit(true);
                _vfx.Play("hit_impact", T.BasePos, 180f, Color.white);
                _texts.Spawn(T.BasePos + Jitter(), b.amount.ToString(), CWhite, 32);
                T.Knock(dir, b.knockback);
                Shake(8f); ZoomPunch(0.03f); AudioManager.Play(Sfx.Hit);
                HitStop(0.06f / sp);
                yield return new WaitForSecondsRealtime(0.06f / sp);
            }

            // 6) RECOVERY — both return to stance
            StartCoroutine(MoveOffset(T, T.combatOffset, Vector2.zero, 0.2f / sp));
            yield return MoveOffset(A, A.combatOffset, Vector2.zero, 0.14f / sp);
        }

        // Lerp a fighter's combat offset (ease-out), holding the sim clock frozen.
        IEnumerator MoveOffset(UnitView u, Vector2 from, Vector2 to, float dur)
        {
            if (u == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, dur);
                float e = 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));
                u.combatOffset = Vector2.Lerp(from, to, e);
                HitStop(0.05f);
                yield return null;
            }
            u.combatOffset = to;
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
                case ChoreoCam.ShakeCrit: _zoomTarget = 1.09f; Shake(12f); ZoomPunch(0.05f); FlashScreen(0.5f); break;
                case ChoreoCam.CinematicZoom: _zoomTarget = 1.15f; ZoomPunch(0.1f); Shake(16f); FlashScreen(0.8f); break;
                case ChoreoCam.SlowMoFinisher: _zoomTarget = 1.24f; Shake(22f); StartSlowMo(); FlashScreen(0.6f); break;
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

        void FlashScreen(float amt) => _flashT = Mathf.Max(_flashT, amt);

        // Sidestep afterimage: a fading ghost silhouette left where the dodger stood.
        void Afterimage(UnitView u)
        {
            if (u != null) StartCoroutine(AfterimageRoutine(u.BasePos));
        }
        IEnumerator AfterimageRoutine(Vector2 pos)
        {
            var go = new GameObject("After", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_stage, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(150, 185);
            var img = go.GetComponent<Image>(); img.sprite = ProceduralArt.Disc(); img.raycastTarget = false;
            float t = 0f;
            while (t < 1f) { t += Time.deltaTime / 0.28f; img.color = new Color(0.55f, 0.82f, 1f, (1f - t) * 0.5f); yield return null; }
            Destroy(go);
        }

        IEnumerator Shockwave(Vector2 pos, Color col)
        {
            var go = new GameObject("Shock", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_stage, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(60, 60);
            var img = go.GetComponent<Image>(); img.sprite = ProceduralArt.Ring(); img.color = col; img.raycastTarget = false;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.35f; float e = Mathf.Clamp01(t);
                rt.sizeDelta = Vector2.one * Mathf.Lerp(60f, 440f, e);
                img.color = new Color(col.r, col.g, col.b, (1f - e) * 0.7f);
                yield return null;
            }
            Destroy(go);
        }

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
            _arena?.Tick(Time.deltaTime);

            if (_screenFlash != null)
            {
                _flashT = Mathf.Max(0f, _flashT - Time.deltaTime * 3.5f);
                _screenFlash.color = new Color(1f, 1f, 1f, _flashT * 0.55f);
            }
        }
    }
}
