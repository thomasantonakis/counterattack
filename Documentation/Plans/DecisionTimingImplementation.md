# Approved plan: decision allowances and automatic rolls

Status: implementation and identified focused acceptance checklist verified. See `Assets/Editor/FeatureTests/DecisionTiming/Verification.md` for results and unexecuted broader user-run regression checks. Original plan prepared 2026-11-08.

The 2026-11-09 stoppage/persistence continuation passed 12 further focused PlayMode
cases, closing the named manual stoppage-review and supported save/rebuild gaps.
The subsequent remaining-test batch closed combined GK/shot/foul follow-up timing,
actual target cancellation/reopening and nonblocking-result coverage: 42 new cases and
8 affected regressions passed. It also reproduced/fixed nonblocking scatter's incorrect
clock hold. Full-suite regression validation is unexecuted and is not claimed.

Continuation 2026-11-09: 22 additional focused PlayMode cases passed for jumper lists
and continuations, manual header holds, inspection and actual MatchManager snapshot
rebuilds. Later batches resolved the remaining focused gaps; Verification.md records
the current acceptance audit. Rules behavior was clarified by the user below.

## Current timing rules (latest user clarification)

- Difficulties 1–2 share one action-selection allowance across every uncommitted
  preview until commitment; only then does the committed action get its own time.
- Difficulty 3 commits the selected action immediately on its key press and starts
  its own allowance. There is no difficulty-3 precommit preview budget.
- Mandatory roll waits and automatic result-review durations keep match time and
  applicable owner budgets running. Enter manual holds and actual Pause suspend them.
  Built-in stopped-clock exclusions and five-second automatic-roll deadlines remain.
- Player movement in MP keeps its budget/countdown active. Snapshot/forfeit-pace
  inside MP uses the phase allowance. Expiry stops tactical-input waits; automatic
  rolls/reviews/movement may continue before that hold returns.
- Show countdown only from 50% spent. Center decision under HOME, events under AWAY;
  event announcements retain priority and never appear simultaneously with timing.

These rules supersede conflicting historical wording below. Latest focused evidence:
33 EditMode and 136 PlayMode passed, zero failed/skipped; compilation separately
clean. See Verification.md for exact artifacts and unsupported mid-action save limits.

## Follow-up clarifications (2026-11-09)

- Opening Rules from Pause preserves the existing gameplay pause. Opening Rules
  beside the instruction box acts like inspection: no new pause, gameplay/CPU
  commands and mandatory-roll deadlines continue even with Rules search focused.
- Timing notices must never overlap event announcements in time or space. Event
  announcements have priority; the separate scene-authored timing display is hidden
  until the announcement queue finishes. Timing accounting itself continues.
- Verify CPU scheduling and automatic rolls coexist without duplicate/stale execution.
  Current Room CPU code defers execution for one settled frame, rather than a fixed
  multi-second roll delay. Keep that scheduling and the five-second fallback.

## Next-session instruction

Read this document and the repository's AGENTS.md, then implement this approved plan. Keep all timing budgets editable in `DecisionTimingConfig.cs`. Use the live Unity CLI/Pipeline for Editor operations, run only this feature's focused tests, and report compilation separately from actual passed/failed/skipped test counts. Do not ask again about decisions explicitly settled here. Do not start whole test suites or ordinary gameplay.

## Implementation clarification (2026-11-08)

The user clarified during implementation: “when game clock is either way stopped by game, there is no need for keeping budgets.” Accordingly, decision budgets are created/charged only while the underlying game clock is eligible to run. Built-in kickoff, restart preparation, halftime, completed-match and stoppage-time clock stops do not need decision allowances. Existing interrupted live-play allowances retain their remaining time; roll waits and blocking results still suspend them. Mandatory rolls still use their independent five-second deadlines, except during gameplay pauses. The budget table remains available for any listed decision that can occur during live clock time. No separate kickoff-confirmation stage is introduced. The user further confirmed that set-piece and penalty repositioning need no budgets while the game clock is stopped, and removed substitution/forced-replacement budgets entirely (including emergency goalkeeper nomination). These clarifications supersede the corresponding original table rows and verification requirements below.

## Summary and approved decisions

Add difficulty-based decision allowances across gameplay. When an allowance expires, stop the match clock, leave gameplay input available, and show a persistent clock PNG and explanation in the existing panel beneath the scoreboard.

Automatically submit mandatory rolls after five real seconds. Exclude roll waits and forced result displays from match time and decision budgets.

The user approved:

- Cover all originally listed waits and the additional missing decisions specified below.
- Stop the match clock after expiry and keep waiting indefinitely; no inactivity penalty, forced tactical choice, or thinking reserve in this release.
- Use the adjusted corner movement budget of 20/15/10 seconds.
- Immediately stop match time and decision budgets during mandatory roll waits and blocking result displays.
- Charge tackle/stand choices to the deciding team's movement allowance; separately timed reposition choices suspend the parent allowance.
- Keep an expired team movement phase stopped until that phase ends, rather than resetting after each token move.
- Preserve existing indefinite manual result holds.
- Use the explicit defaults below for missing waits.
- Put every timing budget in one editable C# code table so the user can tune values without asking an agent.

No multiplayer transport, reconnect policy, competitive sanction, or timing-preset selection UI is part of this implementation. Provide an authority seam for future multiplayer.

## Budget table and single source of configuration

All values are real seconds, independent of accelerated match time. Use the existing `MatchManager.difficulty_level`, with the same timing profile for both teams. Movement rows cover the whole team's phase, including selecting and confirming moves. Jumper rows cover the whole team's selection list.

Implement `DecisionKind` as an enum containing the keys below. Use this table in `Assets/Scripts/Managers/DecisionTimingConfig.cs` as the single source of timing budgets; do not duplicate these defaults in inspector assets or individual managers. The source-file requirement supersedes the earlier suggestion of a configurable profile asset.

The user changes the numbers in this file and lets Unity recompile. Changes are read when a new logical decision begins; they do not refill an already active decision. `AutomaticRollSeconds` controls every mandatory roll, including accuracy, interception, scatter, foul checks, saving and stoppage-action rolls.

```csharp
public static class DecisionTimingConfig
{
    public const float AutomaticRollSeconds = 5f;
    public const float CountdownWarningSeconds = 5f;

    // Budget table                                   Diff 1  Diff 2  Diff 3
    public static readonly System.Collections.Generic.Dictionary<
        DecisionKind, (float Diff1, float Diff2, float Diff3)> Budgets = new()
    {
        // Open play
        [DecisionKind.SelectNextAction]              = (30f,   20f,   15f),
        [DecisionKind.StandardPassTarget]            = (20f,   15f,   10f),
        [DecisionKind.FinalThirdAttackingMovement]   = (30f,   20f,   15f),
        [DecisionKind.FinalThirdDefensiveMovement]   = (30f,   20f,   15f),

        // Movement phases
        [DecisionKind.AttackingMovement]             = (90f,   80f,   60f),
        [DecisionKind.DefensiveMovement]             = (120f, 100f,   90f),
        [DecisionKind.AttackingTwoForTwo]            = (30f,   20f,   10f),
        [DecisionKind.SnapshotOrForfeitPace]          = (10f,   10f,    5f),
        [DecisionKind.TackleReposition]              = (15f,   10f,    5f),
        [DecisionKind.NutmegReposition]              = (15f,   10f,    5f),
        [DecisionKind.DefensiveBoxMovement]          = (10f,   10f,    8f),

        // Outside a movement phase only; otherwise charge the owner's phase.
        [DecisionKind.StandaloneTackleOrStand]       = (10f,   10f,    5f),
        [DecisionKind.StandaloneNutmegDecision]      = (10f,   10f,    5f),

        // First Time Pass
        [DecisionKind.FirstTimePassTarget]           = (20f,   15f,   10f),
        [DecisionKind.FirstTimePassAttackerMovement] = (15f,   15f,   10f),
        [DecisionKind.FirstTimePassDefenderMovement] = (15f,   15f,   10f),

        // High Pass
        [DecisionKind.HighPassTarget]                = (20f,   15f,   10f),
        [DecisionKind.HighPassAttackerMovement]      = (15f,   15f,   10f),
        [DecisionKind.HighPassDefenderMovement]      = (15f,   15f,   10f),
        [DecisionKind.HighPassAttackingJumpers]      = (15f,   15f,   10f),
        [DecisionKind.HighPassDefensiveJumpers]      = (15f,   15f,   10f),
        [DecisionKind.HighPassGoalkeeperRush]        = (10f,   10f,    5f),

        // Long Ball
        [DecisionKind.LongBallTarget]                = (20f,   15f,   10f),
        [DecisionKind.LongBallGoalkeeperRush]        = (10f,   10f,    5f),

        // Headers and ball control
        [DecisionKind.HeaderOrBallControl]           = (10f,   10f,    5f),
        [DecisionKind.HeadedPassOrHeaderAtGoal]      = (10f,   10f,    5f),
        [DecisionKind.HeadedPassTarget]              = (20f,   15f,   10f),

        // Shots
        [DecisionKind.ShotGoalLineTarget]            = (10f,   10f,    5f),
        [DecisionKind.SnapshotGoalLineTarget]        = (10f,   10f,    5f),
        [DecisionKind.SnapshotDefenderMovement]      = (15f,   15f,   10f),

        // Goalkeeper
        [DecisionKind.GoalkeeperPickupOrLeave]       = (10f,   10f,    5f),
        [DecisionKind.GoalkeeperMovement]            = (15f,   15f,   10f),
        [DecisionKind.QuickThrowOrDistribution]      = (10f,   10f,    5f),
        [DecisionKind.GoalkeeperDistributionTarget]  = (20f,   15f,   10f),

        // Throw-in
        [DecisionKind.ThrowInTakerSelection]         = (20f,   15f,   10f),
        [DecisionKind.ThrowInMovementOrThrow]        = (20f,   15f,   10f),
        [DecisionKind.ThrowInActionSelection]        = (20f,   15f,   10f),
        [DecisionKind.ThrowInTarget]                 = (20f,   15f,   10f),

        // Free kick
        [DecisionKind.FreeKickTakerSelection]        = (20f,   15f,   10f),
        [DecisionKind.FreeKickAttackingSetup]        = (30f,   20f,   15f),
        [DecisionKind.FreeKickDefensiveSetup]        = (30f,   20f,   15f),
        [DecisionKind.FreeKickGoalkeeperMovement]    = (15f,   15f,   10f),
        [DecisionKind.FreeKickFinalKickerSelection]  = (20f,   15f,   10f),
        [DecisionKind.FreeKickActionSelection]       = (20f,   15f,   10f),
        [DecisionKind.AdvantageOrFreeKick]           = (10f,   10f,    5f),

        // Corner kick
        [DecisionKind.CornerTakerSelection]          = (20f,   15f,   10f),
        [DecisionKind.CornerAttackingThreeMoves]     = (20f,   15f,   10f),
        [DecisionKind.CornerDefensiveThreeMoves]     = (20f,   15f,   10f),
        [DecisionKind.CornerActionSelection]         = (20f,   15f,   10f),

        // Goal kick
        [DecisionKind.GoalKickAttackingSetup]        = (30f,   20f,   15f),
        [DecisionKind.GoalKickDefensiveSetup]        = (30f,   20f,   15f),
        [DecisionKind.GoalKickActionSelection]       = (20f,   15f,   10f),
        [DecisionKind.GoalKickTarget]                = (20f,   15f,   10f),

        // Penalties and shootouts
        [DecisionKind.PenaltyTakerSelection]         = (20f,   15f,   10f),
        [DecisionKind.PenaltyAttackingSetup]         = (30f,   20f,   15f),
        [DecisionKind.PenaltyDefensiveSetup]         = (30f,   20f,   15f),
        [DecisionKind.ShootoutOrderSelection]        = (90f,   80f,   60f),

        // Kickoff
        [DecisionKind.KickoffFormationSetup]         = (90f,   80f,   60f),
        [DecisionKind.KickoffTakerSelection]          = (20f,   15f,   10f),
        [DecisionKind.KickoffConfirmation]           = (20f,   15f,   10f),

        // Team management
        [DecisionKind.SubstitutionTransaction]       = (90f,   80f,   60f),
        [DecisionKind.ForcedReplacementTransaction]  = (90f,   80f,   60f),
    };

    public static float GetBudget(DecisionKind kind, int difficulty)
    {
        var budget = Budgets[kind];
        return difficulty switch
        {
            1 => budget.Diff1,
            2 => budget.Diff2,
            3 => budget.Diff3,
            _ => throw new System.ArgumentOutOfRangeException(
                nameof(difficulty), difficulty, "Expected difficulty 1, 2, or 3.")
        };
    }
}
```

The forced replacement transaction includes any required emergency goalkeeper nomination. Each separately entered restart team setup phase uses the corresponding setup row; individual moves within that phase do not reset it. Existing target-selection budgets apply after the restart action is chosen; do not create duplicate simultaneous target timers. A separately entered single goalkeeper movement uses its goalkeeper movement row.

The original proposal's `(*)` markers were not explained. They introduce no additional exception. Result panels retain their existing configurable display durations and manual holds.

## Implementation changes

### Timing service and ownership

- Add a central `DecisionTimingService`. Managers explicitly register typed decision kinds and the team owing input; do not infer timing from displayed instructions or depend on the debug monitor.
- Expose begin/end decision, suspend/resume, elapsed-time advancement, current timing state, and a read-only `HoldsMatchClock` result. Inject elapsed time for deterministic tests.
- Identify each logical decision or phase with a stable instance ID. Token changes, target previews, invalid clicks, cancellation, and reopening the same unresolved choice must not replenish the allowance.
- Keep one budget per team movement phase. Once expired, its clock hold persists until that team's phase ends, including across individual token moves.
- Suspend the parent during separately timed reposition choices. Restore its remaining allowance afterward, including an already-expired state. Do not charge parent and child simultaneously.
- Tackle/stand and nutmeg challenge/decline waits consume the deciding team's existing movement budget when one exists; suspend the other team's budget. Otherwise use the standalone allowance.
- Start allowances when legal input becomes available. Exclude automatic movement, forced transitions and blocking presentations. Apply the same accounting to human and AI teams.
- Actor selection, target preview and confirmation within one decision share its allowance. Choosing an action and selecting its target are separate decisions with the corresponding budgets.
- Explicitly register every listed and additional wait. Add development validation identifying awaiting-input states without a timing classification.

### Match-clock integration

- Add timing holds to `MatchManager.CanAdvanceMatchClock()`. Do not use `Time.timeScale`, disable gameplay input, or overwrite `isClockRunning` to implement decision expiry.
- Roll waits immediately hold match time and suspend decision budgets. Blocking result displays do the same; a nonblocking result that permits gameplay does not suspend a live decision.
- Clearing one hold must preserve others, including restarts, halftime, pause menus and expired parent phases.
- Pause menus suspend allowances and automatic-roll deadlines. Inspection cards and rules browsing do not replenish or suspend allowances.
- Preserve existing possession accounting and stoppage-action rules. Stopped match time earns no additional timed possession.

### Automatic rolls

- Add a shared validated roll-command entry point used by manual and automatic requests. Preserve existing dice service, overrides, rule evaluation, animations, presentation and continuation.
- At five eligible seconds, submit the current mandatory roll exactly once. Give each successive roll an independent identity and deadline.
- Revalidate decision ID, owner and readiness immediately before execution. Manual and automatic requests racing at expiry must resolve only once. Cancel stale requests when the decision ends.
- Treat automatic rolls as domain commands independent of keyboard focus or inspection overlays; still respect gameplay pauses and blocking transitions. Never simulate GUI keystrokes.
- Preserve existing result durations, early dismissal and Enter-to-hold behavior. Manual result holds may remain indefinite in this release.

### Scoreboard notice

- Extend the existing `MatchEventBannerController` beneath the scoreboard with persistent timing status, using its authored icon, caption and detail bindings.
- Show the pending decision countdown, with an amber warning during the final five seconds.
- At expiry, show a clock PNG, `Match clock stopped`, and `Awaiting {team} · {decision}.`
- During mandatory rolls show `Match clock stopped · Rolling automatically in {seconds}s.` During blocking results show `Match clock stopped · Reviewing result.`
- Follow-up user clarification supersedes the original priority: transient announcements
  take priority. The separate authored timing display stays hidden until their queue
  finishes, then shows current timing. Timing accounting itself continues.
- Author visuals in edit mode and provide an edit-mode preview. Keep the notice noninteractive. Do not touch designer-owned match-stat layouts or create visual structures only at runtime.

### Persistence and future multiplayer

- Extend the runtime snapshot with optional timing state: decision identities, owners, remaining allowances, expired flags, suspended parent budgets and pending-roll remaining time.
- Restore timing only for matching decisions that gameplay restoration actually resumes. A legitimately different restored decision starts its own allowance. Time spent outside the running match does not consume budgets.
- Older saves without timing data initialize the currently available decision normally. New matches clear all timing state. Never mutate live saves as test fixtures.
- Log decision expiry, timing-hold release and automatic-roll events with owner and decision identity.
- Keep time and command authority replaceable so a future server can own deadlines. Do not implement networking, reconnect policy or competitive sanctions here.

## Grounding from repository inspection

- `Assets/Scripts/Managers/MatchManager.cs` owns the central match clock eligibility check, possession-time crediting and existing restart holds.
- `Assets/Scripts/Managers/RoomDecisionManager.cs` already submits synthetic AI inputs, including roll candidates. This is useful evidence of an existing input path; the timing service must not depend on AI thinking or AI controller ownership.
- `Assets/Scripts/UI/MatchEventBannerController.cs` currently queues transient 3–5 second announcements. Persistent timing status must not be enqueued as an ordinary expiring announcement.
- `Assets/Scripts/UI/DuelResolutionPanelController.cs`, `HeaderChallengePanelController.cs` and `StoppageActionsPanelController.cs` distinguish waiting for rolls, blocking continuation and manually held results. Preserve those distinctions.
- `Assets/Scripts/Managers/RoomSaveService.cs` contains `RoomRuntimeSnapshot` and `RoomClockSnapshot`; extend save state compatibly.
- `GameplayInstructionSnapshot` and `RoomDecisionContext` expose existing input/owner information, but some instruction information is presentation-derived. Timing must use explicit manager registrations rather than parsing copy.
- A `.codegraph/` index exists. Use CodeGraph first for code navigation; verify current source and serialized wiring because the index may omit newer symbols.

## Verification and handoff

Create dedicated folders `Assets/Editor/FeatureTests/DecisionTiming/` and `Assets/Tests/PlayMode/Features/DecisionTiming/`, with independently selectable EditMode and PlayMode fixtures, category `DecisionTiming`, and a short task/verification checklist.

- Parameterize every budget entry across all three difficulties. Verify exact deadline boundaries and independence from accelerated match time.
- Exercise real gameplay entry points for each registered decision family; verify ownership, phase completion, both attacking directions where applicable and actual continuation.
- Verify that previews, invalid inputs, reselection, cancellation and reopening do not reset time.
- Cover nested rolls/repositions, retained parent time, expired-phase latching, tackle/stand attribution and overlapping clock holds.
- Cover manual/automatic deadline races, successive rolls, stale requests, pause/resume, overlay-independent execution and deterministic dice outcomes.
- Verify possession accounting, blocking versus nonblocking results, manual result holds and unchanged stoppage-action allocation.
- Verify snapshot restoration, older saves, new-match cleanup, PNG/caption bindings, persistent status and announcement queue preservation.
- Follow owned fixture cleanup patterns: isolate singletons, subscriptions, scenes, time and temporary saves; suppress legacy runners and restore state after failures.

Use only the live Unity CLI/Pipeline for Editor operations, explicitly targeting this project and supplying required caller/skill metadata. Never launch Unity, use batchmode, run `editor_play`, kill Unity, load scenes additively for convenience, or use GUI scripting.

After code changes, recompile through the live Editor, inspect the command result and Editor.log, and fix compilation errors before handoff. Before each test run, wait for reload, inspect every loaded scene's dirty state, save through `save_all` as needed, and recheck clean state. Resolve any pending run before editing or starting another run. An already-open modified-scenes modal requires the user's manual Save action.

Run only this feature's dedicated fixtures, serially. Retain native XML and per-case evidence under `TestResults/GameplayRegression/Runs/`, outside Unity temporary directories. Report compilation separately from actual passed/failed/skipped counts; `running` is not a result.

Update `Documentation/Testing/RunningTests.md` with the feature filters and retained artifacts. Update migration/review documents only when their claims change. At handoff, identify exact existing affected test folders and fixtures/categories for the user to run, with reasons. Do not execute broader folders or complete suites by default.

## Acceptance criteria and limits

Every classified decision receives its configured allowance. Expiry stops match time without blocking legal input. Interrupted timers retain their budgets, mandatory rolls submit exactly once within five eligible seconds, and the scoreboard explains the active timing hold.

The feature remains editable through the single C# table. No manager hides another timing default. Every configured budget has independent test coverage.

Players may continue waiting after expiry or hold result panels indefinitely. This protects match time; preventing indefinite real-world waiting remains a separate competitive feature. Repeated delays just below the allowance are permitted by the approved rules; do not silently add cumulative penalties.
