# Mac verification follow-up, 2026-11-17

- [x] Fetch and inspect the published Mac report without merging its branch.
- [x] Inspect native XML, login evidence and Windows host/system events.
- [x] Correct obsolete cleanup expectation and run exactly the four reported cases.
- [x] Add focused/skip-native verifier options, login gating and host request expiry.
- [x] Validate 17 tooling cases, PowerShell syntax and clean Unity compilation.
- [x] Refresh exports/local/login checks; commit and repin the Windows supervisor at handoff.
- [ ] Run the short Mac retry when the user has the Mac available.

Original report: verification/sons-mac-20261117T111338Z-81219,
Documentation/Testing/MacReports/20261117T111338Z-81219.json (15,184,506 bytes).
Source ec3839ff16b3c5529e8cc4e039013d27e05fa936. Native counts: 1,736 passed,
four failed, one skipped. All three Mac exports and both packaged local checks
passed. The skipped native case is EOSProbeSessionReuseEditorTests, whose one-use
marker was absent; a fixture-level passed line does not establish its live login.

The four ground-pass failures were the mirrored/host-role cases in
OnlineGroundPassPlayModeTests.UnauthorizedAndStaleCommandsCannotMutate.
The obsolete assertion expected every action family to disappear after ground
cleanup; high/long/movement choices correctly remain. The revised fixture checks
no ground actions remain, all three other families remain, old confirmation is
StaleRevision, the pass stays inactive and ball/statistics do not change.
No gameplay rule changed. Focused corrected run: 20261117T140512374Z/results.xml,
four passed, zero failed/skipped; compilation separately clean.

Packaged Mac EOS login failed at CreateDeviceId, before authenticated login.
The old message omitted ResultCode, so the exact cause is unknown. New messages
include the EOS enum for device creation, user creation and login. No credentials
or identity data are added to reports. This is diagnostics, not a claimed fix
for Mac identity creation; a short Mac retry is still required.

Windows Power-Troubleshooter event 1 confirms sleep 12:53:35.7333885Z through
13:56:58.9342922Z; Kernel-Power event 42 says Button or Lid. All four Mac paired
waits occurred during this interval, so no cross-machine exchange ran. The old
supervisor accepted requests for two hours, longer than the guest's ten-minute
wait, and started an obsolete request after wake. The four completed Mac
requests have been cancelled on verification/queue; the managed late probe
retired through its own launcher. No Editor was killed.

Requests now carry expiresUnix and the host enforces that deadline. Legacy
requests expire after ten minutes. A failed packaged login skips both host waits
while retaining local checks. The host publishes launcher failures rather than
leaving a remote running status. Existing transient status-poll retries remain.
The initial CLI status alias was replaced by the live editor_status endpoint.

Future short run (pull current branch first; open the Mac Editor manually):

    bash Documentation/Setup/VerifyOnlineMac.sh --fixture OnlineGroundPassPlayModeTests --host-wait 180 --attempts 1

This reruns only the affected fixture, builds all three profiles, runs login/local
and attempts pairing only after successful login. It does not rerun all 127
fixtures. To omit native tests explicitly, use --skip-native instead of --fixture.
Focused reports state that scope; they do not replace the historical full result.
Keep the Windows laptop awake with its lid open for the arranged paired window;
thread idle-sleep prevention cannot defeat an explicit lid/button sleep action.
The normal-game human walkthrough and actual latency remain unverified.

Windows exports succeeded: 20261117T140716Z-normal-Windows-build,
20261117T140751Z-Room-Windows-build, 20261117T140813Z-Gameplay-Windows-build.
Each retains the known nonfatal missing-vswhere post-build diagnostic; no C#
compilation errors. Packaged checks passed: 20261117T140833133Z-Room-local,
20261117T140846890Z-Gameplay-local, 20261117T140854526Z-Room-login (liveness true).
Seventeen tooling tests passed on Windows; the PowerShell host-window case is
intentionally Windows-only when the same tooling collection runs on macOS.
All four new tooling checks and the four revised native cases have individual
manifest rows. No whole Windows suite or new paired attempt was run.


Paired follow-up, 2026-11-17: verification/sons-mac-20261117T141510Z-88348
passed all 14 selected native cases, all three Mac builds, EOS login and both
local checks. Both actual Windows-host/Mac-guest probes also passed over
ForceRelays with matching digests, one host roll, pause revision 2 and inactive
guest rules. The original verifier marked pairing failed because the Windows
final report (~53 KB before base64) exceeded the Windows command-line limit.
Both Windows result files were preserved locally as passed; their complete
reports have now been republished successfully and checked remotely.
The original failed Mac report remains unchanged. Explicit reconciliation is
retained in TestResults/GameplayRegression/Runs/20261117T141054275Z-WindowsVerificationHost/20261117T141510Z-88348-paired-comparison.json.
Host uploads now use a temporary UTF-8 --input file; 19 tooling tests pass,
including a >32 KB Unicode payload, create/update SHA behavior and failed-upload
cleanup. This is a tooling-only repair; Unity source/builds are unchanged.
No new Mac run is required to establish these two recovered probe passes.
Normal-game human Host/Join -> team/kit agreement -> Draft -> full Room play,
actual network interruption/recovery and measured latency remain unverified.
These paired attempts recorded recoveryGenerations=0; do not claim reconnect.
