using System.Collections.Generic;
using MTA.Core;
using NUnit.Framework;

namespace MTA.Tests
{
    // Tag mode (Phase AA follow-up): only the FRONT-living monster of each team fights;
    // the next slot tags in when the front dies, and reserves are never acted-by or
    // targeted while benched. Pure sim check — proves the front-only invariant + that the
    // tag path stays deterministic. The brawl (default) path is covered elsewhere and is
    // byte-identical (tagMode defaults false).
    public class TagModeTests
    {
        static SkillData Sk(string id, SkillSlot slot, Stat sc, float mult, EffectKind e,
            float cd = 0f, float charge = 15f) => new SkillData
        {
            skillId = id, slot = slot, scalingStat = sc, powerMultiplier = mult,
            cooldownSeconds = cd, chargeTime = charge, effect = e, targetRule = TargetRule.Enemy
        };

        static SpeciesData Fighter() => new SpeciesData
        {
            speciesId = "fighter", displayName = "Fighter",
            baseStats = new StatBlock { hp = 70, atk = 34, def = 8, spd = 14, intel = 8, luck = 12 },
            growth = GrowthWeights.Uniform(),
            basicSkill = Sk("strike", SkillSlot.Basic, Stat.ATK, 1.2f, EffectKind.Damage),
            activeSkill = Sk("power", SkillSlot.Active, Stat.ATK, 2.6f, EffectKind.Damage, cd: 6f),
            ultimateSkill = Sk("finish", SkillSlot.Ultimate, Stat.ATK, 3.6f, EffectKind.Damage, charge: 12f)
        };

        static SpeciesRegistry Reg() => new SpeciesRegistry(new[] { Fighter() });

        static BattleResult RunTag(int seed, bool tag)
        {
            var a = new TeamConfig(); var b = new TeamConfig();
            for (int i = 0; i < 3; i++)
            {
                a.units.Add(new UnitConfig { speciesId = "fighter", level = 5 });
                b.units.Add(new UnitConfig { speciesId = "fighter", level = 5 });
            }
            return BattleSimulator.Run(a, b, seed, new BalanceConfig(), Reg(), tag);
        }

        // Walk the log in order; a unit is "front" when no lower slot on its team is still alive.
        [Test]
        public void OnlyFrontLivingActsAndIsTargeted()
        {
            foreach (int seed in new[] { 1, 7, 42, 777, 2024 })
            {
                var r = RunTag(seed, true);
                var dead = new HashSet<int>();               // team*100+slot of dead units
                int Key(int t, int s) => t * 100 + s;
                bool FrontLiving(int team, int slot)
                {
                    for (int ss = 0; ss < slot; ss++) if (!dead.Contains(Key(team, ss))) return false;
                    return true;                              // no living lower slot => this is the front
                }
                foreach (var e in r.events)
                {
                    if (e.kind == "Died") { dead.Add(Key(e.targetTeam, e.targetSlot)); continue; }
                    if (e.kind != "Action") continue;
                    Assert.IsTrue(FrontLiving(e.actorTeam, e.actorSlot),
                        $"seed {seed}: a benched unit acted — actor ({e.actorTeam},{e.actorSlot})");
                    if (e.targetTeam != e.actorTeam)          // damage/debuff hits the enemy front only
                        Assert.IsTrue(FrontLiving(e.targetTeam, e.targetSlot),
                            $"seed {seed}: a benched enemy was targeted — target ({e.targetTeam},{e.targetSlot})");
                }
            }
        }

        [Test]
        public void TagBattleIsDeterministic()
        {
            for (int seed = 1; seed <= 5; seed++)
                Assert.AreEqual(RunTag(seed, true).logHash, RunTag(seed, true).logHash, "same seed => same tag hash");
        }

        [Test]
        public void BrawlPathUnchanged_When_TagOff()
        {
            // tagMode omitted (default false) must equal explicit false — the brawl path is intact.
            for (int seed = 1; seed <= 5; seed++)
            {
                var a = new TeamConfig(); var b = new TeamConfig();
                for (int i = 0; i < 3; i++) { a.units.Add(new UnitConfig { speciesId = "fighter", level = 5 }); b.units.Add(new UnitConfig { speciesId = "fighter", level = 5 }); }
                var reg = Reg(); var cfg = new BalanceConfig();
                var withDefault = BattleSimulator.Run(a, b, seed, cfg, reg);
                var withFalse = BattleSimulator.Run(a, b, seed, cfg, reg, false);
                Assert.AreEqual(withDefault.logHash, withFalse.logHash, "brawl default == explicit tagMode:false");
            }
        }
    }
}
