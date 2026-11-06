using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunThrowInLegacy(string branch)
    {
        Board(1); match.difficulty_level = branch == "hard" ? 3 : 2;
        attacker.playerName = attacker.name = "Cafferata"; Place(attacker, Hex(0, 12));
        Place(defender, Hex(0, -10)); ball.PlaceAtCell(attacker.currentHex);
        var throwIn = CreateLegacyThrowInManager();
        if (branch is "feet" or "single")
        {
            yield return BeginLegacyThrowInExecution(throwIn);
            Assert.That(throwIn.isWaitingForThrowTypeSelection, Is.False);
            Assert.That(ground.isActivated && ground.isAwaitingTargetSelection, Is.True);
            Assert.That(ground.imposedDistance, Is.EqualTo(6));
            if (branch == "feet") yield break;
            ground.CleanUpPass();
            yield return Until(() => ground.imposedDistance == 11, "completed throw-to-feet targeting restores the normal pass distance");
        }
        var receiver = Token("Yaneva", true, false, 8); Place(receiver, Hex(3, 10));
        if (branch != "single")
        {
            var second = Token("Kalla", true, false, 3); Place(second, Hex(-3, 10));
        }
        yield return BeginLegacyThrowInExecution(throwIn);
        Assert.That(throwIn.GetInstructions(), Does.Contain("[C]"));
        LegacyKey("C");
        if (branch != "single")
        {
            Assert.That(throwIn.isWaitingForHeaderTarget, Is.True);
            input.TryExecuteSyntheticHexClick(receiver.currentHex, "Legacy migration", out _);
            if (branch == "easy")
            {
                Assert.That(throwIn.isWaitingForHeaderTarget, Is.True);
                Assert.That(throwIn.currentHeaderThrowTarget, Is.SameAs(receiver.currentHex));
                Assert.That(ball.GetCurrentHex(), Is.SameAs(attacker.currentHex));
                input.TryExecuteSyntheticHexClick(receiver.currentHex, "Legacy migration", out _);
            }
        }
        yield return Until(() => ball.GetCurrentHex() == receiver.currentHex && !throwIn.isActivated,
            "throw to head reaches its selected attacker and releases throw-in input");
    }

    private IEnumerator BeginLegacyThrowInExecution(ThrowInManager throwIn)
    {
        throwIn.StartThrowInPreparation(attacker.currentHex, MatchManager.TeamInAttack.Home);
        yield return Until(() => throwIn.isWaitingForTakerSelection && !ball.IsCellMoveInProgress, "throw-in preparation offers taker selection");
        input.TryExecuteSyntheticHexClick(attacker.currentHex, "Legacy migration", out _);
        yield return Until(() => match.finalThirdManager.isActivated || throwIn.isRunningMandatoryMovement,
            "taker selection continues to Final Thirds or mandatory movement");
        yield return ForfeitLegacyFinalThirds();
        yield return Until(() => movement.isActivated && movement.isMovementPhaseAttack, "throw-in starts mandatory attacking movement");
        for (int i = 0; i < 3 && movement.isActivated; i++)
        {
            bool attack = movement.isMovementPhaseAttack, defense = movement.isMovementPhaseDef;
            LegacyKey("X");
            yield return Until(() => !movement.isActivated || movement.isMovementPhaseAttack != attack || movement.isMovementPhaseDef != defense,
                "throw-in forfeit advances one mandatory movement section");
        }
        yield return Until(() => throwIn.isWaitingForOptionalMovementDecision, "completed mandatory movement offers optional movement or throw now");
        Assert.That(match.finalThirdManager.isActivated, Is.False);
        LegacyKey("T");
        yield return Until(() => throwIn.isWaitingForThrowTypeSelection || ground.isActivated, "throw now computes actual available header targets");
    }
    private ThrowInManager CreateLegacyThrowInManager()
    {
        var throwIn = Add<ThrowInManager>(); var push = Add<OutOfBoundsPushManager>(); push.Configure(grid, ball);
        throwIn.ball = ball; throwIn.hexGrid = grid; throwIn.movementPhaseManager = movement;
        throwIn.groundBallManager = ground; throwIn.headerManager = match.shotManager.headerManager;
        throwIn.finalThirdManager = match.finalThirdManager; throwIn.outOfBoundsPushManager = push;
        Subscribe(throwIn); Subscribe(movement); Subscribe(ground); Subscribe(match.finalThirdManager);
        return throwIn;
    }

    public IEnumerator RunThrowInRestrictedLegacy()
    {
        Board(1); attacker.name = attacker.playerName = "Cafferata";
        Place(attacker, Hex(-4, 8)); Place(defender, Hex(1, 10));
        var displaced = Token("Yaneva", true, false, 8); displaced.pace = 6; Place(displaced, Hex(0, 12));
        ball.PlaceAtCell(displaced.currentHex);
        var throwIn = CreateLegacyThrowInManager();
        throwIn.StartThrowInPreparation(Hex(0, 12), MatchManager.TeamInAttack.Home);
        input.TryExecuteSyntheticHexClick(attacker.currentHex, "Legacy migration", out _);
        yield return Until(() => match.finalThirdManager.isActivated || throwIn.isRunningMandatoryMovement,
            "occupied throw-in spot resolves before mandatory movement");
        yield return ForfeitLegacyFinalThirds();
        yield return Until(() => movement.isActivated && movement.isMovementPhaseAttack, "throw-in mandatory attack starts");
        Assert.That(attacker.currentHex, Is.SameAs(Hex(0, 12)));
        Assert.That(ball.GetCurrentHex(), Is.SameAs(attacker.currentHex));
        Assert.That(displaced.currentHex, Is.Not.SameAs(attacker.currentHex));
        input.TryExecuteSyntheticHexClick(attacker.currentHex, "Legacy migration", out _);
        Assert.That(movement.selectedToken, Is.Not.SameAs(attacker));
        LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "restricted throw-in mandatory defense starts");
        LegacyKey("X");
        Assert.That(movement.isActivated && movement.isMovementPhaseDef, Is.True,
            "Defenders within two hexes must leave the protected throw-in zone before forfeit.");
        input.TryExecuteSyntheticHexClick(defender.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(Hex(0, 8), "Legacy migration", out _);
        yield return Until(() => defender.currentHex == Hex(0, 8) && movement.movedTokens.Contains(defender), "close defender leaves the protected zone");
        LegacyKey("X");
        yield return Until(() => movement.isMovementPhase2f2 || throwIn.isWaitingForOptionalMovementDecision, "mandatory defense forfeit now advances");
        if (movement.isMovementPhase2f2) LegacyKey("X");
        yield return Until(() => throwIn.isWaitingForOptionalMovementDecision, "mandatory throw-in movement completes without Final Thirds");
        Assert.That(match.finalThirdManager.isActivated, Is.False);
        LegacyKey("M");
        yield return Until(() => throwIn.isRunningOptionalMovement && movement.isActivated, "optional restricted movement starts");
        input.TryExecuteSyntheticHexClick(displaced.currentHex, "Legacy migration", out _);
        var distant = grid.highlightedHexes.Where(h => h != null && !h.isOutOfBounds
            && !h.isAttackOccupied && !h.isDefenseOccupied && HexGridUtils.GetHexStepDistance(attacker.currentHex, h) > 6)
            .OrderByDescending(h => HexGridUtils.GetHexStepDistance(attacker.currentHex, h))
            .ThenBy(h => h.coordinates.x).ThenBy(h => h.coordinates.z).First();
        input.TryExecuteSyntheticHexClick(distant, "Legacy migration", out _);
        yield return Until(() => displaced.currentHex == distant && movement.movedTokens.Contains(displaced),
            "optional movement takes the displaced attacker outside throw-to-head range");
        Assert.That(HexGridUtils.GetHexStepDistance(attacker.currentHex, displaced.currentHex), Is.GreaterThan(6));
        LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "optional throw-in defense starts");
        LegacyKey("X");
        yield return Until(() => movement.isMovementPhase2f2 || throwIn.isWaitingForThrowTypeSelection || ground.isActivated,
            "optional defense forfeit advances");
        if (movement.isMovementPhase2f2) LegacyKey("X");
        yield return Until(() => throwIn.isWaitingForThrowTypeSelection || ground.isActivated, "optional movement computes throw execution choices");
        Assert.That(ground.isActivated && ground.isAwaitingTargetSelection, Is.True,
            "The original scenario expects no header receiver and automatic throw-to-feet selection.");
        Assert.That(ground.imposedDistance, Is.EqualTo(6));
        Assert.That(match.finalThirdManager.isActivated, Is.False);
    }

}
