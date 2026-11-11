# Counterattack online work: Windows Boot Camp handoff

Start Codex from a fresh Windows checkout of branch
`codex/online-foundation-personal-mac` in **PowerShell**, then say only:

> Read `Documentation/Setup/OnlineNextSessionPrompt.md` and continue the
> focused Windows-host EOS connectivity proof.

This file is committed so it is available after booting into Windows. Follow
`Documentation/Setup/WindowsBootCamp.md` for installation, sign-in, checkout,
Unity, local EOS configuration, and the exact Windows-host/family-Mac-guest
commands. Use PowerShell, not Git Bash or WSL, for Windows steps.

## Current evidence

The online foundation remains isolated from gameplay. The provider-independent
protocol fixture passed 27/27. EOS Connect login, private lobby creation,
two-Mac exact-ID lobby joining, and exact two-way P2P packet exchange passed
in focused Editor tests. With both peers configured for `ForceRelays`, the
packet test passed 1/1 on each Mac across user-reported separate networks in
both network arrangements. In the latest run, the personal Mac was on an
iPhone hotspot and the family Mac was on home Wi-Fi. Latest native XML:

- Personal Mac host:
  `TestResults/GameplayRegression/Runs/20261111T163354297Z/results.xml`.
- Family Mac guest, on that machine:
  `TestResults/GameplayRegression/Runs/20261111T163544902Z/results.xml`.

The focused EOS session-reuse test passed 1/1 at
`TestResults/GameplayRegression/Runs/20261111T163147731Z/results.xml`.
The subsequent host test passed in the same personal Mac Editor process
without a Unity restart. The current helper keeps the native EOS SDK
initialized between focused runs; it releases each platform handle and
shuts EOS down when the Editor quits. The one-use lobby handoff was removed
after the run. Full evidence is in
`Assets/Editor/FeatureTests/OnlineEOSConnectivity/Verification.md`.

Windows native initialization and two sequential device logins passed 1/1 on
2026-11-11 after fixing duplicate platform-adapter registration. Native XML:
`TestResults/GameplayRegression/Runs/20261111T203927659Z/results.xml`.
Windows/macOS relay-only packet exchange also passed 1/1 on each peer:
Windows host `TestResults/GameplayRegression/Runs/20261111T204115387Z/results.xml`;
family Mac guest (user-reported result, XML on that Mac)
`TestResults/GameplayRegression/Runs/20261111T204153959Z/results.xml`.
The user confirmed that initial run used the same network. A separate-network
rerun passed 1/1 on each peer with the family Mac on a hotspot and Windows on
its original network. Windows host native XML:
`TestResults/GameplayRegression/Runs/20261111T204607920Z/results.xml`.
Family Mac guest XML (user-reported completed pass, file on that Mac):
`TestResults/GameplayRegression/Runs/20261111T204630735Z/results.xml`.
The temporary remote handoff was confirmed removed after the rerun.
Standalone Windows-host/macOS-guest relay-only packets subsequently passed on
user-reported separate networks (family Mac hotspot). Windows report:
`TestResults/GameplayRegression/Runs/20261111T211010725Z-Standalone-host/result.json`.
Mac report, user supplied and retained on that machine:
`TestResults/GameplayRegression/Runs/20261111T211038Z-Standalone-guest-65900/result.json`.
The temporary standalone handoff was confirmed removed. Reverse roles, other
platform pairs, per-connection route introspection and playable online gameplay
remain unverified.

## Next session actions

Latest work connects the ground-pass gateway to authenticated EOS semantic
commands/state and an authored guest replica scene. Read
`Documentation/Setup/OnlineGroundPassNetwork.md` and
`Assets/Editor/FeatureTests/OnlineGroundPassNetwork/Verification.md` first.
The focused category passed 13 EditMode + 2 PlayMode cases; Windows packaged
local gameplay and EOS login passed. Next prepare the family Mac with
`BuildEOSStandaloneMac.sh gameplay`, then `RunEOSStandaloneMac.sh login gameplay`
and `RunEOSStandaloneMac.sh local gameplay`. Only after both pass start Windows
`RunEOSStandaloneWindows.ps1 host -Gameplay`; after Gameplay handoff published,
run the Mac `RunEOSStandaloneMac.sh guest gameplay` on the hotspot. The paired
gameplay result is pending. The earlier raw-packet proof is already complete.
Do not claim full M1: interactive Room input, shared pause ownership and actual
disconnect freezing/resync are still unimplemented.

The latest gameplay batch added `OnlineGroundPassAuthority`, an isolated
semantic adapter to real pass selection/confirmation/interception and
continuation, with detached state publications. Final focused native results:
1 EditMode pass at `Runs/20261111T212209215Z/results.xml` and 14 PlayMode passes
at `Runs/20261111T212340273Z/results.xml`, beneath
`TestResults/GameplayRegression/`; zero failures/skips, clean compilation.
Read `Assets/Editor/FeatureTests/OnlineGroundPass/Verification.md` before
continuing. Next: authenticated EOS command/state transport, authored guest
replica startup/input ownership, shared pause and active-resolution recovery.
The gateway is not installed into Room and M1 is not yet complete. Do not claim
the packet proof already carries gameplay or independently simulate guest rules.

The Editor and initial standalone Windows-host/Mac-guest proofs are complete.
Repeat instructions are in `Documentation/Setup/EOSStandaloneProof.md`.
Windows application build and local login passed; latest report:
`TestResults/GameplayRegression/Runs/20261111T210221320Z-Standalone-login/result.json`.
Build diagnostics include missing Visual Studio vswhere.exe, although export
reported Succeeded and the application ran. Mac build/login and the paired
standalone relay run passed; see the feature verification document for the
retained reports and scope. Next assess the remaining M0 gates in the online
plan before the M1 gameplay proof. The older Editor checklist below is retained
for reruns.

1. Read `AGENTS.md`, `Documentation/Setup/WindowsBootCamp.md`,
   `Documentation/Plans/EOSSetup.md`,
   `Documentation/Plans/OnlineMultiplayer.md`, and the EOS verification file.
   Resolve the Windows checkout root with `git rev-parse --show-toplevel`; pass
   it explicitly on Unity CLI calls. Preserve any local changes.
2. Confirm the open Windows Unity Editor is the exact checkout, version
   6000.5.3f1, with a ready Pipeline connection. Check compilation, Play Mode,
   every loaded scene's dirty state, and pending tests before a focused run.
   Never launch Unity from Codex, enter ordinary gameplay, use batchmode,
   modify a loaded scene file on disk, or run whole test suites.
3. Enter the same EOS development product/deployment/desktop client locally
   for **Windows** through the plugin configuration UI. Keep credentials and
   Windows device identity storage local and Git-ignored. Do not ask the user
   to paste IDs or secrets into chat or copy another machine's identity.
4. Run only `EOSProbeSessionReuseEditorTests` first. Inspect the completed
   result and native XML. Diagnose Windows compile/native failures before
   continuing. Then the Windows machine is the **host** and the family Mac
   is the macOS **guest**. Use `RunEOSWindowsHost.ps1 relay` on Windows and
   `RunEOSGuest.sh relay` on the family Mac as documented in the Windows
   guide. Keep the machines on different networks for that proof.
5. Record actual passed/failed/skipped counts and native XML paths from both
   machines. Remove the one-use handoff after the run; the Windows host helper
   handles this automatically. Distinguish evidence from inference. Do not
   claim Windows support from a macOS-only compile or a queued send call.

The family Mac checkout is
`/Users/vasilisantonakis/Documents/Projects/Personal/counterattack`.
Give the user short pull/run commands for it when needed. The repository
owner has authorized necessary Git add, commit, fetch, pull and push commands
without repeated permission requests; `AGENTS.md` records this. Commit only
task-related files. The personal Mac checkout has unrelated local font asset
edits and `Assets/Scripts/ActionScripts/obj.meta`; preserve them.
