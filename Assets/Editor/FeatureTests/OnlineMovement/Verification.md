# Online movement

- [x] Typed start, player selection, destination and team-phase completion.
- [x] Automatic movement after a long pass, both attacking directions.
- [x] Reject wrong owners, invalid/occupied targets, duplicate moves and guest mutation.
- [x] Freeze/resume real movement across connection hold.
- [x] Focused OnlineMovement PlayMode execution and snapshot publication.
- [x] Version extended contract as ground-wire-v6 / binary CA06.
- [x] Rebuild/check updated Windows export.

Dribbling, tackles/nutmegs, goalkeeper interactions, movement rolls and other
interruption choices are the next feature slices. Paired checks deferred.

Compilation separately clean. Focused category OnlineMovement: **6 EditMode +
7 PlayMode passed, zero failed/skipped**. Native XML:
`TestResults/GameplayRegression/Runs/20261113T175353598Z/results.xml` (EditMode),
`TestResults/GameplayRegression/Runs/20261113T175755159Z/results.xml` (PlayMode).
PlayMode fixtures are OnlineMovementPlayModeTests (5) and
OnlineRoomMovementPlayModeTests (2). Real Room controls select and move a player;
owned deterministic boards cover team-phase transitions, connection holds,
invalid/occupied destinations, wrong owners, duplicates, guest guards and
long-pass-to-movement continuation. Immutable snapshots include selected player
and movement status; validated animation frames update authored display objects
without touching authoritative occupancy or rules.

Existing rules are preserved: a ball still in space automatically starts another
movement after the current phase finishes. Long balls entering a Final Third
also open its separate phase; the movement continuation fixture deliberately
lands outside it. Final Third is the next integration dependency, not bypassed.

Affected focused checks: OnlineLongPass PlayMode 16/16 (175431637Z),
OnlineGroundPassNetwork EditMode 13/13 (175537475Z),
OnlineManualControls PlayMode 7/7 (175552440Z), OnlineSharedPause EditMode 11/11
(175821053Z), all with prefix `TestResults/GameplayRegression/Runs/20261113T`
and suffix `/results.xml`. Zero failed/skipped. No full suites ran.
Broader user-run check if needed: category DecisionTiming under
`Assets/Tests/PlayMode/Features/DecisionTiming/` for other movement interruptions;
legacy movement fixtures under `Assets/Tests/PlayMode/` for offline movement.

Windows Room v6 export `build_b5b794829160` succeeded (eight warnings and the
known missing-vswhere.exe diagnostic; no script compile errors). Packaged local
ground/pause/recovery and snapshot-codec check passed:
`TestResults/GameplayRegression/Runs/20261113T180039013Z-Room-local/result.json`.
Build report is retained alongside. This checks the rebuilt application but is
not paired native movement acceptance; movement itself has the focused Room
framework evidence above.
