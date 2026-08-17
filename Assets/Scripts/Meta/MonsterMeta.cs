using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    public enum RoleTag { Tank, Bruiser, Assassin, Mage, Support }

    // Presentation metadata for the collection/encyclopedia: role + rarity,
    // derived from base stats. Pure C#, deterministic, testable.
    public static class MonsterMeta
    {
        public static RoleTag Role(SpeciesData sp)
        {
            var b = sp.baseStats;
            bool healer = sp.activeSkill != null && sp.activeSkill.effect == EffectKind.Heal;
            if (b.def >= 18 || (b.hp >= 120 && b.def >= 14)) return RoleTag.Tank;
            if (b.intel >= 18) return healer ? RoleTag.Support : RoleTag.Mage;
            if (b.spd >= 17) return RoleTag.Assassin;
            if (b.atk >= 20) return RoleTag.Bruiser;
            return RoleTag.Support;
        }

        // 1..5 by total stat budget (HP weighted down so it doesn't dominate).
        public static int Rarity(SpeciesData sp)
        {
            var b = sp.baseStats;
            int tot = b.hp / 6 + b.atk + b.def + b.spd + b.intel + b.luck;
            if (tot >= 95) return 5;
            if (tot >= 85) return 4;
            if (tot >= 75) return 3;
            if (tot >= 65) return 2;
            return 1;
        }

        public static string Stars(int rarity) => new string('*', System.Math.Max(0, System.Math.Min(5, rarity)));

        public static int OwnedPercent(SaveData d, IList<string> roster)
        {
            if (roster == null || roster.Count == 0) return 0;
            int owned = 0;
            for (int i = 0; i < roster.Count; i++) if (d.IsUnlocked(roster[i])) owned++;
            return (int)System.Math.Round(100.0 * owned / roster.Count);
        }
    }
}
