# Test run — eval 6: asset-stack (iteration 1, skill v4)

## Active Role

Lead Architect (asset research). Priority order applied: free → commercial
license → chibi fantasy style → easy Unity integration.

## 1. Monster asset analysis

**A. CraftPix chibi monster packs** — craftpix.net
- Examples: "Chibi Monsters 2D Asset Pack" (5 monsters: turtle, caterpillar,
  piranha, cyclops, pea-knight), "Cute Chibi Monsters" (5 monsters, with walk /
  idle / attack / dead animations), plus the freebie "Free Golem Chibi 2D Game
  Sprites" (3 golems, 17 animation types each, Spriter/SCML + PNG + AI/EPS +
  Unitypackage).
- Cost: freebies are $0; individual paid packs are low-cost (price on each
  product page); an all-access subscription exists.
- License: CraftPix states you can sell and distribute games made with their
  assets. Verify per-pack license page before purchase.
- Art style: vector chibi/cartoon — on-target for 2D chibi fantasy.
- Animations: walk, idle, attack, dead on the monster packs (no dedicated "hit"
  — see gaps).
- Pros: exact style match; animations included; freebies to prototype with;
  vector sources allow recolors for variants.
- Cons: 5 monsters per pack → 2–3 packs needed; animation sources are
  Spriter/SCML (import as sprite sheets in Unity, don't fight the format);
  roster mapping imperfect.

**B. huberthart "2D Monster — Cute & Chibi" series** — itch.io
- Example verified: "Devils" pack — $15, 7 unique chibi monsters, each with
  idle / walk / attack / die animations, ready-to-go prefabs including sliced
  sprites, animation AND sound effects, front+back 8-direction sprites, demo
  scripts, sprite sheets + vector files, no generative AI. Sibling packs exist
  (e.g., "Forest", also $15).
- License: page does not state explicit terms — confirm commercial-use terms
  with the author before purchase (itch asset norms usually allow game use, but
  verify).
- Pros: literally designed for monster games — archetypes, unique skill
  effects, prefabs, per-monster SFX; the fastest possible integration of any
  candidate.
- Cons: paid; 7 per themed pack (need 2 packs for a 12-roster); themes
  (devils/forest) don't match our roster names.

**Honest gap, both candidates:** no pack maps 1:1 to the spec's exact 12
(Slime, Wolf, Bat, Mushroom Beast, Spider, Goblin, Turtle, Ghost, Bee, Golem,
Fire Lizard, Dragonling). Per the asset-driven development rule, adapt the
roster to the chosen pack — the spec's roster requirement is "12 distinct
silhouettes, easy to source," not those exact species. A roster amendment to
game-spec.md should follow the purchase. Missing "hit" animations are solved
the standard mobile way: white-flash + micro-knockback tween (code, free).

## 2. UI asset analysis

**Kenney UI packs** — kenney.nl / kenney-assets.itch.io
- "UI Pack": 430 sprites in 5 colors; "UI Pack (RPG Expansion)": 85 fantasy/RPG
  elements; "Fantasy UI Borders": 130+ sprites for 9-slice RPG windows.
- Cost: free (name your own price). License: CC0 — commercial use allowed, no
  attribution required.
- Pros: zero cost, zero license risk, 9-slice-ready, consistent across packs,
  vector sources.
- Cons: clean-generic rather than chibi-flavored — good enough for MVP; Fantasy
  UI Borders adds the fantasy accent.

## 3. VFX asset analysis

**Cartoon FX Remaster Free (Jean Moreno)** — Unity Asset Store
- 50 unique stylized effects sampled from the 4 commercial packs; works in 2D
  and 3D; includes the Easy Editor to scale/tint/re-time effects.
- Coverage vs spec: slash ✓, fire ✓, impact/hit ✓, explosions ✓, magic ✓; heal
  = tint a magic/pickup effect green via Easy Editor ✓.
- Cost: free. License: Standard Unity Asset Store EULA (extension asset) —
  commercial game use allowed.
- Pros: drop-in prefabs, mobile-tested particle counts, one cohesive cartoon
  style that matches chibi art; $15 upgrade path (Cartoon FX Remaster) if more
  variety is needed later.
- Cons: shared with many games (acceptable at MVP); particle prefabs need a
  sorting-layer pass for a 2D portrait scene.

## 4. Audio asset analysis

**RPG Essentials SFX — Free (Leohpaz)** — itch.io
- 48 free retro-RPG sound effects sampling the author's UI, battle, battle
  magic, heals/buffs, and movement packs. Battle SFX ✓, UI SFX ✓.
- Full "RPG Essentials SFX" bundle: 300+ effects (menus, battle, magic,
  movement, fanfares) for ~$8.
- License: free to use in projects; no reselling/redistributing the pack;
  credits appreciated, not mandatory.

**Minifantasy Dungeon Audio Pack (Leohpaz)** — itch.io — free, 62 SFX + 2 music
tracks → covers the ambient/music slot at $0.

- Pros: purpose-built RPG coverage, consistent author/style across battle, UI,
  and ambient; free tier is genuinely enough for MVP.
- Cons: retro flavor (fits chibi fine); fanfares locked behind the ~$8 bundle.

## Final recommendation — one cohesive stack

| Slot | Pick | Cost |
|------|------|------|
| Monsters (prototype, Build Phases 1–3) | CraftPix Free Golem Chibi (3 animated monsters) | $0 |
| Monsters (release, Build Phase 4) | 2× huberthart Cute & Chibi packs (14 monsters, prefabs+SFX) — pending license confirmation; fallback: 2–3 CraftPix chibi monster packs | ~$30 |
| UI | Kenney UI Pack + RPG Expansion + Fantasy UI Borders | $0 |
| VFX | Cartoon FX Remaster Free | $0 |
| Audio | RPG Essentials SFX Free + Minifantasy Dungeon Audio | $0 |

**Estimated total: $0 for the entire prototype phase; ~$30 at the asset pass
(or ~$15 if the roster shrinks-to-fit one 7-monster pack + freebie golems).**

## Implementation effort (solo dev)

UI: ~2 evenings (import, 9-slice, portrait layout). VFX: ~1 evening (prefab
pass, tint per skill element, sorting layers). Audio: ~1 evening (import,
AudioSource hooks). Monsters: the real cost — huberthart prefabs ≈ 2–3 evenings
for 14; CraftPix sheets ≈ 4–6 evenings (slice, Animator per species). Total
asset integration ≈ 1.5–2 solo-dev weeks — inside the two-week rule, which is
exactly why every candidate above was filtered for included animations.

## Decision persistence

On adoption: amend game-spec.md's roster to the purchased pack's species and
record the final stack + licenses in a new `references/asset-stack.md`.
