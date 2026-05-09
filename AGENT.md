# Counterattack Repo Workflow

## Unity / Compiler Rule
- Never leave the repo in a state with compilation errors.
- After code changes, refresh or recompile Unity scripts before concluding the turn.

## Unity Play Mode Rule
- Codex must never start Unity Play Mode.
- Do not press `Cmd+P` as a blind toggle. Only use it as a stop action when the user has explicitly said Unity is currently in Play Mode, or when the current logs/state clearly show an active or just-failed test run.
- If Play Mode state is uncertain, do not press `Cmd+P`; ask the user to stop Play Mode or confirm it is running.
- After making fixes, Codex may refresh/reload assets and check compilation, then must tell the user to press Play manually.
- The user runs Play Mode and the test suite manually.

## Unity Failure Fix Loop
1. When the user reports a failed Unity scenario, first inspect bounded log slices from `Assets/TestResults.txt` and Unity `Editor.log`; never live-tail logs.
2. After identifying the failure context from the logs, press `Cmd+P` only to stop Unity Play Mode before editing files.
3. Edit and save the relevant files.
4. Return focus to Unity and press `Cmd+R` to reload assets.
5. Check Unity/script compilation after reload and fix any compilation errors until Unity is clean.
6. Do not press Play after the fix; tell the user to press Play and rerun the suite.

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

## Responsibility Split
- The user may run Unity/tests manually.
- When the user reports a failure, assume Unity may be left open in the failed state for investigation.
- Use the bridge, `Assets/TestResults.txt`, and `Editor.log` directly instead of asking the user to diagnose failures.
