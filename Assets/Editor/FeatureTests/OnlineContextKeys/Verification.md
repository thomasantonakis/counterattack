# Contextual online Room keys

- [x] Route existing physical keys to the sole published meaning at the current decision.
- [x] Reject absent and ambiguous action families; preserve host semantic authority.
- [x] Cover real free kicks, corners and throw-ins in both attacking directions.
- [x] Cover authored text/dropdown focus on host and inert guest; buttons consume Enter.
- [x] Register all 63 new cases separately for the deferred Mac run.

Production Room catalog remains v14 and wire v7: no new network actions or payloads.
P/L/C/F/M/S and the contextual Q/K/H/G/V/A/D/B/N/T/X keys use existing
published commands. R rolls, Enter confirms or ends Final Third movement when
that is its unique published meaning. Typing in an input/dropdown/toggle/slider
cannot also issue pitch commands. Dropdown choices remain available.

Compilation: clean, zero C# errors through the live Editor.
Own-feature results: 63 distinct passed, zero remaining failed/skipped:

- OnlineContextKeysEditorTests: 45, Runs/20261116T183113824Z/results.xml.
- OnlineContextKeysPlayModeTests.RealSetPieceKeys/RealThrowInKeys: 16,
  Runs/20261116T183136761Z/results.xml.
- OnlineContextKeysPlayModeTests.AuthoredUiConsumesTyping: two,
  Runs/20261116T183339529Z/results.xml.

Paths are under TestResults/GameplayRegression/. The first PlayMode run retained
two UI fixture failures: selection was inspected after yielding a frame, rather
than synchronously after Dropdown.Show selected its item. The corrected fixture
asserts active/interactable controls, an opened real template and actual Toggle
selection before checking keyboard ownership. It also waits for Hide cleanup.
The two corrected cases passed for host and guest. No production behavior was
changed to satisfy that fixture. The initial red Editor specification exposed
44 missing contextual routes; it also included an invalid long.interception ID,
removed after checking that the real manager publishes long.roll instead.

Affected OnlineBoardInputEditorTests: 44 passed, zero failed/skipped, at
Runs/20261116T183406914Z/results.xml. Mac report tooling: 13 unit tests passed;
fixture inventory and definition hashes validated. No whole suite was run.

For later broader checks use Assets/Tests/PlayMode/Features/OnlineBoardInput
(OnlineBoardInputPlayModeTests) and OnlineFullRoom (OnlineFullRoomPlayModeTests)
for shared live input and full-scene handoff. Paired physical input, macOS focus,
network delivery and measured latency remain deferred until independently ready.

Windows exports refreshed successfully: normal build_b7f255f9383c at
20261116T183555Z-normal-Windows-build, Room build_06bbae37d2b7 at
20261116T183633Z-Room-Windows-build, prepared build_036e943f8ed1 at
20261116T183655Z-Gameplay-Windows-build. Each reports Succeeded with the known
nonfatal EOS missing-vswhere post-build diagnostic (one reported error), nine
normal/two proof warnings, and zero C# compilation errors. Build summaries are
retained under Runs. These exports include the contextual controls.
Packaged Room local (20261116T183707151Z-Room-local), prepared local
(20261116T183720758Z-Gameplay-local) and Room login/liveness
(20261116T183727353Z-Room-login) passed. Local proof roles are not evidence of
fresh-guest rule isolation; that is covered by the actual-scene native cases.
