using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    // Pure-C# replay reconstruction: given a BattleResult event log, rebuilds
    // per-unit HP/alive at any point in time. The MonoBehaviour view renders
    // what this computes; this is edit-mode testable with no scene.
    public class BattlePlayback
    {
        public class Unit
        {
            public int team, slot, maxHp, currentHp;
            public string speciesId;
            public bool Alive => currentHp > 0;
        }

        readonly List<Unit> _units = new List<Unit>();
        readonly Dictionary<int, Unit> _byKey = new Dictionary<int, Unit>();
        List<BattleEvent> _events;
        int _idx;

        public IReadOnlyList<Unit> Units => _units;
        public bool Finished { get; private set; }
        public int WinnerTeam { get; private set; } = -1;
        public double Duration { get; private set; }
        public int DeathsProcessed { get; private set; }
        // Set on the frame an Action lands, so the view can flash it; cleared each ProcessUpTo.
        public readonly List<BattleEvent> JustApplied = new List<BattleEvent>();

        static int Key(int team, int slot) => team * 100 + slot;

        public void Init(BattleResult result)
        {
            _events = result.events;
            _idx = 0;
            Finished = false;
            WinnerTeam = -1;
            DeathsProcessed = 0;
            Duration = result.duration;
            _units.Clear();
            _byKey.Clear();
            foreach (var e in _events)
            {
                if (e.kind != "Spawn") continue;
                var u = new Unit {
                    team = e.actorTeam, slot = e.actorSlot,
                    maxHp = e.final <= 0 ? 1 : e.final, currentHp = e.final <= 0 ? 1 : e.final,
                    speciesId = e.extra };
                _units.Add(u);
                _byKey[Key(u.team, u.slot)] = u;
            }
        }

        // Advance the reconstruction to sim-time t (inclusive).
        public void ProcessUpTo(double t)
        {
            JustApplied.Clear();
            if (_events == null) return;
            while (_idx < _events.Count && _events[_idx].t <= t)
            {
                Apply(_events[_idx]);
                _idx++;
            }
        }

        public void ProcessAll() => ProcessUpTo(double.MaxValue);

        void Apply(BattleEvent e)
        {
            switch (e.kind)
            {
                case "Action":
                {
                    if (!_byKey.TryGetValue(Key(e.targetTeam, e.targetSlot), out var u)) break;
                    if (e.actorTeam == e.targetTeam)                 // heal (ally target)
                        u.currentHp = System.Math.Min(u.maxHp, u.currentHp + e.final);
                    else                                            // damage (enemy target)
                        u.currentHp = System.Math.Max(0, u.currentHp - e.final);
                    JustApplied.Add(e);
                    break;
                }
                case "Died":
                {
                    if (_byKey.TryGetValue(Key(e.targetTeam, e.targetSlot), out var u))
                        u.currentHp = 0;
                    DeathsProcessed++;
                    break;
                }
                case "End":
                    Finished = true;
                    WinnerTeam = e.actorTeam;
                    break;
                // Start / Spawn / Modifier / StallTick: no HP effect.
            }
        }

        public int AliveCount(int team)
        {
            int n = 0;
            for (int i = 0; i < _units.Count; i++)
                if (_units[i].team == team && _units[i].Alive) n++;
            return n;
        }
    }
}
