using System.Collections.Generic;
using System.Text;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase J: the cinematic director is a READ-ONLY consumer of the sim output.
    // These prove it changes neither the outcome nor the log hash, and that the
    // choreography is fully deterministic (same seed ⇒ same fight).
    public class BattleCinematicTests
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

        static BattleResult Run(SpeciesRegistry reg, int seed)
        {
            var a = new TeamConfig(); var b = new TeamConfig();
            for (int i = 0; i < 3; i++)
            {
                a.units.Add(new UnitConfig { speciesId = "hero", level = 5 });
                b.units.Add(new UnitConfig { speciesId = "healer", level = 5 });
            }
            return BattleSimulator.Run(a, b, seed, new BalanceConfig(), reg);
        }

        static Choreography Choreo(SpeciesRegistry reg, int seed)
        {
            var slots = ReplayBuilder.SlotMap(new[] { Hero(), Healer() });
            var result = Run(reg, seed);
            var replay = ReplayBuilder.Build(result, slots);
            return BattleCinematicDirector.Choreograph(result, replay);
        }

        static string Signature(Choreography c)
        {
            var sb = new StringBuilder();
            sb.Append(c.seed).Append('|').Append((int)c.finisher).Append(';');
            foreach (var b in c.beats)
                sb.Append((int)b.move).Append(',').Append(b.hits).Append(',')
                  .Append(b.crit ? 1 : 0).Append(b.dodge ? 1 : 0).Append(b.launch ? 1 : 0).Append(',')
                  .Append(b.knockback).Append(',').Append(b.hitStop).Append(',')
                  .Append((int)b.cam).Append(',').Append(b.endsBattle ? 1 : 0).Append(';');
            return sb.ToString();
        }

        [Test]
        public void Choreography_IsDeterministic_ForSameSeed()
        {
            var reg = Reg();
            Assert.AreEqual(Signature(Choreo(reg, 777)), Signature(Choreo(reg, 777)));
            // Fresh registry, same seed → still identical (seeded only by logHash).
            Assert.AreEqual(Signature(Choreo(Reg(), 2024)), Signature(Choreo(Reg(), 2024)));
        }

        [Test]
        public void Choreograph_DoesNotChangeOutcomeOrHash()
        {
            var reg = Reg();
            var r1 = Run(reg, 555);
            ulong h1 = r1.logHash; int w1 = r1.winnerTeam;
            var slots = ReplayBuilder.SlotMap(new[] { Hero(), Healer() });
            var replay = ReplayBuilder.Build(r1, slots);
            var cho = BattleCinematicDirector.Choreograph(r1, replay);   // read-only
            Assert.AreEqual(h1, r1.logHash, "director must not mutate the log hash");
            Assert.AreEqual(w1, r1.winnerTeam, "director must not mutate the winner");
            Assert.AreEqual(h1, cho.seed, "choreography is seeded by the log hash");
            var r2 = Run(reg, 555);
            Assert.AreEqual(h1, r2.logHash);
            Assert.AreEqual(w1, r2.winnerTeam);
        }

        [Test]
        public void Choreography_PairsOneToOneWithReplay()
        {
            var reg = Reg();
            var slots = ReplayBuilder.SlotMap(new[] { Hero(), Healer() });
            var result = Run(reg, 99);
            var replay = ReplayBuilder.Build(result, slots);
            var cho = BattleCinematicDirector.Choreograph(result, replay);
            Assert.AreEqual(replay.Count, cho.beats.Count);
            for (int i = 0; i < replay.Count; i++)
            {
                Assert.AreEqual(replay[i].t, cho.beats[i].t, 1e-9, "timeline order preserved");
                Assert.AreEqual(replay[i].actorTeam, cho.beats[i].actorTeam);
                Assert.AreEqual(replay[i].targetSlot, cho.beats[i].targetSlot);
            }
        }

        [Test]
        public void Choreography_ComboAndHitStopWithinBounds()
        {
            var reg = Reg();
            var slots = ReplayBuilder.SlotMap(new[] { Hero(), Healer() });
            var result = Run(reg, 12345);
            var replay = ReplayBuilder.Build(result, slots);
            var cho = BattleCinematicDirector.Choreograph(result, replay);

            bool sawCombo = false;
            for (int i = 0; i < cho.beats.Count; i++)
            {
                var b = cho.beats[i];
                bool offense = !b.isBuff &&
                    (b.move == ChoreoMove.Combo || b.move == ChoreoMove.Skill || b.move == ChoreoMove.Ultimate);
                if (offense)
                {
                    sawCombo = true;
                    if (b.move == ChoreoMove.Ultimate) { Assert.GreaterOrEqual(b.hits, 8); Assert.LessOrEqual(b.hits, 15); }
                    else if (b.crit) { Assert.GreaterOrEqual(b.hits, 5); Assert.LessOrEqual(b.hits, 8); }
                    else { Assert.GreaterOrEqual(b.hits, 2); Assert.LessOrEqual(b.hits, 5); }
                }
                if (b.move == ChoreoMove.Combo || b.move == ChoreoMove.Ultimate ||
                    b.move == ChoreoMove.Skill || b.move == ChoreoMove.Death)
                {
                    if (b.hitStop > 0f)
                    {
                        Assert.GreaterOrEqual(b.hitStop, 0.03f - 1e-6f);
                        Assert.LessOrEqual(b.hitStop, 0.10f + 1e-6f);
                    }
                }
            }
            Assert.IsTrue(sawCombo, "battle should produce at least one combo beat");
        }

        [Test]
        public void Choreography_HasExactlyOneFinisher()
        {
            var reg = Reg();
            var slots = ReplayBuilder.SlotMap(new[] { Hero(), Healer() });
            var result = Run(reg, 314);
            var replay = ReplayBuilder.Build(result, slots);
            var cho = BattleCinematicDirector.Choreograph(result, replay);
            int finishers = 0;
            foreach (var b in cho.beats) if (b.endsBattle) finishers++;
            Assert.AreEqual(1, finishers, "exactly one finishing blow");
            Assert.AreNotEqual(FinisherKind.None, cho.finisher);
        }
    }
}
