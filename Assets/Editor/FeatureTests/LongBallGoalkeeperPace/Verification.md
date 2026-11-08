# Long Ball goalkeeper Pace before landing recovery

Gameplay change, using the existing owned GoalkeeperBoxEntryFixture board and
native wrapper `LongBallGoalkeeperPacePlayModeTests` (explicit fixture filter).
The framework gate suppresses the legacy runner; cleanup restores singleton,
input subscriptions, scene, random/time and panel state. No mutable live saves.

- [x] Route direct recipients and empty/teammate landings through the existing Pace response.
- [x] Preserve independent qualifying 1-hex box-entry move, before Pace.
- [x] Keep automatic direct recovery and current goalkeeper handling continuations.
- [x] Re-evaluate occupancy/ZOI after Pace, including claims [V].
- [x] Invalidate stale accuracy/flight/Pace/recovery continuations on cleanup/new action.
- [x] Execute focused cases for both directions: direct/ZOI recovery; move/decline;
  four combinations of entry/Pace; accurate/inaccurate teammate and empty delivery;
  inside/outside claims; entering/leaving ZOI; zero/blocked Pace; reset/new action;
  canceled recovery presentation; out-of-bounds, offside and clock/end gates.
- [x] Refresh existing authored Rules article text without rebuilding layouts.

Compilation and test verdicts are recorded separately below. A running CLI response
is not a pass. First reproduction attempt was canceled because a concurrent pickup
fixture compile error blocked startup. A subsequent reproduction was interfered
with by the other session's test request; its pickup XML is not Long Ball evidence.

## Executed validation

The user manually tested the correction on 2026-11-08 and reported it OK.
Initial complete feature run: **44 passed / 18 failed / 0 skipped**, native XML
`TestResults/GameplayRegression/Runs/20261108T072745026Z/results.xml`.
Eight delivery cases and two teammate-decline cases read ground-pass statistics
instead of aerial-pass statistics; the assertions now use the correct counters.
Eight click-move cases exposed a missing input acknowledgement in the existing
Pace click handler; it now consumes an accepted click through GameInputManager.
Ordering assertions were retained. Compilation subsequently completed cleanly
through the live Editor (0 errors/warnings).
Second complete feature run: **54 passed / 8 failed / 0 skipped**, native XML
`TestResults/GameplayRegression/Runs/20261108T081757906Z/results.xml`.
All eight failures were the new mid-animation identity assertion calling the
occupied-hex lookup while movement temporarily clears that occupancy. Identity is
now asserted through team roles during the animation and the actual defending-GK
lookup before/after arrival; destination and unchanged home keeper are asserted.
No gameplay outcome or timing assertion was relaxed. The affected 16 Landing
cases were rerun: **16 passed / 0 failed / 0 skipped**, native XML
`TestResults/GameplayRegression/Runs/20261108T082252170Z/results.xml`.
All **62 distinct feature cases** now have passing native results (46 cases outside
Landing from the second full feature run, plus all 16 Landing cases from the
final retest). Do not sum overlapping runs. Scripts compile cleanly; Room was
clean and Play Mode stopped after testing. Native artifacts remain local under
TestResults (git-ignored).
No independent calculation/rule helper changed, so the behavioral coverage is
PlayMode; no EditMode suite was run.

Broader checks are user-run, not part of this feature filter:
- `Assets/Tests/PlayMode/LegacyLongBallPlayModeTests.cs`, fixture
  `LegacyLongBallPlayModeTests`: accurate/scatter continuations. Its interception-
  before-Pace expectations were aligned with the approved order; execution is handed off.
- `Assets/Tests/PlayMode/LegacyLongBallOobPlayModeTests.cs`, fixture
  `LegacyLongBallOobPlayModeTests`: restart award geometry.
- `Assets/Tests/PlayMode/GoalkeeperBoxEntryPlayModeTests.cs`, fixture
  `GoalkeeperBoxEntryPlayModeTests`: shared entry eligibility and movement.
- `Assets/Tests/PlayMode/Features/GoalkeeperPickup/`, `GoalkeeperPickupPlayModeTests` once completed:
  independently implemented delayed handling eligibility integration.

High Pass minimum remains 3 hex steps. No full suites are requested or executed.
