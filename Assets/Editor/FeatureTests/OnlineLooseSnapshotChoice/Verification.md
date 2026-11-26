# Collected loose-ball snapshot choice

- [x] Reach the offer through a real pass, movement, nutmeg tie and different-player collection.
- [x] Publish owned shoot/decline and preserve the original mover and cleared assist history.
- [x] Reject wrong team, guest mutations, duplicate attempts, holds and cancelled offers.
- [x] Clear collected-snapshot state in ResetShotProcess; retain the strict cancellation reproduction.
- [x] Register every new case separately for the deferred Mac run.

Full Room is **v13**; wire remains v7. The generic collected-ball snapshot now
publishes `shot.loose-choice` ahead of the suspended movement parent. Its X path
shares the offline decline helper. Reset clears the offer so a stale answer
cannot survive cancellation. The source's movement/shot/assist rules are unchanged.

Compilation: clean, zero C# errors. **10 new passed, zero failed/skipped**:
two `OnlineLooseSnapshotChoiceEditorTests` at `20261116T171848110Z`, eight
`OnlineLooseSnapshotChoicePlayModeTests` at `20261116T172223780Z`.
Both pitch directions use the actual roster and source continuation. The new
collector takes a snapshot without replacing the original pending mover;
declining resumes movement once. Holds/guest calls add no dice draws.

Retained missing-command reproduction: `20261116T171601810Z` (eight failures).
The strict cancelled-offer test reproduced a genuine reset defect at
`20261116T171902637Z` (six passed/two failed), fixed by clearing the stale flag.
Affected `LegacyLooseSnapshotGoalPlayModeTests`: two passed at
`20261116T172341161Z`, covering actual goal/reset/no-assist and declined movement.
Native XML/progress are under `TestResults/GameplayRegression/Runs/`.

All ten cases have individual deferred Mac entries; fixture hashes/inventory
and 13 verifier unit tests pass. Windows exports still precede v11–v13 and must
be refreshed. Physical paired input, full-Room acceptance and RTT remain deferred.
For broader future user checks, use `Assets/Tests/PlayMode/LegacyLooseSnapshotGoalPlayModeTests.cs`
and `Assets/Tests/PlayMode/Features/OnlineLooseBall`; no whole suite was run.
