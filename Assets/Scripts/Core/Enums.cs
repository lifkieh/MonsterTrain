namespace MTA.Core
{
    public enum Stat { HP, ATK, DEF, SPD, INT, LUCK }

    public enum GrowthTier { D, C, B, A, S }   // ordered worst → best on purpose

    public enum SkillSlot { Basic, Active, Ultimate }

    public enum EffectKind { Damage, Heal, Buff, Debuff }

    // AllAllies exists only for team ultimates (e.g. rally). Everything else
    // resolves to a single unit via TargetSelector's deterministic rules.
    public enum TargetRule { Enemy, Ally, Self, AllAllies }

    public enum EndReason { Elimination, HardResolve }

    public enum TrainingType { Strength, Endurance, Agility, Intelligence }
}
