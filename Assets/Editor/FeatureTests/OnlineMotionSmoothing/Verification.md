# Guest motion smoothing

- [x] Smooth accepted unreliable pose updates across one 50 ms stream interval.
- [x] Reuse authored transforms, with no runtime visual objects or rule movement calls.
- [x] Keep reliable state/reconnect correction immediate and cancel previous interpolation.
- [x] Keep host gameplay transforms untouched and freeze guest motion during shared pause/connection hold.
- [x] Ignore duplicate stream packets without restarting interpolation; retarget from the current drawn pose.
- [x] Stop at the last accepted pose when packets are missing; do not extrapolate.
- [x] Verify both directions and 32/36 actors, inert guest managers, unchanged occupancy/save/RNG.
- [x] Complete automatic Update and affected snapshot/compact checks, exports and manifest registration.

Presentation-only change, Room v18/wire v7 unchanged. Commands, rolls, outcomes,
hex occupancy and statistics retain their existing reliable authority path. The
ManualGuest loop queues only frames accepted by OnlineGroundPassReplica. The
original immediate DisplayPresentation path remains for reliable snapshots and
the prepared proof; only live full-Room guest stream frames interpolate. New
frames start at the current displayed transform. The interpolation interval adds
at most 50 ms to reaching a received pose and has no future-frame buffer. This
is a visual tradeoff, not an internet latency measurement.

Initial native reproduction: four missing-entry-point failures at
TestResults/GameplayRegression/Runs/20261116T233720879Z/results.xml. After the
implementation, four GuestMotionAndReliableCorrections cases passed at
20261116T233847714Z. Live Editor compilation clean, zero C# errors. Final
Update-loop, selected affected checks, build and manifest evidence follows below.
No paired or whole-suite test occurred. For later broader user verification,
use Assets/Tests/PlayMode/Features/OnlineFullRoom / OnlineFullRoomPlayModeTests
and OnlineMovement / OnlineMovementPlayModeTests: they share board display and
logical movement projection. Native runtime visuals are all existing authored
objects; Canvas/MatchStatsUI/Stats is unchanged.

Final native own feature: six passed, zero failed/skipped. Automatic Update cases
passed at 20261116T234015396Z/results.xml. Affected in-flight snapshot fixture:
five passed at 20261116T234101041Z/results.xml. Affected compact-pose fixture:
five passed at 20261116T234233825Z/results.xml. All paths are under
TestResults/GameplayRegression/Runs/. Six new Mac entries registered separately,
fixture inventory/hashes validated.

Windows exports all Succeeded with this presentation change on November 17 UTC:
normal build_8067c522ba4b at 20261117T053205Z-normal-Windows-build;
Room build_4dcc7504ef84 at 20261117T053254Z-Room-Windows-build;
prepared build_2d8af55f957c at 20261117T053322Z-Gameplay-Windows-build.
All summaries are under Runs. Zero C# errors; nine normal/two proof warnings and
the known nonfatal EOS missing-vswhere post-build diagnostic (one reported
post-build error/profile). The 13 report-tool unit tests passed.

Packaged checks passed: 20261117T053346943Z-Room-local, 20261117T053402490Z-Gameplay-local and 20261117T053411181Z-Room-login (liveness registration true). Local proof roles are not fresh-guest or paired evidence; native guest isolation is checked separately above.
