# TYM 2.0 — PHASE A: GAP ANALYSIS

Current build = 50600d4 (auto-battler + full presentation/character-direction passes). Audited against
the 16-phase roadmap. Legend: ✅ exists · 🟡 partial · ❌ missing.

| # | Roadmap system | Status | Notes (current code) |
|---|---|:--:|---|
| 1 | Active + Support combat | ❌ | Sim runs 1–3 all-active units (`BattleSimulator`). No active/support split. |
| 1 | 3 skills per monster | ✅ | `SpeciesData.basicSkill/activeSkill/ultimateSkill` already exist. |
| 1 | Unique support ability / monster | ❌ | No support-skill field or effects. |
| 2 | Expanded element system (10) | ❌ | Hardcoded Fire/Water/Nature triangle in `StatMath.ElementMultiplier`. |
| 3 | Void element | ❌ | — |
| 3 | Chronovore (mythical void) | ❌ | — |
| 4 | Feed system | ❌ | Only coin-`Train` grants xp (`Progression.Train`). No food items. |
| 5 | Stat allocation | 🟡 | `LevelMath.AllocatePoint`/`unspentPoints`/`allocated` exist but "Phase-1 debug", not wired to level-up or UI. |
| 6 | Training / bond / skill mastery | 🟡 | `Progression.Train` (coin→xp) exists; no bond, no mastery, no mastery→damage. |
| 8 | Coin economy | ✅ | `SaveData.coins`, battle/quest rewards. |
| 9 | Monster gacha | ❌ | Monsters unlock via player-level. No summon. |
| 9 | Pity | ❌ | — |
| 10 | Monster selling | ❌ | — |
| 11 | Aura gacha | ❌ | — |
| 12 | Supercoin | ❌ | — |
| 13 | Biome expansion (9 new) | 🟡 | 3 painted biomes (Fire/Water/Nature) exist; no Tundra/Desert/etc. |
| 14 | Quest system | ✅/🟡 | Daily/weekly/story quests exist (`Quests`); needs new reward types (food/summons/essence). |
| 15 | Endgame (tower/raids/trials) | ❌ | Career ladder exists; no endless tower / trials / void challenges. |
| 16 | Store readiness | 🟡 | Icon/screenshots pipeline exists (prior passes). |

## Preserve (roadmap mandate)
Deterministic combat (`BattleSimulator.Run`, single `Random(seed)`, FNV log hash), save compatibility
(`SaveData` v2 additive), existing 21-monster roster + presentation. **79 EditMode tests** assert re-run
determinism + the current element triangle (`BalanceParityTests`) — the latter must be updated for the
new table; the former stays green because deterministic value changes keep re-run equality.

## Build order (dependency + risk)
1. **Engine, test-verifiable this turn (low determinism risk, additive):** Element 2.0 + Void, Economy
   (gacha/pity/fusion/sell/supercoin/essence/aura), Progression (feed/stat-alloc/mastery/bond). Pure C#
   + `SaveData` additive fields + unit tests.
2. **Combat rework (careful, sim + tests):** Active+Support + support abilities — keep determinism,
   update tests.
3. **Content + UI (later tranches):** Chronovore + new-element monsters, biomes, quest rewards, endgame,
   summon/fusion/collection screens, showcase captures.
