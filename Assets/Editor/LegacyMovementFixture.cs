using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    // Legacy 009's exact safe move sequence. All destinations are selected through
    // the same input dispatcher and movement pathfinder used by gameplay.
    public IEnumerator RunSafeMovementLegacy()
    {
        Board(1); attacker.name = attacker.playerName = "Cafferata"; attacker.jerseyNumber = 2;
        Place(attacker, Hex(0)); ball.PlaceAtCell(attacker.currentHex);
        defender.name = defender.playerName = "Vladoiu"; defender.jerseyNumber = 5; Place(defender, Hex(1, 2));
        PlayerToken Player(string name, bool home, int jersey, int x, int z)
        {
            var token = Token(name, home, false, jersey); Place(token, Hex(x, z)); return token;
        }
        var ulisses = Player("Ulisses", true, 11, -8, -8);
        var pavlovic = Player("Pavlovic", true, 9, -4, -4);
        var nazef = Player("Nazef", true, 6, 4, 4);
        var noruega = Player("André Noruega", true, 10, -6, -6);
        var murphy = Player("Murphy", true, 5, 12, 12);
        var receiver = Player("Ground receiver", true, 8, 10, 0);
        var marell = Player("Marell", false, 4, 1, 10);
        var soares = Player("Soares", false, 10, 14, 0);
        var poulsen = Player("Poulsen", false, 11, 18, 0);
        var delgado = Player("Delgado", false, 3, 1, -10);
        Subscribe(ground); Subscribe(movement); Subscribe(match.finalThirdManager);
        match.BroadcastSafeEndofMovementPhase(false, false);
        LegacyKey("P"); ClickPass(receiver.currentHex); ClickPass(receiver.currentHex);
        yield return Until(() => !ground.isActivated && ball.GetCurrentHex() == receiver.currentHex, "ground pass enables movement choice");
        yield return ForfeitLegacyFinalThirds();
        LegacyKey("M");
        Assert.That(movement.isAwaitingTokenSelection, Is.True);
        Assert.That(movement.isAwaitingHexDestination, Is.False);
        var moved = new List<PlayerToken>();
        var attackers = new[] { ulisses, pavlovic, attacker, nazef };
        var attackTargets = new[] { Hex(-3, -8), Hex(0, -3), Hex(5), Hex(0, 6) };
        for (int i = 0; i < attackers.Length; i++)
        {
            Assert.That(movement.isMovementPhaseAttack && !movement.isMovementPhaseDef, Is.True);
            yield return MoveLegacySelectedToken(attackers[i], attackTargets[i]);
            Assert.That(movement.attackersMoved, Is.EqualTo(i + 1));
            moved.Add(attackers[i]);
            Assert.That(movement.movedTokens, Is.EquivalentTo(moved));
            Assert.That(movement.isAvailable || ftp.isAvailable || ground.isAvailable || high.isAvailable || longBall.isAvailable, Is.False);
            Assert.That(ground.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
        }
        Assert.That(movement.isMovementPhaseDef && !movement.isMovementPhaseAttack, Is.True);
        Assert.That(movement.defendersMoved, Is.EqualTo(0));
        var defenders = new[] { defender, marell, soares, poulsen, delgado };
        var defenseTargets = new[] { Hex(-4, 5), Hex(5, 10), Hex(14, 5), Hex(14), Hex(6, -9) };
        for (int i = 0; i < defenders.Length; i++)
        {
            yield return MoveLegacySelectedToken(defenders[i], defenseTargets[i]);
            Assert.That(movement.defendersMoved, Is.EqualTo(i + 1));
            moved.Add(defenders[i]);
            Assert.That(movement.movedTokens, Is.EquivalentTo(moved));
        }
        Assert.That(movement.isMovementPhase2f2, Is.True);
        yield return MoveLegacySelectedToken(noruega, Hex(-8, -6));
        Assert.That(movement.attackersMovedIn2f2, Is.EqualTo(1));
        yield return MoveLegacySelectedToken(murphy, Hex(10, 12), endsPhase: true);
        yield return ForfeitLegacyFinalThirds();
        Assert.That(match.attackHasPossession, Is.True);
        Assert.That(movement.isAvailable && ground.isAvailable && high.isAvailable && longBall.isAvailable, Is.True);
        Assert.That(ground.imposedDistance, Is.EqualTo(11));
        Assert.That(ftp.isAvailable || ftp.isActivated || ground.isActivated || movement.isActivated
            || high.isActivated || longBall.isActivated, Is.False);
    }

    private IEnumerator MoveLegacySelectedToken(PlayerToken token, HexCell destination, bool endsPhase = false)
    {
        Assert.That(movement.isAwaitingTokenSelection, Is.True);
        input.TryExecuteSyntheticHexClick(token.currentHex, "Legacy migration", out _);
        Assert.That(movement.selectedToken, Is.SameAs(token));
        Assert.That(movement.isAwaitingTokenSelection && movement.isAwaitingHexDestination, Is.True);
        input.TryExecuteSyntheticHexClick(destination, "Legacy migration", out _);
        yield return Until(() => token.currentHex == destination && !movement.isPlayerMoving
            && (endsPhase ? !movement.isActivated : movement.movedTokens.Contains(token)), $"{token.playerName} finishes movement to {destination.coordinates}");
        Assert.That(movement.isAwaitingHexDestination, Is.False);
    }
}
