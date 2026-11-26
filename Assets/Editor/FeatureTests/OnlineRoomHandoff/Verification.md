# Online Room handoff

- [x] Continue the native frontend connection across Draft ? authored OnlineRoom.
- [x] Own host/replica save contexts and restore prior local context at teardown.
- [x] Use continuous semantic controls without resetting full drafted rosters.
- [x] Execute focused context/lifetime and encoded real-Room transport cases.
- [x] Refresh Windows builds and retain evidence.
- [ ] Paired internet acceptance, measured latency and remaining match menus.

The native path is implemented; tests/builds are required before claiming this
phase verified. Mac execution remains deferred until the user requests it.

Initial native evidence:
- `20261115T180521358Z/results.xml`: 10 EditMode context/failed-launch cases passed.
- `20261115T180539214Z/results.xml`: four encoded full Room kickoff cases passed.
Both live under `TestResults/GameplayRegression/Runs/`; zero failed/skipped.
Compilation passed separately. The encoded cases exercise real host UI commands,
authenticated remote intentions, resync acknowledgement and detached snapshots
through the complete wire codec against the actual Room managers.

Review also identified teardown ordering: dispose the gameplay exchange before
its owning EOS probe releases the platform. A dedicated regression is being added.
No paired internet run or latency measurement has been executed.

Final native results (all under `TestResults/GameplayRegression/Runs/`):
- `20261115T181316630Z/results.xml`: 13 own EditMode cases passed.
- `20261115T180807610Z/results.xml`: 20 full Room PlayMode cases passed
  (16 existing startup/projection plus four new encoded kickoff cases).
- `20261115T180736124Z/results.xml`: seven affected manual controls/recovery cases passed.
- `20261115T181324718Z/results.xml`: 12 affected frontend entry cases passed.
No failures/skips in these final runs; compilation passed separately.
`20261115T181153316Z` retains the two red teardown cases before the fix;
`20261115T181226112Z` retains the first green 12-case lifecycle run.
The final lifecycle count adds old-client rejection. Frontend rules are now v6;
older clients are rejected before Draft can hand one side to an absent full Room.

The live frontend owns scene load, a dedicated authority/replica JSON context,
continuous semantic exchange and teardown. Full rosters retain their session ID
through host initialization. Gameplay subscriptions dispose before EOS releases
its platform, including when cleanup raises an exception. Local save selection
and pending intro context are restored; a failed launch leaves no active session.

Broader offline match/menu regression is deferred to the user: select existing
Room/startup, substitution and shootout folders when their corresponding online
integration lands. No complete suite or paired internet acceptance was run.

Windows exports succeeded:
- Normal eight-scene game: `build_3e2300893217`, evidence
  `20261115T181337Z-normal-Windows-build/build-summary.json`.
- Room proof: `build_984b80d4e982`, evidence
  `20261115T181605Z-Room-Windows-build/build-summary.json`.
- Prepared gameplay proof: `build_18fa40e32639`, evidence
  `20261115T181627Z-Gameplay-Windows-build/build-summary.json`.
All paths are under `TestResults/GameplayRegression/Runs/`. Builds report
Succeeded with one known nonfatal EOS `vswhere.exe` discovery diagnostic;
normal game has nine warnings, proofs two each. No C# compilation errors.
Packaged checks passed: `20261115T181642013Z-Room-local`,
`20261115T181702873Z-Gameplay-local` and `20261115T181717606Z-Room-login`.
The login check registered liveness notifications successfully. Only report,
evidence and detached final-state JSON are retained in Git; raw Player logs stay
local. These are isolated proof profiles, not full normal-game internet acceptance.
