using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    // Species-specific attack style, derived from base stats + basic-skill scaling.
    // Pure presentation classification (no gameplay effect); deterministic + testable.
    public enum AttackStyle { MeleeLunge, HeavySmash, RangedProjectile, AssassinDash, MageCast }

    public static class AttackStyles
    {
        public static AttackStyle For(SpeciesData sp)
        {
            var b = sp.baseStats;
            bool intBasic = sp.basicSkill != null && sp.basicSkill.scalingStat == Stat.INT;
            if (intBasic || b.intel >= 18) return AttackStyle.MageCast;         // caster → projectile
            if (b.spd >= 20 && b.atk < 18) return AttackStyle.RangedProjectile;  // fast + light → ranged
            if (b.spd >= 17) return AttackStyle.AssassinDash;                    // fast → dash
            if (b.atk >= 20) return AttackStyle.HeavySmash;                      // heavy hitter → smash
            return AttackStyle.MeleeLunge;                                       // default
        }

        public static Dictionary<string, AttackStyle> Map(IEnumerable<SpeciesData> species)
        {
            var m = new Dictionary<string, AttackStyle>();
            foreach (var s in species) m[s.speciesId] = For(s);
            return m;
        }

        public static bool IsRanged(AttackStyle s) =>
            s == AttackStyle.MageCast || s == AttackStyle.RangedProjectile;
    }
}
