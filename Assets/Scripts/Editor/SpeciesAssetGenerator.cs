#if UNITY_EDITOR
using MTA.Core;
using MTA.Data;
using UnityEditor;
using UnityEngine;

namespace MTA.EditorTools
{
    // One-shot generator: builds the 10-skill shared pool and all 12 species
    // assets from the approved GDD tables. Run once, then tune in the inspector.
    // Deleting and re-running overwrites (ids are stable, so saves are safe).
    public static class SpeciesAssetGenerator
    {
        const string SkillDir = "Assets/Resources/Skills";
        const string MonsterDir = "Assets/Resources/Monsters";

        [MenuItem("MTA/Generate Phase 1 Content")]
        public static void Generate()
        {
            System.IO.Directory.CreateDirectory(SkillDir);
            System.IO.Directory.CreateDirectory(MonsterDir);

            // ---- shared skill pool (per Phase 1 spec; species sets are Phase 3) ----
            var strike     = Skill("strike", SkillSlot.Basic, Stat.ATK, 1.0f, EffectKind.Damage);
            var zap        = Skill("zap", SkillSlot.Basic, Stat.INT, 1.0f, EffectKind.Damage);
            var powerStrike= Skill("power_strike", SkillSlot.Active, Stat.ATK, 2.8f, EffectKind.Damage, cd: 8f);
            var sparkBurst = Skill("spark_burst", SkillSlot.Active, Stat.INT, 2.8f, EffectKind.Damage, cd: 8f);
            var mend       = Skill("mend", SkillSlot.Active, Stat.INT, 2.5f, EffectKind.Heal, cd: 10f, rule: TargetRule.Ally);
            var warCry     = Skill("war_cry", SkillSlot.Active, Stat.ATK, 0f, EffectKind.Buff, cd: 12f,
                                   rule: TargetRule.Self, affected: Stat.ATK, mag: 0.20f, dur: 8f);
            var slowHex    = Skill("slow_hex", SkillSlot.Active, Stat.INT, 0f, EffectKind.Debuff, cd: 10f,
                                   rule: TargetRule.Enemy, affected: Stat.SPD, mag: 0.20f, dur: 8f);
            var savageRend = Skill("savage_rend", SkillSlot.Ultimate, Stat.ATK, 3.8f, EffectKind.Damage, charge: 15f);
            var mindBlast  = Skill("mind_blast", SkillSlot.Ultimate, Stat.INT, 3.8f, EffectKind.Damage, charge: 15f);
            var rally      = Skill("rally", SkillSlot.Ultimate, Stat.ATK, 0f, EffectKind.Buff, charge: 18f,
                                   rule: TargetRule.AllAllies, affected: Stat.ATK, mag: 0.15f, dur: 10f);

            // ---- 12 species: GDD base stats + growth tendencies + role skill sets ----
            Species("slime", "Slime", B(120,16,18, 8,10, 6), "ACBDCB", strike, warCry, savageRend);
            Species("wolf", "Wolf", B(100,24,12,14, 6, 8), "BACBDC", strike, powerStrike, savageRend);
            Species("fire_lizard","Fire Lizard",B( 90,18,11,11,18, 7), "CBCCAC", zap, sparkBurst, mindBlast);
            Species("bat", "Bat", B( 70,18, 8,20,10,12), "DBDSCB", strike, powerStrike, savageRend);
            Species("mushroom_beast","Mushroom Beast",B(110,12,14,7,20,6),"BDBDAC", zap, mend, rally);
            Species("spider", "Spider", B( 75,20, 9,17,14,10), "DBDABB", strike, slowHex, savageRend);
            Species("turtle", "Turtle", B(150,12,26, 5, 8, 4), "ADSDCD", strike, warCry, rally);
            Species("goblin", "Goblin", B( 95,21,12,13, 9,14), "CBCBDA", strike, powerStrike, savageRend);
            Species("ghost", "Ghost", B( 80, 8,10,12,24,10), "CDCBSB", zap, sparkBurst, mindBlast);
            Species("bee", "Bee", B( 65,16, 7,22,12, 9), "DCDSBB", strike, slowHex, rally);
            Species("golem", "Golem", B(140,20,22, 4, 6, 3), "SBADDD", strike, warCry, savageRend);
            Species("dragonling","Dragonling", B( 85,17,12,10,16, 8), "BABBAB", strike, sparkBurst, mindBlast);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MTA: generated 10 skills + 12 species under Assets/Resources/.");
        }

        static StatBlock B(int hp,int atk,int def,int spd,int intel,int luck) =>
            new StatBlock { hp=hp, atk=atk, def=def, spd=spd, intel=intel, luck=luck };

        static SkillDefinition Skill(string id, SkillSlot slot, Stat scaling, float mult,
            EffectKind effect, float cd = 0f, float charge = 15f,
            TargetRule rule = TargetRule.Enemy, Stat affected = Stat.ATK,
            float mag = 0f, float dur = 0f)
        {
            var s = ScriptableObject.CreateInstance<SkillDefinition>();
            s.skillId = id; s.displayName = id.Replace('_', ' ');
            s.slot = slot; s.scalingStat = scaling; s.powerMultiplier = mult;
            s.cooldownSeconds = cd; s.chargeTime = charge; s.effect = effect;
            s.targetRule = rule; s.affectedStat = affected;
            s.magnitudePercent = mag; s.durationSeconds = dur;
            AssetDatabase.CreateAsset(s, SkillDir + "/" + id + ".asset");
            return s;
        }

        // tendency letter -> weight pyramid centered on that tier: 3/2/1 falloff
        static void Species(string id, string name, StatBlock baseStats, string tendencies,
            SkillDefinition basic, SkillDefinition active, SkillDefinition ult)
        {
            var sp = ScriptableObject.CreateInstance<MonsterSpecies>();
            sp.speciesId = id; sp.displayName = name; sp.baseStats = baseStats;
            sp.basicSkill = basic; sp.activeSkill = active; sp.ultimateSkill = ult;
            sp.growth = new GrowthProfile();
            for (int i = 0; i < 6; i++)
            {
                int center = "DCBAS".IndexOf(tendencies[i]);
                var row = new GrowthProfile.TierWeights();
                float[] w = new float[5];
                for (int t = 0; t < 5; t++)
                    w[t] = Mathf.Max(0, 3 - Mathf.Abs(t - center));
                row.d = w[0]; row.c = w[1]; row.b = w[2]; row.a = w[3]; row.s = w[4];
                sp.growth.perStat[i] = row;
            }
            AssetDatabase.CreateAsset(sp, MonsterDir + "/" + id + ".asset");
        }
    }
}
#endif
