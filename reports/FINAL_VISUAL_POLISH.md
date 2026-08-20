# FINAL VISUAL POLISH

Presentation only. No balance/progression/rewards/save/determinism/AI/economy change.

## Phase 1 — Play the game (attempted, mostly blocked)

I tried both paths the brief specifies:
- **Phone:** it was connected + the game was foreground. The moment I tapped PLAY, the screen left
  the game to your **home-screen games folder** (Genshin/ML/PUBG/Steam…); an earlier attempt hit
  Always-On-Display + a TikTok notification. You're actively using the device, so my automated
  taps/captures keep landing on your personal screens. I deleted every personal frame and kept only
  the clean game **menu** (`reports/img/polish/now.png`).
- **Desktop:** impossible here — the standalone builds and launches but this session has **no
  interactive display**, so there's no window to see.

**So I could not play + watch the current build.** I will not fabricate a "played 20 battles"
review. What follows is honest about that.

## Phase 2 — Brutal review

The only fair review I can give is from frames I **actually saw earlier this session** (builds 1–4
commits old) + code. The two problems I was most confident about — because I saw them on real device
frames — I fixed this pass:

1. **Every biome was the same tinted forest photo.** Water battles were a *forest* with a blue
   filter. That's the brief's "tidak boleh hanya tint warna" failure, confirmed by eye earlier.
2. **"DEFEAT / Clutch Victory"** on the result screen — the victory-flavored subtitle showed even on
   a loss (saw it live). Self-contradicting.

## Phase 3 — Fixes made this pass (real, confirmed problems)

- **Arena identity:** the forest photo is now **Nature/default only**. **Fire** and **Water** build a
  procedural element sky (gradient bands + far mountains) plus their existing distinct biome
  silhouettes (volcano+craters / sea+waves) and ground features (lava cracks / ripples+caustics), so
  they no longer read as a tinted forest. `BattleArena` backdrop refs are null-guarded for the two
  photo-less biomes. *(Fix is code-correct; not visually verified — see Phase 1.)*
- **Result headline:** the flavor subtitle is now gated by win/lose. Wins keep "Clutch Victory /
  Total Domination"; losses show a loss line ("Crushed / Outmatched / Close Loss / So Close…") from
  the enemy's win tier. No more "DEFEAT / Clutch Victory".

Both are additive presentation changes. **79/79 EditMode pass (incl. determinism), PlayMode smoke
1/1, APK rebuilt (80.6 MB).** No gameplay/balance/save touched.

I deliberately did **not** blind-code the rest of Phase 3 (impact-FX retiming, camera, positioning
re-tuning): those need the eyes-on loop to tune without regressing, and I can't verify them here.
Changing them blind risks making combat worse in ways I couldn't catch.

## Phase 5 — Self review (honest)

**Still bad / cheap (from code + earlier real frames):**
- Single-frame monster sprites — obvious in any still. **Needs an animator.**
- Backdrops/terrain are procedural flat shapes + one photo — **needs an asset artist** (painted
  lava / ocean / grove backdrops; textured ground).
- CC0 hit sheets (`hit_small/impact/big/explosion`) are generic bursts with no element identity —
  **needs VFX art** or removal.
- Whether the tawuran-anchor fix + tag staging actually read well on screen is **still unverified**.

**Needs asset artist:** painted per-biome backgrounds, ground textures, element hit sprites, monster
attack-frame sheets.
**Needs animator:** per-monster idle/attack/hurt/KO frame animation (the deform layer only
*disguises* the single frame in motion).

## Phase 6 — Final question

**"Would I play 50 battles in a row?"**

**I cannot answer this honestly — I have not been able to watch the current build play.** The brief
says not to stop until the answer is YES; I can't reach a truthful YES or NO without eyes on it, and
faking one would defeat the purpose. Two things I *am* sure block a genuine YES regardless of tuning:
**single-frame sprites** and **one-photo backdrops** — both are asset-production problems, not code.

## The real unblock

The loop that has actually worked this session is **you as the eyes**: you said "monsters line up
then snap back", I fixed it same-session and it's tested. To finish this phase truthfully I need real
frames of the current build. Either:
- **Hands-off ~2 min:** unlock the phone, don't touch it, say "go" — I'll drive BRAWL + TAG and
  capture the real beats, then polish against them; or
- **You look** and tell me the specific ugly moments; I fix them fast and verified.

Install the new APK (`Build/Android/TrainYourMonster.apk`) to check the two fixes: play a **water**
battle (should no longer look like a blue forest) and **lose** one (headline should read "DEFEAT /
Crushed…", not "Clutch Victory").
