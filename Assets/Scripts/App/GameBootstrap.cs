using System.Collections.Generic;
using MTA.Battle;
using MTA.Core;
using MTA.Data;
using MTA.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.App
{
    // Single entry point for the first playable. Builds all UI in code, owns the
    // GameController, and switches panels by phase. One GameObject, no wiring.
    public class GameBootstrap : MonoBehaviour
    {
        GameController _ctrl;
        SpeciesRegistry _reg;
        Font _font;

        RectTransform _menu, _select, _battle, _result, _progress, _collection, _collContent, _popup, _popupCard;
        Text _selectCount, _resultBanner, _resultStats, _progressText, _rewardText, _collHeader, _popupText, _popupTitle;
        RectTransform _mvpHolder;
        int _collFilter = -1;        // -1 = all, else (int)RoleTag
        bool _collSortRarity = true;
        RectTransform _detail;
        Text _detailName, _detailStats, _detailXp;
        Image _detailXpFill;
        RectTransform _detailArt, _detailStars;
        Button _evolveBtn;
        string _detailSpecies;
        RectTransform _career, _careerContent;
        Text _careerHeader;
        Button _resultContinueBtn;
        List<CareerStage> _stages;
        RectTransform _daily;
        Text _dailyInfo, _dailyHistory;
        Button _dailyClaimBtn;
        RectTransform _settings, _about, _loading;
        Text _settingsInfo;
        // Phases T–X
        RectTransform _onboarding, _quests, _questContent, _achievements, _achContent, _dex, _dexContent, _dexDetail;
        Text _questHeader, _achHeader, _dexHeader;
        Button _menuQuestBtn;
        int _dexSel;
        int _onbPage;
        BalanceConfig _cfg;
        SaveData _profile;
        List<string> _roster, _obtainable;
        List<string> _lastTeam = new List<string>();
        bool _hadSave;
        Button _startBtn, _muteBtn, _modeBtn;
        BattleReplayView _view;
        Dictionary<string, SkillSlot> _slotMap;
        Dictionary<string, AttackStyle> _atkStyles;
        Dictionary<string, Color> _elemColors = new Dictionary<string, Color>();
        Dictionary<string, string> _elemNames = new Dictionary<string, string>();
        Dictionary<string, string> _roleNames = new Dictionary<string, string>();
        Dictionary<string, string> _displayNames = new Dictionary<string, string>();
        Dictionary<string, int> _rarities = new Dictionary<string, int>();
        readonly Dictionary<string, Button> _speciesButtons = new Dictionary<string, Button>();
        readonly List<Button> _speedButtons = new List<Button>();

        void Start()
        {
            _font = UIFactory.DefaultFont();
            _reg = SpeciesDatabase.LoadFromResources();
            var cfg = SpeciesDatabase.LoadBalance();
            _cfg = cfg;

            var pool = new List<string>();
            foreach (var s in _reg.All) pool.Add(s.speciesId);
            pool.Sort(System.StringComparer.Ordinal);
            _roster = pool;                                  // full dex (collection)
            // Obtainable = wild pool: excludes evolution-only forms (earned, not rolled).
            _obtainable = new List<string>();
            foreach (var id in pool) if (!_reg.Get(id).evolutionOnly) _obtainable.Add(id);
            _stages = Career.Build(_obtainable);
            _ctrl = new GameController(_reg, cfg, _obtainable, seedBase: 20260817);
            _slotMap = ReplayBuilder.SlotMap(_reg.All);   // skillId -> slot, for replay classification
            _atkStyles = AttackStyles.Map(_reg.All);      // species -> attack style (presentation)
            foreach (var s in _reg.All)
            {
                _elemColors[s.speciesId] = UIFactory.ElementColor(s.element);
                _elemNames[s.speciesId] = s.element;
                _roleNames[s.speciesId] = MonsterMeta.Role(s).ToString();
                _displayNames[s.speciesId] = !string.IsNullOrEmpty(s.displayName) ? s.displayName : Nice(s.speciesId);
                _rarities[s.speciesId] = MonsterMeta.Rarity(s);
            }

            // Meta progression: load or create the player profile.
            _hadSave = SaveSystem.Exists();
            _profile = SaveSystem.Load() ?? Progression.NewGame(_obtainable);
            if (!_hadSave) SaveSystem.Save(_profile);

            // The code-first scene has no Camera, so it also has no AudioListener — and with
            // no AudioListener Unity plays NOTHING (music, sfx, ui all silent). Guarantee one.
            if (FindObjectOfType<AudioListener>() == null)
                new GameObject("AudioListener", typeof(AudioListener));

            MTA.Battle.AudioManager.Ensure();                       // audio feedback
            MTA.Battle.AudioManager.Muted = _profile.muted;
            MTA.Battle.AudioManager.PlayMusic(MTA.Battle.Music.Menu);
            ApplyDisplaySettings();                                 // fps + quality from save

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var canvas = UIFactory.Canvas("GameCanvas");
            BuildMenu(canvas.transform);
            BuildSelect(canvas.transform, pool);
            BuildBattle(canvas.transform);
            BuildResult(canvas.transform);
            BuildProgress(canvas.transform);
            BuildCollection(canvas.transform);
            BuildDetail(canvas.transform);
            BuildCareer(canvas.transform);
            BuildDaily(canvas.transform);
            BuildSettings(canvas.transform);
            BuildAbout(canvas.transform);
            BuildOnboarding(canvas.transform);
            BuildQuests(canvas.transform);
            BuildAchievements(canvas.transform);
            BuildDex(canvas.transform);
            BuildPopup(canvas.transform);
            BuildLoading(canvas.transform);

            // Cohesion with the battle look: give every meta screen an opaque PAINTED backdrop
            // (behind its own content, so labels/buttons stay fully bright on top). Replaces the
            // flat solid-colour screen fills. The battle panel is left alone (the arena covers it).
            foreach (var scr in new[] { _menu, _select, _result, _progress, _collection, _detail, _career, _daily, _settings, _about, _onboarding, _quests, _achievements, _dex })
                PaintScreen(scr);

            _ctrl.Flow.OnPhaseChanged += OnPhase;
            _view.OnFinished += _ => { if (!ShowcaseActive()) _ctrl.OnBattleFinished(); };   // showcase is READ-ONLY: never trigger the result/reward/save flow
            Quests.SyncDay(_profile, DailyRewards.DayIndex(System.DateTime.Now));
            OnPhase(_ctrl.Flow.Phase);
            // Visual-review harness: `-showcase` on the command line auto-plays a fixed set of
            // deterministic battles (no menus, no input) so the combat can be captured by an
            // external screenshotter. Dev tooling only — never reached in normal play.
            if (ShowcaseActive()) { StartCoroutine(ShowcaseRoutine()); StartCoroutine(HideLoading()); return; }
            if (UiShowcaseActive()) { StartCoroutine(UiShowcaseRoutine()); StartCoroutine(HideLoading()); return; }

            // First launch: run onboarding. Otherwise greet with the daily reward if claimable.
            if (!_profile.onboarded) _ctrl.ToOnboarding();
            else if (DailyRewards.CanClaim(_profile, System.DateTime.Now)) _ctrl.ToDaily();
            StartCoroutine(HideLoading());   // brief branded loading screen
        }

        // Give a screen an opaque painted backdrop behind its content: the screen's own flat fill is
        // made transparent and a full-stretch painted RawImage is inserted at the back of that screen.
        static void PaintScreen(RectTransform screen)
        {
            if (screen == null) return;
            var img = screen.GetComponent<Image>();
            if (img != null) img.color = new Color(0, 0, 0, 0);
            var bg = new GameObject("ScreenBg", typeof(RectTransform), typeof(RawImage));
            var rt = bg.GetComponent<RectTransform>(); rt.SetParent(screen, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var ri = bg.GetComponent<RawImage>(); ri.texture = MTA.Battle.PaintedBackdrop.Menu(); ri.raycastTarget = false;
            rt.SetAsFirstSibling();   // behind the screen's own labels/buttons
        }

        void OnPhase(GamePhase p)
        {
            _menu.gameObject.SetActive(p == GamePhase.MainMenu);
            _select.gameObject.SetActive(p == GamePhase.TeamSelect);
            _battle.gameObject.SetActive(p == GamePhase.Battle);
            _result.gameObject.SetActive(p == GamePhase.Result);
            _progress.gameObject.SetActive(p == GamePhase.Progress);
            _collection.gameObject.SetActive(p == GamePhase.Collection);
            _detail.gameObject.SetActive(p == GamePhase.Detail);
            _career.gameObject.SetActive(p == GamePhase.Career);
            _daily.gameObject.SetActive(p == GamePhase.Daily);
            _settings.gameObject.SetActive(p == GamePhase.Settings);
            _about.gameObject.SetActive(p == GamePhase.About);
            _onboarding.gameObject.SetActive(p == GamePhase.Onboarding);
            _quests.gameObject.SetActive(p == GamePhase.Quests);
            _achievements.gameObject.SetActive(p == GamePhase.Achievements);
            _dex.gameObject.SetActive(p == GamePhase.Dex);
            if (p == GamePhase.Onboarding) RefreshOnboarding();
            if (p == GamePhase.Quests) RefreshQuests();
            if (p == GamePhase.Achievements) RefreshAchievements();
            if (p == GamePhase.Dex) RefreshDex();
            if (p == GamePhase.MainMenu) RefreshMenuBadges();
            if (p == GamePhase.Career) RefreshCareer();
            if (p == GamePhase.Daily) RefreshDaily();
            if (p == GamePhase.Settings) RefreshSettings();
            if (p == GamePhase.Detail) RefreshDetail();
            if (p == GamePhase.TeamSelect) RefreshSelect();
            if (p == GamePhase.Result) ShowResult();
            if (p == GamePhase.Progress) RefreshProgress();
            if (p == GamePhase.Collection) RefreshCollection();

            // Music: menu theme everywhere except battle (view sets Battle) and result (ShowResult sets sting).
            if (p != GamePhase.Battle && p != GamePhase.Result)
                MTA.Battle.AudioManager.PlayMusic(MTA.Battle.Music.Menu);

            AnimatePanel(PanelFor(p));   // page transition
        }

        RectTransform PanelFor(GamePhase p)
        {
            switch (p)
            {
                case GamePhase.MainMenu: return _menu;
                case GamePhase.TeamSelect: return _select;
                case GamePhase.Battle: return _battle;
                case GamePhase.Result: return _result;
                case GamePhase.Progress: return _progress;
                case GamePhase.Collection: return _collection;
                case GamePhase.Detail: return _detail;
                case GamePhase.Career: return _career;
                case GamePhase.Daily: return _daily;
                case GamePhase.Settings: return _settings;
                case GamePhase.About: return _about;
                case GamePhase.Onboarding: return _onboarding;
                case GamePhase.Quests: return _quests;
                case GamePhase.Achievements: return _achievements;
                case GamePhase.Dex: return _dex;
                default: return null;
            }
        }

        // Combo King: the fighter that landed the most damaging hits.
        string ComboKing(BattleResult r)
        {
            if (r == null) return "-";
            var sp = new Dictionary<string, string>(); var cnt = new Dictionary<string, int>();
            foreach (var e in r.events)
            {
                if (e.kind == "Spawn") sp[e.actorTeam + "_" + e.actorSlot] = e.extra;
                else if (e.kind == "Action" && e.actorTeam != e.targetTeam)
                { string k = e.actorTeam + "_" + e.actorSlot; cnt.TryGetValue(k, out var c); cnt[k] = c + 1; }
            }
            int best = 0; string bk = "-";
            foreach (var kv in cnt) if (kv.Value > best) { best = kv.Value; sp.TryGetValue(kv.Key, out var s); bk = Nice(s ?? "?") + "  (" + best + " hits)"; }
            return bk;
        }

        System.Collections.IEnumerator Punch(RectTransform rt)
        {
            if (rt == null) yield break;
            float t = 0f;
            while (t < 1f) { t += Time.unscaledDeltaTime / 0.35f; rt.localScale = Vector3.one * (1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.18f); yield return null; }
            rt.localScale = Vector3.one;
        }

        // Page transition: pop + fade the incoming panel in.
        void AnimatePanel(RectTransform panel)
        {
            if (panel == null) return;
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(PanelIn(panel, cg));
        }

        System.Collections.IEnumerator PanelIn(RectTransform panel, CanvasGroup cg)
        {
            float t = 0f;
            while (t < 1f && panel != null && panel.gameObject.activeInHierarchy)
            {
                t += Time.unscaledDeltaTime / 0.22f;
                float e = 1f - (1f - Mathf.Clamp01(t)) * (1f - Mathf.Clamp01(t));   // ease-out
                cg.alpha = e;
                panel.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, e);
                yield return null;
            }
            if (panel != null) { cg.alpha = 1f; panel.localScale = Vector3.one; }
        }

        void ShowResult()
        {
            bool won = _ctrl.PlayerWon;
            var r = _ctrl.Session.lastResult;
            var d = r != null ? BattleDrama.Compute(r) : null;
            MTA.Battle.AudioManager.PlayMusic(won ? MTA.Battle.Music.Victory : MTA.Battle.Music.Defeat);
            if (!won) MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.Defeat);

            // bannerTitle is phrased from the WINNER's side ("Clutch Victory"), so on a loss it
            // must not be shown verbatim (that read as "DEFEAT / Clutch Victory"). Use a loss line.
            string flavor = d == null ? "" : (won ? d.bannerTitle : LossFlavor(d.tier));
            _resultBanner.text = (won ? "VICTORY" : "DEFEAT") + (flavor != "" ? "\n" + flavor : "");
            _resultBanner.color = won ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.5f);
            _resultStats.text = d == null ? "" :
                "Winner: " + (d.winnerTeam == 0 ? "You" : "Enemy") + "\n" +
                "Battle Duration: " + d.duration.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " s\n" +
                "Survivors: " + d.winnerAlive + " vs " + d.loserAlive + "\n\n" +
                "Damage Leader: " + d.damageLeader + "\n" +
                "Combo King: " + ComboKing(r) + "\n" +
                "Healing Leader: " + d.healingLeader;

            // MVP showcase — procedural portrait + label.
            if (_mvpHolder != null)
            {
                for (int i = _mvpHolder.childCount - 1; i >= 0; i--) Destroy(_mvpHolder.GetChild(i).gameObject);
                if (d != null && !string.IsNullOrEmpty(d.mvpSpecies))
                {
                    var msp = _reg.Get(d.mvpSpecies);
                    if (msp != null)
                    {
                        var art = Portrait(_mvpHolder, d.mvpSpecies, msp, 130);
                        art.anchoredPosition = new Vector2(-300, 0);
                    }
                    UIFactory.Label(_mvpHolder, "MVP:  " + Nice(d.mvpSpecies) + "  (" + (d.mvpTeam == 0 ? "You" : "Enemy") + ")",
                        34, new Vector2(40, 0), new Vector2(620, 130), _font);
                }
            }
            StartCoroutine(Punch(_resultBanner.rectTransform));

            // Encyclopedia: mark the enemies we just fought as seen.
            foreach (var e in _ctrl.Session.enemyTeam) _profile.MarkSeen(e);
            // Meta progression: award XP/coins/levels/unlocks for this battle.
            var rw = Progression.ApplyBattle(_profile, _lastTeam, won, _obtainable);

            // Quests / achievements / retention counters.
            Quests.SyncDay(_profile, DailyRewards.DayIndex(System.DateTime.Now));
            _profile.dailyBattles++;
            if (won) { _profile.dailyWins++; _profile.winStreak++; if (_profile.winStreak > _profile.bestWinStreak) _profile.bestWinStreak = _profile.winStreak; }
            else _profile.winStreak = 0;
            int combo = BestComboCount(r);
            if (combo > _profile.bestCombo) _profile.bestCombo = combo;

            string rt = "Rewards:  +" + rw.playerXp + " XP    +" + rw.coins + " coins";
            if (rw.playerLevelsGained > 0) rt += "    PLAYER LEVEL UP!";
            if (rw.leveledUp.Count > 0) rt += "\nLeveled up: " + NiceLeveled(rw.leveledUp);
            if (rw.newlyUnlocked.Count > 0) rt += "\nUnlocked: " + NiceList(rw.newlyUnlocked);

            // Career: record a stage clear (first clear pays the reward once).
            int stageIdx = _ctrl.Session.careerStageIndex;
            bool career = stageIdx >= 0;
            if (_resultContinueBtn != null) _resultContinueBtn.gameObject.SetActive(career);
            if (career && won)
            {
                long bonus = Career.ClearStage(_profile, stageIdx, _stages[stageIdx].reward);
                if (bonus > 0)
                {
                    rt += "\nSTAGE CLEARED!  +" + bonus + " coins   (" + Career.CompletionPercent(_profile) + "%)";
                    if (Career.IsComplete(_profile)) rt += "\nCAREER COMPLETE!";
                }
            }
            _profile.leaguesCompleted = _profile.careerStage / Career.PerLeague;

            // Persist once (coalesced), then surface any freshly-earned achievements.
            SaveSystem.Save(_profile);
            var newAch = Achievements.CheckNew(_profile, _roster.Count);
            if (newAch.Count > 0) { SaveSystem.Save(_profile); foreach (var a in newAch) rt += "\n★ Achievement: " + a.title; }

            _rewardText.text = rt;
            if (won) MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.Reward);
            if (rw.playerLevelsGained > 0 || rw.leveledUp.Count > 0) MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.LevelUp);
            if (rw.newlyUnlocked.Count > 0) ShowNewMonster(rw.newlyUnlocked);   // new-monster popup
        }

        static Color SpColor(string id) { var c = SpeciesIdentity.ColorFor(id); return new Color(c.r, c.g, c.b); }

        string MuteLabel() => "SOUND: " + (_profile.muted ? "OFF" : "ON");

        void ToggleMute()
        {
            _profile.muted = !_profile.muted;
            MTA.Battle.AudioManager.Muted = _profile.muted;
            SaveSystem.Save(_profile);
            if (_muteBtn != null) { var t = _muteBtn.GetComponentInChildren<Text>(); if (t != null) t.text = MuteLabel(); }
        }

        void DecorateCard(Button b, string id)
        {
            var rt = (RectTransform)b.transform;
            var col = SpColor(id);
            var strip = new GameObject("Strip", typeof(RectTransform), typeof(Image));
            var srt = strip.GetComponent<RectTransform>(); srt.SetParent(rt, false);
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(0, 1); srt.pivot = new Vector2(0, 0.5f);
            srt.sizeDelta = new Vector2(16, 0); srt.anchoredPosition = Vector2.zero;
            var si = strip.GetComponent<Image>(); si.color = col; si.raycastTarget = false;
            // Real monster portrait on the card (procedural/initial fallback).
            var sprite = MTA.Battle.MonsterVisual.For(id, false);
            if (sprite != null)
            {
                var pgo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                var prt = pgo.GetComponent<RectTransform>(); prt.SetParent(rt, false);
                prt.anchorMin = prt.anchorMax = new Vector2(0, 0.5f); prt.pivot = new Vector2(0, 0.5f);
                prt.sizeDelta = new Vector2(88, 88); prt.anchoredPosition = new Vector2(20, 0);
                var pi = pgo.GetComponent<Image>(); pi.sprite = sprite; pi.preserveAspect = true; pi.raycastTarget = false;
            }
            else IconBadge(rt, id, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), 72, 30);

            // Element badge (top-right) + Lv (bottom-right) so the card shows type + level at a glance.
            var spd = _reg != null ? _reg.Get(id) : null;
            string elem = spd != null ? spd.element : "";
            UIFactory.ElementBadge(rt, elem, new Vector2(126, 30), 34, _font);
            int lvl = 0;
            if (_profile != null && _profile.collection != null)
                foreach (var ms in _profile.collection) if (ms.speciesId == id) { lvl = ms.level; break; }
            if (lvl > 0)
            {
                var lvTxt = UIFactory.Label(rt, "Lv" + lvl, 22, new Vector2(120, -30), new Vector2(90, 30), _font);
                lvTxt.color = new Color(1f, 0.88f, 0.5f); lvTxt.raycastTarget = false;
            }
        }

        void IconBadge(RectTransform parent, string id, Vector2 amin, Vector2 amax, Vector2 pos, float sz, int fs)
        {
            var col = SpColor(id);
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var irt = icon.GetComponent<RectTransform>(); irt.SetParent(parent, false);
            irt.anchorMin = amin; irt.anchorMax = amax; irt.pivot = new Vector2(0, 0.5f);
            irt.sizeDelta = new Vector2(sz, sz); irt.anchoredPosition = pos;
            var ii = icon.GetComponent<Image>(); ii.color = new Color(col.r * 0.6f, col.g * 0.6f, col.b * 0.6f, 0.95f); ii.raycastTarget = false;
            var lbl = UIFactory.Label(irt, SpeciesIdentity.Initial(id), fs, Vector2.zero, new Vector2(sz, sz), _font);
            lbl.raycastTarget = false;
        }

        // User-facing name for a species id: its displayName, never raw snake_case.
        string Nice(string id)
        {
            var sp = _reg != null ? _reg.Get(id) : null;
            return sp != null && !string.IsNullOrEmpty(sp.displayName) ? sp.displayName : Humanize(id);
        }
        static string Humanize(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            var parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            return string.Join(" ", parts);
        }

        string NiceList(List<string> ids)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < ids.Count; i++) { if (i > 0) sb.Append(", "); sb.Append(Nice(ids[i])); }
            return sb.ToString();
        }

        // "wolf 3->4" (dev notation from Progression) → "Wolf Lv 4".
        string NiceLeveled(List<string> raw)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < raw.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var s = raw[i];
                int sp = s.IndexOf(' '); int arrow = s.IndexOf("->");
                if (sp > 0 && arrow > sp) sb.Append(Nice(s.Substring(0, sp))).Append(" Lv ").Append(s.Substring(arrow + 2));
                else sb.Append(s);
            }
            return sb.ToString();
        }

        // Best single-monster hit count in a battle (matches the "Combo King" stat).
        int BestComboCount(BattleResult r)
        {
            if (r == null) return 0;
            var cnt = new Dictionary<string, int>(); int best = 0;
            foreach (var e in r.events)
                if (e.kind == "Action" && e.actorTeam != e.targetTeam)
                {
                    string k = e.actorTeam + "_" + e.actorSlot;
                    cnt.TryGetValue(k, out var c); c++; cnt[k] = c; if (c > best) best = c;
                }
            return best;
        }

        RectTransform _progressContent;
        void BuildProgress(Transform parent)
        {
            _progress = UIFactory.Panel(parent, "ProgressPanel", new Color(0.09f, 0.1f, 0.13f));
            UIFactory.Label(_progress, "TRAINER PROFILE", 48, new Vector2(0, 880), new Vector2(1000, 90), _font);
            var holder = new GameObject("ProgContent", typeof(RectTransform));
            _progressContent = holder.GetComponent<RectTransform>(); _progressContent.SetParent(_progress, false);
            _progressContent.anchorMin = _progressContent.anchorMax = new Vector2(0.5f, 0.5f);
            _progressContent.sizeDelta = new Vector2(1040, 1800); _progressContent.anchoredPosition = new Vector2(0, -20);
            UIFactory.Button(_progress, "BACK", new Vector2(0, -900), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }

        // Phase X: retention dashboard — profile stats, streaks, completion bars, next goals.
        void RefreshProgress()
        {
            var d = _profile;
            for (int i = _progressContent.childCount - 1; i >= 0; i--) Destroy(_progressContent.GetChild(i).gameObject);
            float y = 860;
            int winPct = d.battlesPlayed > 0 ? (int)(d.battlesWon * 100L / d.battlesPlayed) : 0;
            UIFactory.Label(_progressContent, d.playerName + "     Trainer Lv " + d.playerLevel, 40, new Vector2(0, y), new Vector2(1000, 56), _font); y -= 66;
            UIFactory.Label(_progressContent, d.coins + " coins        " + d.battlesWon + " / " + d.battlesPlayed + " won  (" + winPct + "%)", 30, new Vector2(0, y), new Vector2(1000, 44), _font).color = new Color(0.8f, 0.85f, 0.95f); y -= 52;
            UIFactory.Label(_progressContent, "Win streak: " + d.winStreak + "    Best: " + d.bestWinStreak + "    Daily streak: " + d.loginStreak, 30, new Vector2(0, y), new Vector2(1000, 44), _font).color = new Color(1f, 0.85f, 0.4f); y -= 84;

            int roster = _roster.Count;
            CompletionBar("Monster Dex", roster > 0 ? Achievements.Discovered(d) * 100 / roster : 0, ref y, new Color(0.4f, 0.8f, 1f));
            CompletionBar("Collection owned", roster > 0 ? d.unlocked.Count * 100 / roster : 0, ref y, new Color(0.5f, 0.9f, 0.5f));
            CompletionBar("Career", Career.CompletionPercent(d), ref y, new Color(1f, 0.7f, 0.3f));
            CompletionBar("Achievements", Achievements.Defs.Length > 0 ? Achievements.UnlockedCount(d) * 100 / Achievements.Defs.Length : 0, ref y, new Color(1f, 0.82f, 0.3f));

            y -= 16;
            UIFactory.Label(_progressContent, "NEXT GOALS", 32, new Vector2(-460, y), new Vector2(600, 44), _font).alignment = TextAnchor.MiddleLeft; y -= 54;
            int shown = 0;
            foreach (var def in Quests.Defs)
            {
                if (shown >= 4 || Quests.IsComplete(d, def)) continue;
                UIFactory.Label(_progressContent, "•  " + def.title + "   (" + Quests.Progress(d, def) + " / " + def.target + ")", 28, new Vector2(-30, y), new Vector2(1000, 40), _font).alignment = TextAnchor.MiddleLeft;
                y -= 48; shown++;
            }
            if (shown == 0) UIFactory.Label(_progressContent, "Every goal complete — you're a legend!", 28, new Vector2(0, y), new Vector2(1000, 40), _font).color = new Color(1f, 0.85f, 0.4f);
        }

        void CompletionBar(string label, int pct, ref float y, Color col)
        {
            UIFactory.Label(_progressContent, label + "   " + pct + "%", 28, new Vector2(-460, y + 4), new Vector2(720, 40), _font).alignment = TextAnchor.MiddleLeft;
            var bar = UIFactory.Box(_progressContent, "cbg", new Color(0, 0, 0, 0.5f), new Vector2(940, 22), new Vector2(0, y - 30));
            var fill = UIFactory.Panel(bar, "cf", col);
            fill.anchorMin = new Vector2(0, 0); fill.anchorMax = new Vector2(Mathf.Clamp01(pct / 100f), 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
            y -= 74;
        }

        void BuildCollection(Transform parent)
        {
            _collection = UIFactory.Panel(parent, "CollectionPanel", new Color(0.09f, 0.1f, 0.13f));
            _collHeader = UIFactory.Label(_collection, "COLLECTION", 40, new Vector2(0, 880), new Vector2(1020, 80), _font);
            // Role filter — 2 rows of 3 so long labels ("Support") never clip.
            string[] fl = { "All", "Tank", "Bruiser", "Assassin", "Mage", "Support" };
            for (int i = 0; i < fl.Length; i++)
            {
                int fi = i - 1;
                float x = -340 + (i % 3) * 340f;
                float y = 808 - (i / 3) * 74f;
                UIFactory.Button(_collection, fl[i], new Vector2(x, y), new Vector2(300, 62), _font, () => { _collFilter = fi; RefreshCollection(); });
            }
            UIFactory.Button(_collection, "Sort", new Vector2(0, 662), new Vector2(220, 58), _font, () => { _collSortRarity = !_collSortRarity; RefreshCollection(); });
            var holder = new GameObject("CollContent", typeof(RectTransform));
            _collContent = holder.GetComponent<RectTransform>(); _collContent.SetParent(_collection, false);
            _collContent.anchorMin = _collContent.anchorMax = new Vector2(0.5f, 0.5f);
            _collContent.sizeDelta = new Vector2(1040, 1300); _collContent.anchoredPosition = new Vector2(0, -60);
            UIFactory.Button(_collection, "BACK", new Vector2(0, -890), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }

        void RefreshCollection()
        {
            int owned = 0; foreach (var id in _roster) if (_profile.IsUnlocked(id)) owned++;
            _collHeader.text = "COLLECTION   " + MonsterMeta.OwnedPercent(_profile, _roster) + "%   (" + owned + "/" + _roster.Count + ")";
            for (int i = _collContent.childCount - 1; i >= 0; i--) Destroy(_collContent.GetChild(i).gameObject);

            var list = new List<string>();
            foreach (var id in _roster)
                if (_collFilter < 0 || (int)MonsterMeta.Role(_reg.Get(id)) == _collFilter) list.Add(id);
            if (_collSortRarity) list.Sort((a, b) => MonsterMeta.Rarity(_reg.Get(b)).CompareTo(MonsterMeta.Rarity(_reg.Get(a))));
            else list.Sort(System.StringComparer.Ordinal);

            const int cols = 3; float tw = 330, th = 176, gx = 18, gy = 14;   // fits 7 rows (21 dex) above BACK
            float x0 = -(cols - 1) * (tw + gx) / 2f, y0 = 540;
            for (int i = 0; i < list.Count; i++)
            {
                int c = i % cols, r = i / cols;
                BuildTile(list[i], new Vector2(x0 + c * (tw + gx), y0 - r * (th + gy)), new Vector2(tw, th));
            }
        }

        // Real monster sprite (front) with procedural fallback.
        RectTransform Portrait(RectTransform parent, string id, SpeciesData sp, float size)
        {
            var sprite = MTA.Battle.MonsterVisual.For(id, false);
            if (sprite != null)
            {
                var go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(size, size);
                var img = go.GetComponent<Image>(); img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false;
                return rt;
            }
            return MTA.Battle.MonsterArt.Build(parent, id, sp != null ? sp.element : "",
                sp != null ? MonsterMeta.Role(sp).ToString() : "Bruiser", size);
        }

        void BuildTile(string id, Vector2 pos, Vector2 size)
        {
            var sp = _reg.Get(id);
            bool owned = _profile.IsUnlocked(id), seen = _profile.IsSeen(id);
            var m = _profile.Find(id);
            var tile = UIFactory.Panel(_collContent, "Tile", owned ? new Color(SpColor(id).r * 0.35f, SpColor(id).g * 0.35f, SpColor(id).b * 0.35f, 0.95f) : new Color(0.16f, 0.16f, 0.2f, 0.95f));
            tile.anchorMin = tile.anchorMax = new Vector2(0.5f, 0.5f); tile.sizeDelta = size; tile.anchoredPosition = pos;
            if (seen)
            {
                // Rarity border — real CC0 9-slice frame (top-strip fallback) + element badge + portrait.
                var rc = RarityColor(MonsterMeta.Rarity(sp));
                var fsprite = UIFactory.UiSprite("frame");
                if (fsprite != null)
                {
                    var fgo = new GameObject("RarityFrame", typeof(RectTransform), typeof(Image));
                    var frt = fgo.GetComponent<RectTransform>(); frt.SetParent(tile, false);
                    frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
                    var fimg = fgo.GetComponent<Image>(); fimg.sprite = fsprite; fimg.type = Image.Type.Sliced;
                    fimg.pixelsPerUnitMultiplier = 3f; fimg.color = rc; fimg.raycastTarget = false;
                    frt.SetAsLastSibling();
                }
                else
                {
                    var frame = UIFactory.Panel(tile, "Frame", rc);
                    frame.anchorMin = new Vector2(0, 1); frame.anchorMax = new Vector2(1, 1); frame.pivot = new Vector2(0.5f, 1);
                    frame.sizeDelta = new Vector2(0, 9); frame.anchoredPosition = Vector2.zero;
                    frame.GetComponent<Image>().raycastTarget = false;
                }
                var art = Portrait(tile, id, sp, Mathf.Min(size.x, size.y) * 0.72f);
                art.anchoredPosition = new Vector2(0, 22);
                UIFactory.ElementBadge(tile, sp.element, new Vector2(size.x / 2 - 32, size.y / 2 - 32), 46, _font);
                UIFactory.StarRow(tile, MonsterMeta.Rarity(sp), new Vector2(0, -34), 18f);
            }
            else
            {
                var siloRoot = MTA.Battle.MonsterArt.Build(tile, "locked_" + id, "", "Bruiser", Mathf.Min(size.x, size.y) * 0.5f);
                siloRoot.anchoredPosition = new Vector2(0, 22);
                siloRoot.gameObject.AddComponent<CanvasGroup>().alpha = 0.16f;   // dim locked silhouette
            }
            UIFactory.Label(tile, seen ? Nice(id) : "???", 24, new Vector2(0, -58), new Vector2(size.x - 16, 42), _font);
            string state = owned ? "Lv " + (m != null ? m.level : 1) : seen ? "SEEN" : "LOCKED";
            var stx = UIFactory.Label(tile, state, 20, new Vector2(0, -78), new Vector2(size.x - 16, 32), _font);
            stx.color = owned ? new Color(0.5f, 1f, 0.6f) : seen ? new Color(0.9f, 0.9f, 0.5f) : new Color(0.7f, 0.7f, 0.7f);
            if (owned)
            {
                var btn = tile.gameObject.AddComponent<Button>();
                string cid = id;
                btn.onClick.AddListener(MTA.Battle.AudioManager.PlayClick);
                btn.onClick.AddListener(() => OpenDetail(cid));
            }
        }

        void OpenDetail(string id) { _detailSpecies = id; _ctrl.ToDetail(); }

        void BuildDetail(Transform parent)
        {
            _detail = UIFactory.Panel(parent, "DetailPanel", new Color(0.1f, 0.11f, 0.14f));
            _detailName = UIFactory.Label(_detail, "", 46, new Vector2(0, 820), new Vector2(1000, 90), _font);
            _detailArt = new GameObject("DetailArt", typeof(RectTransform)).GetComponent<RectTransform>();
            _detailArt.SetParent(_detail, false); _detailArt.anchorMin = _detailArt.anchorMax = new Vector2(0.5f, 0.5f);
            _detailArt.sizeDelta = new Vector2(180, 180); _detailArt.anchoredPosition = new Vector2(340, 630);

            var bgo = new GameObject("XpBg", typeof(RectTransform), typeof(Image));
            var bgr = bgo.GetComponent<RectTransform>(); bgr.SetParent(_detail, false);
            bgr.anchorMin = bgr.anchorMax = new Vector2(0.5f, 0.5f); bgr.sizeDelta = new Vector2(720, 36); bgr.anchoredPosition = new Vector2(0, 700);
            bgo.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var fgo = new GameObject("XpFill", typeof(RectTransform), typeof(Image)); _detailXpFill = fgo.GetComponent<Image>();
            var fr = _detailXpFill.rectTransform; fr.SetParent(bgr, false); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
            _detailXpFill.color = new Color(0.4f, 0.7f, 1f); _detailXpFill.type = Image.Type.Filled; _detailXpFill.fillMethod = Image.FillMethod.Horizontal; _detailXpFill.fillOrigin = 0;

            _detailXp = UIFactory.Label(_detail, "", 26, new Vector2(0, 648), new Vector2(760, 40), _font);
            _detailStats = UIFactory.Label(_detail, "", 28, new Vector2(0, 40), new Vector2(980, 900), _font);
            _detailStats.alignment = TextAnchor.UpperCenter;
            UIFactory.Button(_detail, "TRAIN  (-" + Progression.TrainCost + " coins)", new Vector2(0, -700), new Vector2(560, 110), _font, OnTrain);
            _evolveBtn = UIFactory.Button(_detail, "EVOLVE", new Vector2(0, -820), new Vector2(560, 110), _font, OnEvolve);
            UIFactory.SetButtonColor(_evolveBtn, new Color(0.85f, 0.5f, 0.95f));
            _evolveBtn.gameObject.SetActive(false);
            UIFactory.Button(_detail, "BACK", new Vector2(0, -940), new Vector2(400, 96), _font, () => _ctrl.ToCollection());
        }

        void OnEvolve()
        {
            var sp = _reg.Get(_detailSpecies);
            string evo = Progression.Evolve(_profile, sp);
            if (evo == null) return;
            _profile.evolutionsDone++;
            var newAch = Achievements.CheckNew(_profile, _roster.Count);
            SaveSystem.Save(_profile);
            _detailSpecies = evo;                            // now viewing the evolved form
            MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.Evolution);
            string body = (sp != null ? sp.displayName : _detailSpecies) + "  evolved into\n" + _reg.Get(evo).displayName + "!";
            foreach (var a in newAch) body += "\n\n★ Achievement: " + a.title;
            ShowPopup("EVOLUTION!", body);
            RefreshDetail();
        }

        void RefreshDetail()
        {
            var id = _detailSpecies; if (string.IsNullOrEmpty(id)) return;
            var sp = _reg.Get(id);
            var m = _profile.Find(id) ?? new MonsterSave { speciesId = id, level = 1 };
            _detailName.text = Nice(id) + "    Lv " + m.level;
            if (_detailStars != null) Destroy(_detailStars.gameObject);
            if (sp != null) _detailStars = UIFactory.StarRow(_detail, MonsterMeta.Rarity(sp), new Vector2(0, 762), 26f);
            if (_detailArt != null)
            {
                for (int i = _detailArt.childCount - 1; i >= 0; i--) Destroy(_detailArt.GetChild(i).gameObject);
                Portrait(_detailArt, id, sp, 170);
            }
            int next = Progression.MonsterXpForNext(m.level);
            _detailXpFill.fillAmount = m.level >= Progression.MaxLevel ? 1f : Mathf.Clamp01((float)m.xp / next);
            _detailXp.text = "XP  " + m.xp + " / " + next + "        Coins: " + _profile.coins;
            _detailStats.text = StatsBlock(sp, m.level);
            if (_evolveBtn != null)
            {
                bool can = Progression.CanEvolve(_profile, sp);
                _evolveBtn.gameObject.SetActive(can || (sp != null && !string.IsNullOrEmpty(sp.evolvesTo)));
                _evolveBtn.interactable = can;
                var t = _evolveBtn.GetComponentInChildren<Text>();
                if (t != null) t.text = can ? "EVOLVE" : (sp != null && !string.IsNullOrEmpty(sp.evolvesTo) ? "EVOLVE  (Lv " + sp.evolveLevel + ")" : "EVOLVE");
            }
        }

        int Lg(SpeciesData sp, Stat s) => StatMath.LevelGain(sp.speciesId, s, GrowthTier.B, _cfg);
        int Eff(SpeciesData sp, int lvl, Stat s) => sp.baseStats.Get(s) + Lg(sp, s) * (lvl - 1);

        string StatsBlock(SpeciesData sp, int lvl)
        {
            string L(string n, Stat s) => n + ":  " + Eff(sp, lvl, s) + "   (+" + Lg(sp, s) + "/lvl)   next: " + Eff(sp, lvl + 1, s);
            return "Role: " + MonsterMeta.Role(sp) + "     Element: " + sp.element +
                "     Rarity " + MonsterMeta.Rarity(sp) + "/5\n\n" +
                L("HP", Stat.HP) + "\n" + L("ATK", Stat.ATK) + "\n" + L("DEF", Stat.DEF) + "\n" +
                L("SPD", Stat.SPD) + "\n" + L("INT", Stat.INT) + "\n" + L("LUCK", Stat.LUCK);
        }

        static Color RarityColor(int rarity)
        {
            switch (rarity)
            {
                case 5: return new Color(1f, 0.82f, 0.3f);    // gold
                case 4: return new Color(0.72f, 0.45f, 0.95f); // purple
                case 3: return new Color(0.35f, 0.6f, 0.95f);  // blue
                case 2: return new Color(0.4f, 0.85f, 0.45f);  // green
                default: return new Color(0.65f, 0.65f, 0.7f); // gray
            }
        }

        void OnTrain()
        {
            int gained = Progression.Train(_profile, _detailSpecies);
            if (gained < 0) { ShowPopup("NOT ENOUGH COINS", "Need " + Progression.TrainCost + " coins.\nWin battles to earn more."); return; }
            _profile.trainingsDone++;
            Quests.SyncDay(_profile, DailyRewards.DayIndex(System.DateTime.Now));
            _profile.dailyTrains++;
            var newAch = Achievements.CheckNew(_profile, _roster.Count);
            SaveSystem.Save(_profile);
            if (gained > 0) { MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.LevelUp); var m = _profile.Find(_detailSpecies); ShowPopup("LEVEL UP!", Nice(_detailSpecies) + "  reached  Lv " + (m != null ? m.level : 1)); }
            foreach (var a in newAch) ShowPopup("ACHIEVEMENT!", "★ " + a.title + "\n" + a.desc);
            RefreshDetail();
        }

        void BuildCareer(Transform parent)
        {
            _career = UIFactory.Panel(parent, "CareerPanel", new Color(0.09f, 0.1f, 0.13f));
            UIFactory.Label(_career, "CAREER", 48, new Vector2(0, 860), new Vector2(900, 90), _font);
            _careerHeader = UIFactory.Label(_career, "", 30, new Vector2(0, 782), new Vector2(1000, 56), _font);
            _careerHeader.color = new Color(1f, 0.9f, 0.5f);
            var holder = new GameObject("CareerContent", typeof(RectTransform));
            _careerContent = holder.GetComponent<RectTransform>(); _careerContent.SetParent(_career, false);
            _careerContent.anchorMin = _careerContent.anchorMax = new Vector2(0.5f, 0.5f);
            _careerContent.sizeDelta = new Vector2(1040, 1400); _careerContent.anchoredPosition = new Vector2(0, -40);
            UIFactory.Button(_career, "BACK", new Vector2(0, -890), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }

        void RefreshCareer()
        {
            bool complete = Career.IsComplete(_profile);
            _careerHeader.text = "Completion  " + Career.CompletionPercent(_profile) + "%" + (complete ? "    ALL CLEARED!" : "");
            for (int i = _careerContent.childCount - 1; i >= 0; i--) Destroy(_careerContent.GetChild(i).gameObject);

            const int cols = 3; float bw = 336, bh = 130, gx = 16, gy = 14;   // 18 stages → 6 rows (fits above BACK)
            float x0 = -(cols - 1) * (bw + gx) / 2f, y0 = 560;
            for (int i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                bool cleared = Career.IsCleared(_profile, i);
                bool unlocked = Career.IsUnlocked(_profile, i);
                int c = i % cols, r = i / cols;
                var pos = new Vector2(x0 + c * (bw + gx), y0 - r * (bh + gy));
                string status = cleared ? "CLEARED" : unlocked ? "PLAY  (+" + stage.reward + ")" : "LOCKED";
                var b = UIFactory.Button(_careerContent, stage.name + "\nLv " + stage.enemyLevel + "   " + status,
                    pos, new Vector2(bw, bh), _font, () => _ctrl.SelectCareerStage(stage));
                b.interactable = unlocked;
                UIFactory.SetButtonColor(b, !unlocked ? new Color(0.2f, 0.2f, 0.24f)
                    : cleared ? new Color(0.2f, 0.55f, 0.3f)
                    : new Color(0.85f, 0.55f, 0.15f));   // current frontier highlighted
            }
        }

        void BuildDaily(Transform parent)
        {
            _daily = UIFactory.Panel(parent, "DailyPanel", new Color(0.1f, 0.09f, 0.13f));
            UIFactory.Label(_daily, "DAILY REWARD", 48, new Vector2(0, 840), new Vector2(1000, 90), _font);
            _dailyInfo = UIFactory.Label(_daily, "", 34, new Vector2(0, 560), new Vector2(1000, 260), _font);
            _dailyClaimBtn = UIFactory.Button(_daily, "CLAIM", new Vector2(0, 320), new Vector2(520, 130), _font, OnClaimDaily);
            UIFactory.SetButtonColor(_dailyClaimBtn, new Color(0.2f, 0.75f, 0.35f));
            UIFactory.Label(_daily, "History", 30, new Vector2(0, 180), new Vector2(600, 50), _font).color = new Color(0.8f, 0.8f, 0.85f);
            _dailyHistory = UIFactory.Label(_daily, "", 26, new Vector2(0, -260), new Vector2(900, 760), _font);
            _dailyHistory.alignment = TextAnchor.UpperCenter;
            UIFactory.Button(_daily, "BACK", new Vector2(0, -890), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }

        void RefreshDaily()
        {
            var now = System.DateTime.Now;
            bool can = DailyRewards.CanClaim(_profile, now);
            int preview = DailyRewards.Preview(_profile, now);
            int nextDay = Streaks.NextMilestoneDay(_profile);
            string goal = nextDay > 0
                ? "\n\nNext streak bonus:  Day " + nextDay + "  =  +" + Streaks.NextMilestoneCoins(_profile) + " coins"
                : "\n\nAll streak bonuses claimed — legend!";
            _dailyInfo.text = "Login streak:  " + _profile.loginStreak + " day" + (_profile.loginStreak == 1 ? "" : "s") + "\n" +
                "Coins:  " + _profile.coins + "\n\n" +
                (can ? "Today's reward:  +" + preview + " coins" : "Claimed today — come back tomorrow!") + goal;
            _dailyClaimBtn.interactable = can;
            var h = _profile.rewardHistory;
            string s = "";
            for (int i = h.Count - 1; i >= 0 && i >= h.Count - 12; i--) s += h[i] + "\n";
            _dailyHistory.text = s.Length == 0 ? "(no claims yet)" : s;
        }

        void OnClaimDaily()
        {
            var r = DailyRewards.Claim(_profile, System.DateTime.Now);
            if (r.claimed)
            {
                var ms = Streaks.CheckMilestones(_profile);                 // login-streak milestone bonuses
                var newAch = Achievements.CheckNew(_profile, _roster.Count); // e.g. "Devoted" (30-day)
                SaveSystem.Save(_profile);
                MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.Reward);
                string body = "Day " + r.streak + " streak!\n+" + r.coins + " coins" + (r.streakReset ? "\n(streak restarted)" : "");
                foreach (var m in ms) body += "\n\n★ " + m.Key + "-DAY STREAK BONUS!  +" + m.Value + " coins";
                foreach (var a in newAch) body += "\n★ Achievement: " + a.title;
                ShowPopup("DAILY REWARD", body);
            }
            RefreshDaily();
        }

        void ApplyDisplaySettings()
        {
            Application.targetFrameRate = _profile.targetFps <= 0 ? 60 : _profile.targetFps;
            int max = Mathf.Max(0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(_profile.quality <= 0 ? 0 : max, true);
        }

        void BuildLoading(Transform parent)
        {
            _loading = UIFactory.Panel(parent, "LoadingPanel", new Color(0.06f, 0.07f, 0.1f));
            UIFactory.Label(_loading, "TRAIN YOUR MONSTER", 54, new Vector2(0, 120), new Vector2(1000, 100), _font);
            UIFactory.Label(_loading, "LOADING...", 34, new Vector2(0, -40), new Vector2(800, 70), _font)
                .color = new Color(0.7f, 0.8f, 1f);
            UIFactory.Label(_loading, "v" + Application.version, 26, new Vector2(0, -820), new Vector2(600, 50), _font)
                .color = new Color(0.6f, 0.6f, 0.7f);
            _loading.SetAsLastSibling();
        }

        System.Collections.IEnumerator HideLoading()
        {
            yield return new WaitForSeconds(0.7f);
            if (_loading != null) _loading.gameObject.SetActive(false);
        }

        void BuildSettings(Transform parent)
        {
            _settings = UIFactory.Panel(parent, "SettingsPanel", new Color(0.09f, 0.1f, 0.13f));
            UIFactory.Label(_settings, "SETTINGS", 48, new Vector2(0, 840), new Vector2(1000, 90), _font);
            _settingsInfo = UIFactory.Label(_settings, "", 32, new Vector2(0, 660), new Vector2(1000, 130), _font);
            _muteBtn = UIFactory.Button(_settings, MuteLabel(), new Vector2(0, 560), new Vector2(560, 100), _font, ToggleMute);
            UIFactory.Button(_settings, "FRAME RATE", new Vector2(-150, 440), new Vector2(360, 100), _font, ToggleFps);
            UIFactory.Button(_settings, "QUALITY", new Vector2(230, 440), new Vector2(320, 100), _font, ToggleQuality);

            // Independent volume sliders (persisted in PlayerPrefs).
            VolumeRow("MUSIC", 300, MTA.Battle.AudioManager.MusicVolume, v => MTA.Battle.AudioManager.SetMusicVolume(v));
            VolumeRow("SFX", 190, MTA.Battle.AudioManager.SfxVolume, v => MTA.Battle.AudioManager.SetVolume(MTA.Battle.AudioBus.Sfx, v));
            VolumeRow("UI", 80, MTA.Battle.AudioManager.UiVolume, v => { MTA.Battle.AudioManager.SetVolume(MTA.Battle.AudioBus.Ui, v); MTA.Battle.AudioManager.PlayClick(); });

            UIFactory.Button(_settings, "ABOUT / CREDITS", new Vector2(0, -80), new Vector2(560, 100), _font, () => _ctrl.ToAbout());
            UIFactory.Label(_settings, "v" + Application.version + "    com.trainyourmonster.game", 24, new Vector2(0, -740), new Vector2(1000, 50), _font)
                .color = new Color(0.6f, 0.6f, 0.7f);
            UIFactory.Button(_settings, "BACK", new Vector2(0, -890), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }

        void VolumeRow(string label, float y, float value, System.Action<float> onChange)
        {
            UIFactory.Label(_settings, label, 28, new Vector2(-340, y), new Vector2(220, 60), _font);
            UIFactory.Slider(_settings, new Vector2(120, y), new Vector2(560, 46), value, onChange);
        }

        void RefreshSettings()
        {
            _settingsInfo.text =
                "Sound:  " + (_profile.muted ? "OFF" : "ON") + "         " +
                "Frame rate:  " + (_profile.targetFps <= 0 ? 60 : _profile.targetFps) + " FPS\n" +
                "Quality:  " + (_profile.quality <= 0 ? "Low" : "High");
            if (_muteBtn != null) { var t = _muteBtn.GetComponentInChildren<Text>(); if (t != null) t.text = MuteLabel(); }
        }

        void ToggleFps()
        {
            _profile.targetFps = _profile.targetFps >= 60 ? 30 : 60;
            SaveSystem.Save(_profile);
            ApplyDisplaySettings();
            RefreshSettings();
        }

        void ToggleQuality()
        {
            _profile.quality = _profile.quality <= 0 ? 1 : 0;
            SaveSystem.Save(_profile);
            ApplyDisplaySettings();
            RefreshSettings();
        }

        void BuildAbout(Transform parent)
        {
            _about = UIFactory.Panel(parent, "AboutPanel", new Color(0.08f, 0.09f, 0.12f));
            UIFactory.Label(_about, "ABOUT", 48, new Vector2(0, 840), new Vector2(1000, 90), _font);
            var body = UIFactory.Label(_about,
                "TRAIN YOUR MONSTER\n" +
                "v" + Application.version + "\n" +
                "com.trainyourmonster.game\n\n" +
                "A deterministic monster-raising auto-battler.\n\n" +
                "CREDITS\n" +
                "Design & Code:  Lifkie Lie\n" +
                "Engine:  Unity " + Application.unityVersion + "\n\n" +
                "Art & audio (CC0):\n" +
                "Monsters — isaiah658 (50+ Monsters Pack 2D)\n" +
                "VFX — CodeManu · UI — Kenney\n" +
                "Music — CleytonRX · SFX — rubberduck\n\n" +
                "Thank you for playing!",
                30, new Vector2(0, -20), new Vector2(1000, 1300), _font);
            body.alignment = TextAnchor.UpperCenter;
            UIFactory.Button(_about, "BACK", new Vector2(0, -890), new Vector2(400, 100), _font, () => _ctrl.ToSettings());
        }

        void BuildPopup(Transform parent)
        {
            _popup = UIFactory.Panel(parent, "Popup", new Color(0, 0, 0, 0.75f));
            var card = UIFactory.Panel(_popup, "PopupCard", new Color(0.15f, 0.16f, 0.22f));
            card.GetComponent<Image>().sprite = MTA.Battle.ProceduralArt.RoundedRect();
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f); card.sizeDelta = new Vector2(840, 520); card.anchoredPosition = Vector2.zero;
            _popupCard = card;
            _popupTitle = UIFactory.Label(card, "NEW MONSTER!", 48, new Vector2(0, 160), new Vector2(780, 100), _font);
            _popupTitle.color = new Color(1f, 0.9f, 0.4f);
            _popupText = UIFactory.Label(card, "", 36, new Vector2(0, 10), new Vector2(780, 220), _font);
            UIFactory.Button(card, "NICE!", new Vector2(0, -180), new Vector2(340, 110), _font, () => _popup.gameObject.SetActive(false));
            _popup.gameObject.SetActive(false);
        }

        void ShowPopup(string title, string body)
        {
            if (_popup == null) return;
            _popupTitle.text = title;
            _popupText.text = body;
            _popup.gameObject.SetActive(true);
            _popup.SetAsLastSibling();
            if (_popupCard != null) StartCoroutine(PopIn(_popupCard));
        }

        System.Collections.IEnumerator PopIn(RectTransform card)
        {
            float t = 0f;
            while (t < 1f && card != null)
            {
                t += Time.unscaledDeltaTime / 0.24f;
                float e = Mathf.Clamp01(t);
                float s = Mathf.Lerp(0.7f, 1f, 1f - (1f - e) * (1f - e));   // ease-out overshoot-ish
                card.localScale = Vector3.one * s;
                yield return null;
            }
            if (card != null) card.localScale = Vector3.one;
        }

        void ShowNewMonster(List<string> ids) => ShowPopup("NEW MONSTER!", NiceList(ids).Replace(", ", "\n"));

        // ===================== Phase T — Onboarding =====================
        Text _onbTitle, _onbBody; Button _onbNextBtn;
        static readonly (string, string)[] OnbPages =
        {
            ("WELCOME, TRAINER!", "Raise monsters, build a team of THREE, and battle.\n\nBattles play out automatically — you pick the team, then watch the fight unfold."),
            ("ELEMENTS", "Every monster has an element:\n\nFIRE  beats  NATURE\nNATURE  beats  WATER\nWATER  beats  FIRE\n\nPick monsters that counter the enemy."),
            ("ROLES", "Monsters fill roles:\n\nTANK - soaks damage\nBRUISER - all-round\nASSASSIN - fast & deadly\nMAGE - ranged power\nSUPPORT - heals allies\n\nMix roles for a strong team."),
            ("GROW STRONGER", "Win battles to earn XP and COINS.\n\nOpen a monster's detail and TRAIN it with coins to level it up faster."),
            ("EVOLUTION", "At a high enough level, some monsters EVOLVE into a stronger form.\n\nTap EVOLVE on the detail screen when it lights up."),
            ("GOALS & REWARDS", "Claim DAILY rewards, finish QUESTS, and earn ACHIEVEMENTS.\n\nDiscover every monster in the DEX.\n\nReady? Pick your first team and fight!"),
        };
        void BuildOnboarding(Transform parent)
        {
            _onboarding = UIFactory.Panel(parent, "OnbPanel", new Color(0.06f, 0.08f, 0.13f));
            var card = UIFactory.Box(_onboarding, "OnbCard", new Color(0.12f, 0.14f, 0.2f, 0.98f), new Vector2(940, 1000), new Vector2(0, 40));
            _onbTitle = UIFactory.Label(card, "", 52, new Vector2(0, 380), new Vector2(880, 90), _font);
            _onbTitle.color = new Color(1f, 0.85f, 0.4f);
            _onbBody = UIFactory.Label(card, "", 34, new Vector2(0, 10), new Vector2(820, 620), _font);
            _onbNextBtn = UIFactory.Button(card, "NEXT", new Vector2(0, -400), new Vector2(460, 110), _font, OnbNext);
            UIFactory.Button(_onboarding, "SKIP", new Vector2(360, 900), new Vector2(240, 84), _font, OnbFinish);
        }
        void RefreshOnboarding()
        {
            _onbPage = Mathf.Clamp(_onbPage, 0, OnbPages.Length - 1);
            _onbTitle.text = OnbPages[_onbPage].Item1;
            _onbBody.text = OnbPages[_onbPage].Item2;
            var t = _onbNextBtn.GetComponentInChildren<Text>();
            if (t != null) t.text = _onbPage >= OnbPages.Length - 1 ? "START PLAYING" : "NEXT  (" + (_onbPage + 1) + "/" + OnbPages.Length + ")";
        }
        void OnbNext()
        {
            MTA.Battle.AudioManager.PlayClick();
            if (_onbPage >= OnbPages.Length - 1) OnbFinish();
            else { _onbPage++; RefreshOnboarding(); }
        }
        void OnbFinish()
        {
            _profile.onboarded = true; SaveSystem.Save(_profile);
            _ctrl.StartGame();   // straight into first team selection
        }

        // ===================== Phase U — Quests =====================
        void BuildQuests(Transform parent)
        {
            _quests = UIFactory.Panel(parent, "QuestsPanel", new Color(0.09f, 0.1f, 0.14f));
            _questHeader = UIFactory.Label(_quests, "QUESTS", 46, new Vector2(0, 880), new Vector2(1000, 80), _font);
            var holder = new GameObject("QContent", typeof(RectTransform));
            _questContent = holder.GetComponent<RectTransform>(); _questContent.SetParent(_quests, false);
            _questContent.anchorMin = _questContent.anchorMax = new Vector2(0.5f, 0.5f);
            _questContent.sizeDelta = new Vector2(1040, 1800); _questContent.anchoredPosition = new Vector2(0, -40);
            UIFactory.Button(_quests, "BACK", new Vector2(0, -900), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }
        void RefreshQuests()
        {
            Quests.SyncDay(_profile, DailyRewards.DayIndex(System.DateTime.Now));
            int ready = Quests.UnclaimedReady(_profile);
            _questHeader.text = ready > 0 ? "QUESTS   (" + ready + " ready)" : "QUESTS";
            for (int i = _questContent.childCount - 1; i >= 0; i--) Destroy(_questContent.GetChild(i).gameObject);
            float y = 800f; QuestKind last = (QuestKind)(-1);
            foreach (var def in Quests.Defs)
            {
                if (def.kind != last) { last = def.kind; UIFactory.Label(_questContent, def.kind.ToString().ToUpper(), 30, new Vector2(-470, y), new Vector2(500, 44), _font).color = new Color(0.6f, 0.75f, 1f); y -= 54; }
                QuestRow(def, y); y -= 132;
            }
        }
        void QuestRow(QuestDef def, float y)
        {
            int prog = Quests.Progress(_profile, def); bool done = Quests.IsComplete(_profile, def); bool claimed = Quests.IsClaimed(_profile, def);
            var row = UIFactory.Box(_questContent, "QRow", new Color(0.15f, 0.17f, 0.22f, 0.95f), new Vector2(1000, 118), new Vector2(0, y));
            UIFactory.Label(row, def.title, 30, new Vector2(-150, 24), new Vector2(680, 44), _font).alignment = TextAnchor.MiddleLeft;
            UIFactory.Label(row, prog + " / " + def.target + "     +" + def.coins + " coins", 24, new Vector2(-150, -26), new Vector2(680, 36), _font).alignment = TextAnchor.MiddleLeft;
            // progress bar
            var bar = UIFactory.Panel(row, "Bar", new Color(0, 0, 0, 0.5f));
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f); bar.sizeDelta = new Vector2(660, 12); bar.anchoredPosition = new Vector2(-160, -50);
            var fill = UIFactory.Panel(bar, "Fill", new Color(0.35f, 0.85f, 0.4f));
            fill.anchorMin = new Vector2(0, 0); fill.anchorMax = new Vector2(Mathf.Clamp01(prog / (float)def.target), 1); fill.offsetMin = fill.offsetMax = Vector2.zero;
            if (claimed) UIFactory.Label(row, "CLAIMED", 28, new Vector2(360, 0), new Vector2(240, 80), _font).color = new Color(0.5f, 0.9f, 0.6f);
            else
            {
                var b = UIFactory.Button(row, done ? "CLAIM" : "...", new Vector2(360, 0), new Vector2(240, 84), _font, () => ClaimQuest(def));
                b.interactable = done;
                UIFactory.SetButtonColor(b, done ? new Color(0.35f, 0.8f, 0.4f) : new Color(0.4f, 0.4f, 0.45f));
            }
        }
        void ClaimQuest(QuestDef def)
        {
            if (Quests.Claim(_profile, def, out int coins, out int xp))
            {
                MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.Reward);
                SaveSystem.Save(_profile);
                var newAch = Achievements.CheckNew(_profile, _roster.Count); if (newAch.Count > 0) SaveSystem.Save(_profile);
                RefreshQuests();
                string body = "+" + coins + " coins,  +" + xp + " XP";
                foreach (var a in newAch) body += "\n★ Achievement: " + a.title;
                ShowPopup("QUEST COMPLETE", body);
            }
        }

        // ===================== Phase V — Achievements =====================
        void BuildAchievements(Transform parent)
        {
            _achievements = UIFactory.Panel(parent, "AchPanel", new Color(0.1f, 0.09f, 0.13f));
            _achHeader = UIFactory.Label(_achievements, "ACHIEVEMENTS", 46, new Vector2(0, 880), new Vector2(1000, 80), _font);
            var holder = new GameObject("AContent", typeof(RectTransform));
            _achContent = holder.GetComponent<RectTransform>(); _achContent.SetParent(_achievements, false);
            _achContent.anchorMin = _achContent.anchorMax = new Vector2(0.5f, 0.5f);
            _achContent.sizeDelta = new Vector2(1040, 1900); _achContent.anchoredPosition = new Vector2(0, -40);
            UIFactory.Button(_achievements, "BACK", new Vector2(0, -900), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }
        void RefreshAchievements()
        {
            _achHeader.text = "ACHIEVEMENTS   " + Achievements.UnlockedCount(_profile) + " / " + Achievements.Defs.Length;
            for (int i = _achContent.childCount - 1; i >= 0; i--) Destroy(_achContent.GetChild(i).gameObject);
            float y = 800f;
            foreach (var def in Achievements.Defs)
            {
                bool got = _profile.HasAchievement(def.id);
                var row = UIFactory.Box(_achContent, "ARow", got ? new Color(0.22f, 0.2f, 0.1f, 0.95f) : new Color(0.14f, 0.14f, 0.17f, 0.95f), new Vector2(1000, 128), new Vector2(0, y)); y -= 144;
                var medal = UIFactory.Label(row, got ? "★" : "?", 54, new Vector2(-420, 0), new Vector2(110, 110), _font);
                medal.color = got ? new Color(1f, 0.82f, 0.3f) : new Color(0.4f, 0.4f, 0.45f);
                var title = UIFactory.Label(row, def.title, 32, new Vector2(50, 24), new Vector2(760, 46), _font); title.alignment = TextAnchor.MiddleLeft;
                title.color = got ? Color.white : new Color(0.6f, 0.6f, 0.66f);
                var desc = UIFactory.Label(row, def.desc, 25, new Vector2(50, -24), new Vector2(760, 42), _font); desc.alignment = TextAnchor.MiddleLeft;
                desc.color = new Color(0.7f, 0.72f, 0.8f);
            }
        }

        // ===================== Phase W — Monster Dex =====================
        void BuildDex(Transform parent)
        {
            _dex = UIFactory.Panel(parent, "DexPanel", new Color(0.08f, 0.1f, 0.12f));
            _dexHeader = UIFactory.Label(_dex, "MONSTER DEX", 46, new Vector2(0, 880), new Vector2(1000, 80), _font);
            var holder = new GameObject("DexContent", typeof(RectTransform));
            _dexContent = holder.GetComponent<RectTransform>(); _dexContent.SetParent(_dex, false);
            _dexContent.anchorMin = _dexContent.anchorMax = new Vector2(0.5f, 0.5f);
            _dexContent.sizeDelta = new Vector2(1040, 1900); _dexContent.anchoredPosition = new Vector2(0, -30);
            // detail overlay (hidden until a discovered species is tapped)
            _dexDetail = UIFactory.Box(_dex, "DexDetail", new Color(0.1f, 0.12f, 0.16f, 0.99f), new Vector2(960, 1500), new Vector2(0, 20));
            _dexDetail.gameObject.SetActive(false);
            UIFactory.Button(_dex, "BACK", new Vector2(0, -900), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }
        void RefreshDex()
        {
            _dexDetail.gameObject.SetActive(false);
            int disc = Achievements.Discovered(_profile);
            _dexHeader.text = "MONSTER DEX   " + disc + " / " + _roster.Count + "   (" + (_roster.Count > 0 ? disc * 100 / _roster.Count : 0) + "%)";
            for (int i = _dexContent.childCount - 1; i >= 0; i--) Destroy(_dexContent.GetChild(i).gameObject);
            const int cols = 3; float tw = 330, th = 202, gx = 12, gy = 12;
            float x0 = -(cols - 1) * (tw + gx) / 2f, y0 = 720;
            for (int i = 0; i < _roster.Count; i++)
            {
                string id = _roster[i]; var sp = _reg.Get(id); bool seen = _profile.IsSeen(id);
                int c = i % cols, r = i / cols;
                var tile = UIFactory.Box(_dexContent, "DexTile", new Color(0.15f, 0.16f, 0.2f, 0.95f), new Vector2(tw, th), new Vector2(x0 + c * (tw + gx), y0 - r * (th + gy)));
                UIFactory.Label(tile, "#" + (i + 1).ToString("D2"), 20, new Vector2(-tw / 2 + 34, th / 2 - 22), new Vector2(80, 30), _font).color = new Color(0.6f, 0.6f, 0.7f);
                if (seen)
                {
                    var art = Portrait(tile, id, sp, 116); art.anchoredPosition = new Vector2(0, 26);
                    UIFactory.Label(tile, Nice(id), 24, new Vector2(0, -54), new Vector2(tw - 16, 32), _font);
                    UIFactory.ElementBadge(tile, sp.element, new Vector2(tw / 2 - 32, th / 2 - 30), 42, _font);
                    UIFactory.StarRow(tile, MonsterMeta.Rarity(sp), new Vector2(0, -80), 15f);
                    string cid = id; var btn = tile.gameObject.AddComponent<Button>();
                    btn.onClick.AddListener(MTA.Battle.AudioManager.PlayClick);
                    btn.onClick.AddListener(() => ShowDexDetail(cid));
                }
                else
                {
                    var silo = MTA.Battle.MonsterArt.Build(tile, "dex_" + id, "", "Bruiser", 104);
                    silo.anchoredPosition = new Vector2(0, 26); silo.gameObject.AddComponent<CanvasGroup>().alpha = 0.14f;
                    UIFactory.Label(tile, "???", 26, new Vector2(0, -54), new Vector2(tw - 16, 32), _font).color = new Color(0.5f, 0.5f, 0.55f);
                }
            }
        }
        void ShowDexDetail(string id)
        {
            var sp = _reg.Get(id); if (sp == null) return;
            for (int i = _dexDetail.childCount - 1; i >= 0; i--) Destroy(_dexDetail.GetChild(i).gameObject);
            _dexDetail.gameObject.SetActive(true); _dexDetail.SetAsLastSibling();
            UIFactory.Label(_dexDetail, Nice(id), 46, new Vector2(0, 660), new Vector2(900, 80), _font).color = new Color(1f, 0.85f, 0.4f);
            var art = Portrait(_dexDetail, id, sp, 300); art.anchoredPosition = new Vector2(0, 400);
            UIFactory.ElementBadge(_dexDetail, sp.element, new Vector2(-360, 560), 60, _font);
            UIFactory.StarRow(_dexDetail, MonsterMeta.Rarity(sp), new Vector2(0, 180), 34f);
            UIFactory.Label(_dexDetail, "Element:  " + sp.element + "        Role:  " + MonsterMeta.Role(sp), 30, new Vector2(0, 110), new Vector2(880, 44), _font);
            var bs = sp.baseStats;
            string stats = "HP " + bs.hp + "    ATK " + bs.atk + "    DEF " + bs.def + "\nSPD " + bs.spd + "    INT " + bs.intel + "    LUCK " + bs.luck;
            UIFactory.Label(_dexDetail, stats, 30, new Vector2(0, 10), new Vector2(880, 100), _font);
            // evolution chain
            string chain = Nice(id);
            if (!string.IsNullOrEmpty(sp.evolvesTo)) chain += "   →   " + Nice(sp.evolvesTo) + "  (Lv " + sp.evolveLevel + ")";
            UIFactory.Label(_dexDetail, "Evolution:  " + chain, 28, new Vector2(0, -110), new Vector2(880, 44), _font).color = new Color(0.7f, 0.9f, 0.75f);
            UIFactory.Label(_dexDetail, _profile.IsUnlocked(id) ? "OWNED" : "SEEN", 30, new Vector2(0, -200), new Vector2(880, 44), _font)
                .color = _profile.IsUnlocked(id) ? new Color(0.5f, 1f, 0.6f) : new Color(0.9f, 0.9f, 0.5f);
            UIFactory.Button(_dexDetail, "CLOSE", new Vector2(0, -560), new Vector2(360, 100), _font, () => _dexDetail.gameObject.SetActive(false));
        }

        Text _menuWallet;
        void BuildMenu(Transform parent)
        {
            _menu = UIFactory.Panel(parent, "MenuPanel", new Color(0.08f, 0.09f, 0.12f));
            UIFactory.Label(_menu, "TRAIN YOUR MONSTER", 56, new Vector2(0, 760), new Vector2(1000, 100), _font);
            UIFactory.Label(_menu, "Raise. Evolve. Conquer.", 28, new Vector2(0, 686), new Vector2(1000, 60), _font)
                .color = new Color(0.75f, 0.8f, 0.95f);
            _menuWallet = UIFactory.Label(_menu, "", 30, new Vector2(0, 612), new Vector2(1000, 50), _font);
            _menuWallet.color = new Color(1f, 0.85f, 0.35f);

            // Big PLAY, then a 2-column grid of the rest.
            UIFactory.Button(_menu, "PLAY", new Vector2(0, 500), new Vector2(560, 110), _font, () => _ctrl.StartGame());
            var grid = new (string, System.Action)[]
            {
                ("CAREER", () => _ctrl.ToCareer()),
                ("QUESTS", () => _ctrl.ToQuests()),
                ("DAILY", () => _ctrl.ToDaily()),
                ("COLLECTION", () => _ctrl.ToCollection()),
                ("MONSTER DEX", () => _ctrl.ToDex()),
                ("ACHIEVEMENTS", () => _ctrl.ToAchievements()),
                ("PROGRESS", () => _ctrl.ToProgress()),
                ("SETTINGS", () => _ctrl.ToSettings()),
            };
            for (int i = 0; i < grid.Length; i++)
            {
                float x = (i % 2 == 0) ? -200f : 200f;
                float y = 366f - (i / 2) * 116f;
                var b = UIFactory.Button(_menu, grid[i].Item1, new Vector2(x, y), new Vector2(380, 100), _font, grid[i].Item2);
                if (grid[i].Item1 == "QUESTS") _menuQuestBtn = b;
            }
            UIFactory.Button(_menu, "QUIT", new Vector2(0, -540), new Vector2(360, 88), _font, Quit);
            UIFactory.Label(_menu, "v" + Application.version, 24, new Vector2(0, -650), new Vector2(600, 44), _font)
                .color = new Color(0.5f, 0.5f, 0.6f);
        }

        void RefreshMenuBadges()
        {
            if (_menuWallet != null) _menuWallet.text = "Lv " + _profile.playerLevel + "     " + _profile.coins + " coins";
            if (_menuQuestBtn != null)
            {
                int ready = Quests.UnclaimedReady(_profile);
                var t = _menuQuestBtn.GetComponentInChildren<Text>();
                if (t != null) t.text = ready > 0 ? "QUESTS  (" + ready + ")" : "QUESTS";
            }
        }

        void BuildSelect(Transform parent, List<string> pool)
        {
            _select = UIFactory.Panel(parent, "SelectPanel", new Color(0.1f, 0.12f, 0.14f));
            UIFactory.Label(_select, "PICK YOUR TEAM", 44, new Vector2(0, 760), new Vector2(1000, 80), _font);
            _selectCount = UIFactory.Label(_select, "0 / 3", 32, new Vector2(0, 690), new Vector2(600, 50), _font);

            // 3-column compact grid (fits the full roster without overflowing START).
            int cols = 3;
            float cw = 330, ch = 108, gapx = 18, gapy = 14;
            float x0 = -(cols - 1) * (cw + gapx) / 2f, y0 = 560;
            for (int i = 0; i < pool.Count; i++)
            {
                string id = pool[i];
                int col = i % cols, row = i / cols;
                var pos = new Vector2(x0 + col * (cw + gapx), y0 - row * (ch + gapy));
                var b = UIFactory.Button(_select, "   " + Nice(id), pos, new Vector2(cw, ch), _font, () => OnPickSpecies(id));
                DecorateCard(b, id);
                _speciesButtons[id] = b;
            }

            _modeBtn = UIFactory.Button(_select, "MODE:  BRAWL", new Vector2(0, -560), new Vector2(440, 92), _font, ToggleBattleMode);
            UIFactory.SetButtonColor(_modeBtn, new Color(0.35f, 0.4f, 0.6f));
            _startBtn = UIFactory.Button(_select, "START BATTLE", new Vector2(0, -820), new Vector2(520, 110),
                _font, OnStartBattle);
            UIFactory.Label(_select, "Pick 1-3 monsters   ·   1v1 / 2v2 / 3v3", 24, new Vector2(0, 636), new Vector2(760, 40), _font)
                .color = new Color(0.7f, 0.72f, 0.8f);
            UIFactory.Button(_select, "BACK", new Vector2(0, -958), new Vector2(360, 88), _font,
                () => { if (_ctrl.InCareer) _ctrl.ToCareer(); else _ctrl.BackToMenu(); });
        }

        // Android hardware-back / Esc → phase-appropriate navigation.
        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (_popup != null && _popup.gameObject.activeSelf) { _popup.gameObject.SetActive(false); return; }
            if (_dexDetail != null && _dexDetail.gameObject.activeSelf) { _dexDetail.gameObject.SetActive(false); return; }
            switch (_ctrl.Flow.Phase)
            {
                case GamePhase.MainMenu: case GamePhase.Battle: case GamePhase.Onboarding: break;
                case GamePhase.Detail: _ctrl.ToCollection(); break;
                case GamePhase.About: _ctrl.ToSettings(); break;
                case GamePhase.TeamSelect: if (_ctrl.InCareer) _ctrl.ToCareer(); else _ctrl.BackToMenu(); break;
                case GamePhase.Result: _ctrl.ToMenu(); break;
                default: _ctrl.BackToMenu(); break;
            }
        }

        void BuildBattle(Transform parent)
        {
            _battle = UIFactory.Panel(parent, "BattlePanel", new Color(0.06f, 0.07f, 0.09f));
            UIFactory.Label(_battle, "BATTLE", 30, new Vector2(0, 860), new Vector2(600, 50), _font);
            _view = _battle.gameObject.AddComponent<BattleReplayView>();

            // Playback speed row (bottom).
            float[] speeds = { 0.5f, 1f, 2f, 4f };
            for (int i = 0; i < speeds.Length; i++)
            {
                float sp = speeds[i];
                var b = UIFactory.Button(_battle, sp + "x", new Vector2(-330 + i * 220, -860), new Vector2(200, 90),
                    _font, () => SetSpeed(sp));
                _speedButtons.Add(b);
            }
            SetSpeed(1f);
        }

        void SetSpeed(float sp)
        {
            if (_view != null) _view.SetSpeed(sp);
            for (int i = 0; i < _speedButtons.Count; i++)
            {
                bool active = Mathf.Approximately(new[] { 0.5f, 1f, 2f, 4f }[i], sp);
                UIFactory.SetButtonColor(_speedButtons[i], active ? new Color(0.2f, 0.75f, 0.35f) : new Color(0.2f, 0.4f, 0.7f));
            }
        }

        void BuildResult(Transform parent)
        {
            _result = UIFactory.Panel(parent, "ResultPanel", new Color(0.08f, 0.09f, 0.12f));
            _resultBanner = UIFactory.Label(_result, "-", 64, new Vector2(0, 560), new Vector2(1020, 200), _font);
            _mvpHolder = UIFactory.Panel(_result, "MvpHolder", new Color(0.14f, 0.15f, 0.2f, 0.9f));
            _mvpHolder.anchorMin = _mvpHolder.anchorMax = new Vector2(0.5f, 0.5f);
            _mvpHolder.sizeDelta = new Vector2(760, 150); _mvpHolder.anchoredPosition = new Vector2(0, 360);
            _resultStats = UIFactory.Label(_result, "", 28, new Vector2(0, 70), new Vector2(1000, 420), _font);
            _rewardText = UIFactory.Label(_result, "", 26, new Vector2(0, -260), new Vector2(1000, 200), _font);
            _rewardText.color = new Color(1f, 0.92f, 0.5f);
            UIFactory.Button(_result, "PLAY AGAIN", new Vector2(0, -470), new Vector2(460, 120), _font, () => _ctrl.PlayAgain());
            _resultContinueBtn = UIFactory.Button(_result, "CAREER MAP", new Vector2(0, -620), new Vector2(460, 100), _font, () => _ctrl.ToCareer());
            _resultContinueBtn.gameObject.SetActive(false);
            UIFactory.Button(_result, "BACK TO MENU", new Vector2(0, -770), new Vector2(460, 100), _font, () => _ctrl.ToMenu());
        }

        void OnPickSpecies(string id)
        {
            if (!_profile.IsUnlocked(id)) return;   // locked monsters not selectable
            _ctrl.ToggleSpecies(id);
            RefreshSelect();
        }

        // Brawl (all fight at once) vs Tag (only the front fights; next tags in on death).
        void ToggleBattleMode()
        {
            _ctrl.Session.tagMode = !_ctrl.Session.tagMode;
            RefreshSelect();
        }

        // Loss flavor from the ENEMY's win tier (so DEFEAT never reads a victory subtitle).
        static string LossFlavor(WinTier t)
        {
            switch (t)
            {
                case WinTier.DominantWin: return "Crushed";
                case WinTier.AdvantageWin: return "Outmatched";
                case WinTier.CloseWin: return "Close Loss";
                default: return "So Close...";   // ClutchWin
            }
        }

        void RefreshSelect()
        {
            int pc = _ctrl.Session.playerTeam.Count;
            _selectCount.text = pc + " / " + GameSession.TeamSize + (pc >= 1 ? "     " + pc + "v" + pc : "");
            foreach (var kv in _speciesButtons)
            {
                bool unlocked = _profile.IsUnlocked(kv.Key);
                bool picked = _ctrl.Session.playerTeam.Contains(kv.Key);
                kv.Value.interactable = unlocked;
                UIFactory.SetButtonColor(kv.Value, !unlocked ? new Color(0.22f, 0.22f, 0.26f)
                    : picked ? new Color(0.2f, 0.75f, 0.35f) : new Color(0.2f, 0.55f, 0.95f));
            }
            if (_startBtn != null) _startBtn.interactable = _ctrl.CanStartBattle;
            if (_modeBtn != null)
            {
                bool tag = _ctrl.Session.tagMode;
                var mt = _modeBtn.GetComponentInChildren<Text>();
                if (mt != null) mt.text = tag ? "MODE:  TAG (rotate)" : "MODE:  BRAWL (all)";
                UIFactory.SetButtonColor(_modeBtn, tag ? new Color(0.6f, 0.4f, 0.7f) : new Color(0.35f, 0.4f, 0.6f));
            }
        }

        Dictionary<string, int> BuildLevelMap()
        {
            var m = new Dictionary<string, int>();
            foreach (var ms in _profile.collection) m[ms.speciesId] = ms.level;
            return m;
        }

        void OnStartBattle()
        {
            _lastTeam = new List<string>(_ctrl.Session.playerTeam);
            _ctrl.Session.playerLevels = BuildLevelMap();   // player monsters fight at collection level
            var result = _ctrl.StartBattle();
            if (result == null) return;
            int csi = _ctrl.Session.careerStageIndex;       // boss music on each league-finale stage
            bool boss = csi >= 0 && (csi + 1) % Career.PerLeague == 0;
            var mode = (boss || _ctrl.Session.tagMode) ? MTA.Battle.BattleMode.Arena : MTA.Battle.BattleMode.Brawl;
            PlayBattleView(result, mode, boss);
        }

        // Shared battle-view setup + play (used by real battles and the showcase harness).
        void PlayBattleView(BattleResult result, MTA.Battle.BattleMode mode, bool boss)
        {
            var replay = ReplayBuilder.Build(result, _slotMap);
            _view.elementColors = _elemColors;            // element indicators on fighters
            _view.elementNames = _elemNames; _view.roleNames = _roleNames;   // procedural portraits
            _view.displayNames = _displayNames;           // Title Case names over fighters
            _view.rarities = _rarities;                   // VS-screen rarity stars
            _view.levelByKey = BuildLevelByKey();          // Lv badge on each fighter
            _view.bossMusic = boss;
            _view.mode = mode;
            _view.Play(result, replay, _atkStyles, _battle, _font);
        }

        // Per-fighter level keyed by (team,slot), surfaced as the in-battle Lv badge. Player monsters
        // fight at their saved collection level; enemies at the session's enemy level.
        Dictionary<int, int> BuildLevelByKey()
        {
            var m = new Dictionary<int, int>();
            var s = _ctrl.Session;
            for (int i = 0; i < s.playerTeam.Count; i++)
                m[0 * 100 + i] = (s.playerLevels != null && s.playerLevels.TryGetValue(s.playerTeam[i], out var l)) ? l : 5;
            if (s.enemyTeam != null)
                for (int i = 0; i < s.enemyTeam.Count; i++)
                    m[1 * 100 + i] = s.enemyLevel;
            return m;
        }

        // ===================== Visual-review showcase harness =====================
        static bool ShowcaseActive()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++) if (args[i] == "-showcase") return true;
            return false;
        }

        static bool UiShowcaseActive()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++) if (args[i] == "-uishowcase") return true;
            return false;
        }

        // Meta-UI review harness: visit each menu screen on a timer and screenshot it (no input).
        System.Collections.IEnumerator UiShowcaseRoutine()
        {
            var dir = System.IO.Path.Combine(Application.persistentDataPath, "uishowcase");
            try { System.IO.Directory.CreateDirectory(dir); } catch { }
            if (_loading != null) _loading.gameObject.SetActive(false);   // no loading overlay dimming/lagging the captures
            _detailSpecies = (_profile != null && _profile.collection != null && _profile.collection.Count > 0)
                ? _profile.collection[0].speciesId
                : (_ctrl.SpeciesPool.Count > 0 ? _ctrl.SpeciesPool[0] : "");
            yield return new WaitForSecondsRealtime(1.2f);
            _ctrl.BackToMenu();   yield return new WaitForSecondsRealtime(1.2f); UiCap(dir, "1_menu");       yield return new WaitForSecondsRealtime(0.6f);
            _ctrl.StartGame();    yield return new WaitForSecondsRealtime(1.2f); UiCap(dir, "2_teamselect"); yield return new WaitForSecondsRealtime(0.6f);
            _ctrl.ToCollection(); yield return new WaitForSecondsRealtime(1.2f); UiCap(dir, "3_collection"); yield return new WaitForSecondsRealtime(0.6f);
            _ctrl.ToDetail();     yield return new WaitForSecondsRealtime(1.2f); UiCap(dir, "4_detail");     yield return new WaitForSecondsRealtime(0.6f);
            _ctrl.ToCareer();     yield return new WaitForSecondsRealtime(1.2f); UiCap(dir, "5_career");     yield return new WaitForSecondsRealtime(0.6f);
            _ctrl.ToDaily();      yield return new WaitForSecondsRealtime(1.2f); UiCap(dir, "6_daily");      yield return new WaitForSecondsRealtime(0.6f);
            _ctrl.ToQuests();     yield return new WaitForSecondsRealtime(1.2f); UiCap(dir, "7_quests");     yield return new WaitForSecondsRealtime(0.6f);
            Debug.Log("UISHOWCASE_DONE dir=" + dir);
        }

        static void UiCap(string dir, string name)
        {
            try { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, name + ".png")); } catch { }
        }

        System.Collections.IEnumerator ShowcaseRoutine()
        {
            var A = MTA.Battle.BattleMode.Arena; var Br = MTA.Battle.BattleMode.Brawl;
            var dir = System.IO.Path.Combine(Application.persistentDataPath, "showcase");
            try { System.IO.Directory.CreateDirectory(dir); } catch { }
            // Record the final mixed audio to a WAV so the SFX + element identity can be VERIFIED (P4).
            MTA.Battle.AudioCapture cap = null;
            var al = FindObjectOfType<AudioListener>();
            if (al != null) { cap = al.gameObject.AddComponent<MTA.Battle.AudioCapture>(); cap.Begin(200f); }
            var scenes = new (string name, string[] p, string[] e, bool tag, MTA.Battle.BattleMode mode, int seed, int enemyLevel)[]
            {
                ("1_arena_1v1", new[]{"fire_lizard"}, new[]{"jelly"}, false, A, 101, 12),
                ("2_arena_2v2", new[]{"fire_lizard","mantis"}, new[]{"jelly","golem"}, false, A, 202, 12),
                ("3_arena_3v3", new[]{"fire_lizard","mantis","ghost"}, new[]{"jelly","golem","dragonling"}, false, A, 303, 12),
                ("4_brawl_3v3", new[]{"salamander","treant","kraken"}, new[]{"phoenix","mantis","turtle"}, false, Br, 404, 12),
                ("5_tag_3v3",   new[]{"wolf","golem","ghost"}, new[]{"dire_wolf","turtle","dragonling"}, true, A, 505, 12),
                ("6_victory",   new[]{"fire_lizard","mantis","ghost"}, new[]{"jelly"}, false, A, 606, 2),   // fast stomp → victory hero screen
            };
            yield return new WaitForSecondsRealtime(1.5f);
            foreach (var s in scenes)
            {
                var lv = new Dictionary<string, int>();
                foreach (var id in s.p) lv[id] = 12;
                var result = _ctrl.StartShowcase(s.p, s.e, s.tag, s.seed, lv, s.enemyLevel);
                PlayBattleView(result, s.mode, false);
                _view.SetSpeed(1.6f);
                for (int b = 0; b < 20; b++)
                {
                    yield return new WaitForSecondsRealtime(1.4f);
                    try { ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, s.name + "_" + b.ToString("D2") + ".png")); } catch { }
                }
                yield return new WaitForSecondsRealtime(1.0f);
            }
            if (cap != null) cap.Write(System.IO.Path.Combine(dir, "showcase_audio.wav"));
            Debug.Log("SHOWCASE_DONE dir=" + dir);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
