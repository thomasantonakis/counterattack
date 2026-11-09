# Decision timing verification

Core implementation added 2026-11-09. The latest user revision and its focused verification are recorded first; older batches below are historical. Broader regression suites remain unexecuted. The user's implementation-time clarifications are recorded in
`Documentation/Plans/DecisionTimingImplementation.md`.

## Mid-dribble coverage and immediate countdown default (2026-11-09)

Latest user instruction restores immediate countdown visibility. The prominent setting
at the top of DecisionTimingConfig is `CountdownDisplaySpentFraction = 0f`:
0 shows immediately, 0.5 restores the halfway threshold. Event announcements still
take priority; built-in clock stops still exclude decision budgets. A child choice
now takes display priority immediately under the default, while both allowances run.

Fixed missing mid-dribble coverage: the short SnapshotOrForfeitPace allowance now
also covers move-another-hex / X waits when snapshot is unavailable. Explicit roll,
keeper, shot, reposition and other child interruptions do not get misclassified as
that choice. Each completed pace leads to its next logical choice; invalid clicks do
not refill the current one. Both the choice and owning MP consume time.

The user's manually saved timing/event panel positions and font changes are
preserved. Room SHA-1 stayed fc86a26bb85ea0b4e65a1277938f467f5e4e543d throughout
verification; no authoring alignment helper or loaded-scene disk rewrite was used.
Room is loaded, stopped and clean. The earlier reported panel centers are historical.

Reproduction `20261109T154857367Z`: 6 inside-box cases passed, 6 outside-box cases
failed because only AttackingMovement was registered. All 12 new cases passed after
the fix. Tests cover both directions, all difficulties, inside/outside snapshot range,
concurrent charging, invalid clicks, countdown priority, expiry, legal next movement,
animation and X after expiry. Their display expectations were updated for threshold 0
and rerun following the presentation-only default change.

Final distinct focused scope: **17 EditMode + 44 PlayMode passed, 0 remaining failed,
0 skipped**. Native results/progress retained under TestResults/GameplayRegression/Runs:

| Run | Filter | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T155004757Z` | LiveClockPlayModeTests.DribblePaceChoice, initial fixed run | 12 | 0 | 0 |
| `20261109T155031056Z` | LiveClockPlayModeTests.SnapshotUsesPhase | 2 | 0 | 0 |
| `20261109T155056520Z` | LiveClockPlayModeTests.SnapshotDeclarationPanel | 2 | 0 | 0 |
| `20261109T155121944Z` | PlayModeTests.Duel | 20 | 0 | 0 |
| `20261109T155152824Z` | PlayModeTests.FoulFollowup | 2 | 0 | 0 |
| `20261109T155218505Z` | LiveClockPlayModeTests.CommittedMovement | 6 | 0 | 0 |
| `20261109T155430123Z` | DecisionTimingBannerEditorTests, immediate display/choice priority/event preemption | 17 | 0 | 0 |
| `20261109T155451965Z` | LiveClockPlayModeTests.DribblePaceChoice, threshold-0 rerun | 12 | 0 | 0 |

Expand PlayMode filter names with the DecisionTiming fixture prefix. The repeated
12-case run is counted once in the distinct total. Compilation separately: live
recompile completed cleanly; scriptCompilationFailed is false. No full suites or
ordinary gameplay ran. Full regression/Load Game browser limitations remain below.

## Concurrent choice allowances and snapshot panel follow-up (2026-11-09)

Latest user clarification supersedes the earlier MP-only snapshot/challenge rule.
Snapshot/forfeit-pace, tackle/stand and nutmeg choices each have their own 10/10/5
seconds at difficulties 1/2/3. During an owning MP, both the choice and that team's
phase allowance consume elapsed time. The opposing team's phase is not charged.
Repositioning retains its previous separate, parent-suspending policy.

An eligible MP countdown stays visible before the choice reaches halfway; at the
choice's own halfway point, its countdown takes precedence. On dismissal the MP
countdown returns if eligible. Manual holds and Pause still suspend accounting;
choice expiry releases when its input ends, independently expired MP holds persist.
The actual nutmeg input is N/X (or victim selection); tackle uses T/N.

Snapshot declaration also no longer pulses the next moved-player slot while the
shot owns input. Completed dribbler entries stay visible. Panel signature tracking
includes shot ownership, so entry/exit of that state updates the existing authored
UI without creating runtime visual structures.

Focused results: **38 EditMode + 45 PlayMode distinct cases passed**, zero remaining
failed/skipped cases. This is final outcome per case across the listed runs, not a
claim that the intermediate 23-case run was wholly green. No full suites or ordinary
gameplay ran. The Editor was already playing when verification began; CLI stopped
it before framework execution. Room is preserved loaded and clean at handoff.

| Run | Scope | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T152457084Z` | SnapshotUsesPhase reproduction of missing own allowance | 0 | 2 | 0 |
| `20261109T152850887Z` | DecisionTimingLiveClockEditorTests, concurrent charging/expiry/restore plus affected service cases | 21 | 0 | 0 |
| `20261109T152901692Z` | DecisionTimingBannerEditorTests, three new choice-priority cases plus announcements | 17 | 0 | 0 |
| `20261109T152919496Z` | DecisionTimingLiveClockPlayModeTests | 21 | 2 | 0 |
| `20261109T153046337Z` | SnapshotDeclarationPanel failed-case rerun | 2 | 0 | 0 |
| `20261109T153111144Z` | DecisionTimingPlayModeTests.Duel | 20 | 0 | 0 |
| `20261109T153141322Z` | DecisionTimingPlayModeTests.FoulFollowup | 2 | 0 | 0 |

Native XML/progress are retained under `TestResults/GameplayRegression/Runs/`.
The two panel fixture failures checked the fixture's original attacker instead of
its loaded roster dribbler. Corrected tests capture the actual selected dribbler and
select an actual roster defender. Both directions verify real S declaration,
completed dribbler, absence of an Active next slot during defender selection/motion
and goal-line target selection. Gameplay assertions were preserved.
Compilation separately: live recompile completed without errors;
`EditorUtility.scriptCompilationFailed` is false. `git diff --check` is clean.

Budget source audit requested by the user: throw-in optional-MP/throw choice is
configured 20/15/10 and the optional MP uses normal phase budgets. Throw-in taker
and throw-type selection are excluded while WaitingForThrowInTaker stops the clock.
Free-kick initial setup is excluded; final kicker and execution choices have
20/15/10 after clock resumption. Corner late three-move phases and execution have
20/15/10; initial stopped setup is excluded. Save-and-hold distribution is 10/10/5,
quick-throw target 20/15/10; choosing K enters the stopped GoalKick flow. GoalKick
budgets stay inactive during that built-in clock stop. Header-or-control is
10/10/5. This audit inspected source paths; it is not a new complete restart-flow
integration run. Broader regression and Load Game browser checks remain unexecuted.

## Latest user revision: live rolls and shared previews (2026-11-09)

This revision supersedes historical statements below that roll waits or ordinary
result displays freeze match time/budgets, that each uncommitted preview has its own
allowance, or that snapshot/forfeit inside MP has a separate allowance.

- Difficulties 1–2: one SelectNextAction allowance survives all uncommitted action
  previews and switches until actual commitment. No preview allowance can be banked.
- Difficulty 3: an action key commits immediately and starts that action's allowance.
  The new real-input test exposed Long Ball delaying commitment until target selection;
  ActivateLongBall now commits on selection, consistent with P/C/F/S/M.
- Mandatory rolls and normal automatic result reviews keep match time running.
  A pending roll charges an existing active allowance only for its owner (or both
  teams); it does not invent an allowance. Enter manual holds and actual pauses
  suspend timing. Five-second auto-roll scheduling/race protection is preserved.
- MP animation keeps its phase identity, budget consumption and countdown eligibility.
  An expired allowance yields during automatic continuation, then holds again when
  tactical input returns. Snapshot/forfeit-pace uses the MP allowance.
- Countdown is hidden before 50% of the initial allowance is spent. The initial
  budget is saved with timing; old timing data falls back to configured defaults.
  Manual-hold notices remain visible independently of the countdown threshold.
- Scene-authored decision/events panels are centered under HOME/AWAY respectively.
  Verified saved world x centers: HOME/decision 140, AWAY/events 860. Event priority
  still prevents simultaneous display. No designer-owned stats layout was changed.

Final focused runs: **33 EditMode + 136 PlayMode passed, 0 failed, 0 skipped**.
This includes 17 new service cases and 21 new integration cases, plus 131 affected
existing cases. Filters ran serially through the live Editor; no complete suites or
ordinary gameplay ran. Every run retained native XML/progress under
`TestResults/GameplayRegression/Runs/`:

| Run | Filter / scope | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T144614515Z` | `DecisionTimingLiveClockEditorTests` | 17 | 0 | 0 |
| `20261109T145510752Z` | `DecisionTimingLiveClockPlayModeTests` | 21 | 0 | 0 |
| `20261109T145628830Z` | `DecisionTimingBannerEditorTests` | 14 | 0 | 0 |
| `20261109T145716101Z` | `DecisionTimingEditorTests.ChildSuspendsParentAndRollConsumesChildAllowance` | 2 | 0 | 0 |
| `20261109T145734568Z` | `DecisionTimingPlayModeTests.Duel (20 cases)` | 20 | 0 | 0 |
| `20261109T145805432Z` | `DecisionTimingPlayModeTests.RealAccuracyRoll` | 4 | 0 | 0 |
| `20261109T145824803Z` | `DecisionTimingPlayModeTests.TargetReopening` | 16 | 0 | 0 |
| `20261109T150119312Z` | `DecisionTimingPlayModeTests.TargetCountdownAndClock` | 8 | 0 | 0 |
| `20261109T150137793Z` | `DecisionTimingPlayModeTests.WholeMovementPhase` | 2 | 0 | 0 |
| `20261109T150531450Z` | `DecisionTimingPlayModeTests.GameplayTimingRestore` | 8 | 0 | 0 |
| `20261109T150548664Z` | `DecisionTimingPlayModeTests.NonblockingScatter` | 8 | 0 | 0 |
| `20261109T150616696Z` | `DecisionTimingPlayModeTests.FoulFollowup` | 2 | 0 | 0 |
| `20261109T150639167Z` | `DecisionTimingPlayModeTests.AdditionalDecisions` | 12 | 0 | 0 |
| `20261109T150656559Z` | `DecisionTimingPlayModeTests.StoppageManualReview` | 4 | 0 | 0 |
| `20261109T150714072Z` | `DecisionTimingPlayModeTests.InterruptedTimingRestore` | 4 | 0 | 0 |
| `20261109T150731269Z` | `DecisionTimingPlayModeTests.FileTimingRestore` | 4 | 0 | 0 |
| `20261109T150748844Z` | `DecisionTimingPlayModeTests.ShotFollowup` | 4 | 0 | 0 |
| `20261109T150811711Z` | `DecisionTimingPlayModeTests.KeeperFollowup` | 6 | 0 | 0 |
| `20261109T150834146Z` | `DecisionTimingPlayModeTests.ShootoutReview` | 6 | 0 | 0 |
| `20261109T150851507Z` | `DecisionTimingPlayModeTests.AutomaticStoppageRoll` | 1 | 0 | 0 |
| `20261109T150908834Z` | `DecisionTimingPlayModeTests.RulesTiming` | 2 | 0 | 0 |
| `20261109T150926498Z` | `DecisionTimingPlayModeTests.InspectionTiming` | 2 | 0 | 0 |
| `20261109T150944102Z` | `DecisionTimingPlayModeTests.CpuRollTiming` | 2 | 0 | 0 |

Compilation separately: open Editor reload completed and
`EditorUtility.scriptCompilationFailed == false`. Historical test-fixture compiler
errors were fixed before running tests. `git diff --check` reports only six
Unity-serialized blank-field trailing spaces in Room; the scene was saved through
Pipeline and was not rewritten to normalize them.

Failed iteration artifacts are retained. Motion setup initially attempted an invalid
move; it now uses a real legal off-ball MP animation. The old diff-3 Long Ball target
fixture's supposedly invalid target was legal; it now selects an actual out-of-bounds
hex. Committed movement restore fixtures now exercise actual committed state:
normal save guards reject it, and an explicitly applied unsupported snapshot drops
stale timing without claiming to reconstruct gameplay or accept a new action.
Earlier historical claims that those interrupted saves reopened a pass were based on
uncommitted snapshots and do not establish committed-action resumability. Supported
stable/file saves still preserve IDs/time/expiry and accept the next real input.

New service cases parameterized with roll-stage labels exercise shared timing policy;
they are not independent integration tests of every named manager. Real integration
runs cover accuracy, duels, fouls, scatter, shot/save/handling, keeper, shootout,
manual reviews, CPU dispatch, rules/inspection and supported persistence paths.
The Load Game browser/bootstrap UI and full regression suites were not executed.
Broader user-run recommendations below remain applicable. Earlier audit/results are
historical; do not interpret their roll-freezing assertions as current validation.

## Historical acceptance work

The previously pending 12-case batch compiled and passed after the user dismissed
the external-scene-modification modal by reloading Room. That modal explained the
earlier main-thread timeouts. No Pipeline upgrade/reinstall or Editor restart was used.
Loaded-scene disk whitespace normalization is now prohibited by AGENTS.md.

The following handoff gaps now have focused execution evidence:

- [x] Attacking/defending jumper-selection lists, overflow, optional goalkeeper and reselection stability.
- [x] Manual header/duel/stoppage holds and inspection/rules overlays with automatic deadlines.
- [x] Matching/expired/old timing, pending rolls, disposable file persistence and stale interrupted-phase cleanup through MatchManager rebuild.

The later remaining-test batch also closes the partial rows from the original
acceptance audit below. This is focused acceptance verification, not a fresh run of
every historical feature test or a full regression-suite verdict.

## Remaining-test batch completed (2026-11-09)

All previously identified remaining tests were implemented and run serially through
the live Pipeline on branch `feature/decision-timing-verification`. The original whole
checkout was checkpointed in `b8b4c59`; no worktree was created.

| Run | `DecisionTimingPlayModeTests` filter | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T125738637Z` | `.TargetReopening` | 16 | 0 | 0 |
| `20261109T130108279Z` | `.KeeperFollowup` | 6 | 0 | 0 |
| `20261109T130032772Z` | `.ShotFollowup` | 4 | 0 | 0 |
| `20261109T130149067Z` | `.FoulFollowup` | 2 | 0 | 0 |
| `20261109T130436448Z` | `.NonblockingScatter` | 8 | 0 | 0 |
| `20261109T130607815Z` | `.ShootoutReview` | 6 | 0 | 0 |
| `20261109T130636063Z` | `.DuelManualReview` affected regression | 4 | 0 | 0 |
| `20261109T130720761Z` | `.RealAccuracyRoll` affected regression | 4 | 0 | 0 |

Final batch scope: **42 new PlayMode cases plus 8 affected existing cases = 50 passed,
0 failed, 0 skipped**. Native XML/progress remain under
`TestResults/GameplayRegression/Runs/`. No complete suites or ordinary gameplay ran.
Compilation separately: live recompile completed with zero errors and
`compilationFailed: false`; successful Editor reloads followed in Editor.log.

Reproduced and fixed a gameplay timing defect: scatter intentionally continued while
its result panel stayed visible, but the panel still advertised a blocking review.
The original two cases failed in `20261109T125237816Z` before the fix. The panel now
marks the explicit `continueWithMovement` result as allowing continuation, then clears
that classification when recovery/new single results take ownership. The final eight
cases cover both directions, shot and ground-duel contexts, travel without dismissal,
possession credit during travel, and recovery rolls/results reclaiming their clock holds.
Blocking duel/manual and accuracy-result regressions also passed.

Target tests exercise real action switches and reopening for Standard Pass, FTP,
High Pass and Long Ball, retaining identity/time for both live and expired budgets.
High Pass rejects M while active, so those cases assert that rejection is stable and
use its supported P switch before reopening C. Initial unsupported-M fixture failures
were corrected without changing gameplay rules.

Keeper cases exercise direct hands/feet, deferred pickup after movement, catch rolls,
Quick Throw/distribution expiry, and legal input afterward. Keeper K enters the game's
built-in GoalKick clock stop: the two initially expected allowances were corrected to
assert no allowance, per the user's established rule. Shot cases use authored shooting
paths, real snapshot defender movement and actual goal-line selection, shooter/save/
handling rolls and distribution continuation, in both directions. Foul cases exercise
the real steal foul, independent card/injury deadlines, blocking final review and legal
advantage selection after expiry. Shootout cases cover immediate next-roll handoff,
independent shooter/save deadlines, and visible off-target/save results that do not
block continuation. They reuse the existing owned fixture setup/cleanup.

Fixture corrections remain visible in historical failed artifacts: shot setup now uses
an authored shooting-path origin and actual defender movement; scatter recovery is
asserted before travel because that is the actual gameplay sequence. No assertions
were disabled or gameplay rules changed to obtain passing results.

## Stoppage and persistence batch (2026-11-09)

| Run | Filter on `DecisionTimingPlayModeTests` | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T123543740Z` | `.StoppageManualReview` | 4 | 0 | 0 |
| `20261109T123616744Z` | `.FileTimingRestore` | 4 | 0 | 0 |
| `20261109T123647770Z` | `.InterruptedTimingRestore` | 4 | 0 | 0 |

All paths are under `TestResults/GameplayRegression/Runs/`; native XML and per-case
progress are retained. No full suites or ordinary gameplay ran. Compilation is separate:
live recompile completed with `compilationFailed: false`, followed by successful
reloads in Editor.log. The user-reloaded Room was clean before each test run.

Stoppage cases exercise Enter hold beyond the configured result duration, Enter resume
or Escape early dismissal, real restart callback exactly once, unchanged added-action
allocation, possession exclusion and preserved live/expired parent time. The enabled
test host suppresses only bootstrap Awake/Start/Update, retaining actual MatchManager
rule/input/coroutine behavior. Its first setup run (`20261109T123445972Z`) failed all
four cases because Unity cannot attach an Editor-folder MonoBehaviour; moving the
host to `Assets/Scripts/Testing/` under `#if UNITY_EDITOR` fixed the fixture. No gameplay
rules or assertions were changed to make these cases pass.

File tests use `RoomSaveService.SaveToPath` and `CreateLoadedRunCopy` in a disposable
directory, deserialize the resulting file, and invoke actual MatchManager snapshot
apply/rebuild. Both directions preserve remaining time/identity and expired holds,
leave the source seed unchanged, create a distinct run identity, and accept a real P
action afterward. ApplicationManager/current-save PlayerPrefs are isolated and restored
in finally; disposable files are removed. This verifies file persistence plus gameplay
snapshot application, not the Load Game browser/scene-start bootstrap UI.

Interrupted tests reach a real tackle reposition with a live or expired movement
parent. The normal save guard rejects that active action. Applying a captured snapshot
resets the unreconstructed action, drops stale parent/child timing, clears its hold and
accepts the next real pass. Interrupted actions are **not** resumable saves in the
existing gameplay save format; the timing feature does not introduce that capability.

## Original acceptance audit

| Requirement | Evidence / status |
| --- | --- |
| Every configured budget and difficulty, exact boundary | Historical 192-case EditMode batch; unchanged config/service cases were not rerun wholesale. |
| Every live gameplay decision family through real input | Focused family coverage passed: targets, movement, pass movement, headers/jumper lists, snapshot defense, GK movement/pickup/distribution, final thirds, tackle/nutmeg, shot goal-line/save/handling, foul card/injury/advantage and restart exclusions. KeeperFollowup/ShotFollowup/FoulFollowup close the last identified family gaps. |
| Reselection/invalid input does not refill | Real target, movement, complete jumper-list and TargetReopening action-switch cases passed, including live/expired allowances and rejected High Pass M input. |
| Parent/child interruption, expired phase and overlapping holds | Duel interruption/turnover/manual review plus stoppage and interrupted-snapshot batches passed. |
| Roll deadline/race/stale/pause/inspection, deterministic outcomes | Service tests plus real accuracy/header/tackle/stoppage, Rules and CPU dispatch cases passed. |
| Blocking/manual results and possession/allocation | Header/duel/stoppage/accuracy holds passed. NonblockingScatter and ShootoutReview verify both nonblocking result mechanisms and recovery boundary behavior. |
| Snapshot/older saves/new match/pending roll | Actual rebuild, old-data, pending-roll, file persistence and unreconstructed interruption cleanup passed within the supported stable-save scope. |
| Authored clock PNG, caption, announcement queue | Banner cases and live disjoint Room bounds passed; announcements take priority per later clarification. |
| No full suites/live saves/runtime-only visuals | Observed throughout; broader user-run regressions remain unexecuted. |

No focused acceptance cases from this audit remain unexecuted. The broader user-run
checks below remain unexecuted; focused tests do not establish every possible match
combination or replace a full regression result.

## Continuation batch (2026-11-09, live Editor)

Added 22 independently selectable PlayMode cases. All passed in focused serial runs;
no full suites or ordinary gameplay ran. Existing uncommitted work was preserved.
Native XML and per-case progress remain under `TestResults/GameplayRegression/Runs/`:

| Run | Filter suffix on `DecisionTimingPlayModeTests` | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T103657855Z` | `.JumperSelection` | 4 | 0 | 0 |
| `20261109T103854009Z` | `.ManualHeaderResult` | 2 | 0 | 0 |
| `20261109T103918937Z` | `.GameplayTimingRestore` | 8 | 0 | 0 |
| `20261109T104025852Z` | `.InspectionTiming` | 2 | 0 | 0 |
| `20261109T104127326Z` | `.GameplayRollRestore` | 2 | 0 | 0 |
| `20261109T104329824Z` | `.JumperContinuation` | 4 | 0 | 0 |

Compilation separately: final live `unity recompile` completed with zero errors,
`compilationFailed: false`; Editor.log contains successful reloads after the corrected
test-constructor error. `git diff --check` passed. Loaded Room remained clean.

Jumper coverage uses real landing eligibility, individual nomination/deselection,
wrong-team clicks, empty attacking confirmation, full two-player lists, select-all,
expiry with legal input, ownership and identity preservation, successive real rigged
rolls, blocking review dismissal, headed-pass continuation, defense forfeit/reopening,
winner selection and header-at-goal offer/decline, in both attacking directions.
The first continuation run (`20261109T104221624Z`) had two passing forfeit cases
and two fixture failures: x=12 has no authored heading path. Moving the fixture to
the existing x=13,z=1 heading path resolved them without changing gameplay rules.

Manual header results are held beyond their shortened configured result duration,
exclude match/possession time, and resume to the real headed-pass continuation.
Inspection opens through `InspectPreview`: physical R is consumed while decision
time continues, automatic stoppage rolls execute, stale submission is rejected,
and the pause menu suspends the deadline.

Restoration serializes `MatchManager.CreateRuntimeSnapshot()` and invokes the actual
`ApplyRuntimeSnapshot` path, including manager rebuild and token/ball restoration.
Matching and expired action choices preserve ID/time, old data initializes normally,
unsupported active movement drops stale timing, and restored gameplay accepts P.
Pending stoppage rolls retain ID, sequence and remaining time; discarded-session
time is not charged. New-match clock reset clears timing. These are in-memory
snapshots, not file-based save durability tests; no live save was modified.

Remaining acceptance combinations (do not mark the whole plan complete):

- Manual stoppage results and expired-parent preservation were subsequently verified
  in the stoppage/persistence batch above.
- Rules behavior was clarified and verified in the follow-up below.
- Interrupted parent/child rebuild and disposable-file persistence were subsequently
  verified above. Gameplay rebuild resets active movement rather than resuming it.
- Earlier historical cases were not rerun as a whole-feature verdict. A combined
  semicolon test-name filter matched zero tests, so all reported runs use single
  explicit method filters; zero discovery was not counted as a pass.

## Behavior and task checklist

## CPU, announcement separation and Rules follow-up (2026-11-09)

The user clarified that Rules opened through Pause retains that pause; Rules opened
beside instructions keeps gameplay live. `RulesViewer` no longer creates/restores a
pause itself. The authored Rules consumer blocks pointer input without globally
blocking gameplay, and Rules consumes physical keys while allowing synthetic CPU keys.
The first Rules timing run reproduced that authored modal's CPU-input blocker; the
corrected live and paused cases passed, including Rules search focus and real rolls.

CPU inspection found a one-settled-frame deferral in `ShouldDeferAutomatedExecution`,
not a fixed multi-second roll delay. Existing scheduling is retained. Two integration
cases dispatch the real synthetic `Room AI` R input before/after the five-second
fallback and verify exactly one roll, stale-ID rejection and cache invalidation.
`TimingRollSequence` now participates in the CPU decision-state hash so fallback
completion cannot reuse analysis for the next otherwise identical roll. These cases
exercise dispatch/cache behavior, not an entire autonomous CPU match.

Timing now uses the separate edit-mode-authored `DecisionTimingDisplay` sibling of
`MatchEventBanner`. Announcements always have priority: all queued events display
for their configured duration while timing is hidden, then current timing reappears.
Fourteen parameterized cases cover every event kind. The live saved Room bounds are
event `(0,-340,320,320)` and timing `(340,-340,320,320)`, a 20-unit gap; anchors match
so the displays remain disjoint as their parent changes size. No designer stats layout
was changed and no runtime visual hierarchy is created.

Additional tests cover manual duel holds beyond their configured duration, frozen parent
allowance/possession, and release preserving an independently expired parent; jumper
overflow, rejected select-all, empty clicks and optional GK selection/deselection run
in both directions. The two affected existing Room Rules navigation/search cases pass.

| Run | Focused filter | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T120841677Z` | `DecisionTimingBannerEditorTests` | 14 | 0 | 0 |
| `20261109T121013122Z` | `DecisionTimingPlayModeTests.RulesTiming` | 2 | 0 | 0 |
| `20261109T121037738Z` | `DecisionTimingPlayModeTests.CpuRollTiming` | 2 | 0 | 0 |
| `20261109T121159640Z` | `DecisionTimingPlayModeTests.DuelManualReview` | 4 | 0 | 0 |
| `20261109T121409275Z` | `DecisionTimingPlayModeTests.JumperOverflow` | 2 | 0 | 0 |
| `20261109T121441601Z` | `RulesViewerPlayModeTests.Room` | 2 | 0 | 0 |

Final results: **14 EditMode and 12 PlayMode passed, zero failed/skipped** in these
focused runs. Compilation is separate: live recompilation/reload completed, actual
`EditorUtility.scriptCompilationFailed` is false. A test-only collection assertion
compile error was corrected; its attempted run discovered zero cases and is not
counted as passing. Initial banner fixture initialization failure and the reproduced
Rules blocker remain in historical artifacts. No complete suites or ordinary gameplay ran.

The later stoppage/persistence and remaining-test batches close the named gaps.
The current acceptance audit and final focused results are above.

## Original implementation checklist

- [x] One editable budget table in `Assets/Scripts/Managers/DecisionTimingConfig.cs`.
- [x] Explicit typed gameplay registrations, independent of AI and debug instruction text.
- [x] Unscaled elapsed-time service, stable decision IDs, retained unresolved targets,
  whole-team movement allowances, nested choices and expired-phase clock holds.
- [x] Mandatory rolls share a validated manual/automatic command path and independent
  five-second deadlines. Nested registration cannot repeatedly reset the active roll.
- [x] Pause and blocking result holds suspend timing; expiry leaves input enabled.
- [x] Roll-to-result transitions refresh clock eligibility before crediting possession.
- [x] Built-in clock stoppages skip decision budgets. Set-piece/penalty positioning and
  kickoff setup have no active allowance while the game clock is already stopped.
- [x] Substitution and forced-replacement budgets removed, including emergency keeper nomination.
- [x] Optional save state retains matching identities, remaining time, expired/suspended
  states and roll deadlines. Old saves and genuinely different restored decisions initialize normally.
- [x] Persistent scoreboard clock PNG/countdown, amber warning, expired/roll/review notices,
  noninteractive existing scene bindings, edit-mode preview and preserved announcement queue.
- [x] Development warning for an awaiting-input state with no ready classification.
- [x] Focused compilation/tests; no ordinary gameplay, complete suites, or convenience additive loads.

Restart-only rows remain in the configuration table for completeness, but are not instantiated
while the game clock is stopped. No new kickoff confirmation stage was added.

## Executed evidence

Compilation: live Unity CLI reported clean scripts; the latest reload in `Logs/Editor.log`
contains no new compiler errors. The user-level Editor.log redirects to that project log.
`git diff --check` passed.

All result directories below are under `TestResults/GameplayRegression/Runs/` and contain
native `results.xml`, `progress.jsonl`, and `current-test.txt`.

| Run | Scope | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T052500290Z` | EditMode category `DecisionTiming` | 192 | 0 | 0 |
| `20261109T052555257Z` | `DecisionTimingPlayModeTests` (initial 23 cases) | 23 | 0 | 0 |
| `20261109T052718437Z` | `DecisionTimingPlayModeTests.AdditionalDecisions` | 12 | 0 | 0 |
| `20261109T052906356Z` | `DecisionTimingPlayModeTests.RealAccuracyRoll` after immediate result-hold fix | 4 | 0 | 0 |

Distinct current coverage: **192 EditMode + 35 PlayMode**, all passed; the final four cases
rerun four of the original 23 and are not four additional distinct cases.

EditMode covers every remaining configured budget at all three difficulties, exact boundaries,
reopening, parent/child accounting, deadline races, stale IDs, authority, pauses, blocking reviews,
matching/mismatched snapshot restoration, old-save defaults, built-in stops, nested roll registration,
and banner queue/PNG/warning presentation.

PlayMode uses owned deterministic boards and real manager entry points for Standard Pass,
First Time Pass, High Pass and Long Ball targets; whole movement phases; attacking/defensive
pass movement; headers; snapshot defender selection; goalkeeper box movement; final-thirds;
restart exclusions; paused automatic stoppage rolls; and manual/automatic rigged accuracy rolls
with actual blocking presentation and possession exclusion. Both attacking directions are covered
where applicable. No live save is used as a mutable fixture.

An initial 11-case run had six fixture failures (unavailable actions and advancing a token rather
than ending the team phase); corrected setups passed without changing gameplay expectations.
Historical artifacts remain retained.

## Tackle/nutmeg timing follow-up (2026-11-09)

Fixed a reproduced ownership defect: possession changes during a successful tackle or lost
nutmeg occur before repositioning completes. Timing previously reassigned the unfinished
movement phase to the new attacking/defending sides and discarded its expired hold. The phase
now captures its original attacking side when activated, retains that ownership through the
interruption, and clears it on reset. A new movement phase captures the new possession owner.

Focused filter: `DecisionTimingPlayModeTests.Duel` (includes `DuelInterruption`, `DuelDecline`,
and `DuelTurnoverKeepsExpiredPhase`). Final native result:
`TestResults/GameplayRegression/Runs/20261109T102653052Z/results.xml` — **16 passed, 0 failed,
0 skipped**. Matching per-case progress is retained beside the XML. Compilation and latest
Editor reload were clean; Unity was stopped at handoff. No full suites ran.

Coverage includes both attacking directions, tackle/nutmeg choice attribution to the team's
existing movement allowance, legal challenge declines, roll suspension, independently timed
repositions, expiry with legal input still available, retained unexpired/expired parent time,
turnover before phase completion, hold release at phase end, and fresh ownership of the next
phase. The four turnover cases failed before the fix. Initial nutmeg fixture failures were
corrected to use mandatory legal repositioning and the public destination list, preserving rules.

This adds 16 independently selectable cases to the prior 35 PlayMode cases (51 distinct cases
have passed across the focused batches); prior results were not rerun or represented as a fresh
full-feature/full-suite verdict. Remaining acceptance items are listed at the top of this file.

## Broader checks for the user

These were **not executed** as part of the feature run. The focused tests do not claim exhaustive
coverage of every existing gameplay continuation, jumper list, manual result-hold interaction,
or save/restart combination.

- `Assets/Tests/PlayMode/`: fixtures `LegacyMovementPlayModeTests`,
  `LegacyMovementScatterPlayModeTests`, `LegacyFirstTimePassPlayModeTests`,
  `LegacyHighPassMovementPlayModeTests`, `LegacyHeaderSelectionPlayModeTests`,
  `LegacyHeaderResolutionPlayModeTests`, `LegacyDuelPresentationPlayModeTests`,
  `LegacyGoalkeeperShotPlayModeTests`: shared roll dispatch, nested decisions and continuations.
- `Assets/Tests/PlayMode/Features/GoalkeeperPickup/`: `GoalkeeperPickupPlayModeTests`:
  pickup choices and goalkeeper interruptions.
- `Assets/Tests/PlayMode/Features/PossessionTime/`: `PossessionTimePlayModeTests`:
  clock eligibility and existing stoppage-action possession allocation.
- `Assets/Tests/PlayMode/Features/MatchEventBanners/`: `MatchEventBannerPlayModeTests`:
  existing transient announcements alongside persistent timing.

Because this touches shared clock and input paths, a complete regression check is warranted:
the user should press **Run All** on the EditMode and PlayMode Test Runner tabs when ready.
Codex did not start those full suites.

## Workspace notes

The clock PNG is bound in `Assets/Scenes/Room.unity`; the current loaded scene is preserved.
The required Editor `save_all` also serialized TMP font atlas/cache changes and minor TMP
serialization in Room. These were preserved rather than replacing potentially unsaved Editor
state with old files. No commit was created.
