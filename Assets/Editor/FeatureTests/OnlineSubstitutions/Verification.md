# Online substitutions

- [x] Reproduce missing semantic substitution support through the real pause gateway.
- [x] Apply registration, roster/hex/ball changes on host only, with receipt replay protection.
- [x] Preserve requester-owned pause, stoppage availability and own-team validation.
- [x] Cover both teams, both directions, both squad sizes and guest projection.
- [x] Add authored outfield/normal goalkeeper swap controls and focused coverage.
- [ ] Forced sent-off/double-injury goalkeeper replacement and emergency nomination.
- [x] Register all new cases for deferred Mac testing; compile/test/build.

Pause ownership follows the existing passive-opponent policy: only the requester
may perform controls during their shared pause. No rule change is implied.

Native evidence under `TestResults/GameplayRegression/Runs/`:
- `20261115T190416822Z`: initial missing-command reproducer failed as expected.
- `20261115T193950556Z`: all 18 own PlayMode cases passed, zero failed/skipped.
  Eight cover both teams/directions/squad sizes; four exercise actual authored
  outfield controls, four normal goalkeeper swaps and two allowance boundaries.
- `20261115T194333677Z`: seven affected manual/input/recovery cases passed.
- `20261115T194404917Z`: six affected full Room menu cases passed.
- `20261115T195904143Z`: four affected encoded kickoff cases passed after the
  separately documented payload-size fix.
Compilation was separately clean. No complete suite or paired run was executed.

The authority derives team identity from the connection, validates actual playing/
bench roles and applies existing registration/roster/hex/ball entry points. Stats
and bench/player state replicate to an inert guest. Duplicate accepted receipts
survive rebind; retired identities, conflicting sequences, stale revisions,
connection holds, malformed/cross-team IDs, incorrect roles, re-entry and commands
outside the requester's pause do not mutate the roster. Escape first closes the
personal selection panel without resuming shared play.

The existing rule is three substitutions, with one extra-time credit. The menu
now honors the remaining allowance; that credit is explicitly published so the
guest does not infer it from the half or clamp the fourth substitution away.
`20261115T193045326Z` retains a fixture error that assumed five; it was corrected
to the repository constants. `20261115T193237047Z` then reproduced the real menu
limit bug. `20261115T193537858Z` retains the first two-case green allowance run.

`20261115T190648778Z` contains seven passes and one failure caused by an external
CLI `editor status` error during the run, rather than a substitution assertion.
The affected case passed alone in `20261115T191030762Z`; no logging assertion was
suppressed. `20261115T192144085Z` retains the earlier 16-case clean pass.

All 18 new cases are registered individually for deferred Mac verification.
The normal/forced keeper distinction matters: healthy keeper swaps are covered;
automatic forced replacement, emergency nomination and shootout ordering remain
independent work. The prepared legacy proof profiles do not advertise these
full Room commands; full Room contract v3 gates mixed-version entry.

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
