using System;

namespace MTA.Core
{
    // Plain-C# mirrors of the ScriptableObject definitions. The simulator and
    // tests consume ONLY these, so Core never references UnityEngine assets.
    // Data-layer SOs convert themselves via ToData().

    [Serializable]
    public class SkillData
    {
        public string skillId;
        public string displayName;
        public SkillSlot slot;
        public Stat scalingStat = Stat.ATK;      // ATK or INT per spec
        public float powerMultiplier = 1f;
        public float cooldownSeconds = 0f;       // actives only
        public float chargeTime = 15f;           // ultimates: ready when clock >= chargeTime
        public EffectKind effect = EffectKind.Damage;
        public TargetRule targetRule = TargetRule.Enemy;
        public Stat affectedStat = Stat.ATK;     // buff/debuff only
        public float magnitudePercent = 0f;      // buff/debuff: +/- fraction, e.g. 0.20
        public float durationSeconds = 0f;       // buff/debuff only
    }

    [Serializable]
    public class SpeciesData
    {
        public string speciesId;                 // lowercase_snake, append-only
        public string displayName;
        public StatBlock baseStats;              // level-1 values
        public GrowthWeights growth;             // tendencies; instances roll grades
        public SkillData basicSkill, activeSkill, ultimateSkill;   // exactly 3
    }

    // Per-stat weighted distribution over GrowthTier (D..S).
    [Serializable]
    public class GrowthWeights
    {
        public float[][] perStat;                // [6][5]

        public static GrowthWeights Uniform()
        {
            var w = new GrowthWeights { perStat = new float[6][] };
            for (int i = 0; i < 6; i++) w.perStat[i] = new[] { 1f, 1f, 1f, 1f, 1f };
            return w;
        }

        // Consumes exactly ONE rng sample. RNG order contract lives in BattleSimulator.
        public GrowthTier Roll(int statIndex, Random rng)
        {
            float[] w = perStat[statIndex];
            float total = 0f;
            for (int i = 0; i < w.Length; i++) total += w[i];
            double pick = rng.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < w.Length; i++)
            {
                acc += w[i];
                if (pick < acc) return (GrowthTier)i;
            }
            return GrowthTier.B;                 // float edge fallback
        }
    }
}
