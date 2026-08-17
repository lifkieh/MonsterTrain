using System;

namespace MTA.Core
{
    // Per-monster save/runtime data. Plain C#; stores ids only, never asset refs.
    [Serializable]
    public class MonsterInstance
    {
        public string instanceId;                // GUID at creation
        public string speciesId;                 // resolve via SpeciesRegistry
        public string nickname;
        public int level = 1;
        public int exp;                          // unused in Phase 1 (levels set directly)
        public int unspentPoints;
        public GrowthTier[] growth = new GrowthTier[6];   // rolled once, hidden in UI
        public StatBlock allocated;
        public StatBlock trained;

        public static MonsterInstance Create(SpeciesData species, int level,
            Random rng, GrowthTier[] growthOverride = null)
        {
            var inst = new MonsterInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                speciesId = species.speciesId,
                nickname = species.displayName,
                level = Math.Max(1, level)
            };
            if (growthOverride != null)
                Array.Copy(growthOverride, inst.growth, 6);
            else
                for (int i = 0; i < 6; i++)      // fixed stat order 0..5: RNG contract
                    inst.growth[i] = species.growth.Roll(i, rng);
            return inst;
        }
    }
}
