# Normal multiplayer entry and persistent frontend session

- [x] Main Menu multiplayer opens Create New Game in explicit Online mode.
- [x] Author Host/Join/Copy Code/Leave and connection status in the existing scene.
- [x] Author a nonvisual persistent transport prefab; no runtime-created UI.
- [x] Bind real EOS lobby/P2P lifetime to authenticated frontend channels.
- [x] Direct-first routing with relay fallback; existing proof defaults stay relay-only.
- [x] Preserve offline selections, save context and draft-return PlayerPrefs.
- [x] Reject malformed join codes before starting native login.
- [x] Block local save/Draft entry until authenticated agreement.
- [x] Retain redacted session reports under persistentDataPath/OnlineSessions.
- [x] Focused compilation, native tests and per-case deferred Mac entries.
- [ ] Paired Windows/Mac actual Host/Join acceptance (explicitly deferred).
- [ ] Authoritative Draft handoff and normal full Room lifecycle.
- [ ] Native transport recovery beyond current 15-second disconnect failure.

Compilation: live Editor recompile completed with zero errors, separately from tests.

Native evidence under `TestResults/GameplayRegression/Runs/`:

- `20261115T052747505Z`: 12 EditMode passed, 0 failed/skipped.
- `20261115T052811003Z`: 5 PlayMode passed, 1 fixture expectation failed,
  0 skipped. The fixture incorrectly assumed blank offline names enable Create.
- `20261115T052930597Z`: corrected offline case passed, plus the existing
  `OnlineResultHoldPlayModeTests.OfflineCountdownStillUsesUnscaledTime` passed
  because the substring filter also selected it. 2 passed, 0 failed/skipped.

Aggregate own feature: 18 distinct passed cases (12 EditMode, 6 PlayMode).
The corrected fixture checks blank-name rejection and then real acceptance of
valid offline selections. Gameplay was not changed to satisfy that expectation.
An intervening quoted exact-name filter matched zero tests; it provides no evidence.

Authored controls are hidden offline. The session keeps EOS ticking in background,
checks remote product user/socket/channel before decoding, uses bounded queues,
shows measured RTT when samples exist, and owns cleanup across scene changes.
Actual internet latency has not been measured. Current exports predate this slice.
Unity-generated serializer trailing whitespace was retained in the saved scene.
Unrelated Room, fonts and project settings are excluded.

Run only `OnlineFrontendEntryEditorTests` (EditMode) or
`OnlineFrontendEntryPlayModeTests` (PlayMode). The fixture isolates/restores the
application mode and draft-return/settings PlayerPrefs; it does not start native
login or mutate live saves. Existing framework callbacks suppress the legacy runner.
For broader user-run checks: `Assets/Editor/FeatureTests/OnlineSetupClient/`
(`OnlineSetupView` category), `OnlineSetupAgreement/` (`OnlineSetupAgreement`),
and `OnlineSessionTransport/` (`OnlineSessionTransport`) cover agreement/view and
wire behavior affected by future entry/lifetime changes. No full suite was run.

Normal-scene Windows export `build_06972049607d` succeeded at
`20261115T053740Z-normal-Windows-build/build-summary.json` (9 warnings,
one known nonfatal EOS post-build vswhere.exe diagnostic, zero C# errors).
Output: `Build/OnlineGame/Windows/Counterattack.exe`; no ordinary match was
launched and no full multiplayer acceptance is claimed. The Mac one-command
verifier now also builds the normal game scenes through
`BuildEOSStandaloneMac.sh game`. Its 13 tooling tests and Bash syntax check
passed on Windows; actual macOS export remains deferred.

Real production Host-button acceptance: 1 native PlayMode pass, 0 failed/skipped
at `20261115T054507321Z`. The existing scene controls captured valid selections,
created an EOS private lobby with AllowRelays, then cancelled without a guest.
The test verified transport cleanup, background setting restoration, guarded
local save and the retained redacted session report. This is not a paired join,
remote recovery or RTT measurement. The feature now has 19 distinct passed cases.
Its dedicated fixture/category is `OnlineFrontendNativeLobbyPlayModeTests` /
`OnlineFrontendNativeLobby`; it requires this machine's local EOS configuration.
It is separately registered for the deferred Mac run. Native result/report are
retained beside the XML in the run directory.

Latest proof exports also succeeded: Room `build_6c6089c7a517` (05:42:43 UTC),
prepared `build_92273852a34c` (05:43:03 UTC), each 2 warnings and the known
nonfatal vswhere.exe post-build diagnostic. Packaged checks passed:
`20261115T054303870Z-Room-local`, `20261115T054325787Z-Gameplay-local`,
`20261115T054339522Z-Room-login` (liveness notifications registered).
Build summaries and native evidence are under TestResults/GameplayRegression/Runs.
The normal-scene export and these proofs contain the same production C# changes;
the subsequent added native fixture is Editor/test-only. Full Draft/Room remains.
