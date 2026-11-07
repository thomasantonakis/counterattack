# Penalty shootout scoreboard dots

Room owns ten editable UI indicators under `Canvas/ScoreBoardManager/PenaltyDots`, split into HomePenaltyDots and AwayPenaltyDots. These are scene objects. Each Kick object supplies the pale rim; its Result child supplies the coloured circle. Both use the existing `Assets/Resources/Textures/Common/Circle.png` sprite and ignore raycasts.

ScoreboardManager exposes the pending, scored and missed colours. During shootout order selection, play and completion, it displays the rows and hides TimerPanel. Outside those states it hides the rows and restores TimerPanel. The first five rounds retain their original slots. At the start of each round from the sixth onward, both rows shift left together, retaining four previous results and a grey slot for each team's upcoming attempt. Each team fills that slot after its kick. A completed round remains visible until the next taker starts walking. Score totals remain independent of the visible dot history.

PenaltyShootoutManager retains the shooter walk-up, decisive-kick stakes, goal celebration and winner instructions. The text grid is removed from the walk-up message.

`PenaltyDots-preview.png` illustrates sample results with the scene-authored dot geometry and existing scoreboard/logo. Its sample labels and panel colours are preview settings, not changes to team identity handling.

Seven focused native EditMode cases passed (0 failures/skips), covering 0, 1, 4, 5, 6 and 12 attempts and the scene wiring/normal clock visibility. Results: `TestResults/GameplayRegression/Runs/20261107T105642903Z/results.xml`.

Compilation completed with zero errors/warnings. Complete EditMode execution: 754 passed, 25 failed, 0 skipped; artifact `TestResults/GameplayRegression/Runs/20261107T105702036Z/results.xml`. Twenty-four failures originate in camera preset fixtures missing a Camera component; one is the existing Room Rules Viewer layout expectation (34 vs >188). No camera or Rules Viewer code was changed for this feature.

The complete PlayMode run was cancelled at the user's request; it has no completed suite verdict. Progress is retained under `TestResults/GameplayRegression/Runs/20261107T105911408Z/`. Future verification is restricted to the feature folder `Assets/Editor/FeatureTests/PenaltyDots/`, using the `PenaltyDotPresentationEditorTests` fixture filter.

Upcoming-round window follow-up: five dedicated native EditMode UI cases passed
(0 failed/skipped), retained at
`TestResults/GameplayRegression/Runs/20261107T122130975Z/results.xml`.
Checklist: `Assets/Editor/FeatureTests/PenaltyDotUpcomingRound/TASKS.md`.
No gameplay tests run for this visual change.
