using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase A: attack-style classification is deterministic, and the presentation
    // layer (styles, replay build, drama) never changes the sim hash or event order.
    public class BattleFeelTests
    {
        static SkillData Sk(string id, SkillSlot slot, Stat sc) => new SkillData
        { skillId = id, slot = slot, scalingStat = sc, powerMultiplier = 1f };

        static SpeciesData Sp(string id, int hp, int atk, int def, int spd, int intel, int luck, Stat basicScale)
            => new SpeciesData
            {
                speciesId = id, displayName = id,
                baseStats = new StatBlock { hp = hp, atk = atk, def = def, spd = spd, intel = intel, luck = luck },
                growth = GrowthWeights.Uniform(),
                basicSkill = Sk("b_" + id, SkillSlot.Basic, basicScale),
                activeSkill = Sk("a_" + id, SkillSlot.Active, Stat.ATK),
                ultimateSkill = Sk("u_" + id, SkillSlot.Ultimate, Stat.ATK)
            };

        [Test]
        public void AttackStyle_ClassifiesEachArchetype()
        {
            Assert.AreEqual(AttackStyle.MageCast, AttackStyles.For(Sp("mage", 80, 8, 10, 12, 24, 10, Stat.INT)));
            Assert.AreEqual(AttackStyle.AssassinDash, AttackStyles.For(Sp("assassin", 75, 20, 9, 17, 14, 10, Stat.ATK)));
            Assert.AreEqual(AttackStyle.HeavySmash, AttackStyles.For(Sp("smasher", 100, 24, 12, 12, 6, 8, Stat.ATK)));
            Assert.AreEqual(AttackStyle.MeleeLunge, AttackStyles.For(Sp("tank", 150, 12, 26, 5, 8, 4, Stat.ATK)));
            Assert.AreEqual(AttackStyle.RangedProjectile, AttackStyles.For(Sp("ranger", 65, 16, 7, 22, 12, 9, Stat.ATK)));
        }

        [Test]
        public void AttackStyle_MapIsDeterministic()
        {
            var sps = new[] { Sp("a", 100, 24, 12, 12, 6, 8, Stat.ATK), Sp("b", 80, 8, 10, 12, 24, 10, Stat.INT) };
            var m1 = AttackStyles.Map(sps);
            var m2 = AttackStyles.Map(sps);
            CollectionAssert.AreEquivalent(m1, m2);
            Assert.AreEqual(AttackStyle.HeavySmash, m1["a"]);
            Assert.AreEqual(AttackStyle.MageCast, m1["b"]);
        }

        [Test]
        public void Presentation_KeepsHashAndEventOrder()
        {
            var reg = new SpeciesRegistry(new[] {
                Sp("hero", 100, 26, 12, 14, 10, 20, Stat.ATK),
                Sp("foe", 110, 10, 14, 10, 14, 6, Stat.ATK) });
            var slots = ReplayBuilder.SlotMap(new[] {
                Sp("hero", 100, 26, 12, 14, 10, 20, Stat.ATK),
                Sp("foe", 110, 10, 14, 10, 14, 6, Stat.ATK) });

            var a = new TeamConfig(); var b = new TeamConfig();
            for (int i = 0; i < 3; i++) { a.units.Add(new UnitConfig { speciesId = "hero", level = 5 });
                                          b.units.Add(new UnitConfig { speciesId = "foe", level = 5 }); }

            var r1 = BattleSimulator.Run(a, b, 555, new BalanceConfig(), reg);
            ulong h = r1.logHash;

            // Build presentation artifacts twice — read-only, identical, no mutation.
            var e1 = ReplayBuilder.Build(r1, slots);
            _ = AttackStyles.Map(new[] { Sp("hero", 100, 26, 12, 14, 10, 20, Stat.ATK) });
            _ = BattleDrama.Compute(r1);
            var e2 = ReplayBuilder.Build(r1, slots);

            Assert.AreEqual(e1.Count, e2.Count);
            for (int i = 0; i < e1.Count; i++)
            {
                Assert.AreEqual(e1[i].kind, e2[i].kind);
                Assert.AreEqual(e1[i].t, e2[i].t, 1e-9);
            }
            Assert.AreEqual(h, r1.logHash);                       // building didn't mutate
            var r2 = BattleSimulator.Run(a, b, 555, new BalanceConfig(), reg);
            Assert.AreEqual(h, r2.logHash);                       // same seed → same hash
        }
    }
}
