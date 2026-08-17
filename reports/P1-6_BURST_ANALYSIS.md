# P1-6 — Skill Burst vs Fast-Tail Battles

Question: **is skill burst the root cause of the fast-tail (low P10) battles?**
In-memory overrides only (shared skill-pool damage multipliers — NOT species
data; base stats/growth untouched). `balance.json` on disk untouched. No code
kept, no commits. Fixed: `K=25`, `apsPerSpdLow=0.016`. 5,000 random + 3,960
round-robin per config.

## Results

| Config | active | ult | P10 | P50 | P90 | hard% | firstKill | ult usage% | role spread |
|---|---|---|---|---|---|---|---|---|---|
| base | 2.8 | 3.8 | **17.4** | 29.2 | 71.0 | 6.0 | 7.8 s | 97.6 | 10–80 |
| step | 2.6 | 3.5 | 17.4 | 31.2 | 71.4 | 5.6 | 8.2 s | 98.1 | 10–80 |
| step | 2.4 | 3.2 | 17.9 | 31.2 | 75.0 | 5.6 | 8.7 s | 98.3 | 10–82 |
| step | 2.2 | 3.0 | **18.2** | 34.0 | 79.5 | 6.0 | 9.4 s | 98.8 | 10–82 |
| ACTIVE only | 2.2 | 3.8 | 17.4 | 32.6 | 81.2 | 6.6 | 9.3 s | 98.8 | 10–81 |
| ULT only | 2.8 | 3.0 | 17.4 | 31.2 | 68.2 | 4.9 | 7.9 s | 97.6 | 10–80 |

## Reading

- **P10 barely moves.** Cutting active 2.8→2.2 **and** ultimate 3.8→3.0 shifts the
  fast tail only **17.4 → 18.2 s (+0.8 s)** — nowhere near the 25 s floor. In the
  active-only and ult-only isolations, **P10 stays flat at 17.4 s**.
- **Ultimates are irrelevant to fast battles.** Ultimates charge at 15 s, but the
  mean first kill is ~7.8 s — the fastest battles are decided **before any
  ultimate fires**. So reducing ult multiplier does nothing to P10 (17.4
  unchanged) and even the ULT-only cut leaves the fast tail untouched.
- **Active cut only nudges first-kill.** Active 2.8→2.2 pushes first-kill 7.8 →
  9.3 s and P50 up, but P10 still holds ~17.4 — because the fast kills come from
  **basic attacks** (37.8% of all actions, and the dominant early damage), which
  are multiplier 1.0 and untouched by this sweep.
- **Role spread unmoved** (10–80/82) — burst is not a balance lever either.
- **Useful side effect:** the ULT-only 3.0 cut is a clean, harmless nudge that
  helps the *slow* tail (P90 71.0 → 68.2, hard 6.0 → **4.9%** < 5%) without
  touching the fast tail or role balance.

## Answer: No — skill burst is NOT the root cause of the fast tail

Reducing active + ultimate multipliers by ~20–30% moves P10 by **+0.8 s**. The
fast tail is driven by **basic-attack throughput + focus-fire on low HP pools**,
not by skill burst. Ultimates don't even participate (they charge after the
fastest battles are already over).

### Can the fast-tail be fixed without touching species data?

**Effectively no.** The only levers that raise the P10 floor meaningfully are:

- **HP pools** — but *base* HP is species data (`baseStats.hp`), and the
  `balance.json`-legal proxy (`defaultGainRates[HP]`) can't reach the needed
  magnitude without breaking level scaling (shown in P1-5).
- **Basic-attack throughput** — lowering global `apsPerSpdLow` further *widens*
  the distribution (P1-5), pushing P90/hard out of range before P10 arrives.
- **Skill burst** — ruled out here (+0.8 s).

So lifting P10 to ≥25 requires **either touching species base HP (species data)
or accepting that fast comps end fast.** Burst tuning is the wrong tool for it.

## Recommendation (recommend only — nothing implemented)

1. **Stop chasing the fast tail with burst.** Confirmed it can't do the job.
2. **A modest burst trim is still worth keeping — for a different reason.**
   `ult ≈ 3.0` cleanly buys the P90/hard headroom (P90 68.2, hard 4.9%) and
   `active ≈ 2.4–2.6` lifts P50 into range, both without hurting P10 or roles.
   Treat burst as a **P50/P90/hard** lever, not a P10 lever. (Caveat: these live
   in skill assets, not `balance.json` — non-species, but outside a strict
   "balance.json only" scope.)
3. **The P10 floor needs an HP decision:** either allow a **base-HP lift (species
   data)** — the honest fix — or **relax P10 ≥ 25 to ~18–20**, which the current
   model already meets and which better fits the assassin/glass fantasy (fast
   comps *should* be able to end fast).

## Constraints honored

Only shared skill multipliers were overridden, in memory; **no species data (base
stats/growth) changed**, `balance.json` untouched, no code kept, nothing
committed; 7/7 gate suite unaffected.
