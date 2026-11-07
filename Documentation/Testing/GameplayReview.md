# Gameplay decisions and changes to review

Updated 2026-11-07. Start with [SessionHandoff.md](SessionHandoff.md). This document
separates current wiki findings, the new Rules UI, and older gameplay changes with
their wider effects. Passing deterministic regressions does not prove every wiki
rule or match combination. The user requested a fresh-session review; do not
automatically restart the old migration or run full suites.

## Current wiki comparison: findings and confidence

**Latest score-floor rule:** all attribute-based numeric scores floor the current
injured attribute plus signed modifiers at zero before adding dice. Accepted
jackpots remain 50, and natural-one misses/fouls are preserved. Shared resolution,
probability and guidance code uses GameplayScore. GK injury now reduces Aerial,
Saving and Handling as required by the existing all-attributes injury rule.
The user confirmed that difficult-angle hexes remain in the calculation, with
the combined shooting penalty capped at -2. This is resolved.
Focused results are in the AttributeScoreFloor feature checklist.
Final execution: 23 EditMode and 14 PlayMode cases passed, zero failures/skips;
clean native compilation. This validates the dedicated feature scope only.

**Outside-keeper follow-up:** keep the separate GK nomination and third panel row
outside the box; only the chosen attribute changes to Heading 0. Do not count
that keeper among the two outfield players. Zero initial heading receives no
further distance penalty. The reported Select All failure was reproduced in both
directions and corrected; 18 EditMode and 12 focused PlayMode cases passed.
Artifacts and scope are recorded in GameplayRegressionMigration.

**User decisions supersede the initial findings:** WIKI-003 keeps the current
7-action cap with extra-time cap 3 open; WIKI-004 explicitly allows automatic
jackpots on HP/LB accuracy and Ball Control; WIKI-005/006 follow the wiki's
own-box GK discipline and mandatory-stoppage limits; WIKI-007 retains GK priority;
WIKI-008 permits ordinary Heading 0 challenges/rushes outside own box. See
RulesWikiAudit for the exact contracts and the ApprovedRuleDecisions feature
checklist for executed results. Further gaps are deferred by user instruction.

The approved batch now has clean live compilation, 15 EditMode cases passed and
35 distinct PlayMode cases green after the final four-case fixture-timing retest.
Native XML and exact counts are recorded in GameplayRegressionMigration and the
feature checklist; these are focused results, not full-suite validation.

**Source-review refresh at `dd87cbd` (2026-11-07):** pending actions and detailed
evidence are now in [RulesWikiAudit.md](../RulesWikiAudit.md), WIKI-003–013.
Six concrete differences await a game-versus-wiki decision: extra-time stoppage
cap, STAR on non-duel accuracy/control checks, own-box goalkeeper card policy,
no-card fouls by booked defenders, goalkeeper priority on tied defensive header
scores, and Aerial used for attacking goalkeeper headers. Five further entries
track ambiguous/missing flows: headed-pass landing interceptions, header back to
hands, last-man foul detection, keeper return/pickup eligibility and conflicting
keeper/distribution wording. None was runtime-reproduced in this review; no
gameplay changes, compilation or tests were performed.

The older findings table and migration evidence below remain historical context.
The rule-by-rule comparison is still incomplete. Next action: resolve those
discrepancies with the user, then implement and run focused tests only for the
approved scope. Migration readiness/scorer coverage and legacy 034/038 retirement
remain separate pending work. The prior no-new-defect statement describes the
viewer/import batch, not the new source-review findings.

The English source is pinned to `counterattackgame/wiki@e9c0f9fe0d575d61368f20cfd412bc9194ca2e71`.
Advanced variants take precedence; reckless tackles are excluded. All ten pages
and eight diagrams are imported, but the full behavioral comparison is unfinished.
See [RulesWikiAudit.md](../RulesWikiAudit.md) for chapter coverage and decisions.

| Finding | Code and rule evidence | Decision / wider effect | Confidence |
| --- | --- | --- | --- |
| Penalty play-on | Movement Phase forbids declining a penalty; Set Pieces allows it. MovementPhaseManager.HandleFoulProcess offers A/F for penalty-area fouls. | User confirmed the game is right. Bundled wording is corrected and an upstream patch is prepared. No gameplay change. Mandatory substitution/sending-off restrictions remain; the older booked-defender stoppage question below is separate. | Confirmed source conflict and explicit user decision; no new runtime foul test in this UI batch. |
| Advanced long-pass accuracy | LongBallManager.AccuracyThresholdFor uses 10 across opposite final thirds, 9 otherwise. | Matches the Advanced threshold. Does not establish all destination restrictions, scatter, GK moves or subsequent action availability. | Source-level match; entire Long Pass rule group remains unaudited. |
| Draft variants | Wiki describes 1 GK + 15 outfielders and an online 2 GK + 14 variant. DraftManager uses configured squad size, two keeper slots and squadSize - 2 outfielders. | Proposed digital-edition clarification needs user confirmation. Do not silently add a new draft mode or remove configurable squad sizes. | Source inspection; full draft lifecycle and roster variants still need review. |

During the earlier viewer/import batch, no new gameplay-code defect was established by a completed wiki audit.
In particular, the six migration fixes below were discovered earlier and should
not be relabeled as findings from this wiki import. Keeper pickup timing, aerial
challenge outcomes, action transitions and repeated Quick Guide rules are next
audit targets, not already-proven mismatches.

## Rules viewer changes, reasons and wider effects

| Change | Reason | Wider effect / review boundary |
| --- | --- | --- |
| Offline Markdown edition, diagrams, validated links and shared prefab | Rules should be available in Main Menu and during a match without a network dependency. | Bundled content changes only when regenerated/shipped. Source paragraphs may still contain tabletop variants and unresolved contradictions; import completeness is not rules parity. |
| Continuous article, persistent contents, inline hyperlinks and reading history | Players need to move between related rules and return to their passage. | Back/Forward includes manual scroll checkpoints and match destinations. Positions/history persist only for the application session, not in match saves. |
| Full-text results, highlights, next/previous and TMP keyboard events | Enter must advance search; Escape must clear it rather than close the viewer. Polling input focus missed native submit timing. | Tests exercise submit/cancel events and pointer dispatch; physical hardware and every IME/device are not covered. Search uses literal, case-insensitive phrases, not fuzzy matching. |
| Room pause/input ownership | Reading must not allow board/camera actions or continue CPU/clock progression. | Viewer stores/restores the previous pause state and time scale. Existing forced-decision restrictions must remain intact. No football outcome or save format is changed. |
| Separate pause Rules row and direct button above instructions | Original Resume clone overlapped Resume; automatic navigation produced arrows toward other UI controls. | Rules now precedes Back to Main Menu; direct navigation is None. RoomTip was moved above the box beside it. MatchStatsUI/Stats remains designer-owned. |
| Dedicated font, edit-mode article/result objects | Keep visuals inspectable and prevent rulebook glyph generation from modifying shared panel fonts. | Rebuild through RulesViewerEditorTools after content changes. Review unsupported Markdown before adding new syntax. |

Final controls batch: 11 Rules EditMode + 3 Rules PlayMode + 13 existing Room
presentation tests passed, with zero failures/skips; compilation is clean. Native
artifact paths and earlier failures/reruns are in GameplayRegressionMigration.md.
The later RoomTip move was inspected/saved as a scene-only adjustment without
another automated run. Test artifacts are local and git-ignored. The final UI
tests overlap the earlier 49-check viewer batch and must not be added as unique
coverage. No full gameplay suites were executed for these UI changes.

Remaining tooling gap: RoomPresentationEditorTools.AuthorTips still authors the
old below-box position. The saved Room scene is correct. Align or preserve the
latest placement before rerunning wholesale presentation authoring. Also avoid
blind scene resaves that regenerate unrelated panel IDs; the current diff removes
that serialization churn after semantic comparison.

## Historical migration validation and scope

The complete native PlayMode run finished **362 passed / 0 failed / 0 skipped**:
`TestResults/GameplayRegression/Runs/20261106T172436176Z/results.xml`.
The preceding full EditMode run finished **747 / 0 / 0**:
`TestResults/GameplayRegression/Runs/20261106T144426744Z/results.xml`.
The then-current discovery was 748 EditMode tests; this is not the current total
after Room and Rules coverage was added. The final legacy assertion audit and
runner retirement remain tracked in the migration document. Do not
mistake discovery, clean compilation, or an earlier green checkpoint for later
execution. The 147 original coroutine methods expand into many independent cases.

The six production files changed while executing the migration are in commit
`4cb471c8a9c06d1b4cc793b86e47278b9e621d4e`. Earlier goalkeeper-rule changes were
already present when migration began; see the separate baseline section below.
Original assertions and deterministic failing/passing artifacts informed the fixes.
Where an old expectation disagreed with an explicit earlier rule change, the native
expectation was updated and the decision recorded. No scoring or discipline rule
was changed to satisfy a test. One additional personal-attribution assertion was initially
deferred because the legacy scenarios did not define that rule. The user has now
resolved it in favor of consistent own-goal attribution, as recorded below.

## Questions for the user, in review order

1. **RESOLVED: defensive scatter into own goal is an own goal.** The user confirmed
   consistency across a rushing goalkeeper who ties the best attacking aerial score,
   an outfield defender who ties it, and a defender blocking a shot. If the resulting
   scatter enters that defender's own goal, credit that player with the own goal
   and add one goal to the opposing team's score; do not award a personal goal to
   the attacker. Apply the same attribution to a tied goalkeeper save that scatters
   into the goal. `LooseBallManager.ResolveOwnGoalToken` already follows this policy
   (`c6f84969`, 2026-06-21); no scoring-code change is currently needed. A failed save
   without a deflection remains an attacker goal. Explicit personal/scorer-record
   assertions remain to be added alongside the original team/touch/reset checks.
   This resolves the earlier extra Yaneva.goals=1 assertion in generated header
   branches02/03: its expectation was wrong. Review assists, scorer labels/keys,
   structured OwnGoalScored events and conceded-team reset for consistency.
2. **RESOLVED: an existing booking alone does not force a foul stoppage.**
   The user confirmed stoppages only for straight red, second yellow or second
   injury. The no-new-card forced-stop branch has been removed and real
   advantage continuation is covered by ApprovedRuleDecisionsPlayModeTests.
   Historical context: `dff745b` policy said yes. Legacy016
   expected Play On; dangerous tackles also impose at least yellow under
   `db459a4`. The original booked-defender board now correctly asserts second
   yellow/sending-off and forced restart. A variant with a genuinely non-booking
   first roll retains the intended Play On and later tackle/stat assertions.
   No discipline code was changed during the original migration. This approved
   follow-up changes that policy and can affect advantage, repeated fouls,
   penalties and movement continuation, not only016.
3. **Is Save and Hold intended whenever a keeper recovers in their own box?**
   Earlier `bd4347bd` / `d6fb7218` changes route those recoveries to the keeper's
   distribution decision. Three old shooting branches expected the generic
   AnyOtherScenario action menu. Native tests now assert actual keeper ownership,
   possession, ball position, exact recovery statistics and Save and Hold instead.
   No recovery rule was changed to obtain green tests. Confirm this applies to
   direct landings, interceptions, tackle recovery and handling-related recovery.
   Reverting it would affect Ground Pass, FTP, Long Ball and loose-ball continuations.
4. **What should synthetic input's accepted/rejected return value mean?** FTP can
   execute a valid click without ConsumeCurrentClick, so its synthetic API reports
   false even though gameplay changes. The originals never asserted the return
   value; replacements check the actual state transition. No input code was
   changed. A future fix must distinguish rejected clicks from consumed clicks and
   input-modal forwarding; AI or automation that retries false responses could
   submit duplicate actions. This is an API consistency investigation, not a
   reason to change pass rules.
5. **Confirm the early defending kickoff front-four policy.** The existing AI
   policy places non-GK shirts7/9/10/11 near halfway during the first five minutes
   of half1. The production fix below makes that policy win over a temporary live
   defensive-gap veto during actual kickoff setup. The original positional and
   first-pass/continuation assertions passed. If the policy itself is unwanted,
   discuss it as an AI design change rather than reverting the regression fix
   without a replacement rule.

## Production code changed during migration

These are the six files in `4cb471c`; expected effects and remaining risks are explicit.
The full362-case PlayMode result above includes them all. The747-case EditMode
checkpoint followed the kickoff fix but preceded the final shot duplicate fix.

| Change | Why it was made | Expected effect elsewhere / review concern | Evidence |
| --- | --- | --- | --- |
| `Ball.MoveToCell` | Check penalty-box entry after the final position snap. A sub-millimeter final segment could previously enter the box after the last check. | Every action using this movement can now offer the existing free GK step at its real arrival. Shared entry memory must prevent repeats; nonqualifying destinations must remain silent. No movement allowance or dice threshold changed. | Boundary reproduction0/1/0, `Runs/20261105T222156793Z/results.xml`; four controlled variants4/0/0, `Runs/20261105T222317026Z/results.xml`; subsequent full suite. |
| `MovementPhaseManager.PrepareAttackerReposition` | Remove the old coroutine's post-yield clear of nutmegVictim. The continuation already clears completed state before offering another duel; the old clear could erase the next victim. | Consecutive nutmegs, failed tackles and winner reposition continuations share this path. Review pace consumption, stuns, later steals and cancellation. The fix changes sequencing, not duel outcomes. | Original015 failure in2/1/0, `Runs/20261106T070141710Z/results.xml`; affected3/0/0, `Runs/20261106T091307769Z/results.xml`; full suite. |
| `CodexTacticianProvider` | The full default-roster kickoff showed No.11 stranded behind halfway because generic defensive-gap safety outranked the existing opening front-four policy. Exempt only real early defending kickoff SetupMove candidates for non-GK shirts7/9/10/11 from that specific veto. | Opening defensive placement can change. Live play, later periods, attacking kickoff, other players, keeper and other safety checks retain their conditions. Review the intended opening press policy and boundary at five minutes. | Actual nine manager-approved setup moves plus first pass/continuation1/0/0, `Runs/20261106T144205324Z/results.xml`; full747 EditMode and later PlayMode suites. |
| `ShotManager` | Normal and free-kick logical box-entry offers now call the shared TryStartGKMoveForPenaltyBox method. Previously the local gkWasOfferedMoveForBox flag did not record shared consumption, so later attraction to an in-box blocker could offer another step. | Normal shots, free kicks, blocker attraction and subsequent scatter share eligibility history. One entry should produce one offer, while a later genuine exit/re-entry must rearm it. Saving/block thresholds and outcome rules are unchanged. | Initial31-branch12/19/0 exposed duplicate waits; after fixes, all31 shooting and12 FK branches passed in full362/0/0. |
| `GoalKeeperManager.FindDefendingGoalkeeperForAttackingTeam` | Use the current match's registered tokens; fall back to global search only when there is no player-token manager. Global search could select another loaded scene's GK or omit preview-scene tokens. | Additive scenes and test/preview boards choose the correct keeper. A partially initialized registration list will no longer silently borrow a keeper from another scene. Verify bootstrap/replacement registration if future failures involve missing keepers. Eligibility criteria did not change. | Both-direction registered-board coverage and complete PlayMode regressions. |
| `HelperFunctions.StartCoroutineAndWait` | Replace perpetual await polling with tracked TaskCompletionSource tasks and cancel outstanding tasks on host destruction. Destroyed test/game coroutine hosts previously left callers waiting forever. | All async callers of this helper can now observe TaskCanceledException during teardown/scene unload; review cancellation handling. Stopping a coroutine or disabling a host without destroying it is a separate lifecycle case and is not claimed fixed. Coroutines that throw may also need separate completion/fault handling. | Interrupted-fixture cleanup and full PlayMode regressions; no dice result or gameplay outcome is supplied by cleanup. |

## Earlier gameplay baseline preserved and tested

Commit `5c3e29b` (before the migration authorization commit `9a85be6`) already
changed FirstTimePassManager, GroundBallManager, HeaderManager, HighPassManager,
LongBallManager, LooseBallManager, MovementPhaseManager and ShotManager for the
agreed GK entry/recovery rules. These were supplied gameplay changes, not rules
invented to make the migrated tests pass:

- Recoveries establish possession before attracting the ball; assess entry against
  the newly defending team. Possession changing while already inside does not
  create another entry.
- Tackle/header initial attraction does not offer GK movement or automatically
  transfer possession. Resolve direction/distance first and offer before the final
  loose-ball flight if it enters the defending box.
- An outside-box blocked shot has its attacking shot entry before scatter rolls.
- Accuracy-resolved aerial destinations, both directions, moved/previously beaten
  keepers, move/skip continuation and duplicate prevention are covered by the
  121-case GK PlayMode fixture plus eight isolated GK rule cases.

These rules affect nearly every ball-action manager, so future changes need both
affected entry cases and broad regressions. Other earlier UI, movement-highlight,
queued-steal, scene, font and time-setting edits were preserved; do not attribute
or revert all earlier commits as if they were six migration bug fixes.

## Expectation drift and test setup decisions

| Area | Decision during migration | Wider implication |
| --- | --- | --- |
| Difficulty2 ground/shot confirmation | Keep target-only preview and explicit commit introduced by `bd4347b`; test the current second confirmation. | Input scripts must distinguish preview from committed action. |
| Ground interception queues | Assert live shrinking pathInteractions, not unchanged preview defendingHexes (`d6fb721`). | Preview lists are not authoritative pending-action queues. |
| Kickoff and Final Third confirmations | Use Enter under `d276dfb`, replacing old Space/X confirmations. | Input documentation and synthetic callers must use current controls. |
| Long-ball scatter directions | Translate old zero-based indexes to input die faces1–6 (`12bb02e`), retain exact destinations. | Do not change direction mapping to fit obsolete rig inputs. |
| Foul inside opponent box | Penalty routing under `6ae88a0`, retaining bookings/injuries/substitutions and continuation assertions. | Restart type differs; card/injury totals must not be relaxed. |
| Free unchallenged header/control | Clear contested nominations and choose a fresh winner under newer033a audit and `bb2f0db`. | No stale player may silently remain nominated after defense forfeits. |
| Regular High Pass minimum | Fixed three-hex minimum under `5ff9abb`; retain separate keeper-kick configuration. | Preview, validation and UI must agree; old six-hex expectation was obsolete. |
| GK dive presentation | Duel win probabilities under `42982cc`, retaining feet xDr/xTk checks. | Presentation contract, not a different dive win rule. |
| Shot statistics | A direct unblocked goal is on target; old oracle only marked on target for successful saves. FK xG captures actual wall context after power, exact0.556 retained. | Future changes must distinguish shots blocked before target, saves, direct goals and correct expectation context. |
| GK-wall save after a free step | Shooting04 now supplies the existing saving opportunity introduced by `d6fb7218`; preserves the miss and original continuation. | A GK-wall Saving attempt is not an ordinary interception; don't conflate its statistics. |
| Off-target goal-kick ownership | Under `dbd5ed69`, retain shooter touch through shot/block inputs, then require new keeper taker and cleared prior touch. | Restart preparation deliberately starts a new touch chain. |
| Post-goal ownership | Assert purposeful touch before reset; require cleared touch after reset (`78355f9c`). | Old action ownership must not leak into kickoff. |
| Keeper sent off/replaced | Require removal from pitch, preserved replacement destination and replacement pause (`18b25f64`, `41b35186`). | Fixtures must not retain sent-off tokens as recovery targets. |
| Throw-in039a | Actually move the nearby eligible header receiver out of range before asserting automatic feet. | Forfeiting optional movement alone did not satisfy the old no-header precondition. |
| Original029 unfinished/manual branches | Replace explicit AssertTrue(false,"Break") stop with actual corner setup assertions. Cover applicable rushing-GK holds and non-rushing spills; don't manufacture own goals from a method that never asserted one. | Names and manual breakpoints do not define a finished rule oracle. |
| Asynchronous readiness | Wait for pace, input gates, distance readiness, Final Thirds and setup-resolution completion, rather than same-frame fields or arbitrary sleeps. | Preserve outcomes while accommodating the actual parent/child coroutine lifecycle. |
| Shot/scatter ownership | Final assertion audit checks all related/unrelated managers. ShotManager awaits ResolveLooseBall before ResetShotProcess, so the parent shot can remain active while its scatter child owns input. | Active manager flags are not necessarily exclusive modal/input ownership; investigate before changing lifecycle flags. |
| Authored presentation fixtures | Export only owned resolution panels and camera; scoped panel fingerprint avoids depending on unrelated Room layouts. | Presentation coverage requires explicit fixture regeneration after authored-panel changes. No live scene or Stats layout is restyled by tests. |

## Remaining limits

- Native PlayMode adapters currently run inside the Unity Editor and bridge to typed
  editor fixtures. Standalone Player test builds are not implemented.
- Deterministic scenarios cover specific rules and regressions; green tests do not
  establish all match combinations, hardware timing, long matches or save durability.
- The scorer rule above is resolved. Retain the confirmed own-goal policy and
  execute explicit attribution coverage; the other policy/API points remain review items.
- Full history, failed artifacts and per-original mappings remain in
  [MorningReview.md](MorningReview.md), [GameplayRegressionMigration.md](GameplayRegressionMigration.md)
  and [LegacyScenarioInventory.md](LegacyScenarioInventory.md).
