# Online stoppage actions

- [x] Reproduce an expired regulation/extra-time period requesting an unpublished roll.
- [x] Publish only the attacking manager's authenticated stoppage.roll / R intention.
- [x] Execute the existing D6 + referee leniency rule and capped allowance once.
- [x] Retain the existing five-second automatic roll through the host timing service.
- [x] Respect pause, transport, intro and outstanding result holds.
- [x] Publish expired/waiting/determined state, total and remaining actions.
- [x] Apply that state on the inert guest so the normal clock shows +? / added actions.
- [x] Validate malformed counts without advancing the replica sequence.
- [x] Complete focused runs and register every new Mac case; mark exports for grouped refresh.

Room contract is now full-room-v15; wire v7 remains unchanged. Both full-game
clients must pull and rebuild before paired use. Normal gameplay and automatic
rolls use the existing source calculation and pending restart/action continuation.
The normal offline stoppage presentation and rules remain in place; the online
host has its rules scripts disabled and uses the authenticated authored controls.
No scene or Stats layout change is needed.

The deterministic full-Room fixture advances the real private regulation-clock
method, isolating it from the authored kickoff allowance, then invokes the public
ResolveActionBeforeFinalThird boundary. It never sets the expected expired or
stoppage flags. Host commands and snapshots traverse the real encoder/decoder;
fresh guest cases inspect the real clock text, counts, authority inactivity,
no local RNG/rules/events and pending-command suppression. A separate path
advances the real host timing service through the automatic roll deadline.

Compilation is clean with zero C# errors. Final own-feature coverage: 23 passed,
zero failed/skipped: 11 OnlineStoppageActionsEditorTests at
TestResults/GameplayRegression/Runs/20261116T184348365Z/results.xml and 12
OnlineStoppageActionsPlayModeTests at 20261116T184652107Z/results.xml under
the same Runs root. Eight manual cases cover both teams, regulation/extra time,
and D6 boundaries 1/6; four automatic cases cover both teams and periods. Each
PlayMode case loads a fresh inert guest and checks encoded pending/result state.
The pre-fix eight-case reproduction (20261116T184057142Z) is retained: the real
stoppage prompt incorrectly remained kickoff.pending instead of publishing a roll.
An earlier fixture attempt expired the opening tactical allowance rather than
regulation; it was corrected without changing gameplay.

All 23 new cases have individual deferred Mac manifest rows; fixture inventory
and hashes validate, and the 13 report-tool unit tests pass. No paired test or
whole gameplay suite was run. Current Windows exports still contain v14 at
690c412; refresh both clients for v15 before paired use. Build refresh is grouped
with the next independently verified gameplay batch.

For later broader verification use Assets/Tests/PlayMode/Features/OnlineRoomCoinToss
/ OnlineRoomCoinTossPlayModeTests and Assets/Tests/PlayMode/Features/DecisionTiming
/ DecisionTimingLiveClockPlayModeTests: period transitions and timing are shared.
Those broader folders are handed to the user; only explicitly selected affected
cases are run by Codex during this phase.

Affected focused results: 48 passed, zero failed/skipped. Native XML/progress:
OnlineRosterReplicationEditorTests 13 at 20261116T185042732Z;
ExtraTimeStoppageEditorTests 16 at 20261116T185053787Z;
OnlineFullRoomPayloadEditorTests 15 at 20261116T185114914Z;
OnlineRoomCoinTossPlayModeTests.OrdinaryHalfTransitions four at
20261116T185135369Z. These are retained under the same Runs root.
