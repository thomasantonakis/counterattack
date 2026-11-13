# Online headers

- [x] Reproduce and fix result continuation escaping authority holds.
- [x] Preserve controlled disabled manager ownership and suppress local result keys.
- [x] Typed nominations, confirmations, rolls and headed-pass targets.
- [x] Publish detached nomination/score/winner observations with versioned framing.
- [x] Cover attacking/defending winners, unchallenged headers, interruptions and both directions.
- [ ] Continue into headed shots, goalkeeper and restart outcomes.

Paired checks remain deferred. Focused OnlineHeaders coverage is kept separate
from broader offline header regressions.

Compilation separately clean. Category `OnlineHeaders`: **40 PlayMode passed,
zero failed/skipped**, native XML:
`TestResults/GameplayRegression/Runs/20261113T222242456Z/results.xml`.
Fixture `OnlineHeaderResultPlayModeTests` covers actual high-pass landing into
contested nominations/rolls, both winners, completed headed-pass collection,
automatic roll deadlines, duplicate/wrong-team/guest rejection, nomination
toggle/revision changes, consecutive same-team rollers, free headers,
overlapping connection/shared holds, local result keys, disabled authority
owners and cancellation without late statistics/continuation.
The latest extension includes 12 headed keeper back-pass cases (feet, catch,
automatic catch, spill, cancellation and overlapping holds) and 10 landing
interception cases (success, miss, automatic roll, cancellation and holds),
each in both directions. The earlier 18-case checkpoint remains in
`Runs/20261113T221025969Z/results.xml`.

Back-pass commands reproduced 10 missing-choice failures before integration in
`Runs/20261113T221704665Z/results.xml`, then 10 passed in
`Runs/20261113T221825902Z/results.xml`. Landing interception tests reproduced
8 wrong-actor observations in `Runs/20261113T222021411Z/results.xml`: the dice
were attributed to the header passer instead of the rolling defender. Correcting
that actor left 6 passes and 2 cancelled-continuation null references in
`Runs/20261113T222120170Z/results.xml`. Version guards now retire both the
presentation and parent continuation before accessing cleared interceptor state;
the final 40-case run above passes. Roll rules and outcome thresholds are unchanged.

The original four hold cases all failed before the fix:
`Runs/20261113T220044236Z/results.xml`. The header result used an unscaled timer
and rejected disabled manager components. It now freezes its remaining countdown
through authority holds, accepts an active controlled owner, suppresses local
keys and distinguishes explicit manager cancellation from local dismissal.
The four reproduced cases passed in `Runs/20261113T220150241Z/results.xml`.

`OnlineHeaderObservationEditorTests`: **10 EditMode passed**, zero failed/skipped,
`Runs/20261113T220740407Z/results.xml`. Detached nominations, host scores and
winner observations survive bounded v7/CA07 framing; foreign/duplicate actors,
invalid faces/totals and invalid roller/winner IDs cannot replace accepted state.
Extended scripts require ground-gameplay-v7, ground-manual-v6,
room-gameplay-v5 or room-manual-v5 handoffs, so both desktops must rebuild.

Affected checks: `OnlineSharedPause` **11 EditMode passed** in
`Runs/20261113T221123715Z/results.xml`; existing offline
`DecisionTimingPlayModeTests.ManualHeaderResult` **2 PlayMode passed** in
`Runs/20261113T221144591Z/results.xml`. No complete suites ran.
Affected `OnlineHighPass`: **21 PlayMode passed**, zero failed/skipped, in
`Runs/20261113T221213166Z/results.xml` after the new header router and v7 framing.
After the interception/catch extension, affected
`LegacyHeaderResolutionPlayModeTests`: **12 PlayMode passed**, zero failed/skipped,
in `Runs/20261113T222552477Z/results.xml`. Its initial 12 failures in
`Runs/20261113T222437763Z/results.xml` were the existing aerial fixture requesting
a high pass at kickoff. PrepareLegacyInaccurateHeader now enters open play through
BroadcastSafeEndofMovementPhase before its high-pass key; downstream nomination,
result, interception, tie, scatter and continuation assertions are unchanged.

Still unexecuted: headed goal resolution (typed goal declaration is wired),
header ties/loose-ball resolution and defensive-only headers. Back-pass spill
coverage currently stops at the real loose-ball direction decision. Room's canonical header wiring was verified by the preceding high-pass
feature; these new contested tests own isolated real managers and authored panels.
Paired Mac/Windows acceptance remains deferred. Broader user-run checks:
`Assets/Tests/PlayMode/LegacyHeaderSelectionPlayModeTests.cs`,
`LegacyHeaderResolutionPlayModeTests.cs`, `LegacyHeaderGoalBranchPlayModeTests.cs`
and category DecisionTiming under `Assets/Tests/PlayMode/Features/DecisionTiming/`
for remaining offline nominations, ties, headed goals and timing branches.

Windows exports after source bd15ac3: Room `build_4c0da126ced0` Succeeded
(7 warnings); prepared board `build_1530a316baf7` Succeeded (2 warnings).
Each retains the known missing-vswhere.exe diagnostic (one report error;
no script compile errors). Existing packaged local ground/pause/recovery probes
passed in `Runs/20261113T221441263Z-Room-local/` and
`Runs/20261113T221531403Z-Gameplay-local/`, with result.json,
gameplay-evidence.json and build-report.json retained. These packaged probes
are not human header or paired-network acceptance.
