using System.Collections.Generic;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase F: training spends coins and grants XP/levels; guarded and persistent.
    public class TrainingTests
    {
        static List<string> Roster() => new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };

        [Test]
        public void Train_SpendsCoinsAndGainsXp()
        {
            var d = Progression.NewGame(Roster());
            d.coins = 100;
            int g = Progression.Train(d, "a");
            Assert.GreaterOrEqual(g, 0);
            Assert.AreEqual(100 - Progression.TrainCost, d.coins);
            var m = d.Find("a");
            Assert.IsTrue(m.xp > 0 || m.level > 1);
        }

        [Test]
        public void Train_InsufficientCoins_NoOp()
        {
            var d = Progression.NewGame(Roster());
            d.coins = 0;
            Assert.AreEqual(-1, Progression.Train(d, "a"));
            Assert.AreEqual(0, d.coins);
            Assert.AreEqual(1, d.LevelOf("a"));
        }

        [Test]
        public void Train_LockedMonster_NoOp()
        {
            var d = Progression.NewGame(Roster());   // "h" is locked
            d.coins = 1000;
            Assert.AreEqual(-1, Progression.Train(d, "h"));
            Assert.AreEqual(1000, d.coins);
        }

        [Test]
        public void Train_ManyTimes_LevelsUpAndPersists()
        {
            var d = Progression.NewGame(Roster());
            d.coins = 1000;
            for (int i = 0; i < 12; i++) Progression.Train(d, "a");
            Assert.Greater(d.LevelOf("a"), 1);
            var d2 = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(d));
            Assert.AreEqual(d.LevelOf("a"), d2.LevelOf("a"));
        }
    }
}
