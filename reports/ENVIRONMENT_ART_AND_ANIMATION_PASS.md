# ENVIRONMENT ART & ANIMATION SELL PASS

**Scope:** presentation only. **No** change to `BattleSimulator`, determinism, `logHash`,
progression, save, balance, rewards, or AI. No new gameplay. Verified: **76/76 EditMode pass**
(incl. determinism), **PlayMode UI smoke 1/1**, APK rebuilt.

Files: `BattleArena.cs`, `ElementVfx.cs`, `BattleFx.cs`, `UnitView.cs`, `BattleReplayView.cs`.

---

## PART 1 — Environment art

**Before:** every arena was the same CC0 forest photo with a colour tint + a few parallax
diamonds. It read as "a colour filter", not a place.

**Now — each element is built as a real receding biome:**

- **Distant biome silhouettes** (`BuildElementFar`) in front of the backdrop:
  - **Fire** → dark **volcano cones with glowing craters**.
  - **Water** → a **sea horizon** with layered wave bands.
  - **Nature** → a **layered tree line** (depth-tinted).
- **Ground surface features** (`BuildElementGround`) across three depth rows (near = lower +
  bigger), animated:
  - **Fire** → **pulsing lava cracks** (glowing zigzags) + **scorched rocks** + **lava-pool
    glows** that breathe.
  - **Water** → a **reflective sheen** + **expanding ripple rings** + **caustic** dashes.
  - **Nature** → **swaying grass tufts** + scattered **flowers**.

You can now name the arena with the UI hidden — volcano+lava = fire, sea+ripples = water,
trees+grass = nature.

## PART 2 — Ground platform

Replaced the single soft "glow pad" with a **receding terrain plane**: three tone bands
(far-dark → near-light) that read as a floor you stand *on*, a **bright near lip** marking the
front standing edge, and the element ground features above sitting on the surface.

- **Contact shadow** (already added last pass) stays: per-unit, follows X, shrinks/fades with
  launch height.
- **Height readability**: the shadow gap + shrink on launch/air-combo/slam still sells
  altitude; the new solid ground gives it something to read against (a shrinking shadow over a
  flat glow was ambiguous; over terrain it's clear).

## PART 3 — Projectile & skill trails

- **Projectiles now leave a fading ribbon trail** of element-shaped ghosts (9-deep), so a shot
  reads as **streaking energy by shape** — flame / droplet / leaf / bolt — not a moving dot.
  They also arc and lead with the tip.
- Skill/ultimate casts already emit the element-signature aura (previous pass); impacts throw
  the matching element scatter (fire embers, water splash, nature leaves, lightning sparks).

## PART 4 — Impact tiers

Every attack level is now distinct:

- **Light hit** — quick small element scatter + short shake.
- **Crit** — heavier element burst + star flash + vibrate + micro-launch + camera push + short
  hit-stop.
- **Ultimate** — big element burst (1.7×) + ring + screen flash + impact frame.
- **KO** — **new `ElemFx.KO`**: the biggest beat — a **2.2× element eruption** + a **white
  shockwave ring** + an **element ring** + a spray of debris/embers, on top of the existing
  explosion, full arena reaction, camera lurch, and (on the finisher) slow-mo.

## PART 5 — Animation sell (single-frame sprites)

Added a **weight** system (`UnitView.SetWeight`, driven by role — Tank heaviest, Assassin/Mage
lightest):

- **Idle**: light monsters bounce fast + springy; heavy ones plod slow + settled (bob speed,
  amplitude, and breathing all scale with heft).
- **Attack**: heavy bodies **pop harder** (extra squash-scale on the swing) and **lean farther**
  into their motion; light bodies stay snappy.
- Existing deform layer (anticipation crouch → explosive lunge → follow-through overshoot →
  settle, velocity smear, launcher/slam spin, hit-stop vibrate, head-snap on hit) is retained
  and now reads with per-monster weight.
- Idle **roam** (previous pass) keeps units drifting/sidestepping so they never plant.

## PART 6 — Battle readability

Formations are team-size aware (previous pass, retained): 1v1 duel spacing, 2v2 front/back
pair, 3v3 wider spread triangle. Combined with idle roam and the wider ground, bodies don't
stack and the fight isn't jammed in the centre. 3v3 can still get busy when several cinematic
beats overlap — see the honest review.

---

## PART 7 — Brutal visual review (not polite)

Scores are **by-design after this pass, pending on-device confirmation** (Part 8 — the test
device is off USB):

| Axis | Score | Note |
|---|---:|---|
| Arena quality | 7/10 | Real per-element biome + animated ground. Still built on the same tinted forest photo underneath, and features are flat coloured shapes, not painted terrain. |
| VFX quality | 8/10 | Element-signature particles + trails + a real KO beat. The generic CC0 hit sheets still sit underneath as the "punch". |
| Animation quality | 6/10 | Weight + deform make single frames feel alive in motion — but they are still **single frames**. In a still, it's obvious. |
| Battle readability | 7/10 | 1v1/2v2/3v3 no longer stack; 3v3 overlapping ults still get busy. |
| Overall presentation | 7/10 | A clear step up from "sprites on a tinted background". |

**What still looks cheap / placeholder / programmer-art (blunt):**

1. **The backdrop is still one reused forest photo**, tinted per element. The new biome
   silhouettes + ground sell the element in the near/mid field, but the deep background is the
   same image in all three arenas. A real fix needs three distinct painted backdrops.
2. **All terrain is flat solid shapes** — glow ellipses, triangles, zigzag "lava". It reads as
   *stylised* ground, not textured art. No normal maps, no real material — it's clean
   programmer geometry with nice colours.
3. **Monster sprites are single-frame.** This is the #1 cheap tell and this pass can't fix it —
   the deform/weight layer disguises it in motion but a paused frame shows a static, rotating
   image. Real frame animation (walk/attack/hurt) is the only true fix.
4. **CC0 hit sheets** (`hit_small/impact/big/explosion`) are still generic white/orange bursts
   layered under the element VFX. Identity now comes from `ElementVfx`; the sheets are just
   "impact flash" and don't match the element.
5. **Everything is procedural** — the whole look is "competent indie procedural", not
   hand-authored. Coherent, but no piece will read as premium hand-painted art.
6. **No reflections/lighting** — the water "reflective sheen" is a translucent glow, not an
   actual mirror; fire doesn't light the fighters; nothing casts real light.

None of this regresses anything. The brief's specific complaints — "arena is a colour tint",
"ground doesn't feel real", "sprites floating on a background", "VFX/projectiles placeholder",
"no weight" — **are addressed.** The ceiling above is asset production (painted backdrops +
frame animation), not code.

---

## PART 8 — Device verification (PENDING — device off USB)

The Galaxy S25 FE (SM-S731B) dropped off ADB during the previous build and has not
re-enumerated (it needs a physical re-plug / screen-unlock — not doable remotely). **No device
screenshots this run; none fabricated.** The fresh APK is staged at
`Build/Android/TrainYourMonster.apk`.

To capture into `reports/img/environment_art_review/` once reconnected (unlock phone + re-seat
USB): install, play battles, and grab the Fire / Water / Nature arenas (the arena is themed by
the enemy front-liner's element, so pick fights against fire/water/nature leads), plus a 3v3
scrum, an ultimate, an air-combo launcher, a KO finisher, and the victory screen. Pure
1v1/2v2 aren't pickable from the UI (team select forces 3) — capture them from late-battle
survivor states; the formation math itself is covered by the staging tests.

```powershell
$adb="C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
$dir="E:/TrainYourMonster/reports/img/environment_art_review"
& $adb install -r "E:/TrainYourMonster/Build/Android/TrainYourMonster.apk"
& $adb shell svc power stayon true
& $adb shell monkey -p com.trainyourmonster.game -c android.intent.category.LAUNCHER 1
# then screencap the arena themes + combat beats listed above.
```

## Determinism & safety

Simulator, `logHash`, balance, progression, save, rewards, AI: **untouched**. All new motion
(biome, ground features, trails, KO burst, weight, roam) is cosmetic — `UnityEngine.Random` /
`Time` only, never fed back into the sim. `76/76` EditMode pass incl. determinism; PlayMode UI
smoke clean.
