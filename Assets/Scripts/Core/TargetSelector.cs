using System.Collections.Generic;

namespace MTA.Core
{
    // All targeting rules, all deterministic, all tie-broken by lower slot. (spec)
    public static class TargetSelector
    {
        // Basic attacks: front-most living enemy (lowest slot).
        public static CombatUnit FrontMost(List<CombatUnit> enemies)
        {
            CombatUnit best = null;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].Alive && (best == null || enemies[i].slot < best.slot))
                    best = enemies[i];
            return best;
        }

        // Damage skills / debuffs: lowest current HP, tie -> lower slot.
        public static CombatUnit LowestHpEnemy(List<CombatUnit> enemies)
        {
            CombatUnit best = null;
            for (int i = 0; i < enemies.Count; i++)
            {
                var u = enemies[i];
                if (!u.Alive) continue;
                if (best == null || u.currentHp < best.currentHp ||
                    (u.currentHp == best.currentHp && u.slot < best.slot))
                    best = u;
            }
            return best;
        }

        // Heals: lowest HP-percentage living ally, excluding full-HP allies.
        public static CombatUnit MostInjuredAlly(List<CombatUnit> allies)
        {
            CombatUnit best = null;
            double bestPct = 1.0;
            for (int i = 0; i < allies.Count; i++)
            {
                var u = allies[i];
                if (!u.Alive || u.currentHp >= u.maxHp) continue;
                double pct = u.currentHp / (double)u.maxHp;
                if (best == null || pct < bestPct ||
                    (pct == bestPct && u.slot < best.slot))
                { best = u; bestPct = pct; }
            }
            return best;                          // null => nobody injured
        }
    }
}
