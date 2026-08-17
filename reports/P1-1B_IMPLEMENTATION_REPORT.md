# P1-1B — Implementation Report: Deterministic Seeded Initiative Key

Approved fix (Candidate 5 from `P1-1A_SIDE_BIAS_FIX_RECOMMENDATION.md`)
implemented. Mirror side-bias removed. All Phase 1 gate tests pass. **No balance
tuning, no `balance.json` change, no gameplay redesign.**

## Result at a glance

- `MirrorComps_NoSideBias`: **PASS** — team-A win rate **73.5% → 50.8%** (target 42–58%).
- `Determinism_SameSeedSameHash_100Runs`: **PASS**.
- Full EditMode suite: **7 / 7 pass, 0 fail** (was 6/7).
- Files changed: **3** (`Core/` only). Test and `balance.json` untouched.

## Files changed

| File | Change |
|---|---|
| `Assets/Scripts/Core/BattleState.cs` | Added `public ulong initiativeKey;` to `CombatUnit` (the per-unit tie-break value). +3 lines. |
| `Assets/Scripts/Core/BattleSimulator.cs` | `BuildTeam` now takes `seed`; sets `unit.initiativeKey = InitiativeKey(seed, teamId, slot, speciesId)`; added `InitiativeKey` (FNV-1a 64) + `MixInt` helpers. Both call sites updated. +35/−3. |
| `Assets/Scripts/Core/ActionTimeline.cs` | Tie-break final ordering changed from `team → slot` to `initiativeKey (desc) → slot → team (collision-only)`. The `team A first` bias term is gone. +12/−4 (net). |

Diff summary: `3 files changed, 43 insertions(+), 7 deletions(-)`.

### New tie-break total ordering (`ActionTimeline.Consider`)

`earliest nextActionTime → higher base SPD → higher initiativeKey → lower slot →
lower team`. The last term (`team`) is now reachable only on a full 64-bit hash
collision (~never), so total ordering — and therefore determinism — is
preserved, while the systematic team-A preference is eliminated.

### The key

```
initiativeKey(u) = FNV1a64(seed, u.team, u.slot, u.speciesId)   // computed once at build
```

A pure hash of the seed and the unit's identity. In a mirror, team-A slot-0 and
team-B slot-0 differ only in the `team` field, so they get different keys and
whichever is larger — pseudo-random per seed — acts first. Across the population
this is unbiased; hence 50.8%.

## Rationale

Candidate 5 was chosen (see P1-1A) because it fixes the bias at its source (the
`team` term is deleted) with the **lowest risk to the make-or-break simulator**,
**without consuming any RNG**, and **without any replay-view or test-golden
changes**. It fits the project ethos (boring, readable, ship). The four
alternatives were rejected for: RNG-stream pollution (1, 3), per-match unfairness
(2), or high cost + a hard requirement on the unbuilt replay view (4).

## Before vs after — mirror results

Same test (`MirrorComps_NoSideBias`: 400 mirror battles, level 5, 3v3, baseSeed 555):

| Metric | Before (biased tie-break) | After (initiative key) |
|---|---|---|
| **team-A win rate** | **73.5%** (FAIL, out of 42–58%) | **50.8%** (PASS) |
| P10 / P50 / P90 | 12.5 / 15.6 / 19.5 s | 12.5 / 15.9 / 20.5 s |
| sub-15 s battles | 87 / 400 | 70 / 400 |
| hard-resolves | 0.0% | 0.0% |

Win rate moved to ~50% as intended. Duration is essentially unchanged
(P50 15.6 → 15.9 s) — expected, because this change touches **only** initiative
ordering, not the damage/HP model. The short-duration issue remains open and is
addressed separately (`P1-1_BATTLE_DURATION_ANALYSIS.md`) — out of scope here.

> The exact after-rate (50.8%) was captured via a temporary `TestContext.WriteLine`
> in the test, then **reverted**; the committed `Phase1GateTests.cs` is unchanged
> (verified: `git diff` reports the test file unmodified).

## Determinism verification

- **RNG contract preserved.** `InitiativeKey` is a pure hash — it consumes **zero**
  `System.Random` draws. The documented order (growth rolls → crit rolls →
  hard-resolve flip) is byte-for-byte the same sequence as before; only the
  *order in which tied units act* changed, which re-assigns which unit gets which
  crit draw (an intended behavioral effect of the fix).
- **`Determinism_SameSeedSameHash_100Runs` passes.** It runs the same seed 100×
  and asserts an identical event-log hash each time — confirming the sim is still
  fully reproducible for a given seed after the change.
- **No golden-hash re-baseline was required.** The determinism test compares
  against its own first run (`first = Run(...).logHash`), not a hard-coded
  constant; a grep confirmed no committed golden hash anywhere. So the change
  needed no test edits at all.
- **Replayability preserved.** The sim remains deterministic given (teams, seed),
  so the event log the view replays is stable.

## Test results (full EditMode suite)

`test-run … testcasecount="7" result="Passed" total="7" passed="7" failed="0"`

| Test | Result |
|---|---|
| StatMath_MatchesSpecFormulas | Passed |
| Training_RoutesThroughGrowthGrade | Passed |
| Determinism_SameSeedSameHash_100Runs | Passed |
| AllTeamSizes_Terminate_Within_HardResolve | Passed |
| ThirteenthSpecies_FromPureData_ZeroCode | Passed |
| PreparationSignal_TrainedBeatsUntrained | Passed |
| **MirrorComps_NoSideBias** | **Passed** |

## Success criteria

- ✅ `MirrorComps_NoSideBias` passes (50.8%, in 42–58%).
- ✅ Determinism tests pass (100-run identical hash).
- ✅ No new test failures (7/7; previously 6/7).
- ✅ No balance tuning, no `balance.json` change, no gameplay redesign.

## Scope / constraints honored

- Requirements 1–5 met: team-A bias removed; determinism preserved; RNG contract
  preserved (no draws consumed); replayability preserved; battle flow unchanged
  (same loop, same resolver, same targeting — only tie ordering differs).
- Changes confined to `Core/`. `Assets/StreamingAssets/balance.json` verified
  unchanged. `Phase1GateTests.cs` verified unchanged.

## Notes / follow-ups (not done here)

- The **short-duration** finding (P50 ~16 s vs 30–90 s target) is untouched and
  still open — it is a balance concern for the Stage-3 sweep, deliberately out of
  this task's scope.
- This change is uncommitted (per the standing commit-gate). Recommend committing
  it as the next checkpoint once you approve.
