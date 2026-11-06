using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOutsideKeeperShotLegacy(string branch)
    {
        var players = LoadLegacyRoster();
        var shot = match.shotManager; var shooter = players["Yaneva"]; var gk = players["Kuzmic"];
        var blocker = branch == "failed_save" ? players["Poulsen"] : null;
        var shooterHex = branch == "contact" ? Hex(8, 12) : Hex(8, 10);
        var gkHex = branch == "contact" ? Hex(12, 9) : Hex(12, 10);
        var path = branch == "failed_save" ? new List<HexCell> { Hex(12, 10), Hex(14, 10) }
            : new List<HexCell> { branch == "contact" ? Hex(12, 12) : Hex(12, 10) };
        var reserved = new HashSet<HexCell>(path.Concat(new[] { shooterHex, gkHex, Hex(19) }));
        foreach (var h in reserved.ToArray()) foreach (var n in h.GetNeighbors(grid)) if (n != null) reserved.Add(n);
        foreach (var token in match.playerTokenManager.allTokens.Where(t => t.isPlaying && t != shooter && t != gk && t != blocker))
            if (reserved.Contains(token.currentHex))
                Place(token, grid.cells.Cast<HexCell>().First(h => h != null && !h.isOutOfBounds && h.isInGoal == 0
                    && !reserved.Contains(h) && !h.isAttackOccupied && !h.isDefenseOccupied && h.GetOccupyingToken() == null));
        Place(shooter, shooterHex); Place(gk, gkHex);
        if (blocker != null) Place(blocker, Hex(14, 10));
        ball.PlaceAtCell(shooterHex); match.SetLastToken(shooter); match.UpdatePossessionAfterPass(shooterHex);
        movement.ResetMovementPhase(); match.currentState = MatchManager.GameState.EndOfMovementPhase;
        Invoke(shot, "ResetShotProcess");
        shot.shooter = shooter; shot.shotType = branch == "free_kick" ? "freeKick" : "fullPower";
        shot.isActivated = true; shot.targetHex = Hex(19); shot.alreadyInterceptedDefs = new List<PlayerToken>();
        Set(shot, "trajectoryPath", path);
        Assert.That(keeper.IsGoalkeeperOwnPenaltyHex(gk, gkHex), Is.False);
        AttachAuthoredPanels(); EnableForAuthoredPresentation(shot);
        Subscribe(match.finalThirdManager); Subscribe(match.freeKickManager);
        if (branch == "free_kick")
        {
            // Resolve an actual shooter roll, rather than seeding totalShotPower
            // and shooterRoll as the old phase-level scenario did.
            shot.StartCoroutine(shot.StartShotRoll(4));
        }
        else shot.StartCoroutine((IEnumerator)Invoke(shot, "StartInterceptionPhase"));
        yield return Until(() => shot.isWaitingForOutsideBoxGKShotDecision,
            "all outside save candidates offer the goalkeeper Dive/Block choice before any save");
        string prompt = shot.GetInstructions();
        Assert.That(prompt, Does.Contain("[D]")); Assert.That(prompt, Does.Contain("[B]"));
        Assert.That(gk.currentHex, Is.SameAs(gkHex));
        Assert.That(shot.isWaitingForGKDiceRoll, Is.False);
        if (branch == "free_kick")
        {
            Assert.That(shot.shooterRoll, Is.EqualTo(4)); Assert.That(shot.totalShotPower, Is.GreaterThan(0));
            yield break;
        }
        if (branch == "block")
        {
            Assert.That(prompt, Does.Contain("Tackling: 0"));
            LegacyKey("B");
            yield return Until(() => shot.isWaitingForBlockDiceRoll, "outside goalkeeper chooses a real outfield block");
            var entry = CurrentLegacyShotInteraction();
            Assert.That(LegacyShotField<PlayerToken>(entry, "defender"), Is.SameAs(gk));
            Assert.That(LegacyShotField<int?>(entry, "effectiveTackling"), Is.EqualTo(0));
            Assert.That(LegacyShotField<int>(entry, "requiredNaturalRoll"), Is.GreaterThan(6));
            Assert.That(shot.GetInstructions(), Does.Contain("Tackling: 0"));
            Assert.That(shot.GetInstructions(), Does.Contain("Jackpot"));
            Assert.That(gk.isSentOff, Is.False);
            shot.StartCoroutine(InvokeLegacyShotRoll("StartShotBlockRoll", 6));
            yield return DismissLegacyDuel(false);
            yield return Until(() => shot.isWaitingForShotRoll, "ordinary six cannot block with zero tackling; shooter rolls next");
            Assert.That(gk.isSentOff, Is.False);
            yield break;
        }
        LegacyKey("D");
        yield return Until(() => shot.isWaitingForShotRoll, "outside goalkeeper dive lets the shooter roll first");
        shot.StartCoroutine(shot.StartShotRoll(new RollInputOverride { hasOverride = true,
            roll = branch == "contact" ? 2 : 6, isJackpot = branch == "failed_save" }));
        yield return Until(() => shot.isWaitingForGKDiceRoll, "actual shooter roll opens goalkeeper saving roll");
        var save = CurrentLegacyShotInteraction();
        Assert.That(LegacyShotField<PlayerToken>(save, "defender"), Is.SameAs(gk));
        if (branch == "contact")
            Assert.That(gk.saving + (LegacyShotField<int?>(save, "gkPenalty") ?? 0) + 6,
                Is.GreaterThanOrEqualTo(shot.totalShotPower));
        else Assert.That(shot.totalShotPower, Is.EqualTo(50), "Actual jackpot supplies the failed-save shot power.");
        shot.StartCoroutine(InvokeLegacyShotRoll("ResolveGKSavingAttempt", branch == "contact" ? 6 : 1, save));
        yield return DismissLegacyDuel(false);
        if (branch == "contact")
        {
            yield return Until(() => gk.isSentOff && (match.finalThirdManager.isActivated || match.freeKickManager.isWaitingForKickerSelection),
                "outside-box hand contact forces a straight red and free-kick preparation");
            Assert.That(ball.GetCurrentHex(), Is.SameAs(path[0]));
            Assert.That(gk.currentHex, Is.Null, "The straight red removes the goalkeeper from the pitch.");
            Assert.That(gk.isPlaying, Is.False);
            Assert.That(match.GetGoalkeeperReplacementDestinationHex(gk.isHomeTeam), Is.SameAs(gkHex),
                "Illegal contact preserves the original goalkeeper position for replacement rather than attracting to the save hex.");
            Assert.That(path[0].GetOccupyingToken(), Is.Not.SameAs(gk));
            Assert.That(shot.isWaitingforHandlingTest || loose.isActivated, Is.False);
        }
        else
        {
            yield return Until(() => shot.isWaitingForBlockDiceRoll, "failed outside dive continues to the next outfield blocker");
            Assert.That(LegacyShotField<PlayerToken>(CurrentLegacyShotInteraction(), "defender"), Is.SameAs(blocker));
            Assert.That(gk.isSentOff || match.freeKickManager.isWaitingForKickerSelection, Is.False);
        }
    }

    private object CurrentLegacyShotInteraction()
        => typeof(ShotManager).GetField("currentShotInteraction", Private).GetValue(match.shotManager);
    private static T LegacyShotField<T>(object interaction, string field)
        => (T)interaction.GetType().GetField(field).GetValue(interaction);
    private IEnumerator InvokeLegacyShotRoll(string name, int roll, object interaction = null)
    {
        var method = typeof(ShotManager).GetMethods(Private).Single(m => m.Name == name
            && m.GetParameters().Last().ParameterType == typeof(int?));
        return (IEnumerator)method.Invoke(match.shotManager, interaction == null
            ? new object[] { (int?)roll } : new object[] { interaction, (int?)roll });
    }
}
