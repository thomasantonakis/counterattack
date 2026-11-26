# Stationary online defensive movement

- [x] Reproduce outfield selection deadlock and omitted keeper movement destinations.
- [x] Publish the existing stationary foot tackle alongside ordinary legal destinations.
- [x] Preserve the keeper's dive/stand choices and allow movement away instead.
- [x] Reuse existing move, duel, bookkeeping and continuation paths.
- [x] Reject distant tackles, wrong owner, guest authority and connection-held commands.
- [x] Finish focused native runs and Mac registration; queue grouped export refresh.

Full Room catalog is v16, wire remains v7. There are no new action IDs: existing
duel.tackle and movement.target commands are now published at the missing legal
boundary. Both full-game clients must rebuild. Ordinary stationary outfield
selection offers tackle or movement, matching the offline source; it does not
invent a stand choice. Outside-box keepers retain dive/foot/stand while adding
their already-highlighted normal movement destinations.

The native fixture selects actual players through movement.start, attacking
forfeit and defensive player selection. It uses the manager's real adjacent
selection flags and legal highlights; no expected choice flags are injected.
Tackle cases execute both actual rolls, assert winner/statistics and continue
through real reposition forfeit. Move cases verify actual destination, consumed
mover, no dice and unchanged possession. Both teams/directions are parameterized.

Compilation: clean through the live Editor, zero C# errors. Own-feature native
results: 16 passed, zero failed/skipped, OnlineStationaryDefenderPlayModeTests at
TestResults/GameplayRegression/Runs/20261116T185807545Z/results.xml. The earlier
12-case red reproduction is retained at 20261116T185455814Z; the four distant
selection cases were added after restoring the missing adjacent paths.

Affected results: 41 passed, zero failed/skipped: OutsideKeeperChoices 24 at
20261116T214419078Z; MovedDefenderMayStand two at 20261116T214524317Z;
ThrowInContinues ten at 20261116T214543253Z; OnlineMovementPlayModeTests five
at 20261116T214627789Z. Native XML/progress are under the same Runs root.
A Pipeline polling 400 interrupted the first helper, but native XML and recovered
test_status confirmed all 24 cases completed; they were not rerun or double-counted.

All 16 new cases have separate deferred Mac manifest entries; inventory/hashes
validate. No paired or complete-suite run occurred. Windows exports still contain
v14 at 690c412; v15/v16 require grouped refresh before paired use.

For later broader user verification use Assets/Tests/PlayMode/Features/OnlineGroundDuels
/ OnlineGroundDuelPlayModeTests and OnlineKeeperGroundChoices /
OnlineKeeperGroundChoicesPlayModeTests, since duel and keeper source entry points
are shared. Only the selected affected methods above were executed in this phase.
