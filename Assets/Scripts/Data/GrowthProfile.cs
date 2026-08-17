using System;
using MTA.Core;

namespace MTA.Data
{
    // Inspector-authorable growth tendencies: one weight row per stat.
    // Converts to the Core GrowthWeights the simulator consumes.
    [Serializable]
    public class GrowthProfile
    {
        [Serializable]
        public struct TierWeights
        {
            public float d, c, b, a, s;
            public float[] ToArray() => new[] { d, c, b, a, s };
        }

        // Indexed by (int)Stat: HP, ATK, DEF, SPD, INT, LUCK
        public TierWeights[] perStat = DefaultRows();

        static TierWeights[] DefaultRows()
        {
            var rows = new TierWeights[6];
            for (int i = 0; i < 6; i++)
                rows[i] = new TierWeights { d = 1, c = 2, b = 3, a = 2, s = 1 };
            return rows;
        }

        public GrowthWeights ToWeights()
        {
            var w = new GrowthWeights { perStat = new float[6][] };
            for (int i = 0; i < 6; i++) w.perStat[i] = perStat[i].ToArray();
            return w;
        }
    }
}
