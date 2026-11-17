# Published guest pitch highlights

- [x] Show unambiguous published destinations using existing authored hex renderers.
- [x] Distinguish eligible players, selected player and selected target.
- [x] Preserve source host highlights; never enable guest gameplay managers.
- [x] Clear hints on pause/connection hold, decision withdrawal and component disable.
- [x] Reject ambiguous/noncanonical coordinate hints and hidden/off-board actor hints.
- [x] Verify both directions and 32/36 actors, unchanged occupancy, state, save/RNG and hierarchy.
- [x] Register every new test case separately for the deferred Mac run.
- [x] Refresh Windows exports and packaged checks.

Pure presentation, Room v18/wire v7 unchanged. Hints use the authority's existing
legal choices and projected roster; no local movement or action legality is
calculated. Cyan marks destinations, yellow eligible players, orange selection.
Only full-Room guests render these hints; hosts keep the source managers' visuals.
Tracked hints are cleared independently of the authority's highlighted-hex list.
Existing hex materials are recolored through HexCell's normal API; no visual
objects, layout components or changes under Canvas/MatchStatsUI/Stats are created.

Live Editor compilation clean, zero C# errors. Four pre-fix scene failures at
TestResults/GameplayRegression/Runs/20261117T053606931Z/results.xml reproduced
missing guest destination highlights. Final isolated fixture: 14 passed at
20261117T053958682Z/results.xml. Final authored scene fixture: four passed at
20261117T054019263Z/results.xml. Zero final failed/skipped. Native artifacts are
under Runs. All 18 cases are registered individually and fixture hashes validated.

Coordinate hints are grouped in one pass, avoiding a repeated full action scan
for every hex; ambiguity follows the same clickable-choice semantics. No CPU or
internet latency measurement is claimed. This does not project risk/path previews
that have not been published. No paired or whole-suite tests ran. This visual
change does not require gameplay suites. For later broader user UI checks, use
Assets/Tests/PlayMode/Features/OnlineBoardInput / OnlineBoardInputPlayModeTests and
OnlineFullRoom / OnlineFullRoomPlayModeTests, which share input/display wiring.

Windows builds all Succeeded November 17 UTC: normal build_cc03382d7033
(20261117T054330Z-normal-Windows-build), Room build_19c9c0de2bf3
(054412Z-Room-Windows-build), prepared build_6f40948d50e6
(054434Z-Gameplay-Windows-build). Summary artifacts are under Runs. Zero C#
errors, nine normal/two proof warnings and the known nonfatal EOS missing-vswhere
post-build diagnostic (one reported post-build error/profile).

Packaged checks passed: 20261117T054502288Z-Room-local, 20261117T054516265Z-Gameplay-local and 20261117T054523891Z-Room-login (liveness registration true). The 13 report-tool unit tests passed. These local proof roles do not replace native fresh-guest isolation or paired internet acceptance.
