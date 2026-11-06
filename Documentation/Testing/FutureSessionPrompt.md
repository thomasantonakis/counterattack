# Prompt for a future gameplay review session

Copy the following into a new session:

```text
We are working in /Users/t.antonakis/Documents/Projects/Personal/counterattack.

Review the gameplay decisions from the GameTestScenarioRunner migration with me.
Read AGENTS.md, the relevant Unity skills, and these documents first:
- Documentation/Testing/GameplayReview.md
- Documentation/Testing/GameplayRegressionMigration.md
- Documentation/Testing/MorningReview.md
- Documentation/Testing/RunningTests.md
- Documentation/Testing/LegacyScenarioInventory.md

Verify current git state, final migration status and actual native test artifacts
before claiming the migration is complete. Do not infer pass counts from discovery
or compilation. The known complete PlayMode result is362/0/0 in
TestResults/GameplayRegression/Runs/20261106T172436176Z/results.xml; the documents
may contain newer final EditMode/cleanup results. Inspect those too.

First discuss intended rules with me; don't change gameplay just to match existing
code or obtain green tests. Prioritize:
1. Verify the confirmed own-goal consistency rule; do not reopen it as unanswered:
   a rushing GK or outfielder tying the best attack, or blocking a shot, is the
   own-goal scorer when that defensive scatter enters their own goal. Tied GK
   save scatters follow the same rule; no-deflection failed saves remain attacker goals.
2. Does a previously booked defender force taking a later foul even without a new card?
3. Is every own-box GK recovery meant to enter Save and Hold?
4. What does synthetic click acceptance mean when FTP changes state but returns false?
5. Is the early defending kickoff front-four halfway policy intended as implemented?

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
affected regressions. Run both complete suites before claiming full regression
validation. Keep prior coverage; record approved expectation changes and results.

Use only the already-running Unity Editor through CLI/Pipeline. I authorize native
EditMode and PlayMode test runs and their framework-managed PlayMode transitions.
Do not launch Unity, use batchmode/GUI scripting, or ordinary editor_play. Resolve
any active/pending test run before another run or code edit. Preserve all existing
edits, designer-owned Stats layouts and live saves. No permission questions for
these already-authorized routine actions; rule clarification is welcome.
```
