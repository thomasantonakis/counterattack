## Unity Workflow Rules

- Never start Unity from Codex. Do not launch Unity, run Unity batchmode, press Play, or press Cmd+P unless it is only to stop an already-running Play Mode session.
- Before pressing Cmd+P, check that Unity is currently in Play Mode. If Unity is not in Play Mode, do not press Cmd+P because it would start Play Mode.
- If Unity is in Play Mode, including when stopped at a failed assertion, activate Unity, press Cmd+P once to stop, and wait until Play Mode has stopped. Do not kill the Unity process.
- After code changes, save assets, press Cmd+R in the already-open Unity editor to reload, then inspect `/Users/t.antonakis/Library/Logs/Unity/Editor.log` for compile errors.
- Keep fixing compile errors until the open Unity editor reports clean scripts. Only then tell the user what was fixed.
- Do not use `dotnet build Assembly-CSharp.csproj` as the Unity validation path for this project. It consumes time/tokens and does not stop or reload the open Unity editor.
- Use static shell checks such as `rg`, `sed`, and `git diff --check` freely. For Unity compilation, rely on the open editor reload and Editor.log.
- The user runs Play Mode and the test suite manually.

## Test Iteration Workflow

- When the user is in testing mode and asks Codex to check logs or `Assets/TestResults.txt`, inspect the failure, identify where the fix belongs, apply the fix, and then only stop the already-open Unity editor with Cmd+P and reload it with Cmd+R.
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
