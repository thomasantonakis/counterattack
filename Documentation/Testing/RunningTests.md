# Running gameplay regressions

Online result continuation uses PlayMode category `OnlineResultHold` /
`OnlineResultHoldPlayModeTests` under
`Assets/Tests/PlayMode/Features/OnlineResultHold/`: 6 passed, zero failed/skipped.
Affected shared-pause 13 and offline DuelManualReview 4 also passed; compilation
separately clean. See `Assets/Editor/FeatureTests/OnlineResultHold/Verification.md`.

Authoritative dice observations use focused EditMode category `OnlineRollResults`
/ fixture `OnlineRollResultsEditorTests` in
`Assets/Editor/FeatureTests/OnlineRollResults/`. This covers real authority rolls,
no guest RNG, detached bounded history, run isolation and atomic replica rejection.
See that folder for actual results; this is separate from paired acceptance.

Room online startup uses category `OnlineRoomStartup`: EditMode fixture
`OnlineRoomStartupEditorTests` in `Assets/Editor/FeatureTests/OnlineRoomStartup/`
(2 passed) and PlayMode fixture `OnlineRoomStartupPlayModeTests` in
`Assets/Tests/PlayMode/Features/OnlineRoomStartup/` (1 passed).
The owned real Room action uses category `OnlineRoomAction`: EditMode
`OnlineRoomPublicationEditorTests` in `Assets/Editor/FeatureTests/OnlineRoomAction/`
(6 passed), PlayMode `OnlineRoomActionPlayModeTests` in
`Assets/Tests/PlayMode/Features/OnlineRoomAction/` (6 passed).
All have zero failed/skipped tests; compilation separately clean. Native XML,
fixture boundaries and external acceptance are in those Verification.md files.
Do not count these as a full regression suite or paired online Room acceptance.
Broader user-run checks: `Assets/Tests/PlayMode/LegacyGroundPassPlayModeTests.cs`
(`LegacyGroundPassPlayModeTests`) for offline passes;
`Assets/Editor/RoomSaveBrowserEditorTests.cs` (`RoomSaveBrowserEditorTests`) for
offline save browsing; and `Assets/Tests/PlayMode/Features/DecisionTiming/`
(`DecisionTimingPlayModeTests.FileTimingRestore`) for offline timing persistence.
Run only affected cases if failures
are reported; no broad suites were started by these online batches.

Manual prepared-board controls and recovery exchange use explicit PlayMode
category/fixture `OnlineManualControls` / `OnlineManualControlsPlayModeTests`
under `Assets/Tests/PlayMode/Features/OnlineManualControls/`. Final scope: 7
passed, zero failed/skipped, compilation separately clean. Three cases cover
authored button/input behavior; four run real host/guest exchange iterators
through encoded interrupted fake transport. See
`Assets/Editor/FeatureTests/OnlineManualControls/Verification.md` for native XML,
paired local evidence and unexecuted EOS/Room acceptance.

Online recovery infrastructure uses explicit category `OnlineRecovery`:
`Assets/Editor/FeatureTests/OnlineRecovery/OnlineRecoveryEditorTests.cs` and
`Assets/Tests/PlayMode/Features/OnlineRecovery/OnlineRecoveryPlayModeTests.cs`.
Focused results: 8 EditMode + 8 PlayMode passed, zero failures/skips;
compilation separately clean. See the Editor folder's Verification.md for XML
and remaining transport integration. These are injected-clock authority tests,
not a standalone reconnect or Room acceptance claim.

Prepared online shared pause uses category `OnlineSharedPause`: EditMode
`OnlineSharedPauseEditorTests` under `Assets/Editor/FeatureTests/OnlineSharedPause/`
and PlayMode `OnlineSharedPausePlayModeTests` /
`OnlineSharedPausePresentationPlayModeTests` under
`Assets/Tests/PlayMode/Features/OnlineSharedPause/`. Focused results: 11 EditMode
+ 13 PlayMode passed, zero failed/skipped; compilation separately clean.
See the Editor feature folder's Verification.md for native XML and the v2
packaged/paired pause proof checklist. No broad suites ran.
The paired Windows-host/Mac-guest pause proof also passed, with matching state
digests, sharedPauseRevision=2 and one authority roll; see that verification
file for both retained application report paths. This is separate from the
11 EditMode + 13 PlayMode framework counts.

EOS gameplay command/state and authored replica proof uses explicit category
`OnlineGroundPassNetwork`: EditMode fixture `OnlineGroundPassWireEditorTests`
under `Assets/Editor/FeatureTests/OnlineGroundPassNetwork/` and PlayMode fixture
`OnlineGroundPassNetworkPlayModeTests` under
`Assets/Tests/PlayMode/Features/OnlineGroundPassNetwork/`. Focused results:
13 EditMode + 2 PlayMode passed, zero failed/skipped; compilation separately
clean. Windows/Mac packaged checks and the separate-network paired gameplay
run passed with matching final-state digests and one host-only roll; commands and limits are in
`Documentation/Setup/OnlineGroundPassNetwork.md`. These probes are automated
prepared-board checks, not full M1 or an interactive Room match.

The first isolated online gameplay authority slice uses category
`OnlineGroundPass`: EditMode fixture `OnlineGroundPassBoundaryEditorTests` under
`Assets/Editor/FeatureTests/OnlineGroundPass/` and PlayMode fixture
`OnlineGroundPassPlayModeTests` under `Assets/Tests/PlayMode/Features/OnlineGroundPass/`.
Final focused scope: 1 EditMode + 14 PlayMode passed, zero failed/skipped;
compilation separately clean. See the Editor feature folder's `Verification.md`
for native XML, coverage and unexecuted affected checks. This gateway is not
wired into Room or EOS; guest replication and complete M1 remain pending.

Standalone EOS packaging uses the dedicated authored scene
`Assets/Scenes/EOSStandaloneProof.unity` and instructions in
`Documentation/Setup/EOSStandaloneProof.md`. Argument validation uses only
EditMode category `EOSStandaloneProof`, fixture `EOSStandaloneProofEditorTests`
under `Assets/Editor/FeatureTests/EOSStandaloneProof/` (9/9 passed).
Standalone application reports are JSON with local Player.log, separate from
native Unity Test Framework XML. Windows/Mac builds, logins and paired standalone
relay packets passed on user-reported separate networks. No gameplay suites are needed for
this isolated probe. See that feature folder's `Verification.md` for evidence.

Online multiplayer's initial protocol foundation uses `OnlineMatchProtocolEditorTests`
(EditMode category `OnlineMatchProtocol`) under
`Assets/Editor/FeatureTests/OnlineMatch/`. Run only that explicit fixture for its
isolated account/team, compatibility, revision and retry checks. Current evidence:
27 passed, zero failed/skipped; compilation separately clean. See its
`Verification.md` for native XML and unexecuted networking/gameplay coverage.
This code is not wired into Room, and does not yet prove internet connectivity.

Live EOS probes live under `Assets/Editor/FeatureTests/OnlineEOSConnectivity/`.
They require the ignored local EOS configuration. The shared EOSProbeSession
helper supports sequential probes in one Editor process, keeping the native
SDK initialized and releasing each platform handle after use.
Create the one-use `Temp/OnlineEOSConnectivity.run` marker before running only
`EOSConnectLoginEditorTests.DeviceIdentityCanLogInThroughConnect` by exact EditMode
test-name filter; without the marker, routine runs skip the case. It passed 1/1
on this Mac. The separate `EOSPrivateLobbyEditorTests` case uses
`Temp/OnlineEOSLobby.run` and also passed 1/1. An Editor that ran older probes
which called SDK Shutdown needs a manual restart first. Room must be clean before
running a fixture. See `OnlineEOSConnectivity/Verification.md` for results and
native XML. These probes never start ordinary gameplay.
The two-Mac host/guest lobby join and relay packet cases passed; Windows
session reuse also passed 1/1. Windows/macOS packets remain pending. Exact
filters, one-use markers and local lobby-ID handoff are in the same
`Verification.md`. Do not run the host's five-minute wait until both Editors
are ready. Lobby joining alone does not prove P2P packet delivery.

Restart selection feedback uses RestartSelectionFeedbackPlayModeTests, category
RestartSelectionFeedback, under Assets/Tests/PlayMode/Features/RestartSelectionFeedback/.
Method filters: ThrowSelectionTiming, PostHeaderMovementTiming, CornerTargets,
FinalThirdHover. Latest: 24 new plus 11 affected PlayMode cases passed; compilation
separately clean. Native paths/limitations are recorded in
Assets/Editor/FeatureTests/RestartSelectionFeedback/TASKS.md. No full suites ran.

Seven reported decision-timing issues use DecisionTimingReportedIssuesPlayModeTests
(category DecisionTimingReportedIssues): SnapshotConsumesOneMove, FinalThirdMotion,
TacklePanel and PenaltyKeeperImmediately. Run explicit fixture/method filters only.
Affected checks: DecisionTimingBannerEditorTests, DribblePaceChoice and
SnapshotDeclarationPanel on DecisionTimingLiveClockPlayModeTests,
TiedPenaltyPanelPlayModeTests and DecisionTimingPlayModeTests.ShootoutReview.
Latest final scope: 17 EditMode + 48 distinct PlayMode passes; compile separately
clean. Events/timing now display simultaneously; older preemption claims are historical.
See Verification.md for native XML, original missing-slot log evidence and reruns.

Latest immediate-display checks: DecisionTimingBannerEditorTests (17 EditMode)
and DecisionTimingLiveClockPlayModeTests.DribblePaceChoice (12 PlayMode). The latter
covers both directions/difficulties and inside/outside snapshot range. Countdown
threshold defaults to 0f; 0.5f restores halfway display. Latest affected focused scope
passed 17 EditMode + 44 distinct PlayMode cases; Verification.md lists native runs.

Current concurrent MP choice checks use `DecisionTimingLiveClockEditorTests`,
`DecisionTimingBannerEditorTests`, `DecisionTimingLiveClockPlayModeTests.SnapshotUsesPhase`,
`.SnapshotDeclarationPanel`, `DecisionTimingPlayModeTests.Duel` and `.FoulFollowup`.
Latest final outcomes: 38 EditMode + 45 PlayMode distinct cases passed; compile
separately clean. Choice timers now consume their owning MP concurrently. See the
first section of Verification.md for native XML and the failed-case rerun record.

Latest decision-timing revision uses explicit fixtures
`DecisionTimingLiveClockEditorTests` and `DecisionTimingLiveClockPlayModeTests`
(category `DecisionTimingLiveClock`). It verifies shared diff 1–2 preview time,
immediate diff 3 commitment, live rolls/reviews/MP motion, and MP snapshot charging.
Countdown boundary/event priority uses `DecisionTimingBannerEditorTests`. Latest
focused runs passed 33 EditMode and 136 PlayMode cases, zero failed/skipped; compilation
is separate. Verification.md lists every affected filter and retained native XML.
Older result summaries below describe earlier policies; mandatory rolls and normal
reviews now keep time running, while Enter manual holds/pause suspend timing.


Decision allowances and automatic rolls use category `DecisionTiming`. EditMode fixtures are
`DecisionTimingEditorTests` and `DecisionTimingBannerEditorTests` under
`Assets/Editor/FeatureTests/DecisionTiming/`; PlayMode uses `DecisionTimingPlayModeTests`
under `Assets/Tests/PlayMode/Features/DecisionTiming/`. Run these filters serially, following
the clean-scene/recompile checks below. Budgets apply only while the underlying game clock
can run; substitution/forced-replacement budgets are removed. Mandatory rolls still have
five-second deadlines during built-in stoppages, with gameplay pauses respected.
See `Assets/Editor/FeatureTests/DecisionTiming/Verification.md` for 192 EditMode and 35
distinct initial PlayMode passes, subsequent focused duel runs, native XML under `TestResults/GameplayRegression/Runs/`, and
broader user-run checks. Identified focused acceptance gaps are now verified; this is not a complete regression result.

The 2026-11-09 continuation adds 22 passing focused cases. Use single method filters
`DecisionTimingPlayModeTests.JumperSelection`, `.JumperContinuation`,
`.ManualHeaderResult`, `.InspectionTiming`, `.GameplayTimingRestore` and
`.GameplayRollRestore` (expand each suffix to the full fixture name). A semicolon
combination matched zero cases on this Pipeline; inspect actual counts. Verification.md
records native artifacts and remaining acceptance combinations, including Rules pause
behavior and interrupted parent/child restoration.

Follow-up filters: `DecisionTimingBannerEditorTests` (all 14 announcement kinds),
`DecisionTimingPlayModeTests.RulesTiming`, `.CpuRollTiming`, `.DuelManualReview`,
and `.JumperOverflow`; use the full fixture prefix for each method. The affected
existing Rules checks use `RulesViewerPlayModeTests.Room` (live and paused paths).
These focused runs passed 14 EditMode and 12 PlayMode cases. Rules opened beside
instructions leaves gameplay/CPU/automatic rolls active; opening through Pause
preserves that pause. Event announcements take priority over the separate timing
display. Native XML and remaining acceptance work are recorded in Verification.md.

The formerly pending filters `DecisionTimingPlayModeTests.StoppageManualReview`,
`.FileTimingRestore` and `.InterruptedTimingRestore` passed 12 focused PlayMode cases.
The latter verifies the stable-save guard and stale timing cleanup, not resumable
mid-action saves. File tests use disposable paths and restore application/prefs state.
Their native XML is retained in runs `20261109T123543740Z`, `20261109T123616744Z`
and `20261109T123647770Z`; see Verification.md for the remaining acceptance audit.

Remaining-test batch is complete: single filters `DecisionTimingPlayModeTests.TargetReopening`,
`.KeeperFollowup`, `.ShotFollowup`, `.FoulFollowup`, `.NonblockingScatter` and `.ShootoutReview`
(expand the fixture prefix) passed 42 new cases. Affected `.DuelManualReview` and
`.RealAccuracyRoll` reruns passed 8 cases. No full suites ran. The fixed nonblocking
scatter result allows travel/possession time while recovery rolls and blocking reviews
retain their holds. Verification.md records final XML paths and the completed focused audit.


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
