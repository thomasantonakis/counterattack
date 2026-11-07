# Approved rule decisions — 2026-11-07

## Follow-up: outside keeper nominations and zero Heading

- [x] Reproduce the reported Select All failure in both directions through a real
  HP, keeper rush commitment and defender nomination input.
- [x] Keep GK identity separate from Aerial/Heading selection: up to two outfield
  defenders plus the GK, including when the GK is outside their own box.
- [x] Keep the keeper in the separate third panel row and show exactly Heading 0.
- [x] Zero initial Heading receives no further distance penalty in rolls,
  guidance, expected statistics or projected AI positions. Positive attributes
  retain the distance-two penalty.
- [x] Verify a numeric 6 at distance two totals 6 and an outside keeper win heads
  away; retain jackpot ties and inside/outside rush attribute selection.
- [x] Correct the bundled rulebook and existing prefab text without scene edits.

Reproduction: `Runs/20261107T151011856Z/results.xml`, 0 passed / 2 failed / 0
skipped. Select All retained only the locked keeper instead of all three players.
The exact two new cases passed after the fix in
`Runs/20261107T151140321Z/results.xml` (2/0/0).
Final EditMode: `Runs/20261107T151402292Z/results.xml`, **18/0/0**.
Final PlayMode category: `Runs/20261107T151429305Z/results.xml`, **12/0/0**.
Compilation through the live Editor was clean. An intermediate EditMode run
`Runs/20261107T151254295Z/results.xml` was 17/1/0 because serialized prose lost
a duplicated space; the source spacing was normalized and exact-text checks
passed on rerun. No gameplay assertion was relaxed.
Use category **OutsideKeeperHeaders** for the related PlayMode checks; the wider
approved-decisions PlayMode fixture now contains 37 cases, but is not rerun by
default for this focused follow-up. The initial-batch results below are historical.
For broader user-run header coverage, use `LegacyHeaderSelectionPlayModeTests`,
`LegacyHeaderResolutionPlayModeTests` and `LegacyHeaderGoalBranchPlayModeTests`
under `Assets/Tests/PlayMode/`, plus `HeaderChallengePresentationEditorTests` and
`ActionSequenceEditorTests` under `Assets/Editor/`. They cover other nomination,
goal and AI continuation combinations; they were not run by this follow-up.

## Initial approved batch

- [x] Reproduce High Pass jackpot, own-box goalkeeper card and booked-no-card defects.
- [x] High/Long Pass accuracy and Ball Control accept jackpots; numeric six is separate.
- [x] Keep normal/extra-time stoppage cap 7; extra-time cap remains an open discussion.
- [x] Preserve defending goalkeeper priority on equal team-best aerial scores (unchanged code).
- [x] Own-box goalkeeper fouls use no-card/yellow; outside-box fouls retain regular policy.
- [x] Only straight red, second yellow or second injury forces the foul stoppage.
- [x] Outside-own-box keeper headers use Heading 0 and never Save and Hold.
- [x] Rush eligibility uses final keeper position rather than ball box classification.
- [x] Preview probabilities and challenge labels match the actual outcomes.
- [x] Compile through live Editor and run only ApprovedRuleDecisions fixtures.

Tests reuse the owned deterministic board and cleanup. No live saves or Stats
layouts are modified. Further wiki gaps and legacy migration are out of scope.

The initial batch's fixture filters were `ApprovedRuleDecisionsEditorTests` (15 cases) and
`ApprovedRuleDecisionsPlayModeTests` (35 cases). The PlayMode fixture directly
reuses four outside-keeper discipline scenarios and four foul-stop scenarios as
preservation checks, without running their unrelated original fixtures.

Initial EditMode reproduction: `Runs/20261107T143717366Z/results.xml`,
5 passed / 6 failed / 0 skipped (low-attribute HP probability and mirrored
keeper attributes). Initial PlayMode run: `Runs/20261107T143737661Z/results.xml`,
6/13/0; four failures were an ambiguous reflection overload and four were board
geometry setup errors, corrected in the fixture. Two HP STAR, one booked-no-card
and two outside-header cases reached the differing gameplay behavior. The
corrected keeper card reproduction is `Runs/20261107T144009892Z/results.xml`,
0/4/0, showing harsh leniency instead of normal leniency in both directions.
An intermediate EditMode run `Runs/20261107T144349728Z/results.xml` was 12/2/0;
the two failures were a wrong reflected preview-owner type, subsequently fixed.
Assertions were preserved; no gameplay change was made to satisfy fixture errors.

Green checkpoints before the final card prompt/wording follow-up:
`Runs/20261107T144750810Z/results.xml` (15/0/0 EditMode) and
`Runs/20261107T144818634Z/results.xml` (35/0/0 PlayMode).
Final EditMode: `Runs/20261107T145108743Z/results.xml` (15/0/0).
The final PlayMode rerun `Runs/20261107T145149138Z/results.xml` was 31/4/0:
the four failures were an added normal-leniency prompt assertion at token selection,
before that guidance is displayed. The assertion was moved to the actual tackle-roll
state, without changing its expectation or any gameplay assertions. Only those four
cases were rerun: `Runs/20261107T145407375Z/results.xml`, **4/0/0**. Thus all 35
distinct PlayMode cases are green after the focused retest, alongside 15 EditMode
cases. The final production scripts compile cleanly through the live Editor.
These overlapping runs must not be added together as unique coverage. Native artifacts are local
under `TestResults/GameplayRegression/` and git-ignored.

Rulebook prose was refreshed in the existing prefab with
`RulesViewerEditorTools.RefreshArticleText`; only text changed. No scene resave
or layout authoring was needed. Unity's serialization whitespace and the shared
font's test-generated hyphen glyph cache were removed from the working diff.
The Editor was verified stopped and Room clean after the final focused run.

## Broader verification handed to the user

Shared discipline, aerial continuations and AI previews can affect other features.
No broad fixtures or full suites were run automatically. Relevant existing paths:

- `Assets/Tests/PlayMode/`: `LegacyFoulPlayModeTests`,
  `LegacyKeeperRemainingPlayModeTests`, `LegacyHighPassSelectionPlayModeTests`,
  `LegacyHighPassMovementPlayModeTests`, `LegacyHeaderResolutionPlayModeTests`,
  `LegacyHeaderGoalBranchPlayModeTests` and `GoalkeeperBoxEntryPlayModeTests`.
- `Assets/Editor/`: `HeaderChallengePresentationEditorTests`,
  `ActionSequenceEditorTests`, `RoomAITacticalPlannerEditorTests` and
  `TacticalRulesEditorTests` for panel guidance, tactical probabilities and
  continuation decisions.

For a complete regression check, the user should press **Run All** on both
EditMode and PlayMode tabs and report any failures. Historical whole-suite passes
do not validate this new tree. This task does not resume legacy runner retirement.
