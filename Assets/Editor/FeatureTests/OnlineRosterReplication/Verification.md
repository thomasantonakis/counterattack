# Online roster status replication

- [x] Reproduce missing guest booking/injury/substitution/GK status with real Room entry points.
- [x] Publish full stable roster, current attributes, all player stats and directions.
- [x] Apply values without guest rules/events, preserving authored bench visibility.
- [x] Reject malformed identity/status/position payloads before projection.
- [x] Run feature-only tests, affected full Room handoff and refresh builds.
- [x] Add each new case to the deferred Mac manifest and retain native evidence.

This phase prepares roster state for online menus. It does not implement menu
commands or claim paired full-match internet acceptance.

Native evidence under `TestResults/GameplayRegression/Runs/`:
- `20261115T182122650Z`: one reproducer failed on the missing guest booking flag.
- `20261115T182701670Z`: 13 malformed-publication EditMode cases passed.
- `20261115T183246669Z`: four real Room host ? inert guest cases passed,
  covering squad sizes 16/18 and both directions, including removed active IDs
  and closed-replica timer safety.
- `20261115T182826654Z`: 20 affected full Room cases passed.
- `20261115T183121625Z`: seven affected manual-control/recovery cases passed.
Compilation was separately clean; no failures/skips in the final runs.
The changed catalog compatibility also passed its focused existing check.

Full Room contract v2 publishes detached runtime token snapshots, complete player
stats, authoritative visibility/bench positions, both attacking directions and
substitution availability/counts. Stable authored actor IDs survive removal from
active/bench lists. Guest projection restores these values without rolling dice,
advancing rules or appending events. Closed online presentation cannot construct
an authority timer after the role context is disposed.

The frontend catalog fingerprint now includes the Room contract, so older full
Room builds are rejected before handoff. Prepared proof catalogs remain separate.
All 17 new native cases have individual deferred Mac manifest registrations.

Next work: normal online pause/substitution/shootout controls; full animation
snapshots must also preserve physical in-flight poses across timed publications
(current lightweight pose frames use the reliable gameplay lane). Internet
acceptance, input latency and Mac execution remain unverified/deferred.

Windows build refresh succeeded for contract v2:
- `build_9d1335c6b6f3`: normal eight-scene game,
  `20261115T183646Z-normal-Windows-build/build-summary.json`.
- `build_d90028e5007d`: Room proof,
  `20261115T183723Z-Room-Windows-build/build-summary.json`.
- `build_ad72a8c8ee6b`: prepared gameplay proof,
  `20261115T183745Z-Gameplay-Windows-build/build-summary.json`.
Build evidence is under `TestResults/GameplayRegression/Runs/`. All report
Succeeded, with the existing nonfatal EOS `vswhere.exe` discovery diagnostic;
normal game has nine warnings, proofs two each. No C# compilation errors.

Refreshed packaged checks passed: `20261115T183752243Z-Room-local`,
`20261115T183804854Z-Gameplay-local`, `20261115T183812412Z-Room-login`.
Native login successfully registered liveness notifications. Local reports do not
exercise internet routing or full normal-game acceptance. Thirteen verifier
Python tests passed and the Mac fixture inventory/definition hashes validated.
