# Phase O-0 — Naked Sprites & Identity

Date: 2026-08-18 · Presentation layer only · Author: Lifkie Lie

**Objective (from FIGHT_FEEL_SPEC).** Battle monsters must read as free-standing
creatures in an arena (not cards), and every species' name must match its sprite.
This unblocks all later fight-feel work (Phases O → Q).

## Global-guardrail compliance
- **Gameplay untouched.** No change to `Core/**`, `SaveData`/`SaveSystem`,
  `Progression`/`Career`/`DailyRewards`, `balance.json`, or any stat/growth/skill
  value or `speciesId`. Only `.asset` **displayName** and **sprite bytes** changed
  (both explicitly presentation-allowed).
- **Determinism intact.** No `UnityEngine.Random` added to battle outcome paths; the
  self-referential determinism test stays green (EditMode 66/66).
- **No functionality removed.** All existing screens, choreography and tests preserved.

## Files touched
**Assets (sprite bytes only — `.meta`/GUIDs unchanged, so no reference breakage):**
- `Assets/Resources/MonSprites/<id>_front.png` + `<id>_back.png` — **42 files**,
  remapped from the isaiah658 *50+ Monsters Pack 2D* (CC0). Base species use the
  Normal palette; the three evolutions use the Alternative palette of their base's
  pack number.
- `Assets/Resources/Monsters/goblin.asset` — `displayName` only: **Goblin → Squire**
  (`speciesId` `goblin` unchanged).

**Code (presentation):**
- `Assets/Scripts/Battle/UnitView.cs` — un-boxed rewrite (tasks 1,2,3,4,5).
- `Assets/Scripts/Battle/BattleReplayView.cs` — front sprite for both + mirror; new
  `displayNames` dict; pass `displayName`; `Humanize` fallback.
- `Assets/Scripts/App/GameBootstrap.cs` — feed `displayNames`; `Nice()`/`Humanize()`
  helpers; fixed all raw-id UI leaks (task 8).
- `Assets/Scripts/Meta/BattleDrama.cs` — result-screen leader strings humanized
  (no snake_case).
- `Assets/Scripts/App/Editor/ExternalArtImporter.cs` — pixel-art import settings +
  Android RGBA32/uncompressed override (task 6).

**Report:** `reports/PHASE_O0_SPRITES_IDENTITY.md` (this file).

## What changed, by task
1. **Un-boxed units.** `UnitView` no longer builds a card: the team frame, species-
   tinted inner panel, icon badge and nameplate header are gone. A battle fighter is
   now just: bare front sprite + floating HP bar + soft ground shadow.
2. **Hit flash on the sprite.** Flash is a **white copy of the same sprite** layered on
   top (alpha-driven), so the flash matches the creature silhouette exactly — never a
   backing rectangle. Heal tints green, death tints dark, same mechanism.
3. **Side-view staging (KOF).** Both fighters use the **front** sprite; the player side
   is mirrored (`localScale.x = -1`). Back-sprites are retired from battle (assets kept
   for a future "send-out" moment). They stand on one shared ground line (anchors
   `player (-250,-30)`, `enemy (+250,-30)`); the existing `combatOffset` choreography
   already runs along X, so dash/knockback/launch were preserved unchanged.
4. **Floating HP bar** (148×14) above each head with the delayed-damage ghost fill;
   small bold name label above it. Round-pip HUD unchanged.
5. **Ground shadow.** Soft radial (`ProceduralArt.Glow`) ellipse under each unit, a
   sibling of the stage (not a child), so it stays on the ground line while the fighter
   jumps. Scale `1.0 → 0.55` and alpha `0.45 → 0.15` as height `0 → 300px`.
6. **Pixel-art import.** MonSprites import as Sprite, **Point** filter, **Uncompressed**,
   AlphaIsTransparency on, 64 PPU, plus an explicit **Android RGBA32 / uncompressed**
   platform override so 64px art stays crisp on device (≈16 KB each). Sprites render at
   4× (256px) integer scale.
7. **Identity curation.** All 21 species re-mapped by viewing every candidate PNG (56
   monsters × Normal/Alt). Full table below.
8. **Humanized names everywhere.** Battle fighters, collection tiles, detail header,
   team-select cards, progress list, LEVEL-UP / EVOLUTION popups, result MVP + Combo
   King + damage/healing leaders — all show Title-Case `displayName`; no `snake_case`
   id is shown anywhere in the UI.

**Deferred to Phase P** (spec permits): task 9 quick UI wins — collection role-filter
row wrap/scroll ("Support" clip) and replacing `*****` text stars with the Kenney star
icon. Deferred to keep O-0 focused and low-risk; folded into Phase P's task 6.

## Sprite-mapping table (speciesId → pack sprite → displayName → rationale)
Pack = isaiah658 "50+ Monsters Pack 2D" (CC0). "N" = pack monster number; base uses the
Normal palette, evolutions the Alternative palette.

| speciesId | element | pack sprite | displayName | rationale |
|---|---|---|---|---|
| bat | Fire | #14 Normal | Bat | literal winged bat silhouette |
| bee | Nature | #6 Normal | Bee | black-and-white winged bee |
| dragonling | Fire | #19 Normal | Dragonling | baby winged dragon (icy palette — typing color caveat) |
| fire_lizard | Fire | #23 Normal | Fire Lizard | orange Charmander-style fire lizard |
| ghost | Water | #22 Normal | Ghost | classic white ghost with flame |
| goblin | Fire | #1 Normal | **Squire** | no goblin sprite in pack; gold armored humanoid renamed (spec's own example) |
| golem | Nature | #41 Normal | Golem | blocky tan rock golem |
| jelly | Water | #50 Normal | Jelly | green jellyfish / tentacles |
| kraken | Water | #28 Normal | Kraken | green octopus |
| mantis | Nature | #7 Normal | Mantis | green leaf/mantis bug |
| mushroom_beast | Water | #46 Normal | Mushroom Beast | small mushroom creature (only mushroom in pack) |
| phoenix | Fire | #51 Normal | Phoenix | orange-brown bird in flight |
| salamander | Fire | #34 Normal | Salamander | orange dino/newt fire reptile |
| slime | Water | #4 Normal | Slime | round green blob |
| spider | Nature | #53 Normal | Spider | literal white spider |
| treant | Nature | #37 Normal | Treant | plant-crowned upright creature (walking plant) |
| turtle | Water | #55 Normal | Turtle | mossy-shell turtle |
| wolf | Nature | #42 Normal | Wolf | snarling quadruped canine |
| dire_wolf | Nature | #42 **Alt** | Dire Wolf | evo = frost/blue palette of wolf — clearly a fiercer wolf |
| blade_mantis | Nature | #7 **Alt** | Blade Mantis | evo = red palette of mantis |
| inferno_drake | Fire | #34 **Alt** | Inferno Drake | evo = red palette of the salamander dino — reads as a fire drake |

Evolution coherence: each evolved form is the **alternative color palette of its base's
same pack sprite**, so `wolf→dire_wolf`, `mantis→blade_mantis`, `salamander→inferno_drake`
are palette-consistent silhouettes (the pack's intended alt palettes).

**Honest caveats (softer matches):**
- `dragonling` is a blue baby dragon despite Fire typing — best "dragonling" silhouette
  in the pack; no red dragon exists. Off-color typing only.
- `treant` (#37) and `slime` (#4) are the two loosest name↔art fits (a sprout-humanoid
  and a flowered blob) but both read as plant/blob creatures.
- Result-screen "Damage/Healing Leader" lines Title-Case the id in the Meta layer, so a
  hypothetical goblin leader would read "Goblin", not "Squire" (Meta has no displayName
  map). Cosmetic edge only; can align in Phase P.

## Parameters chosen
- Sprite display `ART = 256` (4× the 64px source), `preserveAspect`, Point filter.
- Ground line offset `FOOT = 108`; shadow 150×42, scale 1.0→0.55, alpha 0.45→0.15 over
  height 0→`MAX_JUMP = 300`.
- HP bar 148×14 at `y = +150`; name label bold 20pt at `y = +174`; element dot 20px.
- Reserve scale 0.62 / dim 0.55 (unchanged); spawn-pop and idle bob unchanged.

## Test / build results
- **EditMode: 66 / 66 passed** (0 failed) — sim, determinism, save, balance untouched.
- **PlayMode UI smoke: PASS** — `Boots_NavigatesAllScreens_NoErrors_NoMisplacedButtons`
  booted the game, walked every screen and a full battle with the new un-boxed fighters;
  0 runtime exceptions, 0 misplaced buttons.
- **Android APK: Succeeded** — `Build/Android/TrainYourMonster.apk` (~75 MB;
  `MTA: Android build = Succeeded`).

## Human QA checklist (verify on device)
- [ ] Fighters look like creatures standing in the arena, **not cards** — no boxes,
      frames, or colored rectangles behind them.
- [ ] Both fighters full-body visible, facing each other on one ground line; player side
      mirrored.
- [ ] Jump / launcher moments read as **airborne** — the ground shadow shrinks and fades
      as the fighter rises.
- [ ] Every monster's name feels right for its look; **no `snake_case`** anywhere
      (battle, collection, detail, team-select, results). "goblin" now shows as **Squire**.
- [ ] Sprites are **crisp** (no blur); performance feels unchanged.
