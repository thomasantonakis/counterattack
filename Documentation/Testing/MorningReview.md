# Gameplay regression migration: morning review

Updated 2026-11-06, Europe/Athens. Full migration remains active. Existing legacy
scenarios take priority over discretionary new unit coverage.

## Verified checkpoint

- **145 of 147 legacy coroutine methods retired after equivalent coverage passed;
  2 remain retained while their native replacements are validated.** Exact per-method mappings are in [LegacyScenarioInventory.md](LegacyScenarioInventory.md).
  Generated branches and matrices are in [LegacyGeneratedBranches.md](LegacyGeneratedBranches.md).
- Latest complete PlayMode suite: **268 passed / 0 failed / 0 skipped**,
  `TestResults/GameplayRegression/Runs/20261106T145334545Z/results.xml`.
- The preceding 203/8/0 attempt expired during authored High Pass flight;
  specific frame-aware waits passed 15/0/0 in `Runs/20261106T094024161Z/results.xml`,
  then the complete 211-case rerun passed. All eight earlier failures are retained.
- Earlier 206-case attempt: **201/5/0**, `Runs/20261106T054332293Z/results.xml`.
  Four timeouts coincided with Mac sleep; one was fixture wiring, since corrected.
- Earlier complete migrated PlayMode suite: **162/0/0**,
  `Runs/20261106T035732634Z/results.xml`.
- Full EditMode result inspected: **747/0/0**,
  `Runs/20261106T144426744Z/results.xml`, executed after the kickoff AI fix.
- Tackle/reposition/consecutive-nutmeg batch after gameplay fix: **3/0/0**,
  `Runs/20261106T091307769Z/results.xml`.
- Own-goal accounting and full conceding-team reset: **1/0/0**,
  `Runs/20261106T091418718Z/results.xml`.
- Compilation is clean and reported separately. The migration document retains all
  failed attempts and corrected reruns; compilation/discovery never count as passing tests.

## Confirmed gameplay defects fixed

1. **Box entry on the final ball snap.** Ball.MoveToCell checked box entry before its
   final sub-millimeter snap. A boundary destination could enter only during that snap,
   after the last check. Controlled existing recovery case reproduced 0/1/0 in
   `Runs/20261105T222156793Z/results.xml`; four controlled variants passed after the fix,
   then full GK and EditMode suites passed. No rule changed.
2. **Consecutive nutmegs lose the new victim.** PrepareAttackerReposition could resume
   after the next nutmeg started and clear its victim. The actual reposition continuation
   already clears completed nutmeg state before offering another choice. Removed the
   stale post-yield clear. Original 015 reproduced the failure in the 2/1/0 batch
   `Runs/20261106T070141710Z/results.xml`; the same assertions passed in the 3/0/0 rerun
   above. All three actual duels, two pace per nutmeg, stuns, final McNulty steal and
   exact player/team stats remain checked.

3. **Early defending kickoff leaves No. 11 behind halfway.** The full manager-generated
   default roster reproduced this in `Runs/20261106T103004218Z/results.xml` and
   `Runs/20261106T103323492Z/results.xml`. The generic live defensive-gap safety
   priority outranked the explicit early front-four press policy. Limit that gap
   veto for actual early kickoff front-four placement commands, keeping live-play,
   goalkeeper and other safety constraints. The complete kickoff case, including the same halfway/flank assertions and actual
   prepared pass/continuation, passed `Runs/20261106T144205324Z/results.xml` (1/0/0).
   Full affected EditMode regressions then passed 747/0/0 in
   `Runs/20261106T144426744Z/results.xml`.

## Drift and fixture decisions

| Area | Decision and evidence |
| --- | --- |
| Medium ground preview, 002b/003 | `bd4347b` deliberately made difficulty 2 target-only before confirmation. Keep committed danger, rolls, possession and stats assertions. |
| Ground interception queue, 003 | `d6fb721` introduced pathInteractions as the live shrinking queue. defendingHexes retains preview candidates. Native tests check live three/two/one order. |
| Kickoff / Final Third input | `d276dfb` replaced Space/X confirmations with Enter. Native tests exercise current input. |
| Long-ball directions | `12bb02e` changed rig input from zero-based direction indexes to die faces 1–6. Translate inputs; retain exact destinations. All 030/031 and six OOB branches passed. |
| Foul inside opponent box, 012 | `6ae88a0` introduced penalty routing; old test expected a generic free kick. All four booking/injury combinations retain discipline, injury, forced-stoppage and continuation assertions, now checking PenaltyKickerSelect. Actual mandatory substitution goes through SubstitutionMenuManager confirmation. Passed 5/0/0 with 013 in `Runs/20261106T042024743Z/results.xml`. |
| Free header/control after defense forfeit, 027 | Old 027 head/control cases reused previous contested nominations and auto-selected a winner. The newer 033a legacy audit and `bb2f0db` explicitly clear those nominations and offer a fresh unchallenged choice. Native replacements retain that newer contract, click Nazef as the single winner, and preserve final target/control-roll checks for both original one/two-attacker inputs. All passed `Runs/20261106T095832439Z/results.xml` (28/0/0). |
| Regular High Pass minimum, 032a | `5ff9abb` explicitly changed six hexes to a fixed three-hex minimum in validation, preview and instructions. Native test checks 3–15 and the actual three-hex boundary, retaining invalid input, hover colors, switching, confirmation and forfeits. GK minimum stays separately configured. |
| GK dive presentation | `42982cc` intentionally uses Duel win probabilities rather than outfield xDr/xTk tie weighting. Native tests retain feet xDr/xTk, check correct dive copy and all presentation gates. Seven cases passed `Runs/20261106T035302342Z/results.xml`. |
| Tied foot duel | CompareTackleRolls directly transfers ties to inline scatter controls; other results wait for dismissal. Corrected a fixture that wrongly waited for a completed-result countdown on a tie. |
| Nested GK movement waits | isPlayerMoving can clear before parent dribble pace/highlights update. Wait for actual pace, input readiness and move registration. Counts unchanged. |
| Fresh defender after nutmeg | Raw JumpToHex updates token position/currentHex but expects caller-owned team occupancy. Old setup omitted it. Native Place marks owned-board occupancy and asserts it. Original zero-pace landing steal then passed `Runs/20261106T054158602Z/results.xml`. |
| Dangerous repeated tackle, 016 | `db459a4` makes a dangerous foul at least yellow even after a successful leniency roll. At the original position Paterson is already booked, so the correct outcome is second-yellow sending-off and a forced free kick. Preserve that original-board branch; a separate variant uses an actual first leniency roll of 1 (no initial booking) and moves Paterson through real gameplay to a front tackle position to retain the intended Play On/reposition/Gilbert win and exact-stat checks. `dff745b` also makes booked defenders force taking the foul even when later leniency shows no further card, matching the already-migrated 012 policy. No card rule is changed. Initial new-case failure: `Runs/20261106T091729449Z/results.xml` (3/1/0). Both passed `Runs/20261106T092449201Z/results.xml` (2/0/0); the legacy method is retired. |
| Throw-in 039a no-header precondition | Actual displacement puts Yaneva at (0,11), one hex from thrower (0,12). Forfeiting all optional movement still leaves a valid header receiver. Native test uses optional movement to take her beyond six hexes before asserting automatic throw-to-feet. All original restrictions/displacement/phase checks retained. Initial 4/1/0: `Runs/20261106T054119792Z/results.xml`; corrected case passed `Runs/20261106T092624454Z/results.xml`. |

- Remaining keeper batch initially passed 10/13, artifact
  `Runs/20261106T102616211Z/results.xml`. `18b25f64` deliberately removes sent-off
  keepers and retains their original hex for replacement. Old 041m expected the
  token still on the pitch; replacement assertions now require removal, no save-hex
  attraction and the original replacement destination. `41b35186` requires a keeper
  replacement pause. Physical input routes dice-modal dismissal before its pause
  guard; the synthetic API rejects paused input. The red-card fixture exercises
  the actual modal consumer, leaving that policy unchanged. The second-injury
  fixture must wait for Final Third activation after unpausing before forfeiting it,
  matching the old helper's sequencing. All 13 keeper cases then passed `Runs/20261106T102856487Z/results.xml`.

Sparse-board offside, occupied scatter paths and serialized empty collections were
fixture defects, fixed without disabling rules or relaxing destination assertions.

## Isolation and execution

- Deterministic owned boards, fresh in-memory game data and rigged dice. MatchManager
  remains inactive to avoid bootstrap/save flushes. No live save is loaded or written.
- Static roster-only fixture comes from the explicitly designated disposable Room seed,
  read only. It contains football attributes/formations; no event history, stats, runtime
  snapshots or save paths. InitializeAttributesFromRoster initializes actual tokens.
- Restore singleton/static ball, subscriptions, random, scene, material and time state.
  Helper completion tasks cancel when their host is destroyed. Enabled owners disable
  before the fixture clears MatchManager, preventing teardown highlight restoration
  against a missing singleton (013 rerun passed `Runs/20261106T041424650Z/results.xml`).
- Test Runner callbacks suppress automatic legacy startup across domain reloads and retain
  native XML/progress/current-case artifacts under `TestResults/GameplayRegression/Runs/`.
  Historical Temp artifacts were copied there; the persistent local artifact directory
  is ignored by git and survives Unity temporary-directory cleanup.
- Tests copy only three authored resolution-panel roots from Room into a test prefab.
  Source hash rejects stale copies; no production layout is edited. Stats designer subtree
  is untouched. Own-goal fixture supplies an inactive MatchStatsUI component whose UI
  Awake/Start never runs; it only receives UpdateScorersDisplay.
- macOS power log confirmed repeated Clamshell/Maintenance Sleep at 07:49:11–08:55:15 EET
  during the 201/5/0 run. One timed-out test had only 1.46 seconds of NUnit active duration
  across twelve wall minutes. Safe movement passed unchanged while awake in
  `Runs/20261106T065957436Z/results.xml`. Wait bounds use Stopwatch so system sleep does not expire them. Generic waits retain
  eight active seconds. High Pass flight is authored as 91 rendered frames, unlike the
  time-based Long Ball flight. Its specific waits also allow 120 rendered frames,
  with a separate 30-second watchdog. No assertion or gameplay animation is changed.
- These PlayMode adapters execute in the Editor Test Runner; standalone Player test builds
  are not provided by the reflection bridge to editor-side typed fixtures.

## Clarifications and remaining work

No unresolved gameplay-rule clarification currently blocks the migrated batches.
Please review the 016 decision above: a booked defender who escapes a new card
still forces taking the foul under `dff745b`. The old 016 expected Play On. I kept
the explicit implemented policy and covered the original forced-foul outcome;
the advantage variant uses a non-booking first roll. No card rule was changed.
FTP's click handler can execute without ConsumeCurrentClick, returning false despite a
valid state transition. Legacy tests did not assert that API return value. Native cases
check actual behavior. Audit accepted/rejected input reporting after existing scenarios migrate.

High Pass movement and inaccurate landing/header nominations passed 28/0/0 in
`Runs/20261106T095832439Z/results.xml`, including affected existing regressions.
Contested-header outcomes and loose-ball recovery passed 12/0/0 in
`Runs/20261106T101129607Z/results.xml`. The initial 11/1/0 run exposed a migration
assertion error: the original header-to-player contract allows Long Ball. Restored
that assertion; gameplay remained unchanged.
Defensive high-pass Header/Ball Control outcomes passed 7/0/0 in
`Runs/20261106T101346129Z/results.xml`, including the original missed-recovery
pickup with exactly one dribbler pace remaining.
Complete migrated PlayMode regression then passed 243/0/0.
Remaining keeper shot/foul audits passed 13/0/0 and six restart-preparation audits
passed 6/0/0 (`Runs/20261106T102925938Z/results.xml`). The Codex kickoff migration
failed a defending-front-line position assertion (0/1/0, `Runs/20261106T103004218Z/results.xml`);
its original scenario remains retained while diagnosis continues. Diagnostics later
showed its initial forecast already had no supported second action; the old scenario
confirmed setup without performing it. The replacement now runs the actual bounded
attacking setup commands before confirming and keeps all first-pass/continuation
assertions. The actual setup executed nine manager-approved moves, confirmed once, and the
complete unchanged first-pass/continuation assertions passed 1/0/0 in
`Runs/20261106T144205324Z/results.xml`; the legacy method is retired.
In-box High Pass free keeper move, separate rush choice, Final Thirds and header
sequence passed 1/0/0 (`Runs/20261106T103755472Z/results.xml`) and is retired.
Penalty/no-Final-Third audit exposed fixture reflection/host issues; the active owned
manager correction passed 1/0/0 (`Runs/20261106T144307468Z/results.xml`) and is
retired. No penalty rule has changed.
Full affected AI/EditMode regressions passed 747/0/0.
Original 017f/g fresh-dribbler and post-tackle recovery audits passed 3/0/0 in
`Runs/20261106T145011501Z/results.xml`. The initial 2/1/0 attempt waited for a
completed-result gate after tied-tackle distance; that presentation continues directly
into movement. Corrected the wait to observe actual Toothnail recovery and panel
release; no gameplay or outcome assertion changed.
Complete migrated PlayMode regression then passed 268/0/0 (1,015 tests including
the latest 747/0/0 EditMode run).
Original 018–022 tied-tackle scatter, direct hits, throw-in, pickup-stat and full
movement box-entry continuations passed 5/0/0 on first execution in
`Runs/20261106T160705672Z/results.xml`; all five methods are retired.
Next: affected movement regressions, then continue remaining movement loose balls, aerial/header resolutions, shooting/free-kick
branches, restart audits and stats UI. The 268 PlayMode + 747 EditMode green checkpoint
precedes this new batch; new cases remain legacy until actually passed.
Retain every unpassed legacy case. Detailed result history remains in
[GameplayRegressionMigration.md](GameplayRegressionMigration.md).

## Retry requests

The original local job queued the user-authorized 04:30 EET retry (superseding 04:25).
The message arrived; resumed inspection occurred at 05:28 EET with Pipeline ready and
Play Mode stopped. The later request was 10:30 EET; the user's explicit restart arrived
when the inspected time was already 11:10 EET, so work resumed immediately without a
competing job. Account quota resets are not inferred from requested retry times.
Old delivery files: `/private/tmp/counterattack-resume-20261106.sh`,
`/private/tmp/local.counterattack.regression-retry-20261106.plist`,
`/private/tmp/counterattack-resume-20261106.log`. No retry launches Unity, batchmode,
GUI scripting or ordinary editor_play.

Latest restart request: **16:15 EET on 2026-11-06**, superseding earlier retry times.
The next live tool inspection occurred at **16:37 EET** after the user said “wake up”;
16:15 had already passed. Resumed immediately, verified Pipeline ready and Play Mode
stopped. No new past-due timer or competing run was created.

023 and 024b2 passed 2/0/0 in `Runs/20261106T162018498Z/results.xml` and are retired.
The first attempt failed both fixtures: my extra nutmeg interruption assumed incorrect
hex adjacency (13,1 is not adjacent to Soares14,0), and a same-call distance assertion
ignored coroutine scheduling. Restored the original inputs and bounded the distance
consumption wait; no gameplay or original outcome assertion changed. Migration:126/147,21remaining.

025a/b real dribble/reposition goal entry and reset passed with affected movement
regressions14/0/0 (`Runs/20261106T162145297Z/results.xml`); retired.128/147 methods migrated,19remain.

## Shooting migration investigation (in progress)

First native31-branch execution:12/19/0, `Runs/20261106T162951849Z/results.xml`.
The originals remain retained. Confirmed production duplicate: ShotManager's logical
box-entry offers set only its local `gkWasOfferedMoveForBox`, bypassing
GoalKeeperManager's shared consumed-entry memory. Later attraction to an in-box
blocker therefore offered a second move for the same entry. Both normal/FK logical
shot offers now register through the shared eligibility/consumption method. Validation
is in progress. The second run14/17/0 (`Runs/20261106T163524410Z/results.xml`)
eliminated the duplicate waits. Remaining presentation failures were my overly narrow
assertion requiring the attached scatter layout: after a pitch handoff the existing
code legitimately opens the other authored scatter layout. Check visible controls in
either authored layout; no LooseBall gameplay change is needed.

Branch04's original expectation omitted a current GK-wall Saving opportunity after
Kuzmic moves15,-1. This behavior was introduced by `d6fb7218` (2026-05-26), not
invented during migration. Added the required missed Saving input2 before preserving
the original empty-space movement outcome. It is a Saving attempt, not an ordinary
interception; recovery/interception statistics remain separately asserted.
Branch28's direct goal is on target and records one shot on target; the old stats
oracle only marked on-target for a successful goalkeeper save. Corrected the oracle
for an unblocked goal; no scoring/statistics gameplay changed.
Corner fixture initially checked/forfeited Final Thirds before the out-of-bounds
flight finished. Replace this with a bounded wait for the actual corner Final Third
state and then kicker selection, preserving every original restart assertion.

024 snapshot goal itself passed `Runs/20261106T162750328Z/results.xml` (1/1/0).
Its companion reached the original shot-declaration endpoint; difficulty2 requires
second-S confirmation. The replacement now asserts that preview then confirms it.
The first attempt also mistakenly changed the original two-for-two attackersMoved4
expectation to0; restored4. These are migration mistakes, not gameplay drift.

Shooting rerun28/3/0 (`Runs/20261106T164010013Z/results.xml`): every remaining
failure is the original AnyOther menu expectation for Kuzmic's own-box recovery
(branches11,15,30). `bd4347bd`/`d6fb7218` (2026-05-26) explicitly route this to
Save and Hold; changed those continuation expectations, retaining named receiver,
ball location, possession, exact recovery/interception/xRecovery counts and disabled
live-action assertions. No recovery gameplay changed.

Original001 stats preview now passes as a native EditMode test:1/0/0,
`Runs/20261106T164455582Z/results.xml`. It logs the original goals/assists/cards/
injury/substitution events, validates actual authored stat values and lineup markers,
and checks every Stats rect transform, active state and component list is unchanged.
Room is opened only as a disposable preview scene, with no Awake/Start, layout calls
or live save writes. The first execution0/1/0 (`Runs/20261106T164349673Z/results.xml`)
needed the same inactive scoreboard dependency as the already-passed goal fixtures.

Generated headed-shot diagnosis: the old038 reset assertion expects Yaneva to
remain the current purposeful touch after the full goal reset. `78355f9c`
(2026-05-27) explicitly clears that chain before the conceding team's post-goal
kickoff setup. Verify Yaneva's scoring credit before reset, then verify retained
goal statistics/scorer credit and cleared ownership after reset. Do not restore a
stale attacking touch just to satisfy the old assertion. Also wire the new owned
GoalFlow/OOB dependencies into HeaderManager; its direct rushing-header goal and
off-target branches need these references, just as ShotManager does.

## Production changes introduced by this migration

These changes are separate from test setup and expectation updates. The final full
regression is still pending; intermediate green checkpoints do not validate later edits.

| File | Reason for change | Evidence |
| --- | --- | --- |
| `Ball.cs` | Assess the snapped arrival position so a short final frame cannot miss box entry. | Reproduction0/1/0 in `Runs/20261105T222156793Z/results.xml`, four controlled variants4/0/0 in `Runs/20261105T222317026Z/results.xml`; subsequent full GK and PlayMode checkpoints passed. |
| `MovementPhaseManager.cs` | Clear stale nutmeg victim before yielding into a continuation, so the old coroutine cannot erase the next nutmeg's victim. | Original015 reproduction in `Runs/20261106T070141710Z/results.xml` (2/1/0), corrected affected cases3/0/0 in `Runs/20261106T091307769Z/results.xml`; subsequent full268/0/0 passed. |
| `CodexTacticianProvider.cs` | During the existing early defending kickoff front-four setup policy only, generic live-play gap checks must not override the explicit halfway positioning policy. | Original opening-position assertion reproduced and retained; actual nine manager-approved setup moves plus first pass and continuation passed1/0/0 in `Runs/20261106T144205324Z/results.xml`, then EditMode747/0/0 and PlayMode268/0/0. |
| `ShotManager.cs` | Register logical normal/FK shot box entry through the shared keeper consumption method, preventing a second offer during attraction to an in-box blocker. | First native shooting run12/19/0 exposed duplicate waits; later28/3/0 passed every shooting branch except the three stale own-box GK menu expectations. Final rerun and existing keeper regressions remain required. |
| `GoalKeeperManager.cs` | Resolve keeper lookup within the match's registered tokens instead of selecting another scene's keeper through a global search. | Both-direction recovery/box tests and full268/0/0 PlayMode checkpoint passed. This is fixture/scene isolation support, not a change to eligibility thresholds. |
| `HelperFunctions.cs` | Cancel owner completion tasks when the host is destroyed, so interrupted test/game coroutines cannot leave async waiters alive. | Interrupted-fixture cleanup case and full268/0/0 checkpoint passed. No dice or gameplay outcome is supplied by cleanup. |

Header029 includes unfinished/manual-only branches. Its corner's explicit
`AssertTrue(false, "Break")` is a manual stop marker, not a gameplay expectation;
replace it with actual kicker-selection/setup assertions. A rushing goalkeeper
that wins the aerial challenge holds without a separate Handling roll, so the old
`gkRush=true, handled=false` corner/spill combination is inapplicable. Cover the real
non-rushing spill and both applicable rush/stay save/quick-throw/goal-kick outcomes.
The method named `LooseBall_OWN_GOAL` only rolled three natural ones and asserted no
own-goal outcome; preserve those inputs under an explicitly unfinished-case name,
then check the actual resulting restart rather than manufacturing an own goal.

## Clarification for review: tied-save goal attribution

Generated header branches02/03 reach the correct right-side goal, retain Yaneva as
purposeful touch before reset, and reset correctly. An additional personal-goal
assertion I added (not present in the legacy tests) found Yaneva.goals=0. Current
`ResolveOwnGoalToken` (`c6f84969`,2026-06-21) classifies any opposing deflecting token
as the own-goal scorer, including Kuzmic after a tied save. The legacy assertions
checked the goal/team/reset/last-touch contract, not personal goal attribution.

**Question to review:** should an on-target headed shot that enters after a tied GK
save credit the original attacker, or count as the GK's own goal under the tabletop
rules? This cannot be decided from the existing assertions. Preserve those original
assertions (check purposeful touch before the deliberate reset), assert the team
score, and defer the discretionary personal-attribution assertion. Do not change
scoring gameplay to satisfy the additional assertion. Existing own-goal tests remain
unchanged. This is a rules clarification, not an excuse to omit any original test.

Header batch33:30/3/0 (`Runs/20261106T165113194Z/results.xml`). All six jumper
save/spill exclusion/extension cases passed;038 jumper legacy method retired. All
14 existing header regressions passed. The only failures were the discretionary
personal-goal assertion described above; original team-goal, touch and reset checks
passed. The replacement retains those original checks and the scoring clarification.

Free-kick setup migration initial failures0/1/0 in `Runs/20261106T165806505Z/results.xml`
and `Runs/20261106T165917404Z/results.xml` were fixture readiness errors: the old
extra M after a pass to space is redundant (movement is already committed), and a
setup token's currentHex changes before FreeKickManager.IsResolvingSetup clears.
Preserved actual old inputs and phase/invalid-click/clearance assertions, checking
the real committed movement state and waiting for setup resolution before next input.
No free-kick gameplay changed.

The first FK branch matrix execution reached the actual natural-one off-target
cinematic, exposing a missing Camera.main in the isolated fixture. The real Room
has an authored camera. Export a camera-only test prefab from that camera in Edit
Mode and instantiate it under the owned fixture root. Do not add a gameplay null
guard, skip the cinematic or force a different outcome to hide the setup failure.

FK matrix12/1/0 in `Runs/20261106T170036296Z/results.xml`: all11 other shot
branches and the standalone full setup audit passed.035 is retired. Remaining
off-target failure is the missing authored camera, not gameplay.
The unfinished029 own-goal method actually yields a GK aerial win with rush, or
a two-outfield-player tie without rush. Its unconditional third GK header roll is
inapplicable without a nominated GK. Native cases cover both valid continuations;
no own goal is manufactured from a method that had no own-goal assertion.

FK01 after the authored camera export reached the real goal kick,0/1/0 in
`Runs/20261106T170914933Z/results.xml`: the old post-off-target touch assertion
expected Noruega, but OutOfBoundsManager deliberately clears ownership and sets the
new goal-kick taker (Kuzmic). Keep Noruega ownership checks through the actual shot
and failed-block inputs, then assert the new Away goal-kick state/taker and cleared
previous touch. The off-target xG oracle must capture FK blocker context after the
power result, at the first block, rather than before the manager gathers the wall.
No restart or expected-goals gameplay changes are required.

FK01 corrected off-target case passed1/0/0 (`Runs/20261106T171139863Z/results.xml`),
including exact0.556 xG from the actual gathered blocker context. All12 FK shot
branches have now passed and036 is retired;132/147 retired,15retained. The camera
export preserved Room SHA256 `BE602B8B79E96EEB56873D17EE1948F557B85C237752250112EB22579703CD01`.

Original header batch19/1/0 (`Runs/20261106T171358848Z/results.xml`), then the
corrected spill case1/0/0 (`Runs/20261106T172004243Z/results.xml`). Its direction
flag clears one coroutine frame before the distance flag is raised; wait for the
meaningful distance state, preserving the assertion. All20 applicable original
header cases passed and eleven029 methods are retired.143/147 retired; remaining
four methods are the two024 snapshot continuations and the034/038 generated matrices.
Next: current full PlayMode regression, then complete EditMode and final runner removal.

Both024 snapshot cases passed2/0/0 (`Runs/20261106T172140032Z/results.xml`) and
are retired.145/147 retired; only generated034 shooting and038 header matrices
remain pending current full regression. The unchanged pure GameStatusSnapshot data
helper moved to Assets/Editor/LegacyGameStatusSnapshot.cs so native fixtures no
longer depend on any type defined in GameTestScenarioRunner. No runner scenario is
called by a native test. Starting the complete current PlayMode suite next.

Full current PlayMode suite requested through live Pipeline with no filter after
clean Unity compilation (0errors,0warnings). No asset/script edits while this run
is active. Inspect native XML and all failures before retiring either matrix.
After successful full validation, remove the obsolete DummyLoader Tester component
and runtime runner script; the pure snapshot helper is already independently in
Assets/Editor. Then compile and execute complete EditMode plus affected cleanup
checks. Preserve the current user's Room scene hash and all live saves.

Active unfiltered PlayMode run: `TestResults/GameplayRegression/Runs/20261106T172436176Z/`.
At the latest inspection160 cases had passed with no failures; this is progress,
not a completed verdict. Wait for its results.xml before any asset/script changes
or competing run. The prepared, unexecuted post-success editor cleanup snippet is
`/private/tmp/retire_gameplay_runner.cs`; it removes only DummyLoader's obsolete
Tester component/root, preserves Room hash/dirty state, then deletes the runner asset.
Do not run it unless every relevant replacement in the full native result has passed.

## User decision: consistent defensive-scatter own goals

The user resolved the earlier tied-save attribution question: a rushing GK who
ties the best attacking aerial score is treated like an outfield defender tying
that score or a player blocking a shot. If their scatter enters their own goal,
it is their own goal, with a team goal for the opponent and no attacker personal
goal. Tied Saving scatters use the same attribution. Existing ResolveOwnGoalToken
already implements this. The previously added Yaneva.goals=1 assertion was the
incorrect expectation; no scoring gameplay is changed. Add explicit scorer-record
and personal-stat assertions, including no-deflection attacker-goal counterexamples.
See GameplayReview.md and FutureSessionPrompt.md for the consolidated review.

The full PlayMode run completed362/0/0 in
`Runs/20261106T172436176Z/results.xml`. A final original-manager-state assertion
audit and scoped authored-panel fingerprint check are being executed separately.
The first new off-target assertion incorrectly assumed FinalThirdManager was already
active while its goal-kick preparation coroutine was still moving the keeper; that
is a fixture readiness assumption, not a production gameplay defect.
