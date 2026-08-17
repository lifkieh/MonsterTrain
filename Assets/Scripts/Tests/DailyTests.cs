using System;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase H: daily-login streak, reward table, and anti-cheat guards.
    public class DailyTests
    {
        static readonly DateTime Base = new DateTime(2026, 1, 1, 9, 0, 0);

        [Test]
        public void FreshClaim_GrantsAndStartsStreak()
        {
            var d = new SaveData();
            var r = DailyRewards.Claim(d, Base);
            Assert.IsTrue(r.claimed);
            Assert.AreEqual(DailyRewards.Table[0], r.coins);
            Assert.AreEqual(DailyRewards.Table[0], d.coins);
            Assert.AreEqual(1, d.loginStreak);
            Assert.AreEqual(1, d.rewardHistory.Count);
        }

        [Test]
        public void SameDay_NoDoubleClaim()
        {
            var d = new SaveData();
            DailyRewards.Claim(d, Base);
            long coins = d.coins;
            Assert.IsFalse(DailyRewards.CanClaim(d, Base.AddHours(6)));
            var r = DailyRewards.Claim(d, Base.AddHours(6));
            Assert.IsFalse(r.claimed);
            Assert.AreEqual(coins, d.coins);
            Assert.AreEqual(1, d.loginStreak);
        }

        [Test]
        public void ConsecutiveDays_BuildStreak()
        {
            var d = new SaveData();
            DailyRewards.Claim(d, Base);
            var r2 = DailyRewards.Claim(d, Base.AddDays(1));
            Assert.IsTrue(r2.claimed);
            Assert.AreEqual(2, d.loginStreak);
            Assert.AreEqual(DailyRewards.Table[0] + DailyRewards.Table[1], d.coins);
        }

        [Test]
        public void Gap_ResetsStreak()
        {
            var d = new SaveData();
            DailyRewards.Claim(d, Base);
            DailyRewards.Claim(d, Base.AddDays(1));   // streak 2
            var r = DailyRewards.Claim(d, Base.AddDays(5));   // gap
            Assert.IsTrue(r.claimed);
            Assert.IsTrue(r.streakReset);
            Assert.AreEqual(1, d.loginStreak);
        }

        [Test]
        public void ClockRollback_Blocked()
        {
            var d = new SaveData();
            DailyRewards.Claim(d, Base.AddDays(10));
            long coins = d.coins; int streak = d.loginStreak;
            Assert.IsFalse(DailyRewards.CanClaim(d, Base.AddDays(9)));
            var r = DailyRewards.Claim(d, Base.AddDays(9));
            Assert.IsFalse(r.claimed);
            Assert.AreEqual(coins, d.coins);
            Assert.AreEqual(streak, d.loginStreak);
        }

        [Test]
        public void SevenDayCycle_Wraps()
        {
            var d = new SaveData();
            for (int i = 0; i < 8; i++) DailyRewards.Claim(d, Base.AddDays(i));
            Assert.AreEqual(8, d.loginStreak);
            // Day 8 pays the same as day 1 (cycle of 7).
            Assert.AreEqual(DailyRewards.Table[0], DailyRewards.RewardFor(8));
            Assert.AreEqual(200, DailyRewards.RewardFor(7));
        }

        [Test]
        public void Persists_AcrossJson()
        {
            var d = new SaveData();
            DailyRewards.Claim(d, Base);
            DailyRewards.Claim(d, Base.AddDays(1));
            var d2 = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(d));
            Assert.AreEqual(d.lastClaimDay, d2.lastClaimDay);
            Assert.AreEqual(d.loginStreak, d2.loginStreak);
            Assert.AreEqual(d.rewardHistory.Count, d2.rewardHistory.Count);
        }
    }
}
