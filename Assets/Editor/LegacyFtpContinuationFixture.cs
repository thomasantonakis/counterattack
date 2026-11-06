using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunFtpContinuationLegacy()
    {
        Board(1);
        Invoke(grid, "LoadPrecomputedShootingPaths");
        Place(attacker, Hex(8)); ball.PlaceAtCell(attacker.currentHex);
        var receiver = Token("FTP receiver", true, false, 10); Place(receiver, Hex(10));
        var context = new RoomDecisionContext { manager = nameof(FirstTimePassManager), expectedTeam = "Home" };
        Assert.That(ftp.GetLegalFirstTimePassTargetsForDecisionPreview(), Does.Contain(receiver.currentHex));
        context.AddHexActionCandidate(nameof(FirstTimePassManager), RoomActionType.FirstTimePass,
            RoomDecisionStep.ChooseTarget, receiver.currentHex, "Evaluate FTP reception");
        var forecast = RoomAIDecisionSnapshotBuilder.Build(new GameplayInstructionSnapshot
            { manager = nameof(FirstTimePassManager), expectedTeam = "Home", isAwaitingInput = true }, context, context.actions);
        foreach (var facts in forecast.candidateExplanations.Where(f => f.isFirstTimePassShotSetup))
            Assert.That(facts.firstTimePassSetupShooterTokenKey,
                Is.EqualTo(MatchManager.GetStableTokenKey(facts.candidate.targetHex.GetOccupyingToken())),
                "An occupied FTP reception cannot borrow another player's movement/Snapshot.");
        foreach (bool canShoot in new[] { false, true })
        {
            movement.ResetMovementPhase();
            var destination = canShoot ? Hex(10) : Hex(-6, -6);
            Assert.That(destination.CanShootFrom, Is.EqualTo(canShoot), "Use authored shooting caches, never an expected-outcome fixture flag.");
            Place(receiver, destination); ball.PlaceAtCell(destination); match.SetLastToken(receiver);
            match.UpdatePossessionAfterPass(destination);
            match.BroadcastEndofFirstTimePass();
            yield return null;
            if (canShoot)
            {
                Assert.That(match.shotManager.isAvailable, Is.True);
                Assert.That(movement.isAvailable && !movement.isCommitted, Is.True,
                    "A scoring receiver retains the actual Snapshot versus Movement choice.");
            }
            else
            {
                Assert.That(movement.isActivated && movement.isCommitted, Is.True);
                Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.MovementPhase));
                Assert.That(match.shotManager.isAvailable, Is.False);
            }
        }
    }
}
