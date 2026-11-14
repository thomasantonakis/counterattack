# Online penalties

- [x] Owned kicker selection, mandatory placement, setup movement and confirmation.
- [x] Both directions, goal/save, deadlines, holds and forbidden box/arc clearance.
- [x] Guest/wrong-role/stale-command rejection and actual Room manager wiring.
- [x] Real in-box dribble/steal foul awards penalty without Final Third movement.
- [x] Native evidence, compilation and individual deferred Mac entries.

Compilation separately clean. Category `OnlinePenalties`:
`OnlinePenaltyPlayModeTests`: 12 passed, zero failed/skipped,
`Runs/20261114T154938927Z/results.xml`. `OnlineRoomPenaltyPlayModeTests`:
2 passed, zero failed/skipped, `Runs/20261114T155418827Z/results.xml`.
Additional focused `OnlineFoulPlayModeTests.InBoxStealFoulAwardsPenalty`:
2 passed, zero failed/skipped, `Runs/20261114T155456793Z/results.xml`.

Isolated tests follow real setup through penalty aiming, shooter/save/handling,
then a goal or keeper distribution, with statistics and continuation assertions.
Room cases use the actual serialized managers and reach the owned shooter roll.
The host board now explicitly initializes authored shooting/heading paths and
geometric caches because its suppressed grid Start never did so. Guest startup
still does not initialize gameplay. No runtime visual structures were added.

Initial run `154700806Z` exposed a fixture defect: Ensure created a manager on the
isolated inactive MatchManager object. Fixture now owns an active coroutine host.
Missing online decisions reproduced in `154800718Z`. The 12/2 run `155209542Z`
then exposed missing Room startup caches (penalty spot rejected as shot origin);
the host initialization fix passed the affected two cases. No assertions weakened.

Every new case has its own deferred Mac manifest entry. No paired, full-match or
whole-suite acceptance is claimed. Current exports precede this penalty/cache
batch and must rebuild before a future paired session.
For broader user-run coverage, use `Assets/Tests/PlayMode/Features/OnlineShots/`
category `OnlineShots` and `Assets/Tests/PlayMode/Features/OnlineGroundDuels/`
category `OnlineGroundDuels`: Room now loads the same path/danger caches as offline.
