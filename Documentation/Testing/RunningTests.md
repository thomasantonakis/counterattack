# Running gameplay regressions

Intro clock visibility uses `IntroClockVisibilityEditorTests` under
`Assets/Editor/FeatureTests/IntroClockVisibility/`. Run only that fixture for
the presentation change; its `TASKS.md` records results for hiding during intro,
restoring after completion/skip/disable, and preserving shootout clock hiding.

Match event banner presentation uses `MatchEventBannerEditorTests` under
`Assets/Editor/FeatureTests/MatchEventBanners/` and `MatchEventBannerPlayModeTests`
under `Assets/Tests/PlayMode/Features/MatchEventBanners/`. Run only these fixture
filters (PlayMode category: `MatchEventBanners`). Their `TASKS.md` records the
native results. These test the presentation queue, notifications, sprite wiring,
unscaled animation and cleanup; they do not constitute full gameplay regression
validation.

Goalkeeper pickup after movement uses `GoalkeeperPickupEditorTests` under
`Assets/Editor/FeatureTests/GoalkeeperPickup/` and `GoalkeeperPickupPlayModeTests`
under `Assets/Tests/PlayMode/Features/GoalkeeperPickup/`. Use only these explicit
filters; category `GoalkeeperPickupFollowup` selects the outside-phase, target
cleanup and catch-percentage follow-ups. The dedicated Verification.md records
execution evidence and exact broader user-run fixtures.

Long Ball Pace ordering uses only `LongBallGoalkeeperPacePlayModeTests` in
`Assets/Tests/PlayMode/Features/LongBallGoalkeeperPace/`, backed by the owned board
in `Assets/Editor/FeatureTests/LongBallGoalkeeperPace/`. See its Verification.md
for scope, retained native results and broader user-run fixtures. Serialize runs
and recompiles across sessions sharing this Editor.


Kickoff Ground Pass has fixture `KickoffGroundPassPlayModeTests` in
`Assets/Tests/PlayMode/Features/KickoffGroundPass/`. It covers opening action
availability across game modes, own-half uncapped passes at every difficulty,
normal Standard Pass limits/interceptions for opponent-half and x=0 targets,
both directions, post-goal and later-half restarts, ordinary-pass cleanup, and
old/new snapshot options. Run only this fixture for the feature. Results and
additional user-run checks are recorded in
`Assets/Editor/FeatureTests/KickoffGroundPass/Verification.md`.
For the snapshot-free Room seed exception, use the focused case filter
`SeedWithoutSnapshotPreservesOpeningChoices`; it checks P/C/L availability,
ordinary new-match restrictions, P activation and hover previews without live-save writes.

Headed back-passes use dedicated fixtures `HeadedBackPassEditorTests` under
`Assets/Editor/FeatureTests/HeadedBackPass/` and `HeadedBackPassPlayModeTests` under
`Assets/Tests/PlayMode/Features/HeadedBackPass/`. Extra-time action caps use
`ExtraTimeStoppageEditorTests` under `Assets/Editor/FeatureTests/ExtraTimeStoppage/`.
Run these explicit fixture filters for this change. Their checklists record results
and broader user-run coverage. Delayed goalkeeper pickup is implemented separately
in the GoalkeeperPickup fixtures described above; its approved scope remains in
`GoalkeeperPickupFutureSessionPrompt.md`.


The shared score floor has dedicated fixtures `AttributeScoreFloorEditorTests`
in `Assets/Editor/FeatureTests/AttributeScoreFloor/` and
`AttributeScoreFloorPlayModeTests` in
`Assets/Tests/PlayMode/Features/AttributeScoreFloor/`. They verify pre-roll
clamping, injuries (including GK skills), snapshot/save/control entry points,
probabilities, labels and jackpot/natural-one preservation. Run only these filters
for the feature; its Verification.md records native results and broader user-run
checks. Do not substitute historical approved-decision results for this batch.

Approved wiki decisions have dedicated fixtures `ApprovedRuleDecisionsEditorTests`
in `Assets/Editor/FeatureTests/ApprovedRuleDecisions/` and
`ApprovedRuleDecisionsPlayModeTests` in
`Assets/Tests/PlayMode/Features/ApprovedRuleDecisions/`. They cover HP/LB/control
jackpots, keeper header attributes/rushes, own-box card policy and mandatory foul
stoppages. Use the explicit fixture filters, not all tests. The feature's
`Verification.md` records actual native results and broader user-run coverage.
The outside-keeper nomination/zero-Heading follow-up uses PlayMode category
`OutsideKeeperHeaders`, which selects the two new cases plus related keeper
header, rush and jackpot-tie cases without unrelated foul or accuracy checks.

Possession time has focused fixtures `PossessionTimeEditorTests` in
`Assets/Editor/FeatureTests/PossessionTime/` and `PossessionTimePlayModeTests` in
`Assets/Tests/PlayMode/Features/PossessionTime/`. Regulation credits actual live
game-clock seconds to the current attacking team, switching immediately on
recovery or a restart possession change, before the next action is committed.
Each consumed stoppage action credits its starter 60 seconds, without wall-time
accumulation. Timed totals and the current action starter survive save/load;
older saves begin accumulating timed possession from zero. See the feature's
`Verification.md` for the focused execution results and durable artifacts.

Open **Window → General → Test Runner** in Unity. This is the same Unity Test
Framework suite used by the CLI/Pipeline automation; there is no separate automated
scenario runner to launch with ordinary Play.

- **EditMode** tests cover isolated rules, calculations, decisions and data/UI formatting.
- **PlayMode** tests cover gameplay input, movement, coroutines, interruption,
  presentations and continuations. Unity enters and exits Play Mode for these tests.
- Select a fixture or case and choose **Run Selected** while iterating. After a change,
  run only the feature's dedicated fixture or category filter. Create a dedicated
  feature folder with its Unity tests and a short task/verification checklist.
  Do not automatically start or wait for whole suites; those require an explicit
  user request.

At feature handoff, Codex identifies any additional affected test folders by exact
path and Test Runner fixture/category, explaining why they need verification.
The user runs these broader checks. When a complete regression check is warranted,
Codex prompts the user to press **Run All** on the appropriate EditMode and/or
PlayMode tabs, then hands over without waiting. The user reports failures for
Codex to inspect and fix. Codex does not execute broader checks unless explicitly
asked to do so.

Wait for an active run to finish before editing scripts: a domain reload can interrupt
PlayMode execution. Native cases build and clean up their own deterministic boards;
they do not use a mutable live save as their fixture.

Before starting tests, finish compilation/reload, save dirty loaded scenes through
Unity CLI `save_all`, and verify their dirty flags are clear. Check after reload:
editor initialization can modify Room. A pending scene-save dialog blocks test
startup and can make CLI commands time out; resolve an already-open dialog manually
with Save before continuing.

Historical discovery before Room presentation additions: **748 EditMode + 362 PlayMode = 1,110 tests**. See
[GameplayRegressionMigration.md](GameplayRegressionMigration.md) for actual completed
run counts; discovery and compilation are not test results.

The CLI/Pipeline drives Unity's same TestRunner API. The Test Runner window shows
individual tests and failures; automation additionally writes durable NUnit XML,
per-case progress and the current case under `TestResults/GameplayRegression/Runs/`.
An async CLI response saying `running` does not mean the suite passed.

Room presentation work on 2026-11-07 adds native fixtures
`RoomPresentationEditorTests` and `RoomPresentationPlayModeTests`; the discovery
count above predates these additions. Their view tests require the edit-mode export
from `RoomPresentationEditorTools.Author()`. See
[RoomPresentationImplementation.md](RoomPresentationImplementation.md) for the
actual focused results and frame captures. The user explicitly reserved the complete
EditMode/PlayMode suites for their own execution; Codex ran only the focused Room
fixtures for this change (13 EditMode and 22 PlayMode cases, including final
preview double-click and shutdown follow-ups).

The retained historical `GameTestScenarioRunner` sequence was a different system:
ordinary Play ran a serial scenario script and logged its assertions. Its replacements
are independently selectable framework tests with cleanup on failure. The migration
inventory records which originals have actually passed before retirement.

Review [MorningReview.md](MorningReview.md) for expectation drift decisions and any
unresolved rules questions. These tests currently target Unity Editor EditMode/PlayMode,
not standalone player test builds.
