# Test run — eval 1: data-layer (iteration 1, skill v4)

## Active Role

Lead Architect. (Unity implementation and architecture explicitly requested.)

## Objective

A monster data layer where all gameplay data is externalized: 12 monsters now,
100+ later, with monster #13 and monster #50 requiring zero code changes, growth
grades and skills fully data-driven, and saves that survive content updates.

## Design Decision

ScriptableObject definitions + a boot-time id registry + a plain-C# instance
layer + versioned JSON saves that store ids only. Content lives in assets and
`balance.json`; code only interprets.

## Benefits

- New content = new `.asset` files. No recompile, no code review, no risk to
  shipped systems.
- Saves reference `speciesId`/`skillId` strings, so old saves keep working when
  monster #50 ships.
- The same data layer feeds the headless battle simulator, so balance testing
  needs no scenes.

## Risks

- `Resources.LoadAll` is fine at 12–100 assets but loads everything at boot;
  mitigated by the Addressables path in Scalability Notes.
- String ids can typo; mitigated by an editor validation script (one-time cost,
  listed below).

## Scope Classification

MVP Safe. (This is Build Phase 1–2 foundation work.)

## Recommendation

Implement now, exactly once, and let every later system consume it.

## Unity Architecture

```
Assets/
  Scripts/
    Core/        // plain C#: StatMath, BattleSimulator, SaveGame, BalanceConfig
    Data/        // ScriptableObject classes (definitions only)
    Battle/      // view layer (replays sim event log)
    Meta/        // training, progression, career screens
  GameData/
    Monsters/    // MonsterSpecies .asset — ONE FILE PER MONSTER
    Skills/      // SkillDefinition .asset
  StreamingAssets/
    balance.json // every tunable number
```

## Data Structure

```csharp
public enum Stat { HP, ATK, DEF, SPD, INT, LUCK }
public enum GrowthTier { D, C, B, A, S }          // ordered worst→best
public enum SkillSlot { Basic, Active, Ultimate }

[System.Serializable]
public struct StatBlock
{
    public int hp, atk, def, spd, intel, luck;
    public int Get(Stat s) => s switch { Stat.HP=>hp, Stat.ATK=>atk,
        Stat.DEF=>def, Stat.SPD=>spd, Stat.INT=>intel, Stat.LUCK=>luck, _=>0 };
}

// Growth is data: per-stat WEIGHTS over tiers. Species define tendencies;
// each acquired monster rolls its actual grades from these weights.
[System.Serializable]
public class GrowthProfile
{
    [System.Serializable]
    public struct StatWeights { public float d, c, b, a, s; }
    public StatWeights[] perStat = new StatWeights[6];   // indexed by (int)Stat
    public GrowthTier Roll(Stat stat, System.Random rng) { /* weighted pick */ }
}
```

## ScriptableObject Design

```csharp
[CreateAssetMenu(menuName = "MTA/Monster Species")]
public class MonsterSpecies : ScriptableObject
{
    public string speciesId;        // "fire_lizard" — stable save key, never rename
    public string displayName;
    public StatBlock baseStats;     // level-1 values
    public GrowthProfile growth;    // data-driven grades
    public SkillDefinition basicSkill, activeSkill, ultimateSkill;
    public Sprite portrait, battleSprite;
}

[CreateAssetMenu(menuName = "MTA/Skill")]
public class SkillDefinition : ScriptableObject
{
    public string skillId;
    public string displayName;
    public SkillSlot slot;
    public Stat scalingStat;        // ATK or INT
    public float powerMultiplier;
    public float cooldownSeconds;
    public EffectKind effect;       // Damage / Heal / Buff / Debuff — data-describable
    public float magnitude, duration;
}

// Boot-time registry: the ONLY place assets are located.
public class SpeciesRegistry
{
    readonly Dictionary<string, MonsterSpecies> byId;
    public SpeciesRegistry() =>
        byId = Resources.LoadAll<MonsterSpecies>("Monsters").ToDictionary(m => m.speciesId);
    public MonsterSpecies Get(string id) => byId[id];
    public IEnumerable<MonsterSpecies> All => byId.Values;
}
```

**Adding monster #13 (and #50):** right-click → Create → MTA → Monster Species;
fill stats, growth weights, drag three skill assets and two sprites; done. The
registry discovers it at next boot. Career mode, UI lists, and the simulator all
iterate `registry.All` — nothing enumerates monsters in code. Skills add the
same way. Zero code changes, both at #13 and at #50.

## Save Design

```csharp
[System.Serializable]
public class MonsterInstance          // owned monster — plain C#, no asset refs
{
    public string instanceId;         // GUID at acquisition
    public string speciesId;          // resolve via registry — ids only, never refs
    public string nickname;
    public int level, exp, unspentPoints;
    public GrowthTier[] growth = new GrowthTier[6];  // rolled once, persisted
    public StatBlock allocated, trained;
}

[System.Serializable]
public class SaveGame
{
    public int saveVersion = 1;       // bump on schema change; migrate on load
    public List<MonsterInstance> monsters = new();
    public string[] activeTeam = new string[3];
    public int highestLeagueCleared;
    public long softCurrency;
    public string lastSaveUtc;
}
```

`JsonUtility` → temp file → atomic replace at
`Application.persistentDataPath/save.json`. **Content-update compatibility:**
saves never contain species data, only ids and per-instance state, so shipping
monsters #13–#50 touches zero bytes of any existing save. Removing/renaming a
speciesId is the only breaking operation — treat ids as append-only. New
*fields* ride the `saveVersion` bump with defaults for old saves.

## Scalability Notes

- **12 → 100+ monsters:** swap `Resources.LoadAll` for Addressables with label
  `"monster-species"` behind the same `SpeciesRegistry` interface; callers never
  change. Defer until asset count or memory says so — not an MVP task.
- **Validation:** a small editor script asserts unique non-empty ids, three
  skills per species, and weights summing > 0 — catches data errors at author
  time instead of runtime.
- **Balance at scale:** because definitions are plain data, the headless
  simulator can sweep all species pairings automatically as the roster grows.
- Formulas (`StatMath`) read multipliers from `balance.json`, so tuning 100
  monsters later is a data exercise, not a refactor.
