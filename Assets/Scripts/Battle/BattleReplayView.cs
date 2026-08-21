using System;
using System.Collections;
using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Presentation mode (Phase AA). Brawl = all units fight concurrently in spread lanes;
    // Arena = one active duel spotlighted centre with the reserves benched at the flanks.
    // Chooses staging + camera only — the simulator/determinism are identical for both.
    public enum BattleMode { Brawl, Arena }

    // Cinematic team-brawl replay (Phase O-1). Every living unit of BOTH teams stands
    // in a formation echelon and fights from second one — no off-screen reserves, no
    // run-in duels. The sim (BattleSimulator) already runs all six units on one
    // interleaved timeline; this view only stages/choreographs it. Each event is driven
    // by its actor/target resolved through the (team,slot) UnitView map — never spawn
    // order — which kills the "statue" bug. Readability is preserved by a SPOTLIGHT rule:
    // at most one full cinematic melee suite (dash→ground→launcher→air→slam) plays at a
    // time; every other concurrent attack uses a compact hit. NEVER re-simulates — winner
    // and logHash are the simulator's. Presentation only.
    public class BattleReplayView : MonoBehaviour
    {
        public float speedMultiplier = 1f;
        public event Action<int> OnFinished;

        const float FREEZE_CAP = 3.5f;   // total global sim-clock freeze budget per battle (task 8)

        static readonly Color CWhite = Color.white;
        static readonly Color CHeal = new Color(0.4f, 1f, 0.55f);
        static readonly Color CCrit = new Color(1f, 0.9f, 0.2f);
        static readonly Color COrange = new Color(1f, 0.6f, 0.15f);
        static readonly Color CDodge = new Color(0.7f, 0.9f, 1f);

        readonly BattlePlayback _pb = new BattlePlayback();
        readonly Dictionary<int, UnitView> _views = new Dictionary<int, UnitView>();   // (team,slot) -> view
        readonly Dictionary<int, AttackStyle> _styleByKey = new Dictionary<int, AttackStyle>();
        readonly Dictionary<int, string> _speciesByKey = new Dictionary<int, string>();
        readonly HashSet<int> _busyUnits = new HashSet<int>();   // units mid dash-choreography
        bool _spotlightBusy;                                     // single-cinematic-at-a-time invariant
        int _spotlightTargetKey = -1;
        float _freezeBudget;

        // --- Tawuran engagement (Phase O-2) ---
        EngagementPlan _plan;
        int _fillerQ;                    // next filler beat index
        float _stageTime;                // real seconds since the fight started (opening charge)
        bool _chargeClashed;
        double _lastClashT = -1;
        double _lastAdvT = -1;   // throttle for the SUPER EFFECTIVE / RESISTED readout
        bool _firstBlood, _finalDuel;   // story-beat callouts (P3)
        const float CHARGE_DUR = 1.15f;      // opening sprint-to-centre duration
        const float CHARGE_CONTACT = 0.62f;  // when the two charges collide

        // --- Phase O fight feel: afterimage pool (×6 units) + impact frames ---
        const int GHOST_POOL = 16;
        Image[] _ghost; float[] _ghostA; int _ghostCur;
        Image _impactOverlay, _impactSil;

        // --- Phase P fight HUD ---
        struct HudRng { public ulong s; public ulong N() { s ^= s >> 12; s ^= s << 25; s ^= s >> 27; return s * 0x2545F4914F6CDD1DUL; } public float Range(float a, float b) => a + (b - a) * ((N() >> 40) / 16777216f); }
        HudRng _hudRng;
        const double COMBO_GAP = 1.2;         // sim-seconds gap that breaks the combo
        Text _comboLabel; int _combo; double _comboLastT = -999; float _comboScale = 1f, _comboAlpha;
        Text _splash; float _splashT, _splashDur, _splashShake; Color _splashColor;
        RectTransform _letterTop, _letterBot; float _letterCur, _letterTarget;

        // --- Phase Q ceremony ---
        public Dictionary<string, int> rarities;   // species -> rarity (set before Play)
        public bool bossMusic;                       // set before Play for league-boss stages
        public BattleMode mode = BattleMode.Brawl;   // Brawl = concurrent team fight; Arena = duel spotlight + benched reserves (boss/finale). Presentation only.
        RectTransform _vs, _vsLeft, _vsRight; Text _vsMid; Image _vsFlash; CanvasGroup _vsGroup; float _vsT; const float VS_DUR = 2.6f; bool _vsFlashed;
        RectTransform _superRoot; Image _superDim, _superSil, _superTint, _superCutin; Text _superBanner;
        Image _finDark; float _finDarkCur, _finDarkTarget;
        RectTransform _victoryRoot; Text _victoryText; Image[] _victoryStars; string _winBanner = "VICTORY!"; int _winStars = 1;   // victory hero screen
        public Dictionary<string, Color> elementColors;   // species -> element indicator color (set before Play)
        public Dictionary<int, int> levelByKey;            // Key(team,slot) -> monster level (set before Play; surfaces level on the fighter)
        public Dictionary<string, string> elementNames, roleNames;   // species -> element / role, for portraits
        public Dictionary<string, string> displayNames;   // species -> Title Case name shown over the fighter
        List<ReplayEvent> _replay; Choreography _cho; int _rIdx;
        RectTransform _root, _stage, _hud; Font _font; FloatingTextPool _texts; BattleFx _fx; BattleArena _arena; VfxPool _vfx; ElementVfx _elem;
        readonly List<Image> _pips0 = new List<Image>(), _pips1 = new List<Image>();       // pip frames
        readonly List<Image> _port0 = new List<Image>(), _port1 = new List<Image>();       // pip portraits
        Dictionary<string, AttackStyle> _styleMap;
        double _clock, _simPerReal; bool _playing, _finishedFired;
        float _shakeT, _shakeDur = 0.25f, _shakeMag, _zoom = 1f, _zoomTarget = 1f, _hitstop;
        float _baseZoom = 1f;   // dynamic rest zoom: framed by roster size so 1v1 is hero-sized and 3v3 isn't miniature (V1)
        float _lastShockwaveT = -1f;   // min-interval gate so concentric rings don't stack into a screen-filling mess (V4)
        Vector2 _camPush;   // directional camera language on big hits
        Vector2 _camFocus, _camFocusTarget;   // cinematic pan: drift toward the action on big/ult hits (P2)
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

        readonly int[] _teamCount = new int[2];   // live roster size per team (drives the formation)

        // Team-brawl formation: layout adapts to team SIZE so 1v1 / 2v2 / 3v3 each read
        // cleanly — no crowding, no stacking, no both-teams-in-one-corner. Player team on
        // the left, enemy on the right; front slot (pick order 0) closest to centre. Wider
        // X spread + deeper Y stagger than before so the arena is used, not just its middle.
        Vector2 Formation(int team, int slot)
        {
            float side = team == 0 ? -1f : 1f;
            int size = Mathf.Clamp(_teamCount[team], 1, 3);
            switch (size)
            {
                case 1:                                                    // 1v1 — cinematic duel spacing
                    return new Vector2(side * 250f, -40f);
                case 2:                                                    // 2v2 — a clean front/back pair
                    return slot == 0 ? new Vector2(side * 245f, -75f)
                                     : new Vector2(side * 445f, 120f);
                default:                                                   // 3v3 — spread triangle (front / back-high / mid-low)
                    switch (slot)
                    {
                        case 0:  return new Vector2(side * 235f, -70f);
                        case 1:  return new Vector2(side * 455f, 140f);
                        case 2:  return new Vector2(side * 400f, -235f);
                        default: return new Vector2(side * (515f + (slot - 3) * 72f), -60f);
                    }
            }
        }

        public void Play(BattleResult result, List<ReplayEvent> replay,
            Dictionary<string, AttackStyle> styleMap, RectTransform parent, Font font)
        {
            _font = font; _root = parent; _replay = replay; _styleMap = styleMap; _rIdx = 0;
            _clock = 0; _playing = true; _finishedFired = false;
            _zoom = 1f; _zoomTarget = 1f; _shakeT = _shakeMag = _hitstop = 0f; _slowmo = 1f; _slowmoT = 0f; _camPush = Vector2.zero; _camFocus = _camFocusTarget = Vector2.zero;
            _spotlightBusy = false; _spotlightTargetKey = -1; _busyUnits.Clear(); _freezeBudget = FREEZE_CAP;
            _stageTime = 0f; _chargeClashed = false; _fillerQ = 0; _lastClashT = -1;
            _combo = 0; _comboLastT = -999; _comboAlpha = 0f; _comboScale = 1f; _splashT = 0f; _letterCur = _letterTarget = 0f;
            _finDarkCur = _finDarkTarget = 0f; _vsFlashed = false;
            _firstBlood = _finalDuel = false;   // story beats (P3)
            _hudRng.s = result.logHash == 0UL ? 0x9E3779B97F4A7C15UL : result.logHash;   // seeded number scatter / pitch
            _pb.Init(result);
            _cho = BattleCinematicDirector.Choreograph(result, replay);   // deterministic (seeded by logHash)
            _plan = EngagementPlanner.Plan(result, replay);               // tawuran engagement plan (seeded by logHash)
            AudioManager.PlayMusic(bossMusic ? Music.Boss : Music.Battle);

            // Auto-pace: 15–60 s window; close matches longer, stomps faster.
            double sim = Math.Max(1.0, result.duration);
            double target = Math.Min(60.0, Math.Max(15.0, sim * 1.5));
            var d = BattleDrama.Compute(result);
            _winBanner = string.IsNullOrEmpty(d.bannerTitle) ? "VICTORY!" : d.bannerTitle;
            _winStars = d.tier == WinTier.DominantWin ? 3 : d.tier == WinTier.AdvantageWin ? 2 : 1;
            bool close = d.winnerAlive <= 1 || d.leadChanges >= 2;
            bool stomp = d.winnerAlive >= 3 && d.leadChanges == 0;
            if (close) target = Math.Min(60.0, target * 1.25);
            else if (stomp) target = Math.Max(12.0, target * 0.7);
            _simPerReal = sim / target;

            BuildStage();
            _vsT = VS_DUR;   // hold on the VS screen before the fight begins
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
            _vfx = new VfxPool(_stage, 24);   // ×6 units — pre-warmed real CC0 impact VFX
            _elem = new ElementVfx(_stage);   // procedural element-signature VFX (fire/water/nature/lightning/heal)

            // Afterimage ghost pool — pre-warmed, sized for six concurrent units (behind fighters).
            _ghost = new Image[GHOST_POOL]; _ghostA = new float[GHOST_POOL]; _ghostCur = 0;
            for (int i = 0; i < GHOST_POOL; i++)
            {
                var gg = new GameObject("Ghost", typeof(RectTransform), typeof(Image));
                var grt = gg.GetComponent<RectTransform>(); grt.SetParent(_stage, false);
                grt.anchorMin = grt.anchorMax = new Vector2(0.5f, 0.5f); grt.sizeDelta = new Vector2(256, 256);
                var gi = gg.GetComponent<Image>(); gi.raycastTarget = false; gi.preserveAspect = true; gi.color = new Color(1, 1, 1, 0);
                gg.SetActive(false); _ghost[i] = gi; _ghostA[i] = 0f;
            }

            // Screen vignette (V4): clear centre → soft dark edges for photographic depth. Sits above
            // the battlefield but below the flash/impact overlays + HUD, so it never dims the drama or
            // the readouts — it only frames the eye toward the fighters.
            var vgGo = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            var vgrt = vgGo.GetComponent<RectTransform>(); vgrt.SetParent(_root, false);
            vgrt.anchorMin = Vector2.zero; vgrt.anchorMax = Vector2.one; vgrt.offsetMin = vgrt.offsetMax = Vector2.zero;
            var vgImg = vgGo.GetComponent<Image>(); vgImg.sprite = ProceduralArt.Vignette();
            // Element-graded vignette (P5): each world tints the frame edges — warm ember (Fire),
            // deep cool (Water), verdant (Nature) — so the biomes feel like different places at a glance.
            Color vg = arenaElem == "Fire" ? new Color(0.16f, 0.05f, 0.02f)
                     : arenaElem == "Water" ? new Color(0.02f, 0.06f, 0.15f)
                     : arenaElem == "Nature" ? new Color(0.03f, 0.11f, 0.05f)
                     : new Color(0f, 0f, 0f);
            vgImg.color = new Color(vg.r, vg.g, vg.b, 0.52f); vgImg.raycastTarget = false;

            // Full-screen crit/ultimate flash overlay (over the fighters).
            var flashGo = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
            var frt = flashGo.GetComponent<RectTransform>(); frt.SetParent(_root, false);
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.sizeDelta = new Vector2(1400, 2000); frt.anchoredPosition = Vector2.zero;
            _screenFlash = flashGo.GetComponent<Image>(); _screenFlash.color = new Color(1, 1, 1, 0); _screenFlash.raycastTarget = false;
            _flashT = 0f;

            // Impact-frame overlays (ult/finisher only): near-black darken + a white victim
            // silhouette copy that renders ABOVE the darken so the victim pops.
            var ovGo = new GameObject("ImpactDark", typeof(RectTransform), typeof(Image));
            var ovrt = ovGo.GetComponent<RectTransform>(); ovrt.SetParent(_root, false);
            ovrt.anchorMin = Vector2.zero; ovrt.anchorMax = Vector2.one; ovrt.sizeDelta = new Vector2(1400, 2000); ovrt.anchoredPosition = Vector2.zero;
            _impactOverlay = ovGo.GetComponent<Image>(); _impactOverlay.color = new Color(0.02f, 0.02f, 0.04f, 0f); _impactOverlay.raycastTarget = false;
            var silGo = new GameObject("ImpactSil", typeof(RectTransform), typeof(Image));
            var silrt = silGo.GetComponent<RectTransform>(); silrt.SetParent(_root, false);
            silrt.anchorMin = silrt.anchorMax = new Vector2(0.5f, 0.5f); silrt.sizeDelta = new Vector2(256, 256);
            _impactSil = silGo.GetComponent<Image>(); _impactSil.raycastTarget = false; _impactSil.preserveAspect = true; _impactSil.color = new Color(1, 1, 1, 0);
            silGo.SetActive(false);

            _views.Clear(); _styleByKey.Clear(); _speciesByKey.Clear();

            _teamCount[0] = _teamCount[1] = 0;
            foreach (var u in _pb.Units) if (u.team >= 0 && u.team < 2) _teamCount[u.team]++;

            // Dynamic framing: the bigger the fight, the wider the rest camera — so 1v1 fills
            // the frame like a hero shot and 3v3 stays readable without cramming. Presentation only.
            int roster = Mathf.Max(_teamCount[0], _teamCount[1]);
            _baseZoom = mode == BattleMode.Arena
                ? (roster <= 1 ? 1.52f : roster == 2 ? 1.34f : 1.18f)   // tighter framing: crop the empty margins, enlarge fighters (V9)
                : (roster <= 1 ? 1.44f : roster == 2 ? 1.26f : 1.11f);

            var size = new Vector2(256, 386);
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
                var anchor = Formation(u.team, u.slot);
                v.Build(_stage, anchor, size, teamColor, speciesColor,
                    u.speciesId, dn, _font, elem, role, u.team == 0);
                v.SetReserve(false);                    // brawl: everyone full-size, on stage
                v.SetWeight(RoleWeight(role));          // heft by role (presentation only)
                v.SetMaxHp(u.maxHp); v.SetHp(u.currentHp); v.PlaySpawn();
                if (elementColors != null && elementColors.TryGetValue(u.speciesId, out var ec)) v.SetElement(ec, elem);
                v.SetBarRaise((u.slot % 3) * 32f);   // stagger HP-bar height per slot so stacked teammates don't collide into label soup
                int k = Key(u.team, u.slot);
                if (levelByKey != null && levelByKey.TryGetValue(k, out var lvl)) v.SetLevel(lvl);   // Lv badge on the fighter
                _views[k] = v;
                _speciesByKey[k] = u.speciesId;
                _styleByKey[k] = (_styleMap != null && _styleMap.TryGetValue(u.speciesId, out var st)) ? st : AttackStyle.MeleeLunge;
            }
            BuildHud();
            BuildFightHud();
            BuildCeremony();
            // Units start in formation (set by Build); the opening charge in
            // UpdateEngagement sprints both teams to the centre and they collide.
        }

        // Combo counter (one global), text splash, and letterbox bars — screen-fixed HUD.
        void BuildFightHud()
        {
            _comboLabel = HudText(_root, "", 62, new Vector2(0, 470), new Vector2(700, 90), FontStyle.Bold);
            _comboLabel.color = new Color(1, 1, 1, 0);
            _splash = HudText(_root, "", 72, new Vector2(0, 150), new Vector2(1000, 160), FontStyle.Bold);
            _splash.color = new Color(1, 1, 1, 0);

            _letterTop = LetterBar(new Vector2(0.5f, 1f));
            _letterBot = LetterBar(new Vector2(0.5f, 0f));
        }

        RectTransform LetterBar(Vector2 anchor)
        {
            var go = new GameObject("Letter", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_root, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
            rt.sizeDelta = new Vector2(1500, 140); rt.anchoredPosition = new Vector2(0, anchor.y > 0.5f ? 140f : -140f);
            var img = go.GetComponent<Image>(); img.color = new Color(0, 0, 0, 0.92f); img.raycastTarget = false;
            return rt;
        }

        Text HudText(RectTransform parent, string s, int size, Vector2 pos, Vector2 sz, FontStyle style)
        {
            var go = new GameObject("HudTxt", typeof(RectTransform), typeof(Text));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false); rt.SetAsLastSibling();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = sz; rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>(); t.font = _font; t.text = s; t.fontSize = size; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        // ---- Phase Q ceremony: VS screen, super flash, finisher darken ----
        void BuildCeremony()
        {
            _finDark = FullImage(_root, new Color(0, 0, 0, 0));   // finisher darken (over arena, under HUD)
            BuildSuper();
            BuildVictory();
            BuildVS();
        }

        // Victory hero screen: a big win-flavour banner + win-tier stars, shown when the fight ends.
        void BuildVictory()
        {
            if (_victoryRoot != null) Destroy(_victoryRoot.gameObject);   // don't let a prior scene's banner linger (showcase)
            var go = new GameObject("VictoryRoot", typeof(RectTransform)); _victoryRoot = go.GetComponent<RectTransform>();
            _victoryRoot.SetParent(_root, false); _victoryRoot.anchorMin = Vector2.zero; _victoryRoot.anchorMax = Vector2.one; _victoryRoot.offsetMin = _victoryRoot.offsetMax = Vector2.zero;
            _victoryText = HudText(_victoryRoot, "", 74, new Vector2(0, 250), new Vector2(1060, 160), FontStyle.BoldAndItalic);
            _victoryText.color = new Color(1, 1, 1, 0);
            _victoryStars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var s = new GameObject("VStar", typeof(RectTransform), typeof(Image));
                var rt = s.GetComponent<RectTransform>(); rt.SetParent(_victoryRoot, false); rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(92, 92); rt.anchoredPosition = new Vector2(-112 + i * 112, 128);
                var img = s.GetComponent<Image>(); img.sprite = ProceduralArt.Star(); img.raycastTarget = false; img.color = new Color(1, 1, 1, 0);
                _victoryStars[i] = img;
            }
            _victoryRoot.gameObject.SetActive(false);
        }

        IEnumerator ShowVictory()
        {
            if (_victoryRoot == null) yield break;
            _victoryRoot.SetAsLastSibling(); _victoryRoot.gameObject.SetActive(true);
            _victoryText.text = _winBanner.ToUpper();
            float t = 0f;
            while (t < 0.4f) { t += Time.deltaTime; float p = Mathf.Clamp01(t / 0.4f); _victoryText.color = new Color(1f, 0.92f, 0.42f, p); _victoryText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, p); yield return null; }
            for (int i = 0; i < 3; i++)
            {
                bool lit = i < _winStars;
                _victoryStars[i].color = lit ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(0.35f, 0.35f, 0.42f, 0.85f);
                float st = 0f;
                while (st < 0.16f) { st += Time.deltaTime; _victoryStars[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, st / 0.16f); yield return null; }
                if (lit) AudioManager.Play(Sfx.Crit);
            }
        }

        Image FullImage(RectTransform parent, Color c)
        {
            var go = new GameObject("Full", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = new Vector2(1500, 2100); rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false; return img;
        }

        void BuildSuper()
        {
            var go = new GameObject("SuperRoot", typeof(RectTransform)); _superRoot = go.GetComponent<RectTransform>();
            _superRoot.SetParent(_root, false); _superRoot.anchorMin = Vector2.zero; _superRoot.anchorMax = Vector2.one; _superRoot.offsetMin = _superRoot.offsetMax = Vector2.zero;
            _superDim = FullImage(_superRoot, new Color(0, 0, 0, 0));
            _superTint = FullImage(_superRoot, new Color(1, 1, 1, 0));
            var sil = new GameObject("SuperSil", typeof(RectTransform), typeof(Image)); _superSil = sil.GetComponent<Image>();
            var srt = sil.GetComponent<RectTransform>(); srt.SetParent(_superRoot, false); srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f); srt.sizeDelta = new Vector2(560, 560); srt.anchoredPosition = new Vector2(0, 40);
            _superSil.raycastTarget = false; _superSil.preserveAspect = true; _superSil.color = new Color(1, 1, 1, 0);
            var cut = new GameObject("SuperCut", typeof(RectTransform), typeof(Image)); _superCutin = cut.GetComponent<Image>();
            var crt = cut.GetComponent<RectTransform>(); crt.SetParent(_superRoot, false); crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f); crt.sizeDelta = new Vector2(380, 380); crt.anchoredPosition = new Vector2(-320, -260);
            _superCutin.raycastTarget = false; _superCutin.preserveAspect = true; _superCutin.color = new Color(1, 1, 1, 0);
            _superBanner = HudText(_superRoot, "", 70, new Vector2(0, -160), new Vector2(1400, 120), FontStyle.BoldAndItalic);
            _superBanner.color = new Color(1, 1, 1, 0);
            _superRoot.gameObject.SetActive(false);
        }

        void BuildVS()
        {
            var go = new GameObject("VS", typeof(RectTransform)); _vs = go.GetComponent<RectTransform>();
            _vs.SetParent(_root, false); _vs.anchorMin = Vector2.zero; _vs.anchorMax = Vector2.one; _vs.offsetMin = _vs.offsetMax = Vector2.zero; _vs.SetAsLastSibling();
            _vsGroup = go.AddComponent<CanvasGroup>();
            FullImage(_vs, new Color(0.03f, 0.03f, 0.06f, 0.96f));
            _vsLeft = BuildVsCard(0);
            _vsRight = BuildVsCard(1);
            _vsMid = HudText(_vs, "VS", 130, new Vector2(0, 20), new Vector2(500, 220), FontStyle.BoldAndItalic);
            _vsFlash = FullImage(_vs, new Color(1, 1, 1, 0));
        }

        RectTransform BuildVsCard(int team)
        {
            string sp = null; foreach (var u in _pb.Units) if (u.team == team) { sp = u.speciesId; break; }
            float side = team == 0 ? -1f : 1f;
            var card = new GameObject("VsCard", typeof(RectTransform)); var rt = card.GetComponent<RectTransform>();
            rt.SetParent(_vs, false); rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(440, 640); rt.anchoredPosition = new Vector2(side * 270f, 40f);
            var sprite = sp != null ? MonsterVisual.For(sp, false) : null;
            if (sprite != null)
            {
                var pg = new GameObject("P", typeof(RectTransform), typeof(Image)); var prt = pg.GetComponent<RectTransform>();
                prt.SetParent(rt, false); prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f); prt.sizeDelta = new Vector2(360, 360); prt.anchoredPosition = new Vector2(0, 90);
                var pi = pg.GetComponent<Image>(); pi.sprite = sprite; pi.preserveAspect = true; pi.raycastTarget = false; pi.rectTransform.localScale = new Vector3(team == 0 ? -1f : 1f, 1f, 1f);
            }
            string dn = displayNames != null && sp != null && displayNames.TryGetValue(sp, out var d) ? d : Humanize(sp);
            HudText(rt, dn, 40, new Vector2(0, -130), new Vector2(440, 60), FontStyle.Bold);
            string el = elementNames != null && sp != null && elementNames.TryGetValue(sp, out var elm) ? elm : "";
            Color ec = elementColors != null && sp != null && elementColors.TryGetValue(sp, out var c) ? c : Color.gray;
            var dot = new GameObject("Elem", typeof(RectTransform), typeof(Image)); var drt = dot.GetComponent<RectTransform>();
            drt.SetParent(rt, false); drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f); drt.sizeDelta = new Vector2(26, 26); drt.anchoredPosition = new Vector2(-120, -186);
            var di = dot.GetComponent<Image>(); di.sprite = ProceduralArt.Disc(); di.color = ec; di.raycastTarget = false;
            HudText(rt, el, 26, new Vector2(-10, -186), new Vector2(200, 40), FontStyle.Bold);
            int rar = rarities != null && sp != null && rarities.TryGetValue(sp, out var rr) ? rr : 3;
            for (int i = 0; i < 5; i++)
            {
                var s = new GameObject("St", typeof(RectTransform), typeof(Image)); var strt = s.GetComponent<RectTransform>();
                strt.SetParent(rt, false); strt.anchorMin = strt.anchorMax = new Vector2(0.5f, 0.5f); strt.sizeDelta = new Vector2(30, 30); strt.anchoredPosition = new Vector2(-60 + i * 32f, -240);
                var si = s.GetComponent<Image>(); si.sprite = ProceduralArt.Star(); si.raycastTarget = false; si.color = i < rar ? new Color(1f, 0.82f, 0.28f) : new Color(0.3f, 0.3f, 0.36f, 0.8f);
            }
            return rt;
        }

        void TickVS()
        {
            _vsT -= Time.deltaTime;
            if ((Input.GetMouseButtonDown(0) || Input.touchCount > 0) && _vsT > 0.3f) _vsT = 0.3f;   // tap to skip
            float p = 1f - Mathf.Clamp01(_vsT / VS_DUR);
            float ease = 1f - (1f - Mathf.Clamp01(p / 0.28f)) * (1f - Mathf.Clamp01(p / 0.28f));   // slam-in
            if (_vsLeft != null) _vsLeft.anchoredPosition = new Vector2(Mathf.Lerp(-780f, -270f, ease), 40f);
            if (_vsRight != null) _vsRight.anchoredPosition = new Vector2(Mathf.Lerp(780f, 270f, ease), 40f);
            if (_vsMid != null) _vsMid.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, Mathf.Clamp01((p - 0.3f) / 0.15f));
            if (!_vsFlashed && p >= 0.3f) { _vsFlashed = true; Shake(14f); if (_vsFlash != null) _vsFlash.color = new Color(1, 1, 1, 0.8f); AudioManager.Play(Sfx.Crit); }
            if (_vsFlash != null && _vsFlash.color.a > 0f) { var fc = _vsFlash.color; fc.a = Mathf.Max(0f, fc.a - Time.deltaTime * 3.5f); _vsFlash.color = fc; }
            if (_vsGroup != null) _vsGroup.alpha = _vsT < 0.3f ? Mathf.Clamp01(_vsT / 0.3f) : 1f;
            if (_vsT <= 0f) EndVS();
        }

        void EndVS()
        {
            if (_vs != null) _vs.gameObject.SetActive(false);
            Splash("ROUND 1", Color.white, 60f, 0.7f, 0f);   // FIGHT! follows on the opening-charge collision
        }

        void CeremonyFreeze(float d) { _hitstop = Mathf.Max(_hitstop, d); }   // ceremony clock hold (not budgeted)

        // KOF/GG-style ultimate ceremony: freeze, dim, element tint, lit caster silhouette,
        // name banner slide, diagonal cut-in — then the ult executes. ≤ ~1.1 s.
        IEnumerator SuperFlash(UnitView caster, string actorSp)
        {
            CeremonyFreeze(1.05f);
            AudioManager.Play(Sfx.Whoosh); AudioManager.Play(Sfx.Ultimate); AudioManager.PlayPitched(Sfx.Bass, 1f, 1f);
            if (_superRoot == null) { yield return new WaitForSecondsRealtime(0.4f); yield break; }
            Color ec = elementColors != null && elementColors.TryGetValue(actorSp, out var e) ? e : new Color(1f, 0.7f, 0.2f);
            _arena?.Flash(ec, 0.4f);   // ultimate → arena flash
            var sprite = caster != null ? caster.CurrentSprite : null;
            int mir = caster != null ? caster.Mirror : 1;
            try
            {
                _superRoot.gameObject.SetActive(true); _superRoot.SetAsLastSibling();
                if (sprite != null) { _superSil.sprite = sprite; _superCutin.sprite = sprite; }
                _superBanner.text = SpeciesIdentity.SkillWord(actorSp).ToUpper();
                _zoomTarget = _baseZoom + 0.18f;
                float total = 1.0f, t = 0f;
                while (t < total)
                {
                    t += Time.deltaTime; float p = t / total;
                    float fade = Mathf.Clamp01(p / 0.15f) * (p > 0.78f ? Mathf.Clamp01((1f - p) / 0.22f) : 1f);
                    _superDim.color = new Color(0, 0, 0, 0.7f * fade);
                    _superTint.color = new Color(ec.r, ec.g, ec.b, 0.35f * fade);
                    _superSil.color = new Color(1f, 1f, 1f, 0.9f * fade);
                    _superSil.rectTransform.localScale = new Vector3(mir, 1f, 1f) * Mathf.Lerp(1.3f, 1f, Mathf.Clamp01(p / 0.3f));
                    _superBanner.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-1500f, 1500f, p), -160f);
                    _superBanner.color = new Color(1f, 0.95f, 0.7f, fade);
                    float cp = Mathf.Clamp01(p / 0.25f);
                    _superCutin.rectTransform.localScale = new Vector3(mir, 1f, 1f);
                    _superCutin.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-720f, -320f, cp), Mathf.Lerp(-640f, -260f, cp));
                    _superCutin.color = new Color(1f, 1f, 1f, fade);
                    yield return null;
                }
            }
            finally
            {
                var clear = new Color(1, 1, 1, 0);
                _superDim.color = _superTint.color = _superSil.color = _superCutin.color = clear; _superBanner.color = clear;
                _superRoot.gameObject.SetActive(false);
            }
        }

        // Ultimate dispatch: ceremony, then the real ult choreography (melee spotlight or ranged).
        IEnumerator SuperUlt(int at, int as_, int tt, int ts, ChoreoBeat b, AttackStyle st, string actorSp, bool ranged)
        {
            var A = View(at, as_);
            yield return SuperFlash(A, actorSp);
            if (ranged)
            {
                Vector2 from = PosOf(at, as_), to = PosOf(tt, ts);
                A?.PlayAttack(new Vector2(at == 0 ? 1f : -1f, 0f), 26f, false);
                _fx.Projectile(from, to, ProjColor(st), () => { });
                yield return new WaitForSecondsRealtime(0.12f);
                yield return RangedHit(at, as_, tt, ts, b, actorSp, true);
                _spotlightBusy = false; _spotlightTargetKey = -1;
            }
            else
            {
                yield return SpotlightCombo(at, as_, tt, ts, b, st, actorSp, true);   // clears _spotlightBusy in finally
            }
        }

        IEnumerator VictoryHold()
        {
            yield return new WaitForSecondsRealtime(2.7f);   // hold the victory hero screen
            OnFinished?.Invoke(_pb.WinnerTeam);
        }

        // Fighting-game round pips: player team left, enemy right, screen-fixed.
        void BuildHud()
        {
            if (_hud != null) Destroy(_hud.gameObject);
            var go = new GameObject("Hud", typeof(RectTransform));
            _hud = go.GetComponent<RectTransform>(); _hud.SetParent(_root, false); _hud.SetAsLastSibling();
            _hud.anchorMin = _hud.anchorMax = new Vector2(0.5f, 0.5f);
            _hud.sizeDelta = new Vector2(1080, 120); _hud.anchoredPosition = new Vector2(0, 740);
            _pips0.Clear(); _pips1.Clear(); _port0.Clear(); _port1.Clear();
            HudLabel("VS", 40, Vector2.zero, new Vector2(200, 70));
            var sp0 = TeamSpecies(0); var sp1 = TeamSpecies(1);
            for (int i = 0; i < sp0.Count; i++) AddPip(0, new Vector2(-150 - i * 82, 0), sp0[i]);
            for (int i = 0; i < sp1.Count; i++) AddPip(1, new Vector2(150 + i * 82, 0), sp1[i]);
        }

        List<string> TeamSpecies(int team)
        {
            var list = new List<string>();
            var units = _pb.Units;
            for (int slot = 0; slot < 6; slot++)
                for (int i = 0; i < units.Count; i++)
                    if (units[i].team == team && units[i].slot == slot) { list.Add(units[i].speciesId); break; }
            return list;
        }

        // Framed portrait chip (V4 backlog): team-coloured rounded frame + dark inset + the monster's
        // portrait. Beats a flat square — the top HUD now shows WHO is on each side. Dead → dims.
        void AddPip(int team, Vector2 pos, string species)
        {
            Color frameCol = team == 0 ? new Color(0.32f, 0.6f, 1f) : new Color(1f, 0.42f, 0.36f);
            var chip = new GameObject("Pip", typeof(RectTransform), typeof(Image));
            var rt = chip.GetComponent<RectTransform>(); rt.SetParent(_hud, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(64, 64); rt.anchoredPosition = pos;
            var frame = chip.GetComponent<Image>(); frame.sprite = ProceduralArt.RoundedRect(); frame.color = frameCol; frame.raycastTarget = false;

            var inset = new GameObject("In", typeof(RectTransform), typeof(Image));
            var irt = inset.GetComponent<RectTransform>(); irt.SetParent(rt, false); irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.sizeDelta = new Vector2(54, 54); irt.anchoredPosition = Vector2.zero;
            var ii = inset.GetComponent<Image>(); ii.sprite = ProceduralArt.RoundedRect(); ii.color = new Color(0.09f, 0.10f, 0.13f, 1f); ii.raycastTarget = false;

            var pg = new GameObject("Port", typeof(RectTransform), typeof(Image));
            var prt = pg.GetComponent<RectTransform>(); prt.SetParent(rt, false); prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f); prt.sizeDelta = new Vector2(52, 52); prt.anchoredPosition = new Vector2(0, 2);
            var pi = pg.GetComponent<Image>(); pi.preserveAspect = true; pi.raycastTarget = false;
            var portrait = MonsterVisual.For(species, false);
            if (portrait != null) { pi.sprite = portrait; pi.color = Color.white; } else pi.enabled = false;

            (team == 0 ? _pips0 : _pips1).Add(frame);
            (team == 0 ? _port0 : _port1).Add(pi);
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

        int CountTeam(int t) { int n = 0; foreach (var u in _pb.Units) if (u.team == t) n++; return n; }

        void UpdatePips()
        {
            SetPipRow(_pips0, _port0, 0, new Color(0.32f, 0.6f, 1f));
            SetPipRow(_pips1, _port1, 1, new Color(1f, 0.42f, 0.36f));
        }

        void SetPipRow(List<Image> pips, List<Image> ports, int team, Color live)
        {
            int alive = _pb.AliveCount(team);
            var deadFrame = new Color(0.22f, 0.22f, 0.26f, 0.9f);
            var deadPort = new Color(0.35f, 0.35f, 0.4f, 0.55f);
            for (int i = 0; i < pips.Count; i++)
            {
                bool on = i < alive;
                if (pips[i] != null) pips[i].color = on ? live : deadFrame;
                if (i < ports.Count && ports[i] != null && ports[i].enabled) ports[i].color = on ? Color.white : deadPort;
            }
        }

        void Update()
        {
            if (_playing)
            {
                if (_vsT > 0f) { TickVS(); UpdateCamera(); return; }   // hold on the VS screen
                _stageTime += Time.deltaTime;
                if (_slowmoT > 0f) { _slowmoT -= Time.deltaTime; if (_slowmoT <= 0f) _slowmo = 1f; }

                if (_hitstop > 0f)
                {
                    _hitstop -= Time.deltaTime;                       // capped heavy/crit/ult/KO freeze
                }
                else
                {
                    _clock += Time.deltaTime * _simPerReal * speedMultiplier * _slowmo;
                    while (_replay != null && _rIdx < _replay.Count && _replay[_rIdx].t <= _clock)
                    {
                        Apply(_replay[_rIdx], _cho.beats[_rIdx], _rIdx);
                        _rIdx++;
                    }
                    while (_plan != null && _fillerQ < _plan.fillers.Count && _plan.fillers[_fillerQ].t <= _clock)
                    {
                        PlayFiller(_plan.fillers[_fillerQ]); _fillerQ++;
                    }
                    _pb.ProcessUpTo(_clock);
                    UpdateEngagement();
                }

                foreach (var u in _pb.Units)
                    if (_views.TryGetValue(Key(u.team, u.slot), out var v)) v.SetHp(u.currentHp);
                UpdatePips();

                // Dynamic mixing — four states drive the music intensity:
                //   normal → close (teams even) → last-monster-alive (climax) → finisher (duck).
                int aliveA = _pb.AliveCount(0), aliveB = _pb.AliveCount(1), total = aliveA + aliveB;
                if (!_finalDuel && aliveA == 1 && aliveB == 1 && _teamCount[0] + _teamCount[1] > 2)
                { _finalDuel = true; Splash("FINAL DUEL!", CCrit, 82f, 1.1f, 6f); ApplyCam(ChoreoCam.ZoomCombo); }   // story beat (P3)
                float intensity;
                if (total <= 2) intensity = 1f;                                   // last monsters alive → full climax
                else if (aliveA == aliveB) intensity = 0.7f;                       // close / even trade
                else intensity = 0.35f * (1f - Mathf.Abs(aliveA - aliveB) * 0.25f); // normal, ebbs with the lead
                AudioManager.SetBattleIntensity(Mathf.Clamp01(intensity));

                bool eventsDone = _replay == null || _rIdx >= _replay.Count;
                if (!_finishedFired && eventsDone && _clock >= _pb.Duration && _hitstop <= 0f)
                {
                    _finishedFired = true; _playing = false; _zoomTarget = _baseZoom + 0.1f; _letterTarget = 0f; _finDarkTarget = 0f;
                    int wIdx = 0; float sidew = _pb.WinnerTeam == 0 ? -1f : 1f;
                    foreach (var wu in _pb.Units)
                        if (wu.team == _pb.WinnerTeam && wu.Alive && _views.TryGetValue(Key(wu.team, wu.slot), out var wv))
                        {
                            wv.PlayVictory();
                            wv.EnterFrom(wv.BasePos, new Vector2(sidew * 150f + wIdx * 14f, -20f + wIdx * 52f));   // step forward, staggered
                            wIdx++;
                        }
                    AudioManager.Play(Sfx.Victory); AudioManager.Announce(Sfx.VoVictory);
                    StartCoroutine(ShowVictory());   // victory hero banner + win-tier stars
                    StartCoroutine(VictoryHold());   // hold the celebration, then show the result screen
                }
            }
            UpdateCamera();
        }

        UnitView View(int t, int s) => _views.TryGetValue(Key(t, s), out var v) ? v : null;
        AttackStyle StyleOf(int team, int slot) =>
            _styleByKey.TryGetValue(Key(team, slot), out var s) ? s : AttackStyle.MeleeLunge;
        Vector2 PosOf(int team, int slot) => View(team, slot) is UnitView v ? v.BasePos : Formation(team, slot);
        static Vector2 Ground(Vector2 p) => new Vector2(p.x, p.y - 132f);   // feet on the arena floor (scales with bigger sprites)

        void Apply(ReplayEvent e, ChoreoBeat b, int idx)
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
                    Vector2 dir = new Vector2(e.actorTeam == 0 ? 1f : -1f, 0f);

                    // Skill / ultimate banners.
                    if (av != null && e.kind == ReplayEventKind.Skill)
                        _texts.Spawn(av.BasePos + new Vector2(0, 100), SpeciesIdentity.SkillWord(actorSp), COrange, 30);
                    if (av != null && ult)
                    {
                        _texts.Spawn(av.BasePos + new Vector2(0, 110), "ULTIMATE", COrange, 44);
                        _texts.Spawn(av.BasePos + new Vector2(0, 62), SpeciesIdentity.SkillWord(actorSp), COrange, 26);
                        _fx.Burst(av.BasePos, BurstKind.Ultimate);
                    }
                    if (av != null && (e.kind == ReplayEventKind.Skill || ult))
                    {
                        // Element-signature cast aura at the caster — recognisable by shape.
                        _elem.Burst(Elem(actorSp), av.BasePos + new Vector2(0, 10), ult ? 260f : 190f, ult ? ElemFx.Ultimate : ElemFx.Cast);
                    }
                    if (e.kind == ReplayEventKind.Skill) AudioManager.Play(Sfx.Skill);
                    else if (ult) AudioManager.Play(Sfx.Ultimate);

                    if (e.isBuff)
                    {
                        av?.PlayAttack(dir, DashDist(st, ult) * 0.4f, ult);
                        if (ult) { _zoomTarget = _baseZoom + 0.12f; ApplyCam(b.cam); }
                        break;
                    }

                    RegisterCombo();   // same offensive-event set as Combo King

                    int tt = e.targetTeam, ts = e.targetSlot;

                    // Element matchup readout: surface the triangle (Fire>Nature>Water>Fire) that the sim
                    // already applies to damage. Throttled so it reads as a call-out, not spam.
                    int adv = ElementAdv(Elem(actorSp), _speciesByKey.TryGetValue(Key(tt, ts), out var tsp) ? Elem(tsp) : "");
                    if (adv != 0 && _clock - _lastAdvT > 0.8)
                    {
                        _lastAdvT = _clock;
                        Vector2 tp = PosOf(tt, ts) + new Vector2(0, 150);
                        if (adv > 0) _texts.Spawn(tp, "SUPER EFFECTIVE!", new Color(1f, 0.82f, 0.28f), 26);
                        else _texts.Spawn(tp, "RESISTED", new Color(0.72f, 0.82f, 1f), 22);
                    }

                    bool ranged = AttackStyles.IsRanged(st);
                    bool big = ult || b.crit || b.hits >= 3;
                    bool clash = _plan != null && _plan.clashEventIdx.Contains(idx);

                    if (ult && av != null) CrowdFlinch(Key(e.actorTeam, e.actorSlot), tt, ts, av.BasePos, 300f);

                    if (clash)
                    {
                        if (_clock - _lastClashT > 0.18) { _lastClashT = _clock; ClashFlourish(e.actorTeam, e.actorSlot, tt, ts, dir); }
                        CompactLunge(e.actorTeam, e.actorSlot, tt, ts, b);   // real damage number + knock inside the clash
                    }
                    else if (ult && !_spotlightBusy)
                    {
                        _spotlightBusy = true; _spotlightTargetKey = Key(tt, ts);   // super ceremony, then the ult executes
                        _camFocusTarget = Vector2.Lerp(PosOf(e.actorTeam, e.actorSlot), PosOf(tt, ts), 0.55f);   // cinematic pan to the clash (P2)
                        StartCoroutine(SuperUlt(e.actorTeam, e.actorSlot, tt, ts, b, st, actorSp, ranged));
                    }
                    else if (ranged)
                    {
                        Vector2 from = PosOf(e.actorTeam, e.actorSlot), to = PosOf(tt, ts);
                        av?.PlayAttack(dir, 26f, false);
                        string pel = Elem(actorSp);
                        Color pcol = pel != "" ? ElemColor(pel) : ProjColor(st);
                        _fx.Projectile(from, to, pcol,
                            () => StartCoroutine(RangedHit(e.actorTeam, e.actorSlot, tt, ts, b, actorSp, ult)),
                            ElemSprite(pel));
                    }
                    else if (big && !_spotlightBusy)
                    {
                        _spotlightBusy = true;                       // claim the single cinematic slot NOW
                        _spotlightTargetKey = Key(tt, ts);
                        _camFocusTarget = Vector2.Lerp(PosOf(e.actorTeam, e.actorSlot), PosOf(tt, ts), 0.55f);   // cinematic pan to the clash (P2)
                        _zoomTarget = _baseZoom + (ult ? 0.14f : 0.06f);
                        StartCoroutine(SpotlightCombo(e.actorTeam, e.actorSlot, tt, ts, b, st, actorSp, ult));
                    }
                    else if (_busyUnits.Contains(Key(e.actorTeam, e.actorSlot)))
                    {
                        CompactLunge(e.actorTeam, e.actorSlot, tt, ts, b);   // already dashing: strike in place
                    }
                    else
                    {
                        StartCoroutine(CompactDash(e.actorTeam, e.actorSlot, tt, ts, b, actorSp));
                    }
                    break;
                }
                case ReplayEventKind.Heal:
                    if (View(e.targetTeam, e.targetSlot) is UnitView hv)
                    {
                        hv.PlayHeal();
                        _texts.Spawn(hv.BasePos + Jitter(), "+" + e.amount, CHeal, 30);
                        _elem.Burst("Heal", hv.BasePos, 160f, ElemFx.Heal);   // rising plus-crosses + aura ring
                        AudioManager.Play(Sfx.Heal);
                    }
                    break;
                case ReplayEventKind.Death:
                {
                    Vector2 knock = new Vector2(e.targetTeam == 0 ? -1f : 1f, 0f);   // away from enemy
                    var dv = View(e.targetTeam, e.targetSlot);
                    if (dv != null)
                    {
                        dv.Knock(knock, b.knockback); dv.Launch(b.endsBattle ? 170f : 130f);   // launch + spin + fade (no run-in)
                        dv.PlayDeath(knock);
                        _vfx.Play("explosion", dv.BasePos, b.endsBattle ? 340f : 240f, Color.white);
                        string kosp = _speciesByKey.TryGetValue(Key(e.targetTeam, e.targetSlot), out var ksp) ? ksp : "";
                        _elem.Burst(Elem(kosp), dv.BasePos, b.endsBattle ? 380f : 270f, ElemFx.KO);   // KO = biggest element beat
                        _arena?.React(b.endsBattle ? ArenaReact.KO : ArenaReact.Debris, Ground(dv.BasePos));   // full arena reaction on the finisher
                        CrowdFlinch(Key(e.targetTeam, e.targetSlot), -1, -1, dv.BasePos, 280f);   // scrum recoils from the KO
                        CamPush(knock, b.endsBattle ? 60f : 44f);   // camera lurches with the KO
                    }
                    if (b.endsBattle)
                    {
                        _texts.Spawn(Formation(1 - e.targetTeam, 0) + new Vector2(0, 240), FinisherWord(b.finisher), CCrit, 40);
                        Splash("K.O.!", CCrit, 96f, 1.5f, 6f);
                        AudioManager.SetFinisher();   // music ducks then swells for the finishing blow
                        ApplyCam(ChoreoCam.SlowMoFinisher);
                        if (dv != null) StartCoroutine(ImpactFrame(dv));   // finisher impact frame
                    }
                    else ApplyCam(ChoreoCam.ShakeCrit);
                    if (!b.endsBattle && !_firstBlood) { _firstBlood = true; Splash("FIRST BLOOD!", COrange, 76f, 1.0f, 5f); }   // story beat (P3)
                    HitStop(b.endsBattle ? 0.15f : 0.10f);   // KO freeze (tiered; heavy only)
                    AudioManager.Play(Sfx.Death);
                    AudioManager.Impact(b.endsBattle, true, _hudRng.Range(0.9f, 1.1f), Elem(_speciesByKey.TryGetValue(Key(e.targetTeam, e.targetSlot), out var vsp) ? vsp : ""));
                    if (b.endsBattle) AudioManager.Announce(Sfx.VoKO);
                    break;
                }
                case ReplayEventKind.Victory:
                    ApplyCam(ChoreoCam.ZoomWinner);
                    break;
            }
        }

        // ---- SPOTLIGHT: the full cinematic melee suite (one at a time). Dash to an
        // approach point beside the target → ground combo → launcher → air combo → slam
        // → recovery. Capped hit-stop only on the heavy beats. ----
        IEnumerator SpotlightCombo(int at, int as_, int tt, int ts, ChoreoBeat b, AttackStyle st, string actorSp, bool ult)
        {
            int key = Key(at, as_);
            _busyUnits.Add(key);
            var A = View(at, as_); var T = View(tt, ts);
            try
            {
                Vector2 dir = new Vector2(at == 0 ? 1f : -1f, 0f);
                float sp = Mathf.Clamp(speedMultiplier, 0.5f, 4f);
                int n = Mathf.Clamp(b.hits, 3, 15);
                if (A == null || T == null) yield break;

                // Dodge → counter (presentation-only sidestep before the hit lands).
                if (b.dodge)
                {
                    Afterimage(T);
                    T.Dodge(new Vector2(-dir.x, 0.3f));
                    _texts.Spawn(T.BasePos + new Vector2(0, 80), "MISS", CDodge, 34);
                    _vfx.Play("puff", T.BasePos, 150f, new Color(1f, 1f, 1f, 0.9f));
                    AudioManager.PlayPitched(Sfx.Whoosh, 1.15f, 0.9f);   // distinct dodge swish
                    yield return new WaitForSecondsRealtime(0.13f / sp);
                    T.PlayAttack(new Vector2(-dir.x, 0f), 70f, false);
                    Splash("COUNTER!", new Color(1f, 0.9f, 0.4f), 64f, 0.8f, 5f); AudioManager.Announce(Sfx.VoCounter);
                    _vfx.Play("hit_small", A.BasePos, 120f, Color.white);
                    A.PlayHit(false); A.Knock(dir, 40f); Shake(6f); AudioManager.Play(Sfx.Hit);
                    yield return new WaitForSecondsRealtime(0.12f / sp);
                }

                float gap = Mathf.Abs(T.BasePos.x - A.BasePos.x);
                Vector2 close = new Vector2(dir.x * (gap - 150f), (as_ - 1) * 30f);   // side-offset by slot

                Color gtint = elementColors != null && elementColors.TryGetValue(actorSp, out var gec) ? gec : Color.white;

                // 1) ANTICIPATION + DASH IN (squash-crouch, dash with afterimages, land overshoot)
                A.Squash(1.10f, 0.85f, 0.07f);
                yield return new WaitForSecondsRealtime(0.05f / sp);
                _vfx.Play("speedlines", A.BasePos + dir * 40f, 150f, new Color(1f, 1f, 1f, 0.32f)); AudioManager.Play(Sfx.Whoosh);   // faint motion blur (V4: was a solid barcode box)
                yield return MoveOffset(A, Vector2.zero, close, 0.10f / sp, true, gtint);
                A.Squash(0.9f, 1.12f, 0.06f);
                Shake(4f);

                // 2) GROUND COMBO (light — flash + squash + vibrate, NO global freeze)
                int ground = Mathf.Max(2, n / 3);
                for (int i = 0; i < ground; i++)
                {
                    A.PlayAttack(dir, 34f, false); T.PlayHit(false);
                    T.combatOffset = new Vector2(dir.x * 10f, 0f);
                    T.Squash(1.2f, 0.8f, 0.08f); T.Vibrate(2.5f, 0.05f);
                    _vfx.Play("hit_small", T.BasePos + ComboJit(i), 130f, Color.white);
                    _fx.Burst(T.BasePos + ComboJit(i), BurstKind.Slash);
                    _arena?.React(ArenaReact.Dust, Ground(T.BasePos));
                    Shake(4f); AudioManager.Play(Sfx.Hit);
                    yield return new WaitForSecondsRealtime(0.05f / sp);
                }

                // 3) LAUNCHER — target spins up, attacker jumps after (heavy → capped freeze)
                AudioManager.Impact(false, true, _hudRng.Range(0.9f, 1.1f), Elem(actorSp));
                _vfx.Play("hit_big", T.BasePos, 230f, Color.white); Shake(14f); FlashScreen(0.4f);
                _elem.Burst(Elem(actorSp), T.BasePos, 220f, ElemFx.Impact);   // element flourish on the launcher
                _texts.Spawn(T.BasePos + new Vector2(0, 46), "LAUNCH!", CCrit, 30);
                _arena?.React(ArenaReact.Crack, Ground(T.BasePos)); _arena?.React(ArenaReact.Debris, Ground(T.BasePos));
                T.Spin(720f, 0.55f); T.Squash(1.25f, 0.78f, 0.09f); T.Vibrate(3f, 0.09f); HitStop(0.09f);
                StartCoroutine(MoveOffset(T, T.combatOffset, new Vector2(dir.x * 24f, 300f), 0.16f / sp, true, gtint));
                yield return MoveOffset(A, close, close + new Vector2(dir.x * 60f, 260f), 0.16f / sp, true, gtint);
                ApplyCam(ChoreoCam.ZoomCombo);

                // 4) AIR COMBO (juggle — afterimages, small squash, no freeze)
                int air = Mathf.Max(2, n - ground - 1);
                for (int i = 0; i < air; i++)
                {
                    A.PlayAttack(dir, 26f, ult && i == air - 1); T.PlayHit(true);
                    T.combatOffset += new Vector2(dir.x * 6f, 14f);
                    A.combatOffset = new Vector2(A.combatOffset.x, T.combatOffset.y - 20f);
                    T.Squash(1.15f, 0.86f, 0.05f); SpawnGhost(T, gtint); SpawnGhost(A, gtint);
                    _vfx.Play("hit_impact", T.BasePos, 150f, Color.white);
                    Shake(6f); AudioManager.Play(Sfx.Hit);
                    yield return new WaitForSecondsRealtime(0.045f / sp);
                }

                // 5) SLAM DOWN (spike — spin down, tiered freeze, impact frame on ultimate)
                AudioManager.Impact(ult, true, _hudRng.Range(0.9f, 1.1f), Elem(actorSp));
                T.Spin(900f, 0.16f);   // (V4: dropped the redundant "SLAM!" banner — LAUNCH + crit word already read)
                yield return MoveOffset(T, T.combatOffset, new Vector2(dir.x * 40f, -30f), 0.11f / sp, true, gtint);
                _vfx.Play(ult ? "explosion" : "hit_big", T.BasePos, ult ? 330f : 250f, Color.white);
                _elem.Burst(Elem(actorSp), T.BasePos, ult ? 320f : 230f, ult ? ElemFx.Ultimate : ElemFx.Impact);   // element signature on the slam
                _arena?.React(ArenaReact.Shockwave, Ground(T.BasePos));
                DamageNumber(T.BasePos, b.amount, true, ult);
                if (b.crit) _texts.Spawn(T.BasePos + new Vector2(0, -70), SpeciesIdentity.CritWord(actorSp), CCrit, 34);
                T.Knock(dir, b.knockback); T.Squash(1.35f, 0.68f, 0.1f); T.Vibrate(3f, ult ? 0.15f : 0.09f);
                A.Knock(-dir, 34f); T.Launch(ult ? 60f : 40f); CamPush(dir, ult ? 56f : 42f);   // recoil + pop + camera push
                if (!ult) MicroImpact(T);
                Shake(ult ? 24f : 18f); FlashScreen(ult ? 0.7f : 0.5f); ZoomPunch(ult ? 0.12f : 0.08f);
                StartCoroutine(Shockwave(T.BasePos, ult ? new Color(1f, 0.6f, 0.2f) : CCrit));
                if (ult) StartCoroutine(ImpactFrame(T));   // impact frame only on ultimates (+ finisher)
                HitStop(ult ? 0.15f : 0.09f);
                yield return new WaitForSecondsRealtime(0.1f / sp);

                // 5b) GROUND BOUNCE ×2 with dust
                yield return Bounce(T, dir, 0.5f, sp);
                yield return Bounce(T, dir, 0.25f, sp);

                // 6) RECOVERY — both return to formation
                StartCoroutine(MoveOffset(T, T.combatOffset, Vector2.zero, 0.2f / sp));
                yield return MoveOffset(A, A.combatOffset, Vector2.zero, 0.14f / sp);
            }
            finally
            {
                _spotlightBusy = false; _spotlightTargetKey = -1; _busyUnits.Remove(key);
                if (A != null) A.combatOffset = Vector2.zero;
            }
        }

        // ---- COMPACT: quick dash-to-target, a few hits, return. No aerial suite, no
        // global clock freeze — many of these overlap without stalling the brawl. ----
        IEnumerator CompactDash(int at, int as_, int tt, int ts, ChoreoBeat b, string actorSp)
        {
            int key = Key(at, as_);
            _busyUnits.Add(key);
            var A = View(at, as_);
            try
            {
                var T = View(tt, ts);
                if (A == null || T == null) yield break;
                Vector2 dir = new Vector2(at == 0 ? 1f : -1f, 0f);
                float sp = Mathf.Clamp(speedMultiplier, 0.5f, 4f);
                Color gtint = elementColors != null && elementColors.TryGetValue(actorSp, out var gec) ? gec : Color.white;
                float gap = Mathf.Abs(T.BasePos.x - A.BasePos.x);
                Vector2 close = new Vector2(dir.x * (gap - 170f), (as_ - 1) * 34f);

                yield return MoveOffset(A, Vector2.zero, close, 0.14f / sp, true, gtint);   // afterimages on the dash
                int n = Mathf.Clamp(b.hits, 1, 3);
                for (int i = 0; i < n; i++)
                {
                    bool last = i == n - 1;
                    A.PlayAttack(dir, 30f, false); T.PlayHit(b.crit && last);
                    _vfx.Play(last && b.crit ? "hit_impact" : "hit_small", T.BasePos + ComboJit(i), last ? 150f : 120f, Color.white);
                    AudioManager.Play(b.crit && last ? Sfx.Crit : Sfx.Hit);
                    T.Squash(1.2f, 0.8f, 0.08f);
                    A.Knock(-dir, last ? 20f : 10f);   // attacker recoils off the impact
                    if (last)
                    {
                        DamageNumber(T.BasePos, b.amount, b.crit, false);
                        T.Knock(dir, b.knockback);
                        _arena?.React(b.crit ? ArenaReact.Crack : ArenaReact.Dust, Ground(T.BasePos));
                        _elem.Burst(Elem(actorSp), T.BasePos, b.crit ? 175f : 135f, ElemFx.Impact);
                        if (b.crit) { T.Vibrate(2.5f, 0.06f); T.Launch(50f); MicroImpact(T); CamPush(dir, 26f); Shake(9f); ZoomPunch(0.03f); StartCoroutine(Shockwave(T.BasePos, CCrit)); }
                    }
                    yield return new WaitForSecondsRealtime(0.05f / sp);
                }
                yield return MoveOffset(A, close, Vector2.zero, 0.16f / sp);
            }
            finally
            {
                _busyUnits.Remove(key);
                if (A != null) A.combatOffset = Vector2.zero;
            }
        }

        // In-place strike when the attacker is already mid-dash (avoids combatOffset conflicts).
        void CompactLunge(int at, int as_, int tt, int ts, ChoreoBeat b)
        {
            var A = View(at, as_); var T = View(tt, ts); if (A == null) return;
            Vector2 dir = new Vector2(at == 0 ? 1f : -1f, 0f);
            A.PlayAttack(dir, 40f, false);
            if (T != null)
            {
                T.PlayHit(b.crit);
                _vfx.Play(b.crit ? "hit_impact" : "hit_small", T.BasePos, 150f, Color.white);
                T.Knock(dir, b.knockback * 0.6f); T.Squash(1.2f, 0.8f, 0.08f); if (b.crit) T.Vibrate(2.5f, 0.06f);
                _arena?.React(b.crit ? ArenaReact.Crack : ArenaReact.Dust, Ground(T.BasePos));
                if (b.crit) _elem.Burst(Elem(_speciesByKey.TryGetValue(Key(at, as_), out var lsp) ? lsp : ""), T.BasePos, 165f, ElemFx.Impact);
                DamageNumber(T.BasePos, b.amount, b.crit, false);
                if (b.crit) { T.Launch(40f); MicroImpact(T); CamPush(dir, 20f); }
            }
            A.Knock(-dir, 14f);   // attacker recoil
            AudioManager.Play(b.crit ? Sfx.Crit : Sfx.Hit);
        }

        // Ranged: fire the compact hit sequence from the slot (projectile already flew).
        IEnumerator RangedHit(int at, int as_, int tt, int ts, ChoreoBeat b, string actorSp, bool ult)
        {
            var T = View(tt, ts);
            float sp = Mathf.Clamp(speedMultiplier, 0.5f, 4f);
            int n = Mathf.Clamp(b.hits, 1, 4);
            for (int i = 0; i < n; i++)
            {
                bool last = i == n - 1;
                bool big = ult && last;
                Vector2 tp = T != null ? T.BasePos : PosOf(tt, ts);
                T?.PlayHit((b.crit || ult) && last);
                _vfx.Play(last ? (ult ? "explosion" : b.crit ? "hit_big" : "hit_impact") : "hit_small",
                    tp, last ? (ult ? 300f : 180f) : 120f, Color.white);
                AudioManager.Play((b.crit || ult) && last ? Sfx.Crit : Sfx.Hit);
                if (T != null) T.Squash(big ? 1.3f : 1.15f, big ? 0.72f : 0.85f, 0.08f);
                if (last)
                {
                    DamageNumber(tp, b.amount, b.crit, ult);
                    _elem.Burst(Elem(actorSp), tp, big ? 300f : 175f, big ? ElemFx.Ultimate : ElemFx.Impact);   // element splash on the landing shot
                    if (T != null) { T.Knock(new Vector2(at == 0 ? 1f : -1f, 0f), b.knockback); _arena?.React(big ? ArenaReact.Shockwave : b.crit ? ArenaReact.Crack : ArenaReact.Dust, Ground(tp)); }
                    Vector2 kdir = new Vector2(at == 0 ? 1f : -1f, 0f);
                    if (big) { T?.Vibrate(3f, 0.15f); T?.Launch(50f); CamPush(kdir, 48f); Shake(18f); ZoomPunch(0.08f); FlashScreen(0.6f); StartCoroutine(Shockwave(tp, new Color(1f, 0.6f, 0.2f))); if (T != null) StartCoroutine(ImpactFrame(T)); AudioManager.Impact(ult, b.crit, _hudRng.Range(0.9f, 1.1f), Elem(actorSp)); HitStop(0.15f); }
                    else if (b.crit) { T?.Vibrate(2.5f, 0.09f); T?.Launch(40f); MicroImpact(T); CamPush(kdir, 26f); Shake(10f); ZoomPunch(0.03f); StartCoroutine(Shockwave(tp, CCrit)); HitStop(0.09f); }
                }
                yield return new WaitForSecondsRealtime(0.05f / sp);
            }
        }

        // Lerp a fighter's combat offset (ease-out). Realtime — the sim clock keeps
        // advancing during movement so concurrent choreographies overlap (no stall).
        IEnumerator MoveOffset(UnitView u, Vector2 from, Vector2 to, float dur, bool ghost = false, Color ghostTint = default)
        {
            if (u == null) yield break;
            float t = 0f, gAcc = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, dur);
                float e = 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));
                u.combatOffset = Vector2.Lerp(from, to, e);
                if (ghost) { gAcc += Time.deltaTime; if (gAcc >= 0.035f) { gAcc = 0f; SpawnGhost(u, ghostTint); } }
                yield return null;
            }
            u.combatOffset = to;
        }

        // Pooled afterimage: a fading copy of the unit's current sprite, tinted.
        void SpawnGhost(UnitView u, Color tint)
        {
            if (_ghost == null || u == null) return;
            var sp = u.CurrentSprite; if (sp == null) return;
            if (tint.a <= 0f && tint.r <= 0f && tint.g <= 0f && tint.b <= 0f) tint = Color.white;
            var g = _ghost[_ghostCur]; _ghostA[_ghostCur] = 1f; _ghostCur = (_ghostCur + 1) % _ghost.Length;
            g.sprite = sp;
            var rt = g.rectTransform;
            rt.anchoredPosition = u.RenderPos;
            float s = u.RenderScale;
            rt.localScale = new Vector3(u.Mirror * s, s, 1f);
            g.color = new Color(tint.r, tint.g, tint.b, 0.5f);
            g.gameObject.SetActive(true);
        }

        void UpdateGhosts()
        {
            if (_ghost == null) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < _ghost.Length; i++)
            {
                if (_ghostA[i] <= 0f) continue;
                _ghostA[i] -= dt / 0.2f;
                if (_ghostA[i] <= 0f) { _ghost[i].gameObject.SetActive(false); continue; }
                var c = _ghost[i].color; _ghost[i].color = new Color(c.r, c.g, c.b, _ghostA[i] * 0.5f);
            }
        }

        // Anime impact frame (ultimate / finisher only): ~2 frames of high contrast — the
        // victim silhouette flashed white above a near-black darken. finally-restored so an
        // exception can never leave the screen black.
        IEnumerator ImpactFrame(UnitView victim)
        {
            if (_impactOverlay == null || _impactSil == null || victim == null) yield break;
            try
            {
                victim.ImpactSilhouette(0.08f);
                var sp = victim.CurrentSprite;
                if (sp != null)
                {
                    _impactSil.sprite = sp;
                    var rt = _impactSil.rectTransform;
                    rt.anchoredPosition = _stage.anchoredPosition + victim.RenderPos * _zoom;
                    float s = victim.RenderScale * _zoom;
                    rt.localScale = new Vector3(victim.Mirror * s, s, 1f);
                    _impactSil.color = Color.white;
                    _impactSil.gameObject.SetActive(true);
                }
                _impactOverlay.color = new Color(0.02f, 0.02f, 0.04f, 0.85f);
                yield return new WaitForSecondsRealtime(0.05f);
            }
            finally
            {
                _impactOverlay.color = new Color(0.02f, 0.02f, 0.04f, 0f);
                if (_impactSil != null) _impactSil.gameObject.SetActive(false);
            }
        }

        // Ground bounce after a slam: a small parabolic hop + dust puff + squash on landing.
        IEnumerator Bounce(UnitView u, Vector2 dir, float h, float sp)
        {
            if (u == null) yield break;
            Vector2 ground = new Vector2(u.combatOffset.x, 0f);
            yield return MoveOffset(u, u.combatOffset, ground + new Vector2(dir.x * 8f, 150f * h), 0.09f / sp);
            yield return MoveOffset(u, u.combatOffset, ground, 0.09f / sp);
            _vfx.Play("puff", u.BasePos + new Vector2(0, -40), 120f + 90f * h, new Color(1f, 1f, 1f, 0.6f));
            u.Squash(1.25f, 0.78f, 0.07f);
        }

        // ---- Tawuran engagement: every frame push each unit's BasePos toward its live
        // engagement anchor (near its planned opponent, drifting to centre, circling,
        // kiting if ranged), with soft separation so bodies never stack. Because the
        // dash/return coroutines return to BasePos, units stay tangled in the scrum
        // instead of retreating to a formation line. Indexed loops = zero alloc. ----
        void UpdateEngagement()
        {
            if (_plan == null) return;
            float dt = Time.deltaTime;
            bool charging = _stageTime < CHARGE_DUR;
            if (!_chargeClashed && _stageTime >= CHARGE_CONTACT) { _chargeClashed = true; OpeningClash(); }

            if (mode == BattleMode.Arena) { UpdateArena(dt); return; }

            // BRAWL: every living unit fights concurrently from its own spread lane.
            var units = _pb.Units;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (!u.Alive) continue;
                int k = Key(u.team, u.slot);
                if (_busyUnits.Contains(k) || k == _spotlightTargetKey) continue;   // don't disturb the cinematic
                var v = View(u.team, u.slot); if (v == null || v.IsDead) continue;

                Vector2 target = charging ? ChargeAnchor(u.team, u.slot) : EngageAnchor(u.team, u.slot, k);
                float spd = charging ? 1700f : 520f;
                v.SetBasePos(ClampArena(Vector2.MoveTowards(v.BasePos, target, spd * dt)));
            }
            if (!charging) Separate();
        }

        // ARENA: front-living fighter of each team duels centre; reserves benched small/dim
        // at the flanks (boss/showcase presentation). Presentation only.
        void UpdateArena(float dt)
        {
            int a0 = FrontLiving(0), a1 = FrontLiving(1);
            var units = _pb.Units;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (!u.Alive) continue;
                int k = Key(u.team, u.slot);
                var v = View(u.team, u.slot); if (v == null || v.IsDead) continue;
                bool active = k == a0 || k == a1;
                v.SetReserve(!active);                                   // benched = 0.62 scale + dim
                if (_busyUnits.Contains(k) || k == _spotlightTargetKey) continue;   // active duel choreo owns it
                float side = u.team == 0 ? -1f : 1f;
                Vector2 target = active
                    ? new Vector2(side * 208f, -30f)                     // duel centre, own side (wider gap for bigger sprites)
                    : BenchAnchor(u.team, u.slot);                       // flank
                float spd = active ? 560f : 940f;                        // reserves snap back to the bench fast
                v.SetBasePos(ClampArena(Vector2.MoveTowards(v.BasePos, target, spd * dt)));
            }
        }

        int FrontLiving(int team)
        {
            var units = _pb.Units; int best = -1, bestSlot = 999;
            for (int i = 0; i < units.Count; i++)
                if (units[i].team == team && units[i].Alive && units[i].slot < bestSlot) { bestSlot = units[i].slot; best = Key(team, units[i].slot); }
            return best;
        }

        // Benched reserve: parked on the far flank, stacked by slot (team 0 left, team 1 right).
        static Vector2 BenchAnchor(int team, int slot)
        {
            float side = team == 0 ? -1f : 1f;
            return new Vector2(side * 376f, 150f - slot * 150f);   // pulled further in so reserves never clip the screen edge under the tighter zoom (V9)
        }

        static Vector2 ChargeAnchor(int team, int slot)
        {
            float side = team == 0 ? -1f : 1f;
            return new Vector2(side * (150f + slot * 55f), (slot - 1) * 150f);   // meet near centre but spread by slot so the opening clash isn't a single pile (V4)
        }

        Vector2 EngageAnchor(int team, int slot, int k)
        {
            int oppKey = CurrentOpp(k);
            var ov = ViewByKey(oppKey);
            Vector2 oppPos = (ov != null && !ov.IsDead) ? ov.BasePos : new Vector2(0f, -20f);
            float side = team == 0 ? -1f : 1f;
            bool ranged = AttackStyles.IsRanged(StyleOf(team, slot));
            float gap = ranged ? 320f : 165f;
            float phase = _plan.idlePhase.TryGetValue(k, out var ph) ? ph : 0f;
            float t = (float)_clock;

            // REAL TAWURAN: chase and stay glued to YOUR current opponent — no returning to a
            // formation lane. Because each unit's opponent comes from its own engagement segment,
            // the fights fan out across the arena on their own; Separate() stops any pixel-collapse.
            Vector2 a = new Vector2(oppPos.x + side * gap, oppPos.y);   // right up on your target
            a.y += (slot - 1) * 66f;   // per-slot vertical lane so a team fans out instead of piling on one point (V1 brawl spread)
            a.x += Mathf.Cos(t * 1.7f + phase) * 30f + Mathf.Sin(t * 0.9f + phase) * 14f;   // weave, never a statue
            a.y += Mathf.Sin(t * 1.9f + phase) * 22f + Mathf.Cos(t * 0.6f + phase) * 12f;
            // stay on your own side of the target you're fighting (don't run past it), but do NOT
            // snap to a home position — you live where the fight is.
            a.x = team == 0 ? Mathf.Min(a.x, oppPos.x - 40f) : Mathf.Max(a.x, oppPos.x + 40f);
            // Fan the scrum across the arena width: a mild pull toward a per-slot lane on the unit's
            // OWN half, so the brawl spreads left-vs-right instead of collapsing into one corner. The
            // unit still tracks its opponent (dominant term); this only biases where the fights sit.
            float laneX = side * (175f + slot * 145f);
            a.x = Mathf.Lerp(a.x, laneX, 0.24f);
            if (ranged && ov != null && !ov.IsDead && Mathf.Abs(ov.BasePos.x - a.x) < 260f)
                a.x += side * 150f;                            // ranged kite: back off when the enemy closes
            return a;
        }

        int CurrentOpp(int k)
        {
            int opp = -1;
            if (_plan.segments.TryGetValue(k, out var segs))
            {
                for (int i = 0; i < segs.Count; i++) if (_clock >= segs[i].t0 && _clock < segs[i].t1) { opp = segs[i].oppKey; break; }
                if (opp < 0 && segs.Count > 0) opp = segs[segs.Count - 1].oppKey;
            }
            var ov = ViewByKey(opp);
            if (ov == null || ov.IsDead) opp = NearestLivingEnemy(k / 100);   // redirect off a dead partner
            return opp;
        }

        int NearestLivingEnemy(int myTeam)
        {
            var units = _pb.Units;
            for (int i = 0; i < units.Count; i++)
                if (units[i].team != myTeam && units[i].Alive) return Key(units[i].team, units[i].slot);
            return -1;
        }

        UnitView ViewByKey(int key) => key < 0 ? null : View(key / 100, key % 100);

        static Vector2 ClampArena(Vector2 p) =>
            new Vector2(Mathf.Clamp(p.x, -448f, 448f), Mathf.Clamp(p.y, -230f, 205f));   // pulled in so units stay on-screen at the tighter zoom

        // Soft separation so the tangle never collapses onto one pixel (rule 8).
        void Separate()
        {
            const float minD = 162f;   // wider so bigger sprites don't overlap
            var units = _pb.Units;
            for (int i = 0; i < units.Count; i++)
            {
                if (!units[i].Alive) continue; int ki = Key(units[i].team, units[i].slot);
                if (_busyUnits.Contains(ki) || ki == _spotlightTargetKey) continue;
                var vi = View(units[i].team, units[i].slot); if (vi == null || vi.IsDead) continue;
                for (int j = i + 1; j < units.Count; j++)
                {
                    if (!units[j].Alive) continue; int kj = Key(units[j].team, units[j].slot);
                    if (_busyUnits.Contains(kj) || kj == _spotlightTargetKey) continue;
                    var vj = View(units[j].team, units[j].slot); if (vj == null || vj.IsDead) continue;
                    Vector2 d = vi.BasePos - vj.BasePos; float dist = d.magnitude;
                    if (dist < minD && dist > 0.01f)
                    {
                        Vector2 push = d / dist * ((minD - dist) * 0.5f);
                        vi.SetBasePos(ClampArena(vi.BasePos + push));
                        vj.SetBasePos(ClampArena(vj.BasePos - push));
                    }
                }
            }
        }

        void OpeningClash()
        {
            _vfx.Play("hit_big", new Vector2(0, -10), 300f, Color.white);
            _vfx.Play("hit_impact", new Vector2(-70, 60), 200f, Color.white);
            _vfx.Play("hit_impact", new Vector2(80, -80), 200f, Color.white);
            StartCoroutine(Shockwave(new Vector2(0, -10), CCrit));
            Shake(16f); FlashScreen(0.5f); ZoomPunch(0.05f); AudioManager.Play(Sfx.Crit);
            _arena?.React(ArenaReact.Shockwave, new Vector2(0, -110));   // the two charges collide
            Splash("FIGHT!", CCrit, 84f, 0.9f, 5f);   // synced to the opening-charge collision
            AudioManager.Announce(Sfx.VoFight);
        }

        // Non-damaging filler beat (whiff / block / shove). NEVER spawns a damage number,
        // never moves HP, never white-flashes a hit — so real hits stay unmistakable.
        void PlayFiller(FillerBeat fb)
        {
            var v = ViewByKey(fb.unitKey); if (v == null || v.IsDead) return;
            int k = fb.unitKey;
            if (_busyUnits.Contains(k) || k == _spotlightTargetKey) return;   // don't interrupt a cinematic
            var ov = ViewByKey(fb.oppKey);
            int at = k / 100;
            Vector2 dir = new Vector2(at == 0 ? 1f : -1f, 0f);
            switch (fb.kind)
            {
                case FillerKind.Whiff:
                    v.PlayAttack(dir, 32f, false);
                    _vfx.Play("speedlines", v.BasePos + dir * 40f, 110f, new Color(1f, 1f, 1f, 0.28f));
                    AudioManager.Play(Sfx.Hover);
                    break;
                case FillerKind.Block:
                    v.PlayAttack(dir, 24f, false);
                    if (ov != null && !ov.IsDead)
                    {
                        Vector2 mid = Vector2.Lerp(v.BasePos, ov.BasePos, 0.5f);
                        _vfx.Play("hit_small", mid, 110f, new Color(0.75f, 0.9f, 1f, 0.9f));   // blue block spark (distinct)
                        ov.Knock(dir, 14f); v.Knock(-dir, 10f);                                 // tiny mutual pushback
                    }
                    AudioManager.Play(Sfx.Hover);
                    break;
                case FillerKind.Shove:
                    v.PlayAttack(dir, 28f, false);
                    if (ov != null && !ov.IsDead) { _vfx.Play("puff", ov.BasePos, 130f, new Color(1f, 1f, 1f, 0.7f)); ov.Knock(dir, 34f); }
                    AudioManager.Play(Sfx.Hover);
                    break;
            }
        }

        // Clash: two units' real events target each other near-simultaneously → lunge, big
        // spark + mini shockwave, mutual push-apart (damage still comes from the real events).
        void ClashFlourish(int at, int as_, int tt, int ts, Vector2 dir)
        {
            var A = View(at, as_); var T = View(tt, ts);
            if (A == null || T == null) return;
            Vector2 mid = Vector2.Lerp(A.BasePos, T.BasePos, 0.5f);
            A.Knock(dir, 55f); T.Knock(-dir, 55f);
            _vfx.Play("hit_big", mid, 250f, Color.white);
            StartCoroutine(Shockwave(mid, CCrit));
            Shake(12f); FlashScreen(0.4f); ZoomPunch(0.04f); AudioManager.Play(Sfx.Crit);
            _texts.Spawn(mid + new Vector2(0, 46), "CLASH", CCrit, 30);
        }

        // Nearby non-participants recoil from an ultimate/KO blast (rule 9).
        void CrowdFlinch(int excludeKey, int tTeam, int tSlot, Vector2 blast, float radius)
        {
            int tKey = tTeam >= 0 ? Key(tTeam, tSlot) : -1;
            var units = _pb.Units;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (!u.Alive) continue;
                int k = Key(u.team, u.slot);
                if (k == excludeKey || k == tKey || _busyUnits.Contains(k) || k == _spotlightTargetKey) continue;
                var v = View(u.team, u.slot); if (v == null || v.IsDead) continue;
                Vector2 d = v.BasePos - blast; float dist = d.magnitude;
                if (dist < radius && dist > 0.01f) v.Knock(d / dist, Mathf.Lerp(70f, 18f, dist / radius));
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
                case ChoreoCam.ZoomCombo: _zoomTarget = _baseZoom + 0.06f; break;
                case ChoreoCam.ShakeCrit: _zoomTarget = _baseZoom + 0.08f; Shake(12f); ZoomPunch(0.05f); FlashScreen(0.5f); break;
                case ChoreoCam.CinematicZoom: _zoomTarget = _baseZoom + 0.15f; ZoomPunch(0.1f); Shake(16f); FlashScreen(0.8f); break;
                case ChoreoCam.SlowMoFinisher: _zoomTarget = _baseZoom + 0.24f; Shake(22f); StartSlowMo(); FlashScreen(0.6f); _letterTarget = 1f; _finDarkTarget = 1f; break;
                case ChoreoCam.ZoomWinner: _zoomTarget = _baseZoom + 0.12f; break;
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

        static Color ProjColor(AttackStyle s) => s == AttackStyle.MageCast ? new Color(0.6f, 0.5f, 1f) : new Color(1f, 0.9f, 0.4f);

        // Element of a species (for element-signature VFX routing), "" when unknown.
        string Elem(string sp) => (sp != null && elementNames != null && elementNames.TryGetValue(sp, out var e)) ? e : "";

        // Element matchup (matches StatMath.ElementMultiplier's triangle): +1 advantage, -1 disadvantage.
        static int ElementAdv(string a, string d)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(d) || a == d) return 0;
            if ((a == "Fire" && d == "Nature") || (a == "Nature" && d == "Water") || (a == "Water" && d == "Fire")) return 1;
            if ((d == "Fire" && a == "Nature") || (d == "Nature" && a == "Water") || (d == "Water" && a == "Fire")) return -1;
            return 0;
        }
        // Role → animation heft. Tanks plod, assassins/mages dart. Presentation only.
        static float RoleWeight(string role)
        {
            switch (role)
            {
                case "Tank": return 1.4f;
                case "Bruiser": return 1.12f;
                case "Support": return 1.0f;
                case "Mage": return 0.85f;
                case "Assassin": return 0.78f;
                default: return 1.0f;
            }
        }
        // Element-shaped projectile head, so ranged shots read as their element.
        static Sprite ElemSprite(string element)
        {
            switch (element)
            {
                case "Fire": return ProceduralArt.Flame();
                case "Water": return ProceduralArt.Droplet();
                case "Nature": return ProceduralArt.Leaf();
                case "Lightning": return ProceduralArt.Bolt();
                default: return ProceduralArt.Glow();
            }
        }
        // Element tint for projectiles / trails.
        static Color ElemColor(string element)
        {
            switch (element)
            {
                case "Fire": return new Color(1f, 0.55f, 0.18f);
                case "Water": return new Color(0.5f, 0.8f, 1f);
                case "Nature": return new Color(0.5f, 0.9f, 0.4f);
                case "Lightning": return new Color(1f, 1f, 0.6f);
                default: return new Color(1f, 0.9f, 0.5f);
            }
        }
        static Vector2 Jitter() => new Vector2(UnityEngine.Random.Range(-24f, 24f), 40f);
        static Vector2 ComboJit(int i) => new Vector2(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-30f, 40f));

        void FlashScreen(float amt) => _flashT = Mathf.Max(_flashT, amt);

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
            if (Time.time - _lastShockwaveT < 0.16f) yield break;   // don't stack concentric rings at a multi-hit clash (V4)
            _lastShockwaveT = Time.time;
            var go = new GameObject("Shock", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(_stage, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(60, 60);
            var img = go.GetComponent<Image>(); img.sprite = ProceduralArt.Ring(); img.color = col; img.raycastTarget = false;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / 0.35f; float e = Mathf.Clamp01(t);
                rt.sizeDelta = Vector2.one * Mathf.Lerp(60f, 360f, e);
                img.color = new Color(col.r, col.g, col.b, (1f - e) * 0.55f);   // thinner ring — less over-layer noise at the climax
                yield return null;
            }
            Destroy(go);
        }

        // Capped global freeze — only heavy/crit/ult/KO request it, and only while budget remains.
        void HitStop(float d)
        {
            if (_freezeBudget <= 0f) return;
            d = Mathf.Min(d, _freezeBudget);
            _freezeBudget -= d;
            _hitstop = Mathf.Max(_hitstop, d);
        }
        // Camera aggressiveness by mode: Arena may punch; Brawl stays calm (6 units active,
        // over-shake/over-zoom destroys readability — the brief's readability>spectacle rule).
        float CamAggr => mode == BattleMode.Arena ? 1f : 0.5f;
        void Shake(float mag) { mag *= CamAggr; float dur = 0.18f + mag * 0.006f; if (dur > _shakeT) { _shakeT = dur; _shakeDur = dur; } _shakeMag = Mathf.Max(_shakeMag, mag); }
        void ZoomPunch(float amt) { _zoom = Mathf.Min(_zoom + amt * CamAggr, _baseZoom + (mode == BattleMode.Arena ? 0.32f : 0.16f)); }
        void CamPush(Vector2 dir, float amt) { if (dir.sqrMagnitude > 0.0001f) _camPush += ((Vector2)dir).normalized * amt * CamAggr; }
        // Lighter impact frame for crits — quick victim silhouette + screen pop, no full darken.
        void MicroImpact(UnitView v) { if (v == null) return; v.ImpactSilhouette(0.05f); FlashScreen(0.35f); }

        // ---- Phase P HUD ----
        // One global combo, fed by the same offensive-event set as Combo King (enemy-
        // targeted Attack/Skill/Ultimate). A gap > COMBO_GAP resets it; it fades on a lull.
        void RegisterCombo()
        {
            if (_clock - _comboLastT > COMBO_GAP) _combo = 0;
            _comboLastT = _clock; _combo++;
            _comboScale = 1.5f; _comboAlpha = 1f;
            if (_comboLabel != null) { _comboLabel.text = _combo + " HITS!"; _comboLabel.color = ComboColor(_combo); }
        }

        static Color ComboColor(int n) =>
            n >= 15 ? new Color(1f, 0.35f, 0.3f) : n >= 10 ? new Color(1f, 0.55f, 0.2f) : n >= 5 ? new Color(1f, 0.85f, 0.3f) : new Color(0.95f, 0.95f, 1f);

        // Damage number at the victim with seeded scatter (never stacks). Light hits are
        // small + short-lived; crits/ults big + long + shake, so real crits dominate.
        void DamageNumber(Vector2 at, int amount, bool crit, bool ult)
        {
            Vector2 scatter = new Vector2(_hudRng.Range(-26f, 26f), _hudRng.Range(-8f, 34f));
            int size = ult ? 52 : crit ? 46 : 30;
            float life = ult ? 0.95f : crit ? 0.85f : 0.55f;
            Color col = ult ? COrange : crit ? CCrit : CWhite;
            _texts.Spawn(at + scatter, amount.ToString(), col, size, life, crit || ult);
        }

        void Splash(string text, Color color, float size, float dur, float shake)
        {
            if (_splash == null) return;
            _splash.text = text; _splash.fontSize = (int)size; _splashColor = color;
            _splashDur = Mathf.Max(0.3f, dur); _splashT = _splashDur; _splashShake = shake;
        }

        void HudTick()
        {
            float dt = Time.deltaTime;
            if (_comboLabel != null)
            {
                _comboScale = Mathf.Lerp(_comboScale, 1f, 12f * dt);
                _comboAlpha = Mathf.MoveTowards(_comboAlpha, 0f, dt / 1.1f);
                bool show = _combo >= 2 && _comboAlpha > 0f;
                var c = _comboLabel.color; c.a = show ? _comboAlpha : 0f; _comboLabel.color = c;
                _comboLabel.rectTransform.localScale = Vector3.one * _comboScale;
            }
            if (_splash != null && _splashT > 0f)
            {
                _splashT -= dt;
                float shown = _splashDur - _splashT;
                float scale = Mathf.Lerp(1.7f, 1f, Mathf.Clamp01(shown / 0.22f));
                float shk = _splashShake * Mathf.Sin(Time.time * 55f) * Mathf.Clamp01(1f - shown / 0.3f);
                _splash.rectTransform.localScale = Vector3.one * scale;
                _splash.rectTransform.anchoredPosition = new Vector2(shk, 150f);
                var c = _splashColor; c.a = _splashT < 0.3f ? Mathf.Clamp01(_splashT / 0.3f) : 1f; _splash.color = c;
            }
            else if (_splash != null && _splash.color.a != 0f) { var c = _splash.color; c.a = 0f; _splash.color = c; }
            _letterCur = Mathf.MoveTowards(_letterCur, _letterTarget, dt / 0.25f);
            if (_letterTop != null) _letterTop.anchoredPosition = new Vector2(0, (1f - _letterCur) * 140f);
            if (_letterBot != null) _letterBot.anchoredPosition = new Vector2(0, -(1f - _letterCur) * 140f);
            _finDarkCur = Mathf.MoveTowards(_finDarkCur, _finDarkTarget, dt / 0.3f);
            if (_finDark != null) _finDark.color = new Color(0, 0, 0, _finDarkCur * 0.5f);
        }

        void UpdateCamera()
        {
            if (_stage == null) return;
            UpdateGhosts();
            HudTick();
            _camPush = Vector2.Lerp(_camPush, Vector2.zero, 6f * Time.deltaTime);   // directional push eases back
            Vector2 shake = Vector2.zero;
            if (_shakeT > 0f)
            {
                _shakeT -= Time.deltaTime;
                float m = _shakeMag * Mathf.Clamp01(_shakeT / _shakeDur);
                shake = new Vector2(UnityEngine.Random.Range(-m, m), UnityEngine.Random.Range(-m, m));
                if (_shakeT <= 0f) _shakeMag = 0f;
            }
            // Cinematic pan (P2): during a spotlight/ult the camera drifts toward the clash, then eases
            // back to a wide, calm centre. Arena leans in more than the readability-first Brawl.
            if (!_spotlightBusy) _camFocusTarget = Vector2.Lerp(_camFocusTarget, Vector2.zero, 4f * Time.deltaTime);
            _camFocus = Vector2.Lerp(_camFocus, _camFocusTarget, 3.5f * Time.deltaTime);
            float panAmt = mode == BattleMode.Arena ? 0.26f : 0.10f;
            Vector2 pan = new Vector2(-_camFocus.x * panAmt, -_camFocus.y * panAmt * 0.5f);
            Vector2 camOffset = _camPush + shake + pan;
            _stage.anchoredPosition = camOffset + new Vector2(0f, -175f);   // frame the action LOWER-CENTRE (V9) + cinematic pan (P2)

            // Rest camera = the roster-framed base zoom; tighten toward the climax and after the win.
            float restZoom = _baseZoom;
            if (_finishedFired) restZoom = _baseZoom + 0.12f;
            else if (_pb.AliveCount(0) + _pb.AliveCount(1) <= 2) restZoom = _baseZoom + 0.14f;   // last-standing close-up
            _zoomTarget = Mathf.Lerp(_zoomTarget, restZoom, 1.6f * Time.deltaTime);
            float zoomCap = _baseZoom + (mode == BattleMode.Arena ? 0.30f : 0.16f);   // punches ride above base
            _zoom = Mathf.Clamp(Mathf.Lerp(_zoom, _zoomTarget, 5.5f * Time.deltaTime), 0.9f, zoomCap);
            _stage.localScale = Vector3.one * _zoom;

            _arena?.SetParallax(camOffset);
            _arena?.Tick(Time.deltaTime);
            _elem?.Tick(Time.deltaTime);

            if (_screenFlash != null)
            {
                _flashT = Mathf.Max(0f, _flashT - Time.deltaTime * 3.5f);
                _screenFlash.color = new Color(1f, 1f, 1f, _flashT * 0.55f);
            }
        }
    }
}
