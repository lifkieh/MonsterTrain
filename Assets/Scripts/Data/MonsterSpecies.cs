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
        public string element;                   // Fire / Water / Nature (elemental triangle)
        public string evolvesTo;                 // speciesId of the evolved form ("" = terminal)
        public int evolveLevel;                  // level required to evolve
        public bool evolutionOnly;               // obtained only via evolution (excluded from wild pools)
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
            element = element,
            evolvesTo = evolvesTo,
            evolveLevel = evolveLevel,
            evolutionOnly = evolutionOnly,
            baseStats = baseStats,
            growth = growth.ToWeights(),
            basicSkill = basicSkill != null ? basicSkill.ToData() : null,
            activeSkill = activeSkill != null ? activeSkill.ToData() : null,
            ultimateSkill = ultimateSkill != null ? ultimateSkill.ToData() : null
        };
    }
}
