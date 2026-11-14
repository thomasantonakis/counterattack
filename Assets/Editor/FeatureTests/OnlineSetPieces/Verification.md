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

Continuation audit: disabled proof Update left free-kick ownership active after a
completed pass. Two new independently selectable `RoomFreeKickRetiresCommittedRestart`
cases reproduced it at `Runs/20261114T153902920Z/results.xml`. Online live-action
commit now retires that restart synchronously. The full focused feature category
then passed **24/0/0** at `Runs/20261114T154013072Z/results.xml` (20 isolated +
4 actual Room cases), compilation separately clean. These replace the earlier
22-case verdict; do not add overlapping run counts. New cases registered for Mac.

Windows exports refreshed at a40ba06 plus presentation fix d611230:
Room `build_5a530af4a1d5` and prepared `build_76ecc6959882` both Succeeded.
Reports retain 7/2 warnings respectively and one known EOS post-build
missing-vswhere.exe lookup error each; no script compilation errors.
Packaged local checks passed at `Runs/20261114T154318506Z-Room-local/` and
`Runs/20261114T154345316Z-Gameplay-local/`, with build summaries retained there.
Native Room login passed at `Runs/20261114T154345328Z-Room-login/result.json`,
liveness notification registration true. These are limited local/login probes,
not paired, relay, full-match or cross-platform acceptance.
