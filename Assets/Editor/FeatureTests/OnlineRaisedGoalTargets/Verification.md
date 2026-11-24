# Online raised goal target verification

- [x] Verify raised headers, shot previews, full-power shots, Snapshots, free-kick previews, free-kick shots, and in-match penalties in the authored Room.
- [x] Verify goal-mouth repositioning and walking the ball into goal. Apply each host publication through the wire and confirm the guest keeps the base highlight and height during hover. Confirm both boards restore the original heights after withdrawal.
- [x] Verify the committed shootout target through the wire. The guest shows the selected marker and clears it after the ball moves.
- [ ] Repeat the targeting checks with paired Windows and macOS standalone players.

Unity 6000.5.3f1 compiled with zero errors. PlayMode category `OnlineRaisedGoalTargets` passed ten cases with zero failures or skips. The native XML and per-case progress are under `TestResults/GameplayRegression/Runs/20261124T191726746Z/`.

The baseline run in `TestResults/GameplayRegression/Runs/20261124T191503559Z/` reproduced seven hover failures. Header aiming passed with the existing fix. The baseline reposition case stopped on a fixture setup error. The corrected fixture reached the reposition path in the final run.

The online publication uses the existing highlight reasons, height field and committed marker. The rules and protocol versions stay the same.

For broader checks on connection holds and normal shot marker expiry, use `RaisedGoalPreviewClearsOnHolds` and `DisabledAuthorityBallClearsGoalMarkerOnMotion` in [OnlineActionPreviewsPlayModeTests](../../../Tests/PlayMode/Features/OnlineActionPreviews/).
