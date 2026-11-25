# Auditioned crowd pool selections

Source: the user listened to the imported recordings and supplied these assignments. These listening decisions supersede the earlier filename/metadata shortlist. Most pool names below are proposed implementation identifiers. Applied runtime assignments are Chatter (WallaBase: 7), Chant (26), Cheer (GoalScored: 2) and Groan (MissedChance: 6). Selected incident/transition pools are now assigned as detailed in GameplayAudio.md. Unselected injury/pass and committed-shot anticipation cues remain pending.

## Updated mix rules

Home/away shares are now 70%/30%. Each stand continuously crossfades its base Walla
with approximately 2 s overlap, at 40–50% of that stand's share. Chants add 30%,
play once, and wait for that stand's next chant slot. Match event reactions add
50% of the relevant stand's share while Walla continues. Goals fill each stand's
remaining budget above Walla and the fading chant, recalculated through crossfades.
This supersedes earlier 100% goal / 60% reaction levels and bed fade-out rules.
An active chant fades out when an event arrives, is discarded and waits for its
stand's next slot. This ambient runtime is now implemented with two Walla sources
per stand and separate chant voices. Chant slots wait 45–90 seconds after finish;
fade-out defaults to 0.35 seconds. A common 0.75 output trim preserves ratios and
leaves headroom. Selected incident hooks are implemented. Fouls use the penalised stand; duels use
both stands. Preparation loops use both stands; substitutions announce after resume.
Injury/pass and committed-shot anticipation decisions remain pending.

| Proposed pool | Clips | Status / use |
| --- | ---: | --- |
| WallaBase | 7 | Walla; selected |
| DuelWon | 2 | Successful tackle / Steal / Nutmeg Win; crop_pending |
| DuelLost | 1 | Lost tackle / Missed Recovery / Missed Steal / Nutmeg Loss; selected |
| WallaAccent | 5 | Walla; confirmed occasional overlays above continuous chatter |
| StandardPass | 1 | Successful Standard Pass?; tentative |
| DefensiveShotRelief | 1 | Missed Shot / Blocked Shot in favor of us; selected |
| Chant | 26 | Chant; selected |
| GoalScored | 2 | Goal Scored; selected |
| MissedChance | 6 | Missed Chance; selected; _03 prepared from 4.4 seconds onward |
| KickOff | 3 | Kick Off; selected |
| HalfTime | 1 | HalfTime; selected |
| FullTime | 1 | Final Whistle; selected |
| InjurySubstitution | 1 | Substitution due to injury; selected |
| FoulReaction | 1 | Penalised stand at the roll establishing the foul; later foul statistics stay silent |
| SetPiecePreparation | 4 | Set Piece Preparations; selected |
| SnapshotAvailable | 1 | Snapshot is available; selected |
| OpponentError | 1 | Off-target of opponent's shot or ball out of bounds for us; selected |
| HomeSubstitution | 4 | Substitution for home team; Sub Home; selected |
| AwaySubstitution | 1 | Substitution for away team; selected |
| DoubleAwaySubstitution | 1 | Double Sub Away; selected |

## Exact file selections

| Pool | File | Seconds | Notes / status |
| --- | --- | ---: | --- |
| WallaBase | `Assets/344SFX_Football_Crowd/Anticipation_Cheers/Audio/Anticipation_Excited_01.wav` | 8.73 |  selected |
| DuelWon | `Assets/344SFX_Football_Crowd/Anticipation_Cheers/Audio/Build_Up_Cheer_Applause_02.wav` | 17.35 | Crop silence in-front ? crop_pending |
| DuelWon | `Assets/344SFX_Football_Crowd/Anticipation_Cheers/Audio/Build_Up_Cheer_Applause_03.wav` | 12.39 | Crop silence in-front ? crop_pending |
| DuelLost | `Assets/344SFX_Football_Crowd/Anticipation_Cheers/Audio/Distant_Excited_Applause_01.wav` | 15.08 |  selected |
| WallaBase | `Assets/344SFX_Football_Crowd/Anticipation_Cheers/Audio/Excited_Applause_01.wav` | 11.00 |  selected |
| WallaAccent | `Assets/344SFX_Football_Crowd/Applause/Audio/Applause_01.wav` | 5.19 | On top selected; occasional overlay |
| WallaAccent | `Assets/344SFX_Football_Crowd/Applause/Audio/Applause_Walla_01.wav` | 9.01 | On Top selected; occasional overlay |
| StandardPass | `Assets/344SFX_Football_Crowd/Applause/Audio/Applause_Walla_03.wav` | 13.30 |  tentative |
| WallaBase | `Assets/344SFX_Football_Crowd/Applause/Audio/Applause_Walla_04.wav` | 9.91 |  selected |
| DefensiveShotRelief | `Assets/344SFX_Football_Crowd/Applause/Audio/Applause_Walla_06.wav` | 6.33 |  selected |
| WallaAccent | `Assets/344SFX_Football_Crowd/Applause/Audio/Applause_Walla_07.wav` | 14.08 | On top selected; occasional overlay |
| WallaAccent | `Assets/344SFX_Football_Crowd/Applause/Audio/Applause_Walla_08.wav` | 9.31 | On Top selected; occasional overlay |
| WallaAccent | `Assets/344SFX_Football_Crowd/Applause/Audio/Applause_Walla_12.wav` | 6.08 | On Top selected; occasional overlay |
| WallaBase | `Assets/344SFX_Football_Crowd/Applause/Audio/Distant_Applause_03.wav` | 8.32 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Applause_Chant_02.wav` | 24.81 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Applause_Singing_01.wav` | 23.83 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Chant_Clap_01.wav` | 15.64 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Chant_Short_01.wav` | 4.64 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Chant_Walla_01.wav` | 9.08 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Chant_Walla_02.wav` | 12.99 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Chant_Walla_03.wav` | 8.94 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Chant_Walla_04.wav` | 9.38 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Distant_Chant_03.wav` | 15.83 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Distant_Chant_04.wav` | 8.96 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Rhythmic_Chant_Clap_01.wav` | 17.75 |  selected |
| Chant | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Rhythmic_Chant_Clap_02.wav` | 21.41 |  selected |
| WallaBase | `Assets/344SFX_Football_Crowd/Chants_Singing/Audio/Singing_Walla_01.wav` | 13.08 |  selected |
| WallaBase | `Assets/344SFX_Football_Crowd/Crowd_Walla/Audio/Lively_Distant_01.wav` | 14.62 |  selected |
| GoalScored | `Assets/344SFX_Football_Crowd/Goal_Celebrations/Audio/Goal_Cheer_Celebrate_01.wav` | 44.61 |  selected |
| GoalScored | `Assets/344SFX_Football_Crowd/Goal_Celebrations/Audio/Goal_Cheer_Celebrate_02.wav` | 40.08 |  selected |
| MissedChance | `Assets/344SFX_Football_Crowd/Groans_Near_Misses/Audio/Groan_Disappointed_01.wav` | 9.08 |  selected |
| MissedChance | `Assets/344SFX_Football_Crowd/Groans_Near_Misses/Audio/Near_Miss_Groan_Applause_04.wav` | 16.55 |  selected |
| MissedChance | `Assets/344SFX_Football_Crowd/Groans_Near_Misses/Audio/Near_Miss_Groan_Applause_05.wav` | 9.73 |  selected |
| MissedChance | `Assets/344SFX_Football_Crowd/Groans_Near_Misses/Audio/Near_Miss_Groan_Applause_06.wav` | 12.19 |  selected |
| MissedChance | `Assets/344SFX_Football_Crowd/Groans_Near_Misses/Audio/Near_Miss_Groan_Applause_07.wav` | 13.59 |  selected |
| MissedChance | `Assets/Audio/Prepared/Near_Miss_Groan_Applause_03_From_4p4.wav` | 16.11 | Keep original 4.4 s to end; selected_prepared; original preserved |
| KickOff | `Assets/344SFX_Football_Crowd/Match_Events/Audio/Referee_Whistle_Applause_01.wav` | 11.50 |  selected |
| HalfTime | `Assets/344SFX_Football_Crowd/Match_Events/Audio/Referee_Whistle_Applause_02.wav` | 16.66 |  selected |
| KickOff | `Assets/344SFX_Football_Crowd/Match_Events/Audio/Referee_Whistle_Chant_Applause_01.wav` | 6.99 |  selected |
| KickOff | `Assets/344SFX_Football_Crowd/Match_Events/Audio/Referee_Whistle_Chant_Applause_02.wav` | 27.09 |  selected |
| FullTime | `Assets/344SFX_Football_Crowd/Match_Events/Audio/Final_Whistle_Applause_01.wav` | 18.35 |  selected |
| InjurySubstitution | `Assets/344SFX_Football_Crowd/Match_Events/Audio/Injured_Player_Stands_Applause_01.wav` | 37.08 |  selected |
| FoulReaction | `Assets/344SFX_Football_Crowd/Shouts_Jeers/Audio/Angry_Shout_Swearing_01.wav` | 13.87 | Foul made; not every tackle; stand attribution pending |
| WallaBase | `Assets/344SFX_Football_Crowd/Shouts_Jeers/Audio/Angry_Groan_Impatient_01.wav` | 33.23 |  selected |
| SetPiecePreparation | `Assets/344SFX_Football_Crowd/Shouts_Jeers/Audio/Angry_Booing_01.wav` | 20.86 | Crossfade loop selected |
| SetPiecePreparation | `Assets/344SFX_Football_Crowd/Shouts_Jeers/Audio/Impatient_Shout_Whistle_01.wav` | 17.19 | Crossfade loop selected |
| SetPiecePreparation | `Assets/344SFX_Football_Crowd/Shouts_Jeers/Audio/Impatient_Shout_Whistle_02.wav` | 15.62 | Crossfade loop selected |
| SetPiecePreparation | `Assets/344SFX_Football_Crowd/Shouts_Jeers/Audio/Impatient_Shout_Whistle_03.wav` | 12.32 | Crossfade loop selected |
| SnapshotAvailable | `Assets/344SFX_Football_Crowd/Shouts_Jeers/Audio/Shout_Urging_Player_Shoot_01.wav` | 5.37 |  selected |
| OpponentError | `Assets/344SFX_Football_Crowd/Shouts_Jeers/Audio/Sarcastic_Shout_Whey_01.wav` | 9.47 |  selected |
| HomeSubstitution | `Assets/344SFX_Football_Crowd/Tannoy_Announcements/Audio/Substitute_Tannoy_Announcement_01.wav` | 18.08 |  selected |
| HomeSubstitution | `Assets/344SFX_Football_Crowd/Tannoy_Announcements/Audio/Substitute_Tannoy_Announcement_02.wav` | 26.80 |  selected |
| AwaySubstitution | `Assets/344SFX_Football_Crowd/Tannoy_Announcements/Audio/Tannoy_Announcement_02.wav` | 12.74 |  selected |
| HomeSubstitution | `Assets/344SFX_Football_Crowd/Tannoy_Announcements/Audio/Tannoy_Announcement_Applause_01.wav` | 16.08 |  selected |
| HomeSubstitution | `Assets/344SFX_Football_Crowd/Tannoy_Announcements/Audio/Tannoy_Announcement_Applause_05.wav` | 13.02 |  selected |
| DoubleAwaySubstitution | `Assets/344SFX_Football_Crowd/Tannoy_Announcements/Audio/Tannoy_Announcement_Long_01.wav` | 28.08 |  selected |

## Resolved folder labels

`Singing_Walla_01.wav` is actually under `Chants_Singing/Audio/`. `Match Events` resolves to `Match_Events`; the tannoy folder is spelled `Tannoy_Announcements`. Every expanded file was found uniquely on disk.

## Prepared YouTube chant additions (2026-11-16)

All 14 additions are assigned to the runtime Chant pool. Each is a 48 kHz,
stereo, 24-bit PCM WAV with 350 ms fade-in and fade-out. The full-video sources
without user-specified ranges use the complete available audio. The music-heavy
`xQfYcqcacfk` source uses the separated vocals stem; its instrumental backing
was removed with UVR-MDX-NET-Inst_HQ_3.

| Prepared asset | YouTube source and range | Duration |
| --- | --- | ---: |
| `Assets/Audio/Prepared/Chant_vkbrTSNdOVA_05m50_to_07m10.wav` | `vkbrTSNdOVA`, 5:50–7:10 | 80 s |
| `Assets/Audio/Prepared/Chant_vkbrTSNdOVA_00m25_to_01m09.wav` | `vkbrTSNdOVA`, 0:25–1:09 | 44 s |
| `Assets/Audio/Prepared/Chant_Ok5Lj1OUmFE_05m40_to_06m20.wav` | `Ok5Lj1OUmFE`, 5:40–6:20 | 40 s |
| `Assets/Audio/Prepared/Chant_TspYx7EaF5A_Full.wav` | `TspYx7EaF5A`, full video | 80 s |
| `Assets/Audio/Prepared/Chant_qWYLGNnBuLo_Full.wav` | `qWYLGNnBuLo`, full video | 119.9 s |
| `Assets/Audio/Prepared/Chant_xQfYcqcacfk_VocalsSeparated.wav` | `xQfYcqcacfk`, full video, vocals stem | 20 s |
| `Assets/Audio/Prepared/Chant_AV46Q76dJGU_Full.wav` | `AV46Q76dJGU`, full Short | 24 s |
| `Assets/Audio/Prepared/Chant_QzOcB7RBR7w_Full.wav` | `QzOcB7RBR7w`, full video | 31.5 s |
| `Assets/Audio/Prepared/Chant_fo0_xqpzzPY_00m08_to_00m26.wav` | `fo0_xqpzzPY`, 0:08–0:26 | 18 s |
| `Assets/Audio/Prepared/Chant_c_cjRBM5pf8_01m46_to_02m25.wav` | `c_cjRBM5pf8`, 1:46–2:25 | 39 s |
| `Assets/Audio/Prepared/Chant_ecgpMCsDJVk_00m00_to_01m00.wav` | `ecgpMCsDJVk`, 0:00–1:00 | 60 s |
| `Assets/Audio/Prepared/Chant_vNf2l9SuHHA_Full.wav` | `vNf2l9SuHHA`, full video | 75 s |
| `Assets/Audio/Prepared/Chant_vUYytZI6Lvs_Full.wav` | `vUYytZI6Lvs`, full video | 89.7 s |
| `Assets/Audio/Prepared/Chant_7defDCYKhV4_00m35_to_01m00.wav` | `7defDCYKhV4`, 0:35 to end | 25.07 s |

The first export pass exposed an FFmpeg output-seek/filter ordering issue: some
files had silent tails or were silent despite full nominal duration. All files
were regenerated with input-side seeking, then checked with `ffprobe`,
`volumedetect`, and `silencedetect` at -45 dB for 250 ms. Every final clip has
nonzero signal and no sustained silent interval.

## Pending decisions

- Walla “On top” is confirmed as occasional ambient overlays over continuous base chatter, yielding to major reactions. Frequency and gains remain tunable implementation choices.
- Angry_Shout_Swearing_01 is confirmed for when a foul is made; participating stand attribution remains to be finalized.
- Near_Miss_Groan_Applause_03 is confirmed for 4.4 seconds to end and has been prepared/imported as a separate WAV.
- Successful Standard Pass remains tentative; do not enable a sound on every routine pass by default.
- SnapshotAvailable is a separate trigger from committed-shot anticipation; define once-per-availability-window delivery before wiring it.
- No committed-shot or defensive danger-anticipation clip has been selected in this table. Leave those selections open rather than assigning a clip by its supplier folder name.
- Fouls use the penalised stand at its foul roll. Cards use Sarcastic_Shout_Whey_01 for the fouled stand. Initial injury incidents still need a pool decision; the selected injury applause is for substitution due to injury.
- Set-piece preparation is a crossfading-loop state. Specify participating stands and return-to-chatter behavior before wiring.
- Tannoy is stadium-wide presentation; decide its relation to stand applause and prevent a double substitution from stacking two announcements.

## Crop measurements

Silence detection (-45 dB for at least 0.15 s) found approximately 0.445 s of leading quiet in Build_Up_Cheer_Applause_02 and 0.200 s in _03. These support a leading-trim review; exact offsets should retain a small natural lead-in. These two duel crops are still pending.

The user selected an exact start of 4.4 seconds for Near_Miss_Groan_Applause_03, irrespective of silence detection. Created `Assets/Audio/Prepared/Near_Miss_Groan_Applause_03_From_4p4.wav`, retaining the rest of the recording: 16.109021 seconds / 773,233 frames at 48 kHz, stereo, 24-bit PCM. Verified the output with ffprobe and the Unity import. Original files remain intact. This prepared clip is now assigned in the runtime Groan pool.

## Next batches

1. Continuous random Walla crossfades and occasional chants are implemented and focused tests pass. Actual listening acceptance is next.
2. Add the five ambient accents with bounded frequency and priority; their overlay semantics are confirmed.
3. Prepare cropped derivatives or playback segments, then integrate goals, chances and duel outcomes with explicit stand attribution.
4. Add selected set-piece, availability, match transition and substitution events in focused batches. Keep unrelated separate cues deferred.

All current Audio/Stadium/Game/Music labels and checkbox placements remain untouched. Preserve original recordings. New behavior requires focused feature tests and listening acceptance; no whole gameplay suites.

Goal timing listening follow-up: Goal_Cheer_Celebrate_01 played with
Near_Miss_Groan_Applause_06 in the live home goal. Its main roar rises roughly
1.8–2.0 s into the original, compared with the groan around 0.8–1.0 s. Assigned
`Assets/Audio/Prepared/Goal_Cheer_Celebrate_01_From_1p0.wav`, retaining 1.0 s to
end (43.607292 s; stereo 48 kHz, 24-bit). Original preserved. Imported and saved
into the Cheer pool through the live Editor, leaving Play Mode running. No code
reload or tests were required for this asset-only edit; audible alignment awaits
the next selection of this clip. Goal_Cheer_Celebrate_02 remains unchanged.
