# Online off-target lob continuation

- [x] Add eight independently selectable real shot/snapshot lob cases, both directions.
- [x] Reproduce cancellation, fix justified defects, run focused cases.
- [x] Register each Mac case and retain build evidence.

Uses the real admitted shot roll, seeded off-target randomizer 1/2, and owned
full-Room cleanup. Reset cases must prevent retired motion and a late restart.

Initial native run 20261117T083846457Z: four normal-flight passes, four reset
failures. Real shot reset followed by placement was overwritten by the old lob
flight by 4.6-4.8 units. This was a gameplay defect, reproduced before the fix.
The shot now supplies its version check to the existing long-ball animation;
that optional check runs during flight and before final placement. The parent
lob continuation also checks before awarding a restart. Other long-ball callers
keep their established cancellation behavior. No rule, dice or schema changes.

Corrected run 20261117T103339995Z: eight passed, zero failed/skipped. Every case
is individually registered for the deferred Mac run. Compilation clean separately.
Native XML/progress retained under TestResults/GameplayRegression/Runs/.
Paired internet testing and measured latency remain deferred.

Affected OnlineLongPassPlayModeTests.LongPassAuthorityAndReplica: four passed,
zero failed/skipped at 20261117T103641109Z/results.xml. Report-tool unit checks:
13 passed. Room v20 / wire v7 remain unchanged.

Optional broader handoff: Assets/Tests/PlayMode/Features/LongBallGoalkeeperPace,
fixture LongBallGoalkeeperPacePlayModeTests, covers other long-ball/keeper
interruptions; Assets/Tests/PlayMode/Features/OnlineGroundShots, fixture
OnlineGroundShotPlayModeTests, covers remaining shot/save paths. These broader
fixtures were not run for this narrow cancellation change; no full suite ran.

Windows exports succeeded: 20261117T103844Z-normal-Windows-build,
20261117T103927Z-Room-Windows-build, 20261117T103951Z-Gameplay-Windows-build.
Known nonfatal vswhere diagnostic retained, zero C# compile errors.
Packaged checks passed: 20261117T103954807Z-Room-local,
20261117T104009673Z-Gameplay-local, 20261117T104017335Z-Room-login (liveness true).
