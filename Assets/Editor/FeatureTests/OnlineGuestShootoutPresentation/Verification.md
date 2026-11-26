# Guest shootout presentation verification

- [x] Show the published shooter and saving goalkeeper in the guest's team previews. Keep both visible through unrelated token hover and switch the pair for the next team.
- [x] Drag the authored guest dice panel with the left mouse button. Keep the right mouse button available to the camera.
- [x] Keep the guest's drag position through pause, the goalkeeper roll and the next kick, using local presentation state.
- [ ] Repeat the preview and drag checks on the paired Windows and macOS players.

The baseline portrait mismatch appeared in `TestResults/GameplayRegression/Runs/20261124T193907712Z/`. After the test waited a rendered frame for the UI raycast, the unchanged panel rejected the left drag in `TestResults/GameplayRegression/Runs/20261124T194041507Z/`.

Unity 6000.5.3f1 compiled with zero errors. The focused PlayMode category `OnlineGuestShootoutPresentation` passed two cases with zero failures or skips. Native XML and per-case progress are in `TestResults/GameplayRegression/Runs/20261124T194151571Z/`.

Broader checks are `ShootoutRollsAndPause` in [OnlineNativeRoomPlayModeTests](../../../Tests/PlayMode/Features/OnlineNativeRoom/) for shared result handling, and `CameraAndPanelGesturesKeepRollOwnership` in [DiceCameraInputPlayModeTests](../../../Tests/PlayMode/Features/DiceCameraInput/) for authority input ownership.
