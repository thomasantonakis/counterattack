# Online setup agreement

- [x] Team-owned names/outfield/GK kits, shared settings, both ready and host start.
- [x] Edits/reconnections invalidate agreement, detached snapshots and bounded receipts.
- [x] Existing Create New Game validation and side-effect-free settings capture.
- [x] Bound Create button sends settings; unbound online guest cannot save/load Draft locally.
- [x] Focused native cases and individual deferred Mac entries.

Compilation separately clean. `OnlineSetupAgreementEditorTests`, category
`OnlineSetupAgreement`: **44 distinct cases passed**, zero failed/skipped.
Initial 42/0/0 at `Runs/20261114T225451692Z/results.xml`; final five authored-screen
cases (including three overlaps) at `Runs/20261114T225819650Z/results.xml`.
Do not add overlapping run counts. Affected `OnlineMatchProtocolEditorTests`:
27/0/0 at `Runs/20261114T225900222Z/results.xml`.

Uses authenticated reserved teams, both host assignments, independent setup
sequences and payload-aware bounded receipts. Client payloads never name their
team/account. Configuration changes clear agreement; connection lifetime changes
also clear agreement, including a rebind between observations. Retired connection
commands are rejected. Only the actual host can change shared settings or advance
after both players agree. Captures and accepted inputs do not expose mutable state.

Validation reuses the Create New Game kit-clash checks and its international team
catalog. Setup validates names, ranges, modes, player pools, referee, weather,
lighting, ball and kit types. Human team control overrides injected CPU settings.
Random referee stays unresolved during capture; a later host transition must
resolve it exactly once and publish the resolved configuration.

The existing Create New Game settings builder was extracted without changing its
offline save flow. `TryCaptureOnlineSetupSettings` reads the authored controls
without writing a save, changing scenes or drawing guest referee randomness.
`BindOnlineSetupConfirmation` routes the existing button to the owning session.
Tests inspect the existing scene in a temporary preview asset context and restore
it, the RNG and UI ownership. No runtime visual structures were added.

**Integration boundary:** setup transport, authored host/join/status controls,
a persistent session across scenes, Draft authority and normal Room menus remain
follow-ups. No connected Create New Game, paired or full-match acceptance claim.
Every one of the 44 cases is registered for explicitly deferred Mac execution.
No full suites ran. Broader user-run checks: `Assets/Editor/FeatureTests/DraftSettingsRules/`
fixture `DraftSettingsRulesEditorTests` (existing setup and draft constraints).
