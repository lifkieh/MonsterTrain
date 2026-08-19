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
        BalanceConfig _cfg;
        SaveData _profile;
        List<string> _roster, _obtainable;
        List<string> _lastTeam = new List<string>();
        bool _hadSave;
        Button _startBtn, _muteBtn;
        BattleReplayView _view;
        Dictionary<string, SkillSlot> _slotMap;
        Dictionary<string, AttackStyle> _atkStyles;
        Dictionary<string, Color> _elemColors = new Dictionary<string, Color>();
        Dictionary<string, string> _elemNames = new Dictionary<string, string>();
        Dictionary<string, string> _roleNames = new Dictionary<string, string>();
        Dictionary<string, string> _displayNames = new Dictionary<string, string>();
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
            }

            // Meta progression: load or create the player profile.
            _hadSave = SaveSystem.Exists();
            _profile = SaveSystem.Load() ?? Progression.NewGame(_obtainable);
            if (!_hadSave) SaveSystem.Save(_profile);

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
            BuildPopup(canvas.transform);
            BuildLoading(canvas.transform);

            _ctrl.Flow.OnPhaseChanged += OnPhase;
            _view.OnFinished += _ => _ctrl.OnBattleFinished();
            OnPhase(_ctrl.Flow.Phase);
            // Retention: greet the player with their daily reward on launch.
            if (DailyRewards.CanClaim(_profile, System.DateTime.Now)) _ctrl.ToDaily();
            StartCoroutine(HideLoading());   // brief branded loading screen
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

            _resultBanner.text = (won ? "VICTORY" : "DEFEAT") + (d != null ? "\n" + d.bannerTitle : "");
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
            // Meta progression: award XP/coins/levels/unlocks for this battle and save.
            var rw = Progression.ApplyBattle(_profile, _lastTeam, won, _obtainable);
            SaveSystem.Save(_profile);
            string rt = "Rewards:  +" + rw.playerXp + " XP    +" + rw.coins + " coins";
            if (rw.playerLevelsGained > 0) rt += "    PLAYER LEVEL UP!";
            if (rw.leveledUp.Count > 0) rt += "\nLeveled up: " + string.Join(", ", rw.leveledUp);
            if (rw.newlyUnlocked.Count > 0) rt += "\nUnlocked: " + string.Join(", ", rw.newlyUnlocked);

            // Career: record a stage clear (first clear pays the reward once).
            int stageIdx = _ctrl.Session.careerStageIndex;
            bool career = stageIdx >= 0;
            if (_resultContinueBtn != null) _resultContinueBtn.gameObject.SetActive(career);
            if (career && won)
            {
                long bonus = Career.ClearStage(_profile, stageIdx, _stages[stageIdx].reward);
                if (bonus > 0)
                {
                    SaveSystem.Save(_profile);
                    rt += "\nSTAGE CLEARED!  +" + bonus + " coins   (" + Career.CompletionPercent(_profile) + "%)";
                    if (Career.IsComplete(_profile)) rt += "\nCAREER COMPLETE!";
                }
            }

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

        void BuildProgress(Transform parent)
        {
            _progress = UIFactory.Panel(parent, "ProgressPanel", new Color(0.09f, 0.1f, 0.13f));
            UIFactory.Label(_progress, "PROGRESS", 48, new Vector2(0, 840), new Vector2(900, 90), _font);
            _progressText = UIFactory.Label(_progress, "", 28, new Vector2(0, -30), new Vector2(980, 1560), _font);
            _progressText.alignment = TextAnchor.UpperLeft;
            UIFactory.Button(_progress, "BACK", new Vector2(0, -880), new Vector2(400, 100), _font, () => _ctrl.BackToMenu());
        }

        void RefreshProgress()
        {
            var d = _profile;
            string s = "Player: " + d.playerName + "    Level " + d.playerLevel + "\n" +
                       "XP: " + d.playerXp + " / " + Progression.PlayerXpForNext(d.playerLevel) + "\n" +
                       "Coins: " + d.coins + "    Battles won: " + d.battlesWon + " / " + d.battlesPlayed + "\n\n" +
                       "COLLECTION  (" + d.unlocked.Count + " / " + _roster.Count + " unlocked)\n";
            foreach (var id in _roster)
            {
                var m = d.Find(id);
                if (d.IsUnlocked(id) && m != null)
                    s += "  " + Nice(id) + "   Lv " + m.level + "   (" + m.xp + "/" + Progression.MonsterXpForNext(m.level) + ")\n";
                else
                    s += "  [LOCKED]  " + Nice(id) + "\n";
            }
            _progressText.text = s;
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
            SaveSystem.Save(_profile);
            _detailSpecies = evo;                            // now viewing the evolved form
            MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.Evolution);
            ShowPopup("EVOLUTION!", (sp != null ? sp.displayName : _detailSpecies) + "  evolved into\n" + _reg.Get(evo).displayName + "!");
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
            SaveSystem.Save(_profile);
            if (gained > 0) { MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.LevelUp); var m = _profile.Find(_detailSpecies); ShowPopup("LEVEL UP!", Nice(_detailSpecies) + "  reached  Lv " + (m != null ? m.level : 1)); }
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
            _dailyInfo.text = "Login streak:  " + _profile.loginStreak + " day" + (_profile.loginStreak == 1 ? "" : "s") + "\n" +
                "Coins:  " + _profile.coins + "\n\n" +
                (can ? "Today's reward:  +" + preview + " coins" : "Claimed today — come back tomorrow!");
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
                SaveSystem.Save(_profile);
                MTA.Battle.AudioManager.Play(MTA.Battle.Sfx.Reward);
                ShowPopup("DAILY REWARD", "Day " + r.streak + " streak!\n+" + r.coins + " coins" +
                    (r.streakReset ? "\n(streak restarted)" : ""));
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
                "Engine:  Unity " + Application.unityVersion + "\n" +
                "Battle simulation:  MTA deterministic core\n\n" +
                "MVP soft-launch candidate.",
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

        void ShowNewMonster(List<string> ids) => ShowPopup("NEW MONSTER!", string.Join("\n", ids));

        void BuildMenu(Transform parent)
        {
            _menu = UIFactory.Panel(parent, "MenuPanel", new Color(0.08f, 0.09f, 0.12f));
            UIFactory.Label(_menu, "TRAIN YOUR MONSTER", 56, new Vector2(0, 540), new Vector2(1000, 100), _font);
            UIFactory.Label(_menu, "first playable", 28, new Vector2(0, 462), new Vector2(1000, 60), _font);
            UIFactory.Button(_menu, "PLAY", new Vector2(0, 350), new Vector2(400, 96), _font, () => _ctrl.StartGame());
            UIFactory.Button(_menu, "CAREER", new Vector2(0, 238), new Vector2(400, 96), _font, () => _ctrl.ToCareer());
            UIFactory.Button(_menu, "DAILY", new Vector2(0, 126), new Vector2(400, 96), _font, () => _ctrl.ToDaily());
            if (_hadSave)
                UIFactory.Button(_menu, "CONTINUE", new Vector2(0, 14), new Vector2(400, 96), _font, () => _ctrl.StartGame());
            UIFactory.Button(_menu, "PROGRESS", new Vector2(0, -98), new Vector2(400, 96), _font, () => _ctrl.ToProgress());
            UIFactory.Button(_menu, "COLLECTION", new Vector2(0, -210), new Vector2(400, 96), _font, () => _ctrl.ToCollection());
            UIFactory.Button(_menu, "SETTINGS", new Vector2(0, -322), new Vector2(400, 96), _font, () => _ctrl.ToSettings());
            UIFactory.Button(_menu, "QUIT", new Vector2(0, -434), new Vector2(400, 90), _font, Quit);
            UIFactory.Label(_menu, "v" + Application.version, 24, new Vector2(0, -560), new Vector2(600, 44), _font)
                .color = new Color(0.55f, 0.55f, 0.65f);
        }

        void BuildSelect(Transform parent, List<string> pool)
        {
            _select = UIFactory.Panel(parent, "SelectPanel", new Color(0.1f, 0.12f, 0.14f));
            UIFactory.Label(_select, "PICK 3 MONSTERS", 44, new Vector2(0, 760), new Vector2(1000, 80), _font);
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

            _startBtn = UIFactory.Button(_select, "START BATTLE", new Vector2(0, -820), new Vector2(520, 110),
                _font, OnStartBattle);
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

        void RefreshSelect()
        {
            _selectCount.text = _ctrl.Session.playerTeam.Count + " / " + GameSession.TeamSize;
            foreach (var kv in _speciesButtons)
            {
                bool unlocked = _profile.IsUnlocked(kv.Key);
                bool picked = _ctrl.Session.playerTeam.Contains(kv.Key);
                kv.Value.interactable = unlocked;
                UIFactory.SetButtonColor(kv.Value, !unlocked ? new Color(0.22f, 0.22f, 0.26f)
                    : picked ? new Color(0.2f, 0.75f, 0.35f) : new Color(0.2f, 0.55f, 0.95f));
            }
            if (_startBtn != null) _startBtn.interactable = _ctrl.CanStartBattle;
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
            var replay = ReplayBuilder.Build(result, _slotMap);
            _view.elementColors = _elemColors;            // element indicators on fighters
            _view.elementNames = _elemNames; _view.roleNames = _roleNames;   // procedural portraits
            _view.displayNames = _displayNames;           // Title Case names over fighters
            _view.Play(result, replay, _atkStyles, _battle, _font);
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
