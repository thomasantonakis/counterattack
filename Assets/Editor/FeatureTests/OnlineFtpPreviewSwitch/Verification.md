# First Time Pass preview to movement switch

- [x] Add real preceding-pass/FTP-entry tests for both teams and directions.
- [x] Reproduce the missing M offer before fixing it.
- [x] Preserve committed, wrong-team, stale and transport-hold boundaries.
- [x] Run only this feature plus affected FTP/context-key regressions.

User reproduction at Intermediate assistance: F allowed target previews, the
instructions advertised M, but M was absent from the authenticated legal actions.
Coverage uses actual TriggerFTP/TriggerMovement through published semantic commands,
checks wire decoding, possession/ball/statistics preservation and a stale old
FTP command. The owned GoalkeeperBoxEntryFixture restores scene/singleton/time/
RNG/event/save state. No live save is used as a mutable fixture.

Before-fix reproduction: 12 failed / 4 committed-boundary passes in
`TestResults/GameplayRegression/Runs/20261119T151621379Z/results.xml`.
Initial corrected Intermediate run: 16 passed / 0 failed / 0 skipped in
`TestResults/GameplayRegression/Runs/20261119T152048353Z/results.xml`.
An additional Experienced fixture initially sent a redundant second preceding-pass
click after that assistance level had already committed; this was a fixture defect
(retained run 20261119T152222076Z), corrected by keeping the preceding pass
at Intermediate and selecting Experienced before real FTP activation.

Final feature run including Experienced immediate-commit boundaries: **20 passed,
0 failed, 0 skipped** at `TestResults/GameplayRegression/Runs/20261119T152426878Z/results.xml`.
Compilation separately clean. Each parameterized case is registered for the next
requested Mac run. No paired acceptance or complete regression suite is claimed.

Affected existing FTP fixture: 14 passed, zero failed/skipped at
`TestResults/GameplayRegression/Runs/20261119T152544323Z/results.xml`.
Existing contextual-key fixture: 45 passed, zero failed/skipped at
`TestResults/GameplayRegression/Runs/20261119T152711351Z/results.xml`.
Broader optional user-run coverage: `Assets/Tests/PlayMode/Features/OnlineMovement/`
(OnlineMovementPlayModeTests) for other movement starts; no broader suite was run.
