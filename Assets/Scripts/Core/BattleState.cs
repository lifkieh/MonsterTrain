using System.Collections.Generic;

namespace MTA.Core
{
    public class StatModifier
    {
        public Stat stat;
        public float percent;                    // +0.20 buff, -0.20 debuff
        public double expiresAt;
    }

    public class CombatUnit
    {
        public int team, slot;                   // slot 1..3 stored as 0..2; slot 0 = front
        public string speciesId, displayName;
        public string element = "";              // elemental triangle (Fire/Water/Nature)
        public StatBlock stats;                  // effective base (level+alloc+training)
        public int maxHp, currentHp;
        public SkillData[] skills;               // [basic, active, ultimate]
        public double[] cooldownReadyAt = { 0, 0, 0 };
        public bool ultimateUsed;
        public double nextActionTime;
        // Deterministic, seed-derived per-unit tie-break key (no RNG draws).
        // Replaces the old "team A first" bias; fair across a mirror. Set at build.
        public ulong initiativeKey;
        public List<StatModifier> modifiers = new List<StatModifier>();

        public bool Alive => currentHp > 0;

        public void PurgeExpired(double now) =>
            modifiers.RemoveAll(m => m.expiresAt <= now);

        // Buffs/debuffs apply at read time; SPD changes therefore affect only
        // the NEXT interval, never retroactive rescheduling (spec).
        public int EffectiveStat(Stat s)
        {
            float mult = 1f;
            for (int i = 0; i < modifiers.Count; i++)
                if (modifiers[i].stat == s) mult += modifiers[i].percent;
            int value = StatMath.RoundStat(stats.Get(s) * mult);
            return value < 0 ? 0 : value;
        }
    }

    public class BattleState
    {
        public List<CombatUnit> teamA = new List<CombatUnit>();
        public List<CombatUnit> teamB = new List<CombatUnit>();
        public double clock;

        public List<CombatUnit> Team(int id) => id == 0 ? teamA : teamB;
        public List<CombatUnit> Enemies(int id) => id == 0 ? teamB : teamA;

        public bool TeamWiped(int id)
        {
            var team = Team(id);
            for (int i = 0; i < team.Count; i++) if (team[i].Alive) return false;
            return true;
        }
    }
}
