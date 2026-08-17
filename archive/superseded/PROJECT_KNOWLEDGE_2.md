# PROJECT_KNOWLEDGE.md — Train Your Monster (Monster Trainer Arena)

**Primary source of truth.** Consolidated from all design-session documents
(GDD v1.0, game-spec v0.5, code-conventions, Phase 1 battle-prototype spec,
Phase 1 scripts drop, and evals test-1/3/4/5/6). Where a downstream doc left a
number TBD and a later doc set it, the later decision is recorded here.

> Working title note: the repository is **Train Your Monster**; the design
> documents use the internal name **Monster Trainer Arena (MTA)**. Same project.

---

## 1. Core Vision

A mobile-first **monster training + auto-battler + collection RPG**, released
commercially on the **Android Play Store** by **one solo developer** with a
minimal budget, within **3–6 months**.

- Audience: casual to midcore mobile players.
- Philosophy: small scope · strong core loop · ship early · expand later.
- Priority order when goals conflict: (1) shipping, (2) maintainability,
  (3) expansion potential.
- Guiding trio: **Fun > Features · Retention > Complexity · Shipping > Perfection.**
- Emotional references: Monster Rancher, Digimon World, Tamagotchi. Explicitly
  **NOT** Pokémon, Genshin-style progression, MMORPG, or open-world RPG.

## 2. Core Fantasy

**"I raised this monster."** Never "I collected another monster." The player
remembers *"my Wolf,"* not *"Wolf #32."*

Why it works (mechanical, not cosmetic):

- **Individuality is mechanical.** Hidden per-instance growth grades (S–D per
  stat) mean your Wolf and someone else's Wolf grow up different.
- **Discovery creates ownership.** Grades are hidden; training reveals them.
  Learning "my Turtle is secretly fast" is a story the player earned.
- **Preparation is the gameplay.** Auto-battle means every win is attributable
  to raising decisions — "I won because I prepared correctly."
- **Nicknames seal it.** You name what you capture.

## 3. The Four Pillars

Every feature must strengthen at least one:

1. **Raising monsters**
2. **Visible growth** — progress must be obvious
3. **Team building**
4. **Long-term goals** — always a next monster, league, milestone, unlock

## 4. Core Loop

**Acquire monster → train → battle → gain rewards → improve monster → unlock
new challenge → repeat.** Every system must support this loop.

- First launch (0–5 min): pick 1 of 3 starters, nickname, one scripted-win
  battle, first training session showing a stat gain (and possibly the first
  "exceptional gain!" moment).
- Session loop (5–15 min): check finished training → allocate stat points →
  glance at next-opponent preview → start a training session → fight 1–3
  career battles → spend coins on the next session.
- Ladder loop (days): 8 rungs per league; rungs 3 & 6 are scout (capture)
  battles; champions gate the next league; Silver+ gates also demand roster depth.
- Late game (weeks): Master champion rewards the 12th monster (Dragonling);
  post-ladder play is mastery grades + grade-hunting.

## 5. MVP Scope

### Allowed (the game does not ship without these)

Auto battle · training · monster collection · **capture (Scouting)** · stat
growth · leveling · save system · career mode.

### MVP Required feature list

- 3v3 auto-battle (headless deterministic sim + replay view)
- Training system (timer-based, 4 types, freshness + grade discovery)
- Leveling + stat allocation (3 points/level)
- Hidden growth grades (per-instance rolls)
- Capture via scout battles
- Career mode: 5 leagues × 8 rungs + champions, promotion gates
- Save/load (versioned SaveGame, atomic writes)
- One currency (Coins) with one sink (training fees)
- Portrait UI shell: team, monster detail, training, league map, battle
- Android build + Play Store listing

### Nice-to-have (only if Phases 3–4 run ahead)

League mastery grades (flawless/swift) · richer opponent preview · battle
replay from saved seed · second save slot. Each is a fast-follow patch otherwise.

### Success criteria (MVP)

Prototype in 30 days · core loop works · training works · leveling works · save
works · 12 monsters exist · 3v3 auto-battle works · Android build works · Play
Store upload possible. **Shipping > Polish.**

## 6. Approved Systems (summary — full detail in later sections)

| System | Status | One-line |
|---|---|---|
| Data layer (SO + registry + balance.json) | Approved, MVP Safe | Content is data; new monster/skill = data only, no code |
| Headless deterministic battle sim | Approved, MVP Safe | The make-or-break system; view replays its event log |
| Progression (XP/levels/points/grades) | Approved, MVP Safe | 3 points/level; growth grades route stat gains |
| Training (timers, freshness, discovery, fees) | Approved, MVP Safe | Formula + copy; grade discovery is the core hook |
| Capture ("Scouting") | Approved, MVP Safe | Win scout battle → pick 1 enemy to recruit at level 1 |
| Career (45-battle table, gates, rewards) | Approved, MVP Safe | Data table + gate checks |
| Save/load (versioned, atomic) | Approved, MVP Safe | ids-only JSON, temp-file atomic replace |
| Economy (Coins, one sink) | Approved, MVP Safe | Battle output funds training input |
| UI shell (5 portrait screens) | Approved, MVP Safe | Biggest pure-labor item |
| Retention (camp appointment + gates + grade curiosity) | Approved, MVP Safe | No missions/login/gacha |

## 7. Rejected / Excluded Systems

### Forbidden in MVP (reject unless explicitly approved)

Multiplayer · PvP ranking · open world · farming · crafting · guilds ·
breeding · equipment · gacha · battle pass · daily missions · social features ·
live-service systems.

> Note: daily *return behavior* is a goal; a daily-*missions feature* is
> forbidden. Return appeal comes from training timers + league progression.

### Rejected in Phase 1 design (with reason)

- **Passive skills** — would add a 4th skill slot and new scope. Locked model
  is exactly 3 (Basic/Active/Ultimate). Rejected.
- **Miss chance / accuracy stat** — pure variance; undermines "I prepared
  correctly." Not a stat in the spec; do not add.
- **Positional formation grid** — slot order is enough. Removed entirely.
- **Second currency / cloud save / achievements / landscape / localization
  beyond English / iOS** — removed entirely for MVP.

### Expansion waves (post-launch — design seams only, do NOT build)

- **Wave 1:** capture expansion (rates, tools, rare variants), traits, evolutions
- **Wave 2:** equipment, trading, local PvP
- **Wave 3:** open world, life simulator, farming, exploration

## 8. Architecture Decisions

- Engine **Unity (C#)**, Android-first, **portrait** orientation.
- **Data-driven:** ScriptableObjects for definitions, `balance.json` for tunables.
  Adding a monster or skill requires **data only — no code changes.** No
  hardcoded monsters, skills, or stat tables.
- **Layer separation (strict):**
  - `Core/` — plain C#, zero scene/MonoBehaviour deps: computes, never renders.
  - `Battle/` — MonoBehaviours: renders (replays event log), never computes.
  - `Data/` — ScriptableObjects: declares, never behaves.
  - `Editor/` — verifies/generates, never ships.
- **Headless simulator** is the balance engine: `BattleSimulator.Run(TeamConfig
  a, TeamConfig b, int seed, BalanceConfig cfg, SpeciesRegistry reg)
  → BattleResult`. Deterministic given a seed; emits an ordered event log that
  is the *only* contract between sim and view. Log hash is the determinism test.
- **Core purity via POCOs:** sim consumes `SpeciesData`/`SkillData` plain
  classes; SOs convert via `ToData()`. Keeps `Core/` asset-free so sweeps/tests
  run headless. (Implemented; deviation from the spec's original API sketch,
  which omitted the registry parameter.)
- **RNG contract (documented, determinism-critical):** one `System.Random(seed)`
  stream consumed in fixed order — growth rolls (team A then B, slot order, stat
  order) → crit rolls in resolution order → hard-resolve coin flip. Changing the
  order breaks the determinism hash by design.
- **Save system:** one flat `SaveGame` root, `JsonUtility` →
  `Application.persistentDataPath/save.json`, temp-file-then-atomic-replace.
  Store ids only (`speciesId`/`skillId`), never asset refs. Bump `saveVersion`
  and default new fields for old saves. speciesIds are **append-only** (never
  rename/remove). Save on pause/quit and after every battle, training completion,
  stat allocation. (MVP Build Phase 2 deliverable — not yet implemented.)
- **Folder layout:**
  ```
  Assets/
    Scripts/{Core, Data, Battle, Meta, Editor, Tests}
    GameData/{Monsters, Skills}      // .asset per species/skill
    Resources/{Monsters, Skills}     // generator output (Resources.LoadAll path)
    StreamingAssets/balance.json
  ```
- **Scale path (deferred, not MVP):** swap `Resources.LoadAll` for Addressables
  behind the same `SpeciesRegistry` interface when asset count/memory demands.
- **Mobile constraints:** no per-frame allocations in battle code; sanity-check
  layouts at 1080×2340 and 720×1520; low-end Android target.

## 9. Canonical Data Model (from code-conventions)

```csharp
public enum Stat { HP, ATK, DEF, SPD, INT, LUCK }
public enum GrowthTier { D, C, B, A, S }      // ordered worst→best (comparisons work)
public enum SkillSlot { Basic, Active, Ultimate }

struct StatBlock { int hp, atk, def, spd, intel, luck; int Get(Stat s); }  // "intel" — int reserved

MonsterSpecies (SO): speciesId, displayName, baseStats:StatBlock,
    growth:GrowthProfile, basicSkill/activeSkill/ultimateSkill, portrait, battleSprite
MonsterInstance (plain C#): instanceId(GUID), speciesId, nickname, level, exp,
    growth:GrowthTier[6], allocated:StatBlock, trained:StatBlock, unspentPoints
SkillDefinition (SO): skillId, displayName, slot, scalingStat(ATK|INT),
    powerMultiplier, cooldownSeconds/chargeTime, effect(Damage|Heal|Buff|Debuff),
    magnitude, duration, targetRule
SaveGame: saveVersion=1, monsters:List<MonsterInstance>, activeTeam:string[3],
    highestLeagueCleared, softCurrency, lastSaveUtc
```

**Stat formula (single source of truth — `Core/StatMath.cs`, nothing else
computes stats):**
```
effective(stat) = base(stat) + levelGain(stat)*(level-1) + allocated(stat) + trained(stat)
levelGain(stat) = round(speciesGainRate[stat] * tierMultiplier[growthTier(stat)])
```

## 10. Progression Decisions

- **Stats — exactly 6:** HP, ATK, DEF, SPD (attack speed + turn priority), INT
  (skill power), LUCK (crit chance + future drops). No additions without strong
  justification.
- **XP:** from career battles; losses grant ~40% of a win (a bad night still
  moves you). MVP level cap **30**.
- **Levels:** each grants **3 stat points** (banked until spent) plus automatic
  growth gains routed through hidden grades.
- **Growth grades:** S/A/B/C/D, one per stat, rolled per instance at
  acquisition from species weight profiles; hidden in UI; surfaced through
  training numbers and level-up gains. `tierMultiplier`: D 0.6, C 0.8, B 1.0,
  A 1.25, S 1.5.
- **Builds** (emergent from allocation + training, no separate system): Tank,
  Bruiser, Assassin, Mage, Support.
- **Unlock pacing anchors (tune via sim):** Bronze ≈ lvl 1–8, Silver 8–14, Gold
  14–20, Platinum 20–26, Master 26–30. Every 2–3 sessions something permanent
  happens (level, grade reveal, recruit, or promotion).

## 11. Capture Decisions ("Scouting")

The smallest capture that still feels like raising:

1. Two rungs per league (**3 and 6**) are scout battles, badged on the map.
2. Win → choose **one** monster from the defeated enemy team to recruit. Player
   choice, no RNG, no items, no capture currency.
3. Recruit joins at **level 1 with freshly rolled growth grades** + immediate
   nickname prompt.

- 10 recruits total across the ladder; starter + 10 + Dragonling = the full 12.
- Data seam: career config carries a `scoutPool` per rung — Wave 1 extends rates/
  tools/variants without touching code.
- **Scope: MVP Safe.** (This is the "capture (basic)" the spec left TBD; this
  "Scouting" design is the accepted resolution.)

## 12. Training Decisions

Timer-based, no minigames, no new currencies, no new assets. Four training
types map to stats: **Strength→ATK · Endurance→HP · Agility→SPD ·
Intelligence→INT.**

Three data-only mechanisms turn "tap the biggest number" into a real decision
(priority order):

1. **Grade discovery (core, ship first).** Yields route through hidden grades
   (`gain = baseYield × tierMultiplier[grade]`). Show the concrete number,
   celebrate outliers ("+4 ATK — exceptional!"). Every session is an experiment
   on *this* monster.
2. **Freshness rotation.** Repeating one type decays its yield (100 → 85 → 70%,
   floor 60%; recovers while training other stats). Gentle — whispers "vary it."
3. **Train-or-fight tension.** A monster in training can't be fielded until the
   timer ends (cancel = no gain).

- **Session tiers** (values in `balance.json`): Quick 15 min · Standard 2 h ·
  **Overnight Camp 8 h** (best total yield; the retention appointment).
- Sessions cost Coins (the economy's only sink).
- Training log on the monster detail screen is the "raising receipt."
- Simplest shippable version = mechanism 1 alone; add 2 & 3 only if playtests
  show the loop is still flat.

## 13. Battle Decisions

**Model stated before numbers (balancing rule). Constants in `balance.json`.**

- **Damage:** `hit = ATK × (1 − DEF/(DEF+K))`, K = 50. Skills scale INT at
  2.5–4× on 6–15 s cooldowns.
- **Mitigation:** `mitigation(DEF) = 1 − DEF/(DEF+50)` — DEF appears ONLY here.
- **Crit:** chance = `min(LUCK × 0.005, 0.30)`; multiplier ×1.5. Bounded
  variance so preparation, not dice, decides.
- **Attack speed:** `aps(SPD) = 0.02 × min(SPD,25) + 0.01 × max(0, SPD−25)`,
  hard cap 1.0. The kink at 25 is the locked SPD-stacking brake. Interval = 1/aps.
- **Ultimates:** charge once per battle (available when clock ≥ chargeTime,
  default 15 s), 3.5–4× budget, no single hit > ~45% of an average same-level HP
  pool (enforced by a data-validation test).
- **Heal:** `round(INT × powerMultiplier)`, capped at maxHP, no mitigation.
- **Damage floor:** `max(1, ...)`. **Miss chance: none.**

System:

- **Team size:** 3v3 (sim supports 1v1/2v2/3v3 + asymmetric for debug). Career
  uses a locked 1v1 → 2v2 → 3v3 Bronze ramp, then 3v3.
- **Formation = slot order** (slots 1–3, slot 1 = front). No positional grid.
- **Turn order:** continuous SPD-driven action timeline, no rounds. Next actor =
  lowest `nextActionTime`; ties break by higher SPD → team A before B → lower
  slot. Total ordering is mandatory or determinism dies.
- **Targeting (deterministic):** basic → front-most living enemy; damage skills →
  lowest-HP enemy (tie: lower slot); heals → lowest-HP% living non-full ally;
  buffs → self unless data says ally; debuffs → as damage skills.
- **AI priority (first valid wins):** Ultimate if charged → Active if cooldown
  ≤ 0 and valid target → Basic attack. No randomness in AI.
- **Victory:** eliminate enemy team. **Anti-stall:** `stallMult(t) = 1 + 0.05 ×
  floor((t−75)/10)` for t > 75. **Hard resolve at 120 s:** higher Σ(currentHP/
  maxHP) wins → tie: more living units → tie: seed-derived coin flip.
  `endReason` records HardResolve.
- **Duration target:** 30–90 s (P10/P90 of the sweep). Under 15 s flagged as
  burst anomaly. 120 s absolute max.
- **Expected TTK at parity:** squishies ~6–10 s focused, tanks ~15–25 s;
  typical battle ~35–75 s.

## 14. Skills (Phase 1 shared pool — 10 assets)

`SkillDefinition` resolver switches on `effect` (Damage/Heal/Buff/Debuff) — four
cases, no per-skill code. Each monster has exactly 3 (Basic/Active/Ultimate);
per-species signature skills are a Phase 3 content-authoring job. Buff/Debuff =
one timed modifier `{stat, ±percent, expiresAt}` applied to effective stats at
read time.

| Slot | Skills |
|---|---|
| Basic (mult 1.0, no cd) | `strike` (ATK), `zap` (INT) |
| Active (2.5–3.0×, 6–10 s cd) | `power_strike` (ATK dmg), `spark_burst` (INT dmg), `mend` (heal 2.5×), `war_cry` (buff +20% ATK 8 s), `slow_hex` (debuff −20% SPD 8 s) |
| Ultimate (3.5–4.0×, charge 15 s) | `savage_rend` (ATK), `mind_blast` (INT), `rally` (team buff +15% ATK/SPD 10 s) |

> `TargetRule.AllAllies` was added for `rally` (one loop in the resolver, no
> per-skill code) — deliberate, flagged deviation.

## 15. Monster Roster (12 — provisional species)

**Roster flexibility rule:** locked by **role, silhouette, and growth
identity**, not species. Names adapt to the purchased asset pack at Build Phase
4. Growth profiles are species *tendencies* — every instance rolls its own
grades. Starters: **Slime / Wolf / Fire Lizard** (sturdy / physical / caster).

Canonical numbering follows the GDD/game-spec:

| # | Name (provisional) | Role | Growth tendency HP/ATK/DEF/SPD/INT/LK | Training pref. |
|---|---|---|---|---|
| 1 | Slime | Beginner tank | A/C/B/D/C/B | Endurance |
| 2 | Wolf | Bruiser | B/A/C/B/D/C | Strength |
| 3 | Fire Lizard | Hybrid caster | C/B/C/C/A/C | Intelligence |
| 4 | Bat | Speedster | D/B/D/S/C/B | Agility |
| 5 | Mushroom Beast | Support | B/D/B/D/A/C | Intelligence |
| 6 | Spider | Assassin | D/B/D/A/B/B | Agility |
| 7 | Turtle | Hard tank | A/D/S/D/C/D | Endurance |
| 8 | Goblin | Lucky bruiser | C/B/C/B/D/A | Strength |
| 9 | Ghost | Mage | C/D/C/B/S/B | Intelligence |
| 10 | Bee | Fast support | D/C/D/S/B/B | Agility |
| 11 | Golem | HP tank/bruiser | S/B/A/D/D/D | Endurance |
| 12 | Dragonling | Late bloomer | B/A/B/B/A/B | Player's choice |

> **Discrepancy noted:** the balance sheet (test-3) lists the same 12 monsters
> in a different display order (Bat #3, Fire Lizard #11). Species, roles, and
> growth tendencies are identical; only the row order differs. Use the GDD
> numbering above as canonical.

### Level-1 base stats (v0 of balance.json, from the balance sheet)

| Monster | HP | ATK | DEF | SPD | INT | LUCK |
|---|---|---|---|---|---|---|
| Slime | 120 | 16 | 18 | 8 | 10 | 6 |
| Wolf | 100 | 24 | 12 | 14 | 6 | 8 |
| Bat | 70 | 18 | 8 | 20 | 10 | 12 |
| Mushroom Beast | 110 | 12 | 14 | 7 | 20 | 6 |
| Spider | 75 | 20 | 9 | 17 | 14 | 10 |
| Goblin | 95 | 21 | 12 | 13 | 9 | 14 |
| Turtle | 150 | 12 | 26 | 5 | 8 | 4 |
| Ghost | 80 | 8 | 10 | 12 | 24 | 10 |
| Bee | 65 | 16 | 7 | 22 | 12 | 9 |
| Golem | 140 | 20 | 22 | 4 | 6 | 3 |
| Fire Lizard | 90 | 18 | 11 | 11 | 18 | 7 |
| Dragonling | 85 | 17 | 12 | 10 | 16 | 8 |

Every role archetype (Tank/Bruiser/Assassin/Mage/Support) has ≥ 2 expressions,
so no capture is a dead end.

## 16. Career Mode Decisions

- **Structure:** 5 leagues (Bronze, Silver, Gold, Platinum, Master) × 8 rungs +
  1 champion = **45 AI battles**. Opponent teams are data (`careerConfig`):
  species, levels, builds per rung.
- **Scout battles:** rungs 3 and 6 of each league.
- **Promotion gates:** beat champion to unlock next league; Silver+ also demands
  roster depth — Silver: 3 monsters ≥ lvl 8; Gold: 4 ≥ 14; Platinum: 5 ≥ 20;
  Master: 6 ≥ 24 (values in balance.json).
- **Rewards:** Coins per rung (first-clear bonus), a recruit per scout win, the
  Dragonling from the Master champion.
- **Replay:** rungs replayable for reduced Coins. **Mastery grades** (flawless =
  no faints, swift = under 45 s) per rung = 10 medium-term goals per league —
  Nice-to-have / first patch.

## 17. Retention Decisions

No daily missions, login rewards, gacha, PvP, or multiplayer. Return behavior
comes from the raising fantasy:

1. **Overnight camp appointment** — ending a session by starting an 8 h camp
   means the player scheduled tomorrow's reason to return.
2. **Promotion gates as raising goals** — the Bronze-5 wall becomes a visible
   ramp; the Silver gate (visible from Bronze 3) demands a deeper roster before
   the player runs out of rungs. (Directly fixes the diagnosed "Bronze wall"
   churn: clearing Bronze breaks Pillar 4 "next battle ran out" and stalls
   Pillar 1; the gate makes the existing loop pose a new demand.)
3. **Grade curiosity** — every recruit is a mystery box opened by training.
4. **Always a next thing on screen** — next rung, scout badge, gate
   requirement, eventually the Dragonling.

## 18. Economy Decisions

One currency, one sink, no inflation possible.

- **Coins** — earned from rung victories (first-clear bonus, reduced replay) and
  champion bonuses. Spent only on training-session fees (Quick < Standard <
  Overnight).
- Tuning rule: an average session's winnings fund ~1.5 sessions of training —
  never coin-starved, but overnight camps stay a real choice.
- XP and stat points are progression, not currency; capture costs nothing but
  victory.

## 19. Asset Decisions

Art style **2D chibi fantasy**. Asset-constrained: adapt design to available
assets; never require custom art for MVP. Priority: (1) free, (2) commercial
license, (3) chibi fantasy, (4) easy Unity integration. **Prototype asset rule:
placeholders during Build Phases 1–3; purchases belong in Build Phase 4.**

Researched cohesive stack (iteration 1):

| Slot | Pick | Cost | License note |
|---|---|---|---|
| Monsters (prototype) | CraftPix Free Golem Chibi (3 animated) | $0 | free |
| Monsters (release) | 2× huberthart "Cute & Chibi" packs (14 monsters, prefabs+SFX) — fallback: 2–3 CraftPix chibi packs | ~$30 | **license UNVERIFIED — confirm with author before purchase** |
| UI | Kenney UI Pack + RPG Expansion + Fantasy UI Borders | $0 | CC0 |
| VFX | Cartoon FX Remaster Free (Jean Moreno) | $0 | Asset Store EULA |
| Audio | RPG Essentials SFX Free + Minifantasy Dungeon Audio (Leohpaz) | $0 | free, no redistribution |

- **Total: $0 prototype phase; ~$30 (or ~$15 if roster shrinks to one 7-pack +
  golems) at the asset pass.** Integration ≈ 1.5–2 solo-dev weeks.
- No pack maps 1:1 to the 12 species → adapt the roster to the chosen pack per
  the roster-flexibility rule; amend game-spec roster after purchase.
- Missing "hit" animation solved the standard way: white-flash + micro-knockback
  tween (code, free).

## 20. Expansion Waves (seams only — do NOT build in MVP)

- **Wave 1:** capture expansion (rates, tools, rare variants), traits, evolutions.
  Seam: `scoutPool` data per rung already exists.
- **Wave 2:** equipment, trading, local PvP.
- **Wave 3:** open world, life simulator, farming, exploration.

## 21. Governing Rules (carry into every future session)

- **Fun check** (gate for every feature): Will players notice? Will they care?
  Will it create memorable moments? If no → Delay or Reject.
- **Two-week rule:** a feature needing > 2 weeks of solo work defaults to Delay/
  Reject. Burden of proof is on the feature.
- **Content minimization:** 12 meaningful monsters over 100 shallow ones;
  reusable systems over content volume.
- **Balancing rule:** state damage / attack-speed / crit assumptions + target
  duration before any numbers. Always provide expected duration, TTK, win-rate
  distribution, dominant-build risks, scaling risks.
- **Decision persistence:** on any major decision, offer updates to game-spec /
  balance.json / code-conventions. Never silently change specs.
- **Self-check before responding:** (1) increases scope? (2) delays release?
  (3) needs more assets? (4) required for MVP? (5) can it be simplified?
  Recommend the simpler version first.

## 22. Known Risks (carried from the balance sheet)

- **SPD stacking** — action economy is multiplicative; braked by the aps kink at
  SPD 25 (data-only).
- **Stall comps** (double tank + healer) — braked by anti-stall ramp + 120 s
  hard resolve.
- **LUCK crit variance** — bounded by 30% cap / ×1.5 mult.
- **Ultimate snowball** — single hit capped at ~45% of an average HP pool.
- **Scaling:** flat K=50 makes DEF fall off as ATK grows (revisit K per league,
  data-only); growth gains can dwarf allocation at high levels (consider
  allocation scaling); pick one INT scaling channel to avoid double-dip; league
  curves must slightly outpace training gains.

---

*Provenance: GDD v1.0, game-spec v0.5, code-conventions, Phase 1 spec v1.0,
Phase 1 scripts drop + README, evals test-1/3/4/5/6. Originals in `/archive`.*
