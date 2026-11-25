# Post-match luck

- [x] Capture pre-resolution inputs without changing rules, rolls, random consumption or player stats.
- [x] Compare equally weighted favourable outcomes with their expected chances; opposing teams mirror each other.
- [x] Count paired ground/aerial contests once, with ties worth half and correct foul/jackpot/goalkeeper branches.
- [x] Show raw rolls, assessed rolls/checks, forced exclusions and unassessed coverage. Never fabricate missing history.
- [x] Author an editable Luck page and tab; keep named-team filters on Players/Pitch only.
- [x] Compile, execute focused EditMode/PlayMode tests, inspect artifacts and rendered page.
- [x] Execute affected report/pitch/hover/audio and gameplay capture regressions by focused filters.

Formula: surplus = sum(actual favourable value − pre-roll expected value);
luck = 100 × surplus / assessed checks, in percentage points. Ties use 0.5.
Each local check has equal weight, not goal value. This is a sample outcome
comparison, not a calibrated skill/rating or statistical significance claim.
Raw player-roll counts remain visible through total/coverage; direction, distance,
stoppage and off-target scatter have no inherently good die face and remain unassessed.
Incomplete contests, unsupported/old evidence and any contest with a forced roll
are excluded. Known legacy single-roll evidence is reconstructed only when its
required inputs exist. Ratings remain deferred.

Native fixtures own and restore board/singleton/event/random state, suppress the
legacy runner, and never load or write live saves. New fields are optional save
evidence (model version 1); old records remain readable.

## Verification — 2026-11-18

- Compilation completed with zero errors; final focused-test compilation had zero warnings.
- `PostMatchLuckEditorTests`: 22 passed, zero failed/skipped, run
  `TestResults/GameplayRegression/Runs/20261117T185500061Z/results.xml`.
- `PostMatchLuckPlayModeTests`: 12 passed, zero failed/skipped, run
  `TestResults/GameplayRegression/Runs/20261117T185735443Z/results.xml`.
- Existing movement/recovery audits: 3 passed in `20261117T185841655Z/results.xml`.
  Existing header-save/handling-spill continuations: 6 passed in
  `20261117T185956983Z/results.xml`.
- Final affected post-match EditMode run: 65 passed (25 report, 18 pitch, 22 luck),
  zero failed/skipped, `20261118T053028941Z/results.xml`.
- Final post-match PlayMode run: 17 passed (12 luck, 2 hover, 2 audio, 1 dropdown),
  zero failed/skipped, `20261118T053211710Z/results.xml`.
- Total distinct executed scope: 65 EditMode + 26 PlayMode = 91 passed, including
  the nine existing gameplay regressions. No whole suites were run.
- Final 1920x1080 sample-data preview: `postmatch-luck.png` beside the final
  EditMode XML. An explicit light background keeps this new page readable in
  both existing panel themes; spacing was tightened so the method fits.
- Main-thread timeouts were a blocking Reload dialog; the user handled it
  manually. Unrelated scene serializer records were restored only while Room
  was unloaded, then synchronously imported before reopening. Font churn removed.
- Unexecuted: hardware-pointer/raycast acceptance, full played-match/extra-time/
  shootout flows and full suites. Historical reports clearly show incomplete
  coverage; unsupported rolls are never presented as assessed results.
