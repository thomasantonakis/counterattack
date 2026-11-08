# Headed back-pass verification

- [x] Preserve landing-only ZOI interception and existing jumper/stun exclusions.
- [x] Direct teammate headers inside the keeper's own box offer hands [H] / feet [F].
- [x] Corners and open play use the same flow. Outside-box reception stays at feet.
- [x] Separate ordinary D6 catch: 1 loose ball from keeper; 2–6 held; STAR becomes 6.
- [x] Reuse the edit-mode-authored single-roll panel for choice/catch/scatter.
- [x] Keep exclusive input, receiving-team ownership and AI legal actions.
- [x] Held ball offers distribution before Final Thirds without inventing a save stat.
- [x] Cancel/reset invalidates pending choice/roll and permits the next normal duel.
- [x] Bundle updated heading/draft/stoppage wording through existing rulebook prefab.
- [x] Persistent phase-end pickup eligibility is implemented separately; see `../GoalkeeperPickup/Verification.md` for its own executed scope.

Focused fixtures: HeadedBackPassEditorTests in this folder; HeadedBackPassPlayModeTests
in Assets/Tests/PlayMode/Features/HeadedBackPass/. Tests reuse the isolated owned board
and exported authored presentation; no live save or designer layout is changed.

Initial PlayMode checkpoint: 22 passed / 0 failed / 0 skipped,
TestResults/GameplayRegression/Runs/20261107T210333178Z/results.xml.
This was after implementation; no native red reproduction was run. Source inspection
established the ordinary-receiver path had no catch choice/check.
Final results are recorded below after focused execution.

Broader verification is user-run: Assets/Tests/PlayMode/ fixtures
LegacyHeaderResolutionPlayModeTests, LegacyHeaderSelectionPlayModeTests,
GoalkeeperBoxEntryPlayModeTests and LegacyKeeperRemainingPlayModeTests;
Assets/Editor/ fixtures DuelResolutionPresentationEditorTests,
HeaderChallengePresentationEditorTests and RulesViewerEditorTests. These cover
shared overlay input, other header/recovery routes and viewer interactions.
No broad suite was executed for this change.

Final EditMode: **11 passed / 0 failed / 0 skipped**,
`TestResults/GameplayRegression/Runs/20261107T210952817Z/results.xml`.

Final PlayMode: **26 passed / 0 failed / 0 skipped**,
`TestResults/GameplayRegression/Runs/20261107T211031868Z/results.xml`.
This expands the initial checkpoint with pre-roll cancellation after selecting hands
and normal-duel input restoration after canceling the choice. Both directions and
corner/open-play combinations passed. No assertion was weakened.
