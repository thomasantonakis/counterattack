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

**No Windows native initialization, Windows/macOS packet exchange, standalone
build, per-connection route introspection, or playable online match is
verified yet.** The macOS Editor compiled the Windows adapter conditional
but cannot validate the Windows branch of that code.

## Next session actions

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
