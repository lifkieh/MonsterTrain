using System;
using System.Collections.Generic;

namespace MTA.Core
{
    // Simulator inputs. growthOverride pins grades for tests; otherwise grades
    // roll from the battle seed per the RNG contract in BattleSimulator.
    [Serializable]
    public class UnitConfig
    {
        public string speciesId;
        public int level = 1;
        public GrowthTier[] growthOverride;      // null => roll from seed
        public StatBlock allocated;
        public StatBlock trained;
    }

    [Serializable]
    public class TeamConfig
    {
        public List<UnitConfig> units = new List<UnitConfig>();

        public void Validate()
        {
            if (units == null || units.Count < 1 || units.Count > 3)
                throw new ArgumentException(
                    "TeamConfig requires 1-3 units (1v1/2v2/3v3 per locked onboarding).");
        }
    }
}
