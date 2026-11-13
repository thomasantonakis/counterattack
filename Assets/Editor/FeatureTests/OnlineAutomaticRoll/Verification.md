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
