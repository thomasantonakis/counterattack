# Implementation prompt: header-at-goal goalkeeper presentation

You are working in the Unity project:

`/Users/t.antonakis/Documents/Projects/Personal/counterattack`

Read the repository instructions in `AGENTS.md` first and follow them strictly. Use Unity CLI/Pipeline for Editor control. Do not start Unity, enter Play Mode, or run the test suite yourself.

## Context

The latest commit is:

`d2d28a9ced015e13eaef4b668b6ffb086d3f35fd`

`feat: Implement header challenge presentation system`

It introduced the scene-authored `HeaderChallengeUI` and the presentation flow used by `HeaderManager` and `LooseBallManager`.

Read these first:

- `Assets/Scripts/Managers/ShotManager.cs`
- `Assets/Scripts/Managers/HeaderManager.cs`
- `Assets/Scripts/Managers/LooseBallManager.cs`
- `Assets/Scripts/UI/HeaderChallengePanelController.cs`
- `Assets/Scripts/UI/DuelResolutionPanelController.cs`
- `Assets/Scripts/UI/SingleRollResolutionPresentation.cs`
- `Docs/Wiki/header-challenge-presentation.md`
- `Assets/Scripts/ActionScripts/GameTestScenarioRunner.cs`
- The latest commit diff and its parent version of the affected files

## Objective

Extend the new header challenge presentation system to cover the `ShotManager` branch for:

**Header at goal when the defending goalkeeper is not jumping.**

The existing header-at-goal flow where the goalkeeper participates in the header challenge must remain unchanged. The new behavior must cover the later `ShotManager` goalkeeper-saving flow, not duplicate or replace `HeaderManager`’s existing header challenge.

## Current ShotManager flow to understand

1. `HeaderManager` resolves a header at goal.
2. If the goalkeeper is involved in the header challenge, the existing `HeaderChallengePanelController` handles the header contest.
3. If the goalkeeper is not jumping, `HeaderManager` eventually calls:

   `ShotManager.ProcessHeaderAtGoal(attacker, attackerTotalScore, headerTargetHex)`

4. `ShotManager` then:
   - finds the defending goalkeeper;
   - finds the save hex;
   - waits for a goalkeeper Saving roll;
   - compares goalkeeper Saving + roll + penalty against the header attacker’s total;
   - handles:
     - goalkeeper loss: goal;
     - goalkeeper tie: loose ball / deflection;
     - goalkeeper win: goalkeeper catches the ball only after a Handling roll;
     - failed Handling: `GoalkeeperHandlingSpill` loose-ball flow;
     - successful Handling: possession change and save-and-hold decision.

Relevant methods include:

- `ProcessHeaderAtGoal`
- `PerformGKHeaderSave`
- `ResolveHandlingTest`
- `ResolveGKSavingAttempt`
- `ResolveLooseBall`
- `ResetShotProcess`
- `GetInstructions`
- `PopulateRoomDecisionContext`

## Mechanics to study carefully

### Potential blocking rolls

Read how `ShotManager`:

- gathers outfield blockers;
- creates `ShotInteraction` objects;
- distinguishes on-path blocks from ZOI blocks;
- calculates required natural rolls;
- applies tackling and goalkeeper-as-outfielder rules;
- orders multiple defenders;
- continues to the next blocker after a failed block;
- reaches the shooter roll only after relevant blockers fail or are exhausted.

Relevant methods include:

- `GatherInterceptors`
- `GatherOutfieldBlockInteractions`
- `BuildOutfieldBlockInteraction`
- `BuildGoalkeeperOutfieldBlockInteraction`
- `OfferBlockRoll`
- `StartShotBlockRoll`
- `GetOutfieldBlockRequiredRoll`
- `GetEffectiveShotBlockTackling`
- `ContinueFreeKickShotAfterShooterRoll`
- `ResolveGKSavingAttempt`

Do not apply ordinary-shot blocker rules to the header-at-goal flow unless the rules explicitly require it. Establish whether this feature concerns only the goalkeeper Saving presentation after `HeaderManager` has already resolved the header, or whether defender blocking can occur before or after that point.

### Shooter timing

Trace exactly when `ShotManager` waits for the shooter roll versus when it waits for a blocker roll or goalkeeper Saving roll.

Pay attention to:

- `isWaitingForShotRoll`
- `isWaitingForBlockDiceRoll`
- `isWaitingForGKDiceRoll`
- `isHeaderAtGoal`
- `headerAttackerTotalScore`
- `totalShotPower`
- `headerGkPenalty`
- `currentShotInteraction`
- `interceptors`

The new presentation must not consume or reorder gameplay rolls. Gameplay randomness must still come from `MatchManager.ResolveGameplayDiceRoll`. UI dice remain cosmetic only.

### Goalkeeper Saving

Compare these flows:

- ordinary shot: `ResolveGKSavingAttempt`;
- header at goal with a jumping goalkeeper: existing `HeaderManager` / header challenge behavior;
- header at goal with a non-jumping goalkeeper: `ProcessHeaderAtGoal` / `PerformGKHeaderSave`.

The requested feature is specifically the third case.

### Handling and loose-ball continuation

Trace the complete continuation after a goalkeeper wins the Saving comparison:

- `PerformGKHeaderSave` sets `isWaitingforHandlingTest`;
- `ResolveHandlingTest` resolves the Handling roll;
- successful Handling:
  - `SaveMade` with saveType `held`;
  - `BallRecovery`;
  - possession change;
  - save-and-hold decision;
- failed Handling:
  - `GoalkeeperHandlingSpill`;
  - `LooseBallManager.ResolveLooseBall`;
  - direction roll;
  - distance roll;
  - possible GK free move;
  - possible defender recovery/interception roll;
  - eventual movement or restart.

Compare the header-owned loose-ball presentation path with the normal ShotResolution loose-ball path. Do not make goalkeeper Handling spills acquire `HeaderChallengeUI` ownership unless that is explicitly required by the intended design.

## Required analysis before editing

Produce a short implementation analysis identifying:

- the exact current call graph;
- the current presentation owner for each roll;
- where the non-jumping-GK header-at-goal flow diverges;
- whether the latest commit’s documented presentation boundary must change;
- the minimum files that need modification;
- the required test scenarios.

Then implement the feature if the intended behavior is unambiguous.

## Required behavior

For a non-jumping-GK header at goal:

- Present the goalkeeper Saving roll clearly as a continuation of the header-at-goal event.
- Show:
  - attacker/header power;
  - defending goalkeeper;
  - Saving value;
  - header save penalty;
  - required save threshold;
  - tie threshold;
  - actual roll and total;
  - outcome: goal, save/hold decision, or loose ball.
- Preserve the existing `ShotManager` state machine and roll ordering.
- Preserve existing header-at-goal movement and goal-flow behavior.
- Block conflicting gameplay input while the roll is pending.
- Route synthetic `RoomDecisionManager` input and physical input through the same ownership rules.
- Preserve roll overrides.
- Close or hand off before later movement, Handling, GK decisions, or loose-ball input where appropriate.
- Preserve the distinction between:
  - `HeaderChallengePanelController`-owned header-deflection presentation;
  - `DuelResolutionPanelController`-owned ShotResolution presentation.
- Do not add a header challenge contestant row for the goalkeeper unless the goalkeeper is actually participating in the header challenge.

## Architecture to evaluate

Evaluate whether the correct design is:

1. Extend `HeaderChallengePanelController` with a distinct header-at-goal goalkeeper-save section; or
2. Reuse `DuelResolutionPanelController` for the non-jumping-GK Saving and Handling rolls; or
3. Add a narrow presentation adapter allowing `ShotManager` to use the header panel only for the initial Saving roll while leaving Handling, loose-ball, recovery, and save-and-hold flows unchanged.

Prefer the smallest architecture that:

- preserves `HeaderManager` authority over header challenges;
- preserves `ShotManager` authority over Saving and Handling;
- avoids runtime UI construction;
- does not alter `Canvas/MatchStatsUI/Stats`;
- does not conflate header challenge results with goalkeeper save results;
- keeps ordinary-shot behavior unchanged;
- keeps jumping-GK header behavior unchanged;
- preserves modal input ownership, Escape, Enter hold/resume, camera presets, and roll overrides.

## Testing requirements

Add or update focused tests/scenarios for at least:

1. Header at goal, GK not jumping, GK Saving wins.
2. Header at goal, GK not jumping, GK Saving ties.
3. Header at goal, GK not jumping, GK Saving loses and the header becomes a goal.
4. GK wins Saving, Handling succeeds.
5. GK wins Saving, Handling fails and starts `GoalkeeperHandlingSpill`.
6. A resulting loose ball continues to direction, distance, and recovery rolls correctly.
7. Existing jumping-GK header challenge behavior remains unchanged.
8. Existing ordinary-shot Saving/Handling behavior remains unchanged.
9. Roll overrides and modal input ownership remain correct.
10. No panel remains incorrectly owned after `ShotManager.ResetShotProcess` or cancellation.

Follow the project’s test iteration rules. Do not run Play Mode or the full suite yourself. Leave relevant runner scenarios prepared for the user to run.

## Validation

After code changes:

- inspect the diff;
- run static checks such as `rg` and `git diff --check`;
- use Unity CLI only for status, recompile, and log inspection;
- if the Pipeline is ready, save/recompile through the prescribed Unity workflow;
- inspect `Editor.log` for compile errors;
- keep fixing compile errors until the open Unity Editor reports clean scripts.

## Final response

Report:

- the chosen architecture and why;
- files changed;
- exact state-machine and presentation-ownership changes;
- tests added or updated;
- remaining ambiguity or manual test steps;
- confirmation that ordinary shots and jumping-GK header challenges were preserved.
