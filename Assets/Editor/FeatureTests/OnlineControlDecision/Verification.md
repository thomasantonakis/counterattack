# Detached control decisions without world serialization

- [x] Replace controls' full JSON.NET match copy with a typed, immutable decision.
- [x] Clone legal choices and preserve command IDs, revisions, accepted receipts,
  pause ownership, reconnect holds, keeper restrictions and shootout selection.
- [x] Retain real pending/retry, pitch input, shootout and keeper UI behavior.
- [x] Check bounded capture memory with a deliberately large world publication.
- [x] Register every new case separately in the deferred Mac manifest.

The host displays controls each frame. Previously each display serialized and
deserialized the full roster, runtime snapshots, stats and presentation state.
`OnlineControlDecision` copies only input metadata, primitive permission flags
and legal choices. It cannot retain mutable roster/statistics references. Strings
are immutable and the copied choices expose a read-only collection. The complete
network state and validation remain unchanged; full Room stays v9, wire stays v7.

Compilation: zero C# errors through the open Editor. Own-feature final native
results: **10 passed, zero failed/skipped**, fixture/category
`OnlineControlDecisionEditorTests` / `OnlineControlDecision`, at
`TestResults/GameplayRegression/Runs/20261116T074104413Z/results.xml`.
The memory case also passed a focused repeat at `20261116T074121755Z`.

Measurement limits: this Mono Editor's per-thread allocation counter returns
zero. An initial retained-heap fixture also allowed one comparison group to be
collected during the other, producing a negative delta. Retained evidence is at
`20261116T073717903Z`. The corrected fixture roots both groups together and uses
Unity's Mono heap accounting. Its coarse allocator blocks require one 512-KiB
comparison allowance (the too-tight fixture is retained at `20261116T073954831Z`),
plus an absolute 4-MiB budget for 4,096 captures. Final large-publication samples
both reported 524,288 bytes, versus 266,240/237,568 for empty publications. These
are heap-accounting deltas, not exact object bytes or an internet latency claim.

Affected native results: **19 passed, zero failed/skipped**:

- Manual controls seven: `20261116T073256440Z/results.xml`.
- Actual pitch input eight: `20261116T073329408Z/results.xml`.
- Owned shootout orders two selected cases: `20261116T073457397Z` and
  `20261116T073527578Z`.
- Opponent-paused forced keeper replacement and emergency nomination: one each
  at `20261116T073759306Z` and `20261116T073829520Z`.

A quoted PowerShell keeper filter selected zero tests; it is not counted as a
pass. Python subprocess arguments preserved the quotes for the two actual runs.
All paths above are under `TestResults/GameplayRegression/Runs/`. The unchanged
runtime was tested before the later measurement-only fixture corrections.
Mac manifest inventory/hashes validate; 13 verifier tooling unit tests passed.

Future broader checks, if requested: the fixtures under
`Assets/Tests/PlayMode/Features/OnlineManualControls`, `OnlineBoardInput`,
`OnlineRequiredKeeper`, `OnlineShootout` and `OnlineSubstitutions` share these
input gates. Do not start entire unrelated suites. Physical Mac input and paired
internet latency remain deferred.

Windows exports all succeeded: normal `build_fc0d013bd09c` at
`20261116T074259Z-normal-Windows-build`, Room `build_b82ce15833a1` at
`20261116T074346Z-Room-Windows-build`, prepared `build_96e70f8d9aac` at
`20261116T074414Z-Gameplay-Windows-build`. Known nonfatal EOS vswhere diagnostic
and nine normal/two proof warnings remain; zero C# errors. Packaged checks all
passed: `20261116T074422347Z-Room-local`, `20261116T074436270Z-Gameplay-local`,
`20261116T074443859Z-Room-login` (liveness notifications registered).
