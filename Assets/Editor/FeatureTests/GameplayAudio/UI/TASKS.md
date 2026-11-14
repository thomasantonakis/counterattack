# Four UI cues

- [x] Inspect existing audio, mixers (none), menu callbacks, scene flow and saved Game preference.
- [x] Save specification in Documentation/Plans/UiAudio.md.
- [x] Generate four deterministic original WAVs and configure library/persistent source prefab.
- [x] Hook accepted forward/back callbacks; preserve validation and scene timing.
- [x] Author feedback in all six menu-flow scenes, compare RectTransforms and restore Room.
- [x] Focused UI-audio policy/playback/dropdown/lifetime tests through the open Editor.
- [x] Record compilation, native results, layout geometry preservation and listening limits.
- [x] Replace cues with user-selected YouTube sound clips, trimmed at requested points.
- [ ] Unity import/recompile and user listening pass after Pipeline reconnect.

Replacement source mapping: MenuHover.wav from PUPfx-oxm3M at 2.10s;
MenuAdvance.wav from 4ealu9Ojkvg at 11.65s; MenuSelect.wav from 4ealu9Ojkvg at
14.00s; MenuBack.wav from 4ealu9Ojkvg at 14.19s. The initial Opus downloads
were silent; these WAVs use the AAC streams. Signal levels were checked after
export. Unity import and playback validation are pending because the Pipeline
reported no connected Editor during this edit.

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

Coin-flip follow-up: Draft/FreeDraft now pick a randomized dice clip without
consuming Unity gameplay RNG. Draft, Room first-half/extra-time, and shootout
roll-off stages auto-roll each manager after `DecisionTimingConfig.AutomaticRollSeconds`
(5 seconds); a single wait wiggle starts after 0.25 seconds and stops at the roll
or when the panel closes. Focused PlayMode tests passed 3/3 on 2026-11-12,
including auto-roll timing and normal/rigged clip routing. In-room match rolls
continue through `ResolveGameplayDiceRoll` and the shared dice audio gain.

Playback correction: Draft's `MenuAudio` prefab now has a separate wiggle voice;
Room's `GameplayAudio` object has a dedicated scene-authored wiggle source. A
roll cue and its next wait wiggle no longer replace each other. The focused
Draft source-separation test passed 1/1.
