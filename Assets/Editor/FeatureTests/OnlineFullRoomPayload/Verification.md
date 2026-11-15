# Full Room payload bounds

- [x] Reproduce a real 36-actor kickoff publication exceeding 64 KiB.
- [x] Compress negotiated full Room states without changing logical snapshots.
- [x] Preserve the 64 KiB wire limit and bound expansion separately to 256 KiB.
- [x] Reject malformed/conflicting/truncated/oversized compressed states.
- [x] Verify large full-roster round trips, legacy profiles and real kickoff.
- [x] Register cases for deferred Mac execution, retain evidence and refresh builds.

The initial failure is retained in `20261115T194455478Z/results.xml` under
TestResults/GameplayRegression/Runs. No limit increase or field omission is used.
Internet latency remains unmeasured.

Final native evidence under `TestResults/GameplayRegression/Runs/`:
- `20261115T195834930Z/results.xml`: 15 own EditMode cases passed.
- `20261115T195846204Z/results.xml`: 13 affected existing wire cases passed.
- `20261115T195904143Z/results.xml`: all four real Room kickoff cases passed.
Zero failed/skipped in these runs; compilation separately clean.

Stress snapshots of 89,505?95,409 expanded bytes round-trip exactly as 8,510?8,638
wire payload bytes (ten fragments). These are stress-fixture measurements, not
internet latency. Gzip is limited to negotiated full Room states; prepared
profiles remain uncompressed. Encoding leaves the caller's snapshot intact.
Expansion rejects bad Base64, truncation, length mismatches, oversized declarations,
expansion bombs, conflicting encodings, incorrect kinds and foreign catalogs;
the following valid message still decodes. All 15 cases are separately registered
for the deferred Mac run. Source snapshots, rolls, counters and fields are retained.

Windows export refresh succeeded:
- `build_e15bd3dbf47d`: `20261115T200545Z-normal-Windows-build/build-summary.json`.
- `build_a5574ebd4241`: `20261115T200623Z-Room-Windows-build/build-summary.json`.
- `build_5d4390ed047c`: `20261115T200645Z-Gameplay-Windows-build/build-summary.json`.
All evidence lives under `TestResults/GameplayRegression/Runs/`. Normal game has
nine warnings; proof profiles two each. The existing nonfatal EOS `vswhere.exe`
diagnostic remains in each successful report; C# compilation is clean.
Packaged checks passed: `20261115T201115129Z-Room-local`,
`20261115T201128795Z-Gameplay-local`, `20261115T201136357Z-Room-login`.
Login registered liveness notifications. These isolated proofs do not establish
paired full-match acceptance. Thirteen Python verifier tests also passed.
