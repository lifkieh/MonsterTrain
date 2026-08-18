using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Content depth: evolution transforms an owned monster in place at its level.
    public class EvolutionTests
    {
        static SpeciesData Sp(string id, string evolvesTo, int evolveLevel) => new SpeciesData
        {
            speciesId = id, displayName = id, evolvesTo = evolvesTo, evolveLevel = evolveLevel,
            baseStats = new StatBlock { hp = 100, atk = 20, def = 12, spd = 12, intel = 10, luck = 8 },
            growth = GrowthWeights.Uniform()
        };

        static SaveData Owning(string id, int level, int xp = 0)
        {
            var d = new SaveData();
            d.unlocked.Add(id);
            d.collection.Add(new MonsterSave { speciesId = id, level = level, xp = xp });
            return d;
        }

        [Test]
        public void Evolve_TransformsInPlace_KeepsLevelAndXp()
        {
            var d = Owning("wolf", 12, 5);
            var sp = Sp("wolf", "dire_wolf", 10);
            Assert.IsTrue(Progression.CanEvolve(d, sp));
            Assert.AreEqual("dire_wolf", Progression.Evolve(d, sp));
            Assert.IsNull(d.Find("wolf"));                 // transformed, no longer the base
            var m = d.Find("dire_wolf");
            Assert.IsNotNull(m);
            Assert.AreEqual(12, m.level);
            Assert.AreEqual(5, m.xp);
            Assert.IsTrue(d.IsUnlocked("dire_wolf"));
        }

        [Test]
        public void CanEvolve_FalseBelowLevel()
        {
            var d = Owning("wolf", 9, 0);
            var sp = Sp("wolf", "dire_wolf", 10);
            Assert.IsFalse(Progression.CanEvolve(d, sp));
            Assert.IsNull(Progression.Evolve(d, sp));
            Assert.IsNotNull(d.Find("wolf"));
        }

        [Test]
        public void CanEvolve_FalseWhenNoEvolutionOrNotOwned()
        {
            Assert.IsFalse(Progression.CanEvolve(Owning("wolf", 20), Sp("wolf", "", 0)));   // terminal
            Assert.IsFalse(Progression.CanEvolve(new SaveData(), Sp("wolf", "dire_wolf", 10))); // not owned
        }
    }
}
