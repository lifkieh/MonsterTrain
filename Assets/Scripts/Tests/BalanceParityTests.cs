using MTA.Core;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase K: the parity framework (controlled variance + element triangle +
    // value-balanced stats). Uses the tuned BalanceConfig defaults.
    public class BalanceParityTests
    {
        static BalanceConfig Cfg() => new BalanceConfig();

        [Test]
        public void Element2_0_AdvantageDisadvantageNeutralVoid()
        {
            var c = Cfg();
            // advantage ×strong, disadvantage ×weak (derived table)
            Assert.AreEqual(c.elementStrongMult, StatMath.ElementMultiplier("Fire", "Nature", c), 1e-5f);      // Fire > Nature
            Assert.AreEqual(c.elementWeakMult,   StatMath.ElementMultiplier("Nature", "Fire", c), 1e-5f);
            Assert.AreEqual(c.elementStrongMult, StatMath.ElementMultiplier("Lightning", "Water", c), 1e-5f);  // Lightning > Water
            Assert.AreEqual(c.elementStrongMult, StatMath.ElementMultiplier("Light", "Shadow", c), 1e-5f);     // Light > Shadow
            Assert.AreEqual(c.elementStrongMult, StatMath.ElementMultiplier("Shadow", "Light", c), 1e-5f);     // Shadow > Light (mutual)
            // neutral cases
            Assert.AreEqual(1f, StatMath.ElementMultiplier("Fire", "Fire", c), 1e-6f);
            Assert.AreEqual(1f, StatMath.ElementMultiplier("Fire", "Metal", c), 1e-6f);   // unrelated pair
            Assert.AreEqual(1f, StatMath.ElementMultiplier("Fire", "", c), 1e-6f);
            // guaranteed crit on advantage only
            Assert.IsTrue(StatMath.ElementForcesCrit("Fire", "Nature"));
            Assert.IsFalse(StatMath.ElementForcesCrit("Nature", "Fire"));
            // Void is pure neutral, no crit either way
            Assert.AreEqual(1f, StatMath.ElementMultiplier("Void", "Fire", c), 1e-6f);
            Assert.AreEqual(1f, StatMath.ElementMultiplier("Fire", "Void", c), 1e-6f);
            Assert.IsFalse(StatMath.ElementForcesCrit("Void", "Fire"));
            Assert.IsFalse(StatMath.ElementForcesCrit("Fire", "Void"));
        }

        [Test]
        public void Dodge_ScalesWithLuckAndIsCapped()
        {
            var c = Cfg();
            Assert.Greater(StatMath.DodgeChance(0, 40, c), StatMath.DodgeChance(0, 0, c));   // defender LUCK helps
            Assert.Less(StatMath.DodgeChance(40, 0, c), StatMath.DodgeChance(0, 0, c));      // attacker LUCK erodes
            Assert.LessOrEqual(StatMath.DodgeChance(0, 100000, c), c.dodgeCap + 1e-6f);      // capped
            Assert.GreaterOrEqual(StatMath.DodgeChance(100000, 0, c), 0f);                   // floored
        }

        [Test]
        public void MirrorDuel_IsNearFifty()
        {
            var c = Cfg();
            double wr = BalanceLab.Duel(c, BalanceLab.Neutral(), BalanceLab.Neutral(), 1000);
            Assert.That(wr, Is.InRange(0.42, 0.58), "identical monsters must be a coin flip: " + wr);
        }

        [Test]
        public void HpDefBudgetSwap_IsNearParity()
        {
            var c = Cfg();
            double wr = BalanceLab.BudgetSwap(c, BalanceLab.Neutral(), Stat.HP, Stat.DEF, 10, 1000);
            Assert.That(wr, Is.InRange(0.40, 0.60), "HP and DEF must carry ~equal value: " + wr);
        }

        [Test]
        public void StrongerWinsMore_ButNotDeterministically()
        {
            var c = Cfg();
            var baseb = BalanceLab.Neutral();
            // A SLIGHT edge (all stats ×1.05) must win more but NOT be a 90%+ auto-win.
            double slightUp = BalanceLab.Duel(c, BalanceLab.Scale(baseb, 1.05), baseb, 1000);
            double slightDown = BalanceLab.Duel(c, BalanceLab.Scale(baseb, 0.95), baseb, 1000);
            Assert.Greater(slightUp, 0.52, "a slightly stronger monster should win a bit more");
            Assert.Less(slightUp, 0.88, "a slight edge must NOT produce a 90%+ auto-win");
            Assert.Less(slightDown, 0.48, "a slightly weaker monster should win a bit less");
            // A LARGE edge should clearly dominate (but this is not a 'slight' edge).
            double big = BalanceLab.Duel(c, BalanceLab.Scale(baseb, 1.30), baseb, 1000);
            Assert.Greater(big, 0.80, "a large power advantage should clearly win");
        }
    }
}
