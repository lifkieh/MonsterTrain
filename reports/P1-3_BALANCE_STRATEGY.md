# P1-3 — Balance Strategy (Minimum Pass)

Design only. **No code, no `balance.json` changed.** Derived from
`reports/P1-2_BASELINE_BALANCE_REPORT.md`.

## Goal

| | Current (baseline) | Target |
|---|---|---|
| Duration P50 | ~17.9 s (P10 13.6 / P90 39.2) | 30–90 s (P10 ≥ 30, P90 ≤ 90) |
| Species win spread | 2.1% – 93.8% | ~40% – 60% |

## Diagnosis (one root, two symptoms)

The baseline showed a single root cause — **offence out-scales HP and DEF** —
producing both symptoms: battles end ~18 s (too fast) and fast/high-ATK species
dominate while tanks collapse (Tank role 9.1%, Turtle 2.1%; Bat 93.8%, Wolf
92.9%). Two structural enablers:

- **DEF is nearly inert.** `mitigation = 1 − DEF/(DEF+K)` with `K=50` lets ~66–86%
  of damage through even for tanks. No survivability payoff.
- **SPD is unchecked.** The diminishing-returns kink is at `spdKink=25`, but MVP
  SPD tops ~22, so action economy scales fully linearly → SPD-stack wins.

Because the two symptoms share this root, a **small set of levers fixes both at
once**. The minimum pass is **3 levers, pulled one at a time, re-sweeping between
each** (the balancing rule — levers interact and can overshoot).

> Correction to the earlier duration analysis: it mentioned "raise K." The
> correct direction is **LOWER K** — `mitigation = 1 − DEF/(DEF+K)`, so a smaller
> K lets *less* damage through and makes DEF stronger. This report supersedes that
> detail.

---

## Priority 1 — Lower K (DEF mitigation constant)

**Lever:** `balance.json` → `k`, e.g. **50 → ~25–30** (single number).

**Why first:** highest dual impact and the cheapest change. It simultaneously
(a) slows every battle by cutting effective damage and (b) rescues tanks — the
worst part of the imbalance — with one number, without disturbing SPD, skills, or
growth.

- **Expected duration impact:** strong ↑. At `K=25`, mid-DEF (16) damage-through
  drops 0.758 → 0.610 (~−20%); tank-DEF (26) drops 0.658 → 0.435 (~−34%). TTK
  rises roughly 20–40%; P50 ~18 s → **~24–28 s** (partway to target).
- **Expected role impact:** compresses the spread from the bottom. High-DEF tanks
  gain the most effective HP → Tank role **9% → ~25–35%**; offence roles ease down
  as targets survive longer. Directly narrows the 2.1%–93.8% gap.
- **Risk:** low–moderate. Over-lowering K can make tanks too durable and push
  toward stalls; mitigated by anti-stall (75 s) + 120 s hard-resolve, and there is
  huge headroom (tanks at 2–18%). Re-sweep and watch `hardResolve%` (baseline
  2.1%).

## Priority 2 — Lower the SPD brake kink (and, if needed, `apsPerSpdLow`)

**Lever:** `balance.json` → `spdKink`, e.g. **25 → ~12–15** (optionally nudge
`apsPerSpdLow` 0.02 → ~0.017). Engages the diminishing-returns brake *inside* the
MVP SPD range it was designed for.

**Why second:** targets the single most dominant build — SPD-stack (Bat 93.8%,
Bee, rush). Priority 1 doesn't touch action economy; this does. Do it after K so
its effect is read against already-slower battles.

- **Expected duration impact:** mild ↑ for fast comps. Bat (SPD 20) aps
  0.40 → 0.32 (−20% actions); Bee (22) 0.44 → 0.34. Rush battles lengthen; slow
  comps barely change.
- **Expected role impact:** caps Assassin/fast-Support dominance. Bat **93.8% →
  ~70–80%**, Bee down; low-SPD tanks unaffected directly (further relative buff).
  Compresses the top of the spread.
- **Risk:** moderate. Above-kink units all lose actions, so it is a partial global
  slowdown too — stack carefully with P1 to avoid overshooting duration. Spider
  (already 39.7%) and other mid-SPD units take collateral; re-check they don't sink
  below ~40%.

## Priority 3 — Global duration trim (raise base HP and/or lower `apsPerSpdLow`)

**Lever:** raise HP pools (base HP and/or `defaultGainRates[HP]` 2.5) **or** lower
`apsPerSpdLow` (0.02 → ~0.015). A broad multiplier to slide the median the rest of
the way into 30–90 s.

**Why last:** it is a whole-distribution knob. Pull it only after P1 + P2 have
fixed the *shape* (DEF and SPD), so you tune duration once against a stable meta
instead of chasing a moving target.

- **Expected duration impact:** primary lever for the final placement. `apsPerSpdLow`
  0.02 → 0.015 ≈ −25% hits/sec ≈ +33% TTK; expected to seat P50 in the **~35–50 s**
  band and lift P10 toward/above 30 s.
- **Expected role impact:** roughly neutral on the *spread* (scales everyone),
  which is why it comes after the shape is fixed; slightly favours HP-heavy
  (tank) builds if the HP route is chosen.
- **Risk:** overshoot into stalls if stacked on top of an aggressive P1/P2 — the
  reason it is sequenced last and tuned in small steps with a re-sweep each time,
  watching P90 ≤ 90 s and `hardResolve%`.

---

## Levers NOT to touch yet (and why)

- **Crit (`critPerLuck` / `critCap` / `critMultiplier`)** — variance, not a
  systematic driver; baseline dominance (Goblin) comes from ATK, not crits.
  Leave; touching it only adds noise to the fantasy.
- **Skill multipliers (2.8× active / 3.8× ult)** — these live in the generated
  skill *assets*, not `balance.json`, and ultimates already **under-fire** (17% of
  L5 battles never reach one). Cutting them now would suppress ultimates further
  and isn't the main duration driver (basics are 37.8% of actions). Revisit only
  after the duration fix lets ultimates fire.
- **`speciesGainRates` (per-species overrides, currently empty)** — powerful but a
  12-wide tuning surface. Do **not** open per-species tuning against a broken
  global curve; fix K / SPD / HP first, then fine-tune outliers if any remain.
- **`tierMultipliers`, `statPointsPerLevel`, training yields** — growth/
  progression, not the combat-shape problem; changing them ripples unpredictably
  into every stat. Out of scope for this pass.
- **Anti-stall / `hardResolveTime`** — battles are too *fast*, not too slow (2%
  hard-resolve). No reason to touch; just monitor as a guardrail while slowing.
- **`minDamage`, `apsCap`** — edge parameters, irrelevant at current values.

## Method (for the later implementation pass — not done here)

Pull **one lever, re-run the P1-2 sweep, read duration + spread, then decide the
next** — never stack blind. Expected sequence to target: **P1 (lower K)** → seat
tanks and slow ~⅓ of the way → **P2 (lower spdKink)** → kill SPD dominance →
**P3 (HP / aps)** → final duration placement. Gate on the same numbers the sweep
already prints (P10/P50/P90, `hardResolve%`, per-species win). Support (Mushroom
27%, Bee 42%) may need a small follow-up (healing barely fires) — a *fine-tuning*
step after the minimum pass, not part of it.

## Constraints honored

Analysis only. No code, no `balance.json`, no gameplay design changed. Uses only
the P1-2 baseline. Implementation deferred.
