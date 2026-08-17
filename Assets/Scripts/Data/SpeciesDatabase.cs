using System.Collections.Generic;
using MTA.Core;
using UnityEngine;

namespace MTA.Data
{
    // Boot-time loader: SO assets -> Core registry. The ONLY place assets are
    // located; everything downstream iterates registry.All (never enumerates
    // monsters in code). Swap to Addressables behind this method at 100+.
    public static class SpeciesDatabase
    {
        public static SpeciesRegistry LoadFromResources(string folder = "Monsters")
        {
            var assets = Resources.LoadAll<MonsterSpecies>(folder);
            var list = new List<SpeciesData>(assets.Length);
            foreach (var a in assets) list.Add(a.ToData());
            return new SpeciesRegistry(list);
        }

        public static BalanceConfig LoadBalance()
        {
            // Resources works on EVERY platform. On Android, StreamingAssets is
            // packed inside the APK and is not File-readable, so prefer the
            // Resources copy; fall back to StreamingAssets in the editor/standalone.
            var ta = Resources.Load<TextAsset>("balance");
            if (ta != null) return BalanceConfig.FromJson(ta.text);

            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "balance.json");
            return BalanceConfig.FromJson(System.IO.File.ReadAllText(path));
        }
    }
}
