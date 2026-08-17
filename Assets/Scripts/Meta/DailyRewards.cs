namespace MTA.Meta
{
    // Pure-C# daily-login reward rules. Time is passed in (System.DateTime) so the
    // whole thing is deterministic + edit-mode testable; the runtime feeds Now.
    public static class DailyRewards
    {
        public const int Cycle = 7;
        // Escalating coins across a 7-day streak cycle; day 7 is the big payout.
        public static readonly int[] Table = { 50, 60, 75, 90, 110, 130, 200 };

        // Local-day number since 2000-01-01 (date only; time-of-day ignored).
        public static int DayIndex(System.DateTime local)
            => (local.Date - new System.DateTime(2000, 1, 1)).Days;

        public static int RewardFor(int streak)
            => Table[((streak < 1 ? 1 : streak) - 1) % Cycle];

        // Blocked once claimed today, and blocked if the clock reads earlier than
        // the last claim (rolled-back clock anti-cheat).
        public static bool CanClaim(SaveData d, System.DateTime local)
            => DayIndex(local) > d.lastClaimDay;

        // What the next claim would pay, given continue-vs-reset of the streak.
        public static int Preview(SaveData d, System.DateTime local)
        {
            int today = DayIndex(local);
            int s = (d.lastClaimDay > 0 && today - d.lastClaimDay == 1) ? d.loginStreak + 1 : 1;
            return RewardFor(s);
        }

        public class DailyResult
        {
            public bool claimed;
            public int coins;
            public int streak;
            public int cycleDay;   // 0..6
            public bool streakReset;
        }

        // Claim today's reward. Extends the streak on a consecutive day, resets it
        // after a gap. No-op (claimed=false) on same-day or rolled-back clocks.
        public static DailyResult Claim(SaveData d, System.DateTime local)
        {
            var res = new DailyResult { streak = d.loginStreak };
            int today = DayIndex(local);
            if (today <= d.lastClaimDay) return res;   // anti-cheat: no double / backward claim

            if (d.lastClaimDay > 0 && today - d.lastClaimDay == 1) d.loginStreak++;
            else { res.streakReset = d.lastClaimDay > 0; d.loginStreak = 1; }

            int cycleDay = (d.loginStreak - 1) % Cycle;
            int coins = Table[cycleDay];
            d.coins += coins;
            d.lastClaimDay = today;

            d.rewardHistory.Add("Day " + d.loginStreak + ":  +" + coins + " coins");
            while (d.rewardHistory.Count > 30) d.rewardHistory.RemoveAt(0);

            res.claimed = true; res.coins = coins; res.streak = d.loginStreak; res.cycleDay = cycleDay;
            return res;
        }
    }
}
