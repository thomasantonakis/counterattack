# Continuous online session boundary

- [x] Separate opt-in continuous mode from the short first-action proof.
- [x] Keep commands and publications alive across completed actions.
- [x] Distinct catalog identity; end only after match end and settled holds/motion.
- [x] Execute focused policy and real multi-pass coverage.
- [x] Run affected manual transport cases and refresh scripts/builds.

Compilation separately clean. Category `OnlineContinuousSession`: **8 EditMode
passed** in `Runs/20261114T104313622Z/results.xml` and **2 PlayMode passed** in
`Runs/20261114T134834652Z/results.xml`, zero failed/skipped. The PlayMode cases
complete a real ground pass and FTP in both directions and call the actual
exchange completion boundary at both snapshots. The default proof ends at
the first pass; continuous mode remains open through the subsequent FTP and
mandatory movement. Initial fixture-only boundary expectations failed twice
(2 cases each) in `20261114T104337663Z` and `20261114T134646867Z`; those XML files
are retained. The corrected fixture captures the real first-pass snapshot
rather than expecting the short proof to reach post-FTP movement.
Affected category `OnlineManualControls`: **7 PlayMode passed**, zero failed/
skipped, in `Runs/20261114T134924791Z/results.xml`. No complete suites ran.

Windows Room build `build_44d223ee7d28` Succeeded (7 warnings); prepared build
`build_8d79466d2fda` Succeeded (2 warnings). Both retain the known missing
vswhere.exe diagnostic (one report error, no script compile errors).
Packaged local checks passed in `Runs/20261114T140800012Z-Room-local/` and
`Runs/20261114T141031312Z-Gameplay-local/`, retaining build-report.json,
result.json and gameplay-evidence.json. Room login passed with liveness
notification registration in `Runs/20261114T141230392Z-Room-login/result.json`.

This removes the first-action transport limit; it does not load normal Room
rosters, implement remaining duels/restarts or establish full-match acceptance.
The default proof remains unchanged. Continuous mode has a two-hour watchdog,
not an automatic forfeit or AI takeover.

Opt-in manual commands after rebuilding both platforms:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Documentation/Setup/RunEOSStandaloneWindows.ps1 host -Room -Continuous
```

```sh
bash Documentation/Setup/RunEOSStandaloneMac.sh guest room continuous
# For reverse roles:
bash Documentation/Setup/RunEOSStandaloneMacHost.sh room continuous
```

Paired continuous operation and full-match completion remain unexecuted.
The one-command Mac verifier covers current native fixtures and automatic
paired probes; see Documentation/Setup/OnlineMacVerification.md.
