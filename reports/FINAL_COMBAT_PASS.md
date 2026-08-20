# FINAL COMBAT PASS

Combat presentation only. No gameplay/balance/progression/save change.

## Honesty first

This phase asks me to **watch** 6 mode/size combos and **score** readability/impact/clarity/
excitement/camera/VFX/pacing. I cannot watch the running build:
- Desktop capture is impossible here (no interactive display — a standalone builds + launches with
  no window).
- The phone is your **personal, in-use** device; every capture attempt this session landed on your
  home screen / TikTok / lock screen. I will not keep poking it, and I will not invent scores for
  battles I haven't seen. Fabricated 1–10 scores would be lying.

So: I made the **one** readability change I'm confident about and can't get wrong, and I answer the
final question straight.

## Fix this pass — near-death danger pulse (serves the explicit target)

Target: *"player must instantly know who is close to dying."* Added a **danger pulse**: any unit
below 25% HP now throbs its HP bar (bright-red flash + bar-group alpha pulse). Guarded so dead and
benched-reserve units are unaffected. This is the standard "about-to-die" read used by Summoners
War / Epic Seven. Additive, no regression risk. **79/79 EditMode, PlayMode 1/1, APK rebuilt.**

Combined with what already exists, the 3-second reads are now wired:
- **Who is winning** → team pips (3 vs 3, dim as units die) at the top.
- **Who is close to dying** → per-unit HP bar colour + **new danger pulse**.
- **Who is being attacked** → target silhouette flashes white + squashes + knocks.
- **Who is attacking** → attacker lunges out of its line with anticipation/overshoot.

Whether these actually *read within 3 seconds on screen* is exactly the eyes-on judgement I can't
make. The wiring is there; the tuning needs a look.

## Per-mode review

| Mode | Score | Note |
|---|---|---|
| BRAWL 1v1 | **[CANNOT ASSESS by eye]** | Code: two fighters, spread anchors, calm cam. Should be the cleanest read. |
| BRAWL 2v2 | [CANNOT ASSESS] | Code: 2 lanes per side. |
| BRAWL 3v3 | [CANNOT ASSESS] | The hard case. Center-pile cause removed in code (anchors chase own opponent, clamped to own half); *unverified*. Last time I **saw** 3v3 (old build) it piled + had idle loiterers. |
| TAG 1v1 | [CANNOT ASSESS] | Same as brawl 1v1 with reserves absent. |
| TAG 2v2 / 3v3 | [CANNOT ASSESS] | Code + `TagModeTests`: only the front fights + is targeted; reserves benched at flanks. Logic proven; look unseen. |

I am not going to colour these boxes with numbers I can't defend.

## Final question — honest

**"Would this combat survive comparison against Monster Legends / Summoners War / Epic Seven / Raid
Shadow Legends?"**

**No. Not close, and no amount of the code-level polish I can do will change that** — because the
gap is **assets**, not presentation code:

1. Those games use **fully frame-animated monster sprites** (idle/attack/cast/hurt/death, often 60+
   frames each). Train Your Monster uses **single-frame** sprites moved by a transform/deform layer.
   In motion the deform disguises it; paused, it's obviously one static image. This alone loses the
   comparison instantly.
2. They have **hand-painted, layered, parallaxed battle backgrounds** per environment. TYM has one
   reused forest photo (now Nature-only) + procedural shapes for Fire/Water. Competent, not painted.
3. Their skill VFX are **authored spritesheet effects** with real timing, secondary motion, and
   screen-space polish. TYM's are procedural silhouettes + CC0 hit sheets — readable and shaped now,
   but clearly a tier below authored VFX.

What TYM *does* have that's genuinely respectable: deterministic tested core, real dynamic audio,
shaped element VFX, a real tag mode, 60 FPS, clean meta UI, and the game-feel layer (anticipation/
overshoot/weight/hit-stop). That's a solid **prototype-plus**, not a store-competitive combat scene.

**Verdict: keep polishing will not get there from code.** The two things standing between this and a
Monster-Legends-tier look are an **animator** (frame animation) and an **asset/VFX artist** (painted
backdrops + authored effects). I'm saying that plainly because the brief said answer honestly.

## To actually close the readability loop (what I need)

The one loop that works is you as the eyes. Either **"go, hands off"** (unlock phone, don't touch
~2 min, I drive BRAWL + TAG and capture real frames, then tune against them), or you glance and name
the specific ugly moments and I fix them fast + verified. Without eyes on the current build I've
taken the code-level readability as far as I responsibly can blind.
