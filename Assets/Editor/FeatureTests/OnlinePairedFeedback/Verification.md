# Paired online feedback verification

- [x] Verify Draft's squad and starting averages on the authored guest screen.
- [x] Verify guest goal-line target elevation and all three movement status rows through a decoded host publication.
- [x] Verify Novice and Experienced kickoff targeting and hover assistance.
- [x] Verify the final-whistle penalty toss can pause, resume and accept the active team's roll with stale UI focus. The focused rerun also checks Escape resume and Away's automatic roll.
- [x] Verify the guest accepts successive countdown updates after a tied one-half match. The original failure was reproduced through the real wire codec at `coin.home` when 4 seconds reused the 5-second publication sequence. Reproduction in `TestResults/GameplayRegression/Runs/20261124T175257900Z/`.
- [ ] Repeat the paired Windows and macOS walkthrough. At the penalty toss, record which application shows the pause, what the pause message says, and whether the countdown advances after the Home roll.

Unity compiled with zero errors. The five PlayMode cases passed with zero failures or skips in `TestResults/GameplayRegression/Runs/20261124T165529557Z/`. The extended penalty-toss case passed in `TestResults/GameplayRegression/Runs/20261124T165720436Z/`.

After the countdown fix, Unity compiled with zero errors and all six PlayMode cases passed with zero failures or skips in `TestResults/GameplayRegression/Runs/20261124T175437871Z/`.

For broader verification use `OnlineNativeRoomPlayModeTests` under `Assets/Tests/PlayMode/Features/OnlineNativeRoom/` and the published-highlight fixtures under `Assets/Tests/PlayMode/Features/OnlinePublishedHighlights/` to check interactions with other Room overlays.
