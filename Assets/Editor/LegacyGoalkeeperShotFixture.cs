using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunOutsideGoalkeeperIneligibleBlockLegacy()
    {
        Board(1); match.difficulty_level = 3;
        Place(attacker, Hex(8, 12)); Place(awayGK, Hex(12, 9));
        ball.PlaceAtCell(attacker.currentHex); match.UpdatePossessionAfterPass(attacker.currentHex);
        var shot = match.shotManager;
        Invoke(shot, "ResetShotProcess");
        shot.shooter = attacker; shot.shotType = "snapshot"; shot.isActivated = true;
        shot.targetHex = Hex(19); shot.alreadyInterceptedDefs = new List<PlayerToken>();
        Set(shot, "trajectoryPath", new List<HexCell> { Hex(12, 12) });
        Assert.That(keeper.IsGoalkeeperOwnPenaltyHex(awayGK, awayGK.currentHex), Is.False);
        Subscribe(shot);
        shot.StartCoroutine((IEnumerator)Invoke(shot, "StartInterceptionPhase"));
        yield return Until(() => shot.isWaitingForOutsideBoxGKShotDecision, "all outside save candidates offer dive/block choice");
        LegacyKey("B");
        yield return Until(() => shot.isWaitingForShotRoll, "ineligible goalkeeper block continues to shooter roll");
        Assert.That(shot.isWaitingForBlockDiceRoll || shot.isWaitingForGKDiceRoll, Is.False);
        Assert.That(awayGK.isSentOff, Is.False);
    }
}
