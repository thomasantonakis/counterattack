using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static LegacyShotBranchCatalog;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunNativeFreeKickShotBranch(int index)
    {
        var branch = BuildFreeKickShotBranchDefinitions()[index];
        yield return RunNativeFreeKickSheetPreparation();
        nativeShotShooter = match.playerTokenManager.allTokens.Single(t => t.playerName.Contains("Noruega")).playerName;
        var shot = match.shotManager;
        LegacyKey("S"); yield return Until(() => shot.isWaitingForShotCommitConfirmation, "free kick requires explicit difficulty-two shot confirmation");
        Assert.That(shot.isActivated, Is.False); LegacyKey("S");
        yield return Until(() => shot.isActivated && shot.isWaitingForTargetSelection, "confirmed free-kick shot opens aiming");
        input.TryExecuteSyntheticHexClick(Hex(19, -1), "Legacy migration", out _);
        yield return Until(() => shot.isActivated && shot.isWaitingForShotRoll, "aimed free-kick shooter rolls before blocks or goalkeeper movement");
        var path = (List<HexCell>)typeof(ShotManager).GetField("trajectoryPath", Private).GetValue(shot);
        Assert.That(path, Is.Not.Empty); Assert.That(path, Does.Contain(Hex(14)));
        var p = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        Assert.That(p["Soares"].currentHex, Is.SameAs(Hex(14))); Assert.That(p["Kuzmic"].currentHex, Is.SameAs(Hex(18)));
        foreach (var step in branch.Steps)
        {
            UnityEngine.Debug.Log($"Native free-kick shot {index + 1}: {step.Type} {step.PlayerName} {step.Roll}");
            yield return ExecuteNativeShootingStep(step, branch.Steps);
        }
        AssertNativeShootingStatistics(branch.Steps);
    }

    private void AssertNativeFreeKickDicePresentation()
    {
        var shot = match.shotManager; if (shot.shotType != "freeKick") return;
        Assert.That(duelPanel.IsOpen && duelPanel.IsAwaitingRoll && duelPanel.playerColumns.activeSelf, Is.True);
        bool attackLeft = (shot.shooter.isHomeTeam ? match.homeTeamDirection : match.awayTeamDirection) == MatchManager.TeamAttackingDirection.LeftToRight;
        var attack = attackLeft ? duelPanel.leftPlayer : duelPanel.rightPlayer;
        var defense = attackLeft ? duelPanel.rightPlayer : duelPanel.leftPlayer;
        Assert.That(attack.playerName.text, Does.Contain(shot.shooter.playerName)); Assert.That(attack.modifier.text, Does.Contain("xGoals"));
        if (shot.isWaitingForShotRoll) Assert.That(attack.die.IsAnimating && !defense.die.IsAnimating, Is.True);
        else
        {
            Assert.That(!attack.die.IsAnimating && attack.total.text.Contains($"Total: {shot.totalShotPower}") && defense.die.IsAnimating, Is.True);
            Assert.That(defense.playerName.text, Does.Contain(LegacyShotField<PlayerToken>(CurrentLegacyShotInteraction(), "defender").playerName));
        }
    }
}
