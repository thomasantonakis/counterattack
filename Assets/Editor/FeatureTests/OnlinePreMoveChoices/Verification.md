# Online choices before the first movement step

- [x] Permit moving instead of an initial nutmeg, using the real source click path.
- [x] Permit direct nutmeg-victim clicks before the first movement step.
- [x] Permit switching from a stationary keeper choice to another eligible defender.
- [x] Keep initial-nutmeg X rejection and existing stationary outfield offers unchanged.
- [x] Reject mandatory throw-in forfeits until the required defenders leave the zone.
- [x] Close the final authority hold gate, including appended uncommitted pass switches.
- [x] Exercise both teams/directions, real rolls/movement/continuation and negative guards.
- [x] Register all 32 new cases separately for the deferred Mac run.
- [x] Complete final affected checks and refresh Windows exports.

Full Room is v18, wire v7. This expands legal publication/typed input at existing
source boundaries and does not change the offline rules. The initial nutmeg offer
allows N, a direct victim click or an immediate legal movement destination; X
remains blocked by the explicit physical-key guard. A selected stationary keeper
can change to another defender without consuming the keeper's move. Its existing
non-forfeit dialog remains in place. Existing stationary outfield forfeit offers
are preserved pending the separate source-rule review in GameplayReview.md.

The required throw-in clearance predicate now guards both publication and execution.
Previously a forfeiting command could be advertised/accepted although the source
refused to end the phase. The final authority publication gate also clears every
late-appended action switch during pause, intro or transport hold. A stale pass
request cannot escape the hold; the same legal switch works again after recovery.

Compilation clean through the live Editor, zero C# errors. Final own-feature
results: 32 passed, zero failed/skipped, OnlinePreMoveChoicesPlayModeTests at
TestResults/GameplayRegression/Runs/20261116T224343764Z/results.xml. The corrected
pre-fix reproduction at 20261116T222547318Z had eight source-rule rejection passes
and 16 missing-choice/invalid-offer failures. The separate stale-switch hold
reproduction at 20261116T223336509Z had four failures before the final gate fix.
Those native XML/progress artifacts are retained.

Fixture corrections used the actual throw-in taker selection and avoided placing
one player over another before clearing the original cell. A draft expectation
that X forfeits an initial nutmeg was corrected after inspecting the full explicit
key guard; production rules were not changed to satisfy that expectation. Negative
cases are now named blocked-forfeit/offline-blocked-forfeit. Source entry points,
not injected expected flags, create all tested choices and outcomes.

All new cases are individually registered for the deferred Mac run, with fixture
inventory/hashes validated. No paired or whole-suite run occurred. For later
broader user checks use Assets/Tests/PlayMode/Features/OnlineGroundDuels /
OnlineGroundDuelPlayModeTests, OnlineThrowIns / OnlineThrowInPlayModeTests and
OnlineFullRoom / OnlineFullRoomPlayModeTests: movement decisions and holds are shared.


Affected native verification: 64 distinct passed, zero failed/skipped:
NutmegVictimSelectionKeepsOtherSteals two at 20261116T223742491Z;
OutsideKeeperChoices 24 at 20261116T223802500Z; ThrowInContinues ten at
20261116T223911634Z; OnlineMovementPlayModeTests five at 20261116T223954995Z;
OnlineManualControlsPlayModeTests seven at 20261116T224019116Z; final
OnlineStationaryDefenderPlayModeTests 16 at 20261116T224459749Z. Repeated earlier
stationary checks are not counted twice. Native XML/progress are under Runs.
The 13 report-tool unit tests also passed.

Windows exports refreshed with v18: normal build_ea484fcb26c9 at
20261116T224921Z-normal-Windows-build; Room build_03352080dd48 at
20261116T224958Z-Room-Windows-build; prepared build_cd7188d162b5 at
20261116T225020Z-Gameplay-Windows-build. All Succeeded, zero C# errors, nine
normal/two proof warnings and the known nonfatal EOS vswhere diagnostic (one
reported post-build error per profile). Actual paired internet acceptance and
latency remain deferred.
Packaged checks passed: Room local 20261116T225734726Z-Room-local, prepared
local 20261116T225748569Z-Gameplay-local, and Room login/liveness
20261116T225756191Z-Room-login. These local proof roles are not fresh-guest
isolation evidence; the dedicated full-scene native fixtures provide that.
