# Room presentation implementation

Updated 2026-11-07. Room scene and fixture prefabs are authored through the live
Unity CLI/Pipeline. All visual objects are editable before Play Mode. Existing
unrelated changes are preserved. The designer-owned MatchStatsUI/Stats hierarchy
was verified unchanged, including referenced prefab records (68 serialized documents;
SHA-256 e7d36342fb85781e7a27c19d4e485fdaf0aa2eb2208bdcef4a6f08e6fdadbf53).

## Implemented behavior

- Movement slots have empty, active, completed and forfeited states. The next slot
  pulses a team-kit face without a number before selection, then the selected
  player's face/number until completion. Pulse duration defaults to 1.5 seconds;
  pause freezes it. Accepted AttMP/DefMP forfeits persist as larger team-colored X
  marks with separate secondary-color borders. Rejected forfeits, 2f2 skips and
  stopped dribbler pace do not fabricate forfeited slots. Save formats are unchanged.
- Snow makes markings and spots orange. Goal nets remain white. Clear/Rain restore
  original colors; action labels keep their styling.
- Ctrl+Shift+S/H hold shooting/heading eligibility overlays at both ends. Last-held
  wins, release restores the other, focus/modal entry clears held states, and chord
  input is consumed before gameplay. Occupied hexes have no icon. New transparent
  soccer-cleat and bust PNGs have contrasting weather-aware outlines and no raycast
  targets. Attribution/license live beside the images under Resources/UI/RoomPresentation.
- Space locks a hovered pitch/list player into an independent centered card at 60%
  screen height. Complete outfield/keeper preview views are copied and uniformly
  scaled, with authored colored value circles and a canvas above the name tag.
  Space closes it. Gameplay input is blocked while camera/pause and ongoing CPU
  actions remain available; existing modals retain priority.
- Authored tips shuffle once per Room load, cycle every 30 seconds without repeats,
  and suspend during pause/intro. Kickoff descriptions and retained legacy kickoff
  scenarios use Enter, while simulated Space still exercises inspection.
- Navigation, orbit, zoom, presets and intro use the shared camera envelope. Zoom
  translates without rotating. Per the user's revised requirement, views outside
  the stadium are permitted: an authored large ground backdrop prevents exposure
  of the old blackout-plane edges. Built-in presets 1/2 fit the pitch beside the
  panel at changing aspect ratios; session-saved Shift+1–4 compositions do not
  auto-adapt. Presets 3/4 provide elevated diagonal and low oblique views.
- Draft completion sets a transient one-use intro request; saved loads and direct
  Room entry bypass it. Readiness includes pitch, playing teams (not bench hexes),
  weather and crowd. The authored 15-second path ends at adaptive Preset 1.
  Intro shows a copy of the existing scoreboard without the timer, plus Skip Intro;
  separate team titles are hidden. Normal HUD and clock return on completion/skip.
  Intro owns gameplay/camera/hover inputs and progression. Skip input is consumed
  through release; missing prerequisites restore kickoff safely.

## Actual validation

Compilation is separate from test results: the open Editor reports clean scripts
through `unity recompile --focus` and Editor.log confirms assembly reload.

| Focused native run | Passed | Failed | Skipped | Artifact under TestResults/GameplayRegression/Runs/ |
| --- | ---: | ---: | ---: | --- |
| RoomPresentationEditorTests | 13 | 0 | 0 | 20261107T061052386Z/results.xml |
| RoomPresentationPlayModeTests | 18 | 0 | 0 | 20261107T061108967Z/results.xml |
| Final badge draw-order/size follow-up | 17 | 1 | 0 | 20261107T075636080Z/results.xml |
| Movement-slot fixture correction rerun | 1 | 0 | 0 | 20261107T081019716Z/results.xml |
| Preview double-click and shutdown follow-up | 21 | 1 | 0 | 20261107T082713307Z/results.xml |
| Shutdown fixture expectation correction | 1 | 0 | 0 | 20261107T083140188Z/results.xml |

The PlayMode run covers accepted/rejected forfeits in both directions, dribbler
pace, slots before/after selection/completion/pause/reset, both card types and hover
sources, CPU continuation, bench readiness, natural intro completion, button/Esc
skip, prerequisite fallback and saved-load bypass. Camera assertions include
unchanged zoom rotation, continuity at 4 seconds, pitch fitting at 4:3/16:9/21:9,
frustum coverage and occupied overlay suppression/highlight preservation in all
three weather palettes. EditMode covers overlay arbitration/focus/modal reset,
intro request/skip release, tips timing and snow restoration with white nets.

25 PNGs in the PlayMode run's `frames/` directory show all four presets at three
aspects, intro at 0/4/7/10/15 seconds, both overlays in Clear/Rain/Snow and both
inspection card types. Visual inspection caught an oversized badge sprite after
these checks; its authored diameter and repeat-authoring draw order were corrected.
The final follow-up frames under `20261107T075636080Z/frames/` show both card types
with visible values inside correctly sized circles. All four card cases pass
assertions that backgrounds draw behind their values and fit within their rows.
That run's sole failure was the movement fixture reading the new X border label
instead of the Number label. The fixture now selects Number explicitly; its
independent rerun passed, including larger-X/border draw-order assertions. Unity
froze before the first rerun started; after the user restarted it, Pipeline was
responsive, scripts clean, and the final single-case run completed normally.
Earlier failure/reproduction artifacts remain retained.

Room small-card left double-click now opens inspection for the displayed outfield
player or goalkeeper. Single/right clicks, draft dragging in Room, pause/modal
entry and replacing an already locked inspection are rejected. Draft quick-pick
remains confined to an active DraftManager. Live-log save-context lookup no longer
creates ApplicationManager, including when MatchManager.OnDestroy runs after the
application has been destroyed. New pointer-dispatch and teardown-entry tests cover
both reported errors. The final follow-up's one failure was a fixture expectation
that a dirty log without a save context could flush successfully; it now correctly
asserts failure, an empty path and the missing-context message. Its independent
rerun passed. All 22 focused PlayMode cases have passing final verdicts across the
last run and rerun; compilation remains clean. The pre-existing legacy shooting
assertion edits were preserved separately and were not broadly rerun in this task.

**Full EditMode and PlayMode regression suites were not run, at the user's explicit
request.** These focused results do not claim full regression validation. Physical
hardware input and a continuously sampled movie are not covered by still captures.
