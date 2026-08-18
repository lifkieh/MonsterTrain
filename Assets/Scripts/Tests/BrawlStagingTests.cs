using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase O-1: the brawl view drives every choreography from the event's actor/target
    // resolved through the (team,slot) UnitView map. This invariant — every replay event
    // resolves to a spawned unit — is what kills the "statue" bug (no event is ever
    // attributed to a missing or wrong view). Pure data check over the sim/replay output.
    public class BrawlStagingTests
    {
        static SkillData Sk(string id, SkillSlot slot, Stat sc, float mult, EffectKind e,
            float cd = 0f, float charge = 15f, TargetRule rule = TargetRule.Enemy) => new SkillData
        {
            skillId = id, slot = slot, scalingStat = sc, powerMultiplier = mult,
            cooldownSeconds = cd, chargeTime = charge, effect = e, targetRule = rule
        };

        static SpeciesData Hero() => new SpeciesData
        {
            speciesId = "hero", displayName = "Hero",
            baseStats = new StatBlock { hp = 100, atk = 28, def = 12, spd = 14, intel = 10, luck = 20 },
            growth = GrowthWeights.Uniform(),
            basicSkill = Sk("strike", SkillSlot.Basic, Stat.ATK, 1f, EffectKind.Damage),
            activeSkill = Sk("power_strike", SkillSlot.Active, Stat.ATK, 2.8f, EffectKind.Damage, cd: 8f),
            ultimateSkill = Sk("savage_rend", SkillSlot.Ultimate, Stat.ATK, 3.8f, EffectKind.Damage, charge: 15f)
        };

        static SpeciesData Healer() => new SpeciesData
        {
            speciesId = "healer", displayName = "Healer",
            baseStats = new StatBlock { hp = 120, atk = 8, def = 14, spd = 10, intel = 20, luck = 6 },
            growth = GrowthWeights.Uniform(),
            basicSkill = Sk("zap", SkillSlot.Basic, Stat.INT, 1f, EffectKind.Damage),
            activeSkill = Sk("mend", SkillSlot.Active, Stat.INT, 2.5f, EffectKind.Heal, cd: 10f, rule: TargetRule.Ally),
            ultimateSkill = Sk("savage_rend", SkillSlot.Ultimate, Stat.ATK, 3.8f, EffectKind.Damage, charge: 15f)
        };

        static SpeciesRegistry Reg() => new SpeciesRegistry(new[] { Hero(), Healer() });

        static int Key(int team, int slot) => team * 100 + slot;

        static (BattleResult result, List<ReplayEvent> replay) RunReplay(int seed)
        {
            var reg = Reg();
            var a = new TeamConfig(); var b = new TeamConfig();
            for (int i = 0; i < 3; i++)
            {
                a.units.Add(new UnitConfig { speciesId = "hero", level = 5 });
                b.units.Add(new UnitConfig { speciesId = "healer", level = 5 });
            }
            var result = BattleSimulator.Run(a, b, seed, new BalanceConfig(), reg);
            var slots = ReplayBuilder.SlotMap(new[] { Hero(), Healer() });
            return (result, ReplayBuilder.Build(result, slots));
        }

        [Test]
        public void EveryEventActorAndTargetResolvesToASpawnedUnit()
        {
            foreach (int seed in new[] { 1, 42, 777, 2024, 99999 })
            {
                var (result, replay) = RunReplay(seed);

                // The spawn set is exactly the units a brawl view creates (one per unit).
                var spawned = new HashSet<int>();
                foreach (var ev in replay)
                    if (ev.kind == ReplayEventKind.Spawn) spawned.Add(Key(ev.actorTeam, ev.actorSlot));

                Assert.AreEqual(6, spawned.Count, "seed " + seed + ": expected 3v3 = 6 spawned units");

                foreach (var ev in replay)
                {
                    if (ev.actorSlot >= 0)
                        Assert.IsTrue(spawned.Contains(Key(ev.actorTeam, ev.actorSlot)),
                            "seed " + seed + ": event " + ev.kind + " actor (" + ev.actorTeam + "," + ev.actorSlot + ") has no UnitView");
                    if (ev.targetSlot >= 0)
                        Assert.IsTrue(spawned.Contains(Key(ev.targetTeam, ev.targetSlot)),
                            "seed " + seed + ": event " + ev.kind + " target (" + ev.targetTeam + "," + ev.targetSlot + ") has no UnitView");
                }
            }
        }
    }
}
