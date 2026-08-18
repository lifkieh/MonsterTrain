using System.Collections.Generic;

namespace MTA.Meta
{
    // One career stage: deterministic opponents, level-scaled difficulty, first-clear reward.
    public class CareerStage
    {
        public int index;                                  // 0-based position in the ladder
        public string league;                              // "Bronze".."Master"
        public string name;                                // "Bronze 1"
        public int enemyLevel;
        public long reward;                                // coins granted on first clear
        public List<string> enemies = new List<string>();  // TeamSize opponent species
    }

    // Pure-C# career ladder + progression rules. No IO, no UnityEngine: fully
    // deterministic from the species pool, so it's edit-mode testable.
    public static class Career
    {
        public const int Stages = 18;
        public const int TeamSize = 3;
        public const int PerLeague = 3;
        public static readonly string[] Leagues = { "Bronze", "Silver", "Gold", "Master", "Champion", "Legend" };

        // Enemy level rises one per stage from the casual baseline (5..16).
        public static int EnemyLevel(int stageIndex) => GameSession.Level + stageIndex;

        // Deterministic ladder built from the species pool (stable per pool ordering).
        public static List<CareerStage> Build(IList<string> pool)
        {
            var list = new List<CareerStage>(Stages);
            for (int i = 0; i < Stages; i++)
            {
                var st = new CareerStage
                {
                    index = i,
                    league = Leagues[i / PerLeague],
                    enemyLevel = EnemyLevel(i),
                    reward = 40 + i * 20,
                };
                st.name = st.league + " " + (i % PerLeague + 1);
                var rng = new System.Random(9000 + i);     // fixed seed per stage
                for (int k = 0; k < TeamSize; k++) st.enemies.Add(pool[rng.Next(pool.Count)]);
                list.Add(st);
            }
            return list;
        }

        // ---- Save-state rules. d.careerStage is the frontier = number of stages cleared. ----
        public static bool IsCleared(SaveData d, int stageIndex) => stageIndex < d.careerStage;
        public static bool IsUnlocked(SaveData d, int stageIndex) => stageIndex <= d.careerStage && stageIndex < Stages;
        public static bool IsComplete(SaveData d) => d.careerStage >= Stages;
        public static int Frontier(SaveData d) => d.careerStage < Stages ? d.careerStage : Stages - 1;

        public static int CompletionPercent(SaveData d)
        {
            int c = d.careerStage < 0 ? 0 : (d.careerStage > Stages ? Stages : d.careerStage);
            return (int)(100L * c / Stages);
        }

        // Record a win. Clearing the current frontier advances it and pays the
        // reward once; replaying an already-cleared stage pays nothing.
        public static long ClearStage(SaveData d, int stageIndex, long reward)
        {
            if (stageIndex == d.careerStage && d.careerStage < Stages)
            {
                d.careerStage++;
                d.coins += reward;
                return reward;
            }
            return 0;
        }
    }
}
