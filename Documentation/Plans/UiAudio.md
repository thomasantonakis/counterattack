# Menu interaction audio

Use four mono PCM WAVs captured from the user's selected YouTube sound sources:
hover from `PUPfx-oxm3M` (2.10s, 240 ms); advance/success from `4ealu9Ojkvg`
(11.65s, 420 ms); selection from the same source (14.00s, 140 ms); and back
from the same source (14.19s, 250 ms). Short fades are applied at clip
boundaries. The AAC streams are used because the initially downloaded Opus
streams decoded to silence. Preserve the existing asset paths and bindings.

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

Clips are 48 kHz, 16-bit mono PCM, trimmed from the source videos above; signal
levels were checked after export and all contain nonzero audio samples.
Explicit scene authoring lives in Assets/Editor/MenuAudioAuthoring.cs; playback
uses Assets/Audio/UI/MenuAudio.prefab and the shared GameplayAudioLibrary.asset.
Runtime gains/cooldowns are in Assets/Scripts/Audio/GameplayAudioConfig.cs.
Unity does not serialize ignoreListenerPause; MenuAudioPlayer sets it in Awake.

## Reusing this YouTube-to-game-audio workflow

Use this workflow when a user supplies a YouTube sound and asks to crop it into
the game. Keep the source URL/ID, selected start and duration, and output asset
path in the relevant feature notes. Check available formats first, then prefer
the AAC audio stream: the Opus versions of the sources above decoded to silent
WAVs. `yt-dlp -F 'https://www.youtube.com/watch?v=VIDEO_ID'` lists formats. With
the AAC format ID from that listing, download it without transcoding:

```sh
yt-dlp -f FORMAT_ID -o '/tmp/youtube-source.%(ext)s' 'https://www.youtube.com/watch?v=VIDEO_ID'
```

Crop from the requested offset and encode a Unity-friendly 48 kHz, mono,
16-bit PCM WAV. Set the fade durations no longer than the crop; omit either
fade when the sound needs a hard boundary. Example for a 420 ms cue starting at
11.65 seconds, with short edge fades:

```sh
ffmpeg -ss 11.65 -i /tmp/youtube-source.m4a -t 0.42 \
  -af 'afade=t=in:st=0:d=0.01,afade=t=out:st=0.39:d=0.03' \
  -ar 48000 -ac 1 -c:a pcm_s16le Assets/Audio/UI/MenuAdvance.wav
```

Use the actual downloaded extension in the input path. Inspect the result with
`ffprobe` (duration, sample rate, channels and PCM codec) and confirm it contains
nonzero samples before importing. Preserve the downloaded source outside the
Unity assets and leave existing source assets intact. Add the prepared WAV to
the appropriate editable audio library/prefab/scene binding so designers can
inspect it and the game can play it; retain the intended asset path and test
the feature's playback behavior. Do not create visual or audio objects only at
runtime.

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

Listen in MainMenu and Room pause/submenus to confirm hover loudness, dropdown
selection tick, navigation distinction and sound through scene changes. The
open Unity Pipeline was unavailable during replacement, so assets still require
Unity import and focused playback checks after the Editor reconnects. No ordinary
gameplay or whole gameplay suite was started.

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
