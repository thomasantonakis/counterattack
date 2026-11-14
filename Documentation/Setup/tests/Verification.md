# Mac verifier and Windows supervisor checklist

- [x] One Bash entry point with standard-library Python orchestration.
- [x] Discover implemented online fixtures and run them serially through Pipeline.
- [x] Preserve XML/progress/build/player evidence in one redacted JSON report.
- [x] Failed preflight still saves/publishes a failed report.
- [x] Report-only Git commit preserves source HEAD, index and local edits.
- [x] Windows requests use fixed modes, unique IDs and pinned source fingerprints.
- [x] Bounded retries, owned standalone cleanup, report retrieval and temporary idle-sleep prevention.
- [x] Local script tests and shell/parser checks.
- [ ] Actual son's Mac execution and paired host/guest reports.

Windows script verification: **11 Python tests passed**, zero failures/errors/
skips, with Bash syntax checks for four scripts and PowerShell parser checks for
both host scripts. Evidence:
`TestResults/GameplayRegression/Runs/20261114T142256700584Z-MacVerifierScriptChecks/results.json`.
Coverage includes redaction, exact parameter-case XML failures, fixture discovery,
legacy marker routing, report publication with staged/unstaged/untracked edits,
ref overwrite rejection, owned command timeout, fatal-report persistence and
source fingerprint stability across lobby commits versus gameplay changes,
and nested host/guest evidence collection while excluding configuration files.
These are orchestration tests, not Unity gameplay or paired acceptance counts.

The real Windows watcher/GitHub request/rejection/response path passed in
`Runs/coordination-smoke-20261114T142314Z-WindowsCoordinatorSmoke/results.json`.
A separate real report-only commit/push and automatic download of a **3 MiB JSON**
passed in `Runs/report-smoke-20261114T142457Z-WindowsReportRetrievalSmoke/results.json`,
exercising GitHub's large-file Git blob fallback. Its payload was explicitly
Windows coordination smoke data, not a Mac gameplay report; the temporary
remote report branch and queue request files were cleaned afterward.
The initial live check exposed PowerShell native stderr handling for absent
queue files; local Continue handling corrected it without restarting Unity.

The Mac process has not executed on Windows. Full Mac/Windows paired success
requires both actual retained reports. The pinned Windows watcher is now active;
its process/stdout/stderr paths are recorded in Temp/MacVerificationHost.process.json.
