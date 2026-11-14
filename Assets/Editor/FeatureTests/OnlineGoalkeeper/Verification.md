# Online goalkeeper interruptions

- [x] Prepare typed free move/stay and deferred pickup/feet commands.
- [x] Prioritize mandatory goalkeeper decisions over parent pass/movement.
- [x] Ten focused PlayMode cases under category `OnlineGoalkeeper`.
- [x] Compile/reload in the live Editor.
- [x] Execute `OnlineGoalkeeperPlayModeTests`; retain and inspect native XML.
- [x] Resolve failures, run focused affected checks and refresh exports.
- [ ] Paired acceptance (deferred).

Windows refresh for checkpoint `61e3e38`: Room `build_db66e2a92ecd`
Succeeded (7 warnings), prepared `build_6fda365964c7` Succeeded (2 warnings).
Both reports retain the known missing-vswhere.exe diagnostic (one report error,
no script compile errors). Packaged local ground/pause/recovery checks passed
in `Runs/20261114T102124157Z-Room-local/` and
`Runs/20261114T102306317Z-Gameplay-local/`, with result.json,
gameplay-evidence.json and build-report.json. They do not prove paired online
goalkeeper play. Exact Mac continuation is in
`Documentation/Setup/OnlineMacContinuation.md`.

Compilation separately clean. Category `OnlineGoalkeeper`, fixture
`OnlineGoalkeeperPlayModeTests`: **10 passed**, zero failed/skipped, in
`TestResults/GameplayRegression/Runs/20261114T101909890Z/results.xml`.
The initial eight cases also passed in `20261114T101643166Z/results.xml`.
The final run adds interrupted pickup and direct guest-rejection coverage.
Deferred affected offline `LegacyKickoffPlayModeTests`: **2 passed**, zero
failed/skipped, in `20261114T101732849Z/results.xml`. No whole suites ran.

The earlier Editor command timeout was a low-disk-space dialog. The user closed
it and uninstalled software; C: subsequently reported approximately 129 GiB free
and Pipeline returned ready. No Editor restart, GUI automation, scene disk edit
or Pipeline upgrade was needed.

Tests exercise an actual ground pass entering the penalty box and a real headed
back-pass received at feet, followed by movement completion and pickup choice.
They include both directions, move/stay, hands/feet, ownership, duplicate commands
and connection holds. Source free-move/handling rules and continuations are reused.
The existing authored status text also displays published team goals. The Mac
build helper accepts a clean up-to-date recompile with compilationFailed null;
Bash syntax validation passed on Windows. Actual Mac execution remains deferred.

Broader user-run coverage: `Assets/Tests/PlayMode/Features/GoalkeeperPickup/`,
fixtures `GoalkeeperPickupPlayModeTests` and `GoalkeeperPickupEditorTests`, for
handling eligibility and saved touch provenance beyond the online adapter;
`Assets/Tests/PlayMode/Features/OnlineHighPass/`, category `OnlineHighPass`, for
additional parent aerial interruptions. No broad runs are implied by this batch.
