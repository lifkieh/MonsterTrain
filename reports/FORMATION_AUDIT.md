# FORMATION AUDIT (Phase 6)

Presentation only — pure visual staging, **no gameplay/position-in-sim change** (the deterministic
charge overwrites these within ~1 s of contact). Evidence: `showcase_v11/` opening frames.

## Before
Formation was purely pick-order (slot). You could not read the team composition (who's the tank vs
mage vs assassin) before combat.

## After — role staging (`BattleReplayView.RoleStageOffset`)
Applied as a visual offset on the starting anchor only:
- **Tank** → forward + planted low (holds the front).
- **Mage** → pushed to the rear + raised.
- **Support** → rear.
- **Assassin** → offset high (flanking read).
- **Bruiser** → holds the line (no offset).

Combined with the per-species stance (tank low/planted, mage/flyer high) from the character profiles,
the composition now reads at the VS→charge moment.

## Honest limitation
This is only visible in the brief pre-charge window (the tawuran charge then pulls everyone to the
clash — intended combat behaviour, unchanged). It stages the *opening*, it does not persist through the
fight. Verified as a start-of-battle read; not a mid-fight feature.
