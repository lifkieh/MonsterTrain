# FINAL COMBAT PRESENTATION REVIEW

**Scope:** presentation-only combat polish. **No** change to the simulator, determinism,
balance, progression, or save format. Verified: **76/76 EditMode tests pass** (incl. the
determinism/logHash suite), PlayMode UI smoke ran clean (no runtime errors), APK rebuilt
(53.7 MB, dev/ARM64). No new gameplay features.

Files touched (all in `Assets/Scripts/Battle/`): `ProceduralArt.cs`, `ElementVfx.cs` (new),
`BattleFx.cs`, `BattleReplayView.cs`, `UnitView.cs`, `BattleArena.cs`.

---

## PART A — Elemental VFX identity

**Problem found (real, in the shipped code):** the only element routing was
`efx = Fire ? "fire" : Water ? "electric" : ""`. So **Water played the lightning/electric
sheet**, **Nature played nothing**, and it only fired on skills/ultimates — every basic hit
was a generic white CC0 burst. `BattleFx` drew its slash/impact/crit/heal/ultimate bursts as
**plain `Image` quads = solid rectangles** (the "programmer art" boxes). Projectiles were 34px
solid squares.

**Fix — a new procedural, silhouette-first element VFX system** (`ElementVfx.cs`) that emits
shape-distinct particles per element. You can tell the element from the outline with colour
removed:

| Element | Signature silhouettes |
|---|---|
| **Fire** | rising **flame tongues** (rounded bulb → wavy point) + **embers** (spraying triangles, gravity) + a **scorch** disc on the ground |
| **Water** | a **wave-slash crescent** + **droplets** (teardrops) arcing out and falling + **mist** bloom |
| **Nature** | tumbling **leaves** (pointed lenses, spinning) + rising **pollen** + a **vine-burst ring** |
| **Lightning** | jagged **bolts** (zigzag) + scattering **sparks** (stars) |
| **Heal** | rising **plus-crosses** + a gold **aura ring** + soft motes |

New pixel shapes in `ProceduralArt`: `Flame`, `Leaf`, `Bolt`, `Plus`, `Crescent`, `Droplet`
— point-filtered chunky pixels, consistent with the monster art.

`BattleFx` bursts **de-boxed**: Slash → crescent arc, Impact/Ultimate → radial glow, Crit →
star, Heal → plus. Projectiles now carry an **element-shaped head** (flame/droplet/leaf/bolt),
point along their flight path, and arc.

**Routing:** element signature now fires on skill/ultimate wind-up (caster), on every heavy
melee landing (launcher + slam), on ranged landings, on crit compact hits, and on heals —
resolved from each attacker's element. Pooled (128 particles), zero per-frame allocation,
cosmetic RNG only (never touches the deterministic sim).

## PART B — Ground & arena

- **Contact shadow strengthened**: larger (178×52), darker (α 0.52), flatter — reads as a
  planted footing, still shrinks/fades with launch height so air combos read as airborne
  (this system already existed; it was too faint).
- **New ground pad** in the arena: a wide soft shelf beneath the fighters plus a brighter
  contact rim at the standing line, so units read as standing **on** a surface instead of
  floating — and it fills the dead lower third of the frame.

## PART C — Battle layout (1v1 / 2v2 / 3v3)

`Formation` is now **team-size aware** (it was a fixed 3-slot echelon regardless of count):

- **1v1** — cinematic duel spacing.
- **2v2** — a clean front/back pair, no overlap.
- **3v3** — a wider spread triangle (front / back-high / mid-low), more X spread and deeper Y
  stagger than before so bodies never stack and the fight isn't jammed into the centre.

## PART D — Movement presentation

New **idle roam** in `UnitView`: when a fighter is not attacking, knocked, or choreographed,
it slowly wanders around its home slot — sidestep / drift / gentle circling — blended out the
instant combat claims it. Never touches `BasePos`, so choreography and VFX anchoring are
unaffected, and it feeds nothing back into the sim. Units no longer plant on one spot.

## PART E — Arena usage

Wider formations (Part C) + idle roam (Part D) spread the fighters across more of the arena,
and the ground pad + parallax give the lower/mid frame something to occupy. Combat still
resolves through the existing engagement planner (unchanged — it's determinism-adjacent), so
the *collision point* is still driven by the sim; the presentation around it now uses more
space.

---

## PART F — Readability audit (honest)

Scores below are **by-design after this pass**, pending on-device confirmation (see Part G —
the test device dropped off USB before capture). Not polite:

| Axis | Score | Note |
|---|---:|---|
| Visual readability | 8/10 | Silhouette-distinct element VFX + firmer shadows help a lot. |
| Spectacle | 7/10 | Good on ults/finishers; basic hits still lean on the generic CC0 punch. |
| Element identity | 8/10 | Fire/Water/Nature/Heal now unmistakable by shape. Was ~3/10 (water=lightning, nature=nothing). |
| Arena usage | 6/10 | Wider + roam helps, but the fight still gravitates to one collision zone. |
| Battle clarity | 7/10 | 1v1/2v2/3v3 formations no longer stack; 3v3 scrums can still get busy on overlapping ults. |

**What still feels cheap / placeholder / programmer-art (blunt):**

1. **The CC0 hit sheets** (`hit_small/impact/big/explosion`, `speedlines`, `puff`) are still
   generic white/orange bursts layered *under* the new element signatures. They read as
   "impact" but not as the element — the element identity now comes entirely from `ElementVfx`
   sitting on top. They should eventually be tinted/replaced per element or dropped.
2. **Monster sprites are static** — they squash/stretch/spin/flash (deform layer), but there
   are no real per-frame walk/attack animations. Up close this is the biggest "it's cheap"
   tell; the deform hides it in motion but not in a screenshot.
3. **One reused backdrop** — the arena is a single tinted forest photo for every element.
   Fire/Water/Nature *tint* it and add ambient particles, but it's not a real lava / ocean /
   grove environment. Reads as "a colour filter", not a place.
4. **Ground pad is a soft glow ellipse**, not a textured floor — it grounds the fighters but
   won't survive a close crop; it's a gradient, not terrain.
5. **Projectiles are a single shape with a short arc** — no real trail/ribbon, so fast ranged
   exchanges still read thin.
6. **No per-unit facing/turn** — mirrored front sprites for both sides (KOF-style), which is
   fine at distance but obvious in a duel.

None of these are regressions; they're the honest ceiling of a procedural + CC0 pipeline. The
element-identity and grounding problems the brief called out **are** fixed.

---

## PART G — Device verification (PENDING — device dropped off USB)

The Galaxy S25 FE (SM-S731B) that this session used for QA **disconnected from ADB during the
build** (Unity kills the ADB server as part of its Android post-process) and did not
re-enumerate — it needs a physical re-plug / screen-unlock, which can't be done remotely. No
device screenshots were captured this run, and **none are fabricated.**

The fresh APK is built and staged at `Build/Android/TrainYourMonster.apk`. As soon as the
device is reconnected (unlock the phone, re-seat the USB cable), the captures for
`reports/img/combat_final_review/` can be taken with this ready script — it installs, plays a
battle, and grabs frames at the requested beats (VS/1v1/2v2/3v3-scrum/air-combo/ultimate/
finisher):

```powershell
$adb="C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$dir="E:/TrainYourMonster/reports/img/combat_final_review"
function cap($n){ & $adb shell screencap -p /sdcard/q.png; & $adb pull /sdcard/q.png "$dir/$n.png" | Out-Null }
& $adb install -r "E:/TrainYourMonster/Build/Android/TrainYourMonster.apk"
& $adb shell svc power stayon true
& $adb shell monkey -p com.trainyourmonster.game -c android.intent.category.LAUNCHER 1
# team-select -> pick 3 owned (blue) -> START, then cap across the fight:
#   VS screen, opening scrum (3v3), a launcher/air-combo, an ultimate, the KO finisher,
#   and the late-battle 2-alive / 1-alive survivor states (proxy for 2v2 / 1v1 staging).
```

Pure 1v1 / 2v2 pickable battles aren't reachable from the normal UI (team select forces 3),
so on-device those formations are best captured from late-battle survivor states; the 1/2/3
formation math itself is exercised by `Formation()` and covered by the staging tests.

---

## Determinism & safety

- Simulator, `logHash`, balance config, progression, save format: **untouched**.
- All new motion (element particles, idle roam, ground pad) is cosmetic and uses
  `UnityEngine.Random` / `Time` only — it never feeds the sim.
- `76/76` EditMode tests pass, including the determinism suite; PlayMode UI smoke clean.
