# Penalty dots feature verification

Run only `PenaltyDotPresentationEditorTests` in Unity Test Runner (EditMode).

- [x] Pending, scored and missed dot states use the existing circle sprite.
- [x] Exactly five slots per team; history rolls independently after five attempts.
- [x] Scene-owned scoreboard children, with raycasts disabled.
- [x] Normal clock visibility restored outside shootouts.
- [x] Seven focused native cases passed, zero failed/skipped.
- [ ] Designer review of `Documentation/Design/PenaltyDots-preview.png` and scene appearance.

Compilation: zero errors/warnings before the test-folder move (no source changes).
Native focused results: `TestResults/GameplayRegression/Runs/20261107T105642903Z/results.xml`.

The broad PlayMode run was cancelled at the user's request. Do not resume it.
