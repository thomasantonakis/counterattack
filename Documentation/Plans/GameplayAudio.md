# Gameplay audio

Status: agreed foundation; implementation started on `feature/gameplay-audio`.

## Current specification decisions (2026-11-10)

UI/dice follow-up: Game sounds controls hovers, selections (including checkboxes),
advance/back navigation and dice. Stadium sounds controls Walla, chants, match
reactions, celebrations, whistles and tannoy. Pause and Game Settings controls
have authored hover/selection feedback; return callbacks emit Back. Local mute
is authoritative, including when clicking the Game sounds checkbox itself.
A 250 ms original procedural dice rattle is now assigned. Accepted match roll
requests emit a uniquely identified incident before resolution for both normal
and rigged dice; pre-match roll-offs use the existing menu voice. Existing .1 s
cooldown prevents rapid stacking. No RNG, rules, timing or input changes. Older
notes deferring interface/dice cues below describe earlier implementation batches.
Current UI details and native validation: UiAudio.md and the Dice feature TASKS.md.


Snapshot/international follow-up: movement-owned snapshot opportunities must play
the urging cue as well as ShotManager opportunities. Use 80% of its stand share with +6 dB recording compensation (about +10 dB
relative to the previous 50% cue), keep its one-shot playing through subsequent frames and GK movement,
and avoid repeating it on each dribble pace. A lower-priority cue blocked by an
existing reaction may wait only while its snapshot opportunity remains available;
do not play a stale cue after that opportunity ends. International matches use
50/50 stand shares, selected by existing GameSettings.matchType = International;
other fixtures retain 70/30. Apply the split consistently to Walla, chants,
reactions, preparation and goal headroom. Local mute settings remain local.

- Chatter must progress continuously through randomly selected recordings,
  crossfading the outgoing clip into the next with approximately two seconds of
  overlap at each join (not a silent gap). Each stand's base Walla stays at
  40–50% of its own volume share. A stand must not
  retain one clip looping forever. Exclude the outgoing recording when an
  alternative exists. The policy for very small pools and
  avoiding the other stand's current clip will be finalized against the actual
  imported inventory. Preserve each stand's gain during a crossfade rather than
  adding two full-volume ambience layers. Implemented with two authored Walla
  sources per stand, using the seven auditioned base Walla recordings. Each new
  clip gets a random 40–50% intensity. The outgoing clip is excluded whenever
  another recording exists; the other stand's active clips are avoided when the
  pool permits, relaxing that preference for small pools to keep sound continuous.
- The 12 auditioned chants play occasionally, once per slot, with a separate
  schedule for each stand. They do not loop continuously or stack with another
  chant from the same stand. Each chant adds 30% of that stand's volume share.
  When an incident arrives, active chants fade out. They do not resume after the
  reaction; each stand waits for its next scheduled chant slot.
- Committing to a goalscoring situation (snapshot, shot or header at goal) should
  produce anticipation from the performing team's supporters. Do not trigger it
  on merely selecting/previewing an action. Map each real commitment point during
  implementation without changing action timing or ownership. The defending stand
  continues normal chatter unless the imported inventory offers a distinct danger
  anticipation recording; do not reuse an attacking goal-anticipation sound for it.
  A shared anticipation pool may cover all three actions if clips suit it; do not
  require three distinct pools without an audible reason.
- Ordinary match event reactions add 50% of each
  participating stand's own volume share, independent of the base Walla gain.
  Walla continues underneath. This supersedes the earlier 100% goal / 60% ordinary
  reaction levels and the earlier policy of fading out chatter for an incident.
  Goals instead fill each stand's remaining budget above Walla and any fading
  chant, continuously recalculated through crossfades. Both stands reach their
  full allocated crowd gain; the shared 0.75 output trim remains in effect.
  Confirmed goals use the selected cheers for the scoring stand and the agreed
  Missed Chance pool for the conceding stand (confirmed 2026-11-10). Outcome sounds must replace any
  active anticipation rather than being dropped behind it.
- Fouls use the penalised stand's FoulReaction at the roll that establishes the
  foul. A subsequent card adds Sarcastic_Shout_Whey_01 (OpponentError) for the
  fouled stand at 50% of its share; successful no-card checks are silent. The
  penalised stand's earlier foul reaction can continue. Injury incident reactions
  still need a selected pool; injury-substitution applause is already assigned.
- Substitutions should receive applause if suitable non-chanting clapping exists.
  Stand participation/intensity can be finalized after inspecting that pool.
- The crowd pack is now imported at `Assets/344SFX_Football_Crowd/`.
  Filename/metadata inventory is complete. The user has now listened to all clips
  and supplied 56 unique selections, expanded from ranges and checked on disk.
  `GameplayAudioPoolSelections.md` and `.json` preserve the exact user assignments,
  notes, actual paths and provisional pool names. These audition choices supersede
  the earlier filename/metadata shortlist. Chatter (7), Chant (12), Cheer (2) and
  Groan (6, including the prepared crop) are now assigned in the runtime library.
  Continuous Walla and occasional chants are implemented; game-event hooks and
  the other proposed pool types remain pending.
  The selections comprise seven base Walla clips, five confirmed occasional Walla
  overlays above continuous chatter, 12 chants, and incident/transition/substitution
  cues. Angry_Shout_Swearing_01 plays when a foul is made; stand attribution remains
  to be finalized. Near_Miss_Groan_Applause_03 is prepared from exactly 4.4 seconds
  to the end as a separate WAV under `Assets/Audio/Prepared/`, with the original
  preserved. Standard Pass remains tentative; two duel leading-trim notes remain open.
  Snapshot availability is a newly selected cue distinct from committed-shot
  anticipation. No committed-shot anticipation clip was selected in this table;
  do not assign it based on a supplier folder name. Selected match whistles and
  tannoy announcements are now scoped for their listed occasions; unrelated
  interface/dice cues remain deferred.
  See `GameplayAudioInventory.md` for a shortlist and `GameplayAudioInventory.json`
  for all 153 WAV paths, durations, formats and embedded descriptions.
- Separate cues (whistles, dice, interface, swishes) are deferred for now. Keep
  their earlier requirements as future scope, without further integration work.
- The designer owns the current Audio/Stadium/Game/Music labels and checkboxes.
  Do not reposition them or rerun layout authoring during sound work.

User listening feedback on the placeholder player: both stands were audible at
different volumes, chatter looped continuously, pause stopped it, and resume
restarted the recordings acceptably. This validates those observed behaviors,
not final crowd balance, seamless assets or the proposed continuous crossfades.

## Experience and constraints

Independent home and away stands provide chatter, occasional chants and incident
reactions. Club/other fixtures allocate 70% of the crowd mix to home supporters and 30%
to away supporters. International fixtures allocate 50% to each stand.
The updated layered mix is Walla at 40–50% of each stand's share, occasional
one-shot chants adding 30%, and ordinary match event sounds adding 50%; goals fill the remaining stand budget. These are linear
gain starting points, not measured perceived loudness. Walla continues underneath
event reactions. Approximately two-second crossfades overlap consecutive Walla
clips without a silent gap; the fading pair must not double the intended bed gain.
Active chants fade out when an incident arrives; Walla continues. Pausing stops stadium
and match effects, discards interrupted reactions, and switches to pause music.
Resuming restarts chatter, never interrupted reactions. Music assets/style remain
to be discussed; silence is intentional until a clip is assigned.

Implementation defaults: two-second Walla overlap, 0.35-second startup and chant
fade-out, and independently randomized chant slots 45–90 seconds after the previous
chant finishes or is discarded. A uniform `CrowdOutputGain = 0.75` trim preserves
the requested relative mix while leaving headroom for chant/event fade overlaps.
These controls all live in GameplayAudioConfig.cs and require listening acceptance.

Sound observes confirmed events; it never advances gameplay, rolls dice, takes
input ownership, changes AI, or waits before allowing gameplay continuation.
Use independent audio randomness, never Unity's gameplay random stream. Future
clients can submit stable incident IDs to suppress duplicate playback locally;
there is no networking implementation. Volume/mute preferences stay local and
must never enter match saves or shared game settings.

## Player settings and code configuration

Three independently saved On/Off checkboxes, using the deck-inclusion checkbox
design. No player-facing sliders. Available in the Main Menu panel (at the
designer's bottom-right placement) and Room → Pause → Game Settings → Audio:

Room uses one horizontal row: `Audio   Stadium [ ]   Game [ ]   Music [ ]`.
The Main Menu retains its bottom-right panel layout.

| Category | Includes |
| --- | --- |
| Stadium | Chatter, chants, crowd reactions, referee whistles |
| Game | Dice, invalid clicks, pause/menu feedback, Match Stats swishes |
| Music | Pause music; other music only if agreed later |

`Assets/Scripts/Audio/GameplayAudioConfig.cs` contains mix shares, intensities,
fade times, cooldowns, priorities and the reaction matrix. Clip assignments live
in a separate editable `GameplayAudioLibrary` asset. No clips are embedded in
code. Category volumes are configured by `GameplayAudioConfig.CategoryVolume`;
only On/Off preferences are saved. Old slider preference values are ignored so
an enabled checkbox cannot remain silently muted by a removed slider.
UI objects must be authored in edit mode; never touch
`Canvas/MatchStatsUI/Stats` layout. Mixer assets/groups do not exist yet; direct
source gain provides these categories without middleware. If mixers are later
needed, discuss the concrete need and create them manually in the Editor.

## Event-to-sound matrix

Each stand independently chooses a random recording from its assigned pool.
Avoid immediate clip repeats when alternatives exist. Stand gain = supporter
share × event intensity × configured Stadium gain (zero when locally disabled).

| Event | Home stand | Away stand | Other sound | Priority/status |
| --- | --- | --- | --- | --- |
| Normal play | Walla, 40–50% of home share | Walla, 40–50% of away share | Approximately 2 s random crossfades | Implemented; listening pending |
| Occasional chant | One-shot chant adds 30% of home share in home slot | One-shot chant adds 30% of away share in away slot | Base Walla continues; chant fades for event | Implemented independent schedules; listening pending |
| Home goal | Cheer fills remaining home share | Missed Chance groan fills remaining away share | Walla continues; chants fade | Connected to confirmed score; listening pending |
| Away goal | Missed Chance groan fills remaining home share | Cheer fills remaining away share | Walla continues; chants fade | Connected to confirmed score; listening pending |
| Home commits to snapshot/shot/header at goal | Attacking anticipation | Normal chatter; distinct danger anticipation only if suitable assets exist | Separate cues deferred | Commitment trigger agreed; implementation mapping/duration pending |
| Away commits to snapshot/shot/header at goal | Normal chatter; distinct danger anticipation only if suitable assets exist | Attacking anticipation | Separate cues deferred | Commitment trigger agreed; implementation mapping/duration pending |
| Off-target shot | Shooter: Missed Chance; opponent: Opponent Error | Same routing by credited team | +50% each; Walla continues | Connected |
| Outfield block / save | Shooter: Missed Chance; defending stand: Defensive Shot Relief | Same routing by credited team | +50% each; paired stat records produce one reaction | Connected |
| Tackle / nutmeg / steal | Winner: DuelWon; loser: DuelLost | Same routing by result | +50% each; misses reverse the pair | Connected |
| Foul | Penalised stand: FoulReaction | Penalised stand only | +50%; Walla continues | Connected |
| Card shown after a foul | Fouled stand: OpponentError (Sarcastic_Shout_Whey_01) | Same routing by fouled team | +50%; once per card decision; no cue for successful no-card check | Connected; listening pending |
| Injuries / ordinary pass | No cue | No cue | Pool decisions deferred by user | Pending |
| Substitution | HomeSubstitution tannoy | AwaySubstitution / DoubleAwaySubstitution tannoy | Once after resume; injured outgoing player adds InjurySubstitution applause for their stand | Connected |
| Kick-off / half-time / full-time | Embedded crowd in selected recording | One stadium recording, not duplicated per stand | KickOff / HalfTime / FullTime through independent whistle source at 50% Stadium gain | Connected |
| Set-piece preparation | SetPiecePreparation crossfade loop at +50% | SetPiecePreparation crossfade loop at +50% | Both stands; base Walla continues, chants fade; stop on execution | Connected |
| Snapshot available | Urging clip for possessing stand once per availability edge | Same routing by team | Lower priority than outcomes; not shot-commit anticipation | Connected |
| Inaccurate ball out | OpponentError for restart beneficiary | Same routing by award | +50%; only inaccuracy, not every deflection | Connected |
| Actual dice roll | No crowd cue by default | No crowd cue by default | Random dice pool; bounded overlap | Implemented original DiceRoll.wav, Game category |
| Invalid click | None | None | Brief invalid cue; cooldown | Agreed cue; validation hooks pending |
| Accepted pause/open or resume | Stop/restart chatter | Stop/restart chatter | Pause/resume cue; music while paused | Agreed |
| Menu item hover | None | None | Quiet hover cue; cooldown | Agreed |
| Match Stats show/hide | Crowd unchanged | Crowd unchanged | Swish pool | Agreed |
| Selection/ordinary confirmation/movement/tackle contact | None assigned | None assigned | No assumed cue | Needs explicit decision |

## Overlap and priorities

Foul timing follow-up: the penalised stand reacts when its tackler's roll
establishes the foul (natural 1 normally, 1–2 when the actual tackle is dangerous),
without waiting for the dribbler's roll or the result panel. Recovery-foul cues
likewise precede the single-roll presentation wait. Keep actual foul rules and
booking/injury/advantage timing unchanged. Later GroundDuelWon/foul statistics
must not publish a second audible foul. Injury incidents remain unassigned;
the previously implemented injury-substitution applause is a distinct cue.

Continuous crossfading Walla is the bed for each stand. Chants are an additional
one-shot layer at 30% of stand share. Ordinary match events add a reaction
layer at 50% of stand share, regardless of Walla level. Goals instead fill the
remaining stand gain above Walla and the fading chant, recalculated every tick.
Reserve output headroom
and verify the sum by listening/meters; do not change the requested relative mix
through an undocumented normalization or ducking rule. Walla must not drop out
for an event under the updated specification.

One reaction channel per stand. Priorities: snapshot urging 5, duels/ordinary
incidents 10, fouls 30, shot outcomes 40, cards 50, goals 100. Goals may replace a lower-priority reaction;
lower/equal-priority reactions are dropped while a reaction is active, never
queued for stale playback. Whistles have their own source. Game effects have a
bounded channel and per-cue cooldowns. Active chants fade out for an incident,
are discarded rather than resumed, and wait for their next per-stand slot.
Ambient Walla accents yield to major
reactions as already agreed. Missing
pools are silent. Pause clears transient sounds; UI feedback remains available.
Initial timings/limits are tunable code defaults, subject to listening review.

## Asset inventory

Initial inspection: no AudioClips, AudioSources, AudioMixers or playback code.
Room has one enabled AudioListener on Main Camera. Existing local preference
patterns use PlayerPrefs. Existing match banner notifications lack stable team
identity for all incidents, so do not infer a stand from displayed player names.

Candidate: [344 SFX Football Crowd](https://assetstore.unity.com/packages/audio/sound-fx/football-crowd-417526).
Imported and inspected: 153 WAVs in nine categories and nine supplier AudioSource
prefabs. All WAVs are stereo, 48 kHz, 24-bit PCM; imported defaults are PCM /
Decompress On Load. No supplier mixers were found. See `GameplayAudioInventory.md`
and `.json` for the category counts, durations and candidate review list. Filenames
and embedded tags were inspected, followed by the user's full listening review.
See `GameplayAudioPoolSelections.md` for that later authoritative selection.
Final loop/crossfade suitability and mix balancing remain unverified. Some recordings combine anticipation, reactions, whistles or chanting;
pool assignment requires listening and may require non-destructive clip segments.
Need neutral chatter, chants, cheers, groans and other agreed reaction pools;
separate whistle, interface, dice and swish recordings are also needed.
User-supplied placeholders: `Assets/Audio/Placeholders/thomas/chatter_01.m4a`
and `chatter_02.aifc`, converted to sibling `.wav` files with originals preserved.
The two WAVs are mono, 16-bit PCM at 16 kHz, 11.004 and 6.72 seconds respectively.
These were initially assigned to Chatter for the first listening test. The active
Chatter pool now uses the seven auditioned purchased clips; the placeholders are
retained on disk. Listening/balancing of the new continuous mix is pending.
Pause music is deferred. Do not buy
assets/services, install generators or add middleware without discussion.

Current inventory (2026-11-10): Room contains an authored GameplayAudio rig with
16 sources (two Walla voices, a chant, a reaction and two preparation
voices per stand, plus whistle, announcements, Game effects and music). Main Menu retains its seven-source non-stadium rig.
Both share `Assets/Audio/GameplayAudioLibrary.asset`, with 27 pool entries:
Chatter 7, Chant 12, Cheer 2, Groan 6 and 14 selected incident/transition pools;
nine original separate-cue/music entries are empty. Main Menu
and Room's Game Settings card contain the three checkboxes. Menu hover is wired;
Room pause is observed from MatchManager.IsPauseMenuOpen. Confirmed goals publish
one live presentation notification from MatchManager.AddGoal, including own goals
using the beneficiary team. The director consumes incident IDs once, observes local
mute/pause and never replays restored scorer history. Notification subscribers are
isolated so an audio exception cannot abort scoring. IDs can be retained for future
client delivery; no networking is implemented. Selected incident, transition, preparation, snapshot-availability and substitution
hooks are connected. Committed-shot anticipation lacks a selected suitable pool;
injuries/ordinary pass and separate dice/invalid-click/Stats cues remain pending. Card reactions now cheer the fouled stand using OpponentError. The converted placeholders remain on disk.

## Implementation batches

1. Foundation: code matrix, local settings, clip library, duplicate suppression,
   independent clip selection, bounded playback and pause handling. Focused tests.
2. Author an inspectable audio rig and checkbox settings UI; expose the three
   controls in Main Menu and Room Game Settings, without designer-owned stats
   layout changes. Preserve the designer's Main Menu panel placement.
3. Import chosen assets, audition/categorize clips, prepare usable ambience beds,
   and tune gains/fades. Record the actual clip inventory and provenance here.
   Import/inventory and user audition selection are complete. Preserve the current
   originals; implement the selections in the batches listed in
   `GameplayAudioPoolSelections.md`, resolving its pending decisions as needed.
4. Implement continuous random chatter crossfades, then connect anticipation,
   confirmed goals, fouls, cards and injuries in focused batches. Separate cues
   are deferred. Agree the remaining matrix before enabling its reactions. Add tests
   for each changed event path, including team attribution and duplicate delivery.
5. Listening pass and release tuning; music remains deferred until agreed.

## Verification criteria

- Open Editor compiles cleanly; report compilation separately from test results.
- Dedicated GameplayAudio fixtures only; retain native XML and progress under
  `TestResults/GameplayRegression/Runs/`. No full gameplay suites for this work.
- Verify home/away gains for both goal directions, per-category volume/mute,
  preference persistence, independent random selection/no immediate repeats,
  duplicate incident suppression, priority and cooldown behavior.
- Verify pause stops transients and resume cannot replay them, missing clips are
  safe, and all audio objects/UI exist in edit mode.
- For the planned chatter change, verify bounded source count, crossfade gain,
  remaining-pool selection for 1/2/many clips, independent stands, mute/pause
  interruption and restoration, and clean transitions around reactions.
- For anticipation, verify real action commitment/cancellation/outcome paths
  for snapshots, shots and headers, both teams, and no duplicate or stale cues.
- Verify each event integration through its real entry point; sounds cannot
  change gameplay state/timing, input ownership or gameplay random state.
- Listening verification is separate and pending supplied assets: seamless chatter,
  independent stands, natural fades, varied reactions, intelligible whistles,
  no clipping/fatigue during repeated rolls and hovers, and suitable pause music.
- UI acceptance: all three checkboxes in both locations, no sliders, values persist
  across scene changes/restarts; no layout mutation under MatchStatsUI/Stats.

Execution results and outstanding acceptance are tracked in
`Assets/Editor/FeatureTests/GameplayAudio/TASKS.md`.

Latest ambient-mix verification: clean Editor compilation, 23 EditMode passes and 6
PlayMode passes, zero failures/skips. These are focused audio tests with silent
owned clips, not a full
gameplay regression or listening result. The remaining batches above are still
required before release.

Selected match-event follow-up: 70/30 shares and goal headroom implemented;
74 incident cases, 16 goal cases and six ambience cases passed, plus 23 EditMode
checks. Two snapshot cases were rerun after adding break/setup guards. No failures
or skips. Native artifacts, precise integration limits and actual-clip listening
checklist: `Assets/Editor/FeatureTests/GameplayAudio/Incidents/TASKS.md`.

Goal listening follow-up: the latest live home goal used Goal_Cheer_Celebrate_01
with Near_Miss_Groan_Applause_06. The roar lagged by about a second. A prepared
copy now retains the celebration from exactly 1.0 seconds onward, preserving the
original 48 kHz stereo 24-bit PCM samples. It is assigned in the Cheer pool;
Goal_Cheer_Celebrate_02 remains unchanged. Actual alignment awaits listening to
the prepared selection. The user reported substitution audio worked well.

Foul/card timing follow-up verification: clean Editor compilation, 48 new and
74 affected audio PlayMode cases passed (zero failures/skips in the final runs).
Reproduction, fixture correction, native artifacts and remaining listening checks:
`Assets/Editor/FeatureTests/GameplayAudio/FoulTiming/TASKS.md`. No whole suites ran.

Snapshot playback details: observe MovementPhaseManager.isWaitingForSnapshotDecision
and ShotManager offers. The started one-shot continues through flag changes and
GK movement. An unplayed offer waits for GK/player/Final Third input ownership and
a free foreground voice, but is discarded if the opportunity disappears, is muted
or is paused. A 15-second per-stand repeat cooldown prevents pace-by-pace shouts.
The original recording has -30 dB mean / -12.5 dB peak; the +6 dB compensation is
code-configured in GameplayAudioConfig, with the original asset unchanged.
International shares use matchType, not the includeInternationals deck checkbox.

Snapshot/international verification: clean Editor compilation; 13 new focused
PlayMode cases and two existing ShotManager offer cases passed, no failures/skips.
See `Assets/Editor/FeatureTests/GameplayAudio/SnapshotAndMix/TASKS.md` for native
artifacts and listening criteria. No UI or gameplay-rule changes.

Final recovery coverage: confirmed standard/FTP/long/headed-pass interceptions
now use DuelWon for the recovering stand and DuelLost for the other stand.
Defensive loose/header pickups also react, excluding teammate collections and
paired tackle/goalkeeper-save recovery records. Rules and continuation remain
unchanged. The complete purchased pack is SHA-256 verified in the user archive
outside the project; only referenced crowd recordings and prepared clips are
retained for release. Personal audition recordings remain local and ignored.

Final recovery batch: 41 focused PlayMode cases passed, no failures/skips; Editor
compilation clean. Used audio is curated under Assets/Audio/Crowd and Prepared
with all GUIDs/references preserved. Full pack remains in the verified external
archive; unused package content is removed. Recovery/TASKS.md retains execution
evidence and listening limits.
