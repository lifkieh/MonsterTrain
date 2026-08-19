# Art Direction & Style Guide — Train Your Monster

**Chosen direction: A — Pokémon-style pixel art.** One style, no mixing.

The 21 monster sprites (isaiah658 "50+ Monsters Pack 2D", CC0) are the anchor: 64×64,
hard 1px outline, 5-colour limited palette, cel shading, point-filtered. Everything else in
the game is now made to match that.

## The rules (every asset must obey)
1. **Resolution / density:** author small. Sprites 64px; generated shapes 24–48px; icon 32px
   upscaled. Never full-res smooth art.
2. **Filtering:** **Point (nearest-neighbour) only.** No bilinear/trilinear. No mipmaps.
3. **Edges:** hard. No anti-aliasing, no soft feathering, no smooth gradients. Alpha is
   near-binary (a light 3-step edge at most).
4. **Palette:** limited / posterised. Cel shading (flat fills + one rim tone), not smooth
   ramps or glows.
5. **Outline:** dark 1px-equivalent silhouette where the source has one; keep it crisp.
6. **Scale on screen:** display at integer-ish multiples so pixels stay square (monsters 4×,
   shapes point-filtered so chunks read).

## Audit — what was inconsistent, and the fix
| Asset | Was | Problem | Fix |
|---|---|---|---|
| Monster sprites (21) | 64px pixel, point | — (the anchor) | Kept. |
| ProceduralArt (disc/glow/ring/triangle/star/rrect) | 96–128px, smoothstep AA, **bilinear** | smooth, high-density, wrong outline/shading vs pixel monsters | Rewritten: 24–48px, **point filter**, hard 3-step alpha → chunky pixel shapes. Feeds shadows, rarity stars, element dots, rings, afterimages, arena particles/mountains, panel/button fills, badges. |
| VFX pack (CodeManu, 8 sheets 660×550) | painterly frames, **bilinear** | smooth painted look, high density | Pixelated (downsampled ×4 → ~165px, hard alpha, posterised) + **point** import. |
| Forest backdrop (MatiasVME, 5120×1440) | painted photo panorama | wildly higher density, painterly | Pixelated (downsampled ×12 → 426×120, 3-bit palette) + **point** import → chunky pixel landscape. |
| Kenney UI (btn/frame/panel 64px 9-slice) | vector-ish, **bilinear** | soft edges vs pixel | Import switched to **point** (crisp 9-slice). |
| App icon (procedural orb) | 512px smooth gradient + soft eyes | smooth, off-style | Redrawn 32px pixel monster-face (banded palette, hard body+rim, hard eyes) nearest-upscaled to 512. |
| UI/HUD text (Arial) | clean sans | — | **Kept** (deliberate exception: HUD/menu text stays a clean readable sans for legibility; many pixel games do this). Documented, not a violation. |

## How it's enforced in code
- `Assets/Scripts/Battle/ProceduralArt.cs` — the pixel-shape generator (point + hard alpha).
- `Assets/Scripts/App/Editor/ExternalArtImporter.cs` — import postprocessor sets **Point**
  for MonSprites/Vfx/Arena/Ui; `ImportAll` re-point-filters the packs.
- `Assets/Scripts/App/Editor/AndroidBuilder.cs` — `MakeIcon` draws the pixel icon.
- Raster packs (Vfx, forest) were pixelated in place (bytes overwritten; `.meta` kept).

## Adding new art later
- Prefer 64px CC0 pixel sprites (isaiah658 packs, or same-constraint pixel art).
- Set Point filter, no mipmaps, uncompressed/RGBA32 on Android.
- No smooth gradients/glows — build shapes via `ProceduralArt` (already pixel) or author them
  as low-res pixel PNGs.
- If a source is painterly and unavoidable, pixelate it (downsample + posterise + point).

## Validation
- `reports/img/pixel_art_consistency.png` — a monster sprite (anchor), the pixelated forest,
  and a pixelated VFX frame side by side: all now share the same chunky-pixel density, hard
  edges, and limited palette.
- EditMode 75/75, PlayMode smoke PASS, Android APK builds with the new pixel icon.
- 54 textures re-point-filtered via `MTA/Import External Art`.
