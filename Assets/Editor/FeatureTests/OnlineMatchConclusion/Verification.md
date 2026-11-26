# Online regular-match conclusion

- [x] Reproduce offline end-panel interference on full online match completion.
- [x] Publish a terminal decision boundary and show both managers the same result.
- [x] Author outcome/score/totals and safe return without moving designer Stats.
- [x] Cover either winner/draw, regulation/extra-time endings and host/guest Leave without writes.
- [x] Run focused checks, register every new case and refresh Windows exports.

Shootout completion keeps its separate authored result view. Actual internet
acceptance remains deferred.

The real half-end entry point reproduced offline end-panel interference in
`20261116T063629183Z/results.xml`: the live online scoreboard was hidden. Full
online regulation endings now use the authored online conclusion and skip the
offline UI relocation/hiding path. Source Stats layout remains designer-owned.
A terminal publication has owner None and no gameplay actions, rejecting stale
kickoff input. Outcome/score/totals restore on a fresh inert guest; Leave closes
session context before loading MainMenu and performs no additional save write.

Own native evidence, under TestResults/GameplayRegression/Runs:
- `20261116T063831103Z/results.xml`: 8 EditMode display-boundary cases passed.
- `20261116T063847072Z/results.xml`: 18 actual-scene PlayMode cases passed:
  Home/Away win or draw; single-half, regulation and extra-time endings; host or
  guest Leave; encoded snapshot projection and no guest rules/events/timer.
All 26 passed with zero failed/skipped; compilation separately clean.
Each case has its own entry in the deferred Mac manifest. Full Room v8 prevents
mixed-version clients from missing the shared conclusion UI.

Affected focused cases also passed, all zero failed/skipped, under the same root:
- `20261116T065205776Z/results.xml`: end-panel ownership 3 (preview asset checks).
- `20261116T065217165Z/results.xml`: continuous-session completion policy 8.
- `20261116T065225345Z/results.xml`: full snapshot payload bounds 15.
- `20261116T065241506Z/results.xml`: authored Room menus/Leave 6.
- `20261116T065334093Z/results.xml`: complete shootout 1, preserving its own view.
- `20261116T065411141Z/results.xml`: Room coin/input-hold guards 2.
This is 26 own + 35 affected distinct cases. Thirteen verifier tooling tests
passed. No complete suite or actual paired internet run was executed. A broader
follow-up, if requested, is Assets/Tests/PlayMode/Features/OnlineShootout,
fixture/category OnlineShootoutPlayModeTests / OnlineShootout, because both
conclusion views share terminal state and safe Leave. Its other 35 cases were
not rerun here; one complete real shootout plus all 26 new conclusion cases ran.

Windows exports succeeded:
- Normal game `build_1c6b680338ac`: `20261116T065701Z-normal-Windows-build/build-summary.json`.
- Room proof `build_fb3323f60eba`: `20261116T065739Z-Room-Windows-build/build-summary.json`.
- Gameplay proof `build_86481c733b7a`: `20261116T065801Z-Gameplay-Windows-build/build-summary.json`.
Normal has nine existing warnings; proofs two each. Each successful report keeps
the known nonfatal EOS vswhere.exe diagnostic; C# compilation has no errors.
Packaged checks passed: `20261116T065855067Z-Room-local`,
`20261116T065908909Z-Gameplay-local`, `20261116T065916494Z-Room-login`.
Login registered native liveness notifications. These are local/login checks,
not paired full-match acceptance. Mac execution remains deferred.
