# Online seat ownership verification

- [x] `OnlineSeatOwnershipEditorTests` verified authenticated setup ownership and rejection of an incompatible older build. Two passed, zero failed, zero skipped. Native results in `TestResults/GameplayRegression/Runs/20261123T225656177Z/`.
- [x] `OnlineSeatOwnershipPlayModeTests` verified Draft and Room startup, allowed and disallowed kickoff confirmations, either kickoff team, and guest replica startup. Two passed, zero failed, zero skipped. Native results in `TestResults/GameplayRegression/Runs/20261123T230013872Z/`.
- [ ] In paired builds, host through the Online menu, confirm Home on the host and Away on the guest, finish Draft and issue a Room command from each machine. Check that perspective, pause ownership and return to the menu follow the correct manager.

Unity compiled with zero errors. For broader checks of the affected Room fixture paths, use `OnlineNativeRoomPlayModeTests` under `Assets/Tests/PlayMode/Features/OnlineNativeRoom/`, `OnlineNativeShootoutOrderPlayModeTests` under `Assets/Tests/PlayMode/Features/OnlineNativeShootoutOrder/`, and `OnlineSharedSubstitutionReviewPlayModeTests` under `Assets/Tests/PlayMode/Features/OnlineSharedSubstitutionReview/`.
