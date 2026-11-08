# Match event banners

- [x] Scene-owned square below scoreboard; Inspector-editable sprites and 3–5 second duration.
- [x] FIFO queue with fade/scale presentation, no raycasts or gameplay delays.
- [x] Card, injury, substitution, restart, foul, stoppage and half/tiebreaker notifications.
- [x] Clean compilation in the live Editor (zero compile errors).
- [x] `MatchEventBannerEditorTests`: 5 passed, 0 failed, 0 skipped.
- [x] `MatchEventBannerPlayModeTests`: 1 passed, 0 failed, 0 skipped. Real unscaled-time animation, queue and disable/re-enable lifecycle.
- [x] Inspect authored placement and all 14 sprite assignments; verify real alpha on every PNG.

Native results:

- EditMode: `TestResults/GameplayRegression/Runs/20261108T130408533Z/results.xml`
- PlayMode: `TestResults/GameplayRegression/Runs/20261108T130757409Z/results.xml`

The initial EditMode run had one fixture failure: normal component disable callbacks do not run in that fixture. Its deterministic queue test now calls explicit cleanup; real disable lifecycle is verified by the passing PlayMode case. The initial native report is retained at `TestResults/GameplayRegression/Runs/20261108T130224926Z/results.xml`.

Presentation changes only; gameplay outcomes and timing are unchanged. Broader gameplay suites are not required for artwork or layout. PNG choice remains provisional.
