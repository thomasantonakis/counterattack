# Four UI cues

- [x] Inspect existing audio, mixers (none), menu callbacks, scene flow and saved Game preference.
- [x] Save specification in Documentation/Plans/UiAudio.md.
- [x] Generate four deterministic original WAVs and configure library/persistent source prefab.
- [x] Hook accepted forward/back callbacks; preserve validation and scene timing.
- [x] Author feedback in all six menu-flow scenes, compare RectTransforms and restore Room.
- [x] Focused UI-audio policy/playback/dropdown/lifetime tests through the open Editor.
- [x] Record compilation, native results, layout geometry preservation and listening limits.
- [ ] User listening pass: especially hover loudness, neutral dropdown tick and navigation character.

Compilation clean. MenuAudioEditorTests: 11/11 passed, native XML/progress under
TestResults/GameplayRegression/Runs/20261110T220640607Z/.
MenuAudioPlayModeTests: 8/8 passed, native XML/progress under
TestResults/GameplayRegression/Runs/20261110T220746395Z/.
Zero failed/skipped in final runs. Tests use muted owned sources; no listening
claim or complete gameplay regression claim. Specification and binding counts:
Documentation/Plans/UiAudio.md. Existing RectTransform records also compared
against HEAD with zero changed records in all six scenes.

Follow-up: checkbox left clicks/submits are audible; programmatic changes,
disabled input and right clicks silent. Pre-match normal/rigged roll-offs use
the assigned dice pool and preserve RNG/results. Audited all Pause/Game Settings
bindings, including Back footer and audio toggles. Latest compilation clean;
MenuAudioEditorTests 11/11: 20261110T222145673Z; MenuAudioPlayModeTests 11/11:
20261110T222114039Z (both under TestResults/GameplayRegression/Runs/).
Zero failed/skipped, XML/progress retained. Listening still pending.
