# First owned online Room action

- [x] Author a dedicated Room copy using the real Room serialized managers.
- [x] Keep offline Room startup separate from the deterministic owned proof.
- [x] Reuse authored semantic controls, authenticated exchange and live recovery.
- [x] Add safe pass/successful interception/failed interception cases in both directions.
- [x] Execute focused category OnlineRoomAction and inspect actual results.
- [x] Validate roster/catalog compatibility and detached presentation.
- [x] Build Windows and execute packaged local/login checks.
- [ ] Build Mac and execute paired manual Room proof with recovery (deferred with user).

Compilation separately clean. Final EditMode: **6 passed, 0 failed, 0 skipped**,
`TestResults/GameplayRegression/Runs/20261112T070120310Z/results.xml`.
Final real Room PlayMode: **6 passed, 0 failed, 0 skipped**,
`TestResults/GameplayRegression/Runs/20261112T064334702Z/results.xml`.
Safe passes and both interception outcomes use real Room manager references,
fixture-rigged dice and both attacking directions. Each checks requester-owned
pause across rebind, once-only stats/roll, ownership and next continuation,
then normally loads a fresh inert Room guest and applies the final snapshot.
The host's legacy pause and intro handlers are explicitly inactive online.
The live authority clock advances, freezes during connection hold and resumes
without replay. Clock-only publications keep the legal decision revision, so
an otherwise legal command remains valid while travelling. The first-action
completion snapshot is immutable until its final digest acknowledgement.

Snapshots include catalog/roster identity, accepted command/control sequence
watermarks, statistics and detached timing/clock data. Animation frames have
their own sequence and publication reference and cannot advance rules. Wrong
catalog/roster, stale/conflicting animation and invalid positions are rejected
without replacing committed state. Wire v3 requires both applications rebuilt.
Unused null DTOs are omitted from control/heartbeat frames; the affected wire
and pause EditMode checks pass (13 and 11 cases respectively).
V3 fragments carry a message identity and a 26-byte header (926-byte maximum
packet); late fragments cannot be combined with a returning connection's
payload. Recovery can restart an interrupted assembly while strict normal
framing remains enabled. Presentation and full timed snapshots are bounded.

Final Windows Room export `build_a1d82ad2db2d`: Succeeded, seven warnings and
the known missing vswhere.exe diagnostic (one error entry). Packaged local:
`TestResults/GameplayRegression/Runs/20261113T163255120Z-Room-local/result.json`
passed with one roll, sharedPauseRevision=2 and recoveryGenerations=2; native
build report is retained beside it. Packaged login:
`TestResults/GameplayRegression/Runs/20261113T163317130Z-Room-login/result.json`
passed, including native interruption/close notification registration and
cleanup without a second peer. These are separate application reports, not
framework cases or actual EOS interruption/rejoin evidence.

See Documentation/Setup/OnlineRoomProof.md for the exact Mac build/local/manual
run and reverse-role commands. Actual EOS interruption/rejoin, both human
applications and reverse roles remain unexecuted. The authored six-token roster
is a deterministic fixture, not production content/catalog integration.
