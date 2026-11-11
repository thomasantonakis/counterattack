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

## Two-Mac lobby join probe (passed)

- [x] Prepare two clean Editor processes with the same EOS deployment and
  separate local device identities. Do not copy EOS identity storage.
- [x] Run only `EOSTwoMacLobbyEditorTests.HostWaitsForDistinctGuestAndDestroysLobby`
  with `Temp/OnlineEOSTwoMacHost.run` on the first Mac. It writes a temporary
  lobby ID to `Temp/OnlineEOSHostLobbyId.txt` and waits up to fifteen minutes.
- [x] Publish that one-use ID to the private repository handoff, then run
  `Documentation/Setup/RunEOSGuest.sh` on the second Mac. Remove the remote
  handoff after the attempt; keep product/client values and product user IDs local.
- [x] Run only `EOSTwoMacLobbyEditorTests.GuestSearchesAndJoinsHostByLobbyId`
  with `Temp/OnlineEOSTwoMacGuest.run` on the second Mac. Inspect both completed
  test results and confirm the host saw a distinct product user.
- [ ] Repeat on separate networks after a same-network connection succeeds.

This probe uses an unadvertised `Joinviapresence` lobby and an exact-ID search.
The earlier invite-only lobby creation test does not establish code-based joins:
EOS invite-only lobbies require an actual invitation. The full lobby ID serves
only as a temporary development join code; a user-friendly code resolver is
future work. These tests do not send P2P packets.

On 2026-11-11, the personal Mac host passed: **1 passed, 0 failed, 0 skipped**.
It observed a distinct EOS product user join and leave, then destroyed the lobby.
Native XML: `TestResults/GameplayRegression/Runs/20261111T144217103Z/results.xml`.
The family Mac guest also passed: **1 passed, 0 failed, 0 skipped**, as reported
by its live `test_status` result. It found the host lobby by exact ID, joined
and left. Its native XML remains on that Mac at
`TestResults/GameplayRegression/Runs/20261111T144726042Z/results.xml` and was
not copied here. Both Editors were stopped, and Room remained clean on the host.
The helper initially
rejected a clean Unity `completed` compilation state, then passed on retry;
its readiness check was corrected afterward. This proves two distinct Mac device
identities can coordinate through the configured EOS lobby. It does not yet
prove P2P packets, relay routing, a different network, Windows interoperability
or playable online matches.

Two host attempts on 2026-11-10 each created and then destroyed a lobby, but
neither observed a distinct guest within five minutes: **0 passed, 1 failed**
per attempt. Native XML on the personal Mac:
`TestResults/GameplayRegression/Runs/20261110T212318979Z/results.xml` and
`TestResults/GameplayRegression/Runs/20261110T213157947Z/results.xml`.
The first guest run on the company Mac skipped before EOS initialization because
its one-use marker was absent. In the second attempt, the guest ran but failed
at Connect `CreateDeviceId` with `NoConnection`, before lobby search or join:
**0 passed, 1 failed** (result reported from the company Mac; its native XML
was not collected here). Thus neither host failure is evidence about join-by-ID.

A credential-free HTTPS check on the company Mac for `api.epicgames.dev`
returned curl certificate error 60 and remote IP `146.112.61.106`. Cisco
documents that address as a Cisco Umbrella content/application block page.
The company security policy must allow the EOS endpoint before that
Mac can complete the probe; do not bypass the filter or disable TLS validation.
Boot Camp on the personal host Mac cannot supply a simultaneous second Editor
while macOS hosts the lobby. The family Mac has now completed local setup and
the user reports that its protocol and EOS Connect login fixtures passed; its
native XML has not been collected here. It is the guest for the next attempt.
Do not copy corporate credentials or device identity storage between machines.
References: [Epic firewall requirements](https://dev.epicgames.com/docs/epic-online-services/firewall-considerations),
[Cisco Umbrella block page addresses](https://www.cisco.com/c/en/us/support/docs/security/umbrella/224799-learn-the-umbrella-block-page-ip.html).

## Two-Mac P2P packet probe (passed)

- [x] Add isolated host and guest fixtures with one-use local markers, an
  exact-ID coordination lobby, and a socket derived from that lobby ID.
- [x] Compile the new fixtures in the open personal Mac Editor.
- [x] Reopen both Editors manually after their prior EOS tests shut down the SDK.
- [x] Run only `EOSTwoMacP2PEditorTests.HostReceivesPacketAndAcknowledgesDistinctGuest`
  on the personal Mac with `Temp/OnlineEOSP2PHost.run`.
- [x] After the host writes `Temp/OnlineEOSP2PHostLobbyId.txt`, publish its
  one-use ID in the private `ActiveEOSP2PLobby.json` handoff. Run
  `bash Documentation/Setup/RunEOSGuest.sh p2p` on the family Mac.
- [x] Confirm the host receives the guest's exact hello bytes and the guest
  receives the host's exact acknowledgement bytes. Record both actual results
  and native XML paths, then remove the remote handoff.

The host accepts a P2P connection only from the distinct product user that
joined its two-seat lobby. Both directions use EOS reliable ordered packets on
the same socket and channel. A successful send call only queues a packet; the
receive assertions are the delivery proof. It will not establish relay routing,
separate-network or Windows interoperability, or playable online matches.

On 2026-11-11, the family Mac guest passed: **1 passed, 0 failed, 0 skipped**.
It received the exact acknowledgement from the host after sending its hello;
the result and XML path remain on that Mac. The personal Mac host failed:
**0 passed, 1 failed, 0 skipped**. Native XML:
`TestResults/GameplayRegression/Runs/20261111T150722348Z/results.xml`.
The host reached its final lobby-destroy assertion, which establishes that it
received and checked the guest's exact hello bytes, sent the acknowledgement,
and observed the guest leave. EOS then returned `TimedOut` when destroying the
lobby. The packet exchange is proven in both directions, but the host fixture
is not green. A focused cleanup fix now closes the P2P connection before
destroying the lobby and retries a transient `TimedOut`; it awaits a fresh
two-Mac run. The temporary Git handoff was removed after the first attempt.

The focused rerun on 2026-11-11 again passed on the family Mac: **1 passed,
0 failed, 0 skipped**, XML at
`TestResults/GameplayRegression/Runs/20261111T151712137Z/results.xml` on that
Mac. The personal Mac host again reached lobby cleanup after the packet exchange
and guest departure, then failed: **0 passed, 1 failed, 0 skipped**, XML at
`TestResults/GameplayRegression/Runs/20261111T151623401Z/results.xml`.
The first destroy callback returned `TimedOut`; a retry returned `NotFound`.
That sequence is consistent with the first destroy request completing on EOS
despite its timeout response. The fixture now accepts `NotFound` only after a
timed-out destroy attempt. The second
temporary Git handoff was removed.

The final focused run on 2026-11-11 passed on both Macs. The personal Mac host:
**1 passed, 0 failed, 0 skipped**, XML at
`TestResults/GameplayRegression/Runs/20261111T152710314Z/results.xml`.
The family Mac guest: **1 passed, 0 failed, 0 skipped**, XML at
`TestResults/GameplayRegression/Runs/20261111T152822714Z/results.xml` on that
Mac. The host confirmed the distinct lobby member, received the exact hello
payload, sent the acknowledgement, observed guest leave and completed cleanup.
The guest received the exact acknowledgement from the host. This is a two-way
EOS P2P packet proof between two macOS Editors on the current networks. It does
not reveal whether EOS used a direct or relay route and does not prove separate
networks, Windows, standalone builds or playable match synchronization. The
third one-use Git handoff was removed after both tests completed.

## Forced-relay packet probe (pending)

- [x] Add focused host and guest cases that set EOS `RelayControl.ForceRelays`
  before making a P2P connection, read back that setting, and verify the same
  exact hello and acknowledgement payloads as the default-route probe.
- [x] After manually reopening the personal Mac Editor, run only
  `EOSTwoMacP2PEditorTests.HostExchangesPacketsWithForcedRelays` on the personal
  Mac with `Temp/OnlineEOSRelayHost.run`. Share its one-use lobby ID through
  the private `ActiveEOSP2PLobby.json` handoff.
- [x] Run `bash Documentation/Setup/RunEOSGuest.sh relay` on the family Mac
  after pulling the probe commit. Confirm both completed results and native XML,
  then remove the temporary handoff.
- [x] Repeat the forced-relay proof with the Macs on separate networks.

The personal Mac host case passed on 2026-11-11: **1 passed, 0 failed,
0 skipped**, native XML at
`TestResults/GameplayRegression/Runs/20261111T161052518Z/results.xml`.
It read back `ForceRelays`, received the guest's exact hello, sent the
acknowledgement, observed guest departure, and destroyed the lobby. The family
Mac guest also passed: **1 passed, 0 failed, 0 skipped**, as reported by its
live `test_status`; its native XML remains on that machine at
`TestResults/GameplayRegression/Runs/20261111T161217944Z/results.xml`.
The guest read back `ForceRelays` and received the host's exact acknowledgement.
The one-use lobby handoff was removed after the host completed.

Both peers used the relay variant. The paired result shows packet delivery
while EOS on both machines was configured to allow only relay servers. It does
not inspect a per-connection route or measure relay latency.
The default-route result alone does not identify whether its packets used a
direct or relayed path. See Epic's [relay-control reference](https://dev.epicgames.com/docs/en-US/api-ref/enums/eos-e-relay-control).

For the separate-network run on 2026-11-11, the user placed the family Mac on
an iPhone hotspot while the personal Mac stayed on its existing network, then
manually restarted both Editors. The personal Mac host passed **1/1**, native
XML `TestResults/GameplayRegression/Runs/20261111T161817260Z/results.xml`.
The family Mac guest passed **1/1**, reported by its live `test_status`, with
native XML on that Mac at
`TestResults/GameplayRegression/Runs/20261111T161910290Z/results.xml`.
Both cases had zero failures and skips. This proves two-way EOS packet delivery
with relay-only settings across the two reported networks in Editor processes.
It does not establish standalone build behavior, Windows interoperability,
latency, per-connection route introspection, or a playable online match.
