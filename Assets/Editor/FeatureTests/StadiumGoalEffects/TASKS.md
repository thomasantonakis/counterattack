# Stadium goal effects

- [x] Use authored ribbon and floodlights; no runtime geometry or scene edits.
- [x] Home club goals / either international team, only Sunset and LateNight.
- [x] Instruction palette from the credited scoring team's kit (own goals use credited team).
- [x] Alternate five-LED color blocks every .4 seconds until return begins.
- [x] Five secondary-color triples move clockwise once during scoring-team return.
- [x] Four .15-second floodlight dips to 20%, separated by .15 seconds at original power.
- [x] Restore original per-tile property blocks, visibility and individual light intensities on completion, disable or lighting change.
- [x] Open Editor compilation clean after removing pre-existing stray `ye` in DecisionTimingConfig.cs:107.
- [x] Focused EditMode fixture `StadiumGoalEffectsEditorTests` / category `StadiumGoalEffects`: 14 passed, 0 failed, 0 skipped.
- [ ] Visual review during a real goal at Sunset and LateNight: check camera-dependent color readability and timing comfort.

Native results: `TestResults/GameplayRegression/Runs/20261117T112800340Z/results.xml`; case progress in the same directory. Tests use owned preview-scene objects and cover eligibility, actual renderer colors, frozen-clock behavior, chase seam/lap limit, four dips and cleanup. No gameplay suites or ordinary gameplay were started. Presentation-only hooks do not change movement, goal attribution or kickoff rules; broader gameplay verification is not warranted.

Inspector controls live on the existing RoomLightingController under Goal effects. The chase duration follows the longest scoring-team return movement at its existing speed of 2 units/second. The authored 256-tile loop uses five-tile alternating blocks, with the final partial block at the seam.
