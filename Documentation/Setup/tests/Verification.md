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
`TestResults/GameplayRegression/Runs/20261114T142042000990Z-MacVerifierScriptChecks/results.json`.
Coverage includes redaction, exact parameter-case XML failures, fixture discovery,
legacy marker routing, report publication with staged/unstaged/untracked edits,
ref overwrite rejection, owned command timeout, fatal-report persistence and
source fingerprint stability across lobby commits versus gameplay changes,
and nested host/guest evidence collection while excluding configuration files.
These are orchestration tests, not Unity gameplay or paired acceptance counts.

The Mac process has not executed on Windows. The real Windows watcher and GitHub
request/rejection/report path must be smoke-checked before calling coordination
ready. Full Mac/Windows paired success requires both actual retained reports.
