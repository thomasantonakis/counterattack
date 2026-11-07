# Camera input during dice panels

- [x] Camera pan uses its own pointer gesture, independent of gameplay click blocking.
- [x] A left gesture starting on a dice panel stays with UI; pitch gestures pan the camera.
- [x] Dice panel drag handles accept only left-button drags; right-button drags rotate the camera.
- [x] Compile in the live Editor: no errors or warnings.
- [x] Run only `DiceCameraInputPlayModeTests` (both attacking directions): 2 passed, 0 failed, 0 skipped.
- [ ] Manual check: while awaiting R in header, duel and stoppage panels, right drag rotates, pitch left drag pans, panel left drag moves the panel; zoom and camera presets work; R rolls normally.

Tests use an isolated board, authored resolution-panel/camera fixtures and real duel entry/key dispatch. They cover threshold, UI ownership, release, pause interruption, zoom, presets and R ownership.

Native XML and per-case progress: `TestResults/GameplayRegression/Runs/20261107T134929742Z/`.
Earlier failed fixture runs are retained alongside the final run. The production
fix was unchanged by the roll-assertion corrections.

Optional broader camera verification is user-run:
`Assets/Tests/PlayMode/RoomPresentationPlayModeTests.cs`, fixture
`RoomPresentationPlayModeTests.CameraIntroWeatherOverlaysAndCardProduceSafeAuthoredFrames`,
because the gesture handler is shared with normal Room camera controls.
Header and stoppage panel mouse gestures still need the manual check above;
the executed native cases exercise a real duel panel only. No complete suites were run.
