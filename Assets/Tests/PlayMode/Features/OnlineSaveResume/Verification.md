# Online checkpoint capture and restore

Run PlayMode fixture `OnlineSaveResumePlayModeTests` under category
`OnlineSaveResume`.

- [x] Finish a real kickoff pass with Home and Away in separate cases.
- [x] Reject save while the kickoff pass is unsettled.
- [x] Save and update a slot through the host pause menu during a guest pause.
- [x] Inspect the selected save through the authored Online → Load Match browser.
- [x] Restore Room with the saved clock, board, possession, stats and history.
- [x] Hold gameplay until both authenticated seats confirm Ready.
- [x] Reject stale Ready, require a new guest confirmation after reconnect, and release once.
- [x] Perform the next movement action and render the published state in a read-only guest Room.
- [ ] Run the packaged macOS and Windows walkthrough with two original EOS profiles.

Unity 6000.5.3f1 compiled with zero errors. The focused PlayMode fixture passed
2 cases with no failures or skips. Native XML and progress are retained under
`TestResults/GameplayRegression/Runs/20261124T232656730Z/`.

The existing fixtures for broader coverage are:

| Feature folder | Fixture |
| --- | --- |
| `Assets/Editor/FeatureTests/OnlineRoomHandoff/` | `OnlineRoomHandoffEditorTests` |
| `Assets/Editor/FeatureTests/OnlineRoomLaunch/` | `OnlineRoomLaunchEditorTests` |
| `Assets/Tests/PlayMode/Features/OnlineFrontendEntry/` | `OnlineFrontendEntryPlayModeTests` |
| `Assets/Tests/PlayMode/Features/OnlineSharedPause/` | `OnlineSharedPausePlayModeTests` |
| `Assets/Tests/PlayMode/Features/OnlineRoomMenu/` | `OnlineRoomMenuPlayModeTests` |

The pause presentation follow-up verifies that the blocked reason is visible
and the Save Match As label changes color when saving becomes available.
Compilation completed with zero errors. Both focused cases passed with zero
failures or skips in `TestResults/GameplayRegression/Runs/20261124T233929225Z/`.

The shared browser and stats follow-up verifies that Online → Load Match opens
the authored offline table with online checkpoint columns. Both Room cases
carry a 1–0 score, a scorer, shots, a yellow card and an injury. They verify
the saved data, restored host and guest stats, and the visible scoreboard and
discipline rows before Ready. The reproduction at
`TestResults/GameplayRegression/Runs/20261124T234903868Z/` had the goal in
restored data while the host scoreboard still showed 0. The fixed run
passed both cases with zero failures or skips in
`TestResults/GameplayRegression/Runs/20261124T234957276Z/`.
The EditMode fixture passed 11 cases with zero failures or skips in
`TestResults/GameplayRegression/Runs/20261124T235041310Z/`.
Unity compilation completed with zero errors. For broader offline browser
coverage, run `RoomSaveBrowserEditorTests` in
`Assets/Editor/RoomSaveBrowserEditorTests.cs`.
