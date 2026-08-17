# PHASE 1 — Balance Lock Decision

**Role:** Creative Director + Lead Architect. Decision document. No code, no
`balance.json` change. Synthesizes P1-1…P1-6 against `docs/KILL_CRITERIA.md`.

## Verdict

**STOP balance research now.** Enough evidence exists to lock the *direction*.
Per KILL_CRITERIA **S3** (balance sweep still failing after 3 tuning passes → stop
head-on tuning, apply the ladder) and **§5 amber-freeze** (balance lives in
`balance.json`, must not hold the phase hostage), the correct move is to
**amber-freeze Phase-1 balance and carry final tuning into a later, timeboxed
pass — balance does NOT block first-playable.**

Four experiment rounds (P1-4 K-sweep, P1-5 global-lever sweep, P1-6 burst sweep)
have exhausted the high-information levers. Further global sweeps are **rejected**
for low information gain (see below).

## What is KNOWN (fully answered)

1. **Initiative side-bias is fixed and holds** — team-A win 49.4–49.9% across
   every config and level (P1-1B, confirmed in P1-2/4/5/6). Determinism + RNG
   contract intact. Closed.
2. **Duration is governed by K + aps, not by burst.**
   - Lower K lengthens battles (P1-4): P50 17.9 → 21.9 at K=25.
   - Lower `apsPerSpdLow` lengthens further (P1-5): K=25+apsLow0.016 → P50 29.2.
   - Skill burst is **not** the fast-tail driver (P1-6): cutting active 2.8→2.2 +
     ult 3.8→3.0 moved P10 only +0.8 s. Ultimates charge at 15 s but first-kill is
     ~7.8 s, so they don't touch fast battles.
3. **`apsPerSpdHigh` is inert** at MVP levels (SPD ≤22 < kink 25) — not a lever.
4. **The duration distribution is intrinsically wide.** Global levers *widen* it
   as they slow it; P10 (fast glass blowouts) cannot be lifted to ≥25 s without
   raising **base HP** (species data) — the legal `balance.json` proxy
   (`defaultGainRates[HP]`) can't reach the magnitude without breaking level
   scaling (P1-5).
5. **Role imbalance is NOT a duration-lever problem.** Tank stayed ~9–10% under
   every K/aps/HP/burst config (P1-2/4/5/6). Tanks lose on **offence** (Turtle
   ATK 12 / SPD 5), which none of these levers touch.
6. **A duration setting that meets P50/P90/hard exists** (just not P10≥25):
   `K=25, apsPerSpdLow≈0.016, ultimate≈3.0` → P50 ~31 / P90 ~68 / hard ~4.9%.

## What is UNKNOWN (still needs evidence — but LATER, not now)

- **The role/species rebalance numbers.** Lifting Tanks/Support to ~40–60%
  requires a **per-species pass** (offence/utility via `speciesGainRates` or kit),
  measured with a **mixed-comp** metric (homogeneous round-robin structurally
  condemns role pieces). Not attempted yet; explicitly deferred.
- **On-device battle duration feel** — numbers are sim-time; whether ~15–35 s
  battles *feel* right needs the playable + a human. Unmeasurable headless.
- These do **not** block first-playable and are not Phase-1 gate items under §5.

## What will be ACCEPTED (locked targets)

Revised, evidence-based, and shippable. The original P10 ≥ 30 s floor is
**rejected** — it is unreachable with legal levers and wrong in spirit (glass
comps *should* end fast; that is the assassin fantasy).

| Metric | Locked acceptance |
|---|---|
| Duration P50 | **30–60 s** |
| Duration P90 | **≤ 90 s** |
| Duration P10 | **≥ 15 s** (was ≥30/≥25 — relaxed; the binding constraint is unreachable without species data) |
| sub-15 s battles | **< 10%** |
| Hard-resolve % | **< 5%** |
| Mirror fairness | **50% ± 3%** (already met, 49.9%) |
| Role/species spread | **direction locked, numbers deferred** to the per-species pass |

**Provisional `balance.json` direction** (to APPLY during the later balance pass,
per the S3 ladder — NOT now, NOT a first-playable blocker):
`k = 25`, `apsPerSpdLow ≈ 0.016`, keep `apsPerSpdHigh`/`spdKink` as-is, and (S3
ladder rung 3) trim shared skill multipliers toward **active ≈ 2.4–2.6 /
ultimate ≈ 3.0**. Expected: P50 ~31 / P90 ~68 / hard ~4.9% / P10 ~17.

## What will NOT be investigated further (rejected — low information gain)

- **More global K / aps / HP sweeps.** Three rounds converged; the ceiling is
  understood (distribution width). No new information.
- **`apsPerSpdHigh` tuning.** Proven inert at MVP SPD.
- **Burst tuning for the fast tail.** Proven ineffective (+0.8 s P10).
- **Chasing P10 ≥ 25 with `balance.json`.** Structurally impossible without
  species base-HP; the criterion is relaxed instead.
- **Any balance work before first-playable.** Amber-freeze (§5): balance rides in
  `balance.json` and is tuned after the game is playable, not before.

## Recommended STOP CONDITION

**Balance research is closed for Phase 1.** Re-open balance only in a single
**post-first-playable, timeboxed per-species pass** with these rules:
1. Metric = **mixed-comp** per-species win contribution (not homogeneous RR).
2. Apply the S3 ladder in order; **cap at 3 passes / 5 evenings**, then ship
   whatever passes the accepted window above.
3. First-playable proceeds **immediately on current defaults** — a working battle
   does not need final balance.

Rationale: **Shipping > Polish** is locked. The core fantasy gate
(preparation beats no-preparation ≥75%) already passes (P1-1B suite), so the game
is mechanically honest today. Everything remaining is tuning, and tuning does not
block a playable build.
