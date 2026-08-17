# FIRST PLAYABLE — Shortest Path Plan

**Role:** Lead Architect. Planning only. Goal: the shortest path from current
state to a **playable** build.

**Playable = the player can:** open game → select a team → start battle → watch
it → win or lose → return to menu → play again. Nothing more.

## Current state (starting point)

- Unity 6000.5.8f1 project compiles clean; 7/7 EditMode tests pass; deterministic
  sim + initiative fix committed (`083d2cb`).
- **Missing for playable:** generated content assets, a battle view (nothing
  renders), any UI, any scene/flow, a build.
- Balance is amber-frozen (see `PHASE1_BALANCE_LOCK.md`) — current defaults are
  fine for a playable; tuning is deferred and does not block this.

## Key architectural decision (shipping-speed)

**Build the UI entirely in code** (uGUI constructed at runtime by a bootstrap
MonoBehaviour). No manual scene wiring, no prefab authoring — the whole game is a
one-GameObject scene created by an editor script. This makes every screen a
compilable, versionable C# file and keeps the headless build reproducible.
Placeholder visuals: solid-colour `Image` quads + HP bars + legacy `Text`
(built-in `LegacyRuntime.ttf`). Target build: **Windows standalone** first
(fastest to run and watch); Android later.

## Missing systems

| # | System | Effort | Dependencies | Risk | Notes |
|---|---|---|---|---|---|
| M1 | Generate content (12 species + 10 skills) | 0.5 ev | none | Low | Run `MTA → Generate Phase 1 Content`; verify registry loads 12. |
| M2 | GameFlow state machine (Menu→Select→Battle→Result→Menu) | 1 ev | none | Low | Pure C# in `Meta/`; edit-mode testable. |
| M3 | Team model + fixed/random enemy | 0.5 ev | M1 | Low | Player picks 3 of 12; enemy = 3 random (or fixed). |
| M4 | Battle replay view (`BattleReplayView` + `UnitView`) | 2–3 ev | M1, sim | **Med** | Consumes event log only; placeholder quads, HP bars, floating damage. The "watch battle" core; riskiest. |
| M5 | Runtime UI framework + screens (Menu, TeamSelect, Result) | 2 ev | M2, M3 | Med | Code-built canvas + buttons; Kenney-style list fallback (KILL_CRITERIA S4). |
| M6 | Scene bootstrap + wiring | 1 ev | M2–M5 | Med | One scene, one bootstrap object built by an editor script. |
| M7 | Windows build + play-through | 1 ev | all | Low | Build, run, click through the 7-step loop. |
| — | Play-mode flow tests | 0.5 ev | M2, M6 | Low | Assert Menu→Select→Battle→Result→Menu transitions headlessly. |

**Total ≈ 8.5–10 evenings** of build. Comfortably inside 14 working days.

## Recommended implementation order

1. **M1** (content) — unblocks everything; nothing runs without species.
2. **M2** (flow) — pure logic, testable, defines the skeleton.
3. **M4** (battle view) — riskiest; prove "watch a battle" early.
4. **M3 + M5** (team select + UI screens) — the interactive shell.
5. **M6** (scene + wiring) — assemble.
6. **M7** (build + play-through) — verify the loop end-to-end.

## Rejected (NOT in first playable)

Training · leveling/allocation UI · save/load · career/leagues/gates · capture ·
economy/coins · progression · real art/animation · audio · VFX · optimization ·
Android polish · localization · anything on the forbidden list. Any of these
appearing is scope creep (KILL_CRITERIA R2 → auto-Post-Launch).

---

## 14-Day Task Breakdown (first-playable only)

Days are ~one solo-dev session each. Headless note: the agent writes + compiles +
logic-tests the code; **visual/watch verification (M7) requires a human running
the build** — that is the milestone handoff.

**Day 1 — Content + flow skeleton.** Run the generator; verify the registry loads
12 species / 10 skills. Create `Meta/GameFlow.cs` (state enum + transitions) and
`Meta/GameState` model (player team, enemy team, last result). Edit-mode tests for
the state machine.

**Day 2 — Team + battle bridge.** `Meta/TeamBuilder` (player picks 3 ids; enemy =
seeded-random 3). Bridge that turns the two teams into `TeamConfig`s, runs
`BattleSimulator.Run`, and hands the `BattleResult` to the view. Edit-mode test:
a full battle runs from a selected team.

**Day 3 — Battle view skeleton (M4a).** `Battle/BattleReplayView.cs` +
`Battle/UnitView.cs`: lay out 3v3 placeholder quads with name + HP bar from the
event log's starting state; no playback yet. Compile-verify.

**Day 4 — Battle playback (M4b).** Drive the event log over time: advance a clock,
apply `Action`/`Died` events to `UnitView`s (HP bar tween, floating damage number,
death fade). End-of-battle → emit a "finished(winner)" callback.

**Day 5 — Battle view polish-to-functional + test.** Handle heals/modifiers events
(HP up, no crash on buff/debuff), variable team sizes, replay-consistency check
(final HP from view == sim). Play-mode test that a battle plays start→finish.

**Day 6 — Runtime UI framework (M5a).** `UI/UIFactory` (code helpers: canvas,
panel, button, label, HP bar) using built-in font. `UI/ScreenBase`. Main-menu
screen: title + "Play" + "Quit".

**Day 7 — Team-select screen (M5b).** Grid/list of the 12 species (name + role +
base stats text); tap to add/remove up to 3; "Start Battle" enabled at 3.
Feeds `GameState.playerTeam`.

**Day 8 — Result screen + loop (M5c).** Win/Lose banner from `BattleResult`;
"Play Again" → Team-select, "Menu" → Main menu. Wire `GameFlow` transitions to
show/hide screens.

**Day 9 — Scene bootstrap + wiring (M6).** `Editor/FirstPlayableSceneBuilder.cs`
builds the single scene (one `GameBootstrap` object). `GameBootstrap` instantiates
the flow + screens + battle view and starts at Main menu. Everything reachable.

**Day 10 — End-to-end flow test.** Play-mode test driving the whole loop
(Menu→Select 3→Battle→finishes→Result→Play Again→Menu) via simulated button
clicks; assert no exceptions and correct state at each step.

**Day 11 — Windows build.** Add the scene to Build Settings (via editor script);
build a Windows standalone; fix any build-only issues (font, missing refs,
Resources loading). Produce a runnable `.exe`.

**Day 12 — Human play-through + fixes (M7).** Run the build; click the 7-step
loop; fix whatever breaks (layout off-screen, unclickable, null refs). Iterate to
a clean run-through.

**Day 13 — Hardening.** A few battles back-to-back (memory/leaks), edge cases
(all-same-species team, fastest/slowest comps), "Play Again" 5× without restart.
Log any non-blocking issues (do not polish).

**Day 14 — First-playable sign-off.** Final run-through of all 7 steps ×3;
`FIRST_PLAYABLE_SIGNOFF.md` with the checklist; tag/commit the milestone. Buffer
for slippage from earlier days.

**Slack:** Days 12–14 are buffer-heavy on purpose; if M4 (battle view) overruns
(its risk), it borrows from here. Per KILL_CRITERIA S4, any single screen past 3
sessions collapses to a plain list layout.
