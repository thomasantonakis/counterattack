# Online automatic roll admission

- [x] Reproduce automatic/manual roll race through real long-pass timing.
- [x] Route deadline execution through the same typed busy boundary as online commands.
- [x] Preserve requester/connection holds and actual dice/statistics exactly once.
- [x] Run focused OnlineAutomaticRoll tests and affected offline automatic-roll case.

This audit precedes further high-pass/header/shot integration. Paired checks deferred.

Compilation separately clean. OnlineAutomaticRoll PlayMode **4 passed, zero
failed/skipped** in
`TestResults/GameplayRegression/Runs/20261113T181541982Z/results.xml`.
The pre-fix reproduction failed all four cases in
`TestResults/GameplayRegression/Runs/20261113T181421683Z/results.xml`:
automatic direction resolution still advertised long.roll while its coroutine
was completing, allowing late input to compete with the accepted result.

An explicit scoped match authority marker and typed DecisionTimingRegistration
callback now route long-pass deadlines through TryExecuteOnlineLongBall. Its
existing busy/version checks claim execution before a nested coroutine draws.
Offline registrations retain their existing handler path. Both connection-held
expiry and late manual commands are covered in both attacking directions; real
dice histories and exactly one pass attempt are asserted.

Affected focused PlayMode evidence: OnlineLongPass **16/16** in
`TestResults/GameplayRegression/Runs/20261113T181650720Z/results.xml`;
OnlineSharedPausePlayModeTests.PausedInterceptionCannotDrawOrReplayARoll **2/2**
in `TestResults/GameplayRegression/Runs/20261113T181807978Z/results.xml`;
DecisionTimingPlayModeTests.AutomaticStoppageRoll **1/1** in
`TestResults/GameplayRegression/Runs/20261113T181828251Z/results.xml`.
Zero failed/skipped. No full suites ran.

Unexecuted: native paired deadline races, automatic long distance/interception
outcomes, and equivalent typed deadline routing for future high/header/shot
adapters. Broader user-run timing coverage: category DecisionTiming in
`Assets/Tests/PlayMode/Features/DecisionTiming/` when requested. The new typed
registration overload is intended for upcoming online action families.

Both Windows exports refreshed after this fix: Room `build_5428d4f1d0fa`
Succeeded (eight warnings), prepared board `build_10beb3d0c739` Succeeded
(two warnings). Both retain the known missing-vswhere.exe diagnostic (one build
report error each; no script compile errors). Packaged existing local checks
passed in `TestResults/GameplayRegression/Runs/20261113T182248410Z-Room-local/`
and `TestResults/GameplayRegression/Runs/20261113T182302253Z-Gameplay-local/`.
Each retains result.json, gameplay-evidence.json and build-report.json. These
are local packaged ground/pause/recovery checks, not native paired deadline-race
acceptance; actual deadline behavior has the focused framework evidence above.
