using MTA.Core;
using UnityEngine;

namespace MTA.Data
{
    // One .asset per skill. Adding a skill = creating an asset, never code.
    [CreateAssetMenu(menuName = "MTA/Skill", fileName = "skill_new")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId;                   // lowercase_snake, stable save key
        public string displayName;
        public SkillSlot slot;
        public Stat scalingStat = Stat.ATK;      // ATK physical, INT skill power (spec)
        public float powerMultiplier = 1f;
        public float cooldownSeconds = 8f;       // actives
        public float chargeTime = 15f;           // ultimates
        public EffectKind effect = EffectKind.Damage;
        public TargetRule targetRule = TargetRule.Enemy;
        public Stat affectedStat = Stat.ATK;     // buff/debuff
        [Range(0f, 1f)] public float magnitudePercent = 0.2f;
        public float durationSeconds = 8f;

        public SkillData ToData() => new SkillData
        {
            skillId = skillId, displayName = displayName, slot = slot,
            scalingStat = scalingStat, powerMultiplier = powerMultiplier,
            cooldownSeconds = cooldownSeconds, chargeTime = chargeTime,
            effect = effect, targetRule = targetRule, affectedStat = affectedStat,
            magnitudePercent = magnitudePercent, durationSeconds = durationSeconds
        };
    }
}
