# Authority-computed action previews

- [x] Publish the source pitch's path/risk markings, shading and raised goal markers.
- [x] Reuse authored guest hex renderers; never recompute interception/shooting rules locally.
- [x] Filter personal hover/debug/occupancy colors from shared observations.
- [x] Preserve assistance visibility: medium target selection does not invent danger information.
- [x] Restore original marker height/flags on pause, connection hold, withdrawal and input disable.
- [x] Keep source-held goal targets independent of the selected-target decision field, including headers.
- [x] Clear held markers on actual ball motion despite the authority Ball component remaining disabled.
- [x] Clear guest markers on streamed motion without changing the accepted logical snapshot.
- [x] Verify both directions, 32/36 actors, normal/dangerous pass previews and real shot trajectories.
- [x] Keep the existing expansion/wire limits with the largest roster, 1,900 previews and maximum instructions.
- [x] Finish final affected checks, manifest registration and Windows exports.

Room is now v20, wire remains v7. Optional preview records carry coordinate,
whitelisted reason, bounded shading/height and committed-goal display status.
They are sampled from existing authority highlights plus the ball's held target.
They are excluded from command admission fingerprints. Guest rendering layers
these observations over the existing published-choice fallback. No new visual
objects/layout are created and Canvas/MatchStatsUI/Stats is unchanged.

Ball's original LateUpdate marker expiry now delegates to an explicit visual
refresh, also called by authority capture while the component is disabled. The
held-target getter avoids losing header markers that are no longer in the
highlighted-hex list or logical selected-target field. Guest smoothing clears
local marker display when its ball moves; it does not mutate replica state.

Compilation clean, zero C# errors. Initial scene run
TestResults/GameplayRegression/Runs/20261117T062139352Z/results.xml had two
medium-assistance guard passes and ten failures. Six reproduced missing path or
selected-goal colors; four risky fixtures put a defender directly in the path,
which is illegal. Moving that defender to an adjacent interception position
corrected the fixture; production pass rules were unchanged. A final transient
HTTP 400 poll did not lose the run: test_status/native XML confirmed 2/12.

The initial capacity run at 20261117T063425864Z passed 21/22 and reproduced an
expanded state of 277,157 bytes, over the existing 262,144-byte limit. Compact
JSON property names fixed this without dropping data or raising caps. All 22
isolated cases passed at 20261117T063709987Z. The tested worst case now retains
36 actors, 1,900 preview entries and 8,192 instruction characters in 222,057
expanded bytes / 15,639 wire bytes; round-trip values are asserted unchanged.

Twelve pass/shot/assistance cases passed at 20261117T063728766Z. Final streamed
marker/hold cases: two passed at 20261117T070134578Z. Explicit disabled-authority
ball-motion expiry: two passed at 20261117T070224629Z. Total own feature: 38
independent cases passed, zero remaining failed/skipped. Native artifacts are
under Runs; failed diagnostic runs are retained. This is visual/transport
coverage, not a whole gameplay suite or paired internet/latency measurement.

For later broader user verification, use these exact folders/fixtures under
Assets/Tests/PlayMode/Features/: OnlineHeaders / OnlineHeaderResultPlayModeTests;
OnlineHighPass / OnlineHighPassPlayModeTests; OnlineLongPass /
OnlineLongPassPlayModeTests; OnlineFullRoom / OnlineFullRoomPlayModeTests. Those
actions share source highlight/goal-target code but were not all rerun here. Header-specific scene behavior remains an unexecuted
boundary for this batch; the common held-marker source is covered by shots.

All 38 new cases individually registered for the deferred Mac run; affected Windows evidence and fixture hashes refreshed. Final focused affected checks: four published-hint cases, four clock-publication cases and 15 full-payload cases passed after the final marker changes. These are selected fixtures, not complete suites.

Windows exports all Succeeded November 17 UTC: normal build_d31bc5a64cbc at
20261117T072059Z-normal-Windows-build; Room build_b793f599f918 at
20261117T072150Z-Room-Windows-build; prepared build_33b5be1d5b97 at
20261117T072218Z-Gameplay-Windows-build. Summary artifacts are under Runs.
Zero C# errors, nine normal/two proof warnings and the known nonfatal EOS
missing-vswhere post-build diagnostic (one reported post-build error/profile).

Final affected evidence: 20261117T071134665Z (four published hints), 20261117T071238307Z (four clock cases), 20261117T071323623Z (15 payload cases). Packaged checks passed at 20261117T072436843Z-Room-local, 20261117T072458877Z-Gameplay-local and 20261117T072513600Z-Room-login (liveness registration true). The 13 report-tool unit tests passed. Local proof roles are not fresh-guest or paired transport evidence.
