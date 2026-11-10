# Dice audio

- [x] Accepted normal/rigged rolls emit a live incident before resolution; no advancement or delays from audio.
- [x] Original short procedural rattle, Game category, existing .1 s cooldown and shared voice.
- [x] Compile, run only GameplayDiceAudioPlayModeTests, inspect native artifacts.
- [ ] Listen to actual rattle in match; automated playback uses muted sources.

Verify normal and rigged outcomes, unchanged gameplay RNG/state/possession,
one notification, duplicate suppression, rapid-roll cooldown, Game mute, pause
and missing clips. No whole gameplay suite.

Compilation clean in open Editor. GameplayDiceAudioPlayModeTests: 8 passed,
0 failed/skipped. XML and progress.jsonl:
TestResults/GameplayRegression/Runs/20261110T221932665Z/.
MenuAudioPlayModeTests additionally covers real pre-match normal/rigged roll-offs:
11 passed, 0 failed/skipped, artifacts 20261110T222114039Z/.
Clip: Assets/Audio/UI/DiceRoll.wav, original procedural 250 ms, mono 48 kHz,
16-bit PCM, peak .38 before existing .65 dice pool gain. No purchased samples.
Both results and UnityEngine.Random state match audio-disabled resolution.
No broader gameplay suites were run; listening remains pending.
