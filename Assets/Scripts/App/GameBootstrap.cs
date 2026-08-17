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

        RectTransform _menu, _select, _battle, _result, _progress;
        Text _selectCount, _resultBanner, _resultStats, _progressText, _rewardText;
        RectTransform _mvpHolder;
        SaveData _profile;
        List<string> _roster;
        List<string> _lastTeam = new List<string>();
        bool _hadSave;
        Button _startBtn, _muteBtn;
        BattleReplayView _view;
        Dictionary<string, SkillSlot> _slotMap;
        Dictionary<string, AttackStyle> _atkStyles;
        readonly Dictionary<string, Button> _speciesButtons = new Dictionary<string, Button>();
        readonly List<Button> _speedButtons = new List<Button>();

        void Start()
        {
            _font = UIFactory.DefaultFont();
            _reg = SpeciesDatabase.LoadFromResources();
            var cfg = SpeciesDatabase.LoadBalance();

            var pool = new List<string>();
            foreach (var s in _reg.All) pool.Add(s.speciesId);
            pool.Sort(System.StringComparer.Ordinal);
            _roster = pool;
            _ctrl = new GameController(_reg, cfg, pool, seedBase: 20260817);
            _slotMap = ReplayBuilder.SlotMap(_reg.All);   // skillId -> slot, for replay classification
            _atkStyles = AttackStyles.Map(_reg.All);      // species -> attack style (presentation)

            // Meta progression: load or create the player profile.
            _hadSave = SaveSystem.Exists();
            _profile = SaveSystem.Load() ?? Progression.NewGame(pool);
            if (!_hadSave) SaveSystem.Save(_profile);

            MTA.Battle.AudioManager.Ensure();                       // audio feedback
            MTA.Battle.AudioManager.Muted = _profile.muted;

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

            _ctrl.Flow.OnPhaseChanged += OnPhase;
            _view.OnFinished += _ => _ctrl.OnBattleFinished();
            OnPhase(_ctrl.Flow.Phase);
        }

        void OnPhase(GamePhase p)
        {
            _menu.gameObject.SetActive(p == GamePhase.MainMenu);
            _select.gameObject.SetActive(p == GamePhase.TeamSelect);
            _battle.gameObject.SetActive(p == GamePhase.Battle);
            _result.gameObject.SetActive(p == GamePhase.Result);
            _progress.gameObject.SetActive(p == GamePhase.Progress);
            if (p == GamePhase.TeamSelect) RefreshSelect();
            if (p == GamePhase.Result) ShowResult();
            if (p == GamePhase.Progress) RefreshProgress();
        }

        void ShowResult()
        {
            bool won = _ctrl.PlayerWon;
            var r = _ctrl.Session.lastResult;
            var d = r != null ? BattleDrama.Compute(r) : null;
            _resultBanner.text = (won ? "VICTORY" : "DEFEAT") + (d != null ? "\n" + d.bannerTitle : "");
            _resultBanner.color = won ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.5f);
            _resultStats.text = d == null ? "" :
                "Winner: " + (d.winnerTeam == 0 ? "You" : "Enemy") + "\n" +
                "Battle Duration: " + d.duration.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " s\n" +
                "Survivors: " + d.winnerAlive + " vs " + d.loserAlive + "\n\n" +
                "Damage Leader: " + d.damageLeader + "\n" +
                "Kills Leader: " + d.killsLeader + "\n" +
                "Healing Leader: " + d.healingLeader;

            // MVP showcase (top damage dealer).
            if (_mvpHolder != null)
            {
                for (int i = _mvpHolder.childCount - 1; i >= 0; i--) Destroy(_mvpHolder.GetChild(i).gameObject);
                if (d != null && !string.IsNullOrEmpty(d.mvpSpecies))
                {
                    IconBadge(_mvpHolder, d.mvpSpecies, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), 110, 44);
                    UIFactory.Label(_mvpHolder, "MVP:  " + d.mvpSpecies + "  (" + (d.mvpTeam == 0 ? "You" : "Enemy") + ")",
                        34, new Vector2(70, 0), new Vector2(660, 130), _font);
                }
            }

            // Meta progression: award XP/coins/levels/unlocks for this battle and save.
            var rw = Progression.ApplyBattle(_profile, _lastTeam, won, _roster);
            SaveSystem.Save(_profile);
            string rt = "Rewards:  +" + rw.playerXp + " XP    +" + rw.coins + " coins";
            if (rw.playerLevelsGained > 0) rt += "    PLAYER LEVEL UP!";
            if (rw.leveledUp.Count > 0) rt += "\nLeveled up: " + string.Join(", ", rw.leveledUp);
            if (rw.newlyUnlocked.Count > 0) rt += "\nUnlocked: " + string.Join(", ", rw.newlyUnlocked);
            _rewardText.text = rt;
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
            IconBadge(rt, id, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), 72, 30);
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
                    s += "  " + id + "   Lv " + m.level + "   (" + m.xp + "/" + Progression.MonsterXpForNext(m.level) + ")\n";
                else
                    s += "  [LOCKED]  " + id + "\n";
            }
            _progressText.text = s;
        }

        void BuildMenu(Transform parent)
        {
            _menu = UIFactory.Panel(parent, "MenuPanel", new Color(0.08f, 0.09f, 0.12f));
            UIFactory.Label(_menu, "TRAIN YOUR MONSTER", 56, new Vector2(0, 400), new Vector2(1000, 100), _font);
            UIFactory.Label(_menu, "first playable", 28, new Vector2(0, 320), new Vector2(1000, 60), _font);
            UIFactory.Button(_menu, "PLAY", new Vector2(0, 80), new Vector2(400, 110), _font, () => _ctrl.StartGame());
            if (_hadSave)
                UIFactory.Button(_menu, "CONTINUE", new Vector2(0, -60), new Vector2(400, 100), _font, () => _ctrl.StartGame());
            UIFactory.Button(_menu, "PROGRESS", new Vector2(0, -200), new Vector2(400, 100), _font, () => _ctrl.ToProgress());
            _muteBtn = UIFactory.Button(_menu, MuteLabel(), new Vector2(0, -340), new Vector2(400, 90), _font, ToggleMute);
            UIFactory.Button(_menu, "QUIT", new Vector2(0, -480), new Vector2(400, 90), _font, Quit);
        }

        void BuildSelect(Transform parent, List<string> pool)
        {
            _select = UIFactory.Panel(parent, "SelectPanel", new Color(0.1f, 0.12f, 0.14f));
            UIFactory.Label(_select, "PICK 3 MONSTERS", 44, new Vector2(0, 760), new Vector2(1000, 80), _font);
            _selectCount = UIFactory.Label(_select, "0 / 3", 32, new Vector2(0, 690), new Vector2(600, 50), _font);

            // 2-column grid of species buttons.
            int cols = 2;
            float cw = 460, ch = 120, gapx = 40, gapy = 24;
            float x0 = -(cw + gapx) / 2f, y0 = 560;
            for (int i = 0; i < pool.Count; i++)
            {
                string id = pool[i];
                var sp = _reg.Get(id);
                int col = i % cols, row = i / cols;
                var pos = new Vector2(x0 + col * (cw + gapx), y0 - row * (ch + gapy));
                var b = UIFactory.Button(_select, "   " + id + "\n   HP" + sp.baseStats.hp + " ATK" + sp.baseStats.atk +
                    " SPD" + sp.baseStats.spd, pos, new Vector2(cw, ch), _font, () => OnPickSpecies(id));
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
            UIFactory.Button(_result, "BACK TO MENU", new Vector2(0, -640), new Vector2(460, 100), _font, () => _ctrl.ToMenu());
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
