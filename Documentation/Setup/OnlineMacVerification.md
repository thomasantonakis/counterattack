# One-command son's Mac verification

Prepared for later use. The user will explicitly request the Mac run and host
supervisor; neither should run while independent gameplay development continues.
OnlineMacTestManifest.json is the maintained inventory. Every new test/case is
appended there, and fixture definition hashes prevent silently missing additions.

Follow `PersonalMac.md` once for GitHub/Unity/EOS setup. Manually open this
checkout in Unity 6000.5.3f1 and stop ordinary Play Mode. Do not copy account
configuration from Windows. Keep the Mac awake and online during the run.

From the Mac checkout, paste this single line:

```sh
git pull --ff-only origin codex/online-foundation-personal-mac && bash Documentation/Setup/VerifyOnlineMac.sh
```

The verifier does not launch Unity, reset edits, change branches or run all
gameplay suites. It discovers implemented `Online*` fixtures, runs each fixture
serially through the open Editor, builds both Mac exports, checks packaged login
and local gameplay, then requests automatic Windows-hosted Room and prepared-board
probes. Legacy one-use Editor EOS probes are covered by the packaged login/lobby/
relay stages instead; their opt-in markers are never changed. Compilation is
reported separately from test results. Dirty scenes are saved through Pipeline
before native runs. Source-code edits must be committed before comparing builds.

Every stage records pass/fail/skipped status. Native XML, per-case progress,
build reports, local/guest evidence and both sides' paired reports are embedded
in one JSON report. A fatal preflight failure still produces and attempts to
publish the report. A failed native run that remains pending stops subsequent
Editor stages; it does not trigger a second run, Editor restart or GUI action.
Skipped checks remain explicit and make an otherwise passing run incomplete.

The report is committed/pushed to a unique branch:

```text
verification/sons-mac-<run-id>
Documentation/Testing/MacReports/<run-id>.json
```

Publication uses a separate Git index and a report-only commit, preserving the
Mac's current branch, staged changes and local files. The uncommitted local
copy is retained under `TestResults/GameplayRegression/Runs/<run>-MacVerification/`.
If GitHub/authentication is unavailable, the complete local report remains and
the terminal reports publication failure. It cannot guarantee a push without
working GitHub credentials/network. Known credential fields and licensing
output are removed from the report; account configuration files are never read.

## Windows host managed by Codex

Both Windows exports must have matching committed gameplay/assets/packages/
settings trees with the Mac. Source commit IDs are retained; transient lobby
publication commits do not change that fingerprint. Start the supervisor from
the Windows checkout:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Documentation/Setup/HostOnlineMacVerification.ps1
```

It watches `verification/queue` for the Mac's authenticated GitHub requests,
starts one matching automatic standalone host at a time, publishes its lobby,
retains the result, cleans its handoff and retires only its own standalone
process if necessary. The Mac allows up to two paired attempts per mode; every
failed attempt stays in the report. Hosts reject mismatched source fingerprints.
Unity is never restarted. The supervisor watches for eight hours by default
(`-Hours` changes that watchdog), so the Windows laptop must stay awake/online.

When the Mac report is published, the supervisor downloads the giant JSON into
its own retained run directory and prints `MAC_REPORT_READY` with its path.
Codex can inspect it directly, or fetch the verification branch without merging
it into the gameplay branch. A final comparison requires both host and guest
reports, equal digests and inert guest gameplay managers.

The paired automatic probes verify the implemented ground-pass/pause/recovery
exchange over EOS; the other action families have native framework coverage.
They do not constitute paired human acceptance of every mechanic, real network
interruption/rejoin, reverse roles, a full roster or a complete online match.
Those limits are embedded in the report, not silently counted as passes.

For a run without the Windows host, append `--no-host`; paired stages will be
recorded as skipped. Individual fixture watchdogs default to 20 minutes and
host readiness waits to 10 minutes. `--help` lists the bounded retry/time options.
