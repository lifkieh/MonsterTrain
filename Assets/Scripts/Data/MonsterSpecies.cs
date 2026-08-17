using MTA.Core;
using UnityEngine;

namespace MTA.Data
{
    // One .asset per species. Adding monster #13 (or #50) = creating an asset
    // plus sprites — zero code changes. (locked architecture)
    [CreateAssetMenu(menuName = "MTA/Monster Species", fileName = "species_new")]
    public class MonsterSpecies : ScriptableObject
    {
        public string speciesId;                 // lowercase_snake — NEVER rename (save key)
        public string displayName;
        public StatBlock baseStats;              // level-1 values
        public GrowthProfile growth = new GrowthProfile();
        public SkillDefinition basicSkill;
        public SkillDefinition activeSkill;
        public SkillDefinition ultimateSkill;    // exactly 3 skills, per spec
        public Sprite portrait;                  // placeholders in Phase 1
        public Sprite battleSprite;

        public SpeciesData ToData() => new SpeciesData
        {
            speciesId = speciesId,
            displayName = displayName,
            baseStats = baseStats,
            growth = growth.ToWeights(),
            basicSkill = basicSkill != null ? basicSkill.ToData() : null,
            activeSkill = activeSkill != null ? activeSkill.ToData() : null,
            ultimateSkill = ultimateSkill != null ? ultimateSkill.ToData() : null
        };
    }
}
