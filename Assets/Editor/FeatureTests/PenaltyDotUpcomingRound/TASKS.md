# Upcoming penalty dot slot

Run only `PenaltyDotUpcomingRoundEditorTests` (EditMode, this folder).

- [x] Before either team takes its sixth kick, both rows retain attempts 2–5 and show a grey fifth slot.
- [x] The first team's sixth result fills its slot while the other row remains pending.
- [x] The completed round stays visible during celebration; the seventh walk advances the window again.
- [x] First-five-round slot order is unchanged.

Native result: 5 passed, 0 failed, 0 skipped.
`TestResults/GameplayRegression/Runs/20261107T122130975Z/results.xml`.
Compilation: zero errors/warnings. Saved scenes verified clean before tests.
Pure scoreboard presentation change: no gameplay tests or broad suites run.

Manual check: reach round six or later and confirm both rows shift to four historical
results plus one grey dot before the new kick; the two teams fill their grey slots
in turn. Uses the scoreboard's existing refresh cadence.
