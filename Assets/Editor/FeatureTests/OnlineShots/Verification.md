# Online shots

- [x] Real header goal-line declaration, challenge and keeper-save command.
- [x] Disabled authority owner, save/handling result holds, cancellation and deadlines.
- [x] Handling, possession/distribution and rebound continuation, both directions.
- [ ] Ground shots, snapshots, blockers and general keeper choices.
- [ ] Goals, restarts and statistics through actual match flow.
- [ ] Focused tests, compilation and refreshed builds.

Paired acceptance remains deferred. This checklist tracks partial progress;
it does not claim complete shot or full-match internet play.

Compilation separately clean. `OnlineHeaderShotPlayModeTests` / category
`OnlineShots`: **16 distinct PlayMode cases have passing final outcomes**,
zero skipped. Native XML under `TestResults/GameplayRegression/Runs/`:

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

Still pending: ordinary shot/snapshot targeting, blocking, general goalkeeper
movement, scored goals and restart continuations, dedicated guest shot-result
presentation, and paired acceptance.
Broader user-run checks when appropriate: `Assets/Tests/PlayMode/LegacyHeaderGoalBranchPlayModeTests.cs`,
`LegacyOriginalHeaderGoalPlayModeTests.cs` and category DecisionTiming in
`Assets/Tests/PlayMode/Features/DecisionTiming/` for other shot/restart timing paths.

Windows exports after 7d69148: Room `build_2f27f3571268` Succeeded (7 warnings),
prepared `build_46d25681fc94` Succeeded (2 warnings). Each retains the known
missing-vswhere.exe diagnostic (one report error, no script compile errors).
Existing packaged local ground/pause/recovery checks passed in
`Runs/20261114T073800755Z-Room-local/` and
`Runs/20261114T073921462Z-Gameplay-local/`, retaining result.json,
gameplay-evidence.json and build-report.json. Packaged Room native login also
passed with liveness notification registration in
`Runs/20261114T073831991Z-Room-login/result.json`. No paired relay or human shot
acceptance is implied by these local/login checks.
