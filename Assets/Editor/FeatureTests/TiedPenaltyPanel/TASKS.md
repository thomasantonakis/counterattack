# Tied penalty panel

Follow-up 2026-11-09: normal penalties now offer the keeper roll immediately after
the shooter, matching shootouts. TiedPenaltyPanelPlayModeTests passed 4/4 in native
run 20261109T181713641Z; DecisionTimingReportedIssuesPlayModeTests adds four explicit
normal/shootout immediate-keeper cases. Compilation is separately clean. No full
suites ran; see DecisionTiming/Verification.md for the complete focused record.

Feature tests:
- EditMode: `TiedPenaltyPanelInstructionEditorTests` (this folder).
- PlayMode: `TiedPenaltyPanelPlayModeTests` in `Assets/Tests/PlayMode/Features/TiedPenaltyPanel/`.

- [x] Normal penalty ties retain the shot/save columns and expand into direction and distance rolls.
- [x] Shootout ties retain the shot/save columns and expand into the direction roll.
- [x] Shootouts display the existing fixed distance (one hex into goal, two otherwise) without inventing a distance roll.
- [x] After a shootout deflection is determined, its result countdown permits shot animation and normal shootout continuation.
- [x] Both attacking directions covered by native PlayMode tests: 4 passed, 0 failed, 0 skipped.
- [ ] Final instruction copy checks after correcting expected jersey-number prefixes.

Native PlayMode result: `TestResults/GameplayRegression/Runs/20261107T114504646Z/results.xml`.
Compilation: zero errors/warnings.

Instruction stakes were retained: `BuildKickStakesInstruction` adds win-if-score and lose-if-miss text during the taker's walk-up. The bracket result grid remains removed. During kick execution the shootout manager currently clears its walk-up instruction flag; that existing timing is unchanged.

No broad suites run. For additional shared-panel verification, the user can run `Assets/Tests/PlayMode/Features/ShootoutRollPanel/` (`ShootoutRollPanelPlayModeTests`).
