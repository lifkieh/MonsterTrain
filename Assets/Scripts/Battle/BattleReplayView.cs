using System;
using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Plays back a BattleResult event log with placeholder visuals. Consumes the
    // log only (via BattlePlayback) — never re-simulates. Fires OnFinished(winner).
    public class BattleReplayView : MonoBehaviour
    {
        public float playbackSpeed = 1.5f;      // sim-seconds per real second
        public event Action<int> OnFinished;    // winnerTeam (0 = player)

        readonly BattlePlayback _pb = new BattlePlayback();
        readonly Dictionary<int, UnitView> _views = new Dictionary<int, UnitView>();
        RectTransform _root;
        Font _font;
        double _clock;
        bool _playing;
        bool _finishedFired;

        static int Key(int team, int slot) => team * 100 + slot;

        public void Play(BattleResult result, RectTransform uiParent, Font font)
        {
            _font = font;
            _root = uiParent;
            _clock = 0;
            _playing = true;
            _finishedFired = false;
            _pb.Init(result);
            BuildViews();
        }

        void BuildViews()
        {
            foreach (var kv in _views) if (kv.Value != null) Destroy(kv.Value.gameObject);
            _views.Clear();

            var size = new Vector2(200, 120);
            foreach (var u in _pb.Units)
            {
                // Team 0 (player) on the left, team 1 on the right; stacked by slot.
                float x = u.team == 0 ? -320f : 320f;
                float y = 180f - u.slot * 150f;
                var color = u.team == 0 ? new Color(0.25f, 0.45f, 0.8f) : new Color(0.8f, 0.35f, 0.3f);
                var view = new GameObject("UnitView").AddComponent<UnitView>();
                view.transform.SetParent(_root, false);
                view.Build(_root, new Vector2(x, y), size, color, _font,
                    u.speciesId + " (" + u.maxHp + ")");
                view.SetMaxHp(u.maxHp);
                view.SetHp(u.currentHp);
                _views[Key(u.team, u.slot)] = view;
            }
        }

        void Update()
        {
            if (!_playing) return;
            _clock += Time.deltaTime * playbackSpeed;
            _pb.ProcessUpTo(_clock);

            foreach (var e in _pb.JustApplied)
            {
                if (_views.TryGetValue(Key(e.targetTeam, e.targetSlot), out var v))
                {
                    bool heal = e.actorTeam == e.targetTeam;
                    v.FlashDamage(e.final, e.crit, heal, _font);
                }
            }
            foreach (var u in _pb.Units)
                if (_views.TryGetValue(Key(u.team, u.slot), out var v)) v.SetHp(u.currentHp);

            if (!_finishedFired && (_pb.Finished || _clock >= _pb.Duration + 0.5))
            {
                _finishedFired = true;
                _playing = false;
                OnFinished?.Invoke(_pb.WinnerTeam);
            }
        }
    }
}
