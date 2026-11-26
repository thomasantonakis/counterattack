# Online opening coin toss

- [x] Host-resolved team-owned rolls and ties after both rosters are confirmed.
- [x] Reuse CoinTossDecision for winner kickoff/direction and opponent continuation.
- [x] Freeze roster edits, reject wrong team/invalid choices, preserve choices on rejoin.
- [x] Publish detached MatchOpeningSetup for the full Room handoff.
- [x] Author and execute the normal Draft opening controls.
- [x] Run focused native tests, refresh affected Draft results and Mac inventory.

Room startup remains separate work; a completed opening snapshot alone does not
prove a remotely playable full Room match. Paired Mac execution stays deferred.

Compilation clean through the live Editor. Native own-feature evidence:
20 EditMode passes at `20261115T063139379Z`, four actual authored Draft scene
passes at `20261115T063248358Z`, zero failures/skips. Each case has its own Mac
manifest entry. Runtime fields preserve the real CoinTossDecision result, each
side's first-half direction and kickoff team. The host cannot edit the confirmed
rosters during opening. Rebind preserves opening consent/choice without rerolls.
Kickoff and both direction buttons are scene-authored and owned by the chooser.

Affected Draft coverage passed: authority40 at `063432294Z`, transport16 at
`063440855Z`, variants38 at `063449987Z`, actual scene20 at `063511578Z`
(all with 20261115T prefix). Existing completion assertions now expect the
opening-roll phase; the new feature tests cover continuation through room-ready.
These are intended new behavior assertions, not removed/disabled legacy tests.
Native XML/progress is retained under TestResults/GameplayRegression/Runs.
Frontend rules version is v4. Exports predate Draft/opening and need refreshing.
Actual Windows/Mac opening and full Room initialization remain unverified.
Broader existing user-run check: `Assets/Editor/DraftOrderPresentationEditorTests.cs`
fixture `DraftOrderPresentationEditorTests`, for the reused coin-toss rule/UI.
No whole EditMode or PlayMode suites were run.
