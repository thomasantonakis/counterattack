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
