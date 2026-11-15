# Online shootout orders and state

- [x] Team-owned authenticated swaps and both-manager confirmation.
- [x] Reject offline bypass, ineligible/cross-team IDs, pause/connection-held input.
- [x] Preserve receipts across rebind and reject stale/conflicting requests.
- [x] Scene-authored own-order controls and inert guest orders/results projection.
- [x] Real staging and first kick, both directions and first-kicking teams.
- [x] Register new native cases, retain focused results and refresh builds.

Coin toss and match transitions remain separate integration work. No paired
internet execution is inferred from local fixtures.

Implementation policy: semantic control requests use the existing authenticated
receipt/revision lane with the distinct `shootout-order` decision ID. Each side
can swap only jersey IDs already in its actual order. Any swap invalidates both
confirmations; both must confirm the new state before the existing staging flow
starts. Existing reduce-to-equate/default-order rules remain unchanged. Shared
pause and connection holds block order changes; duplicate/rebound requests never
start staging twice. Legacy local start/update entry points cannot bypass this.

Initial native evidence: `20261115T215434991Z/results.xml` has 13 validation
passes; `20261115T215450062Z/results.xml` has eight real-scene order/first-kick
passes, both directions/first teams, equal/unequal teams. The first run
`20261115T215300621Z` had a fixture expectation for manual aiming; the actual
shootout rule selects a random target then waits for a shooter roll. No rule
was changed to satisfy that fixture. Later kick outcome checks exposed fixture
readiness (next shooter not yet assigned during celebration) and setup (the
existing -2 shootout save penalty must be included when setting up a tied save).
Those fixes preserve outcome/continuation assertions. Final results follow.

Full Room v6 adds detached shootout orders/eligibility/readiness, pre-shootout
scores, attempts/results, current shooter/keeper, first side/goal side, winner
and completion. The guest projects fields only; it does not set ActiveShootout,
start coroutines, choose random targets or consume dice. The large order overlay
clears during kicks; the compact match status carries the penalty score/attempts.
The existing shot/keeper-save adapters drive kicks; the existing loose-ball
adapter now takes priority during tied-save deflection. Full online completion
requires an explicit authority completion flag and no active motion/holds,
preventing the transient pre-tiebreaker MatchEnded state from closing transport.

Order reconciliation preserves valid preferred slots, adds newly eligible
playing players, keeps the existing reduce-to-equate size and invalidates both
confirmations on roster changes. Its shared revision advances before admission,
so a prior view cannot confirm the changed order. Ordinary substitutions remain
subject to the existing pause, stoppage and allowance rules.

`20261115T220425651Z/results.xml`: 19 EditMode passes, zero failed/skipped,
including explicit full-match completion/hold gates. The first-kick outcome
matrix `20261115T220440309Z/results.xml` has 16 passes, zero failed/skipped:
real shooter/save/deflection rolls, off-target/goal/save/tie, both first teams and
directions, next-shooter readiness, regulation-score isolation and fresh guest.
The earlier eight failed fixture cases remain in `20261115T215854703Z`.

`20261115T220944705Z/results.xml`: four paused-substitution/order reconciliation
cases passed. `20261115T221054182Z/results.xml`: eight complete real shootouts
passed, zero failed/skipped: either winner, early victory/sudden death and both
directions. All kicks use authenticated semantic shooter/save rolls with rigged
dice, followed by actual movement, winner celebration, completion gate and a
fresh encoded guest projection. Sudden death runs through twelve kicks; early
wins through six. No fake final score/attempt flags supply the expected result.

Final ordering rerun: `20261115T221402527Z/results.xml`, eight passed, zero
failed/skipped. End-panel reproducer `20261115T221631877Z/results.xml` failed
because the offline end panel hid the live scoreboard. The online shootout now
uses its own authored completion view, avoiding the offline UI relocation/hiding
path. Final complete-shootout rerun `20261115T221726352Z/results.xml`: eight
passed, zero failed/skipped, including scoreboard preservation for all branches.
All 55 native cases are individually registered for the deferred Mac verifier.

Affected focused native checks passed, all zero failed/skipped:
- Legacy shootout roll panel 6: `20261115T222039135Z/results.xml`.
- Legacy shootout UI feedback 6: `20261115T222100028Z/results.xml`.
- Legacy shootout instruction visibility 4: `20261115T222119156Z/results.xml`.
- Continuous-session policy 8: `20261115T222212023Z/results.xml`.
- Shared pause EditMode 11: `20261115T222219786Z/results.xml`.
- Full payload bounds 15: `20261115T222227776Z/results.xml`.
- Shared pause PlayMode 12: `20261115T222242651Z/results.xml`.
- Encoded full Room kickoff 4: `20261115T222330409Z/results.xml`.
All paths are under TestResults/GameplayRegression/Runs. Compilation separately
clean (recompile_status completed, failed=false, errors=[], compilationFailed=false).
Thirteen verifier tooling tests passed. No complete regression suite or native
Mac/internet pairing was run. Broader substitution interaction verification,
if requested, is `Assets/Tests/PlayMode/Features/OnlineSubstitutions/`, fixture
`OnlineSubstitutionsPlayModeTests`, category `OnlineSubstitutions`, because a
substitution can now invalidate shootout order confirmation; its full 18-case
fixture was not rerun in this batch. Four new real paused-substitution cases ran.

Windows exports succeeded:
- Normal eight scenes: `build_45f1828e126a`, `20261115T222605Z-normal-Windows-build/build-summary.json`.
- Room proof: `build_a14137e3d487`, `20261115T222642Z-Room-Windows-build/build-summary.json`.
- Prepared gameplay: `build_08d2830d18d5`, `20261115T222704Z-Gameplay-Windows-build/build-summary.json`.
Normal has nine existing warnings; proof exports two each. Each report retains
one known nonfatal EOS `vswhere.exe` discovery diagnostic; C# compilation is clean.
Packaged checks passed: `20261115T222719589Z-Room-local`,
`20261115T222732412Z-Gameplay-local`, `20261115T222740003Z-Room-login`.
Native login registered liveness notifications. These probes do not establish
paired full-game operation. All reports are under the same retained Runs root.
Unity-generated scene/meta whitespace is preserved; original Room/font/settings
edits remain outside this commit. Rebuild both full-game clients for Room v6.
