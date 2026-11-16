# Reliable Room publication timing

- [x] Exercise the real ManualHost loop, authenticated recovery handshake and encoded full snapshots.
- [x] Verify same-stage/same-owner player reselections publish on the next host iteration, within the 250 ms clock refresh window.
- [x] Reject old decision commands without replacing the accepted selection.
- [x] Publish pause and resume immediately and keep idle heartbeat near one second.
- [x] Complete actual kickoff and check 250 ms clock-only refreshes preserve a prior legal command.
- [x] Register all 16 passing cases separately and retain native artifacts.

This is native verification of existing runtime behavior, with injected monotonic
transport time and deterministic authored boards of 32/36 actors in both directions.
It does not measure EOS RTT, internet packet loss or rendering latency. No runtime
transport change was required: PublishDecision/WithdrawDecision already advance
the revision used by the immediate-send boundary.

Live Editor compilation is clean, zero C# errors. Twelve selection/pause/heartbeat
cases passed at TestResults/GameplayRegression/Runs/20261116T232933699Z/results.xml.
Clock verification and final manifest evidence follow below. No whole suite or
paired Mac test ran. Existing Windows exports remain current because this phase
only adds test code. For later broader user verification, the affected folders are
Assets/Tests/PlayMode/Features/OnlineFullRoom (OnlineFullRoomPlayModeTests) and
OnlineManualControls (OnlineManualControlsPlayModeTests), sharing the host loop.

Two clock fixture iterations initially failed at the final command assertion:
20261116T233152883Z used Home although Away owned the kickoff; 233319353Z then
reused a sequence already consumed by Away UI commands. The fixture now preserves
the owner's authenticated connection and accepted sequence. Authorization and
production rules were not changed. Native failed artifacts are retained.

Final clock cases: four passed, zero failed/skipped at Runs/20261116T233434438Z/results.xml. Total own feature: 16 distinct passes. All cases registered separately for the deferred Mac run, fixture hashes validated.
