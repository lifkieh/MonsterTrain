# Phase O-2 — "Tawuran" Engagement System (addendum to FIGHT_FEEL_SPEC.md)

Execution order: **O-0 ✓ → O-1 ✓ → O-2 (this file) → O → P → Q.** All Global Guardrails
in `FIGHT_FEEL_SPEC.md` and every rule in `CLAUDE.md` remain in force; CLAUDE.md wins.
Precondition: O-1 concluded in **Branch A** (concurrent sim) — confirmed.

Report: `reports/PHASE_O2_TAWURAN.md`

## Why
O-1 staged everyone but kept auto-battler grammar (formation slots, dash-out-hit-return,
idle between events) — reads as a turn queue. Goal: **tawuran** — a continuous street
brawl where everyone is tangled and always fighting. Sim/log/timestamps stay exactly as
they are; only between/around-event motion changes.

## Grammar (presentation only)
1. Opening charge: both teams sprint to centre and collide (multi-clash spark). FIGHT!
   splash hook for Phase P.
2. Persistent engagement — kill return-to-slot. Each melee unit stays on its partner
   (small disengage step, never retreat to a line). Slots gone after the charge.
3. Living idle — never still: weight shifts, small steps, circling, feints (seeded RNG).
4. Filler beats (non-damaging): whiffs, blocked hits (clash spark + tiny pushback, 0 HP),
   shoves. NEVER spawn damage numbers, never move HP, visibly distinct from real hits,
   rate-limited so real hits stay loudest.
5. Clash moments: two units' real events targeting each other within ~0.15 s → lunge,
   collide with spark + mini shockwave, brief lock, push apart. Deterministic from log.
6. Target switching: next real event names a new target → break and charge it, no teleport.
7. Ranged units kite: drift/backpedal when an enemy closes, fire on the move.
8. Scrum drift toward centre so teams interpenetrate; keep soft separation (no stacking).
9. Crowd flinch: on ult/KO nearby non-participants briefly flinch away from the blast.
10. Spotlight + hit-stop rules carry over from O-1 unchanged; non-spotlight units keep
    brawling per 2–8 instead of idling.

## Architecture
Plan engagements offline from the complete event log (seeded by logHash): an engagement
planner in the Meta choreography layer derives who fights whom in which windows and emits
a continuous motion track per unit. Real events are immovable anchors; filler is elastic
and must never delay/shift a real event. No UnityEngine.Random; no per-frame allocation.
Rendering in MTA.Battle; planning scene-free in Meta.

## Tests
- Untouchable: determinism (logHash), sim, balance, save, progression.
- Add: planner determinism (same log+seed → identical plan) + filler beats carry zero
  damage/HP.
- PlayMode smoke passes a full tawuran battle, 0 runtime errors.

## Out of scope
Player-facing team-size selector (1v1/2v2/3v3 mode) = gameplay feature, separate decision.
