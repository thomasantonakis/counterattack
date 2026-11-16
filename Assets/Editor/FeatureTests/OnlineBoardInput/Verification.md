# Online direct pitch input

- [x] Author input component and instruction label in OnlineRoom through Pipeline.
- [x] Player and hex hits choose an unambiguous published semantic action.
- [x] Host and guest use the existing authenticated command gateway.
- [x] Preserve P/L/C/F/M/S/R and Enter; never transmit physical keys or pixels.
- [x] Reject wrong team, pause/connection holds, pending commands and foreign objects.
- [x] Exercise actual collider raycasts, kickoff selection/movement/confirmation,
  taker selection and mandatory pass continuation in both attacking directions.
- [x] Fresh guest emits one intention, without local movement, rules, RNG or events.
- [x] Register every case separately for the deferred son's Mac run.

Production contract: `full-room-v9`; rebuild both full-game clients. Wire v7 is
unchanged. Generic authored command controls remain available for decisions
without a dedicated key. Mouse selection falls back from a token to its hex
when selecting a pass destination. Enter confirms an existing target preview;
ambiguous commands are never guessed. Input fields consume keyboard entry;
UI controls consume clicks and Enter, while inspection/intro reserve pitch input.
Camera and inspection remain personal. No designer-owned Stats layout is changed.

Compilation: clean through the open Editor, zero C# errors. Final own-feature
native results: **52 passed, zero failed/skipped**:

- `OnlineBoardInputEditorTests`: 44 at
  `TestResults/GameplayRegression/Runs/20261116T072259556Z/results.xml`.
- `OnlineBoardInputPlayModeTests`: eight at
  `TestResults/GameplayRegression/Runs/20261116T072315142Z/results.xml`.

The earlier 41+8 passes predate the additional ambiguous-confirm boundary and
expanded canonical action families; they are retained without double counting.

Affected verification: **11 passed, zero failed/skipped**:
`OnlineManualControlsPlayModeTests` seven at `20261116T072448939Z/results.xml`,
`OnlineFullRoomPlayModeTests.EncodedFullRoomKickoff` four at
`20261116T072521622Z/results.xml`, under the same retained Runs directory.
The Mac verification tooling's 13 unit tests also passed. All 52 new cases have
individual manifest rows, and fixture inventory/definition hashes validate.

Windows exports succeeded: normal `build_a351b6d5acbc` at
`20261116T072736Z-normal-Windows-build`, Room `build_2f56d967847c` at
`20261116T072816Z-Room-Windows-build`, prepared `build_9be99aaf835f` at
`20261116T072838Z-Gameplay-Windows-build`. Nine normal/two proof warnings and
the existing nonfatal EOS missing-vswhere diagnostic remain; zero C# errors.
Packaged local Room `20261116T072912960Z-Room-local` and prepared
`20261116T072926796Z-Gameplay-local` passed. Reports are retained under Runs.

Run only these fixtures/category `OnlineBoardInput` for this feature. Broader
manual coverage remains deferred: two humans using physical mouse/keyboard on
separate networks, macOS event/UI behavior and measured end-to-end latency.
Native tests call the same public input entry points as Update; collider tests
use real Physics raycasts. They do not synthesize operating-system input.

For later broader verification, use `Assets/Tests/PlayMode/Features/OnlineFullRoom`
and `Assets/Tests/PlayMode/Features/OnlineManualControls`: they cover full-room
handoff and older proof controls that share the command gateway. Do not run
whole unrelated suites by default.
`20261116T072934372Z-Room-login` also passed with liveness notifications registered.
