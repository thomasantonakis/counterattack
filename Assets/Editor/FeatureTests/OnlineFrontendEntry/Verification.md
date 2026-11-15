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
