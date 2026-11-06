using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunHeaderSelectionLegacy(bool rollOrder)
    {
        Board(1); Invoke(grid, "LoadPrecomputedShootingPaths");
        var header = match.shotManager.headerManager;
        attacker.playerName = attacker.name = "Nazef"; attacker.heading = 6;
        Place(attacker, Hex(4, 4));
        var kalla = Token("Kalla", true, false, 3); kalla.heading = 5; Place(kalla, Hex(6, 5));
        defender.playerName = defender.name = rollOrder ? "Soares" : "Gilbert";
        defender.heading = 1; Place(defender, rollOrder ? Hex(4, 5) : Hex(3, 3));
        PlayerToken stewart = null;
        if (rollOrder) { Place(awayGK, Hex(6, 4)); awayGK.aerial = 1; }
        else { stewart = Token("Stewart", false, false, 8); Place(stewart, Hex(4, 5)); }
        var passer = Token("Cafferata", true, false, 2); Place(passer, Hex(0));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(passer); match.SetHangingPass("aerial");
        AttachAuthoredPanels(); EnableForAuthoredPresentation(header);
        match.ClearLiveActionAvailabilityForStopPlay();
        header.StartCoroutine(header.FindEligibleHeaderTokens(ball.GetCurrentHex()));
        yield return Until(() => header.isWaitingForAttackerSelection, "real landing eligibility opens attacker header nominations");
        Assert.That(header.attEligibleToHead, Is.EquivalentTo(new[] { attacker, kalla }));
        input.TryExecuteSyntheticHexClick(kalla.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(attacker.currentHex, "Legacy migration", out _);
        Assert.That(header.attackerWillJump.Count, Is.EqualTo(2));
        LegacyKey("Return");
        yield return Until(() => header.isWaitingForDefenderSelection, "attacker confirmation opens defending nominations");
        if (!rollOrder)
        {
            LegacyKey("Return");
            yield return Until(() => header.isWaitingForControlOrHeaderDecision, "defense forfeit reoffers an unchallenged Header/Control choice");
            Assert.That(header.attackerWillJump.Count + header.defenderWillJump.Count, Is.EqualTo(0));
            Assert.That(header.hasEligibleDefenders, Is.False);
            Assert.That(header.attEligibleToHead, Is.EquivalentTo(new[] { attacker, kalla }));
            LegacyKey("H");
            yield return Until(() => header.iswaitingForChallengeWinnerSelection, "free header with two eligible attackers asks for exactly one winner");
            input.TryExecuteSyntheticHexClick(attacker.currentHex, "Legacy migration", out _);
            yield return Until(() => header.isWaitingForHeaderTargetSelection || header.isWaitingForHeaderAtGoal, "selected free header winner continues");
            if (header.isWaitingForHeaderAtGoal) LegacyKey("H");
            yield return Until(() => header.isWaitingForHeaderTargetSelection, "declining header at goal preserves headed-pass selection");
            Assert.That(header.attackerWillJump, Is.EqualTo(new[] { attacker }));
            Assert.That(header.challengeWinner, Is.SameAs(attacker));
            yield break;
        }
        // Nominate GK first to prove actual sorting keeps him last.
        input.TryExecuteSyntheticHexClick(awayGK.currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(defender.currentHex, "Legacy migration", out _);
        Assert.That(header.defenderWillJump, Is.EquivalentTo(new[] { awayGK, defender }));
        LegacyKey("Return");
        var expected = new[] { attacker, kalla, defender, awayGK };
        int[] rolls = { 6, 5, 1, 1 };
        for (int i = 0; i < expected.Length; i++)
        {
            var current = expected[i];
            yield return Until(() => header.isWaitingForHeaderRoll && header.tokenRolling == current, $"header roll {i + 1} belongs to {current.playerName}");
            header.PerformHeaderRoll(new RollInputOverride { hasOverride = true, roll = rolls[i], isJackpot = false });
        }
        var panel = header.headerChallengePanel;
        yield return Until(() => (bool)typeof(HeaderChallengePanelController).GetField("complete", Private).GetValue(panel), "all actual header rolls complete before the authored result gate");
        Assert.That(panel.IsOpen && !header.isWaitingForHeaderTargetSelection, Is.True);
        LegacyKey("Escape");
        yield return Until(() => header.isWaitingForHeaderTargetSelection, "dismissed aerial result exposes header target selection");
        Assert.That(header.challengeWinner, Is.SameAs(attacker));
        Assert.That(header.defenderWillJump[^1], Is.SameAs(awayGK));
    }
}
