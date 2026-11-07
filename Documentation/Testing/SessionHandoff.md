# Session handoff — 2026-11-07

## Follow-up decisions — 2026-11-07

Implemented and focused-verified: direct headed-back-pass choice/catch, landing-ZOI
wording, digital squad wording and period-specific stoppage cap. Compilation was
clean through the live Editor. Native results: HeadedBackPassEditorTests **11/0/0**
at `Runs/20261107T210952817Z/results.xml`, HeadedBackPassPlayModeTests **26/0/0**
at `Runs/20261107T211031868Z/results.xml`, ExtraTimeStoppageEditorTests **16/0/0**
at `Runs/20261107T210821974Z/results.xml`. These are focused counts, not full-suite
validation. The dedicated feature checklists name broader user-run fixtures.
Pickup after movement remains unimplemented and belongs to the separate prompt.

The user resolved the remaining rule questions after the approved scoring batch:

- WIKI-009: headed passes are airborne until the final hex, where the ball drops.
  Eligible defenders in that hex's ZOI may attempt an interception; jumpers,
  stunned players and other excluded players remain ineligible. Existing code
  already implements landing-only attempts; clarify the bundled wording.
- WIKI-010: allow teammate headers to the keeper after open play AND corners.
  Direct reception inside the keeper's own box offers hands [H] / feet [F]. Hands
  rolls an ordinary D6 with no attribute: 1 scatters from the keeper; 2–6 held.
  Feet needs no roll. Outside-box receptions always go to feet.
- WIKI-011: keep current from-behind discipline only. Do not add last-man logic.
- WIKI-012/013: pickup after movement is a separate-session feature. The choice
  occurs before Final Thirds; Quick Throw must bypass Final Thirds. Header-to-feet
  eligibility persists through subsequent Movement Phases while the GK retains
  possession, until pickup or passing. Teammate kicked passes never authorize
  hands; deliberate Drop Ball forbids a re-pickup. Full prompt:
  `Documentation/Testing/GoalkeeperPickupFutureSessionPrompt.md`.
- WIKI-002: digital squads stay 16 or 18, with exactly two goalkeepers in either.
  The old printed draft rules are superseded. No draft gameplay change requested.
- WIKI-003: extra-time stoppage cap is now 3 per half; regulation remains 7.
  This supersedes the earlier open discussion and previous all-halves cap 7.


## Start here

**Latest batch: AttributeScoreFloor.** Numeric scores use
`max(current injured attribute + signed modifiers, 0) + dice` across gameplay,
probabilities and guidance. Accepted jackpots stay 50; natural-one outcomes stay.
GK injuries also reduce Aerial/Saving/Handling (previously omitted). Keep the
user-confirmed shooting penalty cap -2, including difficult-angle hexes in the
calculation. The cap decision is resolved; do not reopen it. Read
`Assets/Editor/FeatureTests/AttributeScoreFloor/Verification.md` for current
focused execution results; earlier counts below are historical.
Final floor validation: **23/0/0 EditMode**, **14/0/0 PlayMode** at
`Runs/20261107T194634274Z/results.xml` and
`Runs/20261107T194704757Z/results.xml`. Native compilation flags are clear and
the loaded MainMenu scene is clean, with Play Mode stopped. Broader shared-rule
regressions are handed to the user; no full suites were executed.

**Latest correction:** GK identity always retains the separate nomination in
addition to two outfield defenders, including outside own box. Only the chosen
attribute changes to Heading 0. Zero Heading receives no additional distance
penalty. The earlier implementation's ordinary-player nomination budget was
incorrect and has been corrected. The focused follow-up uses PlayMode category
`OutsideKeeperHeaders`; see the feature checklist for native results.
Final follow-up: **18/0/0 EditMode**, **12/0/0 focused PlayMode**, clean live
compilation. Native XML: `Runs/20261107T151402292Z/results.xml` and
`Runs/20261107T151429305Z/results.xml`. These supersede the older nomination
semantics below; the rest of the initial approved batch was not re-executed.

**Approved implementation batch:** the user has now decided WIKI-003–008; see
the approved-decisions section of RulesWikiAudit. Accuracy (HP/LB) and Ball
Control accept automatic jackpots; own-box GK card policy is no-card/yellow;
booked/no-new-card fouls permit advantage; retain GK tie priority and current
7-action regulation cap (extra-time cap is now 3). Outside-own-box keepers may contest
and rush using Heading 0, with ordinary headed-pass continuation, not a catch.
Use final keeper position to select the attribute. Dedicated fixtures are
ApprovedRuleDecisionsEditorTests and ApprovedRuleDecisionsPlayModeTests.
WIKI-009–013 were subsequently reviewed; the follow-up decisions above supersede the original deferral.

**Approved batch completed:** 15 dedicated EditMode cases passed; all 35 distinct
PlayMode cases are green after a four-case prompt-timing fixture retest. Final
evidence: `Runs/20261107T145108743Z/results.xml` (15/0/0),
`Runs/20261107T145149138Z/results.xml` (31/4/0; assertion timing), and
`Runs/20261107T145407375Z/results.xml` (4/0/0 retest). There was also a 35/0/0
checkpoint before that prompt assertion. Do not sum the overlapping runs.
Scripts compile cleanly. Bundled text and existing Rules prefab are aligned;
Room/MainMenu layouts and live saves were untouched. Broader regression checks
are handed to the user, not automatically run. The extra-time cap is resolved at 3. Pickup after movement remains deferred to its separate prompt.

**Current review refresh (2026-11-07):** source review started from clean
`dd87cbd`, four commits after the old `58f92ff` handoff. The current task is to
refresh pending actions and report wiki-to-gameplay discrepancies. See the
current-review section in [../RulesWikiAudit.md](../RulesWikiAudit.md):
WIKI-003–008 record concrete source differences; WIKI-009–013 record ambiguous
or missing-flow candidates. No gameplay correction is approved by this review.
No Editor operation, compilation or test run was performed. The material below
describes the earlier session unless explicitly refreshed.

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

Current priority is to discuss the new audit findings and record each decision:
extra-time action cap, non-duel STAR handling, own-box keeper leniency,
booked-defender no-card stoppage, defensive header tie priority and attacking
keeper header attributes. Then resolve headed-pass landing recovery, header back
to keeper, last-man fouls and legal keeper pickup timing. Preserve the existing
penalty play-on and defensive own-goal decisions.

The full clause/diagram comparison remains incomplete. Keep old migration work
separate; do not automatically resume it or start complete suites.

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
