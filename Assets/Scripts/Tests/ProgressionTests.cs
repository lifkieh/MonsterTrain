using System.Collections.Generic;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase C: meta progression rules + save serialization.
    public class ProgressionTests
    {
        static List<string> Roster() => new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };

        [Test]
        public void NewGame_UnlocksStarters()
        {
            var d = Progression.NewGame(Roster());
            Assert.AreEqual(Progression.StartingUnlocks, d.unlocked.Count);
            Assert.AreEqual(Progression.StartingUnlocks, d.collection.Count);
            Assert.IsTrue(d.IsUnlocked("a"));
            Assert.IsFalse(d.IsUnlocked("h"));
            Assert.AreEqual(1, d.playerLevel);
        }

        [Test]
        public void ApplyBattle_Win_RewardsAndDeterministic()
        {
            var roster = Roster();
            var team = new List<string> { "a", "b", "c" };
            var d1 = Progression.NewGame(roster);
            var d2 = Progression.NewGame(roster);
            var r1 = Progression.ApplyBattle(d1, team, true, roster);
            var r2 = Progression.ApplyBattle(d2, team, true, roster);

            Assert.AreEqual(r1.playerXp, r2.playerXp);
            Assert.AreEqual(r1.coins, r2.coins);
            Assert.AreEqual(d1.coins, d2.coins);
            Assert.AreEqual(d1.playerXp, d2.playerXp);
            Assert.Greater(d1.coins, 0);
            Assert.AreEqual(1, d1.battlesWon);
            Assert.AreEqual(1, d1.battlesPlayed);
            // monster gained XP (and levels, since 60 >= 50).
            Assert.GreaterOrEqual(d1.LevelOf("a"), 2);
        }

        [Test]
        public void PlayerLevelUp_UnlocksNextSpecies()
        {
            var roster = Roster();
            var team = new List<string> { "a", "b", "c" };
            var d = Progression.NewGame(roster);
            int before = d.unlocked.Count;
            Progression.ApplyBattle(d, team, true, roster);   // 80 xp
            Progression.ApplyBattle(d, team, true, roster);   // 160 xp -> level 2
            Assert.GreaterOrEqual(d.playerLevel, 2);
            Assert.Greater(d.unlocked.Count, before);         // a new species unlocked
        }

        [Test]
        public void Save_RoundTripsAndIsBackwardCompatible()
        {
            var d = Progression.NewGame(new List<string> { "a", "b", "c", "d", "e", "f" });
            d.coins = 123; d.playerXp = 45; d.playerLevel = 3;
            var json = UnityEngine.JsonUtility.ToJson(d);
            var d2 = UnityEngine.JsonUtility.FromJson<SaveData>(json);
            Assert.AreEqual(d.coins, d2.coins);
            Assert.AreEqual(d.playerLevel, d2.playerLevel);
            Assert.AreEqual(d.unlocked.Count, d2.unlocked.Count);
            Assert.AreEqual(d.collection.Count, d2.collection.Count);

            // Old/partial save (missing new fields) → defaults, lists non-null.
            var old = UnityEngine.JsonUtility.FromJson<SaveData>("{\"saveVersion\":0,\"playerName\":\"X\"}");
            Assert.IsNotNull(old.unlocked);
            Assert.IsNotNull(old.collection);
            Assert.AreEqual("X", old.playerName);
            Assert.AreEqual(1, old.playerLevel);
        }
    }
}
