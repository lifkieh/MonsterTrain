using System.Collections.Generic;
using MTA.Core;
using MTA.Meta;
using NUnit.Framework;

namespace MTA.Tests
{
    // Phase E: role/rarity classification, collection %, and seen persistence.
    public class CollectionTests
    {
        static SpeciesData Sp(int hp, int atk, int def, int spd, int intel, int luck, EffectKind activeEffect = EffectKind.Damage)
            => new SpeciesData
            {
                speciesId = "x", displayName = "x",
                baseStats = new StatBlock { hp = hp, atk = atk, def = def, spd = spd, intel = intel, luck = luck },
                growth = GrowthWeights.Uniform(),
                basicSkill = new SkillData { skillId = "b", slot = SkillSlot.Basic },
                activeSkill = new SkillData { skillId = "a", slot = SkillSlot.Active, effect = activeEffect },
                ultimateSkill = new SkillData { skillId = "u", slot = SkillSlot.Ultimate }
            };

        [Test]
        public void Role_ClassifiesArchetypes()
        {
            Assert.AreEqual(RoleTag.Tank, MonsterMeta.Role(Sp(150, 12, 26, 5, 8, 4)));
            Assert.AreEqual(RoleTag.Mage, MonsterMeta.Role(Sp(80, 8, 10, 12, 24, 10)));
            Assert.AreEqual(RoleTag.Support, MonsterMeta.Role(Sp(110, 12, 14, 7, 20, 6, EffectKind.Heal)));
            Assert.AreEqual(RoleTag.Assassin, MonsterMeta.Role(Sp(70, 18, 8, 20, 10, 12)));
            Assert.AreEqual(RoleTag.Bruiser, MonsterMeta.Role(Sp(100, 24, 12, 14, 6, 8)));
        }

        [Test]
        public void Rarity_InRangeAndOrdered()
        {
            int low = MonsterMeta.Rarity(Sp(60, 8, 6, 6, 6, 4));
            int high = MonsterMeta.Rarity(Sp(140, 24, 22, 20, 20, 14));
            Assert.GreaterOrEqual(low, 1); Assert.LessOrEqual(high, 5);
            Assert.Greater(high, low);
            Assert.AreEqual(3, MonsterMeta.Stars(3).Length);
        }

        [Test]
        public void OwnedPercent_Computed()
        {
            var roster = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };
            var d = Progression.NewGame(roster);        // 6 unlocked of 8
            Assert.AreEqual(75, MonsterMeta.OwnedPercent(d, roster));
        }

        [Test]
        public void Seen_MarksAndPersists()
        {
            var d = Progression.NewGame(new List<string> { "a", "b", "c", "d", "e", "f", "g" });
            Assert.IsFalse(d.IsSeen("g"));
            d.MarkSeen("g");
            Assert.IsTrue(d.IsSeen("g"));
            var d2 = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(d));
            Assert.IsTrue(d2.IsSeen("g"));
        }
    }
}
