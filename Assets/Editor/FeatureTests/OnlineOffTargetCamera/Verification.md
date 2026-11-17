# Online off-target camera-shot continuation

- [x] Enter the real miss branch using an admitted shot roll and deterministic randomizer (1, then 3/4).
- [x] Mark direct camera flight/hold as presentation motion so reliable snapshots and the existing 20 Hz lane carry the actual ball pose.
- [x] Retire camera writes and the outer off-target continuation after a real manager reset.
- [x] Keep the presentation flag scoped to its version and clear it before interactive goal-kick preparation.
- [x] Finish focused motion, shared pause, connection hold and reset cases in both directions.
- [x] Run relevant shot/transport regressions, register every case and refresh Windows builds.

Room remains v20, wire v7. No new client action IDs, dice input or schema fields
are introduced. The source camera miss previously moved the ball transform
without any existing flight flag; unlike high/long passes, it therefore lacked
roomPose and the high-rate presentation lane. The new version-scoped flag uses
the established ball-moving path. The original .4-second flight and scaled
two-second hold are retained. Shared pause/recovery already owns timeScale and
freezes both; it was not necessary to invent a separate pause rule.

Initial native run TestResults/GameplayRegression/Runs/20261117T074455111Z
reproduced two missing-motion failures and four stale-reset ball writes. Eight
other cases failed an early fixture assertion: possession changes before the
ball return/goal-kick setup finishes. The fixture now waits for actual published
Final Third choices, rather than an intermediate raw match-state name. A second
diagnostic run at 20261117T075757131Z still checked readiness too early and was
cancelled through CLI; editor_status confirmed stopped before source edits.
Native progress/XML are retained where available; neither run is claimed green.

The reset cases invoke the real internal ResetShotProcess boundary, place the
ball for the subsequent state and assert that the retired animation cannot
write positions or award a late restart. Successful continuations must publish
playable goal-kick Final Third choices and retain one off-target shot attempt,
zero on-target shots/goals and exactly two authority dice draws. Fixture RNG,
scene/save/singleton cleanup uses the owned full-Room pattern.

No paired or whole-suite tests have run. Full internet acceptance and measured
latency remain deferred. Final results and affected filters follow below.

Final own feature: 20 passed, zero failed/skipped. Full shots: 14 cases at
20261117T080548173Z/results.xml; snapshots: six at 20261117T082239286Z/results.xml.
Snapshot cases include real blocker selection/movement before aiming. All 20
cases are registered separately for the deferred Mac run, fixture hashes valid.
Eight affected ShotAndSnapshotContinueThroughSave cases passed at
20261117T081215194Z/results.xml; a transient final poll HTTP 400 was resolved by
reading native XML and confirming completed 8/8 before continuing. The existing
two off-target possession-clock cases passed separately. Artifact paths share
TestResults/GameplayRegression/Runs/. No passing cases were disabled.

Two affected possession-clock cases passed at 20261117T081638139Z/results.xml.
All three Windows builds succeeded: 20261117T083208Z-normal-Windows-build,
20261117T083259Z-Room-Windows-build, 20261117T083327Z-Gameplay-Windows-build.
Each retains the known nonfatal missing-vswhere post-build diagnostic; no C#
compile errors. Packaged checks passed: 20261117T083621353Z-Room-local,
20261117T083635215Z-Gameplay-local, 20261117T083643842Z-Room-login (liveness true).
Report-tool unit tests: 13 passed. Artifact paths are under the Runs directory.

Broader optional handoff: Assets/Tests/PlayMode/Features/OnlineGroundShots,
fixture OnlineGroundShotPlayModeTests, for remaining shot/save interactions;
Assets/Tests/PlayMode/Features/OnlineOffTargetCamera, fixture
OnlineOffTargetCameraPlayModeTests, for this feature. Use explicit fixtures;
no complete suite or paired test was executed.
