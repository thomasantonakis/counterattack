# Prompt for a future gameplay review session

Refreshed 2026-11-07 after the source review of `dd87cbd`. The older prompt's
uncommitted-work description and full-suite instruction have been superseded.

Copy the following into a new session:

```text
We are working in /Users/t.antonakis/Documents/Projects/Personal/counterattack.

Continue the wiki-to-gameplay review and discuss each discrepancy with me.
Preserve the shared AttributeScoreFloor rule: floor current injured attribute plus
all signed modifiers at zero before numeric dice; jackpots stay 50 and natural-one
misses/fouls stay. GK injuries reduce Aerial/Saving/Handling too. The existing
shooting penalty cap -2 is confirmed: include difficult-angle hexes, then cap
the combined penalty at 2. Do not reopen this decision.
Read that feature's Verification.md before quoting results.
First read the approved-decisions section in RulesWikiAudit and the
ApprovedRuleDecisions feature checklist. WIKI-003–008 have user decisions;
do not ask them again. HP/LB accuracy and Ball Control accept automatic jackpots;
outside-box GK challenges/rushes use Heading 0 and headed-pass continuation.
Keep the GK's separate nomination alongside two outfield defenders regardless of
position. Zero Heading has no extra distance penalty; the focused follow-up
category is OutsideKeeperHeaders. Read its latest results, not only the initial
approved batch's checkpoint.
The further gaps WIKI-009–013 are deferred until the approved batch is completed
and I explicitly resume their discussion. Older decision requests below are
historical and do not override the approved section.
Read AGENTS.md, the relevant Unity skills, and these documents first:
- Documentation/Testing/SessionHandoff.md
- Documentation/Testing/GameplayReview.md
- Documentation/RulesWikiAudit.md
- Documentation/Testing/GameplayRegressionMigration.md
- Documentation/Testing/MorningReview.md
- Documentation/Testing/RunningTests.md
- Documentation/Testing/LegacyScenarioInventory.md

Verify current git state, final migration status and actual native test artifacts
before claiming the migration is complete. Do not infer pass counts from discovery
or compilation. The known complete PlayMode result is362/0/0 in
TestResults/GameplayRegression/Runs/20261106T172436176Z/results.xml; the documents
may contain newer final EditMode/cleanup results. Inspect those too.

Check the current tree; the old test-only work was committed in c391477. The
interrupted shot audit's premature Final Third readiness assertion and explicit
own-goal branch attribution coverage remain pending. Do not automatically restart
migration or tests. The review at dd87cbd was source-only and made no gameplay
changes or new test results.

WIKI-003–008 are decided and implemented in the ApprovedRuleDecisions batch.
Read its Verification.md for actual focused results and remaining broader checks.
The extra-time cap of 3 remains open; current code keeps 7. Keep goalkeeper
priority on tied defensive scores. Preserve no-card/yellow inside-box goalkeeper
discipline and advantage after a booked defender's no-new-card foul. Eligible
accuracy/control jackpots succeed automatically; two duel jackpots tie at 50.
Outside-box GK challenges use Heading 0 and normal headed-pass continuation.

Only after I resume the further-gap discussion, investigate/clarify WIKI-009–013:
headed-pass landing recovery, header back to hands, last-man fouls, keeper
return/pickup eligibility and conflicting source wording. Compare the bundled
pinned English wiki with Advanced taking precedence and reckless tackles excluded.
Finish the individual-clause and diagram matrix before claiming parity. Present
evidence and proposed game/wiki correction; obtain a rule decision before changing
behavior. Existing tests can preserve behavior that differs from the wiki.

Keep these earlier migration decisions/questions separate from the new wiki
findings; don't change gameplay merely to match code or obtain green tests:
1. Verify the confirmed own-goal consistency rule; do not reopen it as unanswered:
   a rushing GK or outfielder tying the best attack, or blocking a shot, is the
   own-goal scorer when that defensive scatter enters their own goal. Tied GK
   save scatters follow the same rule; no-deflection failed saves remain attacker goals.
2. Already-booked/no-new-card stoppage is RESOLVED: no forced stop without a
   straight red, second yellow or second injury. Do not reopen it.
3. Defer own-box recovery/pickup questions until I resume further gaps.
4. Synthetic click acceptance when FTP changes state but returns false remains open.
5. Early defending kickoff front-four halfway policy remains open.

Review the six production changes in4cb471c separately from earlier supplied
keeper-rule changes in5c3e29b and from test-only drift/fixture corrections. Explain
why each was needed, its failing/passing evidence and effects on other actions.
Pay particular attention to shared box-entry memory, possession-before-attraction,
nested shot/scatter ownership, consecutive nutmegs, keeper registration and async
cancellation on scene teardown. Check the exact diffs and current callers, rather
than relying solely on the prose review.

For any rule we decide to change, first define the observable contract. Add new
meaningful Unity Test Framework regression cases per AGENTS.md, reproduce defects
where practical, make the smallest justified fix, and execute the new cases plus
focused feature tests, following AGENTS.md. Hand broader affected-folder/full-suite
checks to me unless I explicitly request that you execute them in this task.
Do not claim full regression validation from focused tests or historical runs.
Keep prior coverage; record approved expectation changes and results.

Use only the already-running Unity Editor through CLI/Pipeline. I authorize native
EditMode and PlayMode test runs and their framework-managed PlayMode transitions.
Do not launch Unity, use batchmode/GUI scripting, or ordinary editor_play. Resolve
any active/pending test run before another run or code edit. Preserve all existing
edits, designer-owned Stats layouts and live saves. No permission questions for
these already-authorized routine actions; rule clarification is welcome.
```
