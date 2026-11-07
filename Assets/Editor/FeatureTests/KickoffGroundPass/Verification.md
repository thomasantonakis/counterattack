# Kickoff Ground Pass

- [x] Opening kickoff offers Ground Pass only; C/L input cannot activate aerial actions.
- [x] Shared kickoff Ground Pass rules apply at every difficulty and in both directions.
- [x] Own-half kickoff targets have no distance cap; opponent-half and x=0 targets use normal Standard Pass range, blocking and interception rules.
- [x] Preserve the existing special branch's interception suppression and normal blocking rules.
- [x] Clean up the exemption after the kickoff pass; ordinary distance limits still apply.
- [x] Older snapshots retain P/C/L; new kickoff snapshots preserve Ground Pass only.
- [x] The protected Room settings/rosters seed, which has no runtime snapshot, retains P/C/L after setup confirmation and action refresh.
- [x] Do not edit the Room direct-play seed or working save JSON.
- [x] Final live Editor compilation and focused fixture results recorded below.

Run only `KickoffGroundPassPlayModeTests` in
`Assets/Tests/PlayMode/Features/KickoffGroundPass/`. The owned deterministic fixture
is in this folder and uses no live saves. Mode checks exercise shared action
availability under Hotseat, Single Player and Multiplayer settings; they do not
claim a multiplayer transport integration test.

Initial reproduction: `TestResults/GameplayRegression/Runs/20261107T210935769Z/results.xml`.
The six opening option cases reproduced the unwanted aerial options. Iteration
also corrected fixture handling of rejected synthetic inputs and setup selection.

Initial implementation validation, 2026-11-07: live Editor recompile/reload clean; **32 passed,
0 failed, 0 skipped** in `KickoffGroundPassPlayModeTests`. Native XML and per-case
progress: `TestResults/GameplayRegression/Runs/20261107T211501818Z/`.
Includes opening, post-goal, half 2 and half 4 restarts; later halves exercise
Away as the kicking team. Both attacking directions and all three difficulties
are covered. Half-transition ceremony itself remains in broader integration tests.
Loaded MainMenu scene was clean before execution. No full suite was run.

Clarified rule: kickoff is not restricted to the own half. The own-half branch
alone removes the range cap and suppresses interceptions. Targets in the
opponent half and on x=0 follow the ordinary Standard Pass branch. The new
`OpponentHalfAndHalfwayUseNormalPassRules` cases cover legal near targets,
out-of-range targets, blocking, danger and actual pass completion at all three
difficulties and in both directions. Final corrected-run results follow below.
The 12 new cases reproduced the rejected-target defect before the correction:
`TestResults/GameplayRegression/Runs/20261107T212046025Z/results.xml` (0 passed,
12 failed at the legal-target assertion).

Corrected validation, 2026-11-07: live Editor recompilation clean; **44 passed,
0 failed, 0 skipped** in `KickoffGroundPassPlayModeTests`.
Native XML and per-case progress:
`TestResults/GameplayRegression/Runs/20261107T212253632Z/`.
This supersedes the earlier own-half-only expectation. No full suite was run.

Room direct-play follow-up: live inspection found `KickoffBlown`, Ground Pass
available but not activated, 342 legal targets, and recent clicks without a P
keypress. Hover previews start only after P; an open inspection card suppresses
gameplay hover/physical action keys. The confirmed regression was P-only options:
the protected seed contains settings/rosters and no runtime snapshot, so the
legacy-snapshot exception did not apply. Loading now records the protected source
before cloning; `StartMatch` preserves P/C/L only for that editor fixture.
`SeedWithoutSnapshotPreservesOpeningChoices` covers this and ordinary new games
in both directions, including P activation and an actual hover preview.
Follow-up validation: **4 passed, 0 failed, 0 skipped**, native results in
`TestResults/GameplayRegression/Runs/20261107T213723524Z/`. Compilation clean.
The canonical JSON SHA-256 remained
`93723fc631b6447e874540e2c9408ce405416a5d2c2f26e3cfd1503b02e9d2fe`
before and after this verification. Broader kickoff tests were not rerun in this follow-up.

Broader checks for the user: `LegacyKickoffPlayModeTests`,
`LegacyCodexKickoffPlayModeTests`, and `LegacyMovementGoalPlayModeTests` in
`Assets/Tests/PlayMode/`, for setup, AI opening preparation and goal-to-restart
integration. These are not automatically executed with this feature fixture.
