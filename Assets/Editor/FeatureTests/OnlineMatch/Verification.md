# Online match foundation — M0 first batch

2026-11-09. Scope: isolated provider-independent identity, compatibility and
command admission. No existing gameplay manager, scene or UI callback is changed.
No live network service is installed or configured.

## Implemented and verified

- [x] Two distinct reserved account/team assignments; host may be either team.
- [x] Exact protocol/rules/catalog compatibility before connection binding.
- [x] Third accounts and conflicting connection identities cannot claim a seat.
- [x] Provider-authenticated sender maps to a team; commands cannot claim one.
- [x] Current revision/decision/semantic-action allowlist checks.
- [x] One accepted action consumes the decision; adapters must dispatch it once.
- [x] Duplicate acknowledgements, conflicting sequence reuse and bounded receipt
  retention with high-water rejection after receipt eviction.
- [x] No new admission while a participant is disconnected.
- [x] Reconnect preserves seat/receipts; old connection traffic stays invalid.
- [x] Malformed/null/oversized IDs, bad sequences and negative revisions rejected.
- [x] JSON command roundtrip still enforces the actual connection's team.
- [x] Invalid decision publication leaves the prior decision intact.

The protocol is single-threaded and in-memory. Only a provider adapter that has
verified identity may call BindAuthenticatedAccount. Admit returns authorization
and admission, not gameplay execution/completion. PublishDecision must receive
legal IDs from authoritative gameplay. Neither API is exposed to remote callers
directly. Persisted reconnect/host-crash recovery and bounded wire decoding are
future integration work; identifier validation is not a complete packet decoder.

## Actual execution

- Unity 6000.5.2f1, live Editor through CLI/Pipeline.
- Compilation: completed, zero errors; Editor not compiling/reloading afterward.
- Loaded Room scene: clean before the test run; no scene save required.
- Explicit fixture: OnlineMatchProtocolEditorTests (category OnlineMatchProtocol).
- EditMode: **27 passed, 0 failed, 0 skipped, 0 inconclusive**.
- Native XML: TestResults/GameplayRegression/Runs/20261109T202334308Z/results.xml.
- Per-case evidence: progress.jsonl and current-test.txt in that same run directory.
- No PlayMode, broader regressions or full suite executed for this isolated batch.

Rerun through the open Editor only, after checking compilation and loaded scene
dirty state per AGENTS.md:

```sh
unity command run_tests --mode editor --filter OnlineMatchProtocolEditorTests --filter_type testName --async_tests true --project-path /Users/t.antonakis/Documents/Projects/Personal/counterattack --caller codex --skill unity-cli --format json
unity command test_status --project-path /Users/t.antonakis/Documents/Projects/Personal/counterattack --caller codex --skill unity-cli --format json
```

An async `running` response is not a pass. Inspect the returned counts and native
XML. There is no reason to run existing gameplay suites for this unwired class.

## Pending M0/M1 evidence

- [ ] User-owned EOS development product/client configuration; see
  Documentation/Plans/EOSSetup.md. User requested setup instructions, not an
  existing product connection.
- [ ] Pin and validate SDK/transport/native compatibility with this Unity version.
- [ ] Actual two-identity authentication, private room/code and connection proof.
- [ ] Windows/macOS both host directions on distinct networks, including fallback.
- [ ] Packet/byte limits, loss/jitter/reconnect and current free service quotas.
- [ ] Network message decoding and authenticated provider adapter.
- [ ] Room replica/input gateway, actual ground pass/interception and outcome sync.
- [ ] Shared pause UI, team substitutions and settings (rules recorded in plan).
- [ ] Durable receipts/checkpoints, host crash policy and resumed-match state.

These tests prove local protocol invariants, not online gameplay or EOS readiness.
