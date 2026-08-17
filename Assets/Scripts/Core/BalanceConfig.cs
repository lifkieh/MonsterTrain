using System;

namespace MTA.Core
{
    // Immutable-by-discipline: loaded once at boot / test setup, then only read.
    // Every tunable number in the game lives here, never in a C# literal.
    [Serializable]
    public class BalanceConfig
    {
        // --- damage model ---
        public int k = 50;                       // mitigation constant
        public float critPerLuck = 0.005f;       // chance per LUCK point
        public float critCap = 0.30f;
        public float critMultiplier = 1.5f;
        public int minDamage = 1;

        // --- SPD -> attacks-per-second curve (the SPD-stacking brake) ---
        public float apsPerSpdLow = 0.02f;
        public int spdKink = 25;
        public float apsPerSpdHigh = 0.01f;
        public float apsCap = 1.0f;

        // --- growth / progression ---
        public float[] tierMultipliers = { 0.6f, 0.8f, 1.0f, 1.25f, 1.5f }; // D,C,B,A,S
        public float[] defaultGainRates = { 2.5f, 1.0f, 0.8f, 0.6f, 1.0f, 0.4f }; // per Stat
        public SpeciesGainRates[] speciesGainRates = new SpeciesGainRates[0];
        public int statPointsPerLevel = 3;

        // --- training yields, indexed by TrainingType (Str,End,Agi,Int) ---
        public float[] trainingBaseYields = { 2f, 4f, 2f, 2f };

        // --- anti-stall / hard resolve ---
        public float antiStallStart = 75f;
        public float antiStallInterval = 10f;
        public float antiStallIncrement = 0.05f;
        public float hardResolveTime = 120f;

        [Serializable]
        public class SpeciesGainRates      // JsonUtility can't do dictionaries
        {
            public string speciesId;
            public float[] rates = new float[6];
        }

        public float TierMultiplier(GrowthTier t) => tierMultipliers[(int)t];

        public float GainRate(string speciesId, Stat s)
        {
            for (int i = 0; i < speciesGainRates.Length; i++)
                if (speciesGainRates[i].speciesId == speciesId)
                    return speciesGainRates[i].rates[(int)s];
            return defaultGainRates[(int)s];
        }

        // JsonUtility is UnityEngine but has no scene dependency; Core stays
        // headless-testable because tests pass the JSON *string* directly.
        public static BalanceConfig FromJson(string json) =>
            UnityEngine.JsonUtility.FromJson<BalanceConfig>(json);
    }
}
