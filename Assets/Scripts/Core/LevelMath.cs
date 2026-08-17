namespace MTA.Core
{
    // Phase 1 exposes leveling as a mechanic (debug triggers), not a feature.
    // XP wiring arrives with Build Phase 2; the math must not change then.
    public static class LevelMath
    {
        public static void ApplyLevelUp(MonsterInstance inst, BalanceConfig c, int levels = 1)
        {
            for (int i = 0; i < levels; i++)
            {
                inst.level++;
                inst.unspentPoints += c.statPointsPerLevel;   // 3 per level, per spec
            }
        }

        public static bool AllocatePoint(MonsterInstance inst, Stat stat)
        {
            if (inst.unspentPoints <= 0) return false;
            inst.unspentPoints--;
            inst.allocated.Add(stat, 1);
            return true;
        }
    }
}
