# Intro clock visibility

- [x] Hide the existing scoreboard timer during the intro, including periodic scoreboard refreshes.
- [x] Restore it before rendering after normal completion, skip, or intro disable.
- [x] Preserve shootout clock hiding and clock text/counting behavior.
- [x] Live Editor compilation clean (zero errors).
- [x] `IntroClockVisibilityEditorTests`: 5 passed, 0 failed, 0 skipped.

Native results: `TestResults/GameplayRegression/Runs/20261108T145034294Z/results.xml`.

Pure presentation change; gameplay suites are not required. The fixture isolates and restores the intro singleton and creates disposable UI without opening scenes or touching designer-owned layouts.
