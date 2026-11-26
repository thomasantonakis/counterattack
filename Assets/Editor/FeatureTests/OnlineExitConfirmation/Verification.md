# Online exit confirmation

- Verify the multiplayer Exit button uses the Main Menu's authored Quit confirmation prefab.
- Verify the Room pause button uses the offline Save & Leave / Leave Without Saving overlay when a checkpoint can be saved.
- Verify cancellation preserves pause, leaving without a save writes nothing, and Save & Leave waits for an existing or new checkpoint.
- Verify the online Room exits directly when saving is unavailable, matching the offline Room.

Run the `OnlineExitConfirmationPlayModeTests` fixture in PlayMode.

All five cases passed with zero failures and skips in
`TestResults/GameplayRegression/Runs/20261126T103543409Z/results.xml`.
