# Online header select-all

- [x] Reproduce missing authenticated bulk nomination / A action.
- [x] Execute the existing attacker/defender select-all and confirmation methods.
- [x] Preserve two-attacker and two-outfield-defender limits plus an eligible keeper.
- [x] Reject bulk selection above the limits while retaining individual nomination.
- [x] Respect owner, guest, connection hold, duplicates and ambiguous A meanings.
- [x] Traverse a real high pass, all actual rolls and winner/next headed-pass decision.
- [x] Register every new native case for the deferred Mac run.
- [x] Complete selected regressions and grouped Windows export refresh.

Full Room contract is v17, wire v7. The new semantic header.all action uses the
already-authored generic command controls or A. It is available only when the
existing SelectAllAvailableAttackers / SelectAllAvailableDefenders rule permits
bulk selection. No lineup is guessed when there are too many candidates. It
confirms exactly as the normal offline A key does. Foul advantage retains A;
ambiguous published A meanings are rejected.

Compilation clean through the live Editor, zero C# errors. Own-feature coverage:
18 passed, zero failed/skipped: OnlineHeaderSelectAllEditorTests two at
TestResults/GameplayRegression/Runs/20261116T220137521Z/results.xml and
OnlineHeaderSelectAllPlayModeTests 16 at 20261116T220152170Z/results.xml.
The 16-case pre-fix native reproduction at 20261116T215930091Z is retained.

Native cases use a real high pass into actual nomination, both teams/directions,
optional keeper participation, over-limit groups, manual fallback, source rolls,
encoded commands/nomination snapshots and winner statistics/continuation. They
do not inject nomination or expected result flags. All 18 cases have individual
deferred Mac rows and fixture inventory/hashes validate. No paired or complete
suite ran. Exports now include v15-v17; see the build evidence below.

For later broader user checks use Assets/Tests/PlayMode/Features/OnlineHeaders
/ OnlineHeaderResultPlayModeTests and OnlineHighPass / OnlineHighPassPlayModeTests:
aerial landing, header result presentation and continuations are shared. Only
selected directly affected methods are executed in this phase.


Affected verification: 57 passed, zero failed/skipped: HeaderNominationsAndFreeHeader
four at 20261116T220430874Z; ContestedHeaderCompletesHeadedPass eight at
20261116T220453717Z; OnlineContextKeysEditorTests 45 at 20261116T220527365Z.
Native XML/progress are retained under the same Runs root. The 13 report-tool
unit tests pass; manifest inventory and hashes validate.

Windows exports succeeded with all v15-v17 source changes: normal
build_779f353246e4 at 20261116T220736Z-normal-Windows-build, Room
build_c8f8ef81c357 at 20261116T220818Z-Room-Windows-build, prepared
build_9ed514ebea55 at 20261116T220840Z-Gameplay-Windows-build. All report
Succeeded with zero C# compilation errors, nine normal/two proof warnings and
the known nonfatal EOS missing-vswhere post-build diagnostic (one reported error).
Packaged checks passed: Room local 20261116T220910864Z-Room-local, prepared
local 20261116T220933002Z-Gameplay-local, and Room login/liveness
20261116T220947732Z-Room-login. Local roles do not prove fresh guest inactivity;
that is covered by the dedicated real-scene native fixtures.
