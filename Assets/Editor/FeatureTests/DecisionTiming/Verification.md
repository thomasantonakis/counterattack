# Decision timing verification

Core implementation added 2026-11-09; acceptance verification remains in progress. The user's implementation-time clarifications are recorded in
`Documentation/Plans/DecisionTimingImplementation.md`.

## Remaining acceptance work

The previously pending 12-case batch compiled and passed after the user dismissed
the external-scene-modification modal by reloading Room. That modal explained the
earlier main-thread timeouts. No Pipeline upgrade/reinstall or Editor restart was used.
Loaded-scene disk whitespace normalization is now prohibited by AGENTS.md.

The earlier handoff marked the whole plan complete too broadly. The following focused
verification is still outstanding; none requires a full suite:

- [x] Attacking/defending jumper-selection lists, overflow, optional goalkeeper and reselection stability.
- [x] Manual header/duel/stoppage holds and inspection/rules overlays with automatic deadlines.
- [x] Matching/expired/old timing, pending rolls, disposable file persistence and stale interrupted-phase cleanup through MatchManager rebuild.

The three known handoff gaps now have focused evidence. This does not establish every
original acceptance combination: see the acceptance audit below. Whole-plan status
remains in progress.

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
| Every live gameplay decision family through real input | Partial: targets, movement, pass movement, headers/jumper lists, snapshot defense, GK box movement, final thirds, tackles/nutmegs and restarts covered. Dedicated combined timing assertions still needed for GK pickup/distribution and shot goal-line/save/foul follow-ups. |
| Reselection/invalid input does not refill | Real target, movement and complete jumper-list cases passed. Actual target cancellation/reopening remains service-level evidence; add manager input integration. |
| Parent/child interruption, expired phase and overlapping holds | Duel interruption/turnover/manual review plus stoppage and interrupted-snapshot batches passed. |
| Roll deadline/race/stale/pause/inspection, deterministic outcomes | Service tests plus real accuracy/header/tackle/stoppage, Rules and CPU dispatch cases passed. |
| Blocking/manual results and possession/allocation | Header/duel/stoppage holds and immediate accuracy-result hold passed; existing nonblocking presentation semantics have not had a dedicated timing integration assertion. |
| Snapshot/older saves/new match/pending roll | Actual rebuild, old-data, pending-roll, file persistence and unreconstructed interruption cleanup passed within the supported stable-save scope. |
| Authored clock PNG, caption, announcement queue | Banner cases and live disjoint Room bounds passed; announcements take priority per later clarification. |
| No full suites/live saves/runtime-only visuals | Observed throughout; broader user-run regressions remain unexecuted. |

Remaining work is the partial rows above. Do not equate closing the three handoff gaps
with verifying every original gameplay family or every presentation combination.

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

Whole-plan acceptance remains in progress. The later stoppage/persistence batch closes
those named gaps; the acceptance audit above records the remaining integration rows.

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
