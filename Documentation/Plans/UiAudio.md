# Menu interaction audio

Generate four original procedural mono PCM WAVs: quiet 65 ms hover swish,
neutral 85 ms selection tick, soft rising 180 ms advance and descending 160 ms
back cue. No purchased samples, services, middleware or music.

Use existing device-local Game sounds preference. Code gains live in
GameplayAudioConfig; hover .18, select .35, advance .45, back .4. Hover has a
global .18 s cooldown; selection .08 s; navigation .1 s. Hover cannot replace
selection/navigation; selection cannot replace navigation. A scene-authored
singleton source persists across scene loads; duplicates are discarded, never
created at runtime. UI remains audible during pause via unscaled time and
ignoreListenerPause. Muting stops the voice and cannot replay it on unmute.

Author nonvisual feedback components on existing menu controls and dropdown
templates in MainMenu, CreateLoadRoom, CreateNewGameScene, Draft, FreeDraft and
Room pause submenus. Preserve every RectTransform and designer layout. Dropdown
opening/selection sounds come from real pointer/submit events on the dropdown and its options;
initial values and programmatic refreshes are silent. Accepted scene/menu
navigation emits advance/back from the owning callback after its guards. Failed
validation must not emit a positive navigation cue. No navigation is delayed or
initiated by sound. Generic selection is deferred until LateUpdate and suppressed
when navigation already played in that frame. Music remains deferred.

Verification: compile in the open Editor; dedicated UI-audio tests only for
cooldowns/priorities, dropdown initialisation and expanded choice, disabled
controls, mute/pause, duplicate suppression and real scene-lifetime persistence.
Audit authored bindings and compare layouts before/after authoring. Retain native
test artifacts; report listening acceptance separately from silent playback tests.

| Event | Asset in Assets/Audio/UI | Duration | Source gain |
| --- | --- | --- | --- |
| Interactable menu hover | MenuHover.wav | 65 ms | .18 |
| Checkbox click/submit, dropdown open/choice, ordinary menu button | MenuSelect.wav | 85 ms | .35 |
| Accepted advance/confirmation | MenuAdvance.wav | 180 ms | .45 |
| Accepted back/return | MenuBack.wav | 160 ms | .40 |

All clips are original, deterministically generated 48 kHz, 16-bit mono PCM.
Raw peaks are .20/.24/.30/.27 respectively, before source gain. Generation and
explicit scene authoring live in Assets/Editor/MenuAudioAuthoring.cs; playback
uses Assets/Audio/UI/MenuAudio.prefab and the shared GameplayAudioLibrary.asset.
Runtime gains/cooldowns are in Assets/Scripts/Audio/GameplayAudioConfig.cs.
Unity does not serialize ignoreListenerPause; MenuAudioPlayer sets it in Awake.

Implementation batches completed: synthesize/import assets and library; persistent
voice and feedback policy; accepted navigation hooks; six-scene authoring; focused
automated checks. Authoring verified unchanged RectTransform geometry for all
existing controls, including the audio labels/checkboxes and match statistics.
Scene feedback counts: MainMenu 660, CreateLoadRoom 12, CreateNewGameScene 68,
Draft 7, FreeDraft 28, Room pause/substitution controls 99. Counts include inactive
templates. Scene objects are saved through the Editor; Room is restored afterward.

Compilation: open Editor clean. Latest MenuAudioEditorTests: 11 passed, 0 failed,
0 skipped, native results TestResults/GameplayRegression/Runs/20261110T220640607Z/results.xml.
Latest MenuAudioPlayModeTests: 8 passed, 0 failed, 0 skipped, native results
TestResults/GameplayRegression/Runs/20261110T220746395Z/results.xml; both directories
also retain progress.jsonl. Playback tests mute their owned source and exercise
pause, mute/unmute, priorities, real dropdown input, silent programmatic changes,
disabled buttons, rejected draft start and persistent scene lifetime. Earlier
failures remain in native artifacts: nonserialized pause flag fixed in playback,
and TMP fixture corrected to wait for Start before opening its popup.

Listening acceptance is pending. In MainMenu and Room pause/submenus, check rapid
hover sweeps stay subtle, dropdowns tick on actual choices, forward/back cues are
distinct and finish across scene changes, and Game sounds Off silences them.
No ordinary gameplay or whole gameplay suite was started. No additional gameplay
regression scope is warranted for these presentation-only hooks.

Checkbox/dice follow-up: all authored Toggle controls now select on real left click
or keyboard submit, including Pause and Game Settings. Right clicks, disabled
controls and programmatic values stay silent. Game mute governs this feedback.
Room binding audit confirmed every Pause/Game Settings Selectable has feedback,
including all three audio toggles and the Back footer; no scene edits were needed.
Back to Main Menu, Back to Pause and Resume retain their accepted Back callbacks.

Pre-match roll-offs also play DiceRoll.wav through the persistent menu voice;
normal and rigged rolls share the cue without adding gameplay RNG calls. Match
rolls use the director's Game effects voice. Both respect the existing .1 s dice
cooldown; no additional UI geometry or sources are created.

Latest follow-up results: MenuAudioEditorTests 11/11 passed under
TestResults/GameplayRegression/Runs/20261110T222145673Z/; MenuAudioPlayModeTests
11/11 passed under TestResults/GameplayRegression/Runs/20261110T222114039Z/;
GameplayDiceAudioPlayModeTests 8/8 passed under
TestResults/GameplayRegression/Runs/20261110T221932665Z/. Native results.xml and
progress.jsonl retained in each. Zero failed/skipped. Compilation independently
clean. One prior setup attempt aborted before cases due to the framework's scene
save task observing Play Mode; its pending collection was cancelled, stopped
state confirmed, and the focused run restarted successfully. Actual listening
remains user verification; tests mute their audio sources.
