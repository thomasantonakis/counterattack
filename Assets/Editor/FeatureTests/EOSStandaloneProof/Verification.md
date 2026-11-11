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
- [ ] Mac standalone build and local login pass.
- [ ] Separate-network standalone Windows host / Mac guest packet reports pass.
- [ ] Verify handoff removal, record platform/architecture and both report paths.

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
