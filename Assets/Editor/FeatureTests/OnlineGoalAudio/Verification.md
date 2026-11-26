# Online guest goal audio

- [x] Use the host scoring entry point and the guest's normal snapshot presentation, in the authored online Room.
- [x] Cover each scoring side, an existing score on the first snapshot, duplicate delivery, and local stadium mute and unmute.
- [ ] Confirm both sides in a packaged two-device listening check.

Run the `OnlineGoalAudioPlayModeTests` fixture in PlayMode.

Both cases reproduced the silent guest before the fix. Both passed afterward,
with 0 failures and 0 skips in `TestResults/GameplayRegression/Runs/20261126T102931651Z/results.xml`.
