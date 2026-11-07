# Shootout roll panel

Run only `ShootoutRollPanelPlayModeTests` in Unity Test Runner, PlayMode.
Test entry points: `Assets/Tests/PlayMode/Features/ShootoutRollPanel/`.
Owned board setup and cleanup: `ShootoutRollPanelFixture.cs` in this folder.

- [x] Keeper roll is offered within one second of an on-target shooter roll, in both attacking directions.
- [x] Off-target result starts a nonblocking countdown, in both attacking directions.
- [x] Shot ball animation starts while the final result countdown is visible.
- [x] Normal shot reset preserves that countdown.
- [x] Next comparison panel reuses the dragged position after layout switching.
- [x] Disabling the shot manager cancels the panel.

Validation: clean Unity recompile (0 errors/warnings); 6 passed, 0 failed, 0 skipped.
Native result: `TestResults/GameplayRegression/Runs/20261107T112014489Z/results.xml`.

Implementation keeps the shooter comparison open directly into the keeper roll for shootouts. Settled outcomes (off-target or non-tied saves) start the panel countdown without delaying shot animation, shootout celebration or next-kick preparation. Tied saves still need their direction decision and retain their existing flow. The visible result survives normal shot cleanup; a new comparison can replace it. Interruptions still cancel it.

Shared panel code is affected. Optional broader verification for the user: the existing `LegacyDuelPresentationPlayModeTests`, `LegacyShootingBranchPlayModeTests` and free-kick shot fixtures in `Assets/Tests/PlayMode/`. Codex did not run those broader tests.
