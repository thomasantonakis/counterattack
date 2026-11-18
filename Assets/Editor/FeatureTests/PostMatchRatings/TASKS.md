# Player ratings prototype

- [x] Use one visible contribution formula for every player, as selected by the user.
- [x] Show base, grouped contributions, raw total and 1–10 bounded result.
- [x] Avoid duplicate tackle/interception/duel credit for possession recoveries.
- [x] Label the page as a prototype; show NR without weighted contributions.
- [x] Keep distance, injuries, expected statistics and luck out of this prototype formula.
- [x] Author a selectable player table, own paging and visible formula before Play Mode.
- [x] Compile, author/save prefab and Room, run focused ratings tests and affected UI regressions.
- [x] Inspect rendered first/second pages and selection breakdown; retain artifacts.

One formula: base 6.0; goals +0.90; assists +0.60; completed ground/aerial passes
+0.02; uncompleted passes −0.04; dribbles +0.10; possession losses −0.12;
recoveries +0.12; shot blocks +0.12; saves +0.20; yellow cards −0.30;
red cards −0.80. Cards are cumulative. Bound the final value to 1.0–10.0;
retain the raw result and each contribution for inspection.

The model reads existing PlayerStats only and never writes ratings back into
gameplay or saves. Stats identities keep same-name players separate. Rows sort
by rating descending, then name/team/jersey/key; unrated rows follow rated rows.
No position, minutes, exposure or competition adjustment is claimed. Weights
remain for discussion/calibration; this is not a validated football rating.

## Verification — 2026-11-18

- Compilation: zero errors. Final editor-tool compilation had zero warnings;
  runtime compilation reported existing obsolete Unity API warnings.
- Dedicated fixture: 9 passed, zero failed/skipped, native XML
  `TestResults/GameplayRegression/Runs/20261118T055000462Z/results.xml`.
- Final affected EditMode filter `PostMatch`: 74 passed (9 ratings, 25 report,
  18 pitch, 22 luck), zero failed/skipped, `20261118T063118596Z/results.xml`.
- Final post-match PlayMode filter: 17 passed (12 luck, 2 hover, 2 audio,
  1 dropdown), zero failed/skipped, `20261118T063325939Z/results.xml`.
- Nine existing gameplay regressions for luck recording also passed earlier,
  as recorded in PostMatchLuck/TASKS.md. Distinct scope across the completed
  report/luck/prototype verification: 74 EditMode + 26 PlayMode = 100 passed.
  No complete suites were executed.
- Final sample-data previews `postmatch-ratings.png`,
  `postmatch-ratings-selected.png` and `postmatch-ratings-page2.png` are retained
  beside the final EditMode XML. First-page spacing was tightened to keep the
  formula and prototype caveats inside the visible report area.
- The original score/stats layouts and live MatchStatsUI were not restyled.
  Unrelated serializer records were restored only with Room unloaded and imported
  synchronously before reopening; incidental font changes removed.
- Unexecuted: complete played-match/shootout flows, hardware-pointer acceptance,
  whole suites and calibration against manager judgment. Weights remain a prototype.
