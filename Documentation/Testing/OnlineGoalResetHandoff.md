# Online goal reset and header highlights handoff

Branch `fix/online-goal-reset-presentation` continues from online foundation commit `e1ebda7d`. The Windows host and Son’s Mac guest entered Room and played through a headed goal. The paired run exposed a rejected goal-reset snapshot and a raised header-at-goal target that dropped on the guest when the host hovered it.

## Paired evidence

- The guest’s `frontend-session-report.json` says `failed` with `Manual guest rejected state 8765, revision 352, phase HeaderGeneric/goal.resolving, pause revision 0`. The guest frontend started at `2026-11-24T17:59:13.9715340Z` and reported failure at `2026-11-24T18:12:36.8570930Z`. The rejection happened before an accepted shared pause. The disabled Resume button is consistent with the session having closed.
- The Windows host logged a headed goal for Home, the start of celebration on goal side `1`, and `For token: 5. Willems; Hex (10, 0, 4) is already occupied by 11. Baez!`. It stopped logging normal goal progression there. The host run artifacts are in `%USERPROFILE%/AppData/LocalLow/DefaultCompany/CA from Chatgpt/OnlineSessions/beabd3b76b9e4deabd51566909abacb9/`. The associated `Player.log` is in the parent `CA from Chatgpt` directory. The session’s live events are in `room-authority__Live Events__H1_00-09.jsonl`, where the goal and reset moves are in sequences 240–265. The guest rejection summary gives the phase and sequence but contains no raw snapshot or validator reason.
- The raised goal-line targets appeared on the guest. When Windows hovered a target, Son’s Mac saw that hex turn black or drop again. The project uses inGoal and CanShootTo for this goal-line set. Header eligibility comes from `HeadingPaths`.
- The previous penalty coin publication bug was reproduced and fixed in `e1ebda7d`. Keep this `goal.resolving` failure separate from the earlier `MatchEnded/coin.home` rejection.

## Goal-reset reproduction

1. Follow [AGENTS.md](../../AGENTS.md), [CONTRIBUTING.md](../../CONTRIBUTING.md), and the Windows setup instructions before Editor operations. This checkout has unrelated font assets and local configuration changes. Stage task files explicitly and preserve those edits.
2. Use the authored Room and a deterministic formation to make a defender finish their goal-reset animation on a hex still logically occupied by a teammate or opponent who is moving farther. The right-side reset formation includes `(10, 0, 4)`. An equivalent repeatable setup can place a scoring-side blocker on an early defender reset destination, start the defender nearby, and make the blocker travel farther toward the celebration. Reuse the existing Room fixture and native PlayMode cleanup. Exercise the real goal-flow entry point and mirror the scenario when practical.
3. Encode intermediate host states with `OnlineGroundPassWire` and apply them with `OnlineGroundPassReplica` during `goal.resolving`. Reproduce the rejection before the fix and assert which two token records claim the same hex. Verify the final guest state is accepted and reaches the conceding team’s kickoff setup with unique token/hex ownership. [Authority goal flow](../../Assets/Scripts/Managers/GoalFlowManager.cs) · [Replica roster validation](../../Assets/Scripts/Online/OnlineRosterPublication.cs) · [Existing goal regression](../../Assets/Editor/FeatureTests/OnlineGoals/OnlineGoalFixture.cs)
4. `MovePlayersToHexes` starts concurrent animation coroutines. Before the fix, each token took its logical hex as its animation ended. `PlayerToken.SetCurrentHex` logged a collision and still updated the token’s `currentHex`. `OnlineRosterPublication.Valid` rejects two on-board tokens claiming one coordinate. If a destination is still occupied, the arriving token now keeps its current logical hex. The existing `roomPose` carries its animated position and `ReconcilePostGoalTokenHexes` commits the final formation after both sides reset. [Token occupancy](../../Assets/Scripts/GamePlay/PlayerToken.cs) · [Host pose publication](../../Assets/Scripts/Online/OnlineGroundPassAuthority.cs)

## Preserve raised header targets through host hover

`HeaderManager.LiftHexesInGoal` records the raised target set and highlights it as `CanShootFrom`. `RefreshHeaderAtGoalHover` can temporarily change a target’s reason to `ShotTargetHover`. Before the fix, `OnlineHexPreview.Capture` filtered out that reason and the guest cleared the raised preview. The authority now publishes the base highlight and height for every explicit header-at-goal target. The focused Room test applies the initial, hovered and cleared publications through the wire and checks the guest’s target height and highlight. [Header targeting](../../Assets/Scripts/Managers/HeaderManager.cs) · [Preview capture](../../Assets/Scripts/Online/OnlineHexPreview.cs) · [Room fixture](../../Assets/Editor/FeatureTests/OnlinePairedFeedback/OnlineFullRoomSceneFixture.PairedFeedback.cs)

## Halftime and shootout movement

- The ordinary halftime and extra-time resets use the same movement and formation reconciliation as the post-goal reset. The occupancy check applies to offline and online play.
- The penalty coin toss opens before either team selects its shootout order. The countdown rejection at `MatchEnded/coin.home` was independently reproduced with the same publication sequence for two countdown values and repaired in `e1ebda7d`.
- Shootout staging exposed a separate occupancy collision after both orders were confirmed. With two pairs occupying each other’s assigned slots, concurrent staging could move an occupant and also start that player’s planned move. Offline, `home:2` and `home:3` claimed `(0, 0, -1)`, and `home:4` and `home:5` claimed `(0, 0, -3)`. Online, two evictions chose the same fallback hex `(-2, 0, -12)`, and the guest rejected `PenaltyShootoutSetup/shootout.resolving`. Staging now releases the participants’ former hexes together before the animations start. Shootout movement releases logical occupancy at departure in both match modes. [Shootout movement](../../Assets/Scripts/Managers/PenaltyShootoutManager.cs)

## Verification and PR handoff

Unity 6000.5.3f1 compiled with zero errors. Each run directory below contains native `results.xml` and `progress.jsonl` under `TestResults/GameplayRegression/Runs/`.

| Case | Reproduced before fix | Passed after fix |
| --- | --- | --- |
| Goal reset in both directions | `20261124T182348714Z`. `away:5` and `home:11` both claimed `(10, 4)` or `(-10, -4)` during `HeaderGeneric/goal.resolving`. The guest rejected both snapshots. | `20261124T183259527Z`. Both online directions and the offline reset reached kickoff with unique occupancy. |
| Host hover on a raised header target | `20261124T182431675Z`. The guest’s `(-19, 0, -1)` target dropped from `0.03` to pitch level. | `20261124T182743362Z`. The guest kept the base highlight and elevation through host hover. Both cleared when aiming ended. |
| Halftime reset with the new formation occupied | | `20261124T183259527Z`. The online and offline cases reached second-half kickoff with unique occupancy. |
| Shootout staging with swapped pairs | `20261124T182950208Z`. Both cases found duplicate occupancy. The online guest rejected the staging snapshot. | `20261124T183259527Z`. The online and offline cases placed the swapped players and reached the first kick. |
| Penalty coin toss countdown | `20261124T175257900Z` on the previous branch. The guest rejected a newer countdown value that reused the previous publication sequence. | `20261124T183459341Z`. The guest accepted successive countdown values at the same roll decision. |

The movement run passed seven cases with zero failures, zero skips and zero occupied-destination warnings. The hover and countdown runs each passed one case with zero failures or skips.

The frontend rules advance from v8 to v9 because an older host can still send a duplicate-occupancy snapshot that disconnects a newer guest. Pull request #12 targets `codex/online-foundation-personal-mac`.

Paired Windows and macOS verification remains pending. Rebuild both platforms from the same revision and check host hover, the headed goal and progression through celebration to kickoff on both screens.

Broader PlayMode checks for the affected paths are `GoalPublishesResetAndConcedingRestart` in [OnlineGoalPlayModeTests](../../Assets/Tests/PlayMode/Features/OnlineGoals/) for pause and the complete kickoff sequence, `FirstKickOutcomeContinues` and `CompleteShootout` in [OnlineShootoutPlayModeTests](../../Assets/Tests/PlayMode/Features/OnlineShootout/) for later shootout movement, and `PublishedGuestChoicesUseAuthoredHexes` in [OnlinePublishedHighlightsPlayModeTests](../../Assets/Tests/PlayMode/Features/OnlinePublishedHighlights/) for committed goal markers.
