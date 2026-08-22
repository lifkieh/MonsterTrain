using System.Collections.Generic;

namespace MTA.Core
{
    // TYM 2.0 Phase 1 — every monster's UNIQUE support ability (activates only when the monster is a
    // Support, not the Active). Five categories; no two monsters share an effect id. Magnitudes are
    // deliberately modest single effects so two supports combine into "two modest boosts", never a
    // broken combo (balance rule: no overpowered support combinations). Data only here; combat
    // application arrives with the Active+Support sim rework. Pure C# — testable.
    public enum SupportCategory { Guardian, Healer, Buffer, Debuffer, Summoner }

    public struct SupportDef
    {
        public string id;                 // unique effect id
        public SupportCategory category;
        public float magnitude;           // effect strength (fraction unless noted)
        public string name, desc;
    }

    public static class SupportAbility
    {
        static readonly Dictionary<string, SupportDef> Table = new Dictionary<string, SupportDef>
        {
            // Guardian — protect the active
            { "golem",         D("bulwark",      SupportCategory.Guardian, 0.25f, "Bulwark", "Redirect 25% of damage taken by the active.") },
            { "turtle",        D("shell_wall",   SupportCategory.Guardian, 0.20f, "Shell Wall", "Active takes 20% less damage.") },
            { "treant",        D("bark_ward",    SupportCategory.Guardian, 0.18f, "Bark Ward", "Shield the active for 18% of its max HP.") },
            { "ghost",         D("phase_veil",   SupportCategory.Guardian, 1.00f, "Phase Veil", "Active dodges the first incoming hit.") },
            // Healer — sustain
            { "mushroom_beast",D("spore_regen",  SupportCategory.Healer,   0.03f, "Spore Regen", "Active regenerates 3% max HP per second.") },
            { "jelly",         D("gel_mend",      SupportCategory.Healer,   0.30f, "Gel Mend", "Emergency heal 30% max HP when the active drops below 30%.") },
            { "slime",         D("split_cleanse", SupportCategory.Healer,   1.00f, "Split Cleanse", "Cleanse debuffs from the active on a timer.") },
            { "squire",        D("field_medic",   SupportCategory.Healer,   0.15f, "Field Medic", "Heal the active 15% max HP once at low HP.") },
            // Buffer — enhance the active
            { "fire_lizard",   D("ignite",        SupportCategory.Buffer,   0.15f, "Ignite", "Active +15% attack.") },
            { "salamander",    D("heat_up",       SupportCategory.Buffer,   0.10f, "Heat Up", "Active +10% crit chance.") },
            { "phoenix",       D("rebirth_boon",  SupportCategory.Buffer,   0.12f, "Rebirth Boon", "Active -12% ultimate energy cost.") },
            { "inferno_drake", D("drake_fury",    SupportCategory.Buffer,   0.18f, "Drake Fury", "Active gains up to +18% attack as HP drops.") },
            { "dragonling",    D("dragon_spirit", SupportCategory.Buffer,   0.12f, "Dragon Spirit", "Active +12% speed.") },
            // Debuffer — weaken enemies
            { "wolf",          D("howl",          SupportCategory.Debuffer, 0.12f, "Howl", "Enemies -12% defense.") },
            { "dire_wolf",     D("alpha_howl",    SupportCategory.Debuffer, 0.18f, "Alpha Howl", "Enemies -18% defense.") },
            { "kraken",        D("ink_cloud",     SupportCategory.Debuffer, 0.15f, "Ink Cloud", "Enemies -15% speed.") },
            { "spider",        D("venom",         SupportCategory.Debuffer, 0.20f, "Venom", "Enemy damage-over-time amplified +20%.") },
            { "bat",           D("screech",       SupportCategory.Debuffer, 0.10f, "Screech", "Enemies -10% accuracy.") },
            // Summoner — temporary joins
            { "mantis",        D("blade_flurry",  SupportCategory.Summoner, 0.35f, "Blade Flurry", "Fly-by strike for 35% attack on a timer.") },
            { "blade_mantis",  D("twin_slash",    SupportCategory.Summoner, 0.28f, "Twin Slash", "Two fly-by strikes for 28% attack each.") },
            { "bee",           D("swarm",         SupportCategory.Summoner, 0.12f, "Swarm", "Summon a swarm: 3 hits of 12% attack.") },
            // Void mythical
            { "chronovore",    D("paradox_core",  SupportCategory.Buffer,   0.15f, "Paradox Core", "Active -15% cooldown / ultimate cost.") },
        };

        static SupportDef D(string id, SupportCategory c, float m, string name, string desc)
            => new SupportDef { id = id, category = c, magnitude = m, name = name, desc = desc };

        public static bool TryGet(string speciesId, out SupportDef def) => Table.TryGetValue(speciesId, out def);
        public static IEnumerable<string> AllSpecies => Table.Keys;
        public static int Count => Table.Count;

        // Balance check helper: are all effect ids unique? (used by tests)
        public static bool AllIdsUnique()
        {
            var seen = new HashSet<string>();
            foreach (var kv in Table) if (!seen.Add(kv.Value.id)) return false;
            return true;
        }
    }
}
