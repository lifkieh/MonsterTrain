using System;
using System.Collections.Generic;

namespace MTA.Core
{
    // Element system 2.0 (TYM 2.0 Phase 2/3). Ten elements + Void. The whole matchup table is derived
    // from the STRONG relations only, so it is always symmetric-consistent (A strong vs B ⇔ B weak vs A).
    // Advantage = ×1.5 + guaranteed crit; disadvantage = ×0.7; neutral = ×1.0. Void is pure neutral —
    // never gives or receives an elemental advantage or crit. Deterministic (no RNG).
    public static class ElementTable
    {
        public const string Void = "Void";

        public static readonly string[] All =
            { "Fire", "Water", "Nature", "Earth", "Wind", "Lightning", "Ice", "Metal", "Light", "Shadow" };

        // A deals a strong (advantage) hit to each listed defender.
        static readonly Dictionary<string, string[]> Strong = new Dictionary<string, string[]>
        {
            { "Fire",      new[] { "Nature", "Ice" } },
            { "Water",     new[] { "Fire", "Metal" } },
            { "Nature",    new[] { "Water", "Earth" } },
            { "Earth",     new[] { "Fire", "Lightning" } },
            { "Wind",      new[] { "Earth", "Nature" } },
            { "Lightning", new[] { "Water", "Wind" } },
            { "Ice",       new[] { "Nature", "Wind" } },
            { "Metal",     new[] { "Ice", "Earth" } },
            { "Light",     new[] { "Shadow" } },
            { "Shadow",    new[] { "Light" } },
        };

        public static bool IsVoid(string e) => e == Void;

        // +1 advantage, -1 disadvantage, 0 neutral. Void (either side) is always 0.
        public static int Advantage(string attacker, string defender)
        {
            if (string.IsNullOrEmpty(attacker) || string.IsNullOrEmpty(defender) || attacker == defender) return 0;
            if (IsVoid(attacker) || IsVoid(defender)) return 0;
            if (Strong.TryGetValue(attacker, out var s) && Array.IndexOf(s, defender) >= 0) return 1;
            if (Strong.TryGetValue(defender, out var d) && Array.IndexOf(d, attacker) >= 0) return -1;
            return 0;
        }

        // Balance helpers (used by tests/reports): how many elements this one beats / loses to.
        public static int StrengthCount(string e) => Strong.TryGetValue(e, out var s) ? s.Length : 0;
        public static int WeaknessCount(string e)
        {
            int n = 0;
            foreach (var kv in Strong) if (Array.IndexOf(kv.Value, e) >= 0) n++;
            return n;
        }
    }
}
