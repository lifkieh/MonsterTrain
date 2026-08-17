# P1-1A — Mirror Side-Bias Fix: Design Decision

Decision document. **No code changed.** Selects the permanent fix for the
initiative side-bias diagnosed in `P1-1_SIDE_BIAS_ANALYSIS.md` (team A wins 73.5%
of mirrors because `ActionTimeline` breaks ties `team A < team B` and mirrored
same-SPD units are phase-locked).

Shared facts that apply to every candidate:

- **Any** change to tie ordering changes which unit resolves first, which
  reassigns crit draws, which changes the event log → the `Determinism_
  SameSeedSameHash` golden hash must be re-baselined **once**. This is
  unavoidable for every real fix and is a one-line test update, not a gameplay
  change.
- Same-seed reproducibility (the actual determinism guarantee) is preserved by
  every candidate, as long as the new rule is itself seed-derived.
- The **duration fix** (`P1-1_BATTLE_DURATION_ANALYSIS.md`) independently shrinks
  first-strike advantage: at 30–90 s TTK, initiative barely swings outcomes. So
  the tie-break only needs to be **unbiased**, not perfectly neutral in every
  single match.

---

## Candidate 1 — Fair random tie-break (live seeded RNG draw)

On an exact tie, draw from the battle's `System.Random` to pick the actor.

- **Complexity:** Moderate. `ActionTimeline.NextActor` currently takes no RNG;
  must thread the rng in and gather the tied set to pick fairly (3-way ties
  exist).
- **Determinism:** Deterministic per seed, **but inserts RNG draws into the
  stream at scheduling time**, interleaved with crit draws. The number of draws
  depends on how many ties occur, so the whole crit sequence shifts. Dirties the
  documented RNG contract (growth → crit → hard-flip) the most of any option.
- **Replayability:** Fine (log replays).
- **Debugging:** Worse. "Why did A act first here?" now depends on hidden RNG
  position; not reproducible by inspection alone.
- **Future multiplayer:** Fair (no side bias). But maximal RNG interleaving is
  the most sensitive to any cross-machine divergence if PvP ever re-simulates
  rather than replays.
- **Readability:** Neutral.

## Candidate 2 — Seeded parity tie-break (one bit per battle)

Ties won by team A if `seed` is even, else team B.

- **Complexity:** Very low (one line).
- **Determinism:** Fully deterministic; **does not touch the RNG stream** (crit
  sequence unchanged). Only event order changes on odd seeds.
- **Replayability:** Fine.
- **Debugging:** Easy and predictable ("odd seed → B wins ties").
- **Future multiplayer:** **Poor.** Within a single match one side *always* wins
  every tie — fair only in aggregate across many battles. A single PvP mirror is
  structurally lopsided for whoever holds the favored side. Unacceptable for
  competitive play.
- **Readability:** Neutral (same "one whole row acts, then the other" feel as
  today, just sometimes B first).

## Candidate 3 — Initial timeline stagger (per-unit offset)

Give each unit a small initial `nextActionTime` offset so equal-SPD units stop
phase-locking.

- **Complexity:** Moderate. A *team-neutral* offset is required — a slot-only
  offset still ties both teams' slot-0; a `(team,slot)` offset re-introduces team
  bias. So the offset must be seeded-random per unit, i.e. Candidate 1's cost
  moved to build time.
- **Determinism:** Deterministic if seeded; adds draws to the build phase → RNG
  contract + hash change. Offsets also perturb *real* cadence, not just ties, so
  it's a broader behavioral change than a pure tie-break.
- **Replayability:** Fine.
- **Debugging:** Mixed — ties vanish (fewer "why first" questions) but the
  timeline gains fractional-second offsets that muddy exact scheduling.
- **Future multiplayer:** Fair in aggregate, but the unit with the smaller offset
  keeps a phase lead for the whole battle → a small persistent per-match edge,
  similar in spirit to Candidate 2 but smaller.
- **Readability:** Slight **plus** — units act on slightly different ticks, less
  robotic than everyone firing on the same instant.

## Candidate 4 — Simultaneous resolution for exact-time ties

Units sharing an exact action time all act against the **pre-tick state**; apply
all damage, then remove the dead. No one steals a kill by acting first.

- **Complexity:** **High.** Requires splitting SkillResolver into compute-then-
  apply, snapshotting targets from pre-tick HP, batching deaths and wipe-checks,
  and defining a mutual-wipe rule. It restructures the most-tested, make-or-break
  system, and it **imposes a requirement on the not-yet-built replay view**
  (render multiple same-`t` events).
- **Determinism:** Deterministic. Still needs a stable sub-order for **crit-RNG
  assignment** among the batch (e.g. team→slot), but that order no longer confers
  a kill advantage. Hash re-baseline as usual.
- **Replayability:** Fine, but the view must group same-timestamp actions — a real
  design constraint on `BattleReplayView`.
- **Debugging:** Mixed — removes first-strike confusion, but deferred deaths and
  batched actions are harder to step through.
- **Future multiplayer:** **Best.** A true mirror trades evenly every match;
  outcome falls to preparation/crits, never initiative. Most aligned with the
  core fantasy ("I won because I prepared correctly").
- **Readability:** Risk — simultaneous same-tick hits can look busy on a chibi
  mobile screen (multiple flashes at once), though the view could fan them out
  visually while resolving them simultaneously.

## Candidate 5 — Deterministic seeded per-unit initiative key (proposed "other")

Replace the `team` term in the final tie-break with a per-unit key computed once
at team build: `initiativeKey(u) = Hash(seed, u.team, u.slot, u.speciesId)`.
Total order becomes: earliest time → higher base SPD → higher `initiativeKey` →
(slot as a collision-only last resort). The `team` bias term is removed
entirely.

- **Complexity:** **Low.** Compute one key per unit at `BuildTeam`; compare keys
  in `ActionTimeline`. A few lines; no structural change to the loop or resolver.
- **Determinism:** Fully deterministic and **RNG-clean** — the key is a pure hash
  of seed+identity, consumes **zero** `System.Random` draws, so the crit stream
  and the RNG contract are untouched. Only event order changes → single hash
  re-baseline.
- **Replayability:** Fine; no view changes required.
- **Debugging:** Easy — the key is a fixed, printable value; "A's slot-0 key >
  B's slot-0 key, so A went first" is fully inspectable and reproducible.
- **Future multiplayer:** Good. No systematic side bias; each mirrored pair's
  initiative is an independent per-seed coin-flip, not a whole-battle side lock
  (strictly better than Candidate 2). Not *perfectly* neutral per match like
  Candidate 4, but that residual is negligible once duration is fixed.
- **Readability:** Neutral (same sequential feel as today).

---

## Comparison

| Axis | 1 Random | 2 Parity | 3 Stagger | 4 Simultaneous | 5 Keyed |
|---|---|---|---|---|---|
| Complexity | Moderate | Very low | Moderate | **High** | **Low** |
| Determinism / RNG hygiene | Dirties stream | Clean | Dirties stream | Clean(ish) | **Clean** |
| Replayability | OK | OK | OK | View constraint | **OK** |
| Debugging | Poor | Easy | Mixed | Mixed | **Easy** |
| Future PvP fairness | Good | **Poor** (per-match) | Fair | **Best** | Good |
| Battle readability | Neutral | Neutral | Slight + | Busy risk | Neutral |
| Passes 50±3% sweep | Yes | Aggregate | Yes | Yes | Yes |

---

## Recommendation — Candidate 5 (deterministic seeded per-unit initiative key)

**Adopt the keyed tie-break.** It is the best fit for this project's constraints:

1. **Removes the systematic bias** (the `team` term is deleted), so the mirror
   sweep passes and no side is structurally favored — the actual defect is fixed
   at its source.
2. **Lowest risk to the make-or-break simulator.** A few lines in
   `ActionTimeline` + one key computed at build. No restructure of the loop,
   resolver, or targeting — critical for a solo dev whose riskiest system this is.
3. **RNG-contract clean.** It consumes no random draws, so the crit sequence and
   the documented growth→crit→flip contract stay intact; only the event order (and
   thus the golden hash) shifts, which every option incurs anyway.
4. **Deterministic, replayable, debuggable** — a fixed, printable per-unit key;
   no hidden RNG state, no view changes, no fractional-time noise.
5. **Good enough for PvP**, and the residual per-match imbalance is immaterial
   once the duration fix lands (first-strike stops mattering at 30–90 s TTK).
6. Honors the project ethos: *boring, readable, ship > perfection*.

**Why not the others:**

- **4 (Simultaneous)** is the theoretical gold standard for PvP fairness and the
  purest expression of "preparation, not initiative, decides" — but its cost
  (restructuring the most-tested system, a mutual-wipe rule, and a hard
  requirement on the unbuilt replay view) is not justified for a residual effect
  that the duration fix already dissolves. **Reconsider it only if Wave-2 PvP
  makes single-match mirror neutrality a hard requirement** — and if so, design
  the replay view for same-tick events from the start.
- **1 / 3** achieve the same fairness as 5 but dirty the RNG stream (1 at
  scheduling time, 3 at build) for no added benefit; 5 gets the fairness without
  touching randomness.
- **2** is the cheapest but fails the fairness intent within a single match — a
  regression risk the moment PvP arrives; 5 fixes exactly 2's weakness for
  near-identical cost.

**Implementation note (for the later approved change, NOT done here):** add an
`initiativeKey` to `CombatUnit`, set it in `BattleSimulator.BuildTeam` from a
hash of `(seed, team, slot, speciesId)`, and change `ActionTimeline.Consider`'s
final comparisons from `team`/`slot` to `initiativeKey` (keep `slot` only as a
collision-proof last tiebreak to preserve total ordering). Then re-baseline the
`Determinism_SameSeedSameHash` golden value and re-run the sweep to confirm the
mirror rate lands in 42–58%.

---

*Analysis and recommendation only. No code, `balance.json`, or design modified.
Implementation deferred pending approval and sequencing with the duration fix.*
