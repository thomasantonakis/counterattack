# Goalkeeper pickup after movement

Focused filters: `GoalkeeperPickupEditorTests` (EditMode),
`GoalkeeperPickupPlayModeTests` (PlayMode). Follow-up category:
`GoalkeeperPickupFollowup`. PlayMode adapters are in
`Assets/Tests/PlayMode/Features/GoalkeeperPickup/` and reuse the owned deterministic
`GoalkeeperBoxEntryFixture`. No live saves or designer Stats layouts are fixtures.

- [x] Explicit delivery kind, source, owner and handling eligibility; no inference from hanging-pass strings.
- [x] Ground Pass, FTP, High Pass and Long Ball exclusions; opponent recovery; header landings and nearby collection.
- [x] Both directions, own/opponent/outside box boundaries, later phases, feet persistence and in-memory runtime snapshot round trip.
- [x] Normal phase-end gate before Final Thirds, once-only action completion, cancellation and stop-play cleanup.
- [x] Authored single-choice overlay, exclusive H/F input, receiving-team ownership and legal AI candidates.
- [x] Pickup has no second roll or save statistic; Q skips Final Thirds; K follows Save and Hold, then Drop Ball forbids re-pickup.
- [x] Hide the choice's whole dice tile, show next actions on separate lines and in instructions; direct catch displays 83.3%.
- [x] Release committed goal target at defensive header winner resolution, before result continuation.
- [x] Update bundled source and existing rulebook prefab paragraphs, preserving unrelated prefab serialization.

Entry-point audit: kicked actions enter through `MatchManager.CommitToAction`;
headed passes mark delivery in `HeaderManager.MoveHeaderToTargetSelection` before
landing. `SetLastToken` and `SetLastTokenFromLooseBall` record explicit touches;
the latter clears the assist chain while preserving handling provenance. Movement
collection uses `ApplyBallCollectionOwnership`; Ground/FTP/header/Long Ball and
loose-ball interceptions reach their existing recovery paths. Own-box recovery
now checks provenance. Save/held catches explicitly mark hands. Deliberate Drop
Ball records its lockout in `FinalThirdManager.DropBall`. New delivery, another
owner, possession loss, restart/reset and half end revoke the relevant permission.

`MovementPhaseManager.EndMovementPhase` arms normal completion only for successful
phase ends. The broadcast consumes this permission once; canceled phases cannot
complete or offer a stale pickup. `ResolveActionBeforeFinalThird` retains action
counting and half-end gates. Choosing feet resumes the regular completion;
choosing hands enters the existing Q/K controls without a second completion.
Temporary movement resets preserve provenance while canceling pending UI.
Stable runtime snapshots persist delivery/owner/eligibility; old snapshots grant
no inferred permission. Saving during the pending modal is blocked by the existing
active-action save gate.

Execution history and final counts are recorded below. Native XML and per-case
progress are under `TestResults/GameplayRegression/Runs/` (git-ignored).
The initial reproduction failed in both directions because Final Thirds started
before pickup. Earlier fixture-only failures concerned Final Thirds readiness,
an occupied movement destination and bypassing the actual collection input path.
Assertions were preserved while fixing setup.

Final compilation is clean through the live Editor. Final EditMode:
**14 passed / 0 failed / 0 skipped** at
`Runs/20261108T092457421Z/results.xml`.

All **41 distinct feature PlayMode cases** passed across the final fixture and
focused retests (overlapping runs must not be added):

| Scope | Passed / failed / skipped | Native XML under Runs/ |
| --- | --- | --- |
| Final 39-case fixture | 36 / 3 / 0 | `20261108T091321442Z/results.xml` |
| Outside-phase, percentage and target follow-up | 6 / 1 / 0 | `20261108T091759094Z/results.xml` |
| Last target-release setup correction | 1 / 0 / 0 | `20261108T092026689Z/results.xml` |

The last three fixture failures were setup defects: an interception test observed
the initial Any Other Scenario before the ball arrived, and the older headed-goal
helper attempted High Pass from the now restricted opening kickoff (Any Other
Scenario also excludes High Pass). The new case starts from the existing regular
end-of-movement option API instead. The target-release check asserts the goal
target is lowered and its committed highlight released while the defensive winner
result is still being presented, before ball movement/continuation.

Earlier red reproduction: `Runs/20261107T215339497Z/results.xml`, 0/2/0.
Intermediate runs: `20261108T083135577Z` 10/24/0 (collection/setup defects),
`20261108T083842497Z` 32/0/0 (corrected deferred-flow matrix), and
`20261108T082720460Z` 11/0/0 (initial EditMode scope). Native XML retains each
case's actual assertions and output. The final rulebook check compares every
existing serialized text entry against the bundled source document.

Broader checks are user-run, not executed for this feature:

- `Assets/Tests/PlayMode/`: `GoalkeeperBoxEntryPlayModeTests`,
  `LegacyKeeperRemainingPlayModeTests`, `LegacyHeaderResolutionPlayModeTests`,
  `LegacyHeaderGoalBranchPlayModeTests`, `LegacyHeaderSelectionPlayModeTests`
  for shared interception, keeper distribution, aerial and movement continuations.
- `Assets/Tests/PlayMode/Features/HeadedBackPass/`: `HeadedBackPassPlayModeTests`
  for direct catch/spill and existing cancellation branches.
- `Assets/Editor/`: `DuelResolutionPresentationEditorTests`,
  `HeaderChallengePresentationEditorTests` for shared overlay input;
  `Assets/Editor/FeatureTests/HeadedBackPass/`: `HeadedBackPassEditorTests`.

No whole EditMode or PlayMode suite was run. Dedicated cases cover the approved
pickup flow; they do not establish every existing foul, goal, half-end, shot,
multiplayer, hardware-input or long-match combination.
