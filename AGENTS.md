## Unity Workflow Rules

- Never start Unity from Codex. Do not launch the Unity app, do not run Unity batchmode, and do not press Play to start a test.
- If Unity is stopped or not in Play Mode, do not send Cmd+P, because Cmd+P would start Play Mode.
- If Unity is in Play Mode, including when it is stopped at a failed assertion, stop the existing editor instance by activating Unity and sending Cmd+P. Do not kill the Unity process.
- After code changes, reload the already-open Unity editor by sending Cmd+R, then inspect `/Users/t.antonakis/Library/Logs/Unity/Editor.log` for compile errors.
- Do not use `dotnet build Assembly-CSharp.csproj` as the Unity validation path for this project. It consumes time/tokens and does not stop or reload the open Unity editor.
- Use static shell checks such as `rg`, `sed`, and `git diff --check` freely. For Unity compilation, rely on the open editor reload and Editor.log.

## Test Iteration Workflow

- When the user is in testing mode and asks Codex to check logs or `Assets/TestResults.txt`, inspect the failure, identify where the fix belongs, apply the fix, and then only stop the already-open Unity editor with Cmd+P and reload it with Cmd+R.
- Before reloading after a test failure fix, deactivate/comment out every test scenario that already succeeded earlier in the current testing pass. Leave enabled only the failed scenario and the scenarios that follow it, so the user can press Play and restart testing from the failure point.
- The user, not Codex, presses Play to restart scenarios. Codex must not press Play or otherwise start tests.
- After incremental fixes have made the remaining tests pass, re-enable/release the full test suite only when the user asks, so the final snapshot can be checked for regressions.
