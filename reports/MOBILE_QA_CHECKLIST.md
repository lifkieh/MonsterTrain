# Mobile QA Checklist — Train Your Monster (First Playable)

Phase 3 device validation. Run on a **real mid-range Android** (≈4 GB RAM,
Snapdragon 6-series / equivalent, Android 10–13) once the APK is installed. Goal:
**one stable Android MVP build.** Ignore balance, features, content.

Mark each: PASS / FAIL / N-A. Log failures with screen + description; only
**critical usability** issues get fixed now (KILL_CRITERIA S4: any screen past 3
sessions → collapse to a plain list).

## 1. Launch & orientation
- [ ] App installs via `adb install -r` without error.
- [ ] Launches to the **Main menu** without a crash.
- [ ] Renders in **portrait**; does not rotate to landscape when the device turns.
- [ ] No black screen / no pink (missing-shader) UI.

## 2. Portrait layout (all four screens)
- [ ] Main menu: title + PLAY + QUIT all on-screen, not clipped by notch/edges.
- [ ] Team select: title, "x / 3" counter, the 12 species buttons, and START
      BATTLE all visible; grid not cut off top or bottom.
- [ ] Battle: 3 player units (left) + 3 enemy units (right) both fully visible.
- [ ] Result: VICTORY/DEFEAT banner + PLAY AGAIN + MENU visible.
- [ ] Nothing overlaps illegibly; safe-area respected on a notched device.

## 3. Touch targets
- [ ] Every button responds to a **tap** (not just visually — actually fires).
- [ ] Species tiles toggle green/blue on tap; counter updates.
- [ ] START BATTLE is disabled until exactly 3 are picked, enabled at 3.
- [ ] Buttons are large enough to hit with a thumb (≥ ~9 mm / ~48 dp). Flag any
      that are too small or too close together.
- [ ] No dead zones; no double-fire on a single tap.

## 4. Battle readability
- [ ] HP bars visibly drain as damage lands.
- [ ] Floating damage numbers are readable (size/contrast) and not a blur.
- [ ] Dead units visibly change (dimmed) — clear who died.
- [ ] The battle is watchable at the default speed (not too fast to follow, not
      boringly slow). Note the rough battle length in seconds.
- [ ] It is clear when the battle ends and who won.

## 5. Text scaling
- [ ] All labels legible at the device's default font scale.
- [ ] Set the system font size to **Large / Largest** → text still fits its
      buttons/labels (no overflow cutting off words, no overlap).
- [ ] Species stat lines ("HP.. ATK.. SPD..") fit inside their tiles.

## 6. Performance (mid-range)
- [ ] Menus scroll/tap with no perceptible lag.
- [ ] Battle plays smoothly — target **≥ 30 fps**; note if it stutters
      (KILL_CRITERIA S6: cut VFX first, then timebox code opt to 3 evenings).
- [ ] No frame hitching when damage numbers spawn.
- [ ] Memory stable across **5 consecutive battles** (no growth/crash) — watch
      via `adb shell dumpsys meminfo com.trainyourmonster.game`.
- [ ] App resumes cleanly after backgrounding (home → reopen).

## 7. Full-loop stability (the MVP gate)
- [ ] Complete the 7-step loop 3× back-to-back without restart:
      Menu → pick 3 → battle → result → PLAY AGAIN → … → MENU.
- [ ] No crash, no soft-lock (a screen you can't leave), no exception spam in
      `adb logcat` (filter `Unity`).

## Critical-vs-defer triage
**Fix now (critical):** crash · soft-lock · unclickable/off-screen button ·
unreadable battle · portrait failure. **Defer (log only):** visual polish,
alignment nits, animation smoothness, color choices, anything cosmetic.

## Capture for the report
- Device model + Android version + RAM.
- `adb logcat -s Unity` during one full loop (attach).
- Rough fps + battle length.
- Screenshot of each of the 4 screens.
→ record results in a `DEVICE_VALIDATION.md` after the run.
