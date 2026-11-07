# Extra-time stoppage verification

- [x] Actual extra-action determination uses cap 7 in regulation and 3 in extra time.
- [x] Existing panel reads the cap from MatchManager for its current-period wording.
- [x] Focused cases cover halves 1–4, low totals and saturated totals.
- [x] Existing final-action penalty exception remains unchanged.

Run ExtraTimeStoppageEditorTests in this folder. Initial checkpoint: 8 passed /
0 failed / 0 skipped. Final expanded low-total results are recorded below.
Broader user-run coverage: LegacyRestartClockPlayModeTests and
RoomPresentationPlayModeTests in Assets/Tests/PlayMode/; RoomPresentationEditorTests
in Assets/Editor/ for timing/restart and the existing stoppage presentation.
No broad fixtures or complete suites were run for this change.

Final EditMode: **16 passed / 0 failed / 0 skipped**,
`TestResults/GameplayRegression/Runs/20261107T210821974Z/results.xml`.
The low-total cases use leniency 1 + roll 1 = 2 in both extra-time halves.
