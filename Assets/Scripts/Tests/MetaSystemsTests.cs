using MTA.Meta;
using NUnit.Framework;
using UnityEngine;

namespace MTA.Tests
{
    // Phase U/V: quests + achievements are pure-C# meta systems (like Progression).
    // These prove reward/claim/unlock logic and that the new v2 save fields round-trip
    // and stay backward-compatible with old saves.
    public class MetaSystemsTests
    {
        static SaveData Fresh() => new SaveData();

        [Test]
        public void Quest_Progress_Derived_And_Claim_Once()
        {
            var s = Fresh();
            var def = System.Array.Find(Quests.Defs, d => d.id == "prog_win10");
            Assert.AreEqual(0, Quests.Progress(s, def));
            s.battlesWon = 10;
            Assert.IsTrue(Quests.IsComplete(s, def));
            Assert.IsTrue(Quests.CanClaim(s, def));
            long coinsBefore = s.coins;
            Assert.IsTrue(Quests.Claim(s, def, out int coins, out int xp));
            Assert.AreEqual(def.coins, coins);
            Assert.AreEqual(coinsBefore + coins, s.coins);
            Assert.IsFalse(Quests.CanClaim(s, def), "cannot claim twice");
            Assert.IsFalse(Quests.Claim(s, def, out _, out _));
        }

        [Test]
        public void Quest_Daily_Resets_On_New_Day()
        {
            var s = Fresh();
            Quests.SyncDay(s, 100);
            s.dailyWins = 2;
            var win = System.Array.Find(Quests.Defs, d => d.id == "daily_win");
            Assert.IsTrue(Quests.IsComplete(s, win));
            Assert.IsTrue(Quests.Claim(s, win, out _, out _));
            Assert.IsFalse(Quests.CanClaim(s, win));
            Quests.SyncDay(s, 101);                        // next day
            Assert.AreEqual(0, s.dailyWins, "daily counters reset");
            Assert.IsFalse(Quests.IsClaimed(s, win), "daily claim reset");
        }

        [Test]
        public void Achievements_Unlock_Once_And_Idempotent()
        {
            var s = Fresh();
            Assert.IsEmpty(Achievements.CheckNew(s, 21));
            s.battlesWon = 1; s.evolutionsDone = 1;
            var fresh = Achievements.CheckNew(s, 21);
            Assert.AreEqual(2, fresh.Count);
            Assert.IsTrue(s.HasAchievement("first_win"));
            Assert.IsEmpty(Achievements.CheckNew(s, 21), "no re-unlock");
        }

        [Test]
        public void Achievement_DexMaster_Needs_Full_Roster()
        {
            var s = Fresh();
            for (int i = 0; i < 21; i++) s.unlocked.Add("sp" + i);
            var fresh = Achievements.CheckNew(s, 21);
            Assert.IsTrue(fresh.Exists(d => d.id == "dexmaster"));
        }

        [Test]
        public void Save_V2_Fields_RoundTrip()
        {
            var s = Fresh();
            s.onboarded = true; s.bestWinStreak = 7; s.evolutionsDone = 3; s.bestCombo = 20;
            s.quests.Add(new QuestState { id = "prog_win10", progress = 10, claimed = true });
            s.achievements.Add("first_win");
            var json = JsonUtility.ToJson(s);
            var back = JsonUtility.FromJson<SaveData>(json);
            Assert.IsTrue(back.onboarded);
            Assert.AreEqual(7, back.bestWinStreak);
            Assert.AreEqual(1, back.quests.Count);
            Assert.IsTrue(back.quests[0].claimed);
            Assert.Contains("first_win", back.achievements);
        }

        [Test]
        public void OldSave_Missing_V2_Fields_Loads_With_Defaults()
        {
            // A pre-v2 save JSON with none of the new fields present.
            string oldJson = "{\"saveVersion\":1,\"playerLevel\":3,\"coins\":200,\"battlesWon\":4,\"unlocked\":[\"wolf\"]}";
            var d = JsonUtility.FromJson<SaveData>(oldJson);
            Assert.AreEqual(3, d.playerLevel);
            Assert.AreEqual(4, d.battlesWon);
            Assert.IsNotNull(d.quests);          // initializer ran → non-null
            Assert.IsNotNull(d.achievements);
            Assert.IsFalse(d.onboarded);
            Assert.AreEqual(0, d.bestWinStreak);
        }

        [Test]
        public void Streak_Milestones_Grant_Once_And_Escalate()
        {
            var s = Fresh();
            s.loginStreak = 7;                       // reaches day-3 and day-7 milestones
            long before = s.coins;
            var g = Streaks.CheckMilestones(s);
            Assert.AreEqual(2, g.Count);
            Assert.AreEqual(before + 150 + 400, s.coins);
            Assert.AreEqual(14, Streaks.NextMilestoneDay(s));
            Assert.IsEmpty(Streaks.CheckMilestones(s), "milestones grant once");
            s.loginStreak = 30;
            var g2 = Streaks.CheckMilestones(s);     // now day-14 and day-30
            Assert.AreEqual(2, g2.Count);
            Assert.AreEqual(0, Streaks.NextMilestoneDay(s), "all claimed");
        }
    }
}
