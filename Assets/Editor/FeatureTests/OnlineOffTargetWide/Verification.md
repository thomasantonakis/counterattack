# Online off-target wide continuation

- [x] Add 12 independent real full-shot/snapshot cases, both directions.
- [x] Reproduce reset behavior, fix justified defects and run feature cases.
- [x] Verify affected ground movement, register Mac cases and refresh builds.

Real shot commands, seeded authority randomizer 5/6, owned Room fixture cleanup.
No paired test or whole suite is requested.

Initial 20261117T104140441Z: four normal-flight passes and four reset failures.
Retired ground movement overwrote the restored ball position by 2.29 units.
Fixed shot passes a version predicate through GroundBallManager to Ball.MoveToCell;
checks precede writes/completion and post-animation cleanup/restart. Ball motion
ownership prevents an old coroutine from clearing a newer move's target. An
already-retired request is rejected before taking ownership. On goalkeeper
resume the owner check precedes restored transform writes. Other callers keep
their optional-callback default and movement rules.

First corrected feature run 20261117T104551873Z: 12 passed, zero failed/skipped.
Four cases cover a replacement ball move after reset, full shot/snapshot and
both directions. Boundary diagnostic 20261117T105012674Z failed four fixture
assertions: yielding an already-rejected enumerator allowed a valid movement
to finish before checking its in-progress flag. Rejection is now inspected
synchronously through the actual iterator; final destination/ownership cleanup
assertions remain. No production rule was changed for the fixture correction.
Native XML/progress retained under TestResults/GameplayRegression/Runs/.
Room v20 / wire v7 unchanged. Paired acceptance and measured latency deferred.

Final strengthened replacement cases: four passed at 20261117T105214505Z.
Affected ground pass outcomes: six passed at 20261117T105354251Z.
Affected ground goalkeeper box entry: four passed at 20261117T105455625Z
(right/left, move/skip). A prior pattern selector matched zero tests; the literal
/ground/ selector then selected the intended four. Zero-match is not counted as
validation. Thirteen report-tool unit checks passed; all 12 feature cases are
separately registered for deferred Mac verification.

Optional broader handoff: Assets/Tests/PlayMode/GoalkeeperBoxEntryPlayModeTests.cs,
fixture GoalkeeperBoxEntryPlayModeTests, for other callers of Ball.MoveToCell;
Assets/Tests/PlayMode/Features/OnlineGroundShots, fixture OnlineGroundShotPlayModeTests,
for remaining shot/save paths. No whole suite was executed.

Windows exports succeeded: 20261117T105712Z-normal-Windows-build,
20261117T105755Z-Room-Windows-build, 20261117T105818Z-Gameplay-Windows-build.
Known nonfatal missing-vswhere post-build diagnostic retained; zero C# errors.
Packaged Room/local 20261117T105841598Z and Gameplay/local 20261117T105856545Z
checks passed. Room/EOS login result is retained in 20261117T105905224Z-Room-login.
Room/EOS login passed with liveness registration true. Full paired transport was not exercised.
