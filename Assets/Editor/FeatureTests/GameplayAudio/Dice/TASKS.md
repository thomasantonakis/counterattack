# Dice audio

- [x] Accepted normal/rigged rolls emit a live incident before resolution; no advancement or delays from audio.
- [x] Six randomized source roll clips, Game category and existing .1 s roll cooldown.
- [x] Play one 5 s wiggle once per active roll wait on its own AudioSource; stop it when that roll resolves.
- [x] Increase dice roll and wiggle gains to sit above crowd audio.
- [x] Compress and normalize the six roll clips and five-second wiggle; all peak-limit at -1 dBFS, with RMS levels around -21 to -24 dBFS.
- [x] Focused randomization/RNG and wait-lifecycle tests.
- [ ] Listen to the actual roll and wait sounds in match; automated playback uses muted sources.

Verify normal and rigged outcomes, unchanged gameplay RNG/state/possession,
one notification, duplicate suppression, rapid-roll cooldown, Game mute, pause
and missing clips. No whole gameplay suite.

Latest verification after the source wiggle replacement and gain increase:
compilation clean. PlayMode `GameplayDiceAudioPlayModeTests`: 9/9 passed,
including start-once/stop-on-end and gain checks; XML and progress.jsonl:
TestResults/GameplayRegression/Runs/20261112T114729744Z/. EditMode random-pool
test remains 1/1 passed at TestResults/GameplayRegression/Runs/20261112T113714966Z/.
MenuAudioPlayModeTests additionally covers real pre-match normal/rigged roll-offs:
11 passed, 0 failed/skipped, artifacts 20261110T222114039Z/.
Roll clips: Assets/Audio/UI/DiceRoll1.wav through DiceRoll6.wav, each 720 ms,
cut from F4Kxnv3Hzmk at 11.90, 14.70, 17.61, 21.88, 29.72 and 33.02 seconds.
The shared voiced lead-in has been removed. The existing audio policy randomizes
from the pool without consuming UnityEngine.Random. Wait cue:
Assets/Audio/UI/DiceWiggle.wav is now a continuous 5 s stitch from
`_P_E0qSQYLc`: the active dice rattle at 43.24-44.66 s and the rattle at 1-2 s,
alternated A/B/A/B/A with 60 ms crossfades and no inserted gaps. The clip has a
short boundary fade and +6 dB gain. It starts once per DecisionTiming roll wait
and is stopped when the wait ends; the resolved roll cue replaces it on the
shared effects source. Dice roll gain is .95 (was .65); wiggle gain is .9 (was
.5). The clips are 48 kHz, 16-bit mono PCM. The first source description
permits use without attribution.
No broader gameplay suites were run; listening remains pending.

2026-11-12 follow-up for Room coin-toss audibility: the Room panel, gameplay
audio director, library, unmuted game-effects source and dedicated unmuted
wiggle source were confirmed live; Game category gain was 0.9, and cue gains
were 0.95 (roll) and 0.9 (wiggle). The original WAVs were still low-level
(roughly -33 to -39 dBFS RMS), so the six roll WAVs and the wiggle WAV now use
compression, +22 dB gain and a -1 dBFS limiter. Resulting RMS levels are about
-21 to -24 dBFS. The controller's Room roll path routes through the gameplay
director; draft/pre-match uses MenuAudioPlayer. Listening in a live match is
still required to judge the final balance against crowd playback.

Follow-up: Room gameplay and dice duels now route wait wiggles through the
dedicated `GameplayAudioDirector.diceWiggleEffects` source; `Room.unity` has a
scene-authored `GameplayAudio/DiceWiggleAudio` source. Draft coin flips use the
dedicated `MenuAudioPlayer.wiggleVoice` on `MenuAudio.prefab`. Roll sources keep
playing while a wiggle starts/stops. Focused PlayMode tests passed 1/1 for the
duel source separation and 1/1 for the Draft source separation on 2026-11-12.
