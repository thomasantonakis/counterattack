# Gameplay audio verification

## Foundation batch

- [x] Create branch and save agreed specification before implementation.
- [x] Code configuration: 70/30 stands; Walla 40–50%; chants 30%; ordinary reactions 50%; goals fill remaining stand gain.
- [x] Three independent local On/Off preferences; code-configured volumes, no sliders or match-save changes.
- [x] Independent RNG, random pools, bounded duplicate history and cue cooldowns.
- [x] Authored-source playback, priorities, chatter fades and pause/resume API.
- [x] Author Room and Main Menu rigs and settings controls through live Editor.
- [x] Compile/reload through open Editor and inspect Editor.log.
- [x] Run only EditMode fixture `GameplayAudioEditorTests`, category `GameplayAudio`.
- [x] Run only PlayMode fixture `GameplayAudioPlaybackPlayModeTests`, category `GameplayAudio`.
- [x] Record actual passed/failed/skipped counts and native XML below.

## Pending release batches

- [x] Import and inventory the purchased crowd pack; record the user's audition selections.
- [ ] Supply separate dice/UI recordings when that deferred scope resumes.
- [ ] Agree remaining incident matrix and music.
- [x] Wire selected confirmed goals/incidents with stable IDs and explicit team identity.
- [ ] Wire actual dice/invalid clicks/Stats transitions and test their real entry points.
- [ ] Listening pass for fades, looping, independent chants, reactions, levels and fatigue.
- [ ] Verify pause/resume with assigned clips and mute changes while paused.
- [x] Inspect rendered checkbox layouts in both scenes.
- [x] Verify the three checkbox bindings, persistence, refresh and listener cleanup automatically.
- [ ] Manual pointer interaction acceptance in the final game UI.

No full gameplay suites requested or run. Foundation tests are not listening
verification or evidence that pending gameplay event integrations work.

## Continuous Walla and chant interruption (2026-11-10)

- [x] Two pre-authored Walla sources per stand, 2-second linear crossfades.
- [x] Choose 40–50% intensity per new clip; exclude outgoing clip when possible.
- [x] Prefer avoiding the other stand's active clips; relax for small pools.
- [x] Preserve bed gain across a crossfade; no new runtime GameObjects.
- [x] Separate one-shot chant channels at 30%; per-stand slots 45–90 s after finish.
- [x] On an accepted event, fade active chants over 0.35 s, discard them, schedule
  new slots; keep Walla playing underneath. Event gain is 50% for both stands.
- [x] Apply a uniform code-configured 0.75 crowd output trim for fade headroom.
- [x] Assign user-selected Chatter 7, Chant 12, Cheer 2, Groan 6 (prepared crop).
- [x] Save Room with 11 sources and verify audio UI transforms are unchanged.
- [ ] Actual listening acceptance of clip transitions, levels and chant frequency.
- [ ] Add the five WallaAccent overlays; this batch implements chants only.
- [x] Connect the selected gameplay goal/foul/duel/shot/transition events and test
  real notification entry points. Injury/pass/anticipation and remaining
  trim decisions stay in the selection plan.

Compilation is clean in the live Editor; active `Logs/Editor.log` was inspected.
Focused results, retained with native XML and per-case progress:

- **23 EditMode passed, 0 failed, 0 skipped**:
  `TestResults/GameplayRegression/Runs/20261110T150927531Z/results.xml`.
- **6 PlayMode passed, 0 failed, 0 skipped**:
  `TestResults/GameplayRegression/Runs/20261110T151102319Z/results.xml`.

Playback uses muted, owned silent clips and exercises real AudioSources, manually
positions playback near the transition and advances audio envelopes deterministically.
Cases cover 0/1/2/3-clip pools, no avoidable repeat, complementary crossfade gain,
mute mid-fade, stop/restart, both scoring sides, independent chants, gradual
chant fade-out, future-slot scheduling, continuing Walla, pause/resume and category
mute. These tests do not constitute a real-asset listening pass or a device build.

The first authoring pass exposed a Unity-object null-coalescing bug in the new
authoring helper; fixed with an explicit Unity null check, then completed and
saved the rig. No UI authoring method was invoked. A read-only check briefly
timed out during post-test Room restoration; the log and subsequent inspection
confirmed the restored, clean Room and 11-source rig. No ordinary gameplay was started.

## Common chatter placeholders (2026-11-10)

Imported and assigned both `Assets/Audio/Placeholders/thomas/chatter_01.wav`
(11.004 seconds) and `chatter_02.wav` (6.72 seconds) to the shared Chatter pool
through the live Editor. Both are mono, 16-bit PCM, 16 kHz. Originals are retained.
Read back the pool references and confirmed Room uses this library, Stadium is
enabled, both chatter sources are active/unmuted, and one active listener is
enabled. No scripts or UI were changed; no gameplay was started or tests rerun
for this asset assignment. Actual listening, gain balancing and loop seams remain
to verify. User can start Room to hear independent looping stand selections;
pause stops them and resume selects chatter again.

## Single-row Room adjustment (2026-11-10)

Room now uses `Audio   Stadium [ ]   Game [ ]   Music [ ]` on one horizontal
row (936 × 62). Reclaimed the vertical group's excess space: the settings card
is now 80 units taller than the pre-audio card, rather than 248. Other settings
positions, checkbox bindings and Main Menu placement/layout are preserved.

Validated clean compilation through the open Editor and inspected a render of
the actual row at `/private/tmp/room-audio-single-row.png`. This is a pure layout
change; no automated gameplay tests or suites were rerun. Existing audio test
results below are from the preceding checkbox functionality change.
Stopped the user's running Play Mode through CLI before authoring; saved Room
through the Editor and left it stopped.

## Checkbox revision (2026-11-10, before single-row adjustment)

Room audio is under `EditSettingsPanel/MenuCard_PauseSettings/WhiteBkg/ButtonsPanel/AudioSettings`,
reachable through Pause → Game Settings. The Game Settings card is 248 units
taller, with capture/status footer controls moved down by the same amount.
Existing dropdown positions and Stats layout are unchanged. Main Menu's root
audio panel retains the designer's position `(-1, -922.6)`, anchors/pivot `(1, .5)`
and size `(390, 310)`. Both panels have three checkboxes and zero sliders.
Checkbox graphics reuse the existing ToggleButton deck-inclusion prefab design.

Category volume now comes from `GameplayAudioConfig.CategoryVolume`; only the
On/Off setting is saved. Old slider preference values are deliberately ignored.

- Compilation: clean through the live Editor; inspected active Editor.log.
- EditMode: **22 passed, 0 failed, 0 skipped**.
  `TestResults/GameplayRegression/Runs/20261110T080721740Z/results.xml`
- PlayMode: **2 passed, 0 failed, 0 skipped**, silent owned clips.
  `TestResults/GameplayRegression/Runs/20261110T080739929Z/results.xml`
- Visual verification: isolated world-space renders of the actual authored card
  and panel inspected at `/private/tmp/room-audio-checkbox-preview.png` and
  `/private/tmp/mainmenu-audio-checkbox-preview.png`. Temporary preview objects
  were destroyed. Screen-space camera renders omit Canvas UI in this setup;
  the world-space renders successfully show the layout and checkbox sprites.
- No full suites or ordinary gameplay started. Actual listening remains pending.

## Initial foundation results (superseded UI)

2026-11-10: open Editor recompile completed without script errors. The active
log is `Logs/Editor.log` (the user-library Editor.log redirects there).

- EditMode: **21 passed, 0 failed, 0 skipped**.
  `TestResults/GameplayRegression/Runs/20261110T071709653Z/results.xml`
- Final PlayMode: **2 passed, 0 failed, 0 skipped** (both scoring sides).
  `TestResults/GameplayRegression/Runs/20261110T072332815Z/results.xml`
  Verifies real source playback with silent, muted, disposable clips; chatter
  reaches its normal gain then fades out, goal levels, whistle/crowd independent
  IDs, pause stopping match sources and selecting music, resume clearing music
  and reactions, and Stadium mute. Restores local preferences and MatchManager.
- Earlier PlayMode pass before strengthening fade/music checks:
  `TestResults/GameplayRegression/Runs/20261110T072027984Z/results.xml`
  (2 passes; these are the same cases, not two additional distinct cases).

Both scenes have seven authored sources, three bound sliders/toggles, and one
shared library with 13 empty pools. Room controls are under PausePanel. Room is
restored, clean, and stopped. Serialized comparison of the designer-owned Stats
subtree found zero changed existing blocks (6 objects / 26 blocks).

`git diff --check` reports Unity serializer trailing whitespace in scene fields;
scenes were saved through the Editor and not rewritten on disk to normalize it.
Source/document diff checks pass. CLI inspections had transient 5-second
main-thread timeouts after reload and while loading Room; subsequent inspections
confirmed completion, clean scenes, and a stopped Editor. No process restart or
GUI automation was used.

Visual/manual UI interaction acceptance remains pending. The edit-mode Game view
screenshot command captured only the camera background, omitting overlay Canvas
UI; it does not establish visual acceptance. No actual listening verification
has occurred; the tests use silent clips and the library has no supplied audio.

## Selected match incidents and revised goal mix

Follow-up: 70/30 stand shares; goals fill the remaining stand budget while Walla
continues and chants fade. Selected duel, foul, shot-outcome, restart, transition,
snapshot-availability and delayed substitution cues are assigned and connected.
Editor compilation is clean. Focused results: 23 EditMode and 96 distinct
PlayMode cases passed, zero failed/skipped; the two snapshot boundary cases
were rerun after a guard update. See `Incidents/TASKS.md` for exact native artifacts,
executed scope and pending listening criteria. No whole suites.
Room is stopped/clean with 16 authored sources and 27 library entries. Audio UI
positions are unchanged. A test-generated em-dash font-cache entry was removed
through the Editor, restoring the previously clean font asset.
