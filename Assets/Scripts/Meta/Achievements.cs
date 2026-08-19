using System;
using System.Collections.Generic;

namespace MTA.Meta
{
    // Achievement system (Phase V). Pure C#, edit-mode testable. Each achievement is a
    // predicate over SaveData (+ the total species count for collection goals). CheckNew
    // unlocks any newly-earned achievements and returns them so the UI can pop a toast.
    // Presentation/meta only — never touches balance or the sim.
    public class AchievementDef
    {
        public readonly string id, title, desc;
        public readonly Func<SaveData, int, bool> earned;   // (save, totalSpecies)
        public AchievementDef(string id, string title, string desc, Func<SaveData, int, bool> earned)
        { this.id = id; this.title = title; this.desc = desc; this.earned = earned; }
    }

    public static class Achievements
    {
        // distinct discovered species = union of unlocked + seen
        public static int Discovered(SaveData s)
        {
            int n = s.unlocked.Count;
            for (int i = 0; i < s.seen.Count; i++) if (!s.unlocked.Contains(s.seen[i])) n++;
            return n;
        }

        public static readonly AchievementDef[] Defs =
        {
            new AchievementDef("first_win",   "First Blood",       "Win your first battle",      (s, t) => s.battlesWon >= 1),
            new AchievementDef("first_evo",   "Metamorphosis",     "Evolve a monster",           (s, t) => s.evolutionsDone >= 1),
            new AchievementDef("combo_master","Combo Master",      "Land a 15-hit combo",        (s, t) => s.bestCombo >= 15),
            new AchievementDef("collector",   "Collector",         "Own 10 monsters",            (s, t) => s.unlocked.Count >= 10),
            new AchievementDef("streak5",     "Unstoppable",       "Win 5 battles in a row",     (s, t) => s.bestWinStreak >= 5),
            new AchievementDef("league",      "League Champion",   "Complete a league",          (s, t) => s.leaguesCompleted >= 1),
            new AchievementDef("evo3",        "Evolution Master",  "Evolve 3 monsters",          (s, t) => s.evolutionsDone >= 3),
            new AchievementDef("trainer10",   "Dedicated Trainer", "Train monsters 10 times",    (s, t) => s.trainingsDone >= 10),
            new AchievementDef("level10",     "Seasoned Trainer",  "Reach Trainer level 10",     (s, t) => s.playerLevel >= 10),
            new AchievementDef("veteran",     "Veteran",           "Win 50 battles",             (s, t) => s.battlesWon >= 50),
            new AchievementDef("dexmaster",   "Master Collector",  "Discover every monster",     (s, t) => t > 0 && Discovered(s) >= t),
        };

        // Unlock any newly-earned achievements; return the freshly-earned defs (for toasts).
        public static List<AchievementDef> CheckNew(SaveData s, int totalSpecies)
        {
            var fresh = new List<AchievementDef>();
            foreach (var def in Defs)
                if (!s.HasAchievement(def.id) && def.earned(s, totalSpecies))
                {
                    s.achievements.Add(def.id);
                    fresh.Add(def);
                }
            return fresh;
        }

        public static int UnlockedCount(SaveData s)
        {
            int n = 0;
            foreach (var def in Defs) if (s.HasAchievement(def.id)) n++;
            return n;
        }
    }
}
