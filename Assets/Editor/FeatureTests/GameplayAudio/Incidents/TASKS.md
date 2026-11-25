# Match incident audio

- [x] Confirm penalised-stand foul, paired duel, deferred card/injury/pass decisions.
- [x] Confirm announcements after pause and both-stand preparation loops.
- [x] Inspect and connect confirmed incident/transition entry points.
- [x] Assign selected pools and author audio sources through the Editor, preserving audio UI.
- [x] Verify 70/30 shares and goals filling each stand's remaining layer gain.
- [x] Verify paired records, pause/mute, substitutions, snapshots and preparation exit.
- [x] Compile and execute only explicit audio fixture filters; retain native XML.
- [ ] Record listening acceptance separately. No full gameplay suites.

## Automated verification (2026-11-10)

Open Editor compilation completed with no script errors. Only explicit audio
fixture/method filters were executed, serially; no whole gameplay suites.

| Filter | Passed | Failed | Skipped | Native results.xml under TestResults/GameplayRegression/Runs/ |
| --- | ---: | ---: | ---: | --- |
| GameplayAudioEditorTests | 23 | 0 | 0 | 20261110T154850331Z/results.xml |
| GameplayIncidentAudioPlayModeTests (initial 72 cases) | 72 | 0 | 0 | 20261110T155049702Z/results.xml |
| GameplayIncidentAudioPlayModeTests.OutsideHandballUsesPenalisedStand | 2 | 0 | 0 | 20261110T155327986Z/results.xml |
| GameplayGoalAudioPlayModeTests | 16 | 0 | 0 | 20261110T155403265Z/results.xml |
| GameplayIncidentAudioPlayModeTests.SnapshotAvailabilityPlaysOnce (boundary rerun) | 2 | 0 | 0 | 20261110T155659168Z/results.xml |
| GameplayAudioPlaybackPlayModeTests | 6 | 0 | 0 | 20261110T155745918Z/results.xml |

Each native folder retains progress.jsonl. This covers 23 distinct EditMode and
96 distinct PlayMode cases; the two snapshot cases were rerun after adding
half-time/full-time/setup guards. Tests use silent muted owned clips, deterministic
owned boards and existing cleanup, and restore local preference keys. Actual
steal success/failure, recovery foul and outside-box goalkeeper handball exercise
real movement paths and continuation. Other incident cases use real GameLog,
transition or substitution record entry points. Inaccuracy tests exercise the
restart award entry, not a complete throw/corner/goal-kick execution. Preparation
and snapshot tests observe real public availability/state without driving full
set-piece play. Scoring/possession/state/roll sequence/gameplay RNG remain unchanged
by notification delivery.

Room returned stopped and clean: 16 preauthored AudioSources, 27 pool entries and
50 clip assignments. No runtime source creation. All 17 Audio settings
RectTransforms match the before-work snapshot exactly (labels, checkbox positions,
anchors, pivots and sizes). A broad 3,530-row UI snapshot is not byte-identical:
260 driven layout values reset when Unity restored the scene, including inactive
stats/lineup/hover layout children. These were not authored or restored by audio
code; no layout API or UI authoring helper was invoked. The Stats root is unchanged.
Main Menu was not reauthored.

## Listening acceptance

- [ ] Hear 70/30 stand balance with independent Walla and chants.
- [ ] Goals fill each stand budget above Walla and fading chants; verify perceived
  balance and clipping with actual clips. Existing shared 0.75 output trim remains.
- [ ] Duels and steals use both result pools; fouls use the penalised stand only.
- [ ] Off-targets use shooter groan/opponent sarcastic cheer; outfield blocks and
  saves use shooter groan/defending applause without paired-record duplication.
- [ ] Hear selected kickoff/half-time/full-time recordings in the independent
  whistle channel. Embedded recording crowds are not duplicated per stand.
- [ ] Preparation loops crossfade on both stands and stop on execution/pause;
  Walla continues. Snapshot urging plays once per available opportunity, never
  each frame or during match breaks.
- [ ] Substitutions stay silent in pause and announce once after resume, sequentially
  for both teams. Multiple away changes choose DoubleAwaySubstitution; injury
  substitutions add the selected applause for the outgoing player's stand.
- [ ] Assess long reactions/announcements and the original duel clips' short lead-ins;
  crop review remains pending, originals intact.

At this original batch, cards were unassigned; the follow-up in ../FoulTiming/TASKS.md
now adds the fouled-stand card cheer. Injury incidents, ordinary-pass applause and
committed-shot anticipation remain deliberately unassigned. Walla accents and separate UI/game/music cues
remain separate batches.

Optional broader user verification: fixtures `LegacyShootingBranchPlayModeTests`
and `LegacyOwnGoalPlayModeTests` in `Assets/Tests/PlayMode/` exercise complete
shot/goal continuation beyond these notifications. Substitution listening still
needs a real menu confirmation/resume pass. Do not substitute a whole-suite run
for these focused audio results.
