# Frontend recovery

- [x] Preserve authoritative setup through temporary transport loss.
- [x] Retry an unanswered host hello with the same nonce and bounded queue.
- [x] Rotate nonce once per outage; reject stale generations and unknown accounts.
- [x] Reconcile accepted requests whose replies were lost using sequence watermarks.
- [x] Preserve agreed Draft settings and avoid duplicate transition events.
- [x] Reaccept only the reserved authenticated peer still present in the lobby.
- [x] Cancel after five minutes without extending the deadline on repeated events.
- [x] Focused native execution and recorded evidence.
- [ ] Actual interrupted/rejoined Windows/Mac EOS session (deferred).

Run the `OnlineFrontendRecoveryEditorTests` fixture or `OnlineFrontendRecovery`
category only. Serialized-channel tests exercise the production recovery driver;
they do not prove native internet recovery. Native notification/lobby membership
and legacy bootstrap reacknowledgement require paired acceptance.

Compilation through the live Editor completed with zero errors.
Own feature: 16 passed, zero failed/skipped at `20261115T053435028Z`.
Affected channels: 26 passed, zero failed/skipped at `20261115T053525114Z`.
Affected entry scenes: 6 passed, zero failed/skipped at `20261115T053605494Z`.
Native XML/progress is retained under `TestResults/GameplayRegression/Runs/`.
The first own-feature run (`20261115T053356798Z`) had 14 passes and two fixture
failures: a 22-character test name violated the real 20-character team-name rule.
The test now uses a valid name and exercises lost-reply recovery unchanged.
The expiry cases also send a repeated loss notification before the deadline.

The production session now observes EOS connection closure/request callbacks,
checks reserved identity/socket and current lobby membership before reaccepting,
and services the original bootstrap acknowledgement for the same returning
account. Control packets use an application-owned bounded queue without delayed
SDK delivery, so unsent obsolete-generation messages can be discarded on loss.
Native reconnect handling has compiled but remains unverified across machines.
This keeps the frontend state only; full Draft/Room recovery is still separate.

All 16 cases have separate deferred Mac manifest entries. Broader user-run checks
if desired: `Assets/Editor/FeatureTests/OnlineSessionTransport/` category
`OnlineSessionTransport`, and `Assets/Tests/PlayMode/Features/OnlineFrontendEntry/`
category `OnlineFrontendEntry`. No full suite was run.

Normal-scene Windows export `build_06972049607d` succeeded at
`20261115T053740Z-normal-Windows-build/build-summary.json` (9 warnings,
one known nonfatal EOS post-build vswhere.exe diagnostic, zero C# errors).
Output: `Build/OnlineGame/Windows/Counterattack.exe`; no ordinary match was
launched and no full multiplayer acceptance is claimed. The Mac one-command
verifier now also builds the normal game scenes through
`BuildEOSStandaloneMac.sh game`. Its 13 tooling tests and Bash syntax check
passed on Windows; actual macOS export remains deferred.
