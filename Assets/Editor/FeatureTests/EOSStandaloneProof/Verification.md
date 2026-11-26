# Standalone EOS proof checklist

- [x] Author an isolated edit-mode scene, restoring the clean Room scene.
- [x] Keep login, exact-ID lobby coordination, ForceRelays readback and exact
  bidirectional packet verification independent of ordinary gameplay.
- [x] Validate explicit roles, required input, fresh evidence directories and
  invalid arguments before native/network operations.
- [x] Compile through the live Windows Editor: zero script errors/warnings.
- [x] Focused category `EOSStandaloneProof`: 9 passed, 0 failed, 0 skipped.
  Native XML: `TestResults/GameplayRegression/Runs/20261111T205334283Z/results.xml`.
- [x] Export the dedicated Windows probe scene through live Pipeline.
- [x] Windows standalone login passes.
- [x] Mac standalone build and local login pass (user supplied reports).
- [x] Separate-network standalone Windows host / Mac guest packet reports pass.
- [x] Verify handoff removal and record platform and both report paths.
- [ ] Record Mac CPU/build architecture and minimum OS; these were not captured.

The first Windows export reported Succeeded, with 7 warnings and one diagnostic
error about missing Visual Studio `vswhere.exe`. The application did launch,
but native initialization failed before login. Initial report:
`TestResults/GameplayRegression/Runs/20261111T205549206Z-Standalone-login/result.json`.
The diagnostic rerun returned `AlreadyConfigured`:
`TestResults/GameplayRegression/Runs/20261111T205911371Z-Standalone-login/result.json`.
The Windows native Gfx plugin owns initialization/platform lifetime before
managed scene startup. The probe now borrows that platform without a second
Initialize, Release or Shutdown. macOS retains explicit managed ownership.
The rebuilt Windows application passed its login probe:
`TestResults/GameplayRegression/Runs/20261111T210221320Z-Standalone-login/result.json`.
The final export still reported Succeeded with 7 warnings and the same
vswhere diagnostic. This is a Windows packaging/login pass, not a paired
standalone packet proof. Reports and Player.log remain in each local run folder.
No full suites or ordinary gameplay were run. No gameplay code was changed.
Instructions: `Documentation/Setup/EOSStandaloneProof.md`.

## Completed separate-network standalone run

The user reported the family Mac remained on the hotspot for this proof.
Mac export reported Succeeded, zero errors and nine warnings (obsolete UI APIs,
enum value and disabled runtime Pipeline). The Bash 3 empty-array launcher bug
was fixed without rebuilding the application. Mac local login passed:
`TestResults/GameplayRegression/Runs/20261111T210903Z-Standalone-login-65730/result.json`.
Windows final rebuilt login also passed:
`TestResults/GameplayRegression/Runs/20261111T210531441Z-Standalone-login/result.json`.

Windows standalone host **passed**, inspected directly in its result.json and
launcher completion (exit 0):
`TestResults/GameplayRegression/Runs/20261111T211010725Z-Standalone-host/result.json`.
Mac standalone guest **passed**, per the user-supplied completed report:
`TestResults/GameplayRegression/Runs/20261111T211038Z-Standalone-guest-65900/result.json`
(file remains on the Mac). Platforms were WindowsPlayer and OSXPlayer, both
Unity 6000.5.3f1, both reporting ForceRelays. Each application verified its
received exact packet, peer/socket/channel and completed lobby cleanup.
The Windows helper removed the temporary standalone handoff; remote directory
inspection confirmed it was absent. The user observed a Windows network-access
prompt; both reports passed, so this run does not require a retry for that alone.

This establishes standalone Windows-host/macOS-guest EOS packaging/login and
two-way relay-only packets on user-reported separate networks. It does not
establish reverse roles, other platform pairs, gameplay synchronization,
recovery, measured latency, per-connection route inspection, signing/notarization
or minimum supported OS/architecture. Framework validation remains 9 passed,
0 failed, 0 skipped; standalone probes are separate application reports.
