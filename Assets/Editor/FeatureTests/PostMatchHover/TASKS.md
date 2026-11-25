# Full Time hover names

- [x] Suppress existing and new pitch/lineup player names while the authored Full Time panel is active.
- [x] Keep ordinary match inspection available when the panel closes.
- [x] Compile and run PostMatchHoverPlayModeTests; retain native results.

The owned fixture restores scene/singleton/event state and never loads live saves.
Related existing regression: PostMatchAudioPlayModeTests (actual ShowPanel entry point).

Verification: compilation clean, zero errors/warnings. Pitch and lineup cases:
2 passed, 0 failed/skipped, native XML
`TestResults/GameplayRegression/Runs/20261117T182725017Z/results.xml`.
Existing Full Time audio regression: 2 passed, 0 failed/skipped (see the following
retained run). Tests cover already-visible names, subsequent hover updates,
inspection token clearing and reopening normal inspection after closing the panel.
Full hardware-pointer/raycast acceptance remains manual.
