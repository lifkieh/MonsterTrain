using System.Collections.Generic;

namespace MTA.Core
{
    // Continuous SPD-driven timeline. Total tie ordering or determinism dies:
    // earliest nextActionTime, then higher BASE spd, then team A, then lower slot.
    public static class ActionTimeline
    {
        public static CombatUnit NextActor(BattleState s)
        {
            CombatUnit best = null;
            Consider(s.teamA, ref best);
            Consider(s.teamB, ref best);
            return best;
        }

        static void Consider(List<CombatUnit> units, ref CombatUnit best)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (!u.Alive) continue;
                if (best == null) { best = u; continue; }
                if (u.nextActionTime < best.nextActionTime) { best = u; continue; }
                if (u.nextActionTime > best.nextActionTime) continue;
                if (u.stats.spd > best.stats.spd) { best = u; continue; }
                if (u.stats.spd < best.stats.spd) continue;
                if (u.team < best.team) { best = u; continue; }
                if (u.team > best.team) continue;
                if (u.slot < best.slot) best = u;
            }
        }
    }
}
