## Unity Workflow Rules

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
- The user runs Play Mode and the test suite manually.

## Test Iteration Workflow

- When the user is in testing mode and asks Codex to check logs or `Assets/TestResults.txt`, inspect the failure, identify where the fix belongs, apply the fix, and then use Unity CLI `editor_stop` only if Play Mode is active, followed by `unity recompile`. If the Pipeline connection is unavailable, ask the user to perform the required Editor action manually; never use GUI scripting.
- Before reloading after a test failure fix, deactivate/comment out every test scenario that already succeeded earlier in the current testing pass. Leave enabled only the failed scenario and the scenarios that follow it, so the user can press Play and restart testing from the failure point.
- The user, not Codex, presses Play to restart scenarios. Codex must not press Play or otherwise start tests.
- After incremental fixes have made the remaining tests pass, re-enable/release the full test suite only when the user asks, so the final snapshot can be checked for regressions.
- When the user reports a failed Unity scenario, inspect bounded log slices from `Assets/TestResults.txt` and Unity `Editor.log`; never live-tail logs.
- When the user reports a failure, assume Unity may be left open in the failed state for investigation.
- Use the bridge, `Assets/TestResults.txt`, and `Editor.log` directly instead of asking the user to diagnose failures.

## FTP Development Workflow

- When working on First Time Pass (`FTP`), always implement the requested gameplay change first and add or update the relevant runner scenarios.
- When useful, ask the user for exact click paths or token/hex sequences before finalizing a new FTP scenario.

## FTP Test Loop

1. Comment in all FTP-related tests relevant to the current FTP change.
2. Ensure the project compiles cleanly.
3. Tell the user to run the tests.
4. If the FTP batch passes, keep moving forward.
5. If a test fails:
   - inspect the failed Unity state yourself
   - inspect token positions / ball position
   - inspect `Assets/TestResults.txt`
   - inspect Unity `Editor.log`
   - determine whether the fix belongs in gameplay code or in the runner
6. After each failure, comment out all previously passed FTP tests and keep only the first unresolved FTP test plus the remaining later FTP tests if needed.
7. Repeat until all FTP-related tests have selectively passed.
8. When the user asks for full FTP regression, comment all FTP-related tests back in and run them together.
9. If the full FTP regression fails, go back to the selective loop above.
10. When FTP is stable and the user asks for broader regression, comment in all audited tests so far and run the wider suite before merging.


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
