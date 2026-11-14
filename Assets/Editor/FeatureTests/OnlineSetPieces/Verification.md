# Online set pieces

- [x] Typed initial/final kicker, setup moves/skips and execution.
- [x] Both directions, defender clearance, indirect free kicks and corners.
- [x] Shot preview/confirmation, shared holds, guest/wrong-team rejection.
- [x] Actual serialized Room-proof free kick through completed ground pass.
- [x] Focused native verification and individual deferred Mac case entries.

Compilation separately clean. Category `OnlineSetPieces`:
- `OnlineSetPiecePlayModeTests`: 20 passed, zero failed/skipped, at
  `Runs/20261114T153040613Z/results.xml`.
- `OnlineRoomSetPiecePlayModeTests`: 2 passed, zero failed/skipped, at
  `Runs/20261114T153309128Z/results.xml`.
- Affected `LegacyFreeKickShotPlayModeTests.OriginalFreeKickSheetSetupKeepsPhaseOwnershipAndOffersShot`:
  1 passed, zero failed/skipped, at `Runs/20261114T153138519Z/results.xml`.

All paths start real preparation and advance actual phase coroutines. Execution
hands ownership to existing pass/cross/long/shot managers. Direct free-kick shot
confirmation stays with the restart manager, preserving its shot type. Ground
restart completion is also checked with actual serialized Room-proof managers.
Setup motion joins the existing player presentation lane. No raw-key network
commands or runtime visual authoring. Source keyboard handlers share semantic
execution methods with the online adapter.

Initial 16 missing-command reproductions: `Runs/20261114T152702437Z/results.xml`.
Intermediate 12/4 run `Runs/20261114T152844251Z/results.xml` exposed a fixture
assumption: moving once does not end a two-move phase. Fixture now forfeits the
remaining move through the real command. Rules were preserved.

Mac execution and paired acceptance remain deferred. No full suite ran.
Full normal Room roster/lifecycle, penalties, throw-ins, substitutions and
keeper replacements remain separate work. For broader user-run verification,
use `Assets/Tests/PlayMode/LegacyFreeKickShotPlayModeTests.cs` fixture
`LegacyFreeKickShotPlayModeTests` (all shot outcomes), and
`Assets/Tests/PlayMode/Features/OnlineFouls/` category `OnlineFouls` (restart entry).
