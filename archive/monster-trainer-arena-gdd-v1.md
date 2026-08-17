# Monster Trainer Arena — MVP Game Design Document v1.0

**Active Role:** Both — Creative Director owns every design verdict below; Lead
Architect verified each system against `code-conventions.md` and the two-week
rule. Compliant with `game-spec.md` v0.5 and SKILL.md v5. Where this GDD makes
a decision the spec left TBD (capture, economy, career numbers), the Decision
Persistence rule applies: fold accepted decisions into `game-spec.md` v0.6.

---

# Executive Summary

Monster Trainer Arena is a portrait-mode Android game where you raise a small
team of chibi monsters and prove your training in 30–90 second 3v3 auto-battles.
You never control monsters in combat — you win *before* the fight, by choosing
what to train, where to spend stat points, and who to bring.

Every monster is an individual. Each one rolls hidden growth grades (S–D per
stat) at acquisition, so your Wolf and someone else's Wolf grow up different.
Training is how you find out who your monster really is: an exceptional stat
gain is the game whispering "this one's special." The loop is Monster Rancher's
heart at Tamagotchi's pace: acquire → train → battle → improve → unlock →
repeat, in 5–15 minute sessions.

Content is deliberately tiny and deep: 12 monsters, 6 stats, 3 skills each,
5 leagues, one currency. Everything is data-driven (ScriptableObjects +
`balance.json`), so a solo developer can build it in 3–6 months and expand it
for years. The MVP ships when a player can pick a starter, raise it, capture
teammates on the way up the Bronze→Master ladder, and feel — provably, in the
win column — that *their* choices made *their* monsters strong.

Scope summary: 8 systems, all classified MVP Safe below; estimated 15–20
solo-dev weeks including the asset pass and release ops. Shipping > Polish.

---

# Core Player Fantasy

**"I raised this monster."** Never "I collected another monster."

Why players care:

- **Individuality is mechanical, not cosmetic.** Hidden growth grades make each
  instance genuinely different. Your Wolf's S-grade ATK isn't lore — it's why
  you beat Gold League.
- **Discovery creates ownership.** Grades are hidden; training reveals them.
  Learning "my Turtle is secretly fast" is a story the player tells, and it
  happened because *they* trained SPD on a hunch.
- **Preparation is the gameplay.** Auto-battle means every win is attributable
  to raising decisions. "I won because I prepared correctly" — the fantasy is
  validated by the core mechanic, not narrated at the player.
- **Nicknames seal it.** You name what you capture. From that moment it's "my
  Ember," not "Fire Lizard #2."

Emotional reference points: Monster Rancher, Digimon World, Tamagotchi — not
Pokémon, not MMORPGs.

---

# Core Gameplay Loop

**First launch (minutes 0–5):** Pick 1 of 3 starters (Slime / Wolf / Fire
Lizard — sturdy, physical, caster). Nickname it. One guided battle (a scripted
win) shows the auto-battle. First training session immediately after shows a
stat gain — and if the roll is lucky, the first "exceptional gain!" moment.

**Session loop (5–15 minutes):** Check finished training → allocate any stat
points → glance at the next opponent preview → start a training session (short
now, or overnight camp before quitting) → fight 1–3 career battles → spend
coins on the next session.

**Ladder loop (days):** Climb 8 rungs per league. Rungs 3 and 6 are **scout
battles** — win them to recruit a new monster. League champions gate the next
league, and from Silver onward the gate demands roster depth, so "I finished a
league" always converts into "time to raise someone new."

**Late game (weeks):** Master League's champion rewards the 12th monster —
Dragonling, a slow-starting, high-ceiling raising project. Post-ladder play is
mastery grades (flawless / swift clears per league) and grade-hunting: raising
new captures to find the S-grade individuals. Expansion Wave 1 (capture
expansion, traits, evolutions) plugs into this exact loop.

---

# MVP Feature List

**Required (the game does not ship without these):**

- 3v3 auto-battle (headless deterministic sim + replay view)
- Training system (timer-based, 4 types, freshness + grade discovery)
- Leveling + stat allocation (3 points/level)
- Hidden growth grades (per-instance rolls)
- Capture via scout battles (below)
- Career mode: 5 leagues × 8 rungs + champions, promotion gates
- Save/load (versioned SaveGame, atomic writes)
- One currency (Coins) with one sink (training fees)
- Portrait UI shell: team, monster detail, training, league map, battle
- Android build + Play Store listing

**Nice To Have (build only if Phase 3–4 run ahead of schedule):**

- League mastery grades (flawless/swift) — first fast-follow patch otherwise
- Opponent preview detail beyond "fast team / hits with skills"
- Battle replay from saved seed (sim already supports it)
- Second save slot

**Post Launch (forbidden or waved — design seams only):**

- Everything on the forbidden list (equipment, PvP, daily missions, etc.)
- Wave 1: capture expansion (rates, tools, rare variants), traits, evolutions
- Wave 2: equipment, trading, local PvP · Wave 3: open world, life sim

---

# Monster Roster

Roster flexibility rule applies: these 12 are locked by **role, silhouette, and
growth identity**, not species. Names adapt to the purchased pack at Build
Phase 4 (candidates researched: huberthart Cute & Chibi packs — license
unverified, confirm before purchase — or CraftPix chibi monster packs;
prototype uses the free CraftPix golems recolored). Growth profiles are species
*tendencies* — every instance rolls its own grades. "Training preference" is
descriptive (the training that best expresses the tendency), not a mechanic.

| # | Name (provisional) | Role | Visual archetype (pack-substitutable) | Growth tendency HP/ATK/DEF/SPD/INT/LK | Training pref. | Why it exists |
|---|---|---|---|---|---|---|
| 1 | Slime | Beginner tank | Round blob | A/C/B/D/C/B | Endurance | Forgiving starter; teaches HP matters |
| 2 | Wolf | Bruiser | Quadruped beast | B/A/C/B/D/C | Strength | The "raise my ace" starter; clean ATK fantasy |
| 3 | Fire Lizard | Hybrid caster | Small reptile | C/B/C/C/A/C | Intelligence | Starter that teaches INT/skill power |
| 4 | Bat | Speedster | Winged critter | D/B/D/S/C/B | Agility | First capture; teaches SPD = action economy |
| 5 | Mushroom Beast | Support | Plant/fungus body | B/D/B/D/A/C | Intelligence | First healer; enables tank comps |
| 6 | Spider | Assassin | Multi-leg crawler | D/B/D/A/B/B | Agility | Glass knife; punishes slow teams |
| 7 | Turtle | Hard tank | Shelled walker | A/D/S/D/C/D | Endurance | DEF-tank; anti-burst answer |
| 8 | Goblin | Lucky bruiser | Small humanoid | C/B/C/B/D/A | Strength | LUCK showcase; crit-gamble builds |
| 9 | Ghost | Mage | Floating spirit | C/D/C/B/S/B | Intelligence | Burst caster; INT ceiling of the roster |
| 10 | Bee | Fast support | Winged insect | D/C/D/S/B/B | Agility | Speed + utility; enables rush comps |
| 11 | Golem | HP tank/bruiser | Rock construct | S/B/A/D/D/D | Endurance | HP-tank (vs Turtle's DEF); free prototype asset |
| 12 | Dragonling | Late bloomer | Small dragon | B/A/B/B/A/B | Player's choice | Master reward; the post-game raising project |

Every role archetype (Tank, Bruiser, Assassin, Mage, Support) has at least two
expressions, so no capture is a dead end and team building (Pillar 3) always
has alternatives.

---

# Capture System — "Scouting"

**The smallest capture that still feels like raising:**

1. Two rungs per league (3 and 6) are **scout battles**, badged on the league
   map so the player anticipates them.
2. Win the scout battle → choose **one** monster from the defeated enemy team
   to recruit. Player choice, no RNG, no items, no capture currency.
3. The recruit joins at **level 1 with freshly rolled growth grades** and an
   immediate nickname prompt.

Why this design:

- **Fun check:** Noticed — a badge, a choice screen, a new face on the bench.
  Cared — captures are the only roster growth and the promotion gates demand
  them. Memorable — the recruit's *first training session* reveals its first
  grade hint; "the Bat I scouted in Bronze turned out S-speed" is a story.
- **Fantasy check:** Joining at level 1 is the whole point. You never capture
  power; you capture *potential*. What it becomes is your work — "I raised
  this monster" is literally enforced.
- **Two-week rule:** one badge, one pick screen (reuses team UI), one nickname
  dialog, instance creation that already exists for the starter, and a
  per-rung `scoutPool` list in career data. ≈ 3–5 solo-dev evenings. Passes.
- **Prepared-correctly check:** capture success is winning the battle — the
  skill you're already building — not a dice roll bolted on.

Seam for Wave 1 (capture expansion): the career config already carries a
`scoutPool` per rung; rates, tools, and rare variants extend that data without
touching code. **Scope: MVP Safe.**

---

# Training System

Timer-based, no minigames, no new currencies, no new assets. Three data-only
mechanisms turn "tap the biggest number" into a real decision:

1. **Grade discovery (core).** Yields already route through hidden grades
   (`gain = baseYield × tierMultiplier[grade]`). Show the concrete number and
   celebrate outliers ("+4 ATK — exceptional!"). Every session is an experiment
   on *this* monster; players train to learn who it is, then build around the
   answer. Grades stay hidden; their *effects* are loudly visible (Pillar 2).
2. **Freshness rotation.** Repeating one training type decays its yield
   (100 → 85 → 70%, floor 60%; recovers while training other stats). Gentle on
   purpose — it whispers "vary it," making rotations-toward-a-build beat greed.
3. **Train-or-fight tension.** A monster in training can't be fielded until the
   timer ends (cancel = no gain). Choosing who improves vs who fights tonight
   is the classic pet-raising decision, free of charge.

Session tiers (values in `balance.json`): Quick 15 min · Standard 2 h ·
**Overnight Camp 8 h** (best total yield, and the retention appointment — see
Retention). Sessions cost Coins (the economy's sink). Opponent previews on the
league map make the choice contextual: facing a rush team, tonight's Endurance
camp beats another Strength rep.

Attachment payoff: the training log lives on the monster's detail screen —
"Ember: +212 ATK trained since Bronze" is the raising receipt.
**Scope: MVP Safe** (formula terms + copy + one availability rule).

---

# Progression System

- **XP:** from career battles; losses grant ~40% of a win (respect the casual
  session — a bad night still moves you). Curve in `balance.json`; MVP level
  cap 30.
- **Levels:** each level grants **3 stat points** (banked until spent) plus
  automatic growth gains: `levelGain(stat) = round(speciesGainRate ×
  tierMultiplier[grade])`, multipliers D 0.6 → S 1.5 per `code-conventions.md`.
- **Stat allocation:** free-form across the 6 stats; the build system (Tank /
  Bruiser / Assassin / Mage / Support) is allocation + training, nothing else.
- **Growth grades:** rolled per instance at acquisition from species weight
  profiles; hidden in UI; surfaced through training and level-up numbers.
- **Unlock pacing (anchors, tune via sim):** Bronze ≈ levels 1–8, Silver 8–14,
  Gold 14–20, Platinum 20–26, Master 26–30. Captures at rungs 3/6 keep a new
  raising project arriving roughly every 4–6 sessions; Dragonling lands as the
  post-ladder project. Every 2–3 sessions something permanent happens: a level,
  a grade reveal, a recruit, or a promotion. **Scope: MVP Safe.**

---

# Battle System

Balance model stated first (per the balancing rule; constants in
`balance.json`):

- **Damage:** `hit = ATK × (1 − DEF/(DEF+50))`; skills use INT with 2.5–4×
  budgets on 6–12 s cooldowns; ultimates charge once per battle, capped at
  ≤45% of an average HP pool (no one-shots).
- **Attack speed:** attacks/sec = `SPD × 0.02`, diminishing above SPD 25 (the
  SPD-stacking brake).
- **Crits:** chance = `LUCK × 0.5%`, capped 30%, ×1.5 damage — preparation
  decides, not dice.
- **Target duration:** 30–90 s.

System:

- **Team size:** 3v3. Pre-battle input only: pick three, order them, confirm
  loadout.
- **Turn order:** continuous SPD-driven action timeline (higher SPD acts more
  often) — no discrete rounds; matches the headless simulator design.
- **Targeting:** slot order is the formation — basic attacks hit the
  *front-most living enemy*; damage skills target lowest-HP enemy; heals target
  lowest-HP ally. Deterministic, readable, and it makes slot order a real
  decision (who tanks) without a positional grid.
- **Victory:** eliminate the enemy team. Anti-stall: +5% global damage every
  10 s after 75 s; at 120 s the team with the higher total HP% wins. (Kills
  the double-tank stall risk from the balance sheet.)
- **Determinism:** seed-driven sim emits an event log; the view replays it.
  Expected TTK at level parity: squishies ~6–10 s focused, tanks ~15–25 s;
  typical battle 35–75 s — verified by the 1,000-battle sim sweep (P10 ≥ 30 s,
  P90 ≤ 90 s) before any league ships. **Scope: MVP Safe.**

---

# Career Mode

- **Structure:** 5 leagues (Bronze, Silver, Gold, Platinum, Master) × 8 rungs +
  a league champion = 45 battles, all AI. Opponent teams are data
  (`careerConfig` in balance.json): species, levels, builds per rung.
- **Scout battles:** rungs 3 and 6 of every league (10 recruits available;
  starter + 10 + Dragonling = the full 12).
- **Progression gates:** beating the champion unlocks the next league, and from
  Silver on the gate also demands depth — Silver: 3 monsters ≥ level 8; Gold:
  4 ≥ 14; Platinum: 5 ≥ 20; Master: 6 ≥ 24 (values in balance.json). Finishing
  a league always converts into a raising goal, which is the anti-churn lever.
- **Rewards:** Coins per rung (first-clear bonus), a recruit at each scout win,
  the Dragonling egg from the Master champion.
- **Replay value:** rungs stay replayable for Coins at reduced yield; mastery
  grades (flawless = no faints, swift = under 45 s) per rung give ten
  medium-term goals per league — Nice To Have in MVP, first patch otherwise.
  **Scope: MVP Safe** (it's a data table + gate checks; grades are the only
  optional part).

---

# Retention Strategy

No daily missions, login rewards, gacha, PvP, or multiplayer. Return behavior
comes from the raising fantasy itself:

1. **The overnight camp appointment.** Ending a session by starting an 8-hour
   camp means *the player* scheduled tomorrow's reason to return — "Turtle's
   endurance camp finishes in the morning." An appointment with your monster,
   not a mission chart.
2. **Promotion gates as raising goals.** The Bronze-wall churn moment ("I
   finished everything") can't occur: the visible Silver gate demands a deeper
   roster before the player runs out of rungs.
3. **Grade curiosity.** Every new recruit is a mystery box the player opens by
   training — the drive to "find out what my Spider is" spans multiple
   sessions by design.
4. **Always a next thing on screen:** next rung, next scout badge, next gate
   requirement, and eventually the Dragonling — Pillar 4 rendered as UI.

---

# Economy

One currency. One sink. No inflation possible.

- **Coins** — earned from: rung victories (first-clear bonus, reduced replay
  yield), league champion bonuses. Spent on: training session fees (Quick <
  Standard < Overnight).
- That's the entire economy: battle output funds raising input, which produces
  battle output. Every reward has a meaningful use (economy rule ✓). XP and
  stat points are progression, not currency; capture costs nothing but victory.
- Tuning rule (balance.json): an average session's winnings fund ~1.5 sessions
  of training, so the player is never coin-starved out of the loop but
  overnight camps stay a real choice. **Scope: MVP Safe.**

---

# MVP Scope Review

| System | Development cost (solo) | Risk | Classification |
|---|---|---|---|
| Data layer (species/skills/registry/balance.json) | ~1 week | Low — pattern proven in conventions | MVP Safe |
| Headless battle sim + duration sweep | ~1.5 weeks | Medium — the make-or-break system; determinism must hold | MVP Safe |
| Battle presentation (replay view, portrait) | ~1.5 weeks | Medium — animation wiring is fiddly | MVP Safe |
| Progression (XP/levels/points/grades) | ~1 week | Low | MVP Safe |
| Training (timers, freshness, discovery, fees) | ~1 week | Low — formulas + copy | MVP Safe |
| Capture (scout badges, pick screen, nickname) | ~0.5 week | Low | MVP Safe |
| Career (45-battle data table, gates, rewards) | ~1 week | Low–Medium — authoring 45 comps takes discipline; the sim auto-tunes difficulty | MVP Safe |
| Save/load (versioned, atomic) | ~0.5 week | Low — pattern in conventions | MVP Safe |
| UI shell (5 screens, portrait, Kenney 9-slice) | ~2 weeks | Medium — biggest pure-labor item | MVP Safe |
| Asset pass (pack purchase, 12 monsters, VFX/SFX hookup) | ~2 weeks | Medium — license verification is the gate; animations included by selection | MVP Safe |
| Release ops (Android build, store listing, QA) | ~1.5 weeks | Medium — first-time Play Store friction | MVP Safe |
| Mastery grades | ~0.5 week | Low | Nice To Have |
| Everything forbidden/waved | — | — | Post Launch |

**Total: ~14–16 working weeks** of core estimate → 3–6 calendar months for one
part-time solo developer with buffer. The 30-day first-playable is the data
layer + sim + a debug battle view: ~4 weeks. Feasible.

---

# Final Recommendation

**1. Build immediately (in this order — each step de-risks the next):**
Data layer → headless sim + duration sweep → debug battle view *(= the 30-day
prototype)* → progression → training core (grade discovery first) → save →
career data + gates → capture → UI shell → asset pass → release ops.

**2. Delay (Nice To Have / fast-follow patch):** mastery grades, battle
replays, rich opponent previews, freshness mechanic *if* playtests show grade
discovery alone already creates the choice.

**3. Remove entirely (not even seams needed):** positional formation grids
(slot order is enough), capture rates/items (Wave 1's problem), multiple save
slots, cloud save, achievements, landscape support, localization beyond
English, iOS, any second currency, and every forbidden-list feature.

This is the smallest game that still delivers "I raised this monster": twelve
individuals, one honest ladder, one currency, and a training system whose whole
job is making the player fall for a specific creature. Optimize the build for
release probability — the sim proves balance, the data layer proves
expandability, and everything else is Wave 1's job.

---

*Decision persistence: on approval, fold into `game-spec.md` v0.6 — capture
("Scouting") design, career structure and gates, economy definition, session
tiers, battle model constants, and the roster's role/growth table.*
