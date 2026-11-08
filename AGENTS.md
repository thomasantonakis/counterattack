## Unity Workflow Rules

- Never open scenes additively unless it is the only way to complete the task. This applies to authoring, inspection, verification, and temporary scene loading. Prefer the already loaded scene, prefab or asset inspection, safe file edits, or a normal scene switch that preserves unsaved work. Convenience alone is not a reason to use additive loading.
- Use the Unity CLI/Pipeline for all Unity Editor control. Before Unity editor operations, check the live CLI connection for this project; if it reports ready, use CLI commands for status, recompile/reload, console/log checks, saving assets/scenes, changing scenes, hierarchy inspection, hierarchy edits, and any user-requested Play/Pause/Stop actions.
- If a Unity CLI call fails inside the sandbox with an analytics/network allowlist error (for example, `api2.amplitude.com`), retry that same CLI call with approved escalated execution before concluding that the Editor or Pipeline is unavailable. Check `unity pipeline list` and `unity status --project-path /Users/t.antonakis/Documents/Projects/Personal/counterattack`; a live Editor process alone does not prove the Pipeline server is ready. If the Editor is alive but the Pipeline connection is absent, ask the user to restart the Pipeline server and then recheck status. Do not upgrade or reinstall the Pipeline package just to force a reconnect.
- Use Unity CLI exclusively for Unity Editor control instead of AppleScript, System Events, GUI scripting, simulated keystrokes, menu clicks, or hotkeys. If Unity CLI/Pipeline is unavailable, blocked by Safe Mode, starting up, or otherwise unsuitable, use safe direct file edits when appropriate or tell the user which manual Editor action is required.
- Target this project explicitly with `/Users/t.antonakis/Documents/Projects/Personal/counterattack` on Unity CLI calls, and pass the required caller/skill metadata on every `unity command` invocation.
- Never start Unity from Codex. Do not launch Unity, run Unity batchmode, or invoke `editor_play`.
- Never create UI or other visual structures only at runtime. Whenever creating or changing visuals, author them as scene or prefab objects available in edit mode so the designer can inspect and edit them before Play Mode.
- Treat `Canvas/MatchStatsUI/Stats` as designer-owned layout. Scripts must not create, move, resize, anchor, add layout components to, enable/disable, or otherwise restyle objects under it; they may only update text values and colors that represent team identity or player status.
- Before stopping Play Mode, query `editor_status` through the Unity CLI. Treat paused Play Mode as Play Mode. If Unity is playing or paused, invoke `editor_stop` and wait until `editor_status` confirms Play Mode has stopped. If the Pipeline connection is unavailable, ask the user to stop Play Mode manually. Do not kill the Unity process.
- After code changes, save through `save_all` when needed, run `unity recompile --project-path /Users/t.antonakis/Documents/Projects/Personal/counterattack`, then inspect the command result and `/Users/t.antonakis/Library/Logs/Unity/Editor.log` for compile errors. If the Pipeline connection is unavailable, ask the user to refresh/recompile manually.
- Keep fixing compile errors until the open Unity editor reports clean scripts. Only then tell the user what was fixed.
- Do not use `dotnet build Assembly-CSharp.csproj` as the Unity validation path for this project. It consumes time/tokens and does not stop or reload the open Unity editor.
- Use static shell checks such as `rg`, `sed`, and `git diff --check` freely. For Unity compilation, rely on the open editor reload and Editor.log.
- Codex is authorized to execute automated EditMode and PlayMode Unity Test Framework tests through the live Unity CLI/Pipeline, inspect their results, diagnose failures, and rerun affected tests. Framework-controlled entry/exit of Play Mode is authorized. Never launch Unity, use batchmode or GUI scripting, or start ordinary gameplay through `editor_play`.
- Before each test run, wait for compilation/reload to finish, inspect every loaded scene's dirty state through Unity CLI, save dirty scenes with `save_all`, then recheck that they are clean before starting tests. Check after recompilation, since editor initializers can modify Room during reload. Do not overwrite unsaved scene changes with on-disk files. A test-start timeout may be a blocking Scene(s) Have Been Modified dialog: inspect this possibility before attributing it to a freeze or restarting anything. An already-open modal needs the user's manual Save action; never use GUI scripting to dismiss it.

## Unity Test Framework Migration

- Prefer isolated EditMode tests for rules and PlayMode tests for coroutines, movement, input and interruptions. Use deterministic boards and rigged dice; wait for meaningful states with bounded timeouts.
- Framework tests must suppress the legacy runner, isolate singleton/event/scene/save state, and restore it even after failure. Never use live saves as mutable fixtures.
- Keep legacy scenarios until equivalent replacement tests have passed. Record mappings and actual results in `Documentation/Testing/GameplayRegressionMigration.md`.
- Execute relevant existing regressions after each focused migration batch. Report compilation separately from passed/failed/skipped test counts and retain result artifacts.
- Keep native XML and per-case progress under `TestResults/GameplayRegression/Runs/`, outside Unity’s temporary directory. Historical `Temp/GameplayRegression` artifacts are copied there so an Editor restart does not erase validation evidence.
- Automated framework tests use filters to rerun failures and affected regressions; Codex executes and inspects these tests itself. Do not comment out passing cases or ask the user to press Play for automated regression verification.

## Required coverage for future gameplay changes

- Classify the change before selecting tests. Purely visual changes (such as scoreboard dots, UI styling, layout, textures, or hiding a timer without changing clock logic) do not require gameplay tests or gameplay suites. Validate compilation and the relevant visual/UI behavior only. A presentation edit in a gameplay manager does not by itself make the change a gameplay behavior change.
- For every new feature, create a dedicated folder containing its Unity tests and a short task/verification checklist. Run only that feature's tests by explicit fixture or category filter. Do not expand into unrelated regressions or whole suites unless the user specifically requests them. Never wait for a whole suite by default.
- At handoff, assess whether the change may affect other features. If broader verification is warranted, tell the user the exact existing test folder paths and Unity Test Runner fixtures/categories to run, with a brief reason. If a complete regression check is warranted, prompt the user to press Run All on the appropriate EditMode and/or PlayMode tabs. The user runs these broader checks and reports failures; do not start them or keep the turn open waiting for results. When failures are reported, inspect the available artifacts, diagnose and fix them, and rerun only focused affected tests.
- Every change to gameplay code must add meaningful Unity Test Framework coverage: at least one new independently runnable test or new parameterized case for the changed behavior. This includes rules, possession, ball/player movement, action availability, input routing, coroutines, interruptions, scoring/statistics, restarts, and gameplay AI. A compilation check or changes to legacy runner scenarios do not satisfy this requirement.
- Reproduce a bug with the new test before fixing it when practical. Exercise real gameplay entry points, deterministic board setup and rigged dice; assert observable outcomes and relevant continuation/state/statistics. Cover affected boundary, negative and interruption cases, and both attacking directions where applicable. Do not copy the implementation into the test or set fixture flags that supply the expected outcome.
- Use EditMode for isolated rules/calculations and PlayMode for scene-dependent gameplay, input, animations and coroutines. Reuse the owned fixture/cleanup patterns; restore singleton, subscription, scene, time and temporary-save state even after failure. Never mutate live saves or designer-owned layouts.
- Execute the feature's own tests through the live Unity CLI/Pipeline and inspect their results. Hand broader affected-folder and full-suite verification to the user as described above. Never start or wait for a complete EditMode or PlayMode suite unless the user explicitly asks Codex to execute that full run in the current task. Report the executed scope without claiming full regression validation. Keep runs serial and resolve or cancel any pending execution before editing scripts or starting another run.
- Diagnose failures as gameplay defects, fixture defects or documented rule drift. Never weaken assertions or change gameplay merely to obtain green tests. If an intended rule is ambiguous, record the question in `Documentation/Testing/GameplayReview.md` and obtain clarification before changing that rule; unrelated authorized work can continue.
- Report compilation separately from actual passed/failed/skipped counts, name any unexecuted coverage, and retain native result artifacts under `TestResults/GameplayRegression/Runs/`. Update the test-running guide and migration/review documents when their instructions or claims change.

## Test Iteration Workflow

- Inspect reported failures yourself using native test XML, bounded slices of `Assets/TestResults.txt` when relevant, and the active Unity Editor log. Never live-tail logs or ask the user to diagnose the failure.
- Assume the Editor may retain the failed state. Resolve pending Test Framework/Pipeline execution before another run or reload. If a manual stop is necessary, query `editor_status`, stop only when playing/paused, and wait for stopped status; never kill Unity.
- Diagnose whether the failure belongs to gameplay, setup, asynchronous readiness or an obsolete expectation. Apply the justified fix, recompile through the open Editor, then run the failed cases and affected regressions with native filters. Leave passing tests enabled and independently selectable.
- For First Time Pass changes, preserve real selection/confirmation/movement/interception and continuation coverage. Add new native tests for the requested change and execute them; current board/input paths should be investigated directly before asking for missing design information.
- After focused cases pass, report the result and hand over the change. Do not expand to whole-suite testing or wait for a whole suite by default. Only run and wait for complete suites when explicitly requested by the user. Report actual counts and artifact paths; a CLI response of `running` is not a verdict.

## CodeGraph usage

This repo may have a `.codegraph/` index. When available, use CodeGraph first for architecture, symbol lookup, impact analysis, callers/callees, and code navigation before falling back to grep/read loops.

For Unity-specific behavior, remember that CodeGraph indexes source code but may not fully understand scene, prefab, inspector, or serialized asset wiring. Verify Unity object references manually when needed.

## Save Log Inspection

- Identify the active run through `ApplicationManager.Instance.GetLastSavedFilePath()` using Unity CLI before attributing live events or notes to a match. Loading a save creates a unique `__Run_...` file and run-specific companions; the selected seed is not the live write target. Inspect `playSessionId`, `playSessionStartedUtc`, `sourceSavePath`, and `inheritedEventSequence` to distinguish replay actions from inherited history. Never combine different play-throughs just because their team names or seed are the same.
- For Room direct-play testing only, the disposable working save is `/Users/t.antonakis/Library/Application Support/DefaultCompany/CA from Chatgpt/SavedGames/__RoomPlaytests/__RoomDirectPlay__gv10-dHYf-vRVz-oLwz_2024-11-26_00-28__Single Player__Inverness Caledonian Thistle__Aurora F.C..json`. Do not assume it is the current loaded match.

## Hex Terminology

- In this project, `inGoal` hexes and `CanShootTo` hexes refer to the same goal-line target set.
- `HexCell.isInGoal != 0` is the persistent flag on the hex.
- `canShootToHexes` is the runtime list built from the same coordinates in `HexGrid.CalculateShootingPaths(...)`.
- Use the terms interchangeably when discussing goal-line shooting targets.
