# Full Room snapshot pose continuity

- [x] Reproduce in-flight ball/player snapping through an encoded full snapshot.
- [x] Publish bounded detached poses without changing logical occupancy/decisions.
- [x] Restore guest visuals after logical state, including held/rejoin snapshots.
- [x] Run focused tests, register every Mac case and retain native evidence.

This targets full snapshot continuity. It does not measure internet latency or
replace the existing reliable presentation lane with unreliable delivery.

The initial `20261115T210117317Z` run exposed a fixture envelope missing its
match ID; `20261115T210205269Z/results.xml` then reproduced the actual in-flight
ball snap (about 0.074 world units). Snapshot pose values are outside the logical
command boundary so animation does not replace legal decisions. Full Room v5
requires finite, complete, matching-roster poses only while motion is active;
older clients fail compatibility before joining. Stationary snapshots retain
logical hex projection. Guest pose restoration only updates authored transforms.

`20261115T210329422Z/results.xml`: nine malformed-pose EditMode cases passed,
zero failed/skipped. Missing/wrong-match/partial/duplicate/unknown actors,
non-finite/out-of-bounds coordinates and static-state pose data are rejected
without consuming publication sequence numbers. Compilation separately clean.

`20261115T210343538Z/results.xml`: five actual-scene PlayMode cases passed,
zero failed/skipped, including 16/18 squads and both directions. Real ball and
player coroutines are captured under a connection hold, encoded/compressed,
then restored in a fresh inert guest. Repeated snapshot projection preserves
poses and logical token hexes, without guest events or timer construction.
All fourteen new native cases are individually registered for deferred Mac use.

Affected focused checks passed: payload bounds 15
(`20261115T210457918Z/results.xml`), roster validation 13
(`20261115T210505643Z/results.xml`), encoded kickoff 4
(`20261115T210520659Z/results.xml`), all zero failed/skipped. Thirteen Python
verifier tests passed. No full suite or actual paired run was executed.
A broader motion follow-up, if requested, is
`Assets/Tests/PlayMode/Features/OnlineMovement/`, fixture/category
`OnlineMovementPlayModeTests` / `OnlineMovement`, because full snapshots now
carry current actor poses while movement runs. It was not run in this batch.

Normal eight-scene Windows export succeeded as `build_ebc8adbf01b5`; evidence
`20261115T210619Z-normal-Windows-build/build-summary.json`. Nine existing warnings
and the known nonfatal EOS `vswhere.exe` diagnostic remain; no C# compile errors.
Both clients must pull/rebuild for full Room v5 before the deferred paired run.
