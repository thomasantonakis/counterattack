# Authority-published authored match instructions

- [x] Reuse the original authored instruction panel on host and guest.
- [x] Read the authority's existing manager instructions without enabling monitor updates, AI analysis, key subscriptions or instruction logging.
- [x] Carry bounded text, actual decision owner and flash status in an optional detached observation.
- [x] Keep instruction-only publications outside command admission boundaries.
- [x] Hide instructions under shared pause, connection hold and blocked gameplay.
- [x] Keep original kit palettes and local flash animation in the existing panel.
- [x] Describe one confirmation per manager in online kickoff setup; preserve offline wording.
- [x] Complete final live-pass/payload/clock checks, manifest registration and exports.

Room is now v19; wire remains v7. OnlineRoom was edited through live CLI authoring:
only one serialized instructionMonitor reference was added to the existing board.
The original monitor remains disabled. GameDebugMonitor's normal Update and
RoomDecisionManager analysis/logging paths are not invoked. The new explicit
rendering path only updates the authored instruction text and its palette;
Canvas/MatchStatsUI/Stats is unchanged. Bootstrap clears the authored placeholder
until a real observation is displayed. Optional missing instructions remain null
for isolated/non-UI fixtures and prepared proofs.

Compilation clean through the live Editor, zero C# errors. Native pre-fix run
TestResults/GameplayRegression/Runs/20261117T054817002Z/results.xml reproduced
eight placeholder-text failures. A final polling HTTP 400 did not lose that run:
subsequent test_status confirmed completed 0/8 and native XML was retained.
Eight authored Home/Away setup cases passed at 20261117T055411284Z/results.xml.
The 13 isolated validation/immutability cases passed at 20261117T055715591Z.

The first two live-pass cases at 20261117T055731002Z reached correct host/guest
text but their final fixture compared occupancy against the initial guest
formation, before projecting the real kickoff-taker move. The fixture now
captures occupancy after the first authoritative projection; subsequent display
and holds must leave it unchanged. Production gameplay was unchanged for this
fixture correction. All native artifacts are under Runs.

For later broader user UI verification, use
Assets/Tests/PlayMode/Features/OnlineFullRoom / OnlineFullRoomPlayModeTests,
OnlineRoomCoinToss / OnlineRoomCoinTossPlayModeTests and OnlineShootout /
OnlineShootoutPlayModeTests. These share match observation/display and more
specialized instruction sources. No complete suite or paired run occurred.
Actual internet latency and cross-machine acceptance remain deferred.

Final own feature: 23 distinct passed, zero final failed/skipped (13 EditMode and
ten PlayMode). Corrected live pass cases passed at 20261117T055859966Z/results.xml.
Four affected clock-publication cases passed at 20261117T055943497Z; 15 affected full-payload cases passed at 20261117T060028540Z;
selected native artifacts are under Runs. All 23 new cases are registered
individually for the deferred Mac run, fixture inventory/hashes validated.

Windows exports all Succeeded November 17 UTC: normal build_72ce597d163f at
20261117T060305Z-normal-Windows-build; Room build_5cc7c1e4d3f3 at
20261117T060356Z-Room-Windows-build; prepared build_c3bb6574a1bc at
20261117T060424Z-Gameplay-Windows-build. Summary artifacts are under Runs.
Zero C# errors, nine normal/two proof warnings and the known nonfatal EOS
missing-vswhere post-build diagnostic (one reported post-build error/profile).
The 13 report-tool unit tests passed; Mac scripts have no stale hard-coded
Room catalog. Actual paired internet acceptance remains deferred.

Packaged checks passed: 20261117T060441099Z-Room-local, 20261117T060455014Z-Gameplay-local and 20261117T060502580Z-Room-login (liveness registration true). These local proof roles do not establish paired transport or fresh-guest isolation; native authored guest projection is tested separately above.
