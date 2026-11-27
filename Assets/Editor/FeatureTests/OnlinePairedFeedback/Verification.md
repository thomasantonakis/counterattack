# Paired online feedback verification

- [x] Verify Draft's squad and starting averages on the authored guest screen.
- [x] Verify guest goal-line target elevation and all three movement status rows through a decoded host publication.
- [x] Verify Novice and Experienced kickoff targeting and hover assistance.
- [x] Verify Intermediate kickoff hover shows an orange target and clears it on pointer exit. Check legal target and destination hover for passes, movement, goalkeeper moves, and set pieces under a connection hold.
- [x] Verify that the High Pass target marker clears on landing in the offline box sequence and during the online goalkeeper challenge.
- [ ] Verify the updated presentation in a paired Windows and macOS match.
- [x] Verify the final-whistle penalty toss can pause, resume and accept the active team's roll with stale UI focus. The focused rerun also checks Escape resume and Away's automatic roll.
- [x] Verify the guest accepts successive countdown updates after a tied one-half match. The original failure was reproduced through the real wire codec at `coin.home` when 4 seconds reused the 5-second publication sequence. Reproduction in `TestResults/GameplayRegression/Runs/20261124T175257900Z/`.
- [ ] Repeat the paired Windows and macOS walkthrough. At the penalty toss, record which application shows the pause, what the pause message says, and whether the countdown advances after the Home roll.

Unity compiled with zero errors. The five PlayMode cases passed with zero failures or skips in `TestResults/GameplayRegression/Runs/20261124T165529557Z/`. The extended penalty-toss case passed in `TestResults/GameplayRegression/Runs/20261124T165720436Z/`.

After the countdown fix, Unity compiled with zero errors and all six PlayMode cases passed with zero failures or skips in `TestResults/GameplayRegression/Runs/20261124T175437871Z/`.

For the pitch highlight update, Unity compiled with zero errors. The focused
assistance cases passed 3/3 in `20261127T164719718Z`, the FTP fixture passed 14/14
in `20261127T164206852Z`, and the offline High Pass box sequence passed 1/1 in
`20261127T164136827Z`. The online High Pass keeper cases passed 6/6 in
`20261127T163737432Z`. The published highlight fixture passed 4/4 in
`20261127T164834772Z`. The native XML files are under
`TestResults/GameplayRegression/Runs/<run>/results.xml`.

For broader verification use `OnlineNativeRoomPlayModeTests` under `Assets/Tests/PlayMode/Features/OnlineNativeRoom/` and the published-highlight fixtures under `Assets/Tests/PlayMode/Features/OnlinePublishedHighlights/` to check interactions with other Room overlays.
