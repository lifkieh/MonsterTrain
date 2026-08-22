using System;
using System.Collections.Generic;
using MTA.Core;

namespace MTA.Meta
{
    // JsonUtility-friendly save model (public fields, Lists — no Dictionary/HashSet).
    [Serializable]
    public class MonsterSave
    {
        public string speciesId;
        public int level = 1;
        public int xp;
        // --- TYM 2.0 additive (old saves default via these initializers) ---
        public int count = 1;     // copies owned (gacha dupes / fusion fuel / selling)
        public int star = 1;      // fusion star 1..5 (higher stats + level cap, no rarity change)
        public int mastery = 1;   // skill mastery 1..5 (training → damage scale)
        public int bondXp;        // bond progression (cosmetic unlocks)
        public int allocHp, allocAtk, allocDef, allocSpd;   // stat allocation spend
        public int unspent;       // unspent stat points (+2 per level)
    }

    // Runtime state for one quest (definition lives in code — see Quests).
    [Serializable]
    public class QuestState
    {
        public string id;
        public int progress;
        public bool claimed;
    }

    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 2;

        public int saveVersion = CurrentVersion;
        public string playerName = "Trainer";
        public int playerLevel = 1;
        public int playerXp;
        public long coins;
        public int battlesPlayed;
        public int battlesWon;
        public int careerStage;      // career frontier = number of stages cleared
        public int lastClaimDay;     // daily-reward: day index of last claim (0 = never)
        public int loginStreak;      // daily-reward: consecutive-day streak
        public int targetFps = 60;   // display: 30 or 60
        public int quality = 1;      // display: 0 = Low, 1 = High
        public bool muted;
        public List<string> unlocked = new List<string>();
        public List<string> seen = new List<string>();
        public List<string> rewardHistory = new List<string>();
        public List<MonsterSave> collection = new List<MonsterSave>();
        public string lastSaveUtc = "";

        // --- v2 additive fields (backward-compatible: old saves default these) ---
        public bool onboarded;              // T: first-launch tutorial completed
        public int winStreak;               // X: current consecutive-win streak
        public int bestWinStreak;           // X: best-ever win streak
        public int evolutionsDone;          // U/V: lifetime evolutions
        public int trainingsDone;           // U/V: lifetime training sessions
        public int bestCombo;               // V: best combo count in a battle
        public int leaguesCompleted;        // U/V: career leagues finished
        public int questDay = -1;           // U: day index the daily quests were rolled for
        public int dailyWins;               // U: wins today (reset per questDay)
        public int dailyBattles;            // U: battles today
        public int dailyTrains;             // U: trainings today
        public List<QuestState> quests = new List<QuestState>();   // U
        public List<string> achievements = new List<string>();     // V: unlocked ids
        public List<string> seenNews = new List<string>();         // reserved
        public List<int> streakMilestones = new List<int>();       // X+: claimed login-streak milestone days

        // --- TYM 2.0 additive: economy + gacha + auras + feed ---
        public long supercoins;             // premium currency (earned via achievements/events; accelerate only, never P2W)
        public long essence;                // from selling monsters → progression
        public int pityEpic, pityLeg, pityMyth;   // monster-gacha pity counters (pulls since last)
        public long gachaSeed;              // monster-gacha RNG stream — SEPARATE from the battle sim seed
        public long auraSeed;               // aura-gacha RNG stream
        public List<string> auras = new List<string>();   // owned aura ids
        public string equippedAura = "";
        public int foodBasic, foodPremium, foodLegendary; // feed inventory

        public MonsterSave Find(string speciesId)
        {
            for (int i = 0; i < collection.Count; i++) if (collection[i].speciesId == speciesId) return collection[i];
            return null;
        }

        public bool IsUnlocked(string speciesId) => unlocked.Contains(speciesId);
        public bool IsSeen(string speciesId) => IsUnlocked(speciesId) || seen.Contains(speciesId);
        public void MarkSeen(string speciesId) { if (!seen.Contains(speciesId) && !IsUnlocked(speciesId)) seen.Add(speciesId); }
        public int LevelOf(string speciesId) { var m = Find(speciesId); return m != null ? m.level : 1; }

        public QuestState Quest(string id)
        {
            for (int i = 0; i < quests.Count; i++) if (quests[i].id == id) return quests[i];
            return null;
        }
        public bool HasAchievement(string id) => achievements.Contains(id);
    }

    public class BattleRewards
    {
        public bool won;
        public int playerXp, playerLevelsGained, monsterXp;
        public long coins;
        public readonly List<string> leveledUp = new List<string>();     // "wolf 3->4"
        public readonly List<string> newlyUnlocked = new List<string>(); // speciesIds
    }

    // Pure-C# progression rules (no IO, no UnityEngine). Deterministic + testable.
    public static class Progression
    {
        public const int MaxLevel = 30;
        public static int PlayerXpForNext(int level) => 100 + (level - 1) * 60;
        public static int MonsterXpForNext(int level) => 50 + (level - 1) * 40;
        public const int StartingUnlocks = 6;
        public const int TrainCost = 30;      // coins per training session
        public const int TrainXp = 45;        // XP gained per session

        // Spend coins to train one owned monster. Returns levels gained, or -1 if
        // it can't be afforded / the monster isn't owned.
        public static int Train(SaveData d, string id)
        {
            if (!d.IsUnlocked(id) || d.coins < TrainCost) return -1;
            d.coins -= TrainCost;
            var m = d.Find(id);
            if (m == null) { m = new MonsterSave { speciesId = id, level = 1 }; d.collection.Add(m); }
            int from = m.level;
            m.xp += TrainXp;
            while (m.level < MaxLevel && m.xp >= MonsterXpForNext(m.level)) { m.xp -= MonsterXpForNext(m.level); m.level++; }
            return m.level - from;
        }

        // --- TYM 2.0 Phase 4: Feed (food → xp/level, NOT mastery) ---
        public static readonly int[] FoodXp = { 30, 120, 400 };   // basic / premium / legendary
        public static int Feed(SaveData d, string id, int foodType)
        {
            if (foodType < 0 || foodType > 2) return -1;
            var m = d.Find(id); if (m == null) return -1;
            if (foodType == 0) { if (d.foodBasic <= 0) return -1; d.foodBasic--; }
            else if (foodType == 1) { if (d.foodPremium <= 0) return -1; d.foodPremium--; }
            else { if (d.foodLegendary <= 0) return -1; d.foodLegendary--; }
            int from = m.level;
            m.xp += FoodXp[foodType];
            while (m.level < MaxLevel && m.xp >= MonsterXpForNext(m.level)) { m.xp -= MonsterXpForNext(m.level); m.level++; }
            if (m.level >= MaxLevel) m.xp = 0;
            return m.level - from;
        }

        // --- TYM 2.0 Phase 5: Stat allocation (+2 points per level; available = derived so it stays
        // consistent however the monster leveled). Spend on HP/ATK/DEF/SPD. ---
        public const int PointsPerLevel = 2;
        public static int StatPointsAvailable(MonsterSave m)
            => m == null ? 0 : Math.Max(0, (m.level - 1) * PointsPerLevel - (m.allocHp + m.allocAtk + m.allocDef + m.allocSpd));
        public static bool AllocateStat(MonsterSave m, int stat)   // 0 HP, 1 ATK, 2 DEF, 3 SPD
        {
            if (m == null || StatPointsAvailable(m) <= 0) return false;
            switch (stat) { case 0: m.allocHp++; break; case 1: m.allocAtk++; break; case 2: m.allocDef++; break; case 3: m.allocSpd++; break; default: return false; }
            return true;
        }

        // --- TYM 2.0 Phase 6: Skill mastery training (coins, separate from level) + bond ---
        public const int MasteryTrainCost = 60;
        public const int MasteryMax = 5;
        public static bool TrainMastery(SaveData d, string id)
        {
            var m = d.Find(id);
            if (m == null || m.mastery >= MasteryMax || d.coins < MasteryTrainCost) return false;
            d.coins -= MasteryTrainCost; m.mastery++; d.trainingsDone++;
            return true;
        }
        public static void AddBond(MonsterSave m, int xp) { if (m != null) m.bondXp = Math.Max(0, m.bondXp + xp); }
        public static int BondLevel(MonsterSave m) => m == null ? 0 : Math.Min(10, m.bondXp / 100);

        // Evolution: an owned monster at/above its evolve level transforms into its
        // evolved species in place (keeps level + xp). Returns the new speciesId, or
        // null if not eligible. sp is the CURRENT species' data (from the registry).
        public static bool CanEvolve(SaveData d, SpeciesData sp)
        {
            if (sp == null || string.IsNullOrEmpty(sp.evolvesTo) || !d.IsUnlocked(sp.speciesId)) return false;
            var m = d.Find(sp.speciesId);
            return m != null && m.level >= sp.evolveLevel;
        }

        public static string Evolve(SaveData d, SpeciesData sp)
        {
            if (!CanEvolve(d, sp)) return null;
            var m = d.Find(sp.speciesId);
            m.speciesId = sp.evolvesTo;                       // transform in place
            if (!d.unlocked.Contains(sp.evolvesTo)) d.unlocked.Add(sp.evolvesTo);
            return sp.evolvesTo;
        }

        // Fresh profile: first N of the roster unlocked, each in the collection at L1.
        public static SaveData NewGame(IList<string> roster)
        {
            var d = new SaveData();
            int n = Math.Min(StartingUnlocks, roster.Count);
            for (int i = 0; i < n; i++)
            {
                d.unlocked.Add(roster[i]);
                d.collection.Add(new MonsterSave { speciesId = roster[i], level = 1, xp = 0 });
            }
            return d;
        }

        // Apply one finished battle. `team` = the player's 3 species; `roster` =
        // full ordered species list (for unlock order). Returns what was earned.
        public static BattleRewards ApplyBattle(SaveData d, IList<string> team, bool won, IList<string> roster)
        {
            var r = new BattleRewards { won = won };
            d.battlesPlayed++;
            if (won) d.battlesWon++;

            r.playerXp = won ? 80 : 30;
            r.monsterXp = won ? 60 : 25;
            r.coins = won ? 50 : 15;
            d.coins += r.coins;

            // Player XP / level.
            d.playerXp += r.playerXp;
            while (d.playerLevel < MaxLevel && d.playerXp >= PlayerXpForNext(d.playerLevel))
            {
                d.playerXp -= PlayerXpForNext(d.playerLevel);
                d.playerLevel++;
                r.playerLevelsGained++;
                // Each player level unlocks the next locked roster species.
                var next = NextLocked(d, roster);
                if (next != null)
                {
                    d.unlocked.Add(next);
                    if (d.Find(next) == null) d.collection.Add(new MonsterSave { speciesId = next, level = 1 });
                    r.newlyUnlocked.Add(next);
                }
            }

            // Monster XP / level for the team that fought.
            foreach (var id in team)
            {
                var m = d.Find(id);
                if (m == null) { m = new MonsterSave { speciesId = id, level = 1 }; d.collection.Add(m); }
                m.xp += r.monsterXp;
                while (m.level < MaxLevel && m.xp >= MonsterXpForNext(m.level))
                {
                    m.xp -= MonsterXpForNext(m.level);
                    int from = m.level; m.level++;
                    r.leveledUp.Add(id + " " + from + "->" + m.level);
                }
            }
            return r;
        }

        static string NextLocked(SaveData d, IList<string> roster)
        {
            for (int i = 0; i < roster.Count; i++) if (!d.unlocked.Contains(roster[i])) return roster[i];
            return null;
        }
    }
}
