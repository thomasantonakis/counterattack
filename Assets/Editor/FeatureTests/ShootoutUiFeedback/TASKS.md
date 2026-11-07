# Shootout UI feedback

Feature fixture: `ShootoutUiFeedbackPlayModeTests` in
`Assets/Tests/PlayMode/Features/ShootoutUiFeedback/`.

- [x] Decisive-kick stakes remain alongside the active roll prompt after the walk-up ends.
- [x] Resolved kicks do not leave stale stakes.
- [x] Both team preview cards refresh at the start of a new shooter/keeper walk.
- [x] Hovers can replace a card; the next walk resets both contestants.
- [x] Natural-one off-target shots retain no calculated shooting power and show Roll 1 / OFF TARGET without a total.
- [x] Six native PlayMode cases passed, zero failed/skipped: `TestResults/GameplayRegression/Runs/20261107T120318565Z/results.xml`.
- [x] Final keyboard-copy change: Press [R] to roll. Compiled cleanly; this final wording-only change was not followed by another test run.
- [x] Removed automatic substitution prefab/scene authoring from domain reload. Explicit menu commands remain.
- [x] Saved all assets/scenes through Unity CLI, requested a controlled script reload and verified Room remained dirty=False and scriptCompilationFailed=False.

Manual checks: decisive shootout kick shows its stakes plus shooter/keeper prompts;
new walks reset the two preview cards after hover overrides; a natural one displays
no shooting total. No broad gameplay suites run.

Shared shot instructions also changed. Optional broader user check: existing
`LegacyFreeKickShotPlayModeTests` in `Assets/Tests/PlayMode/` for free-kick natural-one
presentation. Codex did not execute that fixture for this UI change.

Evidence: latest Editor log preserved at `/private/tmp/shootout-ui-latest-editor.log`.
The live match reached 0-3 before the sixth kick, at which point a home miss would
end the shootout. Stakes previously depended solely on the walk-up instruction flag,
which is cleared before kick execution; the normal instruction aggregation now adds
the current kick's stakes alongside its active manager prompt.
