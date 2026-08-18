# Phase O-1 — Brawl Staging & Active-Fighter Bug (addendum to FIGHT_FEEL_SPEC.md)

Execution order: **O-0 ✓ → O-1 (this file) → O → P → Q.** All Global Guardrails in
`FIGHT_FEEL_SPEC.md` and every rule in `CLAUDE.md` remain in force; on conflict CLAUDE.md wins.

Report: `reports/PHASE_O1_BRAWL_STAGING.md`

## Objective
1. Fix the "statue" bug: with team [a,b,c], monster `a` stands idle while `b`,`c` animate/attack.
2. Replace sequential 1v1 framing with a simultaneous team brawl: every living unit of both
   teams on stage fighting from second one — no off-screen reserves, no duel-slot waiting —
   kept readable via a spotlight rule. As units die the fight shrinks 3v3 → … → 1v1.

## Step 0 — Investigation gate (READ-ONLY, first)
Determine with certainty whether the sim runs all units concurrently on one interleaved
timeline, or sequential 1v1 duels. Quote evidence. Diagnose statue-bug root cause. Branch:
- Branch A (concurrent sim): 1v1 was only camera/staging → do full task list (presentation only).
- Branch B (sequential duels): true simultaneity changes outcomes = gameplay/forbidden.
  Do only presentation-safe subset (fix statue bug, place benched teammates visibly at back
  edge, step forward on tag), report, STOP.

## Tasks (Branch A only)
1. Stage everyone — UnitView per living unit both teams, 3-slot echelon per side, X stagger +
   Y depth offsets, shared ground band; per-unit shadow+HP from O-0; player mirrored; remove
   reserves + run-in-on-death.
2. Event→actor fidelity via explicit instanceId→UnitView map, never slot/spawn order.
3. Approach & return — melee dashes to a side-offset slot beside target, exchange, returns;
   ranged fire from slot.
4. Spotlight rule — at most ONE full cinematic melee suite at a time, granted to highest-drama
   concurrent event (ultimate > crit > heavy > first blood; seeded). Others use compact hits.
5. Scheduling — respect sim timestamps; overlap allowed; document compress/queue policy.
6. Death = fighting-game KO (launch+spin+fade, HP bar despawns), no run-in.
7. Camera — wide default; punch/shake only on spotlight/crit/ult/KO; ult keeps cinematic zoom.
8. Hit-stop discipline — global freeze only for heavy/crit/ult/KO, never light; cap total freeze.
9. HUD — round pips keep working; no overlap with six HP bars at 720×1520.
10. Pools ×6 pre-warmed; zero alloc in combat.

## Tests
- Untouchable: determinism, sim, balance, save, progression.
- Presentation/choreography tests may update to brawl contract (justify); coverage must not shrink.
  Add a test asserting every replay event actor resolves to a live UnitView.
- PlayMode smoke passes a full brawl, 0 runtime errors.

## Out of scope
Player-facing team-size selector (1v1/2v2/3v3 mode) = gameplay feature, separate decision.
