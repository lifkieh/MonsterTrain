# Phase R — Arena Reaction & Environment Pass

Date: 2026-08-19 · Presentation layer only · Author: Lifkie Lie

**Objective.** The arena becomes alive and reactive: it responds to impacts, each element
gets a distinct atmosphere with ambient motion, a third parallax layer adds depth, and the
world reacts to crits/ults/finishers — all pooled, zero runtime allocation.

## Guardrail compliance
Balance, progression, save, battle simulation, and determinism are **untouched**. All work
is in `Battle/BattleArena.cs` (the arena) and the wiring in `Battle/BattleReplayView.cs`.
No `UnityEngine.Random` on any outcome path (arena particle scatter is cosmetic only).

## Files touched
- `Assets/Scripts/Battle/BattleArena.cs` — reactive impact pool + `Flash`, per-element
  atmosphere/motion, foreground parallax layer.
- `Assets/Scripts/Battle/BattleReplayView.cs` — `React()` / `Flash()` calls at the impact
  beats.

## What was built
1. **Reactive arena.** `BattleArena.React(kind, groundPos)` fires from a **pre-warmed pool
   of 40** particles (parallel arrays, ring-buffer cursor, zero per-spawn allocation):
   - **Light hit → dust** (soft glow puffs kicking up).
   - **Heavy/crit → ground crack** (dark shards that pop in and fade) + dust.
   - **Launcher → debris burst** (shards flung up with gravity + spin).
   - **Slam → large shockwave** (expanding ring + dust ring).
   - **KO → biggest reaction** (double shockwave + 8 debris + cracks + arena flash).
   Fired from: ground-combo hits, compact/lunge/ranged hits, launcher, slam, every KO, and
   the opening-charge collision.
2. **Element arenas.** Fire / Water / Nature each have a distinct palette (sky gradient,
   mountains, ground/floor, accent), plus a foreground silhouette layer and — for Water — a
   drifting mist band. (A neutral default palette covers non-elemental.)
3. **Environment motion.** Fire: embers rise + **flicker** + a subtle **heat-shimmer** wobble
   on the backdrop. Water: bubbles rise + **wobble**, a **surface wave** sway on the floor,
   and drifting **mist**. Nature: **tumbling leaves** (rotating) + **wind gusts** (a global
   sinusoidal push) carrying pollen.
4. **Camera parallax — three depth layers.** Far mountains (×0.03), near pillars (×0.09), and
   a new **foreground** silhouette layer (×0.17) all shift against the camera shake/zoom, plus
   a slow backdrop drift; depth reads on every shake and punch-in.
5. **Character idle life.** Already covered by the O-2 tawuran engagement (units continuously
   circle / reposition / kite — never static) plus the UnitView breathing scale and velocity
   lean from Phase O. No statues. (No change needed here; verified.)
6. **Impact on the world.** Crit → environment shake (existing camera shake). Ultimate →
   **arena flash** in the caster's element colour. Finisher → the full **KO** arena reaction
   (double shockwave + debris + cracks + flash) under the slow-mo.
7. **Performance.** Everything is pooled and pre-warmed at arena build: the 40-particle
   reaction pool, the 18 ambient particles, and the arena flash. `Tick`/`React` do no
   allocation (parallel value arrays, cached particle `Image`s, no `GetComponent` in the hot
   loop, no `Instantiate`/`Destroy` during combat). This stacks on the already-pooled VFX
   (×24), afterimages (×16), and floating text from O/P.

## Tests / build
- **EditMode: 69 / 69 passed** — sim, determinism, balance, save, progression, planner,
  director unchanged and green.
- **PlayMode UI smoke: PASS** — boots the game, builds the arena + reaction pool and runs a
  battle; 0 runtime errors. Every Resource (sprites/audio/backdrop) loads.
- **Android APK: Succeeded** — `Build/Android/TrainYourMonster.apk`.

## APK size delta
| Build | Bytes | MB |
|---|---|---|
| Phase Q | 78,820,444 | 78.8 |
| Phase R | 64,827,732 | 64.8 |
| **Δ** | **−13,992,712** | **−13.7 MB** |

The APK got **smaller**. Verified complete, not stripped: the ARM64 `libil2cpp.so` (46 MB),
`libunity.so` (34 MB), `classes.dex`, `global-metadata.dat`, and the game `.resource`
(sprites/audio/arena) are all present, one scene, and PlayMode loads every asset. Phase R
adds only a little code, so the decrease is a **cleaner build** (earlier builds carried extra
IL2CPP/debug or stray recovery/test artifacts) rather than lost content — worth a glance on
device to confirm nothing visual is missing.

## FPS measurements
**Pending device.** Target 60 FPS on Galaxy S25 FE. The device is currently disconnected
(adb dropped), so on-device FPS could not be captured this session. Performance is
zero-allocation by construction (all pools pre-warmed; no per-frame `GetComponent`,
`Instantiate`, or LINQ in battle). To measure on reconnect: install, open a battle, and read
FPS via the on-screen stats / `adb shell dumpsys gfxinfo com.trainyourmonster.game`.

## Screenshots
**Pending device** (adb disconnected). On reconnect: capture the three element arenas (Fire /
Water / Nature) and a KO reaction, and drop them here.

## Human QA checklist (verify on device)
- [ ] Hits kick up **dust**; crits/heavies **crack** the ground; launchers throw **debris**;
      slams send a **shockwave**; the **K.O.** makes the biggest arena reaction.
- [ ] Fire / Water / Nature arenas look clearly different (palette + ambient motion: embers &
      shimmer / bubbles, mist & wave / leaves, pollen & wind).
- [ ] Camera punch-ins and shakes reveal **depth** (foreground vs background move at
      different rates).
- [ ] No monster stands perfectly still; ultimates **flash the arena**; the finisher gets a
      full environmental reaction.
- [ ] **FPS stays smooth** with six fighters + VFX + arena reactions all at once (target 60
      on S25 FE). If it dips, tell me which moment.
