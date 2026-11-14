# Gameplay regression migration

Online goals/kickoff add six independently selectable cases in category
`OnlineGoals`, fixture `OnlineGoalPlayModeTests`, under
`Assets/Tests/PlayMode/Features/OnlineGoals/`. All six passed (0 failed/skipped)
in `Runs/20261114T095649920Z/results.xml`; compilation separately clean.
These extend online coverage rather than replace legacy scenarios. See
OnlineGoals/Verification.md for reproductions, fixture timing correction and
broader user-run kickoff coverage. The subsequent offline kickoff check and
builds could not start because Editor commands timed out after restoring Room;
Pipeline discovery still responds. No whole suites ran.

Online outfield blocks add 10 independently selectable PlayMode cases, all passed
with zero failed/skipped and compilation separately clean. Includes late input
around the existing one-frame delay, cancellation before dice, misses/saves,
successful scatter and holds. OnlineShots now has 40 distinct passing outcomes
across focused runs; see Verification.md for exact scope and retained XML.

Ordinary online shots add 14 cases to OnlineShots, bringing its focused category
to **30 passed**, zero failed/skipped, compilation separately clean. Reproduced
missing selection, disabled-owner continuation, empty post-pass menus and Final
Third preview priority before fixes. Manual controls 7 and offline ShotFollowup 4
also pass. Exact XML and unexecuted blocking/goal paths are in the feature
Verification.md; no complete suites ran or legacy scenarios removed.

Online headed shots add 16 independently selectable PlayMode cases in category
OnlineShots with passing final outcomes. Missing save routing, disabled-authority
continuation and jackpot-enabled numeric handling were reproduced and fixed.
Two fixture tie cases now finish their real rebound before teardown; their
focused reruns pass without suppressing logs. Offline save-floor 4 and retained
jumper/scatter 6 also pass; compilation separately clean, no complete suites.
See OnlineShots/Verification.md for exact XML, mixed initial runs and pending paths.

Online loose balls add 18 independently selectable PlayMode cases, category
OnlineLooseBall, all passed with zero failed/skipped and compilation separately
clean. Mandatory-action routing and cancelled-result replacement races were
reproduced before fixes. Existing LegacyHeaderResolutionPlayModeTests also passed
12 cases. See OnlineLooseBall/Verification.md for XML and remaining boundaries;
no legacy cases removed or complete suites run.

Online keeper distribution adds 14 independently selectable PlayMode cases,
category OnlineKeeperDistribution. All pass with zero failed/skipped; compilation
separately clean. Includes real catches, both-team quick throws, Final Third
drop/kick targeting, holds and stale/guest/replay rejection. Six missing-choice
failures were reproduced before implementation. See the feature Verification.md
for native XML, fixture correction and unexecuted branches. No legacy removals
or complete-suite runs.

Online header integration adds 10 EditMode + 40 PlayMode cases in category
OnlineHeaders. Four result-hold failures were reproduced before the fix; the
final focused feature runs pass with zero failed/skipped, compilation separately
clean. Existing offline ManualHeaderResult (2) and wire/shared-pause (11 EditMode)
also pass. No legacy cases were removed and no complete suites ran. The feature
Verification.md records actual XML and still-unexecuted header branches.
Keeper back-pass and landing interception extensions reproduced missing commands,
wrong dice actors and cancellation null references before fixes. Final focused
coverage includes all 40 cases with unchanged dice thresholds and offline rules.
Affected LegacyHeaderResolutionPlayModeTests passed all 12 after correcting its
shared inaccurate-header fixture to enter legal open play before requesting C.
The initial failures and final XML are recorded in OnlineHeaders/Verification.md;
all downstream assertions remain enabled.

Online high-pass integration adds 21 independently selectable native PlayMode
cases (category OnlineHighPass), with compilation clean and zero failed/skipped
in `Runs/20261113T214426081Z/results.xml`. LegacyHighPassBoxSequencePlayModeTests
was retained and passed (1 case) in `Runs/20261113T214249988Z/results.xml`. Its
fixture now enters open play through the normal match phase API before pressing
C: StartMatch correctly offers only a ground pass at kickoff. The original box
entry, keeper move/rush, F3 and header assertions are preserved. See the
OnlineHighPass feature verification for the initial failure and affected scope.

Restart selection feedback adds 24 native cases for stopped throw-in targeting,
post-header MP commitment, corner HP yellow targets and F3 orange hover. Final
focused runs passed these 24 plus 3 existing high-pass selection and 8 F3 timing
cases. Two high-pass fixtures used obsolete kickoff availability; setup now enters
the legal completed-MP state through the real domain entry point. Assertions and
gameplay rules are preserved. No legacy tests removed or full suites run; see
Assets/Editor/FeatureTests/RestartSelectionFeedback/TASKS.md for native evidence.

Seven reported issues add DecisionTimingReportedIssuesPlayModeTests: 8 snapshot
slot cases, 8 F3-motion cases, 4 tackle ownership/pulse cases and 4 immediate-keeper
penalty cases. The exhausted/taken snapshots reproduced double advancement before
the fix. Final focused scope including affected presentation/shot regressions passed
17 EditMode + 48 distinct PlayMode cases, compilation separately clean. No legacy
scenarios were removed and no full suites ran. DecisionTiming/Verification.md records
the exact source log, failure diagnoses and durable native artifacts.

Mid-dribble timing adds 12 native PlayMode cases in
DecisionTimingLiveClockPlayModeTests.DribblePaceChoice. Six outside-box failures
reproduced the missing choice allowance before the fix; all 12 passed afterward.
Latest focused verification: 17 EditMode + 44 distinct PlayMode cases passed, compile
separately clean, no legacy removals/full suites. See DecisionTiming/Verification.md.

Decision-timing concurrent-choice correction adds four EditMode accounting cases,
three EditMode choice-display cases and two PlayMode snapshot-panel cases. Final
per-case outcomes across focused runs: 38 EditMode + 45 PlayMode passed; no legacy
scenarios removed and no full suites run. Verification.md records native evidence.

Decision-timing live-clock revision (2026-11-09): 17 new isolated EditMode cases and
21 new real-input/coroutine PlayMode cases were added in the DecisionTiming feature
folders, fixtures `DecisionTimingLiveClockEditorTests` and
`DecisionTimingLiveClockPlayModeTests`. Together with affected existing filters,
33 EditMode + 136 PlayMode passed, zero failed/skipped in final runs. Compilation
was separately clean. No legacy scenario was removed and no full suites ran.
See `Assets/Editor/FeatureTests/DecisionTiming/Verification.md` for mappings,
iteration diagnoses, exact XML/progress and supported-save limitations. Current
roll/review timing supersedes historical freeze assertions in earlier evidence.


## Additive Long Ball Pace ordering coverage — 2026-11-08

`LongBallGoalkeeperPacePlayModeTests` adds dedicated native coverage under
`Assets/Tests/PlayMode/Features/LongBallGoalkeeperPace/`, using the existing owned
GoalkeeperBoxEntryFixture board through a new partial in
`Assets/Editor/FeatureTests/LongBallGoalkeeperPace/`. See that feature's
Verification.md for native XML, actual counts, fixture corrections and limits.
Existing LegacyLongBallFixture and LegacyLongBallBoxFixture expectations now put
Pace before landing recovery. They were not retired or automatically executed;
broader legacy/OOB/box-entry verification remains user-run. This is additive
behavior coverage, not another whole-suite migration checkpoint.


## Headed back-pass and extra-time follow-up — 2026-11-07

Dedicated new feature fixtures cover direct teammate headers to the keeper after
open play/corners, hands/feet choice, ordinary catch die, scatter/held continuation,
outside-box exclusion and cancellation/input restoration. Landing-ZOI preview
checks preserve jumper/stun/suppression exclusions; the existing runtime landing
interception flow was retained. The bundled rulebook matches the digital decisions,
including 16/18 squads with exactly two goalkeepers and cap 3 in extra time.

`ExtraTimeStoppageEditorTests`: **16/0/0**, native XML
`Runs/20261107T210821974Z/results.xml`.
`HeadedBackPassEditorTests`: **11/0/0**, native XML
`Runs/20261107T210952817Z/results.xml`.
Final headed-pass PlayMode: **26/0/0** at
`Runs/20261107T211031868Z/results.xml`.
Initial headed-pass PlayMode checkpoint: **22/0/0** at
`Runs/20261107T210333178Z/results.xml`; final expanded results follow in the
feature checklist. Do not add overlapping checkpoints together.

These are new feature cases, not retirement mappings for legacy scenarios.
Compilation is checked separately through the open Editor. A concurrent kickoff
fixture's NUnit collection-assertion syntax briefly blocked compilation and was
corrected without changing its expectation. Broader fixtures remain user-run;
see the two dedicated feature Verification.md files. Delayed pickup is deferred
to `GoalkeeperPickupFutureSessionPrompt.md`; direct header catches do not implement
its provenance, persistence or phase-end choice.


## Shared attribute score floor — 2026-11-07

Numeric scores now use `max(current injured attribute + signed modifiers, 0) +
dice`, via GameplayScore. Resolution, xStats/AI odds and UI guidance agree; accepted
jackpots stay 50 and natural-one misses/fouls remain. GK injury reductions now
include Aerial/Saving/Handling. Existing penalty amounts and the shooting -2 cap
remain; the user confirmed inclusion of difficult-angle hexes and the cap at 2.

| Focused filter | Passed | Failed | Skipped | Native XML under `TestResults/GameplayRegression/Runs/` |
| --- | ---: | ---: | ---: | --- |
| AttributeScoreFloorEditorTests | 23 | 0 | 0 | `20261107T194634274Z/results.xml` |
| AttributeScoreFloorPlayModeTests | 14 | 0 | 0 | `20261107T194704757Z/results.xml` |

Native Editor compilation flags are clean. Reproduction, initial fixture failures
and corrections are retained in the dedicated feature Verification.md, including
the GK injury defect. These are new focused results, not full-suite validation.
Broader existing fixtures and complete EditMode/PlayMode checks are handed to the
user. No legacy migration retirement or deferred wiki-gap work was resumed.

## Outside-box keeper header correction — 2026-11-07

The earlier implementation incorrectly counted an outside keeper as an outfielder.
Keeper identity now retains the separate nomination and third panel row regardless
of position. Only the ability changes: outside-own-box Heading 0 receives no further
distance penalty. Actual rolls, panel guidance and projected AI attributes agree.
Both attacking directions are reproduced with a real HP, rush commitment, Select
All, three player rolls and a winning outside-keeper headed-pass continuation.

| Filter | Passed | Failed | Skipped | Native XML under `TestResults/GameplayRegression/Runs/` |
| --- | ---: | ---: | ---: | --- |
| New nomination reproduction, before fix | 0 | 2 | 0 | `20261107T151011856Z/results.xml` |
| New nomination cases, after fix | 2 | 0 | 0 | `20261107T151140321Z/results.xml` |
| ApprovedRuleDecisionsEditorTests, final | 18 | 0 | 0 | `20261107T151402292Z/results.xml` |
| OutsideKeeperHeaders PlayMode category, final | 12 | 0 | 0 | `20261107T151429305Z/results.xml` |

Live compilation is clean. An intermediate 17/1/0 EditMode result was a rulebook
prose-spacing serialization mismatch, corrected without relaxing the exact-text
assertion. The feature checklist retains its artifact path. Runs overlap and must
not be added as unique cases. No broad fixture or complete suite was executed.

## Approved wiki decisions — 2026-11-07

This is a separate approved rule-correction batch, not migration resumption.
Contracts are recorded in RulesWikiAudit and the checklist at
`Assets/Editor/FeatureTests/ApprovedRuleDecisions/Verification.md`.
Compilation through the live Editor is clean. Only the dedicated feature filters
were executed; no complete suite was started.

| Filter / stage | Passed | Failed | Skipped | Native XML under `TestResults/GameplayRegression/Runs/` |
| --- | ---: | ---: | ---: | --- |
| ApprovedRuleDecisionsEditorTests, final | 15 | 0 | 0 | `20261107T145108743Z/results.xml` |
| ApprovedRuleDecisionsPlayModeTests, first green checkpoint | 35 | 0 | 0 | `20261107T144818634Z/results.xml` |
| ApprovedRuleDecisionsPlayModeTests, final prompt assertion follow-up | 31 | 4 | 0 | `20261107T145149138Z/results.xml` |
| OwnBoxKeeperUsesNormalCards, corrected assertion timing | 4 | 0 | 0 | `20261107T145407375Z/results.xml` |

The final four failures were a fixture assertion at token selection rather than
the tackle-roll state where leniency guidance is displayed. Its expected text and
gameplay/card assertions were retained. All 35 distinct PlayMode cases are green
after that focused retest; do not add overlapping runs as unique cases. Initial
reproductions and fixture corrections are recorded in the feature checklist.
Shared discipline, aerial continuation and AI checks warrant broader user-run
regressions; the checklist names exact paths/fixtures. Legacy 034/038 retirement,
the earlier premature off-target readiness assertion and scorer-event coverage
remain pending. Further wiki gaps are deferred by user instruction.

## Rules controls and Room layout correction — 2026-11-07

User-reported follow-up: reading history now records manual scroll checkpoints;
search uses TMP submit/cancel events for Enter-as-Next and Escape-as-Clear.
The pause-menu Rules row is positioned above Back to Main Menu, and Room's direct
button is a sibling above InstructionsPanel with navigation disabled. MatchStatsUI
layout is not changed.

| Filter | Passed | Failed | Skipped | Native XML under `TestResults/GameplayRegression/Runs/` |
| --- | ---: | ---: | ---: | --- |
| RulesViewerEditorTests | 11 | 0 | 0 | `20261107T101432316Z/results.xml` |
| RulesViewerPlayModeTests | 3 | 0 | 0 | `20261107T101453780Z/results.xml` |
| RoomPresentationEditorTests | 13 | 0 | 0 | `20261107T101232653Z/results.xml` |

Total: **27 passed / 0 failed / 0 skipped**. Compilation through live Pipeline
is clean. The PlayMode coverage now raycasts and dispatches pointer events to the
history buttons, checks manual-scroll restoration, submits through TMP's native
submit interface and cancels through TMP's Escape/deactivation path. It also
checks Room pause restoration. The scene tests check separate row geometry and
the direct button's parent, position and disabled navigation. The first pointer
reproduction exposed a test-cleanup EventSystem restore error; that is fixed and
the earlier failing artifact is retained.

Isolated UI previews in `Runs/RulesViewerVisuals/room-pause-layout-fixed.png` and
`room-instructions-layout-fixed.png` were inspected. Capture cameras now cull to
the UI layer so unrelated production stadium geometry cannot obscure the preview.
No full gameplay suites or ordinary gameplay were started for this UI correction.

## Rules viewer validation — 2026-11-07

This UI batch preserves all legacy gameplay scenarios. No gameplay rule changes
were made. The rulebook's penalty-advantage wording was corrected with user approval.

Compilation through the live Editor completed with **0 errors / 0 warnings**.
Compilation is a separate gate from these executed tests:

| Filter | Passed | Failed | Skipped | Native XML under `TestResults/GameplayRegression/Runs/` |
| --- | ---: | ---: | ---: | --- |
| RulesViewerEditorTests | 11 | 0 | 0 | `20261107T090903421Z/results.xml` |
| RulesViewerPlayModeTests | 3 | 0 | 0 | `20261107T094550929Z/results.xml` |
| RoomPresentationEditorTests | 13 | 0 | 0 | `20261107T090930523Z/results.xml` |
| MovingTokenHoverEditorTests | 8 | 0 | 0 | `20261107T091134015Z/results.xml` |
| ActionSequenceEditorTests | 14 | 0 | 0 | `20261107T094341862Z/results.xml` |

Total for these filters: **49 passed / 0 failed / 0 skipped** (46 EditMode + 3
PlayMode). This is not a full gameplay-regression run or proof of wiki/game parity.
Earlier failed rulebook runs are retained; hidden text indexing was corrected
before the final passing PlayMode runs.

Coverage includes both scenes' persistent button wiring, shared prefab ownership,
document/link validation, authored diagrams/result rows, phrase search and wraparound,
actual inline-link pointer navigation, history, reopening, Room input ownership and
pause/time-scale restoration. Tests use an isolated additive scene and inactive
match stub, restore singleton/session state, and do not read or write live saves.
The framework gate suppresses the legacy runner.

Visual captures at 1600×900, 1280×720 and 1024×768 are retained under
`Runs/RulesViewerVisuals/`. Toolbar and article captures were visually inspected.
The broader rule comparison and pending decisions are in `Documentation/RulesWikiAudit.md`.

2026-11-07 Room presentation follow-up: scene/prefab authoring and live compilation
completed. Focused native Room checks passed: 13 EditMode and 22 PlayMode (including
preview double-click and shutdown follow-ups, with a corrected fixture rerun), with
25 captured frames. The user explicitly requested that Codex leave the full suites
for them; no full suites were run for this task. See
[RoomPresentationImplementation.md](RoomPresentationImplementation.md) for artifacts
and the final visual follow-up.

Updated 2026-11-06. **Handoff requested; do not run full suites again.** Read
[SessionHandoff.md](SessionHandoff.md) for the current pending state. Original objective: migrate the full legacy suite and execute every replacement. Prioritize the user's existing scenarios; discretionary new unit coverage waits until all legacy scenarios are migrated. Compilation and discovery do not count as passing tests.

## Inventory

[LegacyScenarioInventory.md](LegacyScenarioInventory.md) preserves the original 147-method inventory and per-method replacement/artifact mapping. **145 methods have been retired after passing replacements; 2 remain retained for a final common-assertion audit. Their native matrices passed in the complete362/0/0 PlayMode run, but the later strengthened audit is unfinished.** All 002–008 ground/FTP/commitment/cache scenarios are now migrated. [LegacyGeneratedBranches.md](LegacyGeneratedBranches.md) enumerates 31 shooting, 12 free-kick shot, 6 OOB long-ball and 13 header-at-goal definitions, plus runtime matrices.

| Legacy groups | Focus | State |
| --- | --- | --- |
| 001 | stats/UI preview | Migrated and executed, including authored-layout preservation |
| 002–008 | ground pass, FTP, commitment/input and caches | All migrated and executed; legacy methods retired |
| 009–024 | movement, steals, tackles, nutmegs, fouls | 009–022 base flows and all 017b–g audits migrated; 023 and 024b2 loose-ball continuation audits migrated; 025a/b goal entry and both024 loose snapshot flows migrated |
| 025–033 | aerial/header, loose balls, long ball | 026–028 aerial/header resolution, 030–031 long balls and 032–033 audits migrated; all029 original header-at-goal flows migrated; generated038 matrix retained |
| 034–040 | shooting, set pieces, OOB, throw-ins, clock | 037 OOB, 039 throw-ins and 040 penalty cleanup migrated; all035/036 FK cases migrated; generated034 shooting and038 header matrices retained |
| 041–042 | GK challenges, recovery/scatter, restart policy | All keeper wall/outside challenge, shot, foul and restart audits migrated and executed |

Before migration, Assets/Editor contained 23 test source files for tactical decisions, saved-position reconstructions and UI rules. Relevant existing regressions must also execute after each migration batch.

## First batch: goalkeeper box entry

Native Test Runner fixtures: GoalkeeperBoxEntryEditorTests (8 rules) and GoalkeeperBoxEntryPlayModeTests (121 flow/cleanup cases). The latter covers both directions and move/skip decisions. All have passed actual execution.

| Legacy behavior | Replacement cases |
| --- | --- |
| Eligibility, direction, duplicate memory/rearm, previously moved keeper | EditMode rule fixture |
| Ground pass / FTP / dribble / reposition | ground, ftp, dribble, reposition |
| Nonqualifying own-box/outside destinations and same-box possession change | own_box, outside, same_box_recovery |
| Accurate/inaccurate aerial resolved box entry | high, long, high_inaccurate_*, long_inaccurate_* |
| Successful recovery attraction | ground/ftp/steal/loose/header/long_recovery |
| Direct long-ball defender landing / own-box loose attraction | long_direct, loose_own_box |
| Won tackle attraction and reposition | tackle_stay, tackle_reposition |
| GK loses outside-box feet/hands challenge then box reposition | lost_foot, lost_dive |
| Post-roll tackle/header scatter and blocked-shot pre-roll entry | *_scatter_inside/outside |
| Required interrupted cleanup | InterruptedFixtureReleasesAsyncWaitersAndRestoresState |

Scatter cases roll actual direction/distance and assert no flight before the GK decision. Recovery cases assert possession timing and final continuation. GK movement uses the real synthetic input dispatcher. Some ground/FTP recovery cases still enter below winning-roll/pass-selection UI; those layers remain retained legacy coverage. Aerial cases observe the next rush/pace decision; the separate phase remains legacy coverage.

## Isolation and result reporting

- Deterministic additive PlayMode and preview EditMode boards, in-memory match data, and no Room bootstrap/live save writes.
- Restore singleton/static ball, input subscriptions, random state, active scene, owned objects/materials and scene unload, including failure cleanup.
- Resolve GK lookup within this match's registered tokens. Populate manager dependencies only; gameplay selection computes cached hex state.
- Cancel helper completion tasks when their host is destroyed.
- TestRunner callbacks suppress legacy startup across domain reloads and write native NUnit XML, current-test.txt and progress.jsonl under TestResults/GameplayRegression/Runs/.
- Meaningful-state waits use eight active seconds (Stopwatch excludes OS sleep); authored High Pass flight also allows 120 rendered frames with a 30-second watchdog and board diagnostics. No arbitrary test sleeps.
- A narrow reflection adapter connects the PlayMode asmdef to strongly typed editor fixtures without reorganizing Assembly-CSharp.

## Remaining branch setup strategy

The031 native shooting matrix uses the exact final roster positions from its shared
legacy two-movement preparation, then enters through the real movement-ended action
menu, two-step S confirmation, aiming, actual blockers, GK choice, dice and scatter.
The pass/pickup/movement rules in that preparation already have separate passing
replacements. No shot availability or expected outcome is set by fixture flags.
The FK matrix executes the original full pass/pickup/tackle foul and every setup
phase before aiming. Header matrices execute actual High Pass, defensive movement,
accuracy, box-entry GK move, optional rush, Final Thirds, nominations and aerial rolls.
The six jumper cases preserve their original direct resolved-header-to-shot handoff
input (power10) and exercise the real Saving/Handling/scatter pipeline.

The stats preview logs the original events and updates production-authored fields
inside a disposable preview scene, asserting unchanged Stats geometry/components.
Off-target shot cinematics use a camera-only prefab authored in Edit Mode from Room;
no camera or other test visuals are built only at runtime.

## Actual executed results

| Run | Passed | Failed | Skipped | Artifact under TestResults/GameplayRegression/ |
| --- | ---: | ---: | ---: | --- |
| EditMode initial scene setup | 0 | 8 | 0 | 2026-11-05/editmode-run-2.json |
| EditMode initial material cleanup | 0 | 8 | 0 | 2026-11-05/editmode-run-3.json |
| EditMode after fixture fixes | 8 | 0 | 0 | 2026-11-05/editmode-run-4.json |
| PlayMode ground-pass smoke | 1 | 0 | 0 | 2026-11-05/playmode-smoke-results.json |
| PlayMode initial 89 cases | 56 | 33 | 0 | 2026-11-05/playmode-batch-1-results.json |
| PlayMode after shared setup fixes | 85 | 4 | 0 | Runs/20261105T220205004Z/results.xml |
| Expanded PlayMode 113 cases | 112 | 1 | 0 | Runs/20261105T220727020Z/results.xml |
| Failed loose-recovery case in isolation | 1 | 0 | 0 | Runs/20261105T221214815Z/results.xml |
| Deterministic snap-boundary reproduction before fix | 0 | 1 | 0 | Runs/20261105T222156793Z/results.xml |
| Four controlled loose-recovery variants after fix | 4 | 0 | 0 | Runs/20261105T222317026Z/results.xml |
| Complete goalkeeper batch after gameplay fix | 113 | 0 | 0 | Runs/20261105T222435645Z/results.xml |
| Full EditMode regressions, including 008d replacements | 741 | 0 | 0 | Runs/20261105T222802989Z/results.xml |
| Initial ground-pass migration | 1 | 3 | 0 | Runs/20261105T223452943Z/results.xml |
| Ground-pass after setup/queue corrections | 3 | 1 | 0 | Runs/20261105T223911223Z/results.xml |
| Complete 121 GK cases and four ground-pass cases | 125 | 0 | 0 | Runs/20261105T224030584Z/results.xml |
| Initial FTP batch plus ground/clock (clock 5/0/0) | 9 | 7 | 0 | Runs/20261105T224501147Z/results.xml |
| FTP phase/recovery batch after input observation fix | 7 | 0 | 0 | Runs/20261105T224649921Z/results.xml |
| Ground/FTP/kickoff/clock combined | 18 | 0 | 0 | Runs/20261105T224825714Z/results.xml |
| GK blocking and halfway kickoff interception rules | 4 | 0 | 0 | Runs/20261105T225043686Z/results.xml |
| Expanded ground/FTP/kickoff/clock, including switching and forfeit | 22 | 0 | 0 | Runs/20261105T225200803Z/results.xml |
| Full EditMode regressions after all ground/FTP retirements | 745 | 0 | 0 | Runs/20261105T225430660Z/results.xml |
| Initial full movement/steal migration | 1 | 2 | 0 | Runs/20261105T225638636Z/results.xml |
| Movement after nested-GK continuation wait correction | 2 | 1 | 0 | Runs/20261105T225844146Z/results.xml |
| Full EditMode including GK wall and mixed-save selection | 747 | 0 | 0 | Runs/20261106T032921061Z/results.xml |
| Expanded legacy replacements including movement and GK flows | 27 | 0 | 0 | Runs/20261106T033049755Z/results.xml |
| Initial long-ball batch (legacy zero-based scatter input drift) | 2 | 4 | 0 | Runs/20261106T033402132Z/results.xml |
| Long-ball batch after die-face translation | 6 | 0 | 0 | Runs/20261106T033602040Z/results.xml |
| Authored duel bindings (014b) | 1 | 0 | 0 | Runs/20261106T034501169Z/results.xml |
| First authored GK challenge batch | 7 | 1 | 0 | Runs/20261106T035025479Z/results.xml |
| GK challenge cases after tie handoff correction | 7 | 0 | 0 | Runs/20261106T035302342Z/results.xml |
| Complete migrated PlayMode suite, including authored cleanup | 162 | 0 | 0 | Runs/20261106T035732634Z/results.xml |
| Expanded Legacy batch, including 031a–c and 013 | 44 | 1 | 0 | Runs/20261106T040847595Z/results.xml |
| Foul Play On after teardown-order fix | 1 | 0 | 0 | Runs/20261106T041424650Z/results.xml |
| Fixed and generated OOB long-ball cases | 9 | 0 | 0 | Runs/20261106T041600573Z/results.xml |
| Nutmeg selection and stationary input matrix | 8 | 0 | 0 | Runs/20261106T041752093Z/results.xml |
| Foul Play On and four taken-foul combinations | 5 | 0 | 0 | Runs/20261106T042024743Z/results.xml |
| Combined Legacy including new High Pass | 69 | 1 | 0 | Runs/20261106T053048343Z/results.xml |
| Combined Legacy after High Pass correction, with six nutmeg foul branches and header selection | 77 | 1 | 0 | Runs/20261106T053532210Z/results.xml |
| Initial throw-in migration | 4 | 1 | 0 | Runs/20261106T054119792Z/results.xml |
| Corrected nutmeg landing occupancy | 1 | 0 | 0 | Runs/20261106T054158602Z/results.xml |
| Complete PlayMode attempt, interrupted by Mac sleep | 201 | 5 | 0 | Runs/20261106T054332293Z/results.xml |
| Safe movement rerun while awake | 1 | 0 | 0 | Runs/20261106T065957436Z/results.xml |
| First 014/015/017 tackle continuation migration | 2 | 1 | 0 | Runs/20261106T070141710Z/results.xml |
| Later complete EditMode result inspected | 747 | 0 | 0 | Runs/20261106T074339803Z/results.xml |
| Tackle continuation after stale victim cleanup fix | 3 | 0 | 0 | Runs/20261106T091307769Z/results.xml |
| Own-goal accounting/reset after fixture wiring | 1 | 0 | 0 | Runs/20261106T091418718Z/results.xml |
| Initial 016 repeated-foul migration | 3 | 1 | 0 | Runs/20261106T091729449Z/results.xml |
| Repeated-foul branch split, first attempt | 4 | 1 | 0 | Runs/20261106T092152380Z/results.xml |
| Original forced foul and corrected advantage-chain inputs | 2 | 0 | 0 | Runs/20261106T092449201Z/results.xml |
| Complete migrated PlayMode; High Pass frame-budget timeouts | 203 | 8 | 0 | Runs/20261106T092624454Z/results.xml |
| High Pass affected cases after authored-frame wait correction | 15 | 0 | 0 | Runs/20261106T094024161Z/results.xml |
| Complete migrated PlayMode after gameplay and wait corrections | 211 | 0 | 0 | Runs/20261106T094138266Z/results.xml |
| Complete EditMode after gameplay fix and all current migrations | 747 | 0 | 0 | Runs/20261106T095113728Z/results.xml |
| High Pass movement / inaccurate landing / header nominations plus affected regressions | 28 | 0 | 0 | Runs/20261106T095832439Z/results.xml |
| Initial contested header / scatter batch (incorrect migrated Long Ball availability assertion) | 11 | 1 | 0 | Runs/20261106T101004696Z/results.xml |
| Contested header / scatter after restoring the original availability assertion | 12 | 0 | 0 | Runs/20261106T101129607Z/results.xml |
| Defensive high-pass header/control, recovery and failed-recovery pickup | 7 | 0 | 0 | Runs/20261106T101346129Z/results.xml |
| Complete migrated PlayMode including all current header/control replacements | 243 | 0 | 0 | Runs/20261106T101512527Z/results.xml |
| Initial remaining keeper audits (replacement policy / Final Third setup corrections pending) | 10 | 3 | 0 | Runs/20261106T102616211Z/results.xml |
| Remaining keeper audits after fixture/policy corrections | 13 | 0 | 0 | Runs/20261106T102856487Z/results.xml |
| Restart preparation (corner, offside, goal-kick clearance, goalkeeper taker) | 6 | 0 | 0 | Runs/20261106T102925938Z/results.xml |
| Initial Codex kickoff front-line migration, diagnosis pending | 0 | 1 | 0 | Runs/20261106T103004218Z/results.xml |
| Codex kickoff, full decision diagnostics confirm generic gap safety overrides halfway | 0 | 1 | 0 | Runs/20261106T103323492Z/results.xml |
| Codex kickoff after front-line fix, later unprepared-continuation assertion fails | 0 | 1 | 0 | Runs/20261106T103604106Z/results.xml |
| Codex kickoff continuation diagnostics (initial forecast already unsupported) | 0 | 1 | 0 | Runs/20261106T103957563Z/results.xml |
| In-box High Pass free GK move / rush / Final Third / header sequence | 1 | 0 | 0 | Runs/20261106T103755472Z/results.xml |
| Restart + initial penalty cleanup migration (ambiguous reflection overload) | 6 | 1 | 0 | Runs/20261106T103821253Z/results.xml |
| Restart + penalty cleanup after overload correction (inactive coroutine host) | 6 | 1 | 0 | Runs/20261106T144021815Z/results.xml |
| Codex kickoff after actual attacking setup and narrow front-line policy fix | 1 | 0 | 0 | Runs/20261106T144205324Z/results.xml |
| Penalty/no-Final-Third cleanup after owned-host correction | 1 | 0 | 0 | Runs/20261106T144307468Z/results.xml |
| Full EditMode regressions after kickoff AI correction | 747 | 0 | 0 | Runs/20261106T144426744Z/results.xml |
| Initial 017f/g movement recovery audits (incorrect completed-result wait after inline scatter) | 2 | 1 | 0 | Runs/20261106T144849353Z/results.xml |
| Movement recovery audits after meaningful inline-scatter wait correction | 3 | 0 | 0 | Runs/20261106T145011501Z/results.xml |
| Complete migrated PlayMode including keeper/restart/kickoff and 017f/g | 268 | 0 | 0 | Runs/20261106T145334545Z/results.xml |
| Full 018–022 movement scatter, direct hits, throw-in and defensive pickup statistics | 5 | 0 | 0 | Runs/20261106T160705672Z/results.xml |
| Relevant existing movement + recovery + new scatter regressions | 12 | 0 | 0 | Runs/20261106T161008784Z/results.xml |

The four residual failures above rolled through the original dribbler; gameplay correctly stopped at that occupied intermediate hex. The fixture now uses unobstructed outward SW/SE rolls and retains exact-location assertions. No collision rule was changed or assertion relaxed.

Latest live Unity recompile: zero errors and warnings. An import-order error in the result reporter was corrected and recompiled before the current run. Earlier Pipeline timeouts and rejected CLI flags are infrastructure events, not gameplay test verdicts.

Ground-pass phase replacements for 002/002b/003/004 passed in LegacyGroundPassPlayModeTests. Current kickoff and ignored-input replacements passed in LegacyKickoffPlayModeTests. Seven FTP selection/movement/recovery cases passed in LegacyFirstTimePassPlayModeTests. Five restart-clock cases passed in LegacyRestartClockPlayModeTests. These legacy methods have been retired. Medium preview, live queue and kickoff-key drift decisions are in MorningReview.md.

Action switching, movement forfeit, FTP continuation and GK blocking replacements also passed and were retired. Movement 009–011, GK wall/save-push and selected outside-GK shot rules also passed and were retired. Next: finish the current whole-suite run, then continue remaining movement duels, aerial/header resolutions, shooting and set-piece groups. Stats/UI coverage remains retained.

| Affected movement regressions after 018–022 | 12 | 0 | 0 | Runs/20261106T161008784Z/results.xml |
| Initial 023/024b2 box continuation (fixture mistakes) | 0 | 2 | 0 | Runs/20261106T161508887Z/results.xml |
| Corrected box continuation fixtures | 2 | 0 | 0 | Runs/20261106T162018498Z/results.xml |

| Movement regressions including dribble/reposition goal entry and real reset | 14 | 0 | 0 | Runs/20261106T162145297Z/results.xml |

| Initial 024 snapshot goal/decline fixtures (incorrect migrated count) | 0 | 2 | 0 | Runs/20261106T162612880Z/results.xml |
| 024 snapshot goal passed, decline fixture confirmation pending | 1 | 1 | 0 | Runs/20261106T162750328Z/results.xml |
| First shooting smoke (incorrect completed gate after block) | 0 | 1 | 0 | Runs/20261106T162643888Z/results.xml |
| First complete31 shooting branches; duplicate offer exposed | 12 | 19 | 0 | Runs/20261106T162951849Z/results.xml |

| Shooting after duplicate fix; remaining presentation/Final Third fixture corrections | 14 | 17 | 0 | Runs/20261106T163524410Z/results.xml |
| Formerly duplicated in-box blocker after fixture correction | 1 | 0 | 0 | Runs/20261106T163922645Z/results.xml |
| Shooting; only obsolete own-box GK recovery menu expectations remain | 28 | 3 | 0 | Runs/20261106T164010013Z/results.xml |
| Stats preview initial missing scoreboard fixture dependency | 0 | 1 | 0 | Runs/20261106T164349673Z/results.xml |
| Stats preview authored values/markers and layout preservation | 1 | 0 | 0 | Runs/20261106T164455582Z/results.xml |
| Initial generated header-at-goal; post-goal ownership, fixture wiring and corner waits diagnosed | 6 | 7 | 0 | Runs/20261106T164533996Z/results.xml |

| Header33: six jumper cases and all14 existing regressions passed; three added attribution assertions pending rules review | 30 | 3 | 0 | Runs/20261106T165113194Z/results.xml |
| First original FK setup; redundant M acceptance fixture assertion | 0 | 1 | 0 | Runs/20261106T165806505Z/results.xml |

| FK setup waits needed manager resolution, not only token position | 0 | 1 | 0 | Runs/20261106T165917404Z/results.xml |
| Original FK setup and12 shooting branches; missing camera in off-target fixture | 12 | 1 | 0 | Runs/20261106T170036296Z/results.xml |

| FK01 camera supplied; obsolete post-restart shooter-touch expectation exposed | 0 | 1 | 0 | Runs/20261106T170914933Z/results.xml |
| FK01 actual goal-kick ownership and exact stats, after fixture/oracle corrections | 1 | 0 | 0 | Runs/20261106T171139863Z/results.xml |

| Original header goal/save/distribution/preparation cases; one same-frame spill fixture assertion | 19 | 1 | 0 | Runs/20261106T171358848Z/results.xml |

| Original header spill after state-wait correction; all20 original header cases now passed | 1 | 0 | 0 | Runs/20261106T172004243Z/results.xml |

| Both original024 loose-ball snapshot continuations after input/readiness corrections | 2 | 0 | 0 | Runs/20261106T172140032Z/results.xml |

## Review and continuation

[MorningReview.md](MorningReview.md) records decisions, drift, clarifications and the verified local retry for November 6 at 04:30 Europe/Athens. Never start a competing Test Runner job. Retire legacy scenarios only after equivalent coverage passes; renaming the runner or wrapping unisolated legacy scenarios is insufficient.

PlayMode tests currently use an editor-side typed fixture through a small reflection
adapter. They execute in the live Editor Test Runner; standalone Player test builds
are not provided by this adapter. Presentation fixtures use an EditMode-exported
copy of Room's authored panel roots and check its source hash. Regenerate in stopped
Edit Mode through CLI eval: `return FrameworkAuthoredPanelAssets.Export();`.

## Handoff checkpoint

Complete current PlayMode:362passed/0failed/0skipped,
`Runs/20261106T172436176Z/results.xml`. Last complete EditMode747/0/0 remains
`Runs/20261106T144426744Z/results.xml`;748 are discovered, not all748 re-executed.
Final strengthened Shot audit was cancelled at the user's handoff request:15passed,
1failed, no final XML (`Runs/20261106T214114486Z/`). The new failure is a premature
Final Third activation assertion before keeper goal-kick preparation completes.
No production gameplay changed in that audit. Preserve pending test-only changes
and read SessionHandoff.md before any new work. Do not rerun full suites automatically.

### Shootout roll panel flow — 2026-11-07

Feature-only PlayMode run: **6 passed / 0 failed / 0 skipped**.
Fixture: `ShootoutRollPanelPlayModeTests`, under
`Assets/Tests/PlayMode/Features/ShootoutRollPanel/`; setup/checklist under
`Assets/Editor/FeatureTests/ShootoutRollPanel/`. Both attacking directions cover
immediate keeper prompts, off-target countdowns, shot animation during result
countdowns, retained panel position and owner-disable cleanup. Unity compilation
passed separately with zero errors/warnings. Native evidence:
`TestResults/GameplayRegression/Runs/20261107T112014489Z/results.xml`.
No broad gameplay suite was run for this feature; wider shared-panel checks are
handed to the user under the focused-test workflow.

### Shootout UI feedback — 2026-11-07

Feature-only native PlayMode run: **6 passed / 0 failed / 0 skipped**.
Fixture: `ShootoutUiFeedbackPlayModeTests` in
`Assets/Tests/PlayMode/Features/ShootoutUiFeedback/`. Covers decisive kick stakes
with the roll prompt, preview-card refresh and hover replacement across contestant
changes, and natural-one off-target text without a power total. Native evidence:
`TestResults/GameplayRegression/Runs/20261107T120318565Z/results.xml`.
Final Press [R] copy was compiled after this run; no further tests were run for
that wording-only edit. The substitution tool's automatic reload authoring was
removed; a controlled script reload kept Room clean and compilation error-free.
Full suites were not run. Manual checks and optional wider fixture are listed in
`Assets/Editor/FeatureTests/ShootoutUiFeedback/TASKS.md`.

### Shootout rendered instruction visibility — 2026-11-07

Fixed the final display rule that blanked the instruction TextMeshPro element while
the duel roll panel was visible, despite a correct instruction snapshot. Shootouts
now retain that text; ordinary overlay visibility rules are preserved. Four focused
native cases passed (0 failed/skipped), checking the actual text setter during roll
prompts and countdowns in both shootout and ordinary modes. Compilation was clean.
Fixture: `ShootoutInstructionVisibilityPlayModeTests`, under
`Assets/Tests/PlayMode/Features/ShootoutInstructionVisibility/`.
Evidence: `TestResults/GameplayRegression/Runs/20261107T121440378Z/results.xml`.

# Goalkeeper pickup feature — 2026-11-08

New dedicated feature coverage, not retirement of legacy cases:
`Assets/Editor/FeatureTests/GoalkeeperPickup/` and
`Assets/Tests/PlayMode/Features/GoalkeeperPickup/` map direct header feet,
outside recovery/return, nearby collection and successful movement completion to
`GoalkeeperPickupEditorTests` and `GoalkeeperPickupPlayModeTests`.
Clean live compilation; 14/0/0 final EditMode at
`Runs/20261108T092457421Z/results.xml`. All 41 distinct PlayMode feature cases
passed across `Runs/20261108T091321442Z/results.xml` (36/3/0),
`Runs/20261108T091759094Z/results.xml` (6/1/0) and
`Runs/20261108T092026689Z/results.xml` (1/0/0). These overlap; do not add counts.
The feature Verification.md explains fixture corrections and exact broader
user-run coverage. No legacy scenario was retired and no whole suite was run.
