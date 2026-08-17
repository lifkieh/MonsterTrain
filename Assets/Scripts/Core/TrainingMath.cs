namespace MTA.Core
{
    // Training = permanent gains routed through the hidden growth grade.
    // gain = baseYield[type] x tierMultiplier[grade]   (grade discovery lives here)
    // Phase 1 note: freshness decay is a product-layer choice mechanic and is
    // deliberately absent; it must not affect balance-model validation.
    public static class TrainingMath
    {
        public static Stat StatFor(TrainingType t) => t switch
        {
            TrainingType.Strength => Stat.ATK,
            TrainingType.Endurance => Stat.HP,
            TrainingType.Agility => Stat.SPD,
            _ => Stat.INT
        };

        public static int Gain(TrainingType type, GrowthTier grade, BalanceConfig c) =>
            StatMath.RoundStat(c.trainingBaseYields[(int)type] * c.TierMultiplier(grade));

        public static int ApplySession(MonsterInstance inst, TrainingType type, BalanceConfig c)
        {
            var stat = StatFor(type);
            int gain = Gain(type, inst.growth[(int)stat], c);
            inst.trained.Add(stat, gain);
            return gain;                          // callers surface this ("+4 ATK!")
        }
    }
}
