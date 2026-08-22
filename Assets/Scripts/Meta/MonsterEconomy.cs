using System.Collections.Generic;

namespace MTA.Meta
{
    // TYM 2.0 collection economy — pure C# (no UnityEngine, no IO) so it is fully unit-testable and
    // DETERMINISTIC. Its RNG is a self-contained LCG on a save-persisted stream (gachaSeed / auraSeed),
    // completely separate from the battle simulator's seed — economy never perturbs combat determinism.
    public static class MonsterGacha
    {
        public static readonly int[] Rates = { 0, 55, 25, 12, 6, 2 };   // index by rarity 1..5 (Common..Mythical), sums 100
        public const int PityEpic = 20, PityLeg = 80, PityMyth = 200;

        static uint Next(ref long s) { unchecked { s = s * 6364136223846793005L + 1442695040888963407L; } return (uint)((ulong)s >> 33); }
        static int RollN(ref long s, int n) => n <= 0 ? 0 : (int)(Next(ref s) % (uint)n);

        // Rarity 1..5. Mutates seed + pity counters (pity guarantees an Epic≤20 / Legendary≤80 / Mythical≤200).
        public static int RollRarity(SaveData sv)
        {
            long s = sv.gachaSeed; int rarity;
            if (sv.pityMyth >= PityMyth - 1) rarity = 5;
            else if (sv.pityLeg >= PityLeg - 1) rarity = 4;
            else if (sv.pityEpic >= PityEpic - 1) rarity = AtLeastEpic(ref s);
            else rarity = ByRate(ref s);
            sv.gachaSeed = s;
            sv.pityEpic = rarity >= 3 ? 0 : sv.pityEpic + 1;
            sv.pityLeg = rarity >= 4 ? 0 : sv.pityLeg + 1;
            sv.pityMyth = rarity >= 5 ? 0 : sv.pityMyth + 1;
            return rarity;
        }
        static int ByRate(ref long s) { int r = RollN(ref s, 100), acc = 0; for (int i = 1; i <= 5; i++) { acc += Rates[i]; if (r < acc) return i; } return 1; }
        static int AtLeastEpic(ref long s) { int e = Rates[3], l = Rates[4], m = Rates[5]; int r = RollN(ref s, e + l + m); return r < e ? 3 : (r < e + l ? 4 : 5); }

        // Summon one monster. poolByRarity[r] = species of rarity r (1..5). Adds to the collection
        // (new entry or +1 duplicate). Returns the species id, or null if all pools are empty.
        public static string Summon(SaveData sv, List<string>[] poolByRarity, out int rarity, out bool isNew)
        {
            rarity = RollRarity(sv); isNew = false;
            int r = rarity;
            while (r >= 1 && (poolByRarity == null || r >= poolByRarity.Length || poolByRarity[r] == null || poolByRarity[r].Count == 0)) r--;
            if (r < 1) return null;
            long s = sv.gachaSeed; int idx = RollN(ref s, poolByRarity[r].Count); sv.gachaSeed = s;
            string id = poolByRarity[r][idx];
            var mon = sv.Find(id);
            if (mon == null)
            {
                sv.collection.Add(new MonsterSave { speciesId = id, level = 1, count = 1, star = 1, mastery = 1 });
                if (!sv.unlocked.Contains(id)) sv.unlocked.Add(id);
                isNew = true;
            }
            else mon.count += 1;
            return id;
        }
    }

    // Fusion: ★N → ★N+1 consumes N duplicate copies (roadmap: Wolf★1 + Wolf★1 = Wolf★2). No rarity change.
    public static class MonsterFusion
    {
        public static int FuseCost(int star) => star;                       // ★1→★2 costs 1, ★2→★3 costs 2, …
        public static bool CanFuse(MonsterSave m) => m != null && m.star >= 1 && m.star < 5 && m.count >= FuseCost(m.star) + 1;
        public static bool Fuse(MonsterSave m)
        {
            if (!CanFuse(m)) return false;
            m.count -= FuseCost(m.star);   // keeps 1 as the base, consumes the fuel copies
            m.star += 1;
            return true;
        }
    }

    // Selling: sell one DUPLICATE (never the last copy) → coins + essence, scaled by rarity.
    public static class MonsterSelling
    {
        public static bool Sell(SaveData sv, MonsterSave m, int rarity, out long coins, out long essence)
        {
            coins = 0; essence = 0;
            if (m == null || m.count < 2) return false;   // never sell the last copy
            m.count -= 1;
            coins = 40L * rarity;
            essence = 2L * rarity;
            sv.coins += coins; sv.essence += essence;
            return true;
        }
    }
}
