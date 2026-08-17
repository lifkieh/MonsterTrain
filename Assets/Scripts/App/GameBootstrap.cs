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

        RectTransform _menu, _select, _battle, _result;
        Text _selectCount, _resultBanner;
        Button _startBtn;
        BattleReplayView _view;
        readonly Dictionary<string, Button> _speciesButtons = new Dictionary<string, Button>();

        void Start()
        {
            _font = UIFactory.DefaultFont();
            _reg = SpeciesDatabase.LoadFromResources();
            var cfg = SpeciesDatabase.LoadBalance();

            var pool = new List<string>();
            foreach (var s in _reg.All) pool.Add(s.speciesId);
            pool.Sort(System.StringComparer.Ordinal);
            _ctrl = new GameController(_reg, cfg, pool, seedBase: 20260817);

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var canvas = UIFactory.Canvas("GameCanvas");
            BuildMenu(canvas.transform);
            BuildSelect(canvas.transform, pool);
            BuildBattle(canvas.transform);
            BuildResult(canvas.transform);

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
            if (p == GamePhase.TeamSelect) RefreshSelect();
            if (p == GamePhase.Result)
            {
                _resultBanner.text = _ctrl.PlayerWon ? "VICTORY" : "DEFEAT";
                _resultBanner.color = _ctrl.PlayerWon ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.5f, 0.5f);
            }
        }

        void BuildMenu(Transform parent)
        {
            _menu = UIFactory.Panel(parent, "MenuPanel", new Color(0.08f, 0.09f, 0.12f));
            UIFactory.Label(_menu, "TRAIN YOUR MONSTER", 56, new Vector2(0, 400), new Vector2(1000, 100), _font);
            UIFactory.Label(_menu, "first playable", 28, new Vector2(0, 320), new Vector2(1000, 60), _font);
            UIFactory.Button(_menu, "PLAY", new Vector2(0, 0), new Vector2(360, 110), _font, () => _ctrl.StartGame());
            UIFactory.Button(_menu, "QUIT", new Vector2(0, -160), new Vector2(360, 90), _font, Quit);
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
                var b = UIFactory.Button(_select, id + "\nHP" + sp.baseStats.hp + " ATK" + sp.baseStats.atk +
                    " SPD" + sp.baseStats.spd, pos, new Vector2(cw, ch), _font, () => OnPickSpecies(id));
                _speciesButtons[id] = b;
            }

            _startBtn = UIFactory.Button(_select, "START BATTLE", new Vector2(0, -820), new Vector2(520, 110),
                _font, OnStartBattle);
        }

        void BuildBattle(Transform parent)
        {
            _battle = UIFactory.Panel(parent, "BattlePanel", new Color(0.06f, 0.07f, 0.09f));
            UIFactory.Label(_battle, "BATTLE", 34, new Vector2(0, 820), new Vector2(600, 60), _font);
            _view = _battle.gameObject.AddComponent<BattleReplayView>();
        }

        void BuildResult(Transform parent)
        {
            _result = UIFactory.Panel(parent, "ResultPanel", new Color(0.08f, 0.09f, 0.12f));
            _resultBanner = UIFactory.Label(_result, "-", 72, new Vector2(0, 300), new Vector2(1000, 120), _font);
            UIFactory.Button(_result, "PLAY AGAIN", new Vector2(0, 0), new Vector2(420, 110), _font, () => _ctrl.PlayAgain());
            UIFactory.Button(_result, "MENU", new Vector2(0, -160), new Vector2(420, 90), _font, () => _ctrl.ToMenu());
        }

        void OnPickSpecies(string id)
        {
            _ctrl.ToggleSpecies(id);
            RefreshSelect();
        }

        void RefreshSelect()
        {
            _selectCount.text = _ctrl.Session.playerTeam.Count + " / " + GameSession.TeamSize;
            foreach (var kv in _speciesButtons)
            {
                bool picked = _ctrl.Session.playerTeam.Contains(kv.Key);
                UIFactory.SetButtonColor(kv.Value, picked ? new Color(0.2f, 0.75f, 0.35f) : new Color(0.2f, 0.55f, 0.95f));
            }
            if (_startBtn != null) _startBtn.interactable = _ctrl.CanStartBattle;
        }

        void OnStartBattle()
        {
            var result = _ctrl.StartBattle();
            if (result == null) return;
            _view.Play(result, _battle, _font);
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
