using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // TYM 2.0 Phase 4/5/6 verification.
    public class Progression2Tests
    {
        [Test]
        public void Feed_ConsumesFood_AddsXp()
        {
            var d = new SaveData();
            d.collection.Add(new MonsterSave { speciesId = "wolf", level = 1 });
            Assert.AreEqual(-1, Progression.Feed(d, "wolf", 0));   // no food in inventory
            d.foodBasic = 5;
            Progression.Feed(d, "wolf", 0);
            Assert.AreEqual(4, d.foodBasic);
            Assert.AreEqual(30, d.Find("wolf").xp);                // basic food = 30 xp
        }

        [Test]
        public void StatAllocation_TwoPointsPerLevel()
        {
            var m = new MonsterSave { speciesId = "wolf", level = 5 };  // 4 levels → 8 points
            Assert.AreEqual(8, Progression.StatPointsAvailable(m));
            Assert.IsTrue(Progression.AllocateStat(m, 1));             // ATK
            Assert.AreEqual(1, m.allocAtk);
            Assert.AreEqual(7, Progression.StatPointsAvailable(m));
            for (int i = 0; i < 7; i++) Progression.AllocateStat(m, 0);
            Assert.AreEqual(0, Progression.StatPointsAvailable(m));
            Assert.IsFalse(Progression.AllocateStat(m, 0));            // none left
        }

        [Test]
        public void Mastery_TrainRaises_DamageScales()
        {
            Assert.AreEqual(1.0f, StatMath.MasteryMultiplier(1), 1e-5f);
            Assert.AreEqual(1.5f, StatMath.MasteryMultiplier(5), 1e-5f);
            var d = new SaveData { coins = 200 };
            d.unlocked.Add("wolf"); d.collection.Add(new MonsterSave { speciesId = "wolf", mastery = 1 });
            Assert.IsTrue(Progression.TrainMastery(d, "wolf"));
            Assert.AreEqual(2, d.Find("wolf").mastery);
            Assert.AreEqual(140, d.coins);                            // 200 - 60
        }

        [Test]
        public void Bond_Accumulates()
        {
            var m = new MonsterSave();
            Progression.AddBond(m, 250);
            Assert.AreEqual(2, Progression.BondLevel(m));             // 250/100 = 2
        }
    }

    public class SupportAbilityTests
    {
        [Test]
        public void EverySupport_HasUniqueId()
            => Assert.IsTrue(SupportAbility.AllIdsUnique(), "support effect ids must be unique");

        [Test]
        public void AllFiveCategories_Represented()
        {
            var cats = new HashSet<SupportCategory>();
            foreach (var sp in SupportAbility.AllSpecies)
                if (SupportAbility.TryGet(sp, out var d)) cats.Add(d.category);
            Assert.AreEqual(5, cats.Count);
        }

        [Test]
        public void Magnitudes_Modest_NoSingleEffectOP()
        {
            foreach (var sp in SupportAbility.AllSpecies)
                if (SupportAbility.TryGet(sp, out var d))
                    Assert.LessOrEqual(d.magnitude, 1.0f, sp + " support too strong");
        }
    }
}
