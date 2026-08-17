# KILL_CRITERIA.md — Monster Trainer Arena

**Active Role:** Creative Director (scope protection), thresholds verified by
the Lead Architect. Companion to PROJECT_KNOWLEDGE.md; commit both at repo
root. This document is approval-gated under Decision Persistence: its
thresholds may be retuned **now, in advance** — never in the moment a rule
trips, because that moment is exactly what the rule exists for.

**Purpose:** endless development is the default failure mode of solo projects.
This file replaces in-the-moment judgment (which is always compromised by sunk
cost and attachment) with decisions made in advance, while calm. When a
tripwire fires, the prescribed action is the DEFAULT; not taking it requires a
written override (see Override Protocol).

**Cadence:** one 15-minute review at the end of every development week, plus a
mandatory check at every phase gate. The weekly checklist is at the bottom.

Clock definitions: **T** = the week the Phase 1 scripts are first imported
into Unity. Phase clocks start when the phase starts. "Evening" = one solo-dev
work session (~2 h).

---

## 1. Simplification Triggers

*The feature stays; the implementation shrinks to its pre-named fallback.*

| # | Tripwire (objective) | Prescribed action |
|---|---|---|
| S1 | Any system exceeds its Scope Review estimate by **>50%** (e.g., a 1-week system passes 1.5 weeks unfinished) | Ship the fallback from the Fallback Ladder (§3) — same day decision |
| S2 | The same problem survives **3 working sessions** with no measurable progress | Simplify around it or route via fallback; stop attacking it head-on |
| S3 | Balance tuning: sweep still fails the 30–90 s window after **3 full tuning passes** (or 5 evenings total) | Apply the balance ladder in order: retune K → retune HP gain rates → cut skill multipliers to 2.5×/3.5× flat → drop Buff/Debuff from launch balance (Damage/Heal only) |
| S4 | Any single UI screen exceeds **3 evenings** | Rebuild it as a plain Kenney 9-slice list layout; no custom layout work |
| S5 | A bug consumes **>2 evenings** without a reliable repro | Simplify or fence off the feature it lives in; log as known-issue if non-blocking |
| S6 | Battle scene below **30 fps** on the low-end test device | Cut VFX/particle counts first; code optimization is timeboxed to 3 evenings, then cut more VFX |
| S7 | Content authoring (45 career comps) exceeds **1 week** | Filler rungs share comps; only champions and scout battles get unique teams |

## 2. Feature Removal Triggers

*The feature leaves the MVP entirely.*

- **R1 — Required features are never removed, only simplified** (§3). Removal
  applies to Nice To Have items and anything not in the GDD's Required list.
- **R2 — New ideas during Phases 1–5 are auto-classified Post Launch.** Zero
  exceptions without a written approval note that names the existing work cut
  to pay for it (GDD rule, restated here because this is where it dies).
- **R3 — Schedule breach:** at any phase gate, if the project is **>2 weeks**
  behind the phase targets in §4 → remove the largest unfinished Nice To Have
  item immediately. If none remain → apply the Fallback Ladder to the largest
  in-progress Required system.
- **R4 — Nice To Have sunset:** at the start of Phase 5 **or** at week T+20,
  whichever comes first, every unfinished Nice To Have item (mastery grades,
  battle replays, rich opponent previews, second save slot) is removed to
  post-launch automatically. No review, no discussion.
- **R5 — Asset overrun:** if the Phase 4 asset pass exceeds **+1 week**, the
  count of *unique sprite sets* drops (recolors fill the gap); the species
  count stays 12 and the purchase budget stays ≤ $30. If the preferred pack's
  license is still unverified at Phase 4 start, it is removed as a candidate
  that day — the verified fallback ships.

## 3. Fallback Ladder (pre-committed simplest shippable versions)

Decided now, while nobody is attached to the fancy version:

| Required feature | Simplest shippable fallback |
|---|---|
| Capture pick screen | 3-button dialog listing the defeated team; choice mechanic is untouchable |
| Training UX | Grade discovery only — freshness decay and opponent-context hints cut |
| Overnight camp | Standard 2 h session with a 4× yield "overnight" flag; no scheduling UI |
| Battle presentation | Static sprites, HP bars, floating damage numbers; no animation blending |
| League map | Vertical list of 45 buttons with lock icons |
| Opponent preview | One line of text: "Fast team / Hits with skills / Tanky" |
| Promotion gates | A text requirement on the locked button; no dedicated screen |
| Monster detail | Stats + allocate buttons + training log as plain text |
| Skills content | The Phase 1 shared 10-skill pool ships as-is; signature skills become post-launch |
| Audio | Battle hits + UI clicks + one music loop; ambient cut |

## 4. Phase Timeboxes and Gates

| Phase | Target | Hard cap | On hitting the cap |
|---|---|---|---|
| 1 — Battle prototype | 4 wks | **6 wks** | Freeze per §5, even if amber |
| 2 — Progression | 3.5 wks | 5 wks | R3 fires; enter Phase 3 with fallbacks |
| 3 — Content pass | 4.5 wks | 6 wks | S7 + R3 fire; champions protected |
| 4 — Asset pass | 3.5 wks | 5 wks | R5 fires; ship prototype art where needed |
| 5 — Release | 2.5 wks | 4 wks | Ship with known-issues list (§6) |
| **Total** | **~18 wks** | **T+26 wks** | §6 hard ship / §7 decision point |

## 5. Phase 1 Freeze Criteria

Phase 1 is **frozen the same day** that all seven success criteria pass —
determinism hash ×100 · sweep P10 ≥ 30 s and P90 ≤ 90 s with ≤5% hard
resolves · mirror 50%±3% · trained beats untrained ≥75% · 13th-species
zero-code test · mechanics unit tests · debug replay on an Android device.

**Green means gone.** No polishing past green: after freeze, Phase 1 code
changes only via a bug fix attached to a failing test. Refactors require a
written override.

**Amber freeze (the anti-perfectionism clause):** at the 6-week hard cap, if
criteria 1, 5, 6, 7 pass (determinism, zero-code, mechanics, device) but the
balance criteria (2–4) still fail → **freeze anyway** and carry balance tuning
into Phases 2–3. Balance lives in `balance.json`; it does not block
progression work and must not hold the phase hostage.

**Red flag:** if the determinism test itself still fails at 6 weeks, the
foundation is broken — go directly to §7.

## 6. MVP Release Freeze (Ship Triggers)

- **Content lock:** end of Phase 4 **or** week T+22, whichever comes first.
  After lock, only release-blocking bugs are worked.
- **Release-blocking bug (exhaustive definition):** crash · save corruption or
  loss · progression blocker (a required rung/gate cannot be passed) · Play
  Store policy/build failure. *Nothing else blocks release.* Everything else
  ships on a known-issues list.
- **Ship bar (all objective):** battle ≥30 fps on the low-end device · launch
  balance.json passes the sweep window · manual new-player run
  (install → starter → first capture) completes in ≤20 minutes without dev
  intervention, 3 consecutive times · save survives 20 kill/relaunch cycles.
- **Hard ship date: week T+26** — upload to the Play Store with whatever
  passes the blocking-bug definition. Exactly **one** 4-week extension is
  permitted, ever, and only with a written cut list; at week 30 the build
  ships or §7 fires. Polish is not, and will never become, a release
  condition (Shipping > Polish is locked).

## 7. Project-Level Decision Point (last resort)

Trips on any of: (a) determinism unfixable by week 8 · (b) the
preparation-signal gate (trained ≥75%) provably unreachable by any
balance.json tuning by the end of Phase 2 — meaning the core fantasy is
mechanically false · (c) week 30 with no shippable build · (d) **4
consecutive weeks with zero commits.**

The action is not automatic cancellation. It is a mandatory written decision,
using the CD feature framework, choosing exactly one: **continue with a named
cut list · pause with a dated resume · stop.** Drifting is the only forbidden
outcome; stopping on purpose is a legitimate result of this document.

## 8. Override Protocol

Any tripped rule may be overridden **once per rule for the whole project**,
with a written note (rule, reason, new deadline) recorded per Decision
Persistence. A second trip of the same rule cannot be overridden. Overrides
never apply to: the T+26 hard ship date (§6 has its single built-in
extension), R2 (new-idea freeze), or the §7 decision requirement.

## Weekly 15-Minute Review Checklist

1. Any system >50% over estimate? (S1)
2. Same problem 3 sessions running? (S2)
3. Any screen past 3 evenings, any bug past 2? (S4, S5)
4. Am I inside the current phase's target? Within 2 weeks of it? (§4, R3)
5. Did I add or gold-plate anything not in the GDD this week? (R2)
6. Weeks since last commit? (§7d)
7. If Phase 1: are any success criteria green that I'm still polishing? (§5)

Answer honestly, apply the prescribed action, write one line in the log, close
the file. The whole point is that the decisions are already made.
