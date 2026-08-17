# Test run — eval 4: training-redesign (iteration 1, skill v4)

## Active Role

Creative Director. (Player psychology and decision-making — no architecture
requested.)

## Objective

Turn training from "tap the biggest number" into a genuine decision, using only
what the MVP already has: four timer-based training types, hidden growth grades,
stat allocation, and league opponents.

## Design Decision

The current system has no decision because it has no *tension*: every option is
always available, always equally efficient, and identical for every monster.
Add tension with three data-only mechanisms, in priority order:

**1. Growth-grade discovery (the core fix).** Training yield already routes
through the hidden growth grade — so make the *result* expressive: "Wolf gained
+4 ATK!" vs the usual +2. Now every session is an experiment on *this* monster:
players train to *learn who their Wolf is*, then double down or pivot builds.
The dominant question becomes "what is my monster good at?" — which has a
different answer per instance. This is Pillar 1 and Pillar 2 doing the work, and
it deepens the core fantasy: discovery is how "Wolf #32" becomes "my Wolf."

**2. Rotation via freshness.** Repeating the same training type decays its yield
(100% → 85% → 70%, floor 60%; decays recover while training other stats). The
greedy single-button strategy now underperforms a rotation *aimed at a build*.
Gentle numbers on purpose — this should whisper "vary it," not scream "you're
playing wrong."

**3. Opponent-driven demand.** The league screen already previews the next
opponent team; that preview makes training contextual: facing a Bat rush, SPD
(act first) or HP (survive the burst) beats another +ATK. "What should I train
next?" gets a different answer every league rung.

One rule, free of charge: a monster in training is unavailable for battle until
the timer ends (or the session is cancelled for zero gain). Choosing *who*
improves versus *who* fights is the classic pet-raising tension — pure
Tamagotchi, zero new systems.

## Benefits

- Decisions vary by monster (grades), by history (freshness), and by opponent
  (context) — three independent reasons the biggest number isn't the answer.
- Reinforces attachment: discovery produces "MY Wolf is an ATK monster" moments.
- Costs almost nothing: two formula terms in `balance.json`, one preview label,
  one availability rule. No minigames, no currencies, no assets.

## Risks

- Hidden grades can frustrate if feedback is vague → always show the concrete
  gained number and a subtle "exceptional gain!" flourish; the *effects* of
  grades must be visible even though grades are not.
- Freshness can read as punishment → keep decay shallow, recovery fast, and
  never show a red "-30%" — show the current yield, not the penalty.
- Opponent preview must not become homework → one glance ("fast team, hits with
  skills"), not a stat spreadsheet.

## Scope Classification

MVP Safe. Self-check: no new assets, no new screens, no new currencies; strictly
formula + copy changes. Simplest shippable version = mechanism 1 alone; add 2
and 3 only if the loop still feels flat in playtesting.

## Recommendation

Implement (1) growth-grade discovery now — it's nearly free because the yield
formula already exists — playtest, then layer (2) freshness and (3) opponent
context if needed. Record the chosen decay curve and yield ranges in
`balance.json` and the design intent in `game-spec.md`.
