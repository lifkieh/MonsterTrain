using System.Collections.Generic;
using UnityEngine;

namespace MTA.Battle
{
    // Loads real monster battle sprites (CC0 pack, Resources/MonSprites) with a
    // cache. Pokemon-style: player side shows the BACK sprite, enemy the FRONT.
    // Returns null if a sprite is missing so callers can fall back to procedural art.
    public static class MonsterVisual
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite For(string speciesId, bool back)
        {
            string key = speciesId + (back ? "_back" : "_front");
            if (_cache.TryGetValue(key, out var s)) return s;
            s = Resources.Load<Sprite>("MonSprites/" + key);
            _cache[key] = s;
            return s;
        }

        public static bool Available(string speciesId) => For(speciesId, false) != null;
    }
}
