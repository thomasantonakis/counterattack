using System.Linq;
using NUnit.Framework;

public sealed class RosterAttackOptionsEditorTests
{
    public static RoomAIDecisionSnapshot BalancedXI()
    {
        var snapshot = new RoomAIDecisionSnapshot { expectedTeam = "Home", teamInAttack = "Home", attackHasPossession = true,
            homeTeamDirection = "LeftToRight", awayTeamDirection = "RightToLeft", currentState = "MovementPhase",
            ball = new RoomHexCoordinates { x = 0, z = 0 } };
        snapshot.tokens.AddRange(new[] {
            Player("Home",1,-14,0,5,6,0,5,0,true),
            Player("Home",6,0,0,5,2,2,6,3),
            Player("Home",14,4,-4,4,2,6,4,2),
            Player("Home",12,5,-5,6,2,1,4,5),
            Player("Home",3,2,6,5,6,2,4,4),
            Player("Home",15,3,-1,4,2,2,4,6),
            Player("Home",2,1,-6,4,2,2,4,2),
            Player("Home",4,-4,3,4,2,3,3,2),
            Player("Home",5,-4,-3,4,2,3,3,2),
            Player("Home",8,-2,5,4,2,2,3,3),
            Player("Home",9,-2,-5,4,2,2,3,3),
            Player("Away",1,16,0,4,1,0,4,0,true),
            Player("Away",4,8,0,4,2,3,3,2),
            Player("Away",5,8,3,4,2,3,3,2) });
        snapshot.legalActions.Add(new RoomActionCandidate { actionType = RoomActionType.StartMovement,
            step = RoomDecisionStep.ChooseActionType, key = "M", isExecutableNow = true });
        return snapshot;
    }

    private static RoomAITokenSnapshot Player(string team, int number, int x, int z, int pace, int dribbling,
        int heading, int highPass, int shooting, bool goalkeeper = false) => new RoomAITokenSnapshot
        {
            tokenKey = team + ":" + number, teamSide = team, jerseyNumber = number, playerName = "Player " + number,
            isPlaying = true, isGoalKeeper = goalkeeper, currentHex = new RoomHexCoordinates { x = x, z = z },
            pace = pace, dribbling = dribbling, heading = heading, highPass = highPass, shooting = shooting, resilience = 4
        };

    [Test]
    public void BalancedXIProvidesGroundRunningAerialAndDribbleOptionsSimultaneously()
    {
        var plan = RoomAIRosterAttackPlan.Build(BalancedXI());
        Assert.That(plan.playingOutfieldCount, Is.EqualTo(10));
        Assert.That(plan.lacksStrongDribbler, Is.False);
        Assert.That(plan.options.Select(option => option.route).Distinct(), Is.EquivalentTo(new[] {
            RoomAIAttackingRoute.GroundCombination, RoomAIAttackingRoute.RunningRelease,
            RoomAIAttackingRoute.AerialRelease, RoomAIAttackingRoute.DribbleLed }));
        Assert.That(plan.aerial, Is.Not.Null, "Having a dribbler must not remove the aerial alternative.");
        Assert.That(plan.options.Where(option => option.route == RoomAIAttackingRoute.GroundCombination).Count(), Is.EqualTo(9));
    }

    [Test]
    public void NoDribblersOrHeadersStillCreatesPassingAndRunningOptions()
    {
        var snapshot = BalancedXI();
        foreach (var token in snapshot.tokens.Where(token => token.teamSide == "Home" && !token.isGoalKeeper))
        { token.dribbling = 2; token.heading = 2; }
        var plan = RoomAIRosterAttackPlan.Build(snapshot);
        Assert.That(plan.lacksStrongDribbler, Is.True);
        Assert.That(plan.options.Select(option => option.route).Distinct(), Is.EquivalentTo(new[] {
            RoomAIAttackingRoute.GroundCombination, RoomAIAttackingRoute.RunningRelease }));
        Assert.That(plan.options.All(option => !string.IsNullOrEmpty(option.preparation)
            && !string.IsNullOrEmpty(option.continuation)), Is.True);
    }

    [Test]
    public void AerialRolesIgnoreNamesAndShirtNumberRoles()
    {
        var snapshot = BalancedXI();
        var first = RoomAIAerialReleasePlan.Build(snapshot);
        Assert.That(first.runnerTokenKey, Is.EqualTo("Home:12"), "A substitute shirt can be the best running option.");
        foreach (var token in snapshot.tokens)
        { token.playerName = "Renamed"; token.jerseyNumber += 30; }
        var renamed = RoomAIAerialReleasePlan.Build(snapshot);
        Assert.That(renamed.headerTokenKey, Is.EqualTo(first.headerTokenKey));
        Assert.That(renamed.runnerTokenKey, Is.EqualTo(first.runnerTokenKey));
    }

    [Test]
    public void BenchSentOffAndGoalkeeperDribblingDoNotCreateOutfieldOptions()
    {
        var snapshot = BalancedXI();
        snapshot.tokens.Single(token => token.tokenKey == "Home:3").dribbling = 2;
        var bench = Player("Home",16,0,2,6,6,6,6,6); bench.isPlaying = false;
        var sentOff = Player("Home",17,0,-2,6,6,6,6,6); sentOff.isSentOff = true;
        snapshot.tokens.AddRange(new[] { bench, sentOff });
        var plan = RoomAIRosterAttackPlan.Build(snapshot);
        Assert.That(plan.lacksStrongDribbler, Is.True);
        Assert.That(plan.playingOutfieldCount, Is.EqualTo(10));
        Assert.That(plan.options.Any(option => option.route == RoomAIAttackingRoute.DribbleLed), Is.False);
        Assert.That(plan.options.Any(option => option.receiverTokenKey is "Home:16" or "Home:17"), Is.False);
    }

    [Test]
    public void RosterAndRouteOptionsRefreshDuringTheSamePossession()
    {
        var snapshot = BalancedXI();
        var first = RoomAITacticalPlanner.UpdateAttackPlan(snapshot, null, new RoomAITacticalWeights());
        Assert.That(first.rosterAttackPlan.options.Any(option => option.route == RoomAIAttackingRoute.DribbleLed), Is.True);
        string identity = first.rosterAttackPlan.rosterIdentity;
        snapshot.tokens.Single(token => token.tokenKey == "Home:3").dribbling = 2;
        var next = RoomAITacticalPlanner.UpdateAttackPlan(snapshot, first, new RoomAITacticalWeights());
        Assert.That(next.rosterAttackPlan.rosterIdentity, Is.Not.EqualTo(identity));
        Assert.That(next.rosterAttackPlan.options.Any(option => option.route == RoomAIAttackingRoute.DribbleLed), Is.False);
        Assert.That(next.rosterAttackPlan.options.Any(option => option.route == RoomAIAttackingRoute.AerialRelease), Is.True);
    }

    [Test]
    public void RouteReadinessChangesWithTheBoardWithoutChangingTheXI()
    {
        var snapshot = BalancedXI();
        var passing = new RoomAICandidateExplanation { candidate = new RoomActionCandidate { isExecutableNow = true },
            actorTokenKey = "Home:12", isSupportingPassLaneMove = true, supportsConcreteContinuation = true };
        var dribbling = new RoomAICandidateExplanation { candidate = new RoomActionCandidate { isExecutableNow = true },
            actorTokenKey = "Home:3", fullPaceDribbleReachProbability = .9f };
        snapshot.candidateExplanations.AddRange(new[] { passing, dribbling });
        var initial = RoomAIRosterAttackPlan.Build(snapshot);
        Assert.That(initial.options.Any(option => option.route == RoomAIAttackingRoute.GroundCombination
            && option.hasEvaluatedContinuation), Is.True);
        Assert.That(initial.options.Any(option => option.route == RoomAIAttackingRoute.DribbleLed
            && option.hasEvaluatedContinuation), Is.True);
        passing.supportsConcreteContinuation = false;
        var blockedPass = RoomAIRosterAttackPlan.Build(snapshot);
        Assert.That(blockedPass.rosterIdentity, Is.EqualTo(initial.rosterIdentity));
        Assert.That(blockedPass.options.Any(option => option.route == RoomAIAttackingRoute.GroundCombination
            && option.hasEvaluatedContinuation), Is.False);
        Assert.That(blockedPass.options.Any(option => option.route == RoomAIAttackingRoute.DribbleLed
            && option.hasEvaluatedContinuation), Is.True);
        passing.supportsConcreteContinuation = true;
        dribbling.fullPaceDribbleReachProbability = .1f;
        var blockedDribble = RoomAIRosterAttackPlan.Build(snapshot);
        Assert.That(blockedDribble.options.Any(option => option.route == RoomAIAttackingRoute.DribbleLed
            && option.hasEvaluatedContinuation), Is.False);
        Assert.That(blockedDribble.options.Any(option => option.route == RoomAIAttackingRoute.GroundCombination
            && option.hasEvaluatedContinuation), Is.True);
    }

    [Test]
    public void DifferentConcreteRoutesAreScoredWithoutAnExclusivePlanBonus()
    {
        var snapshot = BalancedXI();
        var ground = new RoomActionCandidate { actionType = RoomActionType.StartMovement, step = RoomDecisionStep.ChooseActor,
            isExecutableNow = true };
        var dribble = new RoomActionCandidate { actionType = RoomActionType.StartMovement, step = RoomDecisionStep.ChooseTarget,
            isExecutableNow = true };
        snapshot.legalActions.AddRange(new[] { ground, dribble });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = ground, actorTokenKey = "Home:12",
            isSupportingPassLaneMove = true, supportsConcreteContinuation = true });
        snapshot.candidateExplanations.Add(new RoomAICandidateExplanation { candidate = dribble, actorTokenKey = "Home:3",
            fullPaceDribbleReachProbability = .9f });
        var roster = RoomAIRosterAttackPlan.Build(snapshot);
        var plan = new RoomAIAttackPlan { planType = RoomAIAttackPlanType.WideAerialAttack, rosterAttackPlan = roster };
        var ranked = RoomAITacticalPlanner.RankCandidates(snapshot, new[] { ground, dribble }, plan, new RoomAITacticalWeights());
        Assert.That(ranked.Single(item => item.candidate == ground).reasons, Has.Some.Contains("GroundCombination"));
        Assert.That(ranked.Single(item => item.candidate == dribble).reasons, Has.Some.Contains("DribbleLed"));
        Assert.That(ranked.SelectMany(item => item.reasons), Has.None.Contains("matches wide aerial plan"));
    }
}
