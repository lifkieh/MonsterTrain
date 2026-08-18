# Asset Sourcing — Real Game Assets (Phase N)

Date: 2026-08-18. Replaced procedural monster rendering with **real downloaded
CC0 game assets** + real audio. All downloads verified as CC0 (public domain) or
noted otherwise. Raw packs live in `Assets/ExternalArt/`; wired copies in
`Assets/Resources/MonSprites` and `Assets/Resources/Audio`.

## Acquired assets

### Monster sprites (CORE)
| Field | Value |
|-------|-------|
| Pack | **50+ Monsters Pack 2D** |
| Source | OpenGameArt — https://opengameart.org/content/50-monsters-pack-2d |
| Author | isaiah658 (monsters #26/#47 derived from j0j0n4th4n, also CC0) |
| License | **CC0** (public domain, attribution appreciated not required) |
| Sprite count | **56 monsters** × front + back × normal + alternate palette = 224 PNG |
| Animation | Single-frame (Pokémon-style battle sprites) — animated in-engine via transform |
| Resolution | 64×64 px, transparent PNG |
| Download URL | https://opengameart.org/sites/default/files/50_monsters_pack_2d_version_1.0_0.zip |
| Suitability | **9/10** — covers all 21 species + evolutions, exact Pokémon battle framing |

### Battle music
| Field | Value |
|-------|-------|
| Track | **Battle RPG Theme** (CleytonRX) |
| Source | OpenGameArt — https://opengameart.org/content/boss-battle-theme |
| Author | Cleyton Kauffman |
| License | **CC0** |
| Files | MP3 (6.9 MB) + OGG (4.6 MB) — OGG imported (Android-friendly) |
| Download URL | https://opengameart.org/sites/default/files/CleytonRX%20-%20Battle%20RPG%20Theme%20Var_0.ogg |
| Suitability | **8/10** — looped RPG battle theme |

### Creature sound effects
| Field | Value |
|-------|-------|
| Pack | **80 CC0 creature SFX** |
| Source | OpenGameArt — https://opengameart.org/content/80-cc0-creature-sfx |
| Author | rubberduck |
| License | **CC0** |
| Count | 80 `.ogg` vocalizations (roars/grunts/bugs/burbles/…) |
| Download URL | https://opengameart.org/sites/default/files/80-CC0-creature-SFX_0.zip |
| Suitability | **7/10** — monster voices for attack/crit/death/heal (5 wired) |

### Evaluated but NOT used
| Candidate | Reason |
|-----------|--------|
| Animated Fox Sprite Pack (CC-BY, IDoTheDrawing) | 1 creature only; CC-BY adds attribution burden — dropped for CC0 consistency |
| Textured Cute Monster Pack (CC0, quaternius, 21 monsters) | **3D** (FBX/OBJ/Blend) — not usable in the 2D UI battle without a render pipeline |
| 50+ Monsters Pack 3D | Same (3D) |

## Species → asset mapping
Base species map to distinct monster sprites (#1–#18, normal palette). Evolutions
use their base monster's **alternate color palette** (a "same-lineage, upgraded"
recolor — the shiny/evolution look).

| Species | Element | Sprite | Species | Element | Sprite |
|---------|---------|--------|---------|---------|--------|
| bat | Fire | Monster #1 | jelly | Water | Monster #16 |
| bee | Nature | Monster #2 | treant | Nature | Monster #17 |
| dragonling | Fire | Monster #3 | mantis | Nature | Monster #18 |
| fire_lizard | Fire | Monster #4 | **dire_wolf** | Nature | Monster #12 (alt) |
| ghost | Water | Monster #5 | **inferno_drake** | Fire | Monster #13 (alt) |
| goblin | Fire | Monster #6 | **blade_mantis** | Nature | Monster #18 (alt) |
| golem | Nature | Monster #7 | | | |
| mushroom_beast | Water | Monster #8 | | | |
| slime | Water | Monster #9 | | | |
| spider | Nature | Monster #10 | | | |
| turtle | Water | Monster #11 | | | |
| wolf | Nature | Monster #12 | | | |
| salamander | Fire | Monster #13 | | | |
| phoenix | Fire | Monster #14 | | | |
| kraken | Water | Monster #15 | | | |

Player-side units render the **back** sprite, enemy-side the **front** sprite
(authentic Pokémon framing). Sprite missing → procedural `MonsterArt` fallback.

## License verification
All wired assets are **CC0** (public domain). The 50+ Monsters `Credits.txt` and
the OGA CC0 designations are retained in `Assets/ExternalArt/`. No attribution is
legally required; credits are recorded here and appreciated.

## Before / after
- `reports/screenshots/before_collection_procedural.png` — procedural blob monsters.
- `reports/screenshots/after_collection_real_sprites.png` — real CC0 pixel-art monsters
  (on-device: Samsung SM-S731B). Each species now a distinct recognizable creature
  with element badge + rarity frame; locked entries use a procedural silhouette.

Real sprites are wired into: battle units (1v1, back/front), collection, monster
detail, result MVP, and team-select cards. Audio: real CC0 battle music + creature
SFX (attack/crit/ultimate/death/heal).

## Honest gaps
- These are **retro 64×64 pixel** monsters, not hand-painted anime illustrations.
  Free CC0 anime-grade animated monster art for 21 unique species does not exist as
  a single downloadable pack (the closest — quaternius's 21 monsters — is 3D). This
  is the best available **real, license-clean** 2D battle art.
- Sprites are single-frame; idle/attack/hurt/death are done with in-engine transform
  animation (bob, lunge, shake, dissolve) — Pokémon-style, not frame-by-frame.
- FX (attack/hit/explosion/elemental), UI icons, rarity icons, and arena backgrounds
  remain **procedural** (generated), not yet replaced — monsters + audio were the
  priority; those are the next sourcing pass.
