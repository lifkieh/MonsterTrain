# PROJECT_KNOWLEDGE.md — Monster Trainer Arena

**Purpose:** permanent project memory. A new Claude instance (or human) with
zero prior context must be able to continue development from this file alone.
Read it together with the skill's `references/game-spec.md` and
`references/code-conventions.md`. **Where this file and game-spec.md v0.5
disagree, THIS FILE WINS** — it captures GDD v1.0 and the approved pacing
review, which post-date the spec (the v0.6 fold is a pending task, see Risks).

Governance: the project operates under the `monster-trainer-arena` skill
(v5): Creative Director decides before Lead Architect builds; CD is the
default role; the fun check, two-week rule, self-check, and scope
classifications (MVP Safe / MVP Risky / Post Launch) apply to every proposal.

---

# Project Vision

A solo developer ships a commercial monster-training auto-battler on the
Android Play Store within 3–6 months, on a near-zero budget, built in Unity
(C#), portrait orientation, 2D chibi fantasy art from marketplace assets.
Development philosophy: small scope · strong core loop · ship early · expand
later. Priority when goals conflict: shipping > maintainability > expansion
potential. Fun > Features · Retention > Complexity · Shipping > Perfection.
Audience: casual to midcore mobile players.

# Core Fantasy

**"I raised this monster"** — never "I collected another monster." Players
remember "my Wolf," not "Wolf #32." Feel: Monster Rancher, Digimon World,
Tamagotchi — explicitly NOT Pokémon, Genshin, MMORPG, or open world.
Individuality is mechanical: hidden per-instance growth grades make every
monster genuinely different; training is how the player discovers who their
monster is; auto-battle makes every win attributable to preparation ("I won
because I prepared correctly," never "I tapped faster").

Four pillars — every feature must strengthen at least one:
(1) Raising monsters · (2) Visible growth · (3) Team building ·
(4) Long-term goals.

# Core Loop

Acquire monster → train → battle → gain rewards → improve monster (EXP,
levels, stat allocation) → unlock new challenge → repeat. Every system must
support this loop. Sessions of 5 and 15 minutes must satisfy; daily return
comes from the overnight training camp the player chose, never from mission
systems.

# MVP Scope

**Allowed:** auto battle · training · monster collection · capture (basic,
"Scouting") · stat growth · leveling · save system · career mode.

**Success criteria:** prototype within 30 days · core loop works · training
works · leveling works · save works · 12 monsters exist · 3v3 auto battle
works · Android build works · Play Store upload possible. Shipping > Polish.

**Build roadmap:** Phase 1 core battle prototype → Phase 2 progression
(XP/levels/allocation/training/save) → Phase 3 content pass (12 monsters,
per-species skills, career, leagues) → Phase 4 asset pass (purchase + swap
placeholders) → Phase 5 release (build, store, QA, perf). Capture UI lands in
Phase 2–3 (slot formally unassigned; the mechanic is designed, below).

# Forbidden Scope

Reject unless explicitly approved: multiplayer · PvP ranking · open world ·
farming · crafting · guilds · breeding · equipment systems · gacha · battle
pass · daily missions · social features · live-service systems.

Two-week rule: any feature needing >2 weeks additional solo effort defaults
to Delay or Reject; burden of proof is on the feature. Content minimization:
smallest content set that validates gameplay (12 meaningful monsters, never
100 shallow ones). Asset-driven development: never require custom art.

# Approved Decisions

(GDD v1.0 approved; pacing review approved; all locked.)

1. 12-monster roster — binding by role/silhouette, species provisional
   (roster flexibility rule).
2. Capture exists; uses Scout Battles; captured monsters join at **Level 1**;
   growth grades rolled on capture.
3. Bronze onboarding ramps **1v1 → 2v2 → 3v3** across rungs 1–3; Bronze scout
   battles moved to **rungs 1 and 4** (pacing fix); all other leagues scout at
   rungs 3 and 6.
4. Catch-up XP exists: 2–3× XP for monsters below the current league's level
   band. Replay battles grant **full XP, reduced coins**.
5. Career: 5 leagues (Bronze→Master) × 8 rungs + champion = 45 battles, all
   AI. Depth gates (retuned, supersede GDD originals): Silver 3 monsters ≥ 8 ·
   Gold 4 ≥ 12 · Platinum 5 ≥ 17 · Master 6 ≥ 21. Losses grant ~40% XP.
6. Level bands: Bronze 1–8, Silver 8–14, Gold 14–20, Platinum 20–26, Master
   26–30. MVP level cap 30. 3 stat points per level, banked until spent.
7. Economy: ONE currency (Coins). Sources: rung victories (first-clear bonus,
   reduced replay yield), champion bonuses. Sink: training fees. Tuning rule:
   average session winnings fund ~1.5 training sessions.
8. Training sessions: Quick 15 min · Standard 2 h · Overnight Camp 8 h (best
   total yield; the daily-return hook). A monster in training cannot be
   fielded (cancel = no gain).
9. Battle model (constants in balance.json): mitigation 1−DEF/(DEF+50);
   aps = 0.02×SPD to 25, +0.01 beyond, cap 1.0; crit = LUCK×0.5% cap 30%,
   ×1.5; ultimates ≤45% of average HP pool, charge at 15 s (rally 18 s);
   anti-stall +5% damage per 10 s after 75 s; hard resolve at 120 s (higher
   total HP%, then more living units, then seed coin flip). Target 30–90 s.
10. Targeting: basic → front-most living enemy; damage skills → lowest current
    HP enemy; heals → lowest HP% injured ally; ties → lower slot. Slot order
    IS the formation.
11. Starter choice: Slime / Wolf / Fire Lizard. Dragonling is the Master
    champion reward (post-game raising project). 10 recruits via scouts.
12. Phase 1 uses a **10-skill shared pool**; per-species signature skills are
    Phase 3 content. Skills = data assets only (Damage/Heal/Buff/Debuff).
13. Tier multipliers D 0.6 / C 0.8 / B 1.0 / A 1.25 / S 1.5. Growth grades are
    species *tendencies* (weight pyramids); instances roll actual grades.
14. Expected pacing after fixes: full MVP ladder ≈ 20–22 calendar days,
    ≈ 5 h active play.

# Rejected Decisions

Recorded so they are not re-litigated by accident:

- **Equipment for MVP** — rejected (Post Launch, Wave 2). The 25–30 item list
  was explicitly refused; scope cost ≈ 3–6 weeks, trips the two-week rule.
- **Miss/accuracy chance** — rejected. Pure variance against the fantasy;
  accuracy is not a stat. Crits are the only variance, capped.
- **Passive skills** — rejected. The skill model is exactly Basic / Active /
  Ultimate (3 per monster).
- **Positional formation grids** — removed entirely; slot order suffices.
- **Capture rates / items / RNG capture** — Wave 1 ("capture expansion"),
  not MVP.
- Removed entirely from MVP consideration: multiple save slots, cloud save,
  achievements, landscape support, localization beyond English, iOS, any
  second currency.
- **"Speedster" archetype** — replaced by **Support** in the canonical build
  list (Tank, Bruiser, Assassin, Mage, Support).
- Freshness decay in Phase 1 simulation — deferred to product layer (Phase 2)
  so it cannot contaminate balance-model validation.

# Capture Design ("Scouting")

Badged scout battles (Bronze rungs 1 & 4; elsewhere 3 & 6). Winning one lets
the player **choose one monster from the defeated enemy team** to recruit —
no RNG, no items. Recruit joins at level 1 with freshly rolled grades and an
immediate nickname prompt. Capture success = winning the battle, i.e. the
skill the player is already building. You capture *potential*, never power —
that is what makes capture serve "I raised this monster." Wave 1 extends the
per-rung `scoutPool` data with rates/tools/rare variants; zero code seams
needed beyond that list.

# Training Design

Four types map to stats: Strength→ATK, Endurance→HP, Agility→SPD,
Intelligence→INT. Timer-based, no minigames, no currencies beyond the Coin
fee. Depth comes from three data-only mechanisms:

1. **Grade discovery (core):** gain = baseYield × tierMultiplier[hidden
   grade]; the shown number ("+4 ATK — exceptional!") is how players learn who
   their monster is. Grades stay hidden; their effects are loudly visible.
2. **Freshness rotation (Phase 2 product layer):** repeat-type yield decays
   100→85→70%, floor 60%, recovers while training other stats. Gentle by
   design.
3. **Context:** opponent preview + train-or-fight exclusivity make "what
   should I train next?" answer differently every rung.

Training log on the monster screen is the raising receipt ("+212 ATK trained
since Bronze").

# Progression Design

XP from battles only (wins full, losses ~40%, catch-up 2–3× below band,
replays full XP). Each level: +3 points, banked. Effective stat =
base + levelGain×(level−1) + allocated + trained, where levelGain =
round(speciesGainRate × tierMultiplier[grade]) — implemented once in
StatMath. Something permanent should happen every 2–3 sessions: a level, a
grade reveal, a recruit, or a promotion.

# Battle Design

3v3 (1v1/2v2 during Bronze onboarding; simulator accepts 1–3 per side).
Player input before battle only: pick three, order them, confirm loadout.
Continuous SPD-driven action timeline (no rounds); tie order = earlier time →
higher base SPD → team A → lower slot (total ordering is a determinism
requirement). AI: ultimate if charged & valid → active if ready & valid →
basic. Victory by elimination; anti-stall and 120 s hard resolve as in
Approved Decision 9. Deterministic given a seed; the event log is the ONLY
contract between simulation and presentation, and its hash is the determinism
test. Expected TTK at parity: squishies 6–10 s focused, tanks 15–25 s.

# Economy Design

Coins in (rung wins, first-clear bonuses, champions) → Coins out (training
fees, tiered by session length). Nothing else. No dead currencies, no
inflation path, every reward feeds raising. XP and stat points are
progression, not currency; capture costs only victory.

# Asset Strategy

Prototype (Phases 1–3): free placeholders — CraftPix Free Golem Chibi
(3 golems, 17 animations) + colored quads. **No purchases before Phase 4**
(prototype asset rule). Phase 4 stack (researched, iteration 1):

- Monsters: huberthart "2D Monster — Cute & Chibi" packs, $15 each, 7 animated
  monsters with prefabs + SFX — **license unverified, confirm with author
  before purchase**; fallback: CraftPix chibi monster packs (seller-friendly
  license, ~5 monsters/pack).
- UI: Kenney UI Pack + RPG Expansion + Fantasy UI Borders — free, CC0.
- VFX: Cartoon FX Remaster Free — free, Asset Store EULA, 50 effects, 2D-ok.
- Audio: RPG Essentials SFX Free + Minifantasy Dungeon Audio — free, no
  redistribution, credits optional.
- Cost: $0 prototype; ~$15–30 at asset pass. Integration ≈ 1.5–2 weeks.

Roster flexibility rule: no pack maps 1:1 to the provisional species; adapt
the roster to the purchased pack (silhouettes/roles/progression paths are
binding, names are not). Always flag unverified licenses. Missing "hit"
animations: white-flash + micro-knockback tween.

# Architecture Principles

- **Data-driven everything.** Monsters AND skills are data; adding either
  requires zero code changes. No hardcoded monsters, skills, or stat tables.
  Tunables live in `balance.json`, never in C# literals.
- **Headless deterministic simulation.** Core/ is plain C# (POCO content
  layer; SOs convert via `ToData()`); view replays the event log and never
  computes outcomes; 1,000-battle sweeps run in seconds in edit mode.
- **Single sources of truth:** StatMath for all math; SpeciesRegistry as the
  only asset locator (everything iterates `registry.All`); saves store ids
  only, `speciesId` is append-only forever.
- **Versioned saves:** one SaveGame root, JsonUtility, atomic temp-then-replace
  writes, `saveVersion` bump + defaults for every schema change.
- **Mobile first:** portrait, no per-frame allocations in battle code, low-end
  Android target. Boring readable C# over clever abstractions.
- Core computes, never renders · Battle renders, never computes · Data
  declares, never behaves · Editor verifies, never ships.

# Data Model Overview

Core POCOs: `SpeciesData` (id, name, baseStats, GrowthWeights, 3 SkillData) ·
`SkillData` (id, slot, scalingStat ATK|INT, powerMultiplier, cooldown,
chargeTime, effect, targetRule incl. AllAllies, affectedStat, magnitude%,
duration) · `MonsterInstance` (instanceId GUID, speciesId, nickname, level,
exp, unspentPoints, growth[6], allocated, trained — plain C#, save-ready) ·
`StatBlock` {hp, atk, def, spd, intel, luck} · `BalanceConfig` (all constants;
JsonUtility-loaded; species gain-rate overrides as a list, not a dict).
Data SOs: `MonsterSpecies`, `SkillDefinition`, authorable `GrowthProfile`
(per-stat tier-weight rows). `SaveGame` (Phase 2): version, monsters[],
activeTeam[3] ids, highestLeagueCleared, softCurrency, lastSaveUtc.
RNG contract (documented in BattleSimulator.cs): growth rolls (team A then B,
slot order, stat order 0–5) → crit rolls in resolution order → hard-resolve
flip. Changing this order breaks the determinism hash on purpose.

# Current Repository State

- **Skill package:** `monster-trainer-arena.skill` v5 (SKILL.md +
  references/game-spec.md v0.5 + references/code-conventions.md). Installed
  and validated. Skill test suite: 6/6 passed (data layer, equipment scope
  bait, balance sheet, training redesign, retention wall, live asset
  research).
- **Documents:** GDD v1.0 (approved) · Pacing review (approved) · Phase 1
  Battle Prototype Specification v1.0 · this file.
- **Code:** `mta-phase1-scripts.zip` — 24 scripts: Core×18 (Enums, StatBlock,
  BalanceConfig, StatMath, ContentData, MonsterInstance, LevelMath,
  TrainingMath, TeamConfig, BattleEvent, BattleState, ActionTimeline,
  TargetSelector, SkillResolver, BattleSimulator, SpeciesRegistryCore,
  BalanceSweep), Data×4 (GrowthProfile, SkillDefinition, MonsterSpecies,
  SpeciesDatabase), Editor generator (builds 10 skills + 12 species from GDD
  tables), Phase1GateTests, balance.json v0, README.
- **IMPORTANT — code is unverified:** authored outside Unity; it has never
  been compiled. First task in Claude Code: import, compile, run
  `Phase1GateTests`, fix whatever the compiler finds.
- **Spec drift:** game-spec.md is at v0.5 and predates the GDD/pacing
  approvals. The v0.6 fold (capture design, gates, economy, session tiers,
  battle constants, scout-rung change) is pending; until done, this file
  supersedes the spec.

# Current Risks

1. Code never compiled — expect minor fixes on first import (gate tests are
   the safety net).
2. balance.json v0 values are unsimulated: success criteria 2–4 (duration
   percentiles, mirror fairness at scale, prep signal) are unproven until the
   first sweep + tuning pass.
3. huberthart asset license unverified (blocks the Phase 4 purchase, nothing
   earlier).
4. Spec drift until the v0.6 fold is committed.
5. Capture's build-phase slot unassigned (mechanic designed; UI flow is
   Phase 2–3 work).
6. Solo-dev schedule risk: UI shell (~2 wks) and asset pass (~2 wks) are the
   pure-labor humps; nothing may be added to them.
7. Resources.LoadAll is fine at 12 species; the Addressables swap behind
   SpeciesRegistry is deliberately deferred — do not do it early.

# Next Milestone

**30-day first playable** (Phase 1 complete). Definition of done = the Phase 1
success criteria: determinism hash ×100 · sweep P10 ≥ 30 s, P90 ≤ 90 s,
≤5% hard resolves · mirror 50%±3% · trained beats untrained ≥75% ·
13th-species-from-data test green · debug replay runs on an Android device.
Immediate task order: import zip → compile → run generator (MTA → Generate
Phase 1 Content) → run gate tests → first 1,000-battle sweep → tune
balance.json until criteria pass → build BattleReplayView + debug scene →
device build.

# Future Expansion Waves

Post-launch only; design seams, never build early:
**Wave 1:** capture expansion (rates, tools, rare variants), traits,
evolutions. **Wave 2:** equipment, trading, local PvP. **Wave 3:** open
world, life simulator, farming, exploration.

# Lessons Learned During Design

1. **Asset research changed the design** (not vice versa): no pack matched the
   named roster, producing the roster-flexibility and prototype-asset rules.
   Check the market before designing content.
2. **Level-1 capture created a hidden re-leveling tax** that compounded per
   league (~45% of playtime); pacing math caught it, catch-up XP + gate
   retune fixed it without touching the rule itself. Run the pacing math on
   any acquisition change.
3. **The 3v3-vs-one-starter contradiction** at first launch was caught in
   review, not design — fixed by the 1v1→2v2→3v3 ramp. Simulate minute one.
4. Behavioral rules earn their keep only under test: the equipment scope-bait
   test is why "never architect a feature the CD rejects" exists in the skill.
5. A mandatory closing question on every response was tried and removed —
   rigid per-response rituals age badly.
6. Naming collisions bite late ("Phase 2" meant two things); the roadmap owns
   "Phase," expansions own "Wave."
7. Determinism is a feature you design in (RNG contract, total tie ordering),
   not one you retrofit.
8. Content minimization applied to skills too: a 10-skill shared pool
   validates the model; 36 signature skills would have been balancing content
   before the model existed.

# Things Future Claude Sessions Must Never Change Without Approval

- The core fantasy, the four pillars, and the core loop.
- The 6 stats (HP/ATK/DEF/SPD/INT/LUCK) — no additions without strong
  justification.
- Exactly 3 skills per monster (Basic/Active/Ultimate); no passives.
- Hidden per-instance growth grades S–D; species carry tendencies only.
- 3 stat points per level; level-1 capture with grades rolled on capture.
- 3v3 with the 1v1→2v2→3v3 Bronze ramp; 30–90 s battle target.
- No miss/accuracy mechanic; crit as the only variance, capped at 30%.
- Single currency; no daily missions or any forbidden-list feature.
- Zero-code content invariant (new monster/skill = data only).
- `speciesId`/`skillId` strings are append-only forever (save keys).
- The RNG consumption order and timeline tie-ordering (determinism).
- StatMath as the only place stat/damage math lives.
- The event log as the sole sim↔view contract.
- The two-week rule, fun check, and CD-before-LA collaboration order.
- Decision persistence: never silently change game-spec.md, balance.json, or
  code-conventions.md.

---

# Executive Summary

*One-page briefing for a new technical lead.*

You are inheriting Monster Trainer Arena: a solo-dev Android auto-battler
(Unity, C#, portrait, 2D chibi) with a 3–6 month ship mandate and a design
that is **finished and locked** — your job is execution, not redesign. The
fantasy is "I raised this monster": every creature rolls hidden growth grades,
training reveals them, and fully automatic 3v3 battles (30–90 s) convert
preparation into wins. Content is deliberately tiny: 12 monsters, 6 stats,
3 skills each, 5 leagues, one currency, one capture mechanic (win a badged
scout battle, pick one defeated monster, it joins at level 1 — you capture
potential, never power).

Architecture is data-driven and headless: ScriptableObjects + balance.json
define everything (adding monster #13 or #50 is asset-only, enforced by a
test); a deterministic, seed-driven simulator emits an event log the view
merely replays, and a sweep tool runs 1,000 battles in seconds to prove the
30–90 s window and the "trained beats untrained ≥75%" fantasy gate. All 24
Phase 1 scripts exist but have **never been compiled** — your first hour is
import, compile, generate content (one menu item), and run the gate tests;
your first week is tuning balance.json until the sweep passes; your first
month is the debug replay view on a device. That is the 30-day first
playable, and it is realistic.

Guardrails: work under the project skill (CD decides before LA builds; fun
check; two-week rule; scope classes). The forbidden list is hard. Three open
items need attention early: fold this file's decisions into game-spec.md
v0.6, verify the huberthart asset license before any Phase 4 purchase, and
assign capture's build-phase slot. The riskiest system (the simulator) was
built first on purpose — prove it green, and everything downstream is data
entry, UI labor, and discipline. Ship it.
