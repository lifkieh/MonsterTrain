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

        // TYM 2.0 Active+Support: 0–2 virtual support species (never built as combat units). Empty ⇒
        // classic battle (byte-identical). When present, units[0] is the single Active.
        public List<string> supports = new List<string>();
        public bool IsSupportBattle => supports != null && supports.Count > 0;

        public void Validate()
        {
            if (units == null || units.Count < 1 || units.Count > 3)
                throw new ArgumentException(
                    "TeamConfig requires 1-3 units (1v1/2v2/3v3 per locked onboarding).");
            if (supports != null)
            {
                if (supports.Count > 2)
                    throw new ArgumentException("At most 2 support monsters.");
                if (supports.Count == 2 && supports[0] == supports[1])
                    throw new ArgumentException("Support slots must be different monsters.");
                // an Active+Support team fields exactly ONE active
                if (supports.Count > 0 && units.Count != 1)
                    throw new ArgumentException("An Active+Support team has exactly 1 active monster.");
            }
        }
    }
}
