# Online pickup shortcut

- [x] Reproduce missing V pickup through actual movement selection.
- [x] Route V to the published legal ball destination in attack/defence/two-for-two.
- [x] Preserve goalkeeper long-pass claiming and reject ambiguous meanings.
- [x] Exercise real attacking/defensive collections, ownership, recovery stats and dribble continuation.
- [x] Respect excluded hanging-pass collectors and set-piece next-touch restrictions.
- [x] Cover both teams/directions, guest authority, connection hold and duplicate commands.
- [x] Register each of the 29 new cases separately for the deferred Mac run.

No new wire actions or fields: Room remains v16, wire v7. V selects the same
movement.target action as clicking the ball's legal hex. Publication remains the
legality gate; no client-supplied position or movement result is accepted.
The real source automatically starts movement when a completed action leaves
an unpossessed ball. The fixture follows that existing flow rather than sending
a second movement.start. No gameplay rule was changed for fixture setup.

Compilation clean through the live Editor, zero C# errors. Own-feature results:
29 passed, zero failed/skipped. OnlinePickupKeyEditorTests 13 at
TestResults/GameplayRegression/Runs/20261116T215225946Z/results.xml;
OnlinePickupKeyPlayModeTests 16 at 20261116T215246159Z/results.xml under the
same Runs root. Eight successful collections and eight source-enforced exclusion
cases use real managers. The corrected eight-case pre-fix reproduction at
20261116T215122072Z failed at the missing V mapping and is retained.

Affected shortcut checks: OnlineBoardInputEditorTests 44 at 20261116T215341080Z
and OnlineContextKeysEditorTests 45 at 20261116T215349162Z passed, with zero
failed/skipped. No whole suite or paired test was run. Manifest inventory and
hashes validate. Exports still contain v14; refresh with the header batch.

For later broader user verification use Assets/Tests/PlayMode/Features/OnlineMovement
/ OnlineMovementPlayModeTests and OnlineGroundDuels / OnlineGroundDuelPlayModeTests
for collection/movement interruptions shared with the new shortcut. Physical
macOS keyboard focus and paired delivery remain deferred.
