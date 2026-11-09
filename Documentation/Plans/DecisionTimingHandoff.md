# Decision timing — next-session handoff

## Latest continuation

The user saved timing/event panel positions: preserve Room as authored; do not
rerun AlignLoadedTimingDisplays. Timer now shows immediately by default; adjust
CountdownDisplaySpentFraction (0f immediate, 0.5f halfway) in DecisionTimingConfig.cs.
All mid-dribble move/X choices now have the short allowance, even without S being
available. 17 EditMode + 44 PlayMode distinct focused cases passed, compile clean
separately; native evidence is first in Verification.md. No full suites ran.

## Subsequent clarification verified

The current rule gives snapshot/forfeit-pace and tackle/nutmeg choices their own
10/10/5 allowance while charging the owning MP concurrently. Choice countdown
wins at its own halfway point; repositioning retains parent suspension. Snapshot
resolution suppresses the next-mover pulse. Latest focused verification: 38 EditMode
and 45 PlayMode distinct cases passed, compilation separately clean. Full suites
remain unexecuted. Verification.md records failures/reruns and exact artifacts.
Room is loaded, stopped and clean. Earlier MP-only choice wording below is historical.

## Latest revision handoff

The user's subsequent rules are implemented and focused verification passed:
33 EditMode + 136 PlayMode cases, zero failed/skipped in final runs; compilation
separately clean. Difficulties 1–2 now share selection time across previews until
commitment; difficulty 3 commits on the action key (Long Ball fixed). Rolls/automatic
reviews and MP movement keep clock time running; manual Enter holds/pause suspend.
Snapshot/forfeit uses MP time. Countdown starts at half spent; decision/events are
centered under HOME/AWAY with announcement priority. See the first section of
Verification.md and current rules in DecisionTimingImplementation.md.

Historical roll-freeze/preview-budget and interrupted-save claims below are superseded.
Committed mid-action gameplay is not a supported resumable save; normal guards reject
it and restore cleanup drops stale timing. Supported stable/file saves are verified.
Full suites and Load Game browser/bootstrap remain unexecuted; no full regression
verdict is claimed. Continue on the existing branch, without a worktree.

## Previous status (historical)

All identified focused acceptance gaps have been implemented and verified. The final
remaining-test batch passed 42 new PlayMode cases and 8 affected existing cases, with
zero failed/skipped in the final runs. Compilation is clean. A reproduced nonblocking
scatter result-clock defect was fixed and both continuing and blocking review paths
were tested. Current results/audit are in
`Assets/Editor/FeatureTests/DecisionTiming/Verification.md`.

Work continues in the same checkout on `feature/decision-timing-verification`, branched
from master; `b8b4c59` checkpoints the entire original tracked/untracked work. No
worktree was created. Do not restart the old remaining-work list below: it is retained
as historical context. Broader/full-suite regression checks remain user-run and were
not executed by Codex. All prior user clarifications remain in the approved plan.

## Historical handoff

Prepared 2026-11-09. Project: `/Users/t.antonakis/Documents/Projects/Personal/counterattack`.

## Objective and actual status

Finish implementing and verifying the approved decision-timing plan, preserving the user's
clarifications below. The core implementation exists, and the tackle/nutmeg follow-up is complete.
**The whole plan is not yet verified complete.** An earlier response overstated completion;
`DecisionTimingImplementation.md` now correctly marks acceptance verification as in progress.

Start with the remaining jumper-selection coverage, then manual result holds/inspection overlays,
then real gameplay save/load restoration. Reproduce and fix any defects those tests expose.
Reconcile the complete original acceptance checklist before claiming the whole plan is done;
the outstanding items listed here are known gaps, not proof that all other combinations work.

Read:

1. Repository `AGENTS.md`.
2. `Documentation/Plans/DecisionTimingImplementation.md` — approved requirements and clarifications.
3. This handoff.
4. `Assets/Editor/FeatureTests/DecisionTiming/Verification.md` — detailed scope and retained results.
5. `Documentation/Testing/RunningTests.md` — test workflow.

## User decisions that must persist

- **Do not run full suites.** Run only new/affected feature cases with explicit fixture, test-name,
  or category filters. Do not run every existing regression as a substitute.
- When the game itself has already stopped the match clock, there is no need for decision budgets.
  This includes kickoff, set-piece positioning, penalty repositioning, and other built-in clock stops.
- No substitution or forced-replacement budget, including emergency goalkeeper nomination.
  Those two rows were removed from the implementation's enum/table.
- Keep an interrupted live-play allowance's remaining time; do not refill it after a temporary stop.
- Mandatory rolls still auto-submit after five eligible real seconds, including during built-in
  clock stoppages. Gameplay pauses suspend their deadlines.
- Expiry holds the match clock and keeps legal input available indefinitely. No forced tactical
  choices, inactivity penalty, thinking reserve, networking, or competitive sanctions.
- Preserve existing result durations, early dismissal, and indefinite manual result holds.
- Keep all timing numbers editable in `DecisionTimingConfig.cs`.
- No separate kickoff-confirmation stage was added. Restart-only configuration rows remain dormant
  while the underlying game clock is stopped; do not reactivate them merely because the original
  historical table lists them.

## What has been implemented

### Timing core and gameplay integration

- `Assets/Scripts/Managers/DecisionTimingConfig.cs`: typed `DecisionKind` enum and one budget table
  for difficulties 1–3, plus automatic-roll and countdown-warning constants.
- `DecisionTimingService.cs`: injected elapsed-time accounting, begin/end, suspend/resume, stable
  IDs, current state, expired holds, pending rolls, authority switch, snapshot capture/restore,
  and retained unresolved choices. Time is independent of the accelerated match clock.
- `DecisionTimingRegistration.cs` and `DecisionTimingRegistrations.cs`: explicit typed registrations
  owned by partial gameplay-manager classes. They do not parse instructions or depend on AI/debug
  monitor activity. Actor/preview changes within the same registered decision do not refill time.
- `MatchManager.DecisionTiming.cs`: builds registrations, advances time, validates shared roll
  submissions, and warns in development when an awaiting-input state lacks a ready classification.
- `MatchManager.cs`: timing holds participate in `CanAdvanceMatchClock()` without changing
  `Time.timeScale`, disabling input, or overwriting `isClockRunning` to implement expiry.
- Built-in stopped-clock decisions do not create/charge budgets. Whole-team movement phases retain
  their allowances through token changes and separately timed child choices.

### Rolls, clock accounting and interruption fixes

- Manual/AI manager roll handlers and automatic requests share the validated timing submission path;
  existing manager handlers still perform the actual dice, overrides, rule evaluation and continuation.
- Independent identities/deadlines for successive rolls; stale/racing submissions are rejected.
  Automatic commands bypass physical keyboard focus and inspection input routing.
- Roll waits and blocking result panels hold match time and suspend decision budgets. Nonblocking
  presentations keep their existing continuation semantics.
- Fixed nested roll registration resetting the active child's deadline every frame: registrations
  collect the selected roll first and apply it once after priority resolution.
- Fixed the roll-to-result transition so clock eligibility refreshes immediately after execution,
  before the same frame can credit possession time during a newly opened blocking result.
- **Latest fix:** a successful tackle/lost nutmeg changes possession before repositioning ends.
  Timing used to reassign the unfinished movement phase to the new sides and prematurely release
  an expired hold. `MovementPhaseManager` now captures its original attacking team at activation,
  keeps it through the interruption, and clears it on reset. The next phase captures the new owner.
  See `timingPhaseAttackIsHome` in `DecisionTimingRegistrations.cs` and activation/reset hooks in
  `MovementPhaseManager.cs`.

### Presentation, persistence and documentation

- `Assets/Scripts/UI/MatchEventBannerController.cs`: persistent countdown, amber final-five-second
  warning, expired/automatic-roll/review notices, and suspension/resumption of transient announcements.
- `Assets/Art/UI/DecisionClock.png` is bound to the existing banner in `Assets/Scenes/Room.unity`.
  No runtime-only visual hierarchy was added; the banner offers an edit-mode timing preview.
  `Assets/Editor/DecisionTimingAuthoring.cs` authored/bound the PNG.
- Optional `RoomRuntimeSnapshot.decisionTiming` and `timingRollSequence` added through
  `RoomSaveService.cs` and MatchManager snapshot creation/restoration. Matching timing state can be
  restored; missing data initializes normally. **Real gameplay restoration is still a verification gap.**
- Expiry, hold-release and automatic-roll events include owner and decision identity.
- Feature tests/checklists and RunningTests documentation were added. No commits were created.

## Executed tests and evidence

Compilation was clean at the last handoff. Live Unity reported ready, not compiling, Play Mode
stopped. The user-level Editor.log redirects to this project's `Logs/Editor.log`; no new compiler
errors followed the latest reload. Recheck live state at the start of the next session.

All paths below are relative to `TestResults/GameplayRegression/Runs/`. Each run retains native
`results.xml` and per-case progress. Counts are historical focused results, **not** one fresh
whole-feature or full-suite run on the latest source.

| Run directory | Executed scope | Passed | Failed | Skipped |
| --- | --- | ---: | ---: | ---: |
| `20261109T052500290Z` | EditMode category `DecisionTiming` | 192 | 0 | 0 |
| `20261109T052555257Z` | Initial `DecisionTimingPlayModeTests` cases | 23 | 0 | 0 |
| `20261109T052718437Z` | `.AdditionalDecisions` | 12 | 0 | 0 |
| `20261109T052906356Z` | `.RealAccuracyRoll` after immediate result-hold fix | 4 | 0 | 0 |
| `20261109T102653052Z` | `.Duel` filter after phase-owner fix | 16 | 0 | 0 |

The accuracy rerun repeats four of the initial 23 cases. Across the batches, **192 distinct
EditMode and 51 distinct PlayMode cases have passed**. No full suites ran.

Existing coverage includes every configured budget/difficulty and exact boundaries; service-level
races, pauses, snapshot matching and parent/child accounting; banner queue/PNG behavior; real pass
selection and movement, headers, snapshot defender input, goalkeeper box movement, final-thirds,
restart exclusions, stoppage rolls, and real rigged accuracy rolls/results/possession accounting.

The latest 16 duel cases exercise both attacking directions, tackle/nutmeg choice ownership,
declining challenges, paused parent time during rolls and child repositioning, live/expired parent
restoration, possession changes before phase completion, hold release when the phase ends, and
fresh ownership of the next phase. Four turnover cases failed before the production fix.
Initial nutmeg fixture errors were corrected to perform mandatory repositioning; nutmeg winners
cannot simply press X to stay put. Gameplay rules/assertions were not weakened.

## Remaining work: recommended order

### 1. Complete jumper-selection sequences

- Add independently selectable tests in the existing DecisionTiming feature folders.
- Exercise real HeaderManager attacking/defending jumper selection, full lists, individual
  selection/deselection, confirmation and invalid input, in both attacking directions.
- Assert one allowance for each team's whole list, correct owner, unchanged identity/remaining time
  through reselection, and release only when that decision actually ends.
- Verify transitions into rolls and subsequent headed-pass/header-at-goal choices. Cover cancellation
  or reopening where the gameplay actually supports it; do not invent a new rule or stage.
- Useful existing fixtures: `Assets/Editor/LegacyHeaderSelectionFixture.cs` and the
  `LegacyHeader*` PlayMode fixtures. Inspect actual filenames/methods before reuse.

### 2. Manual result holds and inspection/rules overlays

- Use the authored panel fixture pattern. Test Enter-to-hold, remaining held beyond the normal
  result duration, resumption/early dismissal, and the real continuation.
- Assert both clock and parent-budget exclusion throughout blocking reviews. Clearing a review must
  preserve an independently expired parent/other hold.
- Open inspection/rules overlays through their real supported entry points. Verify decision time
  continues and automatic rolls execute despite keyboard focus, while a real pause menu suspends them.
- Exercise successive/stale roll identities and deterministic dice outcomes without GUI key simulation.
- Service tests and real accuracy results already pass; these additional interaction combinations
  have not been fully exercised.

### 3. Real gameplay snapshot restoration

- Drive actual MatchManager capture and restore/rebuild entry points using owned fixtures and
  in-memory or disposable temporary saves. Never mutate live user saves.
- Verify the restored gameplay genuinely resumes the decision whose timing is restored; a serialized
  service-only round trip is insufficient.
- Cover matching/different decisions, old saves, new-match cleanup, pending roll time, suspended
  parent/child decisions, and expired phases. Time outside the match must not be charged.
- Inspect the latest captured movement-owner field and reconstruction behavior specifically: it is
  runtime manager state, while the saved timing identities/owners live in the timing snapshot.
  Do not assume those paths align without testing them.
- Fix any demonstrated integration gap rather than altering saved state/fixture flags to supply the
  expected answer. If gameplay restoration cannot resume a decision, verify that stale timing is dropped.

### 4. Reconcile and hand over accurately

- Compare the original plan's acceptance requirements with actual implementation and test evidence.
- Update `Verification.md`, `TASKS.md`, the original plan status and RunningTests when claims change.
- Report compilation separately from actual new passed/failed/skipped counts and artifact paths.
- List any remaining unexecuted coverage explicitly. Do not call the whole plan done because the
  core exists or because selected tests passed. Do not start full suites.

## Test structure and workflow

- EditMode: `Assets/Editor/FeatureTests/DecisionTiming/`.
- PlayMode wrapper: `Assets/Tests/PlayMode/Features/DecisionTiming/DecisionTimingPlayModeTests.cs`.
- Fixture implementation: `DecisionTimingFixture.cs` and `DecisionTimingDuelFixture.cs`, partial
  extensions of `GoalkeeperBoxEntryFixture` under the Editor feature folder. The wrapper uses
  reflection to access the Editor-owned fixture and restores state through its Cleanup method.
- Reuse owned singleton/event/scene/time/random cleanup and legacy-runner suppression.
- For presentation tests, reuse `AttachAuthoredPanels()` / `EnableForAuthoredPresentation()`.
  Do not manually subscribe a manager again after enabling it normally. The exported test panels
  validate their source fingerprint; don't bypass that validation.
- Serial runs only. Resolve any pending execution before edits/recompile/another run.
- Wait for compilation/reload, inspect every loaded scene's dirty flag, save dirty scenes through
  CLI `save_all`, and recheck clean before each run. Do not overwrite unsaved scene state.
- CLI example (use the actual new case filter):

```sh
unity command run_tests --mode playmode \
  --filter DecisionTimingPlayModeTests.Duel --async_tests true \
  --project-path /Users/t.antonakis/Documents/Projects/Personal/counterattack \
  --caller codex --skill unity-cli --format json
```

This example selects only the 16 duel cases; do not rerun them unnecessarily for unrelated additions.
A `running` response is not a verdict. Inspect completion and retained native results.

Use the live Unity CLI/Pipeline for Editor operations. Never launch Unity, use batchmode,
`editor_play`, GUI scripting, kill Unity, or open scenes additively for convenience. Follow AGENTS.md
for connection failures, stopping Play Mode, compile errors and any modified-scene dialog.
The installed CLI was `1.0.0-beta.17` during this work; check the current version as the skill requires.
Analytics requests frequently hit the sandbox allowlist; retry the same CLI call with approved
escalation before treating Pipeline as unavailable. This installation used `editor_status`,
`save_all`, `eval`, and `run_tests`; `unity pipeline list` had been renamed, so discover current
commands instead of assuming an old command is supported. Do not upgrade Pipeline to reconnect.

## Workspace preservation

The implementation and tests are uncommitted, with many new files still untracked. Inspect the
current diff/status and preserve all existing work. Room has the authored clock binding. Required
Editor saves also serialized TMP font/cache changes. Additional font assets changed during the
user's later Editor activity. Do not reset or discard those files or infer that every dirty file
belongs to this task. Preserve whichever scene is currently loaded; the last known loaded scene
was Room, but that is not an instruction to switch scenes.
