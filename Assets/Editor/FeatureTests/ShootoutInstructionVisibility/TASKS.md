# Shootout instruction visibility

Native fixture: `ShootoutInstructionVisibilityPlayModeTests` in
`Assets/Tests/PlayMode/Features/ShootoutInstructionVisibility/`.

Failure evidence: live CLI inspection returned snapshot `GOAL FOR Inverness CT!!!`
while the actual instructionText.text was empty. Latest log copied to
`/private/tmp/shootout-empty-instruction-editor.log`. GameDebugMonitor's final
text assignment unconditionally hid instructions whenever a duel roll overlay
was visible; the earlier stakes tests checked snapshots without testing this setter.

- [x] Real instruction text retains decisive-kick stakes while the keeper roll panel is open.
- [x] Real instruction text retains the goal celebration during the result countdown.
- [x] Ordinary shot prompt overlays continue hiding the instruction text.
- [x] Ordinary shot result overlays continue hiding the instruction text.

Result: 4 passed, 0 failed, 0 skipped.
Native XML: `TestResults/GameplayRegression/Runs/20261107T121440378Z/results.xml`.
Compilation: clean. Scenes saved and checked clean before the feature test run.
No broad gameplay suites run.

Manual check: reach a shootout with the roll panel open; verify the instruction box
continues to show its current stakes/prompt, walk-up or celebration while the old
result counts down. For optional additional shared UI checks, run
`Assets/Tests/PlayMode/Features/ShootoutUiFeedback/`.
