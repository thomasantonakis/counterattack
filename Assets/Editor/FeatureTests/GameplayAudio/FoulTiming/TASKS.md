# Foul timing and card reaction follow-up

- [x] Identify live goal clips and assign a one-second-trimmed celebration copy.
- [x] Confirm early foul cue and Sarcastic_Shout_Whey_01 for the fouled stand's card response.
- [x] Reproduce delayed tackle-foul cue before changing its trigger.
- [x] Move foul audio to the tackler/recovery roll without changing resolution timing.
- [x] Suppress the later statistics notification and test one cue per foul.
- [x] Add card reaction; keep injury incidents silent.
- [x] Compile and run only focused audio fixtures through the live Editor.
- [x] Record native artifacts and listening acceptance separately.
- [ ] User listening acceptance of the revised goal/foul/card timing.

Initial delayed-foul reproduction: 1 failed as expected, artifact
`TestResults/GameplayRegression/Runs/20261110T161905173Z/results.xml`.
Initial expanded run: 32 passed / 16 failed because the owned board did not build
the dangerous-tackle geometry cache (fixture defect, no gameplay-rule change).
Artifact: `TestResults/GameplayRegression/Runs/20261110T162153310Z/results.xml`.
The fixture now invokes the real CalculateGoalDistanceAndDangerousTackleCaches
builder, rather than setting expected danger flags. Final new-feature run:
**48 passed, 0 failed, 0 skipped**, native XML
`TestResults/GameplayRegression/Runs/20261110T162357038Z/results.xml`.
Sibling progress.jsonl files are retained. Open Editor compilation is clean.

The 32 tackle cases use actual PerformTackleDiceRoll entries: normal 1/2, dangerous
1/2/3, nutmeg 1/2 and jackpot, both penalised teams and both directions. Foul
playback is asserted before the dribbler rolls; stats, state and possession cannot
advance early. The dribbler roll then reaches the existing booking continuation
with no second cue. Sixteen actual leniency/card cases cover yellow, second
yellow/red, straight red and no-card, both teams/directions. The fouled stand
gets OpponentError at 50% of its share. Injury incidents remain unassigned.

Goal asset verification: prepared copy retains exactly source PCM samples after
frame 48,000 (1.0 s), 2,093,150 frames remaining; stereo 48 kHz 24-bit, 43.607292 s.
Original intact. Saved assignment through Editor while the user match kept running;
user subsequently stopped Play Mode before code changes/testing. No gameplay was
started by Codex outside framework tests. Actual listening to revised goal/foul/
card timing is pending; the user reported substitution playback worked well.

Affected audio fixture: **74 passed, 0 failed, 0 skipped**, native XML
`TestResults/GameplayRegression/Runs/20261110T162516280Z/results.xml`.
Final scope is 122 PlayMode passes (48 new + 74 affected), no failures/skips.
No full gameplay suites or additional EditMode fixture runs were executed for this
follow-up. The owned fixtures already retain real steal, recovery-foul and keeper
handball continuation checks; no broader gameplay suite is warranted for this
presentation-only change. Editor stopped/clean, Cheer pool references the saved
one-second-trimmed copy plus unchanged Goal_Cheer_Celebrate_02. No audio UI edits.
