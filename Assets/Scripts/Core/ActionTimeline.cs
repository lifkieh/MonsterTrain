using System.Collections.Generic;

namespace MTA.Core
{
    // Continuous SPD-driven timeline. Total tie ordering or determinism dies:
    // earliest nextActionTime, then higher BASE spd, then higher initiativeKey
    // (seed-derived, team-neutral), then lower slot, then lower team as a
    // hash-collision-only last resort. The old "team A first" bias is gone.
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
                if (u.initiativeKey > best.initiativeKey) { best = u; continue; }
                if (u.initiativeKey < best.initiativeKey) continue;
                if (u.slot < best.slot) { best = u; continue; }
                if (u.slot > best.slot) continue;
                if (u.team < best.team) best = u;   // hash-collision-only fallback
            }
        }
    }
}
