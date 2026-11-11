# Online recovery

- [x] Injectable monotonic liveness timeout and sequenced authenticated heartbeats.
- [x] Authority connection generations; retire peers before permitting return.
- [x] Reserved-account return; snapshot acknowledgement gates release.
- [x] Connection hold independent of shared pause; preserve original time scale.
- [x] Focused owned-board preview, pending roll, movement and paused recovery tests.
- [x] Execute focused EditMode and PlayMode category OnlineRecovery.
- [ ] Wire heartbeat/rejoin/snapshot exchange into EOS and guest submission.
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
