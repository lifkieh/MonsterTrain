using System.Collections.Generic;

namespace MTA.Core
{
    // id -> SpeciesData. Test-injectable; the Data layer provides a loader that
    // builds one from ScriptableObjects at boot. Ids are append-only save keys.
    public class SpeciesRegistry
    {
        readonly Dictionary<string, SpeciesData> byId = new Dictionary<string, SpeciesData>();

        public SpeciesRegistry(IEnumerable<SpeciesData> species)
        {
            foreach (var s in species)
            {
                if (byId.ContainsKey(s.speciesId))
                    throw new System.ArgumentException("Duplicate speciesId: " + s.speciesId);
                byId[s.speciesId] = s;
            }
        }

        public SpeciesData Get(string id)
        {
            if (!byId.TryGetValue(id, out var s))
                throw new KeyNotFoundException("Unknown speciesId: " + id);
            return s;
        }

        public IEnumerable<SpeciesData> All => byId.Values;
        public int Count => byId.Count;
    }
}
