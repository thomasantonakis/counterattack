# First real gameplay authority slice

## Scope and checklist

- [x] Add an isolated `OnlineGroundPassAuthority` around existing rules.
- [x] Route both participant roles through `OnlineMatchProtocol.Admit`.
- [x] Publish semantic action IDs and read-only value snapshots, independent of
  screen coordinates, synthetic keys and arbitrary method names.
- [x] Revalidate current board/target legality immediately before admission.
- [x] Transfer roll ownership to the defending team; never accept a client roll value.
- [x] Retain duplicate receipts across a pending-preview reconnect and retire old connections.
- [x] Revoke stale boundaries on pause, board changes and cleanup.
- [x] Order asynchronous outcome publications separately from command revisions.
- [x] Add focused real-rule PlayMode coverage in both attacking directions,
  deterministic board setup and fixture-controlled next random draw.
- [x] Reuse owned fixture cleanup and the global framework legacy-runner gate.
- [x] Wire this adapter to authenticated EOS semantic messages and automated
  host/guest commands in the isolated network proof (paired run pending).
- [x] Author the guest replica scene; suppress guest rules/clock/AI/roll execution.
- [ ] Route physical online input through the authority gateway in an interactive mode.
- [ ] Complete M1 shared pause ownership, personal presentation independence,
  active-resolution disconnect freezing and authoritative resynchronization.
- [ ] Prove matching outcomes in standalone Windows/Mac runs in both host roles.

The network-proof follow-up is recorded in
`Assets/Editor/FeatureTests/OnlineGroundPassNetwork/Verification.md`.
No online adapter is installed into Room yet. Existing offline input continues
through its current entry points. This is a tested M1 foundation batch, not
complete M1 or playable online gameplay. DTOs cover this ground-pass slice;
they are not a full Room snapshot, recovery format, wire codec or compatibility
handshake. Team/jersey IDs identify the prepared roster only; catalog and
substitution identity must be established before production use.

## Audited decision and mutation boundary

| Boundary | Allowed sender | Real authority entry point | Published state / continuation |
| --- | --- | --- | --- |
| Available ordinary ground pass | Current attacking team; host role grants no privilege | MatchManager.TriggerStandardPass | Target-choice decision |
| Target preview / confirmation | Current attacking team, current decision/revision, currently legal target | GroundBallManager.HandleGroundBallPath | Selected target or committed resolution |
| Pending interception roll | Defending team | GroundBallManager.PerformGroundInterceptionDiceRoll without override | One roll sequence, token stats, outcome and possession |
| Resolution completion | Authority coroutine only | Existing pass/interception flow | Ball/token coordinates, touch, possession, stats and continuation availability |
| Pending-preview reconnect | Same authenticated account, new connection lifetime | Protocol rebind; no gameplay replay | Same preview and original receipt; retired connection rejected |
| Pause / externally changed board | No stale command | WithdrawDecision before admission | New boundary after resume; preview retained |

The adapter is main-thread only. Capture sorts detached token/action values and
never calls the lazy stats getter. A dispatch exception faults the adapter;
accepted partial mutations are not retried. The host-only transport integration
must serialize all admission and capture calls. GroundBallManager retains its
interception queue; the new public seam exposes only its pending hex.

The existing pause/intro blocker is respected as a safety boundary. It still
contains local presentation semantics and must be split before live host UI can
no longer obstruct a guest. Reconnect evidence here is at a pending preview;
it does not freeze or restore an already-running coroutine or restore after a
process crash. No gameplay rule or ambiguous ownership policy was changed.

## Verification

Live Windows Editor compilation: clean, zero script errors/warnings.
Initial PlayMode batch: **12 passed, 0 failed, 0 skipped**, native XML:
`TestResults/GameplayRegression/Runs/20261111T211925724Z/results.xml`.
The subsequent publication-sequence and pause-boundary additions are validated
by the final runs:

- EditMode: **1 passed, 0 failed, 0 skipped**,
  `TestResults/GameplayRegression/Runs/20261111T212209215Z/results.xml`.
- PlayMode: **14 passed, 0 failed, 0 skipped**,
  `TestResults/GameplayRegression/Runs/20261111T212340273Z/results.xml`.

Test execution uses only category
`OnlineGroundPass`, serially through live CLI/Pipeline. The authored Room scene
was clean before each run; test-owned scene and singleton/event/random/time
state is restored by GoalkeeperBoxEntryFixture. No live saves are fixtures.

Feature locations:

- EditMode: `Assets/Editor/FeatureTests/OnlineGroundPass/`, fixture
  `OnlineGroundPassBoundaryEditorTests`.
- PlayMode: `Assets/Tests/PlayMode/Features/OnlineGroundPass/`, fixture
  `OnlineGroundPassPlayModeTests`.

Broader user-run checks (not executed automatically):
`Assets/Editor/FeatureTests/OnlineMatch/OnlineMatchProtocolEditorTests.cs`,
fixture/category `OnlineMatchProtocolEditorTests` / `OnlineMatchProtocol`, for
the new authority-only withdrawal API; and
`Assets/Tests/PlayMode/LegacyGroundPassPlayModeTests.cs`, fixture
`LegacyGroundPassPlayModeTests`, for existing offline pass/interception behavior.
This batch adds a read-only pass-manager seam and an unwired adapter; no full
suite is warranted or claimed.
