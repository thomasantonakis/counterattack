using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunScoreFloorSnapshot(bool injured, bool jackpot, int side)
    {
        Board(side); Invoke(grid, "LoadPrecomputedShootingPaths");
        Place(attacker, Hex(8 * side));
        attacker.shooting = injured ? 2 : 1;
        if (injured) attacker.ReceiveInjury();
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        AttachAuthoredPanels(); EnableForAuthoredPresentation(match.shotManager);
        EnableForAuthoredPresentation(loose); Subscribe(movement); Subscribe(match.finalThirdManager);
        var shot = match.shotManager;
        shot.StartShotProcess(attacker, "snapshot");
        yield return Until(() => shot.isWaitingforBlockerSelection, "snapshot opens defensive player selection");
        input.TryExecuteSyntheticHexClick(defender.currentHex, "Attribute score floor", out _);
        yield return Until(() => shot.isWaitingforBlockerMovement, "selected defender can move two hexes");
        var destination = defender.currentHex.GetNeighbors(grid).First(h => h != null && !h.isOutOfBounds
            && h.GetOccupyingToken() == null);
        input.TryExecuteSyntheticHexClick(destination, "Attribute score floor", out _);
        yield return Until(() => shot.isWaitingForTargetSelection, "defense forfeit opens snapshot aiming");
        input.TryExecuteSyntheticHexClick(Hex(19 * side), "Attribute score floor", out _);
        yield return Until(() => keeper.isActivated || shot.isWaitingForShotRoll, "aimed shot reaches box-entry move or shooter roll");
        if (keeper.isActivated) LegacyKey("X");
        yield return Until(() => shot.isWaitingForShotRoll, "snapshot is ready to roll");
        var attribute = (string)Invoke(shot, "BuildShotPresentationAttribute");
        Assert.That(attribute, Does.Contain("= 0"));
        shot.StartCoroutine(shot.StartShotRoll(new RollInputOverride { hasOverride = true, roll = jackpot ? 6 : 4, isJackpot = jackpot }));
        yield return Until(() => shot.totalShotPower == (jackpot ? 50 : 4), "numeric score floors the modified attribute before adding dice; STAR stays 50");
        Assert.That(attacker.shooting, Is.EqualTo(1), "injury was already applied to the current attribute, not subtracted twice");
        Assert.That(shot.isActivated, Is.True);
    }

    public IEnumerator RunScoreFloorHeaderSave(bool injured, int side)
    {
        Board(side); Invoke(grid, "LoadPrecomputedShootingPaths");
        Place(attacker, Hex(13 * side)); Place(awayGK, Hex(16 * side, 3));
        awayGK.saving = injured ? 1 : 0;
        if (injured) awayGK.ReceiveInjury();
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(attacker);
        AttachAuthoredPanels(); EnableForAuthoredPresentation(match.shotManager); EnableForAuthoredPresentation(loose);
        var shot = match.shotManager;
        shot.ProcessHeaderAtGoal(attacker, 4, Hex(19 * side), 4);
        yield return Until(() => shot.isWaitingForGKDiceRoll, "real headed-shot entry offers the third-hex save");
        Assert.That(keeper.CalculateGoalkeeperWallPenalty(awayGK, Hex(16 * side)), Is.EqualTo(-1));
        shot.PerformGKHeaderSave(4);
        yield return Until(() => duelPanel.outcome.text.Contains("Saving total:"), "save roll displays its calculated total");
        Assert.That(duelPanel.outcome.text, Does.Contain("Saving total: 4"));
        Assert.That(duelPanel.outcome.text, Does.Contain("TIED SAVE"));
        yield return Until(() => loose.isWaitingForDirectionRoll, "tie scatters rather than awarding a goal");
        Assert.That(awayGK.saving, Is.Zero);
        Assert.That(match.gameData.stats.homeTeamStats.totalGoals, Is.Zero);
        loose.PerformDirectionRoll(1);
        yield return Until(() => loose.isWaitingForDistanceRoll, "tied save scatter advances to distance");
        loose.PerformDistanceRoll(1);
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (shot.isActivated || loose.isActivated)
        {
            Assert.That(timeout.Elapsed.TotalSeconds, Is.LessThan(8), "complete the real save/scatter continuation before teardown");
            if (keeper.isActivated) LegacyKey("X");
            if (loose.isWaitingForInterceptionRoll) loose.PerformInterceptionRoll(1);
            yield return null;
        }
    }

    public IEnumerator RunScoreFloorControl(int side)
    {
        Board(side); Place(attacker, Hex(0)); attacker.dribbling = 1; attacker.ReceiveInjury();
        ball.PlaceAtCell(Hex(2 * side)); match.SetLastToken(attacker);
        AttachAuthoredPanels(); var header = match.shotManager.headerManager;
        EnableForAuthoredPresentation(header); EnableForAuthoredPresentation(loose);
        yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
        LegacyKey("B");
        yield return Until(() => header.isWaitingForControlRoll, "uncontested distance-two landing offers control");
        _ = header.PerformControlRoll(6);
        yield return Until(() => duelPanel.outcome.text.Contains("Total"), "control displays its pre-roll floor");
        Assert.That(duelPanel.outcome.text, Does.Contain("Total 6"));
        yield return Until(() => loose.isWaitingForDirectionRoll, "numeric six still fails target nine and scatters");
        Assert.That(attacker.dribbling, Is.Zero);
    }
}
