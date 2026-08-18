using System;

namespace MTA.Core
{
    // THE single source of truth for stat, damage, and rate math.
    // Nothing else in the codebase computes these. (code-conventions.md)
    public static class StatMath
    {
        public static int RoundStat(double x) =>
            (int)Math.Round(x, MidpointRounding.AwayFromZero);

        public static float Mitigation(int def, BalanceConfig c) =>
            1f - def / (float)(def + c.k);

        public static float AttacksPerSecond(int spd, BalanceConfig c)
        {
            float aps = c.apsPerSpdLow * Math.Min(spd, c.spdKink)
                      + c.apsPerSpdHigh * Math.Max(0, spd - c.spdKink);
            return Math.Min(Math.Max(aps, 0.01f), c.apsCap);   // floor avoids /0
        }

        public static double ActionInterval(int spd, BalanceConfig c) =>
            1.0 / AttacksPerSecond(spd, c);

        public static float CritChance(int luck, BalanceConfig c) =>
            Math.Min(luck * c.critPerLuck, c.critCap);

        // Evade chance: a defensive use of LUCK (attacker LUCK erodes it), so LUCK
        // is a real two-sided stat and hits carry controlled miss variance.
        public static float DodgeChance(int attackerLuck, int defenderLuck, BalanceConfig c)
        {
            float d = c.dodgeBase + (defenderLuck - attackerLuck) * c.dodgePerLuck;
            return Math.Min(Math.Max(d, 0f), c.dodgeCap);
        }

        // Elemental triangle: Fire > Nature > Water > Fire. Advantage boosts damage,
        // disadvantage reduces it symmetrically; same/none = neutral. Deterministic
        // (no RNG), so it never touches the growth→crit→dodge→variance contract.
        public static float ElementMultiplier(string attacker, string defender, BalanceConfig c)
        {
            if (string.IsNullOrEmpty(attacker) || string.IsNullOrEmpty(defender) || attacker == defender) return 1f;
            bool adv = (attacker == "Fire" && defender == "Nature")
                    || (attacker == "Nature" && defender == "Water")
                    || (attacker == "Water" && defender == "Fire");
            if (adv) return 1f + c.elementAdvantage;
            bool dis = (defender == "Fire" && attacker == "Nature")
                    || (defender == "Nature" && attacker == "Water")
                    || (defender == "Water" && attacker == "Fire");
            if (dis) return 1f / (1f + c.elementAdvantage);
            return 1f;
        }

        public static float StallMultiplier(double t, BalanceConfig c) =>
            t <= c.antiStallStart
                ? 1f
                : 1f + c.antiStallIncrement *
                       (float)Math.Floor((t - c.antiStallStart) / c.antiStallInterval);

        // Per-level automatic gain for one stat: gainRate x growth-tier multiplier.
        public static int LevelGain(string speciesId, Stat s, GrowthTier g, BalanceConfig c) =>
            RoundStat(c.GainRate(speciesId, s) * c.TierMultiplier(g));

        // effective = base + levelGains*(level-1) + allocated + trained  (conventions)
        public static StatBlock EffectiveStats(MonsterInstance inst, SpeciesData sp, BalanceConfig c)
        {
            var result = new StatBlock();
            for (int i = 0; i < 6; i++)
            {
                var stat = (Stat)i;
                int value = sp.baseStats.Get(stat)
                          + LevelGain(sp.speciesId, stat, inst.growth[i], c) * (inst.level - 1)
                          + inst.allocated.Get(stat)
                          + inst.trained.Get(stat);
                result.Set(stat, value);
            }
            return result;
        }
    }
}
