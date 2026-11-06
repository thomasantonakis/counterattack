# Running gameplay regressions

Open **Window → General → Test Runner** in Unity. This is the same Unity Test
Framework suite used by the CLI/Pipeline automation; there is no separate automated
scenario runner to launch with ordinary Play.

- **EditMode** tests cover isolated rules, calculations, decisions and data/UI formatting.
- **PlayMode** tests cover gameplay input, movement, coroutines, interruption,
  presentations and continuations. Unity enters and exits Play Mode for these tests.
- Select a fixture or case and choose **Run Selected** while iterating. After a change,
  choose **Run All** on both tabs to check the full regression.

Wait for an active run to finish before editing scripts: a domain reload can interrupt
PlayMode execution. Native cases build and clean up their own deterministic boards;
they do not use a mutable live save as their fixture.

Current discovered suite: **748 EditMode + 362 PlayMode = 1,110 tests**. See
[GameplayRegressionMigration.md](GameplayRegressionMigration.md) for actual completed
run counts; discovery and compilation are not test results.

The CLI/Pipeline drives Unity's same TestRunner API. The Test Runner window shows
individual tests and failures; automation additionally writes durable NUnit XML,
per-case progress and the current case under `TestResults/GameplayRegression/Runs/`.
An async CLI response saying `running` does not mean the suite passed.

The retained historical `GameTestScenarioRunner` sequence was a different system:
ordinary Play ran a serial scenario script and logged its assertions. Its replacements
are independently selectable framework tests with cleanup on failure. The migration
inventory records which originals have actually passed before retirement.

Review [MorningReview.md](MorningReview.md) for expectation drift decisions and any
unresolved rules questions. These tests currently target Unity Editor EditMode/PlayMode,
not standalone player test builds.
