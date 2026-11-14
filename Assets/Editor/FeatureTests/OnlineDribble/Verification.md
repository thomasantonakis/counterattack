# Online dribble continuation and steals

- [x] Reproduce missing continuation after the first real dribble step.
- [x] Typed one-hex continuation, pace forfeit and declined-nutmeg steal.
- [x] Real recovery/miss, ownership, deadlines, holds and cancellation.
- [x] Focused compilation and category OnlineDribble.
- [x] Append every new case to OnlineMacTestManifest.json; Mac run deferred.

Compilation separately clean. Category `OnlineDribble`, fixture
`OnlineDribblePlayModeTests`: **12 passed**, zero failed/skipped, in
`TestResults/GameplayRegression/Runs/20261114T144142718Z/results.xml`.
Initial reproduction: 12 missing-continuation failures in
`20261114T143557925Z/results.xml`. Both attacking directions cover two real
dribble steps, one-hex boundary and guest rejection, remaining-pace forfeit,
declined nutmeg, steal miss/recovery, actor/team ownership, automatic deadlines,
late requests, held result and cancellation before result continuation.
Real source helpers retain offline input semantics. An existing duel version
now guards delayed steal results after movement reset.

Affected existing cases: `LegacyMovementPlayModeTests.StealAfterDecliningNutmeg`
**2 passed** in `20261114T144326376Z/results.xml`; its
`DribblerForfeitRegistersOnceAndNextAttackerMoves` **1 passed** in
`20261114T144435409Z/results.xml`, all zero failed/skipped. No whole suites ran.
Broader user-run scope when available: `Assets/Tests/PlayMode/Features/OnlineMovement/`,
category `OnlineMovement`, for non-ball-carrier selection and movement phases.
Mac manifest has a separate ledger entry for each of the 12 new parameter cases.

Nutmeg attempts, tackle duels, fouls/cards/injuries and movement snapshots remain
subsequent slices. Do not claim this is complete movement or a full online Room.
