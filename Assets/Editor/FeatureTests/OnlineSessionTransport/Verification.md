# Online frontend session transport

- [x] Dedicated bounded frontend framing with gameplay wire compatibility preserved.
- [x] Fragment ordering, replacement, cross-protocol rejection and malformed contracts.
- [ ] Persistent authenticated EOS session and authored host/join controls.
- [ ] Actual RTT/queue telemetry and latency acceptance.
- [x] Focused native wire cases and individual deferred Mac entries.

Compilation separately clean. `OnlineSessionWireEditorTests`, category
`OnlineSessionTransport`: **22 passed**, zero failed/skipped, at
`Runs/20261114T233020058Z/results.xml`. Affected existing
`OnlineGroundPassWireEditorTests`: **13 passed**, zero failed/skipped, at
`Runs/20261114T233110599Z/results.xml`.

Common reliable ordered JSON framing now serves gameplay and frontend without
changing gameplay v7/CA07 bytes. Frontend has session-wire-v1/CA11 identity and
its own setup/heartbeat/phase message contract. Both retain 64 KiB bounds,
900-byte fragments, per-message identity, strict ordering and retirement of
interrupted messages. Unicode, overflow, malformed headers/contracts, cross-wire
rejection and old partial retirement have focused coverage. No user input is
replayed as raw keyboard/mouse events.

Wire framing does not authenticate a peer. Persistent EOS lifecycle, validated
peer dispatch, host/join UI and scene transitions remain next; no connected
frontend/paired/full-match acceptance claim. Every new case is in the deferred
Mac manifest. No whole suites ran.

Authenticated channel/RTT extension: **33 further distinct cases passed**, zero
failed/skipped. Together with the 22 framing cases this feature has 55 distinct
passes; compilation separately clean.
- `OnlineSetupChannelEditorTests`: 24/0/0 at `Runs/20261114T234304037Z/results.xml`.
- `OnlineRoundTripStatisticsEditorTests`: 7/0/0 at `Runs/20261114T234528535Z/results.xml`.
- Two additional post-start rejoin cases: 2/0/0 at `Runs/20261114T234645532Z/results.xml`.

Channels consume the authenticated transport's peer identity, perform exact
compatibility checks and bind a fresh nonce/connection lifetime before accepting
setup requests. Role dispatch prevents a guest injecting host snapshots. Local
transport loss cannot be released by a cached snapshot. Rejoin clears setup
readiness before start and preserves an already agreed transition afterwards,
without repeating its event. Acknowledgements queue before local UI reactions.
Tests exchange real serialized fragments and complete both-team agreement/start.

Heartbeat round trips use the sender's monotonic clock, with bounded pending
requests and 128 samples. Telemetry exposes last/p50/p95; fake delay verified a
90 ms measurement. This is algorithm/serialized-channel evidence, **not measured
internet latency**. The native EOS lifetime and authored host/join controls still
need integration. No actual frontend paired/native lobby acceptance is claimed.
