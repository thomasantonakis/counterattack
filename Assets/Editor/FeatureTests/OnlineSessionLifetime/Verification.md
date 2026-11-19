# Online authentication renewal and native callback lifetime

- [x] Preserve the actual hour-boundary InvalidAuth and Mac native crash evidence.
- [x] Add expiry-driven same-identity Connect login renewal with bounded retries.
- [x] Pump renewal on every normal frontend/gameplay/packet-probe tick.
- [x] Retire owned P2P subscriptions before native platform release; late disposal is inert.
- [x] Dispose nested Room exchange before frontend exchange teardown.
- [x] Compile cleanly and execute 35 OnlineAuthRenewal feature cases.
- [x] Verify actual packaged login/renewal and clean shutdown; retain results.
- [ ] Validate a real hour-boundary session and repeat focused Mac checks later.

Human reproduction: frontend started 2026-11-19T13:32:04.0448638Z and failed
InvalidAuth at 14:32:04.3255009Z. Mac diagnostic branch
verification/sons-mac-20261119T144722Z-session-93237 contains a SIGSEGV in
EOS_P2P_RemoveNotifyPeerConnectionInterrupted. A gameplay subscription captured
the P2P pointer but could be disposed after its owning Mac platform was released.
Windows borrowed its platform and therefore did not expose the same release bug.

The native resource owner retires notification leases before releasing the
platform, supports early cleanup and makes later iterator disposal harmless.
Renewal processes SDK notices outside native callbacks, never creates/rebinds a
product user on renewal, and surfaces failures through the regular managed pump.
The local SDK says expiry notices arrive about one minute before expiry: retry
work is bounded to 50 seconds, individual callback waits to 20 seconds, retry
spacing five seconds. It preserves one in-flight refresh and rejects identity
changes, old replies and disposed callbacks. No credentials are logged.

Focused final feature run: 20261119T150007534Z/results.xml, 35 passed, zero
failed/skipped. Earlier helper-only run 20261119T145710207Z: 24 passed.
Affected EOSStandaloneProofEditorTests: nine passed at 20261119T150137929Z.
Native authored host/cancel case also passed; see its retained result XML.
Isolated Windows auth export 20261119T150504Z-Auth-Windows-build succeeded;
known nonfatal vswhere post-build diagnostic retained, zero C# compile errors.
Actual native quick login/renewal: 20261119T150545278Z-WindowsAuthSoak/result.json
passed with authenticationRefreshes=1 and liveness registration true.
A 3,900-second real expiry-window soak started at
20261119T150630795Z-WindowsAuthSoak and is pending. It must observe at least
one SDK-notified refresh in addition to the forced initial proof refresh.
No real hour-boundary pass is claimed yet. No new paired/Mac run executed.
The original Mac crash collector found one native SIGSEGV, whose first symbol
is EOS_P2P_RemoveNotifyPeerConnectionInterrupted. Its bootstrap matches the
Windows match, preserving run attribution; raw collector report is retained.

Final callback teardown also uses raw native ticks for the short lobby-cleanup
pump, so an already-recorded auth-renewal failure cannot prevent session Dispose.
Normal gameplay/session loops use the checked renewal pump. Core/native argument
coverage is 35 cases; nine existing argument cases and the native host/cancel
case passed. Remaining acceptance: completed real soak and a rebuilt Mac run.

Final post-cleanup compilation is clean. Repeated final native feature: 35 passed at 20261119T151243870Z/results.xml. Normal/Room/gameplay exports still predate this batch; refresh before paired use.
