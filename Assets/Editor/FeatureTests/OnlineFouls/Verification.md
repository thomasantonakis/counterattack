# Online fouls

- [x] Real steal foul, owned leniency/injury rolls and advantage/restart choice.
- [x] Both directions, bookings, injury pace, deadlines, holds, stale commands, guest rejection and cancellation.
- [x] Second yellow sends defender off and forces the awarded restart.
- [x] Register each case for deferred Mac execution.

Compilation separately clean. Category `OnlineFouls`, fixture
`OnlineFoulPlayModeTests`: **16 passed**, zero failed/skipped, at
`TestResults/GameplayRegression/Runs/20261114T152353072Z/results.xml`.
Initial 14 missing-decision reproductions: `20261114T152016781Z/results.xml`;
first 14 passes: `20261114T152223851Z/results.xml`.

Tests enter through actual dribble/decline/steal, assert roll actor order,
booking/injury status, published team totals, remaining pace, real advantage
continuation and awarded free-kick preparation. No remote dice overrides or raw
keys. Epoch checks prevent a cancelled foul reopening injury or choice flags.
Mac execution remains deferred; every parameter case has its own manifest row.

Free-kick preparation/execution commands, double injury/substitution and penalty
preparation still need their own online integration. This is no full-match or
paired acceptance claim. No complete suite ran.

Affected `LegacyFoulPlayModeTests`: 5 passed, zero failed/skipped,
`Runs/20261114T152445990Z/results.xml`.
