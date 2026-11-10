# Snapshot cue and international crowd balance

- [x] Inspect actual movement/shot availability, GK input ownership, voice priorities and clip levels.
- [x] Identify International matchType as the existing match classification.
- [x] Reproduce missing cue from the real dribbler continuation offer.
- [x] Observe both availability paths; defer blocked cues only while an offer remains valid.
- [x] Raise snapshot cue gain and prevent next-frame/GK-move interruption or repeated pace cues.
- [x] International stand shares 50/50; other matches 70/30, all crowd layers.
- [x] Compile and run focused audio fixtures; retain native artifacts.
- [ ] Listening acceptance with actual recording, separately from silent automated tests.

Reproduction: real ContinueDribblerMovement sets the MovementPhaseManager
snapshot flag while ShotManager.isAvailable remains false; old audio stayed silent.
One failed reproduction:
`TestResults/GameplayRegression/Runs/20261110T164258402Z/results.xml`.
After the fix: **13 passed, 0 failed, 0 skipped**, native artifact
`TestResults/GameplayRegression/Runs/20261110T164846847Z/results.xml`. Sibling
progress.jsonl retained. Open Editor compilation completed without errors.

Four movement cases use the actual dribbler continuation offer for both teams
and attacking directions. They assert source playback survives a later frame and
a GK-input ownership interruption, does not restart with transient flag changes,
and uses 80% stand intensity plus +6 dB recording compensation. Five pending-cue
cases verify foreground/GK deferral, stale-offer cancellation, pause and mute.
Four match-type cases cover both goal sides in International and Club fixtures:
Walla, chants, goal headroom, preparation and mute. Club uses includeInternationals
=true to verify that deck inclusion does not select an international crowd mix.
This is presentation-only: no gameplay rules, clock, input ownership or AI edits.

Config remains GameplayAudioConfig.cs. Club/other shares .7/.3; International .5/.5.
SnapshotRepeatCooldownSeconds=15, SnapshotIntensity=.8, SnapshotClipGainDb=6,
SnapshotPriority=5. An availability cue has one incident ID; local playback may
wait while the opportunity remains valid. Once started it continues naturally
through flag/GK changes; actual outcomes, pause and local mute still take precedence.
No source or UI authoring, original clips unchanged, no whole gameplay suites.

Listening pending: hear the real urging recording during dribbling and loose-ball
snapshots, including GK movement; confirm the roughly +10 dB increase is suitable.
Check a real International match balance separately from numerical gain checks.

Existing ShotManager offer regression: 2 passed, 0 failed/skipped; artifact
`TestResults/GameplayRegression/Runs/20261110T165002630Z/results.xml`. Final executed scope: 15 focused PlayMode passes, zero failures/skips
in final runs. No broader audio/gameplay suites were rerun.
