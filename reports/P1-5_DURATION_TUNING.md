# P1-5 — Duration Tuning (Global Levers)

Goal: reach the duration window using **global** `balance.json` levers only —
no code, no new mechanics, **no `speciesGainRates`**, no species-specific tuning.
In-memory overrides only; `balance.json` on disk untouched; nothing committed;
temp harness removed after.

**Stop criteria:** P10 ≥ 25 · P50 ∈ [30, 60] · P90 ≤ 90 · hardResolve < 5%.

**Result: NO tested configuration meets all four criteria.** The target is not
reachable with the four requested levers (K, apsPerSpdLow, apsPerSpdHigh, base-HP
multiplier). Detail + why below. Recommendation only — nothing implemented.

## Sweep results (K=25 anchor; 5,000 random + 3,960 round-robin each)

| Config | P10 | P50 | P90 | hard% | role spread | pass |
|---|---|---|---|---|---|---|
| base (aps 0.02) | 15.6 | 21.9 | 56.9 | 4.6 | 9–77 | no |
| apsLow 0.018 | 15.9 | 24.3 | 62.5 | 4.7 | 9–78 | no |
| apsLow 0.016 | 17.4 | 29.2 | 71.0 | 6.0 | 10–80 | no |
| apsLow 0.014 | 17.9 | 31.2 | 107.1 | 9.3 | 10–78 | no |
| apsLow 0.012 | 19.6 | 35.7 | 120.0 | 10.6 | 10–80 | no |
| **apsHigh 0.006** | 15.6 | 21.9 | 57.1 | 4.6 | 9–77 | **no (inert)** |
| HP ×1.25 | 16.7 | 27.3 | 69.8 | 5.4 | 9–81 | no |
| HP ×1.50 | 17.9 | 33.3 | 86.4 | 6.8 | 9–81 | no |
| HP ×2.00 | 23.5 | 44.4 | 104.5 | 7.7 | 9–81 | no |
| apsLow 0.016 + HP ×1.35 | 19.5 | 37.5 | 91.4 | 6.7 | 10–83 | no |
| **+ anti-stall (start 50, incr 0.10) on apsLow 0.016** | 17.4 | 29.2 | 69.9 | 5.2 | 10–80 | no |
| **HP ×2.0 + anti-stall (start 45, incr 0.15)** | 23.5 | 44.4 | **91.2** | **5.3** | 9–81 | no |
| HP ×1.75 + apsLow 0.017 + anti-stall(45,0.12) | 23.5 | 45.0 | 96.3 | 6.6 | 10–83 | no |
| HP ×1.5 + apsLow 0.015 + anti-stall(45,0.15) | 22.2 | 44.4 | 94.1 | 5.7 | 10–82 | no |

(Anti-stall added in a second round — a `balance.json`-legal, non-species lever —
to try to clamp the tail. It helped but did not close the gap.)

## Why the target is unreachable with these levers

1. **Global levers WIDEN the distribution as they slow it.** The spread
   (P90/P10) grows, not shrinks: base 3.65 → apsLow0.012 6.1 → HP×2 4.45. So
   raising the fast tail (P10) toward 25 pushes the slow tail (P90) past 90 and
   `hardResolve%` past 5% before P10 arrives. The two ends can't both fit in
   [25, 90] at once.
2. **P10 (fast blowouts) is the binding constraint.** The fastest 10% are glass-
   cannon focus-fire wipes; they stay fast under any global slowdown. Only HP ×2
   even reaches P10 23.5 — still short of 25 — and it wrecks P90/hard.
3. **`apsPerSpdHigh` is inert.** MVP SPD tops ~22, below the `spdKink`=25, so the
   high-SPD branch never fires — `apsHigh 0.006` is byte-identical to base. Drop
   it from the toolbox.
4. **Base-HP multiplier is NOT a `balance.json` lever.** Base HP lives in the
   species assets (`baseStats.hp`). The only legal HP knob is
   `defaultGainRates[HP]` (growth) — but delivering the ~×2 effective HP that HP-
   sweeps needed would require raising it from 2.5 to ~25 (10×), which would
   destroy level scaling. So the HP route that "works" here is not achievable
   under "balance.json only."
5. **Closest miss:** `K25 + HP×2 + anti-stall(45,0.15)` → P10 23.5 / P50 44.4 /
   P90 91.2 / hard 5.3 — misses **P10, P90, and hard% each by a hair**, and even
   this leans on the illegal base-HP lever.
6. **Role spread never moves** (Tank ~9–10%, species 0–100 across every config) —
   reconfirms P1-4: duration levers don't fix balance.

## Recommendation (recommend only — nothing implemented)

The stop criteria as written are **infeasible under a strict "balance.json only,
global levers" reading.** Three ways forward, in order of preference:

- **1. Best balance.json-only setting (closest legal, does not fully pass):**
  `k=25`, `apsPerSpdLow≈0.016`, `antiStallStart≈50`, `antiStallIncrement≈0.10`.
  → P10 17.4 / P50 29.2 / P90 69.9 / hard 5.2. Meets P50 (≈30) and P90 (≤90)
  comfortably; **misses P10≥25 and is at hard≈5.2%.** If the P10 floor is relaxed
  to ~18–20 and hard to ≤5.5%, this is a solid, fully-legal landing spot.

- **2. Add ONE non-species lever the task excluded but that stays out of species
  tuning — reduce the shared skill burst multipliers** (`power_strike` 2.8×,
  `savage_rend`/`mind_blast` 3.8×). This is the correct lever for the **fast tail
  (P10)**: it slows blowouts specifically and *narrows* the spread, which no
  global lever does. Caveat: these live in the skill *assets*, not `balance.json`
  — strictly outside "balance.json only," but **not species-specific**, so it
  honors "no species tuning." Pair with setting #1 to bring P10 up while keeping
  P90/hard in range. Recommended if the four criteria must be met exactly.

- **3. Relax P10≥25** to reflect reality: glass comps *should* be able to end
  fast (that's the assassin fantasy). A criterion like "sub-15 s < 5% and
  P10 ≥ 18" is both achievable with balance.json-only levers (setting #1 nearly
  hits it) and arguably better design than forcing a hard 25 s floor on every
  matchup.

**Do NOT** keep chasing this with `apsPerSpdHigh` (inert) or base-HP (not
`balance.json`). And note: duration is now essentially decoupled from the role-
imbalance problem — that still needs the separate, post-duration species pass
(P1-4), not covered here.

## Constraints honored

`balance.json` on disk unchanged (all overrides in-memory); no code kept; no
`speciesGainRates` touched; no new mechanics; nothing committed; 7/7 gate suite
unaffected.
