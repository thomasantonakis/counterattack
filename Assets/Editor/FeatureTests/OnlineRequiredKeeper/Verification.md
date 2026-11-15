# Online required goalkeeper decisions

- [x] Reproduce the missing affected-team hold and legacy pause-panel interference.
- [x] Hold required keeper decisions without transferring an existing user pause.
- [x] Permit only the affected team to fix its required keeper; block premature resume.
- [x] Author emergency nomination and force appropriate bench/outgoing choices.
- [x] Project requirements, roles, zeroed emergency attributes and goalkeeper kit.
- [x] Verify both teams/directions, substitutions exhausted/bench missing, pause ownership and negative cases.
- [x] Register new cases for deferred Mac testing and retain native evidence.
- [x] Refresh the normal Windows build.

Mandatory team decisions must preserve requester-owned pause/resume. Ordinary
pause controls remain passive for the other team. The affected team owns its
keeper choice, including when a prior user pause belongs to its opponent.

Implementation policy:
- A required goalkeeper creates an authority hold owned by the affected team when
  no user pause exists. An existing requester's pause remains theirs.
- Only the affected team can supply its mandatory keeper choice, including during
  an opponent-owned pause. Ordinary controls remain requester-owned.
- Resume is blocked until the real goalkeeper requirement clears. Completing an
  emergency nomination does not silently release an existing shared pause.
- Send-off replacement uses a playing outfield player off and bench keeper on;
  double injury replaces the injured keeper. Emergency nomination uses the real
  eligibility/conversion rule without spending a substitution.
- Role, requirement and kit changes project to the guest without rules or events.
  All selection controls and visual parts are authored in the scene.

The first reproducer exposed the offline pause-panel activation. The next run
reached the final visual check and exposed exact-float colour comparison; the
fixture now checks a 1e-5 material precision bound. No gameplay assertion or rule
was weakened. The sent-off reproducer subsequently passed; both full matrices passed. Native Mac execution and internet acceptance remain deferred.


Focused Windows native evidence (all under TestResults/GameplayRegression/Runs):
- `20261115T204309481Z/results.xml`: sent-off reproducer, 1 passed.
- `20261115T204419565Z/results.xml`: forced bench replacements, 16 passed.
- `20261115T204750695Z/results.xml`: emergency nominations, 16 passed.
All 33 cases passed with zero failed/skipped; compilation separately clean.
Each case is registered individually for deferred Mac execution. These exercise
both teams/directions, send-offs/double injuries, no bench/no allowance,
authored confirmation, duplicate requests, illegal nominees, premature resume,
and guest role/kit/state projection. Ordinary substitution rebind/replay coverage
remains in OnlineSubstitutions; no separate forced-keeper reconnect claim is made.

Retained red evidence: `20261115T203511389Z/results.xml` reproduces the offline
pause panel bug; `20261115T204026792Z/results.xml` records the float fixture issue.

Affected focused checks also passed: ordinary substitutions 18
(`20261115T205129191Z/results.xml`), Room menu 6
(`20261115T205524289Z/results.xml`), manual controls 7
(`20261115T205615185Z/results.xml`) and full payload bounds 15
(`20261115T205643813Z/results.xml`). Thirteen Python verifier tests passed.
The live Editor reports completed compilation, failed=false, errors=[],
compilationFailed=false. No whole suite ran. A broader transport-hold follow-up, if requested, is
`Assets/Tests/PlayMode/Features/OnlineSharedPause/`, fixture/category
`OnlineSharedPausePlayModeTests` / `OnlineSharedPause`: this batch adds a required
keeper hold to that shared pause path. It was not executed in this batch.

Encoded real Room kickoff: 4 passed, zero failed/skipped, in
`20261115T205658232Z/results.xml`.

Normal eight-scene Windows export succeeded: `build_9d36e69b262f`, evidence
`20261115T205908Z-normal-Windows-build/build-summary.json`. Nine warnings and
one known nonfatal EOS `vswhere.exe` discovery diagnostic remain; zero C#
compilation errors. The isolated proof exports/local/login checks from 8be708e
precede this full Room-only keeper UI; paired internet play remains unverified.
Unity-authored scene/meta serializer whitespace is preserved.
