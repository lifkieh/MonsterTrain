# Fight Feel & Visual Polish — Execution Spec (Phases O-0 → Q)

Project: Train Your Monster (MTA) · Repo: `E:\TrainYourMonster` · Unity 6000.5.8f1 · Android portrait
Status: APPROVED plan. Execute **one phase per session**, in order: **O-0 → O → P → Q**.
Companion file: `CLAUDE.md` (hard rules — always in force). If this spec and CLAUDE.md ever
conflict, CLAUDE.md wins.

> Catatan (ID): File ini untuk Claude Code. Jalankan SATU fase per sesi, verifikasi di HP
> antar fase, baru lanjut fase berikutnya.

---

## 0. Global guardrails (every phase)

**Scope — presentation layer ONLY.**
- MAY change: `Assets/Scripts/Battle/**` (BattleReplayView, UnitView, MonsterVisual, Vfx,
  BattleArena, AudioManager, MonsterArt), `Assets/Scripts/App/**` (GameBootstrap, UIFactory,
  editor tools incl. ExternalArtImporter), and the choreography classes in Meta
  (`BattleCinematicDirector`, `BattleDrama`, `ReplayBuilder`, `BattlePlayback`) — choreography
  timing/visuals only, never outcomes. Meta classes must stay free of UnityEngine scene deps;
  all rendering lives in MTA.Battle / MTA.App.
- MUST NOT change: `Assets/Scripts/Core/**`, `Meta/SaveData.cs`, `SaveSystem`, `Progression`,
  `Career`, `DailyRewards`, `Assets/Resources/balance.json` (and its StreamingAssets mirror),
  any stat/growth/skill values inside `MonsterSpecies` `.asset` files, and any `speciesId`
  string (save keys). Editing `.asset` **sprite references and `displayName`** IS allowed.
- Determinism: the self-referential test (same seed → same `logHash`) must stay green. All
  visual randomness (ghost jitter, dust direction, vibrate phase, etc.) must come from the
  director's RNG seeded by `logHash` — never `UnityEngine.Random` in battle code.

**Performance (low-end Android target).**
- No per-frame allocations in battle: pre-warmed pools for ghosts / damage numbers / VFX,
  cached `WaitForSeconds`, no LINQ in hot paths, no `Instantiate`/`Destroy` during combat.
- Sanity-check layout at 1080×2340 AND 720×1520 (PlayMode smoke covers 1080; eyeball 720 via
  Game view or note it for device QA).

**Verification protocol (run after implementation, before commit).**
```powershell
# EditMode (all existing 66 tests must pass; you may ADD tests, never delete/weaken)
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -batchmode -projectPath E:/TrainYourMonster -runTests -testPlatform EditMode -testResults out_edit.xml -logFile out_edit.log
# PlayMode UI smoke (boots game, walks every screen + a battle, asserts no runtime errors)
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -batchmode -projectPath E:/TrainYourMonster -runTests -testPlatform PlayMode -testResults out_play.xml -logFile out_play.log
# APK build (launch detached for long builds; success line: "MTA: Android build = Succeeded")
& "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe" -batchmode -quit -projectPath E:/TrainYourMonster -executeMethod MTA.App.EditorTools.AndroidBuilder.BuildApk -logFile b.log
```
Optional (adb is flaky on this setup — never blocking): install
`Build/Android/TrainYourMonster.apk` via the Unity-bundled adb.

**Report + git protocol (per phase).**
1. Write `reports/PHASE_<ID>.md`: what changed, files touched, parameter values chosen,
   test/build results, and the phase's **Human QA checklist** copied in for the user.
2. Commit as author `Lifkie Lie <llifkie@gmail.com>`, push to `origin/master`.
   NEVER add Co-Authored-By / AI attribution trailers; verify
   `git log -1 --format=%b` contains no "Co-Authored".
3. STOP. Summarize for the user what to verify on device. Do not start the next phase.

---

## Phase O-0 — Naked Sprites & Identity
Report: `reports/PHASE_O0_SPRITES_IDENTITY.md`

**Objective.** Battle monsters must read as free-standing creatures in an arena (not cards),
and every species' name must match its sprite. This unblocks all later fight-feel work.

### Tasks
1. **Un-box the battle units.** In battle, render each unit as a bare transparent sprite:
   no card panel, no rarity frame, no colored background rect behind the fighter. Locate how
   `UnitView` builds its visual (likely the same card pattern as the collection grid) and
   strip battle presentation down to: sprite + floating HP bar + shadow. Preserve aspect
   ratio; pivot at bottom-center on the ground line.
2. **Hit flash on the sprite itself.** White/colored flash must tint the monster sprite
   (color or material swap on its renderer), never a backing rectangle.
3. **Side-view staging (KOF style).** Both fighters stand on ONE shared ground line in the
   lower half of the portrait screen, facing each other along X. Use **front sprites for
   both** sides; mirror the player side (`flipX` / negative X scale). Retire back-sprites
   from battle (keep the assets; they may return for a "send out" moment later). Adapt the
   existing `combatOffset` choreography axes so dash/knockback run along the new facing axis.
   Reserves stay off-screen; the run-in on death continues to work.
4. **Floating HP bar.** Small bar above each fighter's head (current HP + name optional),
   replacing any card header in battle. Keep the existing round-pip HUD.
5. **Ground shadow.** One soft ellipse sprite under each unit (can be generated or a simple
   radial-gradient PNG). Scale ~1.0 → 0.55 and alpha ~0.45 → 0.15 as the unit's height above
   ground (from `combatOffset.y`) goes 0 → max jump height. This is what makes launchers and
   air combos read as "airborne".
6. **Pixel-art import settings.** In the AssetPostprocessor / `ExternalArtImporter` for
   `Resources/MonSprites`: Filter Mode = Point, Compression = None (RGBA32), Alpha Is
   Transparency = on, and render sprites at integer scale multiples so they stay crisp.
7. **Identity curation (the big one).** Current mapping is wrong (e.g. "bat" shows a yellow
   knight, "mantis" a fox, "jelly" a sheep, "ghost" a sauropod, "bee" a robot). Fix per
   species, for all 21:
   - Locate the species→sprite assignment mechanism (`MonsterVisual` and/or the
     `MonsterSpecies` `.asset` sprite fields) and the full available sprite set
     (`Assets/ExternalArt` / `Resources/MonSprites` — isaiah658 "50+ Monsters Pack 2D", CC0).
     If the unused pack sprites were never imported, tell the user to re-download the pack
     and stop that subtask.
   - **View every candidate sprite PNG** (you can read images) and build a mapping table.
     Preference order: (a) re-map a better-fitting sprite to the existing name;
     (b) if nothing fits the name, rename `displayName` to fit the sprite (e.g. yellow
     knight → "Squire"). NEVER change `speciesId`.
   - Keep the three evolution pairs coherent: evolutions use the base's alt-color palette,
     so a remapped base must still produce a sensible `dire_wolf` / `inferno_drake` /
     `blade_mantis`. Note each pair's result in the table.
   - Respect role/element silhouettes where possible (Tank looks bulky, Assassin lean, etc.).
   - Put the full final table (speciesId → sprite file → displayName → rationale) in the
     phase report for human review.
8. **Humanize names everywhere.** All UI (collection, detail, team select, battle, results)
   must show `displayName` — never raw ids like `mushroom_beast` or `fire_lizard`. Title-case,
   no underscores.
9. **Quick UI wins (do if under budget, else defer to Phase P).** Collection role-filter row
   must not clip ("Support" is cut off) — make it wrap to two rows or horizontally
   scrollable. Replace the `*****` text stars with the star icon from the Kenney UI pack.

### Acceptance criteria
- In battle: no boxes/frames behind fighters; both fighters full-body visible, facing each
  other on one ground line, front sprites, player mirrored; shadow scales with height.
- All 21 species: name plausibly matches sprite; mapping table in report; `speciesId`
  untouched; evolution pairs coherent.
- No raw snake_case ids visible anywhere in UI.
- EditMode all green · PlayMode smoke PASS · APK builds · report written · committed+pushed.

### Human QA checklist (user, on device)
- Fighters look like creatures standing in the arena, not cards.
- Jump/launcher moments read as airborne (shadow shrinks).
- Every monster's name feels right for its look; no `snake_case` anywhere.
- Sprites are crisp (no blur), performance feels unchanged.

---

## Phase O — Fight Feel Core
Report: `reports/PHASE_O_FIGHT_FEEL.md`

**Objective.** Make hits feel heavy and motion feel alive using pure code — squash & stretch,
victim reactions, tiered hit-stop, anime impact frames, afterimages. No new assets.

### Tasks (parameter values are starting points — tune by eye, log finals in the report)
1. **Squash & stretch (attacker).** Anticipation before a dash: scale (1.10x, 0.85y) for
   ~0.07 s, then dash with stretch ~1.15 along motion / 0.90 perpendicular; return with a
   small overshoot ease. Implement as a deform layer on `UnitView` separate from
   `combatOffset` so choreography math is untouched.
2. **Velocity lean & spins.** Rotate sprite toward movement, clamped ±12°. Full 360° spin
   (~720°/s) during launcher rise and slam descent — single-frame sprites tolerate rotation
   extremely well.
3. **Victim selling (the core of game feel).**
   - Knockback: offset burst with ease-out decay over 0.25–0.35 s, distance scaled by hit
     tier.
   - Juggle: parabolic arc (parameterized, NOT physics sim) fitted to the director's
     existing air-combo beat timing, so the timeline never desyncs.
   - Ground bounce on slam: 1–2 bounces at ~50% then ~25% height, dust puff VFX (existing
     `puff`) on each contact.
   - Impact squash on victim: (1.2x, 0.8y) for ~0.08 s on hit.
   - Vibrate: victim shakes ±2–3 px at ~50 Hz for the duration of hit-stop.
4. **Tiered hit-stop.** Using the existing sim-clock freeze: light ~0.04 s, heavy ~0.09 s,
   crit/ultimate ~0.15 s. Attacker holds pose; victim vibrates; VFX may keep playing.
5. **Impact frames (anime signature).** On crit / ultimate / finisher: 1–2 frames
   (~0.05 s) of full-screen high contrast — victim silhouette flashed white, everything else
   near-black (fullscreen overlay + temporary material/color swap). Restore state in a
   `finally`/coroutine-safe way so an exception can never leave the screen black.
6. **Afterimages.** Pooled ghost SpriteRenderers (pool 8–12, pre-warmed at battle start).
   During dash/ultimate movement spawn one every ~0.035 s: copy of current sprite, tinted
   element color, alpha 0.5 → 0 over 0.2 s, no rotation update after spawn.
7. **Seeded randomness.** Any jitter above pulls from the logHash-seeded director RNG.

### Acceptance criteria
- Deform layer proven independent: determinism test + choreography director tests green.
- No allocations during a battle after warm-up (spot-check with Profiler if convenient;
  at minimum: all spawning goes through pools).
- EditMode all green · PlayMode smoke PASS · APK builds · report + params · committed.

### Human QA checklist
- Hits feel weighty (pause + shake + knockback), launchers/slams feel acrobatic.
- Impact frames pop on crits/ults without feeling seizure-y (if too strong, report it).
- No stretched-sprite artifacts, no ghost sprites stuck on screen, stable FPS on device.

---

## Phase P — Fight HUD & Callouts
Report: `reports/PHASE_P_FIGHT_HUD.md`

### Tasks
1. **Damage numbers.** Pooled floating numbers: pop scale 1.4 → 1.0, rise ~60 px, fade over
   0.6 s. Crits: ~1.6× size, shake, distinct color. Element-advantage hits may tint.
2. **Live combo counter.** "N HITS!" (Killer Instinct energy) fed by the same combo tracking
   that powers the result screen's Combo King. Scale-punch on each increment; color ramps at
   5 / 10 / 15; fades out when the combo breaks.
3. **HP ghost bar.** Behind the main HP fill: on damage, main fill drops instantly, a
   white/red ghost fill lerps down after a 0.4 s delay — the classic fighting-game
   "recently lost HP" bar.
4. **Text splashes.** "ROUND N" → "FIGHT!" at round start (integrate with existing round
   pips), "K.O.!" on the finishing blow (with the existing slow-mo), "COUNTER!" on the
   dodge→counter choreography. Scale-in + tiny shake; skippable by tap where blocking.
5. **Letterbox bars.** Two black rects slide in top/bottom during the finisher slow-mo,
   slide out on the victory beat.
6. Pull in any Phase O-0 "quick UI wins" that were deferred.

### Acceptance criteria
- Readable at 720×1520; numbers/counter pooled (no per-frame alloc).
- EditMode green · PlayMode smoke PASS · APK builds · report · committed.

### Human QA checklist
- You can "read" the fight: damage, combos, and the K.O. moment are obvious and exciting.
- HP ghost bar makes big hits feel big. Nothing overlaps or clips on the phone.

---

## Phase Q — Super & Ceremony
Report: `reports/PHASE_Q_SUPER_CEREMONY.md`

### Tasks
1. **Ultimate super-flash (KOF/Guilty Gear style).** On ultimate cast: dim overlay to ~70%
   black over 0.15 s, caster stays fully lit above the overlay (sorting order), zoom-in,
   skill-name banner slides across, optional diagonal portrait cut-in (~0.4 s), then the
   move executes. Freeze the sim clock during the ceremony via the existing hit-stop
   mechanism. Total ceremony ≤ ~1.5 s.
2. **VS screen pre-battle.** Both teams' lead portraits slam in from left/right, divider
   flash, names + league; 1.5–2 s, tap to skip. Built from existing portraits + UIFactory.
3. **Finisher upgrade.** Final blow → existing slow-mo + desaturate/darken the arena layers
   + zoom + "K.O.!" + ~0.5 s hold before the victory sequence.
4. **Audio impact layering.** Whoosh on wind-up; impact SFX tiered light/heavy/crit; pitch
   randomized ±10% (seeded RNG); bass thump on ultimates. Assets: download from kenney.nl —
   **"Impact Sounds"** and **"Voiceover Pack: Fighter"** (announcer: "Fight!", "K.O.!" etc.).
   Both are expected CC0 — verify the license file inside each download before use; if the
   terminal can't download them, ask the user to download and drop them into
   `Assets/ExternalArt/`, then import via `ExternalArtImporter.ImportAll` and update
   `reports/ASSET_SOURCING.md` (name, author, license, URL).
5. **Ceremony budget.** Total added presentation overhead across VS screen + supers +
   finisher must stay ≤ ~15 s per battle. If over, trim durations.

### Acceptance criteria
- Ultimates feel like an event; VS screen and K.O. ceremony are skippable/snappy.
- ASSET_SOURCING.md updated; licenses verified or explicitly flagged unverified.
- EditMode green · PlayMode smoke PASS · APK builds · report · committed.

### Human QA checklist
- The super-flash moment makes you want to screenshot it (these become store screenshots).
- Announcer + impact audio lands; nothing clips or peaks painfully at max SFX volume.
- Battle total length still feels good; skips work.

---

## Deferred — Phase R (do NOT execute without explicit user approval)
Idle breathing, parallax scroll on dash, reactive arena (slam dust decals, super darkening),
distinct per-element arenas, ×2 speed toggle, release keystore + AAB, store art, privacy
policy / Data Safety / content rating. These get their own spec after Q ships.

## Appendix — Kickoff prompts (paste into Claude Code, one per session)
```text
Read CLAUDE.md and FIGHT_FEEL_SPEC.md at the repo root. Execute Phase O-0 ONLY, exactly as
specified, respecting all Global Guardrails. Before editing, list the files you plan to
touch. When done: run EditMode tests, PlayMode smoke, and the APK build per the spec, write
the phase report including the sprite-mapping table and Human QA checklist, commit as
Lifkie Lie <llifkie@gmail.com> with no AI/co-author trailers, push, then STOP and tell me
what to verify on my phone.
```
For later sessions, replace "Phase O-0" with "Phase O" / "Phase P" / "Phase Q" (and drop the
sprite-mapping-table mention).
