# DECISIONS.md — Train Your Monster

Approved decisions only. Format: **Decision · Reason · Status.** "Approved"
means locked in game-spec v0.5 / GDD v1.0 / Phase 1 spec and consistent across
the source documents. Rejected items are listed at the end as approved *non*-
decisions (things the team decided NOT to do).

---

## Vision & product

| Decision | Reason | Status |
|---|---|---|
| Ship an Android Play Store MVP in 3–6 months, solo | Scope discipline; release probability is the top metric | Approved |
| Core fantasy = "I raised this monster" (not "collected") | The whole design differentiates on attachment to individuals | Approved |
| Four pillars: raising, visible growth, team building, long-term goals | Every feature must strengthen ≥ 1 | Approved |
| Fun > Features · Retention > Complexity · Shipping > Perfection | Tie-breaker when goals conflict | Approved |
| References Monster Rancher / Digimon / Tamagotchi; NOT Pokémon/MMO/open-world | Sets the pet-raising tone and cuts scope temptations | Approved |

## Scope & process rules

| Decision | Reason | Status |
|---|---|---|
| Two-week rule: >2 weeks solo → default Delay/Reject; burden on the feature | Protects the ship date | Approved |
| Fun check gates every feature (notice / care / memorable) | Kills low-value scope early | Approved |
| Content minimization: 12 deep monsters over 100 shallow | Depth + solo feasibility | Approved |
| Decision persistence: offer spec/balance/conventions updates, never silent | Cross-session consistency | Approved |
| Self-check (scope? delay? assets? MVP? simpler?) before responding | Bias toward the simpler version | Approved |

## Architecture

| Decision | Reason | Status |
|---|---|---|
| Unity (C#), Android-first, portrait | Target platform | Approved |
| Data-driven: ScriptableObjects + `balance.json`; new monster/skill = data only | No recompile/risk to add content; expandable for years | Approved |
| Strict layers: Core computes, Battle renders, Data declares, Editor verifies | Keeps sessions composable; any PR blurring lines is wrong | Approved |
| Headless deterministic simulator; view replays its event log | Enables replays + honest 1,000-battle balancing without scenes | Approved |
| Core purity via POCOs (`SpeciesData`/`SkillData`, `ToData()`) | Keeps Core asset-free so sweeps/tests run headless | Approved (impl.) |
| `BattleSimulator.Run` takes a `SpeciesRegistry` param | Instances resolve species by id per conventions | Approved (impl. deviation from spec sketch) |
| Fixed RNG consumption order (growth → crit → coin flip) | Determinism hash depends on it | Approved |
| Versioned `SaveGame`, ids-only, temp-file atomic replace, append-only ids | Saves survive content updates; no corruption/lost progress | Approved |
| 6 stats only: HP/ATK/DEF/SPD/INT/LUCK | No stat bloat | Approved |
| Canonical types in code-conventions (StatBlock, GrowthTier, etc.) extended, not replaced | Cross-session code compatibility | Approved |

## Progression

| Decision | Reason | Status |
|---|---|---|
| 3 stat points per level, manually allocated, banked | Player agency = build identity | Approved |
| Hidden growth grades S/A/B/C/D per stat, rolled per instance | Makes two same-species monsters different (the core hook) | Approved |
| `tierMultiplier` D 0.6 / C 0.8 / B 1.0 / A 1.25 / S 1.5 | Growth routing | Approved |
| Level cap 30; losses grant ~40% of a win's XP | Respect casual sessions | Approved |
| Builds (Tank/Bruiser/Assassin/Mage/Support) emerge from allocation + training, no separate system | Zero extra scope | Approved |

## Training

| Decision | Reason | Status |
|---|---|---|
| Timer-based, no minigames; 4 types (Strength→ATK, Endurance→HP, Agility→SPD, Intelligence→INT) | Tamagotchi pacing, low build cost | Approved |
| Grade discovery is the core decision hook — ship it first | Nearly free; turns training into an experiment on *this* monster | Approved |
| Freshness rotation (100→85→70%, floor 60%, recovers) | Beats greedy single-button; gentle nudge | Approved (add only if loop feels flat) |
| Train-or-fight tension: monster in training can't battle | Classic pet-raising decision, free | Approved |
| Session tiers Quick 15 m / Standard 2 h / Overnight Camp 8 h | Camp is the organic return appointment | Approved |

## Capture ("Scouting")

| Decision | Reason | Status |
|---|---|---|
| Scout battles at rungs 3 & 6; win → pick 1 enemy to recruit | Smallest capture that still feels like raising; player choice, no RNG | Approved (resolves spec's "capture basic — TBD") |
| Recruit joins at level 1 with fresh grades + nickname prompt | You capture *potential*, not power | Approved |
| `scoutPool` data per rung (Wave 1 seam) | Extends to rates/tools/variants without code | Approved |

## Battle

| Decision | Reason | Status |
|---|---|---|
| 3v3 (sim supports 1v1/2v2/3v3 + asymmetric); Bronze ramps 1v1→2v2→3v3 | Onboarding ramp | Approved |
| Damage `ATK × (1 − DEF/(DEF+50))`; DEF only inside mitigation | One formula, one place | Approved |
| Crit `min(LUCK×0.005, 0.30)` × 1.5 | Bounded variance — prep, not dice | Approved |
| aps `0.02×min(SPD,25) + 0.01×max(0,SPD−25)`, cap 1.0 | SPD-stacking brake at the kink | Approved |
| Ultimates charge once/battle (≥15 s), ≤45% of avg HP pool, validated | No one-shots | Approved |
| No miss chance / no accuracy stat | Variance undermines "I prepared correctly" | Approved |
| Formation = slot order (slot 1 front); no positional grid | Real decision without a grid | Approved |
| Continuous SPD action timeline, totally-ordered tie-breaks | Determinism requirement | Approved |
| Deterministic targeting rules (front / lowest-HP / lowest-HP% ally) | Readable, attributable outcomes | Approved |
| Rule-based AI (Ultimate→Active→Basic), no randomness | Outcomes attributable to preparation | Approved |
| Anti-stall ramp after 75 s; hard resolve at 120 s by Σ HP% | Kills double-tank stall | Approved |
| Duration target 30–90 s | Casual session fit | Approved |
| Skills: exactly 3/monster (Basic/Active/Ultimate); resolver switches on effect | Data-only expandability | Approved |
| Phase 1 shared 10-skill pool; per-species signatures deferred to Phase 3 | Validate model before authoring content | Approved |
| `TargetRule.AllAllies` for team buffs | One resolver loop, no per-skill code | Approved (impl.) |

## Career, retention, economy

| Decision | Reason | Status |
|---|---|---|
| 5 leagues × 8 rungs + champion = 45 AI battles, all data | Reusable structure | Approved |
| Promotion gates demand roster depth (Silver 3≥8 … Master 6≥24) | Converts "finished a league" into a raising goal | Approved |
| Retention via camp appointment + gates + grade curiosity — no missions/login/gacha | Return behavior from the fantasy, not live-service | Approved |
| One currency (Coins), one sink (training fees); ~1 win funds ~1.5 sessions | No inflation, no dead currency | Approved |
| Mastery grades (flawless/swift) | 10 medium-term goals/league | Approved as Nice-to-have / first patch |

## Assets

| Decision | Reason | Status |
|---|---|---|
| 2D chibi fantasy; adapt design to free/cheap assets; never require custom art | Asset-constrained solo budget | Approved |
| Prototype asset rule: placeholders in Phases 1–3; purchases at Phase 4 | Don't buy before the loop is validated | Approved |
| Roster flexibility: adapt species to the purchased pack | Distinct silhouettes/roles/paths matter, not species names | Approved |
| Cohesive stack: CraftPix golems (proto) → huberthart Cute&Chibi (release) · Kenney UI · Cartoon FX Remaster Free · Leohpaz SFX | One coherent look, ~$0 proto / ~$30 pass | Approved (huberthart license **unverified — confirm before purchase**) |

## Approved non-decisions (rejected / excluded)

| Rejected | Reason | Status |
|---|---|---|
| Passive skills / 4th skill slot | New scope; 3-skill model is locked | Rejected |
| Miss/accuracy stat | Undermines prep fantasy | Rejected |
| Positional formation grid | Slot order suffices | Removed |
| 2nd currency, cloud save, achievements, landscape, non-English, iOS, 2nd save slot | Scope cuts for ship probability | Removed for MVP |
| Everything on the forbidden list (PvP, equipment, gacha, daily missions, breeding, guilds, open world, etc.) | Out of MVP; some are Wave 1–3 seams only | Forbidden in MVP |

---

*All decisions traceable to `/archive`. When any of these change, update this
file and `game-spec.md` per the decision-persistence rule.*
