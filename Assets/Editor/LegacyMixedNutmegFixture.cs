using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunMixedNutmegLegacy()
    {
        var players = LoadLegacyRoster(); var dribbler = players["Yaneva"];
        Place(dribbler, Hex(6, 2)); Place(players["Nazef"], Hex(8, 6));
        Place(players["Paterson"], Hex(4, 4)); Place(players["Vladoiu"], Hex(3, 4));
        Place(players["McNulty"], Hex(4, 6)); Place(players["Gilbert"], Hex(3, 5));
        ball.PlaceAtCell(dribbler.currentHex); match.SetLastToken(dribbler);
        Subscribe(movement); match.BroadcastSafeEndofMovementPhase(false, false);
        LegacyKey("M"); input.TryExecuteSyntheticHexClick(dribbler.currentHex, "Legacy migration", out _);
        foreach (var step in new[] { Hex(6, 3), Hex(6, 4), Hex(5, 4) })
            yield return LegacyDribbleStep(dribbler, step);
        Assert.That(movement.isWaitingForNutmegDecision, Is.True);
        Assert.That(movement.nutmeggableDefenders, Does.Contain(players["Paterson"]));
        Assert.That(movement.nutmeggableDefenders, Has.No.Member(players["Stewart"]));
    }
}
