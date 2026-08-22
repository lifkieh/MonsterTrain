using MTA.Core;
using NUnit.Framework;

namespace MTA.Tests
{
    // TYM 2.0 Phase 1 — Active+Support combat math + anti-OP caps.
    public class SupportCombatTests
    {
        static SupportDef S(string species) { SupportAbility.TryGet(species, out var d); return d; }

        [Test]
        public void TwoGuardians_ReductionsCapped()
        {
            var m = SupportCombat.Compute(true, S("turtle"), true, S("golem"));   // shell_wall + bulwark
            Assert.LessOrEqual(m.dmgReduction, 0.40f);
            Assert.LessOrEqual(m.redirectFrac, 0.5f);
        }

        [Test]
        public void TwoBuffers_AttackAppliedButCapped()
        {
            var m = SupportCombat.Compute(true, S("fire_lizard"), true, S("inferno_drake"));  // ignite + drake_fury
            Assert.Greater(m.atkMult, 1.0f);        // applied
            Assert.LessOrEqual(m.atkMult, 1.5f);    // capped, no runaway
        }

        [Test]
        public void Debuffer_WeakensEnemy_Capped()
        {
            var m = SupportCombat.Compute(true, S("dire_wolf"), false, default);   // alpha_howl
            Assert.Greater(m.enemyDefReduction, 0f);
            Assert.LessOrEqual(m.enemyDefReduction, 0.30f);
        }

        [Test]
        public void NoSupports_IsNeutral()
        {
            var m = SupportCombat.Compute(false, default, false, default);
            Assert.AreEqual(1f, m.atkMult, 1e-5f);
            Assert.AreEqual(0f, m.dmgReduction, 1e-5f);
            Assert.AreEqual(0f, m.summonDps, 1e-5f);
        }
    }
}
