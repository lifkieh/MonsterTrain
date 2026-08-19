# Phase P — Fight HUD & Callouts

Date: 2026-08-19 · Presentation layer only · Author: Lifkie Lie

**Objective.** Make the fight readable and exciting: pooled damage numbers, a live combo
counter, an HP ghost bar, text splashes (ROUND/FIGHT!/K.O.!/COUNTER!), and letterbox bars
— tuned for the O-2 tawuran staging. Plus the deferred Phase O-0 quick UI wins.

## Files touched (presentation only)
- `Assets/Scripts/Battle/FloatingCombatText.cs` — pooled numbers with pop-in, tiered
  size/life, crit shake.
- `Assets/Scripts/Battle/BattleReplayView.cs` — seeded-scatter tiered damage numbers, one
  global combo counter, ROUND/FIGHT!/K.O.!/COUNTER! splashes, letterbox bars.
- `Assets/Scripts/Battle/UnitView.cs` — HP ghost bar (main instant, red ghost lags 0.4 s).
- `Assets/Scripts/Battle/ProceduralArt.cs` — `Star()` icon.
- `Assets/Scripts/App/UIFactory.cs` — `StarRow` (5 star icons).
- `Assets/Scripts/App/GameBootstrap.cs` — O-0 wins: role filter → 2 rows; star icons
  replace `*****` in collection tile + detail.

Gameplay/Core/sim/balance/save/planner/director untouched. Number scatter is seeded by
`logHash` (deterministic); HUD adds no per-frame allocation (pooled text, cached widgets).

## What was built, by task
1. **Damage numbers.** Pooled, pop scale 1.4→1.0, rise ~60 px, fade over life. Spawned at
   the victim with a **seeded scatter** (±26 x, −8..+34 y from `logHash` RNG) so numbers
   never stack. **Tiered:** light hits size 30 / life 0.55 s; crits size 46 / life 0.85 s
   + shake + gold; ultimates size 52 / life 0.95 s + orange. So real crits/ults visually
   dominate the compact chatter.
2. **Live combo counter — one at a time.** A single global "N HITS!" label, scale-punch on
   each increment, colour ramp at 5 / 10 / 15, fades on a lull. **Display rule (documented):**
   the combo is fed by the *same offensive-event set as Combo King* — enemy-targeted
   Attack/Skill/Ultimate (non-buff) events. Each such event increments the one global
   counter; a gap `> COMBO_GAP = 1.2 s` (sim-time) resets it to 1; the label fades out
   ~1.1 s after the last hit. Only shown at `combo ≥ 2`, and only ever ONE counter on
   screen (never per-unit), so the busy tawuran stays readable.
3. **HP ghost bar.** The main green fill now drops **instantly**; a **red** ghost fill
   behind it holds ~0.4 s then drains down to the new value — the classic "recently lost
   HP" bar. Heals snap the ghost up immediately.
4. **Text splashes.** "ROUND 1" at stage build, then **"FIGHT!" fired from the O-2
   opening-charge collision hook** (`OpeningClash`, ~0.62 s) so it lands exactly as the two
   teams clash. "K.O.!" on the finishing blow (with the existing slow-mo + finisher
   banner), "COUNTER!" on the dodge→counter beat. All scale-in (1.7→1.0) + shake, then
   fade. They never block the auto-playing replay, so no tap-to-skip is needed.
5. **Letterbox bars.** Two black bars slide in top/bottom during the finisher slow-mo
   (`ChoreoCam.SlowMoFinisher`) and slide back out on the victory beat.
6. **Deferred O-0 quick UI wins.**
   - Collection role filter is now **2 rows of 3** (300 px buttons at x = ±340, 0), spanning
     ±490 ref-units. At 720×1520 the effective half-width is ~495, so "Support" no longer
     clips (the old single row spanned ±534 and overflowed). Sort button moved to y=662.
   - The `*****` text stars are replaced by a **star-icon row** (`UIFactory.StarRow`,
     procedural gold `Star()` sprite — the Kenney pack shipped no star, noted below) on the
     collection tiles and the detail screen; the detail stats line now reads "Rarity N/5".

## Note on the star icon
`Resources/Ui` / `ExternalArt/ui_kenney` contain only the 9-slice btn/frame/panel — **no
star sprite**. Rather than block on a download (adb/network flaky here), the star is a
crisp procedural 5-point `ProceduralArt.Star()` (gold filled / dim empty). Same visual
intent as the spec (an icon, not asterisks); swap for a real Kenney star later if desired.

## Tuning for tawuran (as requested)
- Damage numbers spawn at the victim with seeded scatter → never stack in the scrum.
- Light-hit numbers are smaller + shorter-lived; crits/ults bigger + longer + shake.
- Exactly ONE live combo counter (global), definition shared with Combo King (above).
- "FIGHT!" is driven by the O-2 opening-charge hook, not a fixed timer.

## Tests / build
- **EditMode: 69 / 69 passed** — sim, determinism, balance, save, progression, planner,
  director unchanged and green.
- **PlayMode UI smoke: PASS** — full battle with damage numbers, combo counter, splashes,
  letterbox, HP ghost bar + the collection screen (2-row filter, star icons); 0 runtime
  errors. (The `UI_LAYOUT` lines are warn-only from the batch's 640×480 window, not the
  device — the harness notes batch res ≠ device; the 2-row filter fits at 1080 and 720.)
- **Android APK: Succeeded** — `Build/Android/TrainYourMonster.apk` (~75 MB;
  `MTA: Android build = Succeeded`).

## Human QA checklist (verify on device)
- [ ] You can "read" the fight: damage numbers, the combo counter, and the K.O. moment are
      obvious and exciting; numbers don't stack into an unreadable pile.
- [ ] Only one combo counter shows at a time; it ramps colour and fades when the chain
      breaks.
- [ ] The HP ghost bar makes big hits feel big (red chunk drains a beat after the green).
- [ ] "FIGHT!" lands right as the two teams collide; "K.O.!" and "COUNTER!" pop at the
      right beats; letterbox bars frame the finisher.
- [ ] Collection: the role-filter row shows all six roles with **"Support" fully visible**
      (no clip); rarity shows as **star icons**, not `*****`. Nothing overlaps at 720×1520.
