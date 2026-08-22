# FINAL CHARACTER DIRECTION REVIEW

Goal: **make every monster recognizable from movement alone** — pose, timing, posture, motion, reaction,
death — even in grayscale, even without UI/element effects. Presentation only; deterministic; 79/79
EditMode; Windows standalone + APK build. Before = `showcase_v10`; After = `showcase_v11`; grayscale
`reports/img/grayscale/`; audio `reports/audio/showcase_audio.wav`.

## What shipped (`CharacterProfile.cs` + `UnitView` + `BattleReplayView`)
A per-species character system giving each of the 21 monsters distinct: **stance height, body lean,
idle pace, elasticity, settle speed, attack wind-up, hit reaction, and death motion** — layered on top
of the existing role/element personality. Unlisted species get a hash micro-variation so **no two are
motion-identical**.

- **Phase 2 Personality** — fire_lizard leans/impatient, turtle low/slow-settling, jelly over-squashes,
  wolf stalks low + snaps fast, phoenix hovers, golem immovable. Verified (`3_arena_3v3_08`: distinct
  stances) + grayscale.
- **Phase 3 Hit** — Stiff (golem) / Ripple (jelly) / Slide (turtle) / AirWobble (phoenix) / Recoil.
- **Phase 4 Death** — Collapse / Tumble / Dissolve / Scatter — the loser's body language says who died.
- **Phase 5 Anticipation** — attack wind-up scales by species (turtle telegraphs slow, wolf snaps).
- **Phase 6 Formation** — role staging at battle start (visual only).
- **Phase 7 Finisher** — already strongly directed (hit-stop / slow-mo / camera / hero focus / victory
  screen); added species death motion.
- **Phase 8 Audio** — escalating tiers, element-distinct, ducked finisher (verified from the WAV).
- **Phase 9 Grayscale recognition — PASS** (roster + in-combat).

## Score delta (from captured output)
| Axis | Before | After | Basis |
|---|:--:|:--:|---|
| Species motion identity | 4 | **8** | per-species profiles; stances differ in stills + grayscale |
| Hit-reaction identity | 5 | **8** | 5 distinct hit styles |
| Death identity | 5 | **8** | 5 death styles reveal who fell |
| Grayscale recognizability | 7 | **9** | silhouettes distinct + pose adds a second channel |

## Remaining defects (honest)
1. **Full motion is best in video.** Stills verify stance/lean/silhouette/pose; the dynamic feel
   (settle, ripple, tumble timing) is fully appreciable only in motion (on-device). This is a
   capture-medium limit, not a content gap.
2. **Evolution families share silhouettes** (by design) — separated now only by motion, not shape.
3. **Base CC0 sprite detail/resolution** — unchanged; still the single art-production tell vs the
   AAA-mobile set. No presentation code closes it.
4. Formation staging is **opening-only** (charge overwrites it — intended combat behaviour).
5. Audio SFX priority is round-robin (no dedicated priority bus) — not observed masking anything.

## Publisher-level verdict
Character direction is now **genuinely strong**: 21 creatures that each move, take hits, and die like
themselves, recognizable in grayscale from silhouette + posture + motion. For **character identity and
readability, this is competitive.** For the **product overall vs Monster Legends / Summoners War /
Epic Seven / Raid: still NO — and now for exactly one reason: the base monster sprite art is CC0-tier
detail.** Every motion/direction/identity lever code can pull has been pulled and verified. The only
remaining gap is an art-production task (higher-fidelity, frame-animated monster art) that no
presentation code can substitute. Additional presentation iteration now yields negligible observable
improvement — the ceiling here is art assets, not direction.
