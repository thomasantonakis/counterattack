# Online shots

- [x] Real header goal-line declaration, challenge and keeper-save command.
- [x] Disabled authority owner, save/handling result holds, cancellation and deadlines.
- [x] Handling, possession/distribution and rebound continuation, both directions.
- [x] Ordinary shot/snapshot selection, confirmation, aiming and snapshot defender movement.
- [x] Post-pass action choices and interrupted preview validation.
- [x] Outfield shot blocks, delayed-roll admission, cancellation and scatter continuation.
- [ ] General keeper choices.
- [ ] Goals, restarts and statistics through actual match flow.
- [ ] Focused tests, compilation and refreshed builds.

Paired acceptance remains deferred. This checklist tracks partial progress;
it does not claim complete shot or full-match internet play.

Compilation separately clean. Category `OnlineShots`: **30 PlayMode passed,
zero failed/skipped**, in
`TestResults/GameplayRegression/Runs/20261114T082408043Z/results.xml`.
This includes 16 headed-shot cases and 14 ordinary-shot/snapshot cases.
Ordinary coverage includes real preview/cancel/confirmation, legal goal-line
targeting, actual snapshot defender movement, shooter/save/handling rolls,
numeric power/statistics, automatic deadlines, post-pass action choices, lost
ball possession and Final Third preview interruption, all in both directions.

The subsequent outfield-block extension adds **10 passed**, zero failed/skipped,
in `Runs/20261114T083951023Z/results.xml`, fixture `OnlineShotBlockPlayModeTests`.
This brings the feature to **40 distinct cases with passing outcomes across
focused runs**; the preceding 30-case run was not repeated for this extension.
Block coverage includes misses through shooter/save/handling, successful blocks
through real scatter, automatic roll admission, late manual requests, overlapping
holds and cancellation before the one-frame dice draw, all in both directions.
The initial 10 missing-block-stage failures remain in
`Runs/20261114T083734057Z/results.xml`. The authority now claims the pending block
before the existing frame delay; version checks prevent cancelled rolls and old
continuations from affecting replacement state. Offline timing and thresholds
are unchanged.
Affected `LegacyGoalkeeperShotPlayModeTests`: **1 passed**, zero failed/skipped,
in `Runs/20261114T084139593Z/results.xml`. Compilation is separately clean;
the Editor is stopped and the loaded Room scene is clean at this checkpoint.
Further user-run coverage when resuming: category OnlineShots (40 cases) under
`Assets/Tests/PlayMode/Features/OnlineShots/` and fixture
`LegacyFreeKickShotPlayModeTests` in `Assets/Tests/PlayMode/LegacyFreeKickShotPlayModeTests.cs`,
because free kicks share the block coroutine. These broader checks were not run
for the block extension; the user requested a break after this phase.

Reproductions for the ordinary batch: 8 missing-start failures in
`20261114T074345707Z/results.xml`; 8 disabled-owner shooter continuations in
`20261114T081008014Z/results.xml`; 2 empty post-pass menus in
`20261114T081521984Z/results.xml`; 2 passes / 2 Final Third priority failures in
`20261114T082237069Z/results.xml`. Supported action availability now considers
movement/shot/long/high choices independently of ground-pass availability.
Final Third owns input ahead of shot previews; typed input also preserves
existing extra-roll, free-kick and final-extra-movement restrictions.

Affected manual controls: **7 passed** in `20261114T082609343Z/results.xml`.
Existing offline `DecisionTimingPlayModeTests.ShotFollowup`: **4 passed** in
`20261114T082715704Z/results.xml`. No complete suites ran.

Earlier headed-shot checkpoint (native XML under the same Runs directory):

- `20261113T230140267Z/results.xml`: 14 passed; 2 tied-save cases failed in
  teardown because the fixture ended with the async save owner still awaiting
  rebound completion. The tie cases now consume real direction/distance commands
  and wait for both shot and loose-ball owners to finish; no logs are suppressed.
- `20261114T073036527Z/results.xml`: corrected Home-direction tie, **1 passed**.
- `20261114T073113960Z/results.xml`: corrected reverse-direction tie, **1 passed**.

These exercise actual header goal-line choice, defensive nomination, challenge
rolls, goalkeeper save, numeric handling, held-ball distribution or real rebound,
host roll contexts, guest/wrong-team rejection, automatic deadlines, overlapping
holds and cancelled save/handling results. Tied rebounds finish through open play.

Reproductions retained: `20261113T225512897Z` (12 missing-save routing failures),
`20261113T225717949Z` (2 passed / 10 failed: disabled authority owner could not
continue after save), and `20261113T225946002Z` (2 passed / 10 failed: 8 incorrect
jackpot-enabled handling observations and 2 fixture tie-score assumptions).
The online continuation guard now accepts an active, intentionally disabled
authority manager while preserving version cancellation and offline checks.
Handling explicitly disables jackpots, consistent with its numeric hold/spill
rule, rather than consuming an unused jackpot draw. The real heading path's
closest save hex is two steps from this fixture keeper, so the tie uses natural
3 plus Saving 4 against heading power 7; no game threshold changed.

Affected offline checks: `AttributeScoreFloorPlayModeTests.SavingZeroTiesHeaderDespiteDistancePenalty`
**4 passed** in `20261114T073145618Z/results.xml`;
`LegacyHeaderSaveJumpersPlayModeTests` **6 passed** in
`20261114T073252978Z/results.xml`. No complete suites ran.

Still pending: general goalkeeper movement, scored goals
and restart continuations, dedicated guest shot-result
presentation, and paired acceptance.
Broader user-run checks when appropriate: `Assets/Tests/PlayMode/LegacyHeaderGoalBranchPlayModeTests.cs`,
`LegacyOriginalHeaderGoalPlayModeTests.cs` and category DecisionTiming in
`Assets/Tests/PlayMode/Features/DecisionTiming/` for other shot/restart timing paths.

Current Windows exports after 3d50d86: Room `build_7c547af10080` Succeeded
(7 warnings), prepared `build_3ffe256f4de2` Succeeded (2 warnings). Both retain
the known missing-vswhere.exe diagnostic, with no script compile errors.
Packaged local ground/pause/recovery checks passed in
`Runs/20261114T083231230Z-Room-local/` and
`Runs/20261114T083440776Z-Gameplay-local/`, retaining result.json,
gameplay-evidence.json and build-report.json. Shot behavior has the focused native
evidence above; these local probes do not constitute human shot or paired acceptance.
These exports precede the outfield-block extension; rebuild both desktops before
testing that extension in packaged or paired play.

Earlier Windows exports after 7d69148 (before the ordinary-shot extension): Room `build_2f27f3571268` Succeeded (7 warnings),
prepared `build_46d25681fc94` Succeeded (2 warnings). Each retains the known
missing-vswhere.exe diagnostic (one report error, no script compile errors).
Existing packaged local ground/pause/recovery checks passed in
`Runs/20261114T073800755Z-Room-local/` and
`Runs/20261114T073921462Z-Gameplay-local/`, retaining result.json,
gameplay-evidence.json and build-report.json. Packaged Room native login also
passed with liveness notification registration in
`Runs/20261114T073831991Z-Room-login/result.json`. No paired relay or human shot
acceptance is implied by these local/login checks.
