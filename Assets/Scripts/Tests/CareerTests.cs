using System.Collections.Generic;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase G: deterministic career ladder + frontier progression rules.
    public class CareerTests
    {
        static List<string> Pool() => new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };

        [Test]
        public void Build_IsDeterministicAndFullLength()
        {
            var pool = Pool();
            var s1 = Career.Build(pool);
            var s2 = Career.Build(pool);
            Assert.AreEqual(Career.Stages, s1.Count);
            for (int i = 0; i < Career.Stages; i++)
            {
                Assert.AreEqual(s1[i].name, s2[i].name);
                Assert.AreEqual(s1[i].enemyLevel, s2[i].enemyLevel);
                Assert.AreEqual(Career.TeamSize, s1[i].enemies.Count);
                CollectionAssert.AreEqual(s1[i].enemies, s2[i].enemies);
            }
        }

        [Test]
        public void DifficultyScales_MonotonicFromBaseline()
        {
            Assert.AreEqual(GameSession.Level, Career.EnemyLevel(0));
            for (int i = 1; i < Career.Stages; i++)
                Assert.Greater(Career.EnemyLevel(i), Career.EnemyLevel(i - 1));
            Assert.AreEqual(GameSession.Level + Career.Stages - 1, Career.EnemyLevel(Career.Stages - 1));
        }

        [Test]
        public void FreshSave_OnlyFirstStageUnlocked()
        {
            var d = new SaveData();
            Assert.IsTrue(Career.IsUnlocked(d, 0));
            Assert.IsFalse(Career.IsCleared(d, 0));
            Assert.IsFalse(Career.IsUnlocked(d, 1));
            Assert.AreEqual(0, Career.CompletionPercent(d));
        }

        [Test]
        public void ClearStage_AdvancesFrontierAndPaysOnce()
        {
            var d = new SaveData();
            long paid = Career.ClearStage(d, 0, 40);
            Assert.AreEqual(40, paid);
            Assert.AreEqual(1, d.careerStage);
            Assert.AreEqual(40, d.coins);
            Assert.IsTrue(Career.IsCleared(d, 0));
            Assert.IsTrue(Career.IsUnlocked(d, 1));

            // Retry an already-cleared stage: no repeat reward, frontier unchanged.
            long again = Career.ClearStage(d, 0, 40);
            Assert.AreEqual(0, again);
            Assert.AreEqual(1, d.careerStage);
            Assert.AreEqual(40, d.coins);

            // A win on a not-yet-reached stage cannot skip the frontier.
            long skip = Career.ClearStage(d, 5, 40);
            Assert.AreEqual(0, skip);
            Assert.AreEqual(1, d.careerStage);
        }

        [Test]
        public void FullSweep_CompletesCareer()
        {
            var d = new SaveData();
            var stages = Career.Build(Pool());
            for (int i = 0; i < Career.Stages; i++)
            {
                Assert.IsTrue(Career.IsUnlocked(d, i));
                Assert.AreEqual(stages[i].reward, Career.ClearStage(d, i, stages[i].reward));
            }
            Assert.IsTrue(Career.IsComplete(d));
            Assert.AreEqual(100, Career.CompletionPercent(d));
        }
    }
}
