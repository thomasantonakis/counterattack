# Post-match report — tabs and leaderboards

- [x] Read-only projections of existing recorded player statistics; no gameplay/save schema changes.
- [x] Keep the existing score, scorers, stats objects and main-menu action.
- [x] Add edit-mode authored Match Stats / Top Performers tabs, six metrics, named general team filters and fixed bottom paging.
- [x] Combine pass counts/accuracy, add xRecoveries, and explain recorded ground/aerial/hanging completions.
- [x] Provide empty states and retain legacy records with unknown team as Unassigned.
- [x] Compile through the open Unity Editor and inspect Editor.log (project log redirects to `Logs/Editor.log`).
- [x] Run only `PostMatchReportEditorTests` (category `PostMatchReport`). Retain native XML.
- [x] Inspect both authored tabs in an isolated edit-mode camera render and check saved Room wiring.

Scope: presentation and read-only reporting. The full-time view uses the existing
pause state and audio policy. Pitch/heatmaps are covered by the separate
PostMatchPitch feature; luck and calibrated ratings remain future stages. A rating cannot
be inferred from movement volume. Passing/duels include attempted/contested rows
with zero successes; other metrics require positive values. Legacy team names are not guessed.

Broader user-run checks: `EndGamePanelOwnershipEditorTests` in
`Assets/Editor/FeatureTests/EndGamePanelOwnership/` for scene ownership/dimensions,
and `ShootoutUiFeedbackPlayModeTests` in
`Assets/Tests/PlayMode/Features/ShootoutUiFeedback/` for the existing shootout-result
presentation path. Also review normal full-time, extra-time and completed-shootout
results through their existing match flows. No whole gameplay suite is required
for this presentation-only stage. These broader checks were not executed here.

## Filter and sorting revision — 2026-11-17

- [x] Show named-team filters only on Top Performers and Pitch; Stats shows both teams.
- [x] Include zero-success pass/duel participants; sort successes descending,
  attempts/contests ascending, then name ascending. Show duel success percentage.
- [x] Remove internal hanging-pass wording from the report copy.
- Compilation: zero errors. Report fixture **25 passed, 0 failed/skipped**,
  `TestResults/GameplayRegression/Runs/20261117T171343671Z/results.xml`.
- Existing ownership regression: **3 passed**, `20261117T171426966Z/results.xml`.
- Full-time audio: **2 passed**, `20261117T171943988Z/results.xml`.
- Existing ordinary pause music: **1 passed**, `20261117T172004118Z/results.xml`.
- Existing crowd playback/fade/pause/resume/mute/chant retry/crossfade: **7 passed**,
  `20261117T172032585Z/results.xml`. All paths above are under
  `TestResults/GameplayRegression/Runs/`; no failures or skips in these final runs.
- Pitch/dropdown validation is recorded in the sibling PostMatchPitch/TASKS.md.
  No full gameplay suites or complete live-match/shootout flows were executed.
- Unrelated scene serialization changes were restored only while Room was
  unloaded; incidental font atlas changes removed. F3/repositioning notes remain
  in GameplayReview.md; gameplay input/rules were not changed.

## Earlier revision — 2026-11-17

- Six metrics: Distance, Recoveries, Passes, Duels, Goals, Assists. Percentages are
  in Passes (no minimum), xRecoveries in Recoveries. Positive-outcome filtering
  remains: Duels requires one win and Recoveries requires one recovery.
- General named-team filters are below the main tabs, shared across Match Stats,
  Players and Pitch. Value headings identify the selected metric. Paging is fixed
  at the bottom. The existing stats text/row objects are preserved.
- Passing audit: `MovementPhaseManager` credits `PassCompleted` or
  `AerialPassCompleted` when the attacking collector gathers a hanging pass;
  `LongBallManager`, `HeaderManager` and ordinary pass managers also use these
  counters. Targeted-but-not-completed aerial passes do not count as completions.
- Report EditMode fixture: **23 passed, 0 failed/skipped**, native XML
  `TestResults/GameplayRegression/Runs/20261117T161157313Z/results.xml`.
  Initial revision `20261117T160607657Z` had one missing-CanvasGroup failure;
  authoring now checks Unity's object validity before adding the column groups.
- Audio PlayMode fixture `PostMatchAudioPlayModeTests`: **2 passed, 0 failed/skipped**,
  native XML `TestResults/GameplayRegression/Runs/20261117T161800341Z/results.xml`.
  Uses an owned silent audio rig, inactive match state and the actual ShowPanel
  entry point. Verifies immediate stop of stadium/match sounds, rejection of late
  dice/whistles/chants, persistence across Update/reopening, and music on/off.
  Restores singleton/preferences and never starts or saves a live Room match.
- Compilation completed with zero errors; the final editor-tool compilation also
  had zero warnings. Earlier runtime compilation reported existing obsolete API
  warnings. Updated passing, recovery and pitch previews are retained in
  `TestResults/GameplayRegression/Runs/20261117T162014697Z/`.
- Final authoring-only spacing refinements were visually checked after tests.
- No original scene objects removed. Final Room changes to original records are
  confined to seven objects inside the post-game panel; unrelated serialized
  records were restored only while Room was unloaded. Font atlas changes removed.
- Remaining broader user-run coverage: `EndGamePanelOwnershipEditorTests` at the
  folder above; `PauseMusicPlayModeTests` in
  `Assets/Tests/PlayMode/Features/MusicLibrary/`; and
  `GameplayAudioPlaybackPlayModeTests` in
  `Assets/Tests/PlayMode/Features/GameplayAudio/`. These check existing ownership
  and ordinary pause/resume audio. No whole suites were run.

Unexecuted for this revision: full live-match result flows, full mouse/raycast
automation and whole gameplay suites. Audio is tested with silent clips; listening
to the actual authored music remains a manual acceptance check.

## Foundation verification — 2026-11-17 (historical)

- Final compilation: completed, zero errors. Existing obsolete-API warnings were
  reported on the earlier runtime compilation; final editor-tool compilation had
  zero warnings. No gameplay rules or statistics recording code changed.
- Final dedicated fixture: **23 passed, 0 failed, 0 skipped**.
- Native XML: `TestResults/GameplayRegression/Runs/20261117T154056786Z/results.xml`.
  Per-case results and progress are retained in that same directory.
- Initial run `20261117T152512862Z`: 19 passed, 3 failed due to EditMode not invoking
  runtime OnEnable listener setup. Explicit report initialization now binds its
  owned listeners, supporting both runtime and edit-mode inspection. Repeated
  report initialization, team/metric buttons and paging passed afterward.
- Intermediate `20261117T152803773Z`: 22 passed; rendered inspection then identified
  the existing parent layout does not control child heights. A fitter and top
  pivot were added only to the new page, with a test for height and non-overlap.
- `20261117T153023376Z`: 23 passed. Final row spacing/contrast and saved-scene cleanup
  were followed by the final 23-case run above.
- Both 1920x1080 camera-rendered previews (sample data, not a played match):
  `TestResults/GameplayRegression/Runs/20261117T154056786Z/postgame-players.png`
  and `postgame-stats.png`. Preview camera/canvas/clones were removed afterward.
- Original Room object IDs were compared with the starting revision: zero removed;
  only three original records changed (EndGamePanel component list, recap Content
  children, and recap Viewport component list). Existing stats/score objects and
  unrelated layouts are byte-for-byte unchanged. Unity's global canvas refresh
  initially serialized unrelated layout values; these were restored, with final
  exact cleanup performed only after switching normally away from the saved Room.
- Incidental dynamic-font atlas changes were removed. Room was reopened normally
  and confirmed clean before the final tests. No additive scene loading was used.
- Code/document whitespace checks pass. Unity-generated YAML whitespace is retained
  in the authored scene/prefab rather than normalizing loaded-scene serialization.

Unexecuted: automated PlayMode lifecycle/pointer/raycast tests, real match result
flows, full gameplay suites. The EditMode interaction checks invoke actual authored
button callbacks and do not claim a full mouse-input or played-match verification.
