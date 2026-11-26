# Online recovery

Room/clock follow-up rerun: 8 EditMode passed at
`TestResults/GameplayRegression/Runs/20261112T063233913Z/results.xml` and 8
PlayMode passed at `Runs/20261112T063248480Z/results.xml`, zero failed/skipped;
compilation separately clean. A new coordinator holds from construction until
the first authenticated snapshot acknowledgement. Manual exchange observes
authenticated EOS interruption/close notifications and send failures, with
bounded heartbeat fallback. Known graceful leave is distinct from transport
loss. Actual two-machine EOS interruption/rejoin is deferred with the user.

- [x] Injectable monotonic liveness timeout and sequenced authenticated heartbeats.
- [x] Authority connection generations; retire peers before permitting return.
- [x] Reserved-account return; snapshot acknowledgement gates release.
- [x] Connection hold independent of shared pause; preserve original time scale.
- [x] Focused owned-board preview, pending roll, movement and paused recovery tests.
- [x] Execute focused EditMode and PlayMode category OnlineRecovery.
- [x] Wire heartbeat/rejoin/snapshot acknowledgement into the manual exchange and gate guest submission.
- [ ] Bounded packaged recovery scenario and separate-network paired run.

This is live-authority recovery infrastructure, not process-crash restoration.
Tests use disposable owned boards and injected clocks, with no live save writes.
Live Windows compilation is clean (zero reported compilation errors/warnings).
EditMode: 8 passed, 0 failed, 0 skipped; native XML:
`TestResults/GameplayRegression/Runs/20261111T223028082Z/results.xml`.
PlayMode, including ordered hold publications while shared-paused: 8 passed,
0 failed, 0 skipped; native XML:
`TestResults/GameplayRegression/Runs/20261111T223213503Z/results.xml`.
An initial fixture clock-reset defect and the rejection of pause requests
during a connection hold were diagnosed from native results and corrected.
Failed runs remain retained in Runs/20261111T222913027Z and
Runs/20261111T222946588Z. This does not complete handoff step 1.

Affected shared-pause PlayMode scope: 13 passed, 0 failed/skipped, native XML
`TestResults/GameplayRegression/Runs/20261111T223301198Z/results.xml`.
Windows export build_3d3a2bcd40cb reported Succeeded, seven warnings and the
known missing vswhere.exe diagnostic. Its bounded packaged local recovery
scenario passed at
`TestResults/GameplayRegression/Runs/20261111T223558929Z-Gameplay-local/result.json`:
recoveryGenerations=2, sharedPauseRevision=2, rollSequence=1 and one pass attempt.
Relay transport is explicitly not exercised by this local report. The manual
exchange's deterministic interrupted-transport tests are recorded under
OnlineManualControls/Verification.md. Actual EOS interruption/rejoin and Room
clock/startup integration still require acceptance evidence.
