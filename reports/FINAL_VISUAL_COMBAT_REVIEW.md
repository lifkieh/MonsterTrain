# FINAL VISUAL & COMBAT REVIEW

Presentation only. No new features/balance/progression/save. Per the brief: **no fake screenshots,
no fake scores, no fabricated observations** — every claim below is tagged:
- **[SEEN]** — a real device frame I personally viewed *this session* (builds 1–6 commits old).
- **[SHIPPED]** — a fix already committed this session (verified to compile + pass tests).
- **[CODE]** — true about the source; accurate about *what exists*, not about how it looks on screen.
- **[UNVERIFIED]** — needs eyes/ears on the current build, which I do not have.

## Phase 11 — Real evidence (done first, because it gates everything)

**Capture was not possible, and here is exactly why:** the target device is your **personal phone,
in active use**. This session, every capture attempt landed on your own content — home-screen game
folder, TikTok, WhatsApp, lock-screen/AOD, and today a **different game (Mobile Legends: Adventure)**
whose screen appeared mid-sweep. I relaunched Train Your Monster to a guaranteed-foreground state and
it *still* got interrupted. I deleted every non-game / personal frame rather than keep or use it.
Desktop capture is also impossible here — a Windows standalone builds and launches but this
automation session has **no interactive display**, so it renders no visible window.

**Therefore this review contains no new gameplay screenshots.** I will not fabricate them. The only
clean game frame I retained this session is the menu (`reports/img/polish/now.png`). Everything about
the *current* combat look is **[UNVERIFIED]** and marked as such.

## Phases 1–2 — Combat readability & staging

- **[SHIPPED]** BRAWL anchor rewritten so each unit chases *its own* opponent, clamped to its own
  half of the arena, no center-drift, no formation snap-back (fixes the "line up then return" you
  reported). **[UNVERIFIED]** whether it now reads as a spread scrum on screen.
- **[SHIPPED]** TAG: front fighter duels center, reserves benched + dimmed at the flanks; only the
  front acts/is targeted (proven by `TagModeTests`, headless). **[UNVERIFIED]** on-screen look.
- **[SHIPPED]** near-death **HP danger pulse** (<25% HP throbs the bar) — serves "who is close to
  dying".
- **[SEEN, old build]** the last combat I actually watched piled center-right with idle loiterers.
  The code cause is removed; I cannot confirm the result.
- **Readable "in 2 seconds"?** The *signals* are wired (team pips = winning; HP colour + pulse =
  dying; target flash = attacked; attacker lunge = attacking) **[CODE]**, but whether they land in
  2s is **[UNVERIFIED]**.

## Phase 3 — Element identity

**[CODE + SEEN old build]** Element VFX are shape-distinct, not colour-only: fire tongues+embers+
scorch, water wave-crescent+droplets+ripples, nature leaves+pollen+vine-ring, lightning zigzag
bolts+sparks, heal plus-crosses+aura. **[SEEN]** a real water battle showed a crescent wave-slash +
a green nature ring — confirmed *not* boxes. **Remaining weakness [CODE]:** the CC0 hit sheets under
the element layer are still generic bursts with no element identity.

## Phase 4 — Impact feel

**[CODE]** the tier hierarchy exists: light (spark+vibrate) < heavy (hit-stop+shake) < crit (star+
push+shockwave) < ultimate (ceremony+eruption+impact-frame) < KO (2.2× eruption+white shockwave+
debris+slow-mo). **[UNVERIFIED]** whether the five tiers *feel* distinct on screen.

## Phase 5 — Environment quality

- **[SHIPPED]** biomes no longer share one tinted forest photo: Nature keeps the photo; **Fire/Water
  build a procedural element sky** + distinct biome silhouettes (volcano/sea) + ground features
  (lava cracks / ripples). Directly targets "does it read as recolor" — **[UNVERIFIED]** result.
- **[SEEN, old build]** before this fix, the water arena was a blue-tinted forest — confirmed cheap.
- **[CODE]** honest ceiling: still procedural shapes, not painted backdrops.

## Phase 6 — Grounding

**[CODE]** per-unit contact shadow that shrinks/fades with launch height (altitude read), receding
ground bands. **[SEEN old build]** fighters still read slightly floaty over a dark lower band. **Not
implemented:** landing dust, slam decal, takeoff puff. **[UNVERIFIED]** current feel.

## Phase 7 — Screen composition

**[SEEN old build]** confirmed issues: dark empty lower ~third of the battle frame; HP-bar name
labels overlap when units bunch; "BATTLE" title clipped by the notch. **[UNVERIFIED]** on current
build. These are real and remain the most likely "unintentional-looking" frames.

## Phase 8 — Audio

**[CODE + system evidence]** audio *outputs* (verified an active media track on device after the
AudioListener fix); events are wired (hit/crit/ult/KO/victory/defeat + 4-state dynamic mix + finisher
duck). **[UNVERIFIED — I cannot hear it]** whether events are recognizable, repetitive, or well
mixed. I will not score the mix by ear I don't have.

## Phase 9 — Play Store reviewer ("keep playing after 10 battles?")

Honest reviewer take, from confirmed evidence (not a played session I can't run):
**Probably not, for a discerning player** — because **single-frame sprites** and **procedural
(not painted) art** read as unfinished next to store games, and I cannot demonstrate the combat is
readable/satisfying on the current build. **What would keep a casual player:** fast auto-battles,
collection + evolution chase, real audio, tag mode, 60 FPS.

## Phase 10 — Competitor comparison (presentation only)

vs Monster Legends / Summoners War / Epic Seven / Raid:
- **Biggest strengths (ours):** deterministic tested core; shaped element VFX; real dynamic audio;
  game-feel layer (anticipation/overshoot/weight/hit-stop); tag mode; clean meta UI.
- **Biggest weaknesses:** (1) single-frame sprites vs their full frame animation — decisive; (2)
  procedural/one-photo backgrounds vs painted layered art; (3) CC0/procedural VFX vs authored
  spritesheets.
- **Highest-ROI presentation improvements** (in order): **frame animation for monsters** (animator);
  **painted per-biome backdrops** (artist); authored element hit VFX. All three are **asset
  production, not code** — code polish cannot close this gap.

## Phase 12 — Scores (only where I have evidence) + verdict

I score **only** axes I can back with real evidence, and refuse the rest rather than fake a number.

| Axis | Score /100 | Basis |
|---|---|---|
| Gameplay (auto-battler loop) | **62** | [CODE/tests] works, deterministic, shallow depth |
| Combat Readability | **N/A** | [UNVERIFIED] can't see current combat — will not fake |
| Combat Feel | **N/A** | [UNVERIFIED] can't see current combat |
| Visual Quality | **40** | [SEEN] single-frame sprites + procedural/one-photo art |
| Animation Quality | **30** | [SEEN/CODE] single-frame + transform deform only |
| Audio Quality | **N/A** | [UNVERIFIED — can't hear] outputs confirmed, mix not judged |
| UX | **72** | [SEEN/SHIPPED] clean meta UI, broken screens fixed |
| Progression | **58** | [CODE] career/quests/ach/dex/daily exist, thin |
| Retention | **55** | [CODE] streaks/quests/collection chase exist, unproven |
| Store Readiness | **38** | asset + unverified-combat blockers |

**"Would I personally upload this build to Play Store today?" → NO.**

**Blockers, ordered by impact:**
1. **Single-frame monster sprites** — the #1 "unfinished" tell. Needs an animator. (asset)
2. **Combat readability/feel unverified on the current build** — I literally cannot confirm the
   battle is satisfying to watch; that must be seen before shipping. (needs eyes)
3. **Procedural / one-photo environments** — needs a background artist. (asset)
4. **Generic CC0 hit VFX** with no element identity under the element layer. (asset)
5. **Empty lower battle frame + HP-label overlap + notch clip** — real composition issues [SEEN old
   build], likely still present. (code-fixable, but needs eyes to tune)

## The honest bottom line

Blockers 1, 3, 4 are **asset production** — no amount of the presentation code I can write fixes
them. Blockers 2 and 5 are code-tunable but require **eyes on the running build**, which I do not
have (desktop = no display; phone = your personal device, in use). The one loop that has actually
produced verified fixes this session is **you as the eyes** ("monsters line up then snap back" → I
fixed it, tested). To go further truthfully I need either a **hands-off ~2-minute capture window**
(unlock the phone, don't touch it, say "go") or your specific observations. I'm not going to invent
a green verdict I can't stand behind.
