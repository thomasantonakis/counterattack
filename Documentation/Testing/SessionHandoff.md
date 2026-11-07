# Session handoff — 2026-11-07

## Start here

The user requested coherent commits and a push, then a fresh session to review
whether gameplay or documentation is missing. This is a review handoff, not an
instruction to resume the old migration or run full test suites automatically.

Read this file, [GameplayReview.md](GameplayReview.md), and
[../RulesWikiAudit.md](../RulesWikiAudit.md). The complete wiki-to-code audit is
**not finished**. Importing all pages and passing viewer tests do not establish
that every football rule is implemented correctly.

## Decisions already made

- Audit common rules with **Advanced variants taking precedence**. Exclude
  reckless tackles and their dependent references.
- Resolve each gameplay/wiki discrepancy with the user before changing behavior.
- **Penalty-area fouls can be played on.** The user explicitly confirmed the
  game is right and the wiki's mandatory-penalty sentence is wrong. Keep gameplay;
  the bundled Movement Phase page is corrected. An upstream patch is prepared,
  checked against the pinned source, and has not been published to the wiki repo.
- Defensive scatter into the player's own goal is that defender's own goal,
  including a rushing keeper/aerial tie, outfield aerial tie, block or tied save.
  No-deflection failed saves remain attacker goals. Do not reopen this decision.
- Rules uses one offline continuous document, persistent contents, full-text
  search with results AND next/previous matches, inline links, and reading history.
- Main Menu, Room and pause-menu Rules all open the same viewer. Room progression
  suspends while reading and the prior pause state is restored on close.
- Enter in search means Next; Escape clears a nonempty search instead of closing.
  Back/Forward includes manually scrolled reading positions.
- Pause-menu Rules is a separate row immediately above Back to Main Menu.
  Room Rules is above InstructionsPanel with automatic navigation disabled.
  RoomTip is now above the instructions box, beside Rules.

## What changed in this session

The starting repository was clean at `9862d4d` on master. Changes are grouped into
content, viewer/scene integration, tests/results documentation, and review/handoff
commits. No ball-action, scoring, discipline, AI or save-format rule was changed.

- `4d48132`: bundled Advanced rulebook, diagrams, audit findings and penalty patch.
- `9bc820d`: shared viewer, dedicated font, authoring tool and scene integration.
- `3a05edd`: Rules tests and executed validation record.
- The following documentation commit refreshes this handoff and GameplayReview.

The viewer is `Assets/Prefabs/RulesViewer.prefab`, with an instance in MainMenu and
Room. Its objects, diagrams, contents buttons and search rows are authored in edit
mode. Runtime code updates visibility, search colors, input ownership and scrolling;
it does not construct the UI hierarchy. A dedicated Rules font avoids changing
shared designer fonts. MainMenuManager.OpenRules now opens the viewer.
RoomInputOwnership reserves input/camera/Escape while it is open.

Source is bundled under `Assets/Docs/Wiki/English/` and `Images/`, pinned to
`counterattackgame/wiki@e9c0f9fe0d575d61368f20cfd412bc9194ca2e71`.
Ten English pages, eight diagrams and 523 rendered/searchable blocks are included.
The game edition excludes reckless tackles and has the approved penalty wording.
Core/Advanced and tabletop setup text otherwise remains pending chapter review.
Source attribution and authoring instructions are in the bundled README.

`RulesViewerEditorTools.Build` validates internal links and rebuilds the prefab
and entry buttons. Save scenes first. **Do not run broad Room authoring tools
blindly:** `RoomPresentationEditorTools.AuthorTips` still has the older below-box
tip defaults. The saved Room scene contains the user's latest above-box placement;
bringing that helper into alignment is a small remaining tooling task.

Unity regenerated unrelated panel IDs while previewing the layout. Those changes
were removed after comparing normalized serialized documents for semantic equality.
The original unrelated text color was restored. MatchStatsUI/Stats was not restyled.

## Findings versus the wiki

1. **Confirmed wiki error, resolved:** mandatory penalties conflict with the
   Set Pieces play-on option. Current HandleFoulProcess offers play-on for penalty
   fouls; the user approved that behavior. See
   `Documentation/WikiPatches/penalty-advantage.patch`.
2. **Source-level match:** LongBallManager.AccuracyThresholdFor returns 10 for
   opposite final thirds and 9 otherwise, matching Advanced Long Pass accuracy.
   Eligibility, scatter, GK movement and continuation are not fully audited by
   that observation.
3. **Unresolved documentation variant:** Getting Started describes both one
   keeper/fifteen outfielders and two keepers/fourteen outfielders. DraftManager
   uses configured squad size (fallback sixteen), reserves two keeper slots and
   selects `squadSize - 2` outfielders. Confirm the digital edition wording; do
   not assume all tabletop draft variants must be implemented.
4. The existing booked-defender forced-stoppage policy, GK recovery/distribution
   boundaries and synthetic input return semantics remain separate review items.
   Penalty play-on approval does not resolve those other questions.

There is **no newly confirmed gameplay-code defect from a completed wiki audit**.
Do not present the older six migration fixes as new wiki discrepancies.

## Executed validation and its limits

The final Rules controls batch passed:

| Filter | Passed / failed / skipped | Native XML under TestResults/GameplayRegression/Runs/ |
| --- | --- | --- |
| RulesViewerEditorTests | 11 / 0 / 0 | 20261107T101432316Z/results.xml |
| RulesViewerPlayModeTests | 3 / 0 / 0 | 20261107T101453780Z/results.xml |
| RoomPresentationEditorTests | 13 / 0 / 0 | 20261107T101232653Z/results.xml |

These 27 checks cover scene wiring/layout, local links, pointer-dispatched history
buttons, manual scroll history, TMP submit/cancel events, search and Room pause
restoration. The earlier viewer batch also passed 8 hover and 14 action-sequence
tests; the 49-check and 27-check reports overlap and must not be summed as unique
coverage. Paths and failed/retest history are in GameplayRegressionMigration.md.

The subsequent RoomTip move was a scene-only RectTransform edit: verified and saved
above the box with clearance for Rules. No automated tests were rerun for that
low-impact layout-only adjustment. No scripts changed after clean live compilation.
Editor was last observed ready, stopped, and with Room saved; recheck live state
rather than assuming that remains true.

PNG previews are in `Runs/RulesViewerVisuals/`. XML/progress and compilation
envelopes are retained locally outside Temp. **TestResults is git-ignored**, so
a fresh clone will not receive those files through this push. A new session in
this workspace can inspect them; another machine needs copied artifacts or an
explicitly requested rerun. Physical keyboard/hardware input is not established
by UI-event tests or still images.

Historical full-suite checkpoints remain 747 EditMode passed
(`20261106T144426744Z/results.xml`) and 362 PlayMode passed
(`20261106T172436176Z/results.xml`). They predate later Room/viewer changes.
No full suites were run for the current UI work; do not quote those historical
counts as full validation of the current tree or as current discovery totals.

## Older migration follow-up: committed, still not fully validated

The old handoff's “uncommitted test-only work” is now committed in `c391477`:
scoped panel fingerprints, authored-panel fixture updates and strengthened
shooting/header assertions. Earlier production fixes remain in `4cb471c`.
Later Room work is in `f8295b4`, `018c242` and `9862d4d`; see
[RoomPresentationImplementation.md](RoomPresentationImplementation.md).

The interrupted Shot audit at `Runs/20261106T214114486Z/` recorded 15 passed
and 1 failed, without final XML. Its premature off-target Final Third assertion
is still present in LegacyShootingBranchesFixture.ExpectOffTarget:
the keeper must finish goal-kick preparation before asserting Final Third
activation and restart availability. No focused rerun resolved that in this
session. This is a readiness/oracle investigation, not authority to change gameplay.

Two legacy originals remain in GameTestScenarioRunner:
Scenario_034_Shooting_Branch and Scenario_038_HeaderAtGoal_Branch. Do not delete
them or DummyLoader's runner until the strengthened common-assertion audit and
explicit own-goal scorer/event assertions pass. The previous complete 362-case
run predates those stronger assertions. Old whole-Room hashes are historical;
use the scoped panel fingerprint for owned presentation fixtures.

## Fresh-session review order

1. Inspect current git status/log and the evidence above. Do not assume old
   “uncommitted” lists, full-suite counts or scene hashes describe the new tree.
2. Complete a rule-by-rule matrix from every English chapter, Quick Guide and
   diagram. Map rule to current code, reachable input flow, relevant fixture,
   actual result and disposition. Distinguish source support from executed proof.
3. Review drafting wording and the unresolved policy/API questions in
   GameplayReview.md. Preserve approved penalty play-on and own-goal decisions.
4. Review keeper pickup timing, own/opponent-touch conditions, entry/re-entry
   interruptions, aerial/control continuations, shot/loose-ball ownership and
   set-piece transitions. These are audit targets, not established new bugs.
5. If authorized to finish migration, correct readiness waits without weakening
   outcomes, add explicit own-goal attribution coverage, then retire the two
   originals only after their replacements pass.
6. Keep live saves and designer Stats layout untouched. Use live Unity CLI for
   all Editor control; never launch Unity, use batchmode/GUI scripting, or start
   ordinary gameplay. Framework tests are authorized, but this handoff does not
   request another full-suite run.

## Copyable prompt for the next session

Read Documentation/Testing/SessionHandoff.md,
Documentation/Testing/GameplayReview.md and Documentation/RulesWikiAudit.md.
Review the current tree for missing gameplay rules,
documentation or coverage against the pinned English wiki, using Advanced variants
and excluding reckless tackles. Complete the rule-by-rule comparison before claiming
parity. Preserve the confirmed penalty play-on and defensive own-goal decisions.
Present each new discrepancy with code/source evidence and proposed game or wiki
correction for my decision. Keep source inspection, executed tests and pending work
distinct. Do not automatically resume migration, run full suites, modify live saves,
or restyle designer-owned Stats. Check git and live Editor state before any execution.
