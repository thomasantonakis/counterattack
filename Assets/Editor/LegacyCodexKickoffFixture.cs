using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunCodexKickoffLegacy()
    {
        LoadLegacyRoster();
        var kickoff = Add<KickoffManager>(); match.kickoffManager = kickoff; input.kickoffManager = kickoff;
        kickoff.ball = ball; kickoff.hexGrid = grid; kickoff.playerTokenManager = match.playerTokenManager;
        kickoff.freeKickManager = match.freeKickManager; kickoff.groundBallManager = ground;
        kickoff.movementPhaseManager = movement; kickoff.headerManager = match.shotManager.headerManager;
        Subscribe(kickoff); Subscribe(ground); Subscribe(match.finalThirdManager);
        match.currentState = MatchManager.GameState.KickOffSetup; kickoff.StartPreKickoffPhase();
        string defendingTeam = match.teamInAttack == MatchManager.TeamInAttack.Home ? "Away" : "Home";
        var legal = kickoff.BuildAutomatedSetupCandidates(defendingTeam);
        foreach (int jersey in new[] { 7, 11, 9, 10 })
        {
            var actor = match.playerTokenManager.allTokens.Single(t => t.isPlaying
                && t.isHomeTeam == (defendingTeam == "Home") && t.jerseyNumber == jersey);
            int flank = System.Math.Sign(actor.currentHex.coordinates.z);
            var choices = legal.Where(a => a.actor == actor || a.actionType == RoomActionType.Confirm).ToList();
            Assert.That(choices.Any(a => a.actor == actor && a.targetHex != null && a.targetHex.coordinates.x == 0
                && (jersey is not (7 or 11) || flank == 0 || a.targetHex.coordinates.z * flank >= 0)), Is.True,
                $"Opening fixture must offer a legal halfway destination for defending No. {jersey}.");
            var setupContext = new RoomDecisionContext { manager = nameof(KickoffManager), expectedTeam = defendingTeam, expectedInput = "hex" };
            var setupSnapshot = RoomAIDecisionSnapshotBuilder.Build(null, setupContext, choices);
            var setupDecision = CodexTacticianProvider.Instance.Decide(setupSnapshot, choices, null,
                new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
            Assert.That(setupDecision.selectedCandidate?.actor, Is.SameAs(actor));
            Assert.That(setupDecision.selectedCandidate.targetHex, Is.Not.Null);
            Assert.That(setupDecision.selectedCandidate.targetHex.coordinates.x, Is.EqualTo(0),
                $"Defending No. {jersey}; half={setupSnapshot.clock?.currentHalf}; state={setupSnapshot.currentState}; "
                + string.Join(" | ", setupDecision.rankedCandidates.Take(5).Select(e =>
                    $"{e.candidate.targetHex?.coordinates}: frontPriority={e.kickoffFrontLinePriority}; {string.Join("; ", e.reasons.Take(4))}")));
            if ((jersey is 7 or 11) && flank != 0)
                Assert.That(setupDecision.selectedCandidate.targetHex.coordinates.z * flank, Is.GreaterThanOrEqualTo(0));
        }
        match.difficulty_level = 3;
        // Prepare the opening through the actual bounded setup commands. Merely
        // accepting an unready forecast on the legacy gameplay-audit formation
        // does not establish a supported second action.
        bool homeConfirmed = false;
        for (int step = 0; step < 13 && !homeConfirmed; step++)
        {
            var commands = kickoff.BuildAutomatedSetupCandidates("Home");
            var preparationContext = new RoomDecisionContext { manager = nameof(KickoffManager), expectedTeam = "Home", expectedInput = "hex" };
            var preparationSnapshot = RoomAIDecisionSnapshotBuilder.Build(null, preparationContext, commands);
            var preparation = CodexTacticianProvider.Instance.Decide(preparationSnapshot, commands, null,
                new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
            Assert.That(preparation.selectedCandidate, Is.Not.Null);
            Assert.That(kickoff.TryExecuteAutomatedSetupCandidate(preparation.selectedCandidate, out string error), Is.True, error);
            homeConfirmed = preparation.selectedCandidate.actionType == RoomActionType.Confirm;
            yield return Until(() => !kickoff.IsWaitingForMovementToComplete(), "manager-approved attacking kickoff setup movement finishes");
        }
        Assert.That(homeConfirmed, Is.True, "Attacking setup must terminate within its manager-owned move budget.");
        LegacyKey("Return");
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.KickoffBlown));
        var setup = RoomAIDecisionSnapshotBuilder.Build(null, new RoomDecisionContext
        { manager = nameof(KickoffManager), expectedTeam = match.teamInAttack.ToString(), expectedInput = "hex" }, new List<RoomActionCandidate>());
        var plan = CodexTacticianProvider.BuildKickoffPlan(setup);
        Assert.That(plan, Is.Not.Null);
        var taker = match.playerTokenManager.allTokens.Single(t => MatchManager.GetStableTokenKey(t) == plan.takerTokenKey);
        var receiver = match.playerTokenManager.allTokens.Single(t => MatchManager.GetStableTokenKey(t) == plan.receiverTokenKey);
        var landing = Hex(plan.landingHex.x, plan.landingHex.z);
        Place(receiver, landing); Place(taker, ball.GetCurrentHex()); match.SetLastToken(taker);
        ground.ActivateKickoffGroundBall(taker);
        var context = new RoomDecisionContext { manager = nameof(GroundBallManager),
            expectedTeam = taker.isHomeTeam ? "Home" : "Away", expectedInput = "hex" };
        ground.PopulateRoomDecisionContext(context);
        var snapshot = RoomAIDecisionSnapshotBuilder.Build(null, context, context.actions);
        var decision = CodexTacticianProvider.Instance.Decide(snapshot, context.actions, null,
            new RoomAITacticalWeights(), AIManager.CodexTacticianDifficulty.UltraHard);
        var chosenLanding = decision.selectedCandidate?.targetHex ?? decision.selectedCandidate?.targetToken?.currentHex;
        var opening = snapshot.kickoffPlan;
        Assert.That(chosenLanding, Is.Not.Null); Assert.That(opening, Is.Not.Null);
        Assert.That(opening.firstPassEvaluated, Is.True);
        var chosenReceiver = match.playerTokenManager.allTokens.Single(t => MatchManager.GetStableTokenKey(t) == opening.receiverTokenKey);
        Assert.That(chosenReceiver.currentHex, Is.SameAs(chosenLanding)); Assert.That(opening.collectionCost, Is.EqualTo(0));
        Assert.That(opening.remainingPace, Is.EqualTo(chosenReceiver.pace));
        Assert.That(opening.remainingAttackingMoves, Is.EqualTo(movement.AttackingMoveLimit));
        Assert.That(opening.continuationEvaluated, Is.True,
            $"Forecast={plan.receiverTokenKey}: {plan.secondAction}; chosen={opening.receiverTokenKey}: {opening.rationale}; "
            + string.Join(" | ", decision.rankedCandidates.Take(6).Select(e =>
            {
                var next = RoomAIKickoffPlanner.FromPassFacts(snapshot, e.explanation);
                return next == null ? e.candidate.label : $"{next.receiverTokenKey} at {next.landingHex.x},{next.landingHex.z}: continuation={next.continuationEvaluated}, value={next.value}; {next.secondAction}";
            })));
        Assert.That(opening.secondActionTargetHex, Is.Not.Null);
        input.TryExecuteSyntheticHexClick(chosenLanding, "Legacy migration", out _);
        yield return Until(() => ball.GetCurrentHex() == chosenLanding && !ground.isActivated,
            "Codex's evaluated first kickoff pass reaches its prepared receiver without a retrieval move");
        Assert.That(chosenReceiver.currentHex, Is.SameAs(ball.GetCurrentHex()));
    }
}
