# TYM 2.0 — TRANCHE 1–2: BUILD + BALANCE + ECONOMY + TEST REPORT

Foundation engine for TYM 2.0, built + **verified by 95 passing EditMode tests** (79 original stayed
green → determinism + save compatibility intact). Pure-C# + additive save; no UI yet (UI + combat
rework are later tranches). Follows the mandatory workflow A→G.

## Systems implemented + VERIFIED this tranche
| Roadmap system | Files | Verified by |
|---|---|---|
| Element system 2.0 (10 + Void) | `Core/ElementTable.cs`, `StatMath.ElementMultiplier` | `ElementTableTests` (4), `BalanceParity.Element2_0` |
| Void rules (neutral, no crit) | `ElementTable` | `Void_IsPureNeutral`, `Element2_0…Void` |
| Guaranteed crit on advantage | `SkillResolver` (roll consumed, overridden) | `ElementForcesCrit` asserts + 79 determinism tests |
| Monster gacha + rates + pity | `Meta/MonsterEconomy.cs` | `MonsterEconomyTests` (5) |
| Monster fusion | `MonsterFusion` | `Fusion_RaisesStar…` |
| Monster selling | `MonsterSelling` | `Selling_NeverSellsLastCopy` |
| Feed system | `Progression.Feed` | `Feed_ConsumesFood_AddsXp` |
| Stat allocation | `Progression.StatPointsAvailable/AllocateStat` | `StatAllocation_TwoPointsPerLevel` |
| Skill mastery | `StatMath.MasteryMultiplier`, `Progression.TrainMastery` | `Mastery_TrainRaises_DamageScales` |
| Bond | `Progression.AddBond/BondLevel` | `Bond_Accumulates` |
| Unique support ability / monster | `Core/SupportAbility.cs` (22 defs) | `SupportAbilityTests` (3) |
| Supercoin / essence / aura / food (save) | `Meta/SaveData.cs` additive fields | old saves default via initializers |

## Balance report
- **Element web** (verified `NoElement_StrictlyDominates`, `EveryElement_HasStrengthAndWeakness`):
  every element has ≥1 strength AND ≥1 weakness; net (str−weak) never exceeds +1 (Lightning/Metal are
  +1 "rarer/stronger", Nature/Earth are −1 "common/fragile", Fire/Water/Wind/Ice 0, Light/Shadow mutual
  counters). No element strictly dominates. Advantage ×1.5 + guaranteed crit; disadvantage ×0.7.
- **Void:** pure neutral both ways, never crits/critted — rare & special per rule. Cap 3 Void monsters.
- **Support:** all 22 effect ids unique (`AllIdsUnique`), 5 categories represented, every magnitude
  ≤ single modest effect (no single-support OP; two supports = two modest boosts, no broken combo).
- **No mandatory monster:** support effects are sidegrades across categories, none required.
- **Mirror/parity tests still pass** (`MirrorDuel`, `BudgetSwap`, `StrongerWinsMore`) → the element
  buff didn't break stat balance for neutral matchups.

## Economy report
- **Currencies:** coins (existing), **supercoins** (premium — earned via achievements/events,
  accelerate only), **essence** (from selling), **auraShards** (planned aura pulls), **food** inventory.
- **Gacha rates:** C55/R25/E12/L6/M2. **Pity:** Epic≤20, Leg≤80, Myth≤200 (all guaranteed — verified).
- **RNG isolation:** gacha uses a save-persisted LCG stream (`gachaSeed`) **completely separate** from
  the battle-sim seed → economy never perturbs combat determinism.
- **Fusion:** ★N→★N+1 costs N duplicates, cap ★5, no rarity change. **Selling:** dupes only, never the
  last copy, coins+essence by rarity.
- **No P2W:** all obtain paths use earned currency; supercoin accelerates, never buys victory.

## Test report — Phase D
`EditMode: total 95, passed 95, failed 0` (was 79; +16 new). Includes re-run determinism suites
(Phase1Gate / TagMode / BattleFeel / ReplayPresentation / BattleCinematic) still green after the
element-damage change → the deterministic draw-order + hash contract is preserved.

## Audit (Phase F) — what remains, ranked
1. **Active + Support combat sim rework** (headline) — 1 active + 2 support; apply support effects.
   Large, touches `BattleSimulator` + battle tests. Design-first next tranche.
2. **UI** — summon / fusion / feed / stat-allocation / collection-2.0 / sell screens (engine exists,
   no front-end yet).
3. **Chronovore + new-element monsters** — content (species assets + sprites = art budget) + wire mastery
   into the sim (`MonsterInstance`/`MatchRunner`).
4. **Biomes (9), quest reward types, endgame (tower/trials/raids), aura gacha UI, battle pass** — content.

These are the correct next tranches; this tranche delivered the **verified engine foundation** the rest
builds on.
