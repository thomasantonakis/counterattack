# Online Room coin toss and half transitions

- [x] Own real Room toss rolls/choices through the authoritative protocol.
- [x] Reject legacy local input; freeze result/retie across shared/connection holds.
- [x] Author dice/choices and project detached guest toss state without RNG/rules.
- [x] Connect real final whistle to shootout ordering and extra-time to next half.
- [x] Cover choice ownership, both winners/directions, ties, replay and hold paths.
- [x] Register new cases, retain focused evidence and refresh Windows exports.

Initial first-half opening from Draft remains handled by OnlineOpeningSetup.
This feature connects the existing in-Room tiebreaker and extra-time flows.

Implementation: the real scene-owned CoinTossPanel/DraftOrderPanel models resolve
host dice and existing CoinTossDecision rules. Authenticated turn-owned semantic
commands drive the model; legacy local buttons/keys cannot bypass admission.
The authored online view shows detached dice and permitted choices. Result/retie
deadlines retain their remaining time through overlapping pause/connection holds.
Half resets publish a non-interactive transition before real kickoff setup.
Extra-time opening choices and the shootout coin-toss winner project to guests.

Retained reproductions under TestResults/GameplayRegression/Runs:
- `20261115T223649534Z/results.xml`: actual guest toss panel stayed open after the
  host transition because an absent optional object was materialized as empty.
- `20261116T054633234Z/results.xml`: isolated replica null-copy reproducer failed.
  Replica/view copies now use null-preserving JSON; absence of coin, pose,
  shootout and token hex objects remains absence, and copies stay detached.
  Existing malformed-state fixtures use that copy path so their assertions reach
  the intended malformed field rather than an accidental empty coin object.
- `20261116T060222212Z/results.xml`: two Room input-hold admission reproductions.
  Coin legal actions now honor the real gameplay-input gate before publication.

Own focused native results, zero failed/skipped:
- `20261116T055344805Z/results.xml`: 13 EditMode null-copy/validation cases.
- `20261116T055400361Z/results.xml`: 16 real Room toss/transition cases: both
  purposes/winners, first-kick versus direction choice and both ends/directions.
- `20261116T055819353Z/results.xml`: 2 ties across overlapping holds.
- `20261116T055913949Z/results.xml`: 4 ordinary regulation/extra-time half swaps.
- `20261116T060407632Z/results.xml`: 2 held-input/legacy-choice bypass cases.
All 37 new cases are individually registered for deferred Mac execution.
Earlier green smokes remain in `055033672Z`, `055053296Z` and `055124056Z`.

Affected focused results (same Runs root), zero failed/skipped:
- `20261116T060447178Z`: roster validation 13.
- `20261116T060455254Z`: snapshot pose validation 9.
- `20261116T060503531Z`: shootout validation/completion gates 19.
- `20261116T060511754Z`: gameplay wire 13.
- `20261116T060520109Z`: 31 legacy Room coin cases plus 13 repeated new cases.
- `20261116T060531472Z`: legacy coin presentation 16.
- `20261116T060718805Z`: full payload bounds 15.
- `20261116T060734270Z`: actual snapshot poses 5.
- `20261116T060850151Z`: full guest projection 4.
- `20261116T060952485Z`: real manual controls/recovery 7.
- `20261116T061024233Z`: encoded full Room kickoff 4.
- `20261116T061111067Z`: online Room menus/Leave 6.
This is 179 distinct focused cases (37 own + 142 affected), not 192: the legacy
coin filter also selected the new 13-case fixture. Native XML retains class names.
Compilation is separately clean. Thirteen Python verifier tests passed. No whole
project suite, native Mac execution or actual paired internet acceptance ran.
Broader follow-up, if requested: Assets/Tests/PlayMode/Features/OnlineHeaders,
fixture/category OnlineHeaderResultPlayModeTests / OnlineHeaders, and
Assets/Tests/PlayMode/Features/OnlineShots, fixture OnlineGroundShotPlayModeTests
(category OnlineShots), because nullable snapshot copying is shared. These whole
feature fixtures were not rerun in this batch; wire/pose/projection/manual
representative coverage ran as above.

CLI note: self-update returned UPGRADE_MSIX_UPDATE_FAILED; winget reported a
successful installation, but the active alias still reported 1.0.0-beta.17. Work
continued through the working explicit legacy Pipeline commands. The Editor and
Pipeline package were not restarted/upgraded.

Windows exports succeeded:
- Normal eight scenes: `build_acb5a08ccd9a`, `20261116T061535Z-normal-Windows-build/build-summary.json`.
- Room proof: `build_63cb34e29597`, `20261116T061613Z-Room-Windows-build/build-summary.json`.
- Prepared gameplay: `build_bede77380cf5`, `20261116T061635Z-Gameplay-Windows-build/build-summary.json`.
Normal has nine existing warnings; proofs two each. Reports retain the known
nonfatal EOS `vswhere.exe` diagnostic (one per successful build); no C# errors.
Packaged local Room, local gameplay and native Room login passed in
`20261116T061907180Z-Room-local`, `20261116T061928157Z-Gameplay-local` and
`20261116T061941854Z-Room-login`. Login registered liveness notifications.
Both clients must rebuild for full Room v7. No paired internet latency or full
remote acceptance is claimed; the son's Mac run remains deferred.
