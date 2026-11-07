# Attribute score floor

Contract: numeric total = max(current attribute + all signed modifiers, 0) +
dice. The current attribute already includes injury. Jackpot total stays 50;
natural-one misses/fouls and numeric-only handling/card/injury checks remain intact.

- [x] Reproduce negative effective attributes before the fix.
- [x] Share the floor across resolution, probabilities, previews and labels.
- [x] Cover low/injured shooting, saves, heading and Ball Control; isolated dive odds/labels.
- [x] Preserve positive modified attributes, bonuses, jackpot totals and natural-one probability rules.
- [x] Compile through the live Editor; run only AttributeScoreFloor fixtures.

The shared helper is `GameplayScore`: apply every signed modifier before the floor,
then add the numeric dice. Callers retain eligibility for STAR and special outcomes.
`PlayerToken.HandleInjury` now also reduces Aerial, Saving and Handling; those
current GK attributes had been omitted from the existing all-attributes injury rule.
No save format, live save or designer Stats layout was changed.

Confirmed by the user: retain difficult-angle hexes in shooting calculations
and cap the combined shooting penalty at 2. Existing code already does this;
no gameplay change or additional test run was needed for this confirmation.

## Executed evidence

Native XML/progress is under `TestResults/GameplayRegression/Runs/` (git-ignored):

| Filter / stage | Passed | Failed | Skipped | Native XML |
| --- | ---: | ---: | ---: | --- |
| Initial EditMode reproduction | 1 | 12 | 0 | `20261107T152530285Z/results.xml` |
| Initial gameplay fixture run | 2 | 12 | 0 | `20261107T153050083Z/results.xml` |
| Corrected gameplay checkpoint | 14 | 0 | 0 | `20261107T153426569Z/results.xml` |
| AttributeScoreFloorEditorTests, final | 23 | 0 | 0 | `20261107T194634274Z/results.xml` |
| AttributeScoreFloorPlayModeTests, final | 14 | 0 | 0 | `20261107T194704757Z/results.xml` |

The initial gameplay failures included eight fixture waits for blocker movement
before selecting a defender; those now perform a real defensive snapshot move.
Save tests also exposed the omitted GK injury reductions (Saving total 5 instead
of 4), and ended an async save while it was still awaiting scatter. They now
complete direction/distance and the real continuation before cleanup. No gameplay
assertion was relaxed to accommodate fixture errors. One fixture compile error
from wrapping the void distance-roll API in StartCoroutine was corrected.

Final native Editor state: `isCompiling=false`,
`EditorUtility.scriptCompilationFailed=false`, Play Mode stopped and loaded MainMenu
clean. Earlier transient Pipeline recompile-status network failures and historical
compiler log entries are not current compilation failures. The rulebook source and
existing prefab were refreshed in Edit Mode. Shared-font glyph-cache changes from
test rendering are removed from the final source diff.

The 23 EditMode cases cover shared score boundaries, bonuses, jackpot totals,
shot/save/aerial/control probabilities, numeric-one shot exclusion, current injury
attributes, guidance and bundled wording. The 14 gameplay cases cover healthy and
injured snapshots (numeric/STAR), distance-penalized headed saves tied into scatter,
and injured distance-two controls, all in both directions. No hardware/manual-input,
full match, full dive flow or complete-suite validation is claimed.

## Broader checks handed to the user

The helper affects shared rules and AI expectations. Relevant existing fixtures:

- `Assets/Tests/PlayMode/`: `LegacyShootingBranchPlayModeTests`,
  `LegacyFreeKickShotPlayModeTests`, `LegacyGoalkeeperShotPlayModeTests`,
  `LegacyHighPassHeaderPlayModeTests`, `LegacyHeaderResolutionPlayModeTests`,
  `LegacyMovementPlayModeTests`, `LegacyFoulPlayModeTests` and
  `GoalkeeperBoxEntryPlayModeTests` for other shot, save, control, injury and dive
  continuations. Also `OutsideKeeperHeaders` category for the prior GK fix.
- `Assets/Editor/`: `DuelResolutionPresentationEditorTests`,
  `HeaderChallengePresentationEditorTests`, `ActionSequenceEditorTests`,
  `RoomAITacticalPlannerEditorTests` and `TacticalRulesEditorTests` for existing
  expected totals, displayed odds and AI choices.

For a complete regression check, the user should press Run All on both EditMode
and PlayMode tabs and report failures. Codex did not execute these broader checks
or wait for them. Historical and overlapping runs must not be summed as coverage.
