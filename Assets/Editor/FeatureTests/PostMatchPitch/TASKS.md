# Post-match pitch report

- [x] Project existing full-board action-boundary snapshots and committed movement paths without changing gameplay recording.
- [x] Include active players with no ball touches; exclude setup, bench, sent-off, off-pitch and reset movement.
- [x] Normalize direction across regulation halves and extra-time choices.
- [x] Author a Pitch tab, schematic pitch, 36 editable player markers, hover/pin heatmaps and half/possession filters.
- [x] Share the report's named-team filter and show an empty state for missing history.
- [x] Compile and inspect the open Editor log.
- [x] Run only `PostMatchPitchEditorTests` (category `PostMatchPitch`), retain native XML and inspect a rendered preview.

Method: each recorded resolved-action snapshot contributes one equal position
sample per active player. This is action-weighted occupancy, not elapsed real
time or touch frequency. Traversal heatmaps use counted-for-distance committed
paths, omitting the origin and excluding resets. Heat intensity is relative to
the selected player's own map. Sparse saved history remains sparse; no positions
are inferred from a player's touches. Old saves without opening choices use the
historical default first-half direction. Extra time uses its own opening choice.

No gameplay recording, rules, movement or save schemas are changed in this stage.
Broader user-run checks: existing save/load presentation and completed-match
flows; no full gameplay suites are required for the read-only projection.

## Dropdown revision — 2026-11-17

- [x] Three independent, authored dropdowns in one row: period, possession, heatmap.
- [x] Only applicable half/extra-time choices, with correctly spelled labels.
- [x] Each actual popup shows every option without scrolling, supports selection
  and reopening without duplicate options.
- [x] Pitch uses actual markings' 27.5:21.65 ratio; directions remain normalized.
- Compilation: zero errors. EditMode fixture **18 passed, 0 failed/skipped**;
  first revised run `20261117T171401372Z/results.xml`, followed by a final rerun
  after correcting popup padding: `20261117T172105157Z/results.xml`.
- `PostMatchDropdownPlayModeTests`: **1 passed, 0 failed/skipped** in
  `TestResults/GameplayRegression/Runs/20261117T171924104Z/results.xml`.
  Earlier failed runs `20261117T171519334Z` and `20261117T171815143Z` exposed TMP
  popup shrinking and doubled viewport padding. Templates were corrected;
  assertions were retained. Unsupported arrow glyph replaced with an ASCII v.
- Native results are under `TestResults/GameplayRegression/Runs/`. The fixture
  owns and cleans its canvas/panel, never uses a live save, and runs through the
  native Framework bridge. Hardware pointer/raycast and real-match acceptance
  remain unexecuted. No whole suites were run.
- Final sample-data pitch, passing, recovery and stats PNG previews are retained
  beside `20261117T172105157Z/results.xml`. Room reopened clean; editor reports
  no script compilation errors.

## Earlier verification — 2026-11-17

- Compilation: zero errors; final editor-tool compilation zero warnings.
- Nine distinct EditMode cases passed, no skipped cases. Initial fixture run
  `TestResults/GameplayRegression/Runs/20261117T161711320Z/results.xml` passed all
  eight data-projection cases and exposed a missing CanvasRenderer on the custom
  pitch Graphic. The component is now explicitly required/authored. The affected
  UI case passed in `TestResults/GameplayRegression/Runs/20261117T162014697Z/results.xml`.
- Cases cover stationary/no-touch players, read-only serialization, staggered hex
  coordinates, both extra-time directions, halftime normalization, team/possession
  filters, uncontrolled balls, setup/bench/sent-off/off-pitch exclusions, path
  counting, duplicate event IDs, missing legacy history and actual authored
  marker hover/pin/filter callbacks. No runtime objects are added to the view.
- Rendered 1920x1080 sample preview, pinned player: same final run directory,
  `postgame-pitch.png`. Passing/recovery/stats previews are retained alongside it.
  Temporary canvas/camera/clones were cleaned up. The final compact pitch spacing
  was checked visually after the tests.
- Shares named team filters with the other report tabs; final Room reopened clean
  in Edit Mode. No gameplay recorder, rule or save schema changed.

Unexecuted: real played-match history acceptance, all match variants and whole
gameplay suites. Native UI checks invoke the real marker handlers/button callbacks
but do not claim a complete hardware-pointer/raycast test.
