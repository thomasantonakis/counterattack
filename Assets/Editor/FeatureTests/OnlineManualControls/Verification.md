# Manual online controls

- [x] Author inspectable start, hex X/Z preview/confirm, defending roll and pause/resume controls.
- [x] Route host and guest intentions through the same authority and shared-pause gateways.
- [x] Personal camera/statistics remain separate; no designer Stats layout changes.
- [x] Add explicit `-ca-online-manual` standalone option; ordinary automated proof remains selectable.
- [x] Execute focused category OnlineManualControls (both attacking directions).
- [x] Build and retain packaged local recovery result.
- [x] Manual exchange heartbeat/rejoin, lifetime nonce/generation checks and full-state acknowledgement before hold release.
- [x] Execute real host/guest exchange iterators with encoded packets, injected clock and interrupted transport, in both directions.
- [ ] Paired human-controlled EOS run and actual network interruption/rejoin.

This owns controls in OnlineGroundPassProof, not Room. The guest remains inert.
The first complete action ends the proof after final-state digest acknowledgement.
Pending controls block retries locally; authority still validates every command.
Tests exercise EventSystem button pointer callbacks, real pass/interception,
wrong-team roll/resume, invalid target and disconnected submission.

Compilation separately clean, zero reported script errors/warnings. Final
focused PlayMode category OnlineManualControls: **5 passed, 0 failed,
0 skipped**. Native XML:
`TestResults/GameplayRegression/Runs/20261111T224700476Z/results.xml`.
Two cases drive the actual ManualHost/ManualGuest iterators through a bounded
encoded fake transport, drop delivery at the pending real interception roll,
advance the injected liveness clock, rejoin, and inject a queued old-generation
confirmation before the defending human-button callback. Both exchange loops
finish with one authority roll, one pass attempt, an inert guest and matching
final digests. Their paired evidence JSON is retained in adjacent
`*-ManualTransport-1/` and `*-ManualTransport--1/` run directories. This is not
an EOS transport or two-machine run. Three other cases verify authored real
buttons and pending/disconnected input behavior.

The live Editor returns to the clean Room scene. Scene YAML was saved through
Pipeline; Unity serializer whitespace is retained. Do not normalize Room or
the proof scene merely to remove generated m_Name whitespace.

Final Windows export build_caf18b6a417f reported Succeeded, with the existing
missing vswhere.exe diagnostic (one error entry) and seven warnings. The rebuilt
application's bounded local gameplay/pause/recovery check passed:
`TestResults/GameplayRegression/Runs/20261111T224837579Z-Gameplay-local/result.json`.
Its evidence records recoveryGenerations=2, sharedPauseRevision=2 and one roll.
The export is local and ignored; no credentials or application bundle are
committed. The packaged local check is separate from the five framework cases
and does not exercise EOS or the two-human interactive exchange.
