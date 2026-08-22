# GRAYSCALE RECOGNITION TEST (Phase 9)

The ultimate identity audit: strip colour, keep silhouette + pose, ask "can this monster still be
identified?" Captures converted to grayscale via PIL. Evidence: `reports/img/grayscale/`.

## Test 1 — full roster silhouettes (`roster_gray.png`, all 21 species, colour removed)
**PASS for the roster.** Ignoring the name labels, these read from silhouette alone:
- Clearly identifiable: **bat, bee, wolf, dire_wolf, golem, ghost, jelly, kraken, phoenix, spider,
  treant, turtle, fire_lizard, mantis, blade_mantis, dragonling, inferno_drake, mushroom_beast**.
- Only look-alikes are **evolution families** (wolf↔dire_wolf, jelly↔slime, fire_lizard↔salamander,
  mantis↔blade_mantis) — these *should* resemble each other (same evolution line); their **motion now
  differs** (profile: slime more elastic than jelly, salamander leans harder than fire_lizard, etc.).

## Test 2 — in-combat, colour removed (`arena3v3_gray.png`)
**PASS.** Even grayscale, mid-fight: Fire Lizard (bipedal lizard, **forward-leaning** aggressive
stance), Jelly (round blob), Golem (bulky humanoid, **low + wide + planted**), Mantis (angular insect),
Dragonling (small quadruped, **raised**) are each distinguishable by silhouette + the character-
direction pose. Golem's heavy planted posture and Fire Lizard's lean read without any colour.

## Verdict
Identity survives grayscale. Silhouettes were already strong (distinct CC0 sprites); the Character
Direction pass added a **second recognition channel — stance/lean/height/motion** — so a creature reads
even when colour, element VFX, and (family) silhouette overlap. The only ambiguity left is *within
evolution families by silhouette*, which is correct by design and is now broken by motion.
