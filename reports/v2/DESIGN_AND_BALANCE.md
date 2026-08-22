# TYM 2.0 — PHASE B: DESIGN + BALANCE (resolved before coding)

## 1. Element system 2.0
Roadmap gives per-element STRONG lists but its per-element WEAK lists are internally inconsistent
(e.g. Wind is "strong vs Nature" but Nature's weak list omits Wind; Lightning claims "weak to Light"
but Light is "strong vs Shadow only"). **Resolution: derive the whole table from the STRONG relations
only**, so it is always symmetric-consistent:
- `mult(A→B) = 1.5` if A is listed strong vs B (advantage).
- `mult(A→B) = 0.7` if B is strong vs A (disadvantage).
- else `1.0`.
- **Elemental advantage forces a critical hit** (guaranteed), but the crit RNG roll is still *consumed*
  in `SkillResolver` (result overridden) so the deterministic draw-order contract is untouched.

Strong relations (from roadmap): Fire→{Nature,Ice}; Water→{Fire,Metal}; Nature→{Water,Earth};
Earth→{Fire,Lightning}; Wind→{Earth,Nature}; Lightning→{Water,Wind}; Ice→{Nature,Wind};
Metal→{Ice,Earth}; Light→{Shadow}; Shadow→{Light}; **Void→{} (neutral, see §2).**

Resulting strength/weakness count (balance audit): Fire 2/2, Water 2/2, Wind 2/2, Ice 2/2 (balanced);
Nature 2/3 & Earth 2/3 (slightly fragile — they are the common starter elements, fair to be
counterable); Lightning 2/1 & Metal 2/1 (slightly strong — rarer elements); Light/Shadow 1/1 (mutual
counters, neutral to all else). Every element has ≥1 strength AND ≥1 weakness → satisfies the rule.
No element strictly dominates (intransitive web). Documented asymmetry is intentional, not a defect.

## 2. Void (rare/special)
Void = pure neutral: `mult` is always 1.0 both ways, never gives/receives an elemental crit. Cap of 3
Void monsters ever (roadmap). First = **Chronovore** (Mythical, Control): Time Rend (basic), Temporal
Collapse (ultimate), **Paradox Core** (support: −15% ultimate-energy cost / cooldown for the active).
Strong-but-not-unbeatable: high control, average raw stats, no elemental advantage (can't crit-counter).

## 3. Economy + gacha (no P2W)
- **Currencies:** `coins` (exists), `supercoins` (premium — earned via achievements/events, NOT sold
  for power; accelerate only), `essence` (from selling monsters → progression), `auraShards` (aura pulls).
- **Monster gacha rates:** Common 55 / Rare 25 / Epic 12 / Legendary 6 / Mythical 2 (%). Deterministic
  seeded RNG stored in save (own stream, never touches the battle sim seed).
- **Pity:** guaranteed Epic≤20, Legendary≤80, Mythical≤200 (counters in save, reset on hit).
- **Fusion:** two same-species same-star → +1 star (max ★5). Higher stats + level cap + support scaling.
  **No rarity upgrade.**
- **Selling:** duplicate → coins + essence by rarity. Never sell the last copy (guard).
- **Aura gacha:** Normal/Rare/SuperRare/Unique/Legendary/Mythical/Impossible. Impossible = 1e-11 %
  (prestige, cosmetic bonus only, no power).

## 4. Progression 2.0
- **Feed:** Basic/Premium/Legendary food → +xp (no mastery). Food items in save inventory.
- **Stat allocation:** +2 points per level (wire the existing `LevelMath`), spend on HP/ATK/DEF/SPD.
- **Skill mastery:** per-monster mastery L1–L5 → damage ×{1.0,1.1,1.2,1.35,1.5} (roadmap). Raised by
  Training (coins). Feeds `damageScale` in the sim deterministically.
- **Bond:** bond-xp from battles/quests → unlocks (titles/anim/lore); minor, cosmetic.

## 5. Support-ability framework (no duplicates)
5 categories (Guardian/Healer/Buffer/Debuffer/Summoner). Each of the 21 monsters gets ONE unique
support effect (a `SupportEffect{category, magnitude, id}` keyed by speciesId). No two share an id.
Applied once at battle start / on a trigger to the ACTIVE monster (Active+Support combat, tranche 2).

## Determinism guarantee
All economy/gacha RNG uses a SEPARATE seeded stream persisted in save — it never touches
`BattleSimulator`'s seed or draw order. Element/mastery changes are pure deterministic functions; the
79 re-run-equality tests stay green. `BalanceParityTests` (asserts the old triangle) is rewritten for
the new table.
