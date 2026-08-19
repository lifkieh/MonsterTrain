# DUAL BATTLE MODES — DESIGN & ARCHITECTURE PASS

Design + architecture only. **No implementation.** Grounded in the actual code
(`BattleSimulator`, `ReplayEvent`/`ReplayBuilder`, `BattleCinematicDirector`,
`EngagementPlanner`, `BattleReplayView`, `UnitView`). Hard constraint honoured: **the
simulator is not to be changed.**

---

## 0. The one fact that drives everything

**The simulator is, mechanically, a Brawl — and only a Brawl.**

- `BattleSimulator.Run` builds each team with `for (slot = 0; slot < cfgTeam.units.Count; …)` —
  team-size agnostic (1, 2, 3, N).
- The main loop pulls `ActionTimeline.NextActor(state)` — **every unit on both teams acts the
  moment its own action timer fires**, concurrently. There is no turn structure, no "front
  fighter", no bench.
- Targeting is free-for-all: `ChooseSkill` → `SkillResolver.HasValidTarget` picks *any* valid
  enemy. Any unit can hit any enemy at any time.
- The sim→view contract is a flat event log (`Spawn / Action / Modifier / Died / End`) carrying
  `actorTeam/slot`, `targetTeam/slot`, `amount`, `crit`. It says **"unit (0,1) hit (1,2) for X at
  t"** — nothing about staging, duels, or reserves.

Consequence: **there is no "active fighter vs standby" anywhere in the model.** All three of your
monsters are always live, always taking and dealing damage. Any "Arena / tag / 1v1" concept is
therefore a **presentation illusion layered on a brawl** — and, as shown below, a *leaky* one.

---

## 1–4. Existing-architecture audit

| Layer | What it is | Mode nature |
|---|---|---|
| `BattleSimulator` (Core) | headless deterministic sim, concurrent action timeline, free targeting | **Brawl** (100%) |
| `ReplayEvent` / `ReplayBuilder` | classifies the sim log into typed events (actor/target/amount/crit) | **mode-agnostic** |
| `BattleCinematicDirector` | per-event `ChoreoBeat` (combo length, dodge, launch, knockback, hit-stop, cam), seeded by logHash | **mode-agnostic** (cinematic weight per hit) |
| `EngagementPlanner` | per-unit continuous engagement **segments** + **filler** beats + **clash** detection | **Brawl-only** brain |
| `BattleReplayView` | consumes all of the above; currently *spotlights one combo at a time* while others idle | **confused** (Arena-style rendering of a Brawl sim) |
| `UnitView` | deform/weight/shadow/`combatOffset`/idle-roam + **`SetReserve(bool)`** (dim+scale a benched unit) | **shared** — already has bench support, currently forced off |

**1. How much of the system already fits Arena Mode?** ≈ **60–65%** *(presentation only)*.
The presentation ingredients exist and are strong: a spotlight-combo path (`SpotlightCombo`:
dash → ground combo → launcher → air combo → slam → recovery), dodge/counter beats, ultimate
ceremony, cinematic cameras (`ZoomCombo/CinematicZoom/SlowMoFinisher`), and **`UnitView.SetReserve`
already dims+shrinks a benched unit**. What's missing: a scheduler that picks *which* single pair
is "active", the discipline to bench everyone else, and a tag-in-on-death flow.
**Caveat (important):** true Arena semantics — "standby units are safe until tagged in" — **cannot
be honoured**, because the sim keeps damaging the benched units. Their HP bars would keep dropping
while they sit on the bench. Arena over this sim is therefore **leaky** unless you fake it.

**2. How much already fits Brawl Mode?** ≈ **75–80%**.
The sim *is* a brawl (100%), and `EngagementPlanner` already derives the continuous "everyone is
tangled with someone" plan (segments/fillers/clashes). Missing: the View currently **renders it as
a spotlight instead of concurrent fights**, so it under-delivers the brawl it already computed —
plus positioning/lanes to stop the centre pile-up.

**3. Which is closer to the current implementation?**
Mechanically **Brawl** (the sim and the planner are brawl). Visually it's an **attempted Arena
spotlight** bolted on top. The on-device result — one featured combo + idle loiterers + a centre
pile — is exactly this mismatch: *a brawl simulation rendered through a half-built arena spotlight.*

**4. What needs separating?**
Only the **staging strategy in the View**. Introduce a `BattleMode { Brawl, Arena }` and extract a
`IBattleStager` (which units are spotlighted, where they stand, reserve on/off, camera width, how
many fights render at once). The sim, ReplayBuilder, Director, Drama and `EngagementPlanner` stay
**shared and untouched**. Arena would add a `SpotlightScheduler` (pick the active pair + order from
the same replay); Brawl keeps consuming `EngagementPlanner`.

---

## 5. Presentation-layer separation (sim unchanged)

### Shared (both modes)
`BattleSimulator`, `ReplayEvent`/`ReplayBuilder`, `BattleCinematicDirector` (ChoreoBeats),
`BattleDrama`, **`UnitView`** (deform, weight, shadow, `combatOffset`, `SetReserve`),
**`BattleArena`** (biome/ground/parallax), **`ElementVfx`**, `BattleFx`, `AudioManager` (music +
dynamic mix + finisher duck), HP bars, floating combat text, **VS screen**, **Result screen**,
finisher ceremony, screen flash / impact frames / camera shake.

### Arena Only
- `SpotlightScheduler` — from the replay, choose the single "active duel" over time + the tag order.
- One-fight-at-a-time gating: only the active pair runs the full cinematic (combo/launcher/air/
  slam/counter); everyone else is benched.
- Reserve staging: `SetReserve(true)` on non-active units, parked small/dim on the flanks.
- Tag-in on death: swap animation bringing the next unit into the spotlight.
- Close "duel" camera (tighter zoom on the pair), counter close-ups, big ultimate showcase.

### Brawl Only
- `EngagementPlanner` consumption for **all** units concurrently (each fights its segment opponent).
- Filler beats (whiff/block/shove) + clash lunges in the gaps.
- Positioning / lanes so the six don't pile centre (the current bug).
- Wide camera (whole arena), idle roam, **no** reserve dimming — everyone full-size and active.

---

## 6. UX

- **Where the player picks:** a small **Arena / Brawl toggle on the Team Select screen** (remembered
  in `SaveData`, additive field). Optional global default in Settings.
- **When a mode is forced automatically:**
  - **Career (ladder stages):** default **Brawl** — it's the auto-battler power-fantasy ("watch my
    team win"), fast, repeatable, fits the collection game. Career already has 21 escalating stages.
  - **Boss / league-finale stages:** force **Arena** — the game already switches to `bossMusic` on
    finale stages; a cinematic single-duel showcase makes the boss the big moment.
  - **Normal PLAY:** honour the player's toggle (default Brawl).
- **Career = Arena or Brawl?** → **Brawl** for normal stages, **Arena** for the boss/finale stage of
  each league.
- **Boss = Arena or Brawl?** → **Arena** (spectacle + readability for the marquee fight).

---

## 7. Cost analysis (presentation-only; sim never touched)

Rough solo-dev estimates. "Arena" assumes the *leaky* presentation-only version (no sim change).

| | Engineering | UI | VFX | Testing | Total |
|---|---|---|---|---|---|
| **Arena Mode** | Med-High — SpotlightScheduler + rewrite View staging to gate one fight, bench others, tag-in-on-death (reuse spotlight/reserve/combo) | Low — bench markers, active highlight, tag banner | Low-Med — tag-in flash, tighter cam (reuse combo/launcher VFX) | Med — staging + tag-order + determinism replays + the "benched HP still drops" reconciliation | **~2–3 wk** |
| **Brawl Mode** | Med — make View render all units on their `EngagementPlan` segments concurrently; positioning/lanes; kill the centre pile | Low — HP-bar de-overlap (bars already exist) | Low — reuse existing VFX | Med — concurrency readability, no overlap, team sizes | **~1.5–2 wk** |
| **Dual Mode** | High — both stagers + `IBattleStager` abstraction + `BattleMode` plumbing (Career/Boss assignment, Team-Select toggle, save field) + refactor `BattleReplayView` to swap strategies | Med — mode-select UI + per-mode HUD | Med — arena tag-in + brawl polish | High — **both** modes × determinism × 1/2/3-unit teams × finisher edge cases | **~4–6 wk** |

Note: Dual ≠ Arena + Brawl added; it also carries the abstraction/refactor + double the
test-matrix + ongoing maintenance of two divergent presentation paths forever.

---

## 8. Recommendation

### Pick: **B — Brawl only** (delivered as a *cinematic brawl*).

Not A, not C.

**Technical reasons**
1. **The sim is a brawl.** Brawl is the only mode that matches the model with zero lies and zero
   sim changes. Arena must either change the sim (forbidden) or **leak** — benched "standby" units
   keep losing HP because the sim is still fighting them; a player watching a "duel" will see the
   guy on the bench die without being touched. That's worse than the current bug.
2. **~75–80% already exists.** `EngagementPlanner` is the brawl brain and it's built, tested, and
   deterministic. The remaining work is making the **View** actually render the plan it already
   computes (all units concurrent) instead of spotlighting one — plus positioning to stop the pile.
3. **Dual doubles cost and the test matrix forever**, to ship a second mode whose Arena half is
   architecturally compromised. Bad trade for a solo/small project heading to the Play Store.

**Player reasons**
1. The game is sold as **collection + auto-battler** ("Raise. Evolve. Conquer."). "Watch my team
   of three gang up and win" (keroyokan) is the on-brand fantasy. Brawl delivers it.
2. My own on-device audit's real complaint was **not** "brawl is bad" — it was **loitering +
   centre pile-up**. Both are fixable inside Brawl by staging all units concurrently with
   positioning, and giving the *current most-dramatic* engagement a larger spotlight while the
   others fight **smaller on the flanks (still visibly fighting, HP still updating)**. That is
   sim-honest, readable, and alive — the "hybrid staging" the audit asked for, **without** a second
   mode.

**What "cinematic brawl" means concretely (still Brawl, one mode):** everyone is always fighting
(sim-honest); the camera + scale emphasise the beat that matters right now (a launcher, an
ultimate, a clash) by enlarging that engagement, while the other fights continue de-emphasised on
the flanks. Reuse the existing spotlight/launcher/ultimate cinematics for the emphasised beat only.

**If the team still wants Arena for spectacle:** scope it to **boss/finale stages only**, and
accept it as a *staged showcase* (the emphasised-fight camera pushed to the extreme), **not** a
mechanically-real tag battle — because a mechanically-real tag battle is impossible without
changing the simulator.

*(Design/audit only — no code written, per the brief.)*
