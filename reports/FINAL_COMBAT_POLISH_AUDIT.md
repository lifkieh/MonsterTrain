# FINAL COMBAT POLISH AUDIT

Presentation-only audit. **No** balance/damage/AI/progression/save/determinism/rewards touched.

## Honesty preamble (read this first)

Section H asks me to **judge with my eyes, not from code**, and Section I asks whether I'd play 50
battles in a row. **I cannot do that part autonomously, and I will not fake it:**

- **Desktop capture is impossible in this environment.** A Windows standalone builds and launches
  but this automation session has **no interactive display**, so it creates no visible window to
  screenshot (verified this session — a live `TrainYourMonster.exe` process exists with no window).
- **The Android device is your personal phone, in active use.** Every attempt this session hit
  Always-On-Display sleeps, personal-app content (WhatsApp/TikTok), or USB drops. My last capture
  attempt this turn returned your lock screen + a TikTok notification, which I deleted.

So the **current build** (after the tawuran-anchor fix + tag mode) I have **NOT seen**. Everything
below is split into:
- **[SEEN]** — a real device frame I personally viewed *this session* (from a build 1–3 commits old).
- **[CODE]** — assessed from the source (accurate about what exists, not about how it *looks*).
- **[CANNOT ASSESS]** — needs eyes on the current build; not answered rather than guessed.

The productive loop this session has been: **you look, you tell me the specific problem** ("baris 3
terus balik ke posisi", "no sound") **and I fix it fast and verified.** That works. Blind visual
polishing does not, and I won't pretend otherwise.

---

## A. Two-mode audit (BRAWL / TAG, 10× each of 1v1/2v2/3v3)

**[CANNOT ASSESS]** — 60 observed battles is exactly the eyes-on task I can't run. What I *did* see
earlier (older builds):
- **[SEEN, old build]** BRAWL 3v3: all six clustered center-right during the opening charge; one
  "spotlight" combo ran while others (e.g. Dragonling) stood idle far left; left ~40% of the arena
  was empty. **This is the exact problem I then changed** (`EngageAnchor` now glues each unit to its
  own opponent instead of a center-drift / formation lane) — but I have **not** verified the new
  behaviour on screen.
- **[CANNOT ASSESS]** TAG in any size — the tag presentation (Arena staging) has never been seen
  running. Its *logic* is proven headlessly (front-only acts + is targeted, `TagModeTests`), but
  "does it look like a fighting-game tag" is unanswered.

## B. Impact FX pass — current state **[CODE]**

What exists today:
- **Basic:** silhouette contact-flash (white sprite copy) + `hit_small` CC0 spark + short vibrate +
  knock + squash. **Missing vs the brief:** a real "hit pause" on basics (hit-stop is only on
  crit/ult/KO by design). Basics may still read light — *plausible but unverified*.
- **Crit:** brighter flash, star burst, camera shake + push, micro-launch, short hit-stop, shockwave.
- **Ultimate:** the 4 stages exist in code (cast aura → super ceremony/cut-in → projectile/dash
  travel → element eruption + impact frame), but whether the **4 stages read as distinct** on
  screen is **[CANNOT ASSESS]**.
- **KO:** the biggest beat in code (`ElemFx.KO` 2.2× eruption + white shockwave + element ring +
  debris + explosion + full arena reaction + slow-mo + camera lurch). Reads biggest *by budget*;
  unverified by eye.

## C. Skill shape pass **[CODE + SEEN old build]**

`ElementVfx` + `ProceduralArt` already emit **shaped** silhouettes, not boxes: Fire = flame tongues +
embers + scorch; Water = wave-slash **crescent** + droplets + mist; Nature = leaves + pollen + vine
ring; Lightning = zigzag bolts + sparks; Heal = plus-crosses. **[SEEN, old build]** in a water
battle I saw a real crescent wave-slash + a green nature ring — confirmed *not* coloured boxes.
Remaining cheapness: the **CC0 hit sheets** (`hit_small/impact/big/explosion`) still sit under the
element layer as generic white/orange bursts and are **not** element-shaped — they're the "circle
without identity" the brief warns about.

## D. Positioning pass **[CODE]**

- **BRAWL:** current algo = each unit chases its engagement-segment opponent + weaves + soft
  `Separate()` (135px min), clamped to its own side of its target, no center-drift, no formation
  snap-back (this turn's fix). On paper this is "engagement zones + local avoidance". **Whether it
  actually reads as a spread keroyokan and doesn't re-pile is [CANNOT ASSESS].**
- **TAG:** front fighter → duel center (±190), reserves benched at the flanks (±452, dimmed 0.62).
  Matches "active center, reserves clear, not interfering" **in code**; unseen.

## E. Grounding pass **[CODE + SEEN old build]**

Exists: per-unit contact shadow (shrinks/fades with launch height), receding ground tone-bands + near
lip. **Missing vs brief:** no **landing dust** on touchdown, no **slam impact decal**, no takeoff
puff. **[SEEN, old build]** monsters still read slightly floaty over a dark lower band. Grounding is
**improved but not finished** — and the specific "dust/decal" asks are **not implemented**.

## F. Arena readability — **honest answer, as demanded**

**[SEEN, old build] The water arena did NOT read as sea.** It was the single reused **forest photo**
(`Resources/Arena/forest`) tinted blue, with water ripple/caustic decals only on the lower ground
band. So: **"still looks like one background that is tinted" — YES, that failure is real** for the
deep backdrop. The element **ground** features + distant biome silhouettes I added help the
near/mid field, but the **far backdrop is the same forest image for all three elements.** Fire and
Nature backdrops I have **not** seen on device, so I won't claim they read as lava/forest — but by
construction they use the **same** tinted forest photo, so the same criticism almost certainly
applies. **This is the clearest confirmed problem in the whole audit.**

## G. Camera pass **[CODE]**

Current: Brawl shake/zoom/push scaled ×0.5 + zoom capped 1.12 (calm); Arena keeps full punch + 1.35
cap. Letterbox + finisher slow-mo exist. Direction matches "cinematic not heboh" **in intent**;
whether Brawl is now calm *enough* with 6 active units is **[CANNOT ASSESS]**.

## H. Device review

**[CANNOT ASSESS]** — could not view the build (see preamble). No eyes-based review was performed.

## I. Brutal final verdict

I will answer only what I can support, and mark the rest honestly.

1. **Does BRAWL now look like a real 3v3?** — **UNKNOWN.** The center-pile cause is removed in code,
   but I have not seen the result. On the last build I *did* see, the answer was **no** (pile + idle
   loiterers).
2. **Does TAG now look like a tag battle?** — **UNKNOWN.** Logic is correct + tested; the look is
   unseen.
3. **10 cheapest things** (from code + old frames, honestly): (1) far backdrop is one tinted forest
   photo in every biome; (2) CC0 hit sheets are generic circles with no element identity; (3)
   single-frame monster sprites (obvious in any still); (4) no landing dust / slam decal; (5) lower
   arena band still reads as empty-ish dark space; (6) basics may still feel light (no basic
   hit-pause); (7) ground is stylised flat shapes, not textured terrain; (8) projectiles are one
   shape + a short trail, no ribbon/impact-decal variety; (9) HP-bar name labels overlap when units
   bunch; (10) result screen still shows the "DEFEAT / Clutch Victory" contradiction on a loss.
4. **10 most professional** (from code + old frames): (1) shaped element VFX (flame/crescent/
   leaf/bolt), not boxes; (2) deform-layer game feel (anticipation/overshoot/squash/weight); (3)
   KO/ultimate cinematic budget; (4) VS screen; (5) clean, readable meta UI after the anchor fix;
   (6) locked 60 FPS on device; (7) dynamic music mix + real audio (now that the AudioListener is
   fixed); (8) tag mode is a *real* rotation, not a fake; (9) 1v1/2v2/3v3 actually work; (10)
   deterministic, test-covered core.
5. **Is combat sellable vs an indie mobile game?** — **NOT YET, and not honestly assessable by me
   right now.** The two blockers I'm confident about: the **one-photo-for-every-biome backdrop** and
   **single-frame sprites**. Neither is fixable blind to a professional bar.
6. **Would I play 50 in a row?** — **I cannot answer this honestly without seeing it, so I won't.**
   Refusing to fake this is the point of Section I.

---

## What I recommend instead of blind polishing

The confirmed, code-level problem worth fixing next (I'm confident it's real because I **saw** it):
**make the arena backdrop element-specific** so Water reads as sea, Fire as lava, Nature as forest —
right now all three are the same tinted forest photo. That's a targeted, describable change.

But per the brief ("find problems first, don't code immediately") and because I can't verify visuals
here, the right next step is a **short hands-off capture session**: unlock the phone, leave it alone
~2 min, and I'll drive BRAWL + TAG battles, capture the real beats, and *then* polish against real
frames until the answers to I.1/I.2/I.6 are genuinely yes. Say "go, hands off" when ready.
