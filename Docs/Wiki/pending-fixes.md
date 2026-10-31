# Pending fixes and verification

This is the single active Wiki work list. Items below are either confirmed
follow-ups or explicitly unrun checks. Tactical judgments stay hypotheses
until replayed against the identified saved run and legal gameplay state. Do not
change the currently running match or start Play Mode/tests from Codex; the user
runs those checks.

## 1. Thomas 7 / UH7: replay unresolved tactical decisions

Use the distinct 30 October 2026 run documented in Git history and its run
companions. Preserve the run identity (`playSessionId` beginning
`10012b34`, inherited event boundary 4); do not mix it with the 28/29 October
runs. Build recorded-board/full-provider fixtures before changing behavior.

- **Forced loose-ball roll (#93):** determine why the direction-to-distance
  transition did not produce a separately recorded decision. Reproduce with
  and without the tactical note panel; mandatory `[R]` input must remain human
  controlled and be recognized as a new actionable state.
- **Carrier route and space pass (#100–108, #128–130):** compare direct carry,
  self-release and teammate pass at identical phase/budget horizons. Explain
  why the route reversed to a visited hex and why action-stage and target-stage
  continuation forecasts diverged. Retain legitimate evasive reversals and
  useful safe space passes.
- **Collector mismatch (#153–158):** compare the direct keeper pass with the
  chosen space pass using live legality, back-pass rules, interception,
  collection cost and next distribution. Preserve the intended eligible
  collector across F3 when still legal; otherwise replan explicitly.
- **Coordinated defense (#31–32, #67–68):** compare the whole defender
  allocation, including Noruega coverage, central-line connectivity, roles,
  specific pass/carry threats and remaining movement budget. Do not hard-code
  player coordinates or roles.
- **Keeper recovery and support (#94–98, #109–114):** reconstruct keeper
  eligibility and the exact attacking/defending/2f2 budgets. Compare immediate
  recovery against the next legal opportunity, and evaluate Denier/Iwu support
  against separated outlets and retained defensive cover. Confirm whether
  interception correctly ends the defending phase under the game rules.
- Preserve the positive separated Soares outlet (#135–136) and genuine safe
  space releases. Forecasts with unknown finishing/collection outcomes must
  remain explicitly unresolved, not be presented as certain retention.

Older kickoff/setup and draft-loop observations in `codex-tactician.md` were
partly superseded by later kickoff changes. Reopen only if a current replay
reproduces them; do not treat historic observations as current defects.

## 2. Tactical planner validation still outstanding

The following fixes and fixtures are authored, but the source notes do not
record a user-run verification after the latest changes. Run the focused
EditMode suites and gameplay scenarios during the user's next test pass:

- `ActionSequenceEditorTests` and its `DefensiveResponseEditorTests`,
  `HighPassScatterEditorTests`, and `ReboundRecoveryEditorTests` cases.
- `KickoffOpeningEditorTests`, `RosterAttackOptionsEditorTests`,
  `ReportedMovementReplayEditorTests`, and `RoomAITacticalPlannerEditorTests`.
- Run the isolated `Scenario_042_CodexKickoff_FirstPassOnReceiver` only when
  the user enables `runCodexKickoffOnly`; do not change scenario selection as
  part of cleanup.
- Thomas 7 recorded-board coverage added after the older passing 360-test run:
  jumper eligibility; high-pass/F3 reach and offside; preparation/reply and
  scatter branches; receiver/defender alternatives; paired 2f2 outlets; kickoff
  pressure/recovery; and latency profiling. Use the exact fixtures and inspect
  their current scenario switches before the test pass.
- Continue full-provider/manual match review for tactical quality. A compile,
  helper diagnostic or old passing suite is not a live-match verdict.

## 3. Save browser and match-stat checks

The save-slot/session and shared Delete-column behavior is implemented; its
latest prepared `RoomSaveBrowserEditorTests` set has 31 cases and was not run in
the latest recorded pass. Run it, then manually check in both Single Player and
Hot Seat that Delete removes only the selected manual slot, Load remains a
separate action, unsaved play sessions stay hidden, Save overwrites the active
manual slot, and Save As creates/retargets a slot without changing the working
session identity.

The recovery-stat changes are implemented. Manually run runner scenarios 021,
031b, the affected shooting pickup branches and offside restart 042f. Confirm
expected/made recoveries update player and team totals exactly once. The static
MatchStatsUI/EndGamePanel wiring audit passes; visually inspect stats, scorers
and lineups at intended resolutions without changing designer-owned
`Canvas/MatchStatsUI/Stats` layout.

## 4. Set-piece coverage gaps

Review only against the intended rules and existing manager-owned legal
options. Add/execute coverage for:

- A real resolution method for an occupied corner spot.
- The rare free-kick taker case with no adjacent free hex (rule decision needed
  before implementation).
- Corner High Pass: corner flag is set before activation.
- Final taker auto-selection, defender-clearance enforcement and instruction
  box obligation text.
- Prepared-position reviews for free kicks, corners and throw-ins, including
  goalkeeper phase safeguards.

## 5. Duel panel manual presentation check

The tackle/nutmeg duel panel is authored and compiled. Manually verify both
attacking directions, result/jackpot/foul paths, required-roll messaging,
countdown and Escape/Enter behavior, pitch-input blocking until dismissal,
forced substitutions, dragging/clamping after expansion and resize, and saved
position across duels. Do not expand this check to unrelated goalkeeper or
single-roll challenge panels.

## Completion rule

After each user-run verification, update this file: remove completed items and
retain only remaining work. Keep implementation details, historical evidence
and resolved investigations out of separate Wiki files.
