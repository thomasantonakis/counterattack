# EOS Connect login proof

- [x] Create the ignored one-use `Temp/OnlineEOSConnectivity.run` marker, then
  run `EOSConnectLoginEditorTests.DeviceIdentityCanLogInThroughConnect` by exact
  name through the connected Editor, with local EOS configuration and Room clean.
- [x] Verify native initialize/create/shutdown and Connect Device ID login.
- [x] Record the actual result and native XML path here.
- [x] Keep product/client values and product user IDs out of test output and Git.

After the user reopened Unity, the exact fixture completed on 2026-11-10:
**1 passed, 0 failed, 0 skipped, 0 inconclusive**. The login callback returned
a valid EOS Product User ID. Native XML:
`TestResults/GameplayRegression/Runs/20261110T204345472Z/results.xml`.
Compilation completed with zero errors; Room remained clean; Play Mode stayed
stopped. The marker was consumed. This does not prove a second user, lobby,
P2P traffic, relay or standalone build.

Initial attempts on 2026-11-10 did not reach Connect login. The explicit
fixture attribute caused two name-filtered runs to select zero tests. After
switching to a one-use marker, the first case failed at native initialization
with `AlreadyConfigured` (1 failed; XML:
`TestResults/GameplayRegression/Runs/20261110T185206895Z/results.xml`). A
second attempt accepted that result but platform creation returned null (1
failed; XML: `TestResults/GameplayRegression/Runs/20261110T185317129Z/results.xml`).
The prior one-off native probe had called EOS `Shutdown` in this Editor process;
the SDK disallows further calls after shutdown. The fixture now reports this
state as inconclusive and requires a fresh Editor process before its first run.

This fixture contacts the configured EOS deployment and may create one disposable
local device identity and product user. It consumes the marker before contacting
EOS, so ordinary test runs skip it. It does not enter Play Mode, create a lobby,
send P2P traffic or
prove cross-network connectivity. The identity is only for a connectivity proof;
it does not establish durable account ownership.

## Private lobby policy probe

- [x] Create the ignored one-use `Temp/OnlineEOSLobby.run` marker and run only
  `EOSPrivateLobbyEditorTests.DeviceIdentityCanCreateAndDestroyPrivateLobby`.
- [x] Confirm the EOS policy permits a two-seat invite-only lobby with join-by-ID,
  then destroy it and record the native XML.

After a fresh Editor start, the private-lobby fixture passed on 2026-11-10:
**1 passed, 0 failed, 0 skipped, 0 inconclusive**. Native XML:
`TestResults/GameplayRegression/Runs/20261110T205106861Z/results.xml`.
The callback returned a lobby ID; the destroy callback returned success. No
identifier was printed. Room remained clean and Play Mode stayed stopped.

This second fixture uses a fresh Editor process, logs in one device identity,
creates a private lobby and destroys it. It does not prove another player can
join or that EOS P2P transport works. If the configured policy rejects lobby
creation, record the service error before changing portal permissions.
