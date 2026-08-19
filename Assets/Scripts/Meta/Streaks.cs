using System.Collections.Generic;

namespace MTA.Meta
{
    // Login-streak milestone rewards (retention). The 7-day daily table already gives a
    // "come back tomorrow" reason; these one-time escalating bonuses give a "come back for
    // 30 days" reason. Pure C#, edit-mode testable. Claimed thresholds persist in
    // SaveData.streakMilestones (additive, backward-compatible). No PvP, no gacha.
    public static class Streaks
    {
        // (consecutive-day threshold, one-time bonus coins). Escalates to keep 30-day play.
        public static readonly (int day, int coins)[] Milestones =
        {
            (3, 150), (7, 400), (14, 900), (30, 2500),
        };

        // Grant any milestone the CURRENT login streak has reached and not yet claimed.
        public static List<KeyValuePair<int, int>> CheckMilestones(SaveData s)
        {
            var granted = new List<KeyValuePair<int, int>>();
            foreach (var m in Milestones)
                if (s.loginStreak >= m.day && !s.streakMilestones.Contains(m.day))
                {
                    s.streakMilestones.Add(m.day);
                    s.coins += m.coins;
                    granted.Add(new KeyValuePair<int, int>(m.day, m.coins));
                }
            return granted;
        }

        public static int NextMilestoneDay(SaveData s)
        {
            foreach (var m in Milestones) if (!s.streakMilestones.Contains(m.day)) return m.day;
            return 0;   // all claimed
        }

        public static int NextMilestoneCoins(SaveData s)
        {
            foreach (var m in Milestones) if (!s.streakMilestones.Contains(m.day)) return m.coins;
            return 0;
        }
    }
}
