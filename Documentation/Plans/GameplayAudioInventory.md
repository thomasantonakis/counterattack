# Imported football crowd inventory

Root: `Assets/344SFX_Football_Crowd/`

153 WAVs; all stereo, 48 kHz, 24-bit PCM. Nine supplier prefabs each contain one AudioSource; no supplier mixer assets were found. Unity currently imports all 153 as PCM / Decompress On Load. No import settings or playback assignments were changed in this inspection.

This is a filename, duration and embedded-metadata review, not a listening verdict. Supplier comments often describe multiple events, so clip names alone are not sufficient to assign clean anticipation, goals or neutral ambience.

## Categories

| Folder | WAVs | Duration range |
| --- | ---: | ---: |
| Anticipation_Cheers | 8 | 5.21–29.38 s |
| Applause | 21 | 4.91–57.20 s |
| Chants_Singing | 26 | 4.64–31.35 s |
| Crowd_Walla | 6 | 8.78–33.76 s |
| Goal_Celebrations | 6 | 8.90–44.61 s |
| Groans_Near_Misses | 31 | 2.27–29.54 s |
| Match_Events | 10 | 6.99–37.08 s |
| Shouts_Jeers | 24 | 5.08–33.23 s |
| Tannoy_Announcements | 21 | 8.12–95.84 s |

## First listening shortlist

Listen in the Project window: select the WAV, then click Play in the Inspector's bottom preview panel. Ordinary Play Mode is unnecessary.

| Candidate | Seconds | Review purpose |
| --- | ---: | --- |
| `Crowd_Walla/Audio/General_Walla_01.wav` | 9.83 | Steady chatter candidate; check embedded kicks/calls and usable transition points. |
| `Crowd_Walla/Audio/General_Walla_02.wav` | 13.26 | Steady chatter candidate; compare texture and level with 01. |
| `Crowd_Walla/Audio/General_Walla_03.wav` | 33.76 | Longest named general walla; check whether it remains neutral throughout. |
| `Crowd_Walla/Audio/Lively_Distant_01.wav` | 14.62 | Possible livelier ambience; decide whether it belongs in regular chatter. |
| `Anticipation_Cheers/Audio/Anticipation_Excited_01.wav` | 8.73 | Attacking anticipation candidate; tags mention multiple event types. |
| `Anticipation_Cheers/Audio/Anticipation_Excited_02.wav` | 5.21 | Short anticipation candidate; tags also describe a missed goal, so inspect the ending. |
| `Goal_Celebrations/Audio/Goal_Cheer_Celebrate_01.wav` | 44.61 | Confirmed-goal candidate; identify the cheer onset and useful duration. |
| `Goal_Celebrations/Audio/Goal_Cheer_Celebrate_02.wav` | 40.08 | Confirmed-goal alternative; identify the cheer onset and useful duration. |
| `Goal_Celebrations/Audio/Goal_Cheer_Applause_01.wav` | 33.10 | Goal plus applause candidate; check for embedded whistles/chants. |
| `Goal_Celebrations/Audio/Goal_Cheer_Applause_02.wav` | 16.70 | Goal plus applause alternative; check for embedded whistles/chants. |
| `Applause/Audio/Applause_01.wav` | 5.19 | Substitution applause candidate; confirm it has no chanting. |
| `Applause/Audio/Applause_02.wav` | 7.89 | Substitution applause alternative; confirm it has no chanting. |
| `Applause/Audio/Distant_Applause_01.wav` | 6.15 | More distant substitution applause candidate. |
| `Applause/Audio/Distant_Applause_02.wav` | 4.91 | More distant substitution applause alternative. |

## Further pool decisions

- `Goal_Cheer_Offside_01.wav` is not an automatic confirmed-goal selection: review separately for a disallowed-goal reaction.
- Build-up recordings may contain both anticipation and outcomes; select usable segments rather than allowing unrelated outcomes to sound during a committed attack.
- Fouls have named candidates under Shouts_Jeers, including Foul_Angry_Reaction and Foul_Angry_Reaction_Referee. Review what is actually audible before team/intensity mapping.
- Injury applause is present by name in Match_Events. Injured_Player_Applause_01 explicitly includes a chant in its metadata; it is not automatically suitable for the non-chanting substitution pool.
- No dedicated card or defensive danger-anticipation pool is established by this inventory. Determine suitability through listening; leave absent cues silent.
- Check chants for neutral words and recognizable team references before enabling random gameplay chants.
- Whistles, announcements and other separate cues remain deferred. Mixed crowd/whistle recordings must be identified during listening.

## Next implementation batch

Choose usable chatter recordings first, prepare non-destructive segments if needed, then implement continuous random crossfades between them. Keep the current placeholder assignments until the replacement selection is settled. Crossfade and event integration tests will be executed only for their own focused batches. All UI label and checkbox placements remain designer-owned.

Full per-file paths, durations, format details and embedded tags are in `GameplayAudioInventory.json`.

Release curation: this is the original imported-pack inventory. Original paths
now describe the external archive layout; runtime clips are curated under
Assets/Audio/Crowd and Assets/Audio/Prepared. Current runtime paths and archive
provenance are in GameplayAudioPoolSelections.json. Unused package prefabs and
recordings are excluded from the project and Git.
