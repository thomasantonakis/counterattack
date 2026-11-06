using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunInaccurateHeaderLegacy(string branch)
    {
        yield return PrepareLegacyInaccurateHeader();
        var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        var header = match.shotManager.headerManager;
        var nazef = players["Nazef"]; var kalla = players["Kalla"];
        Assert.That(header.isActivated && header.isWaitingForAttackerSelection, Is.True);
        Assert.That(header.attEligibleToHead, Is.EquivalentTo(new[] { nazef, kalla }));
        Assert.That(header.defEligibleToHead, Is.EquivalentTo(new[] { players["Gilbert"], players["Paterson"], players["Stewart"], players["McNulty"] }));
        if (branch == "landing") yield break;
        bool addKalla = branch.EndsWith("/two"); string action = branch.Split('/')[0];
        input.TryExecuteSyntheticHexClick(players["Pavlovic"].currentHex, "Legacy migration", out _);
        Assert.That(header.attackerWillJump.Count, Is.EqualTo(0));
        Assert.That(header.isWaitingForAttackerSelection, Is.True);
        input.TryExecuteSyntheticHexClick(nazef.currentHex, "Legacy migration", out _);
        Assert.That(header.attackerWillJump, Does.Contain(nazef));
        input.TryExecuteSyntheticHexClick(nazef.currentHex, "Legacy migration", out _);
        Assert.That(header.attackerWillJump, Has.No.Member(nazef));
        input.TryExecuteSyntheticHexClick(nazef.currentHex, "Legacy migration", out _);
        if (addKalla) input.TryExecuteSyntheticHexClick(kalla.currentHex, "Legacy migration", out _);
        Assert.That(header.attackerWillJump, Is.EquivalentTo(addKalla ? new[] { nazef, kalla } : new[] { nazef }));
        Assert.That(header.isWaitingForDefenderSelection, Is.False);
        Assert.That(header.attEligibleToHead, Is.EquivalentTo(new[] { nazef, kalla }));
        Assert.That(header.defEligibleToHead.Count, Is.EqualTo(4));
        LegacyKey("Return");
        yield return Until(() => header.isWaitingForDefenderSelection, "confirmed attacking header nominations hand input to defense");
        Assert.That(header.isWaitingForAttackerSelection, Is.False);
        if (action == "attack") yield break;
        if (action == "defense")
        {
            input.TryExecuteSyntheticHexClick(players["Gilbert"].currentHex, "Legacy migration", out _);
            Assert.That(header.defenderWillJump, Does.Contain(players["Gilbert"]));
            input.TryExecuteSyntheticHexClick(players["Vladoiu"].currentHex, "Legacy migration", out _);
            Assert.That(header.defenderWillJump, Has.No.Member(players["Vladoiu"]));
            Assert.That(header.defenderWillJump, Does.Contain(players["Gilbert"]));
            input.TryExecuteSyntheticHexClick(players["Gilbert"].currentHex, "Legacy migration", out _);
            Assert.That(header.defenderWillJump, Has.No.Member(players["Gilbert"]));
            input.TryExecuteSyntheticHexClick(players["McNulty"].currentHex, "Legacy migration", out _);
            input.TryExecuteSyntheticHexClick(players["Stewart"].currentHex, "Legacy migration", out _);
            Assert.That(header.defenderWillJump, Is.EquivalentTo(new[] { players["McNulty"], players["Stewart"] }));
            Assert.That(header.attackerWillJump, Is.EquivalentTo(addKalla ? new[] { nazef, kalla } : new[] { nazef }));
            Assert.That(header.defEligibleToHead.Count, Is.EqualTo(4));
            LegacyKey("Return");
            yield return Until(() => header.isWaitingForHeaderRoll, "both sides confirm and actual header rolling begins");
            Assert.That(header.isWaitingForAttackerSelection || header.isWaitingForDefenderSelection, Is.False);
            yield break;
        }
        LegacyKey("Return");
        yield return Until(() => header.isWaitingForControlOrHeaderDecision, "defense forfeit reoffers the unchallenged Header/Control choice");
        if (action == "forfeit") yield break;
        // The newer 033a contract clears contested nominations after a defense
        // forfeit and asks for one fresh winner from all eligible attackers.
        Assert.That(header.attackerWillJump.Count + header.defenderWillJump.Count, Is.EqualTo(0));
        LegacyKey(action == "head" ? "H" : "B");
        yield return Until(() => header.iswaitingForChallengeWinnerSelection, "unchallenged choice selects exactly one of the eligible attackers");
        input.TryExecuteSyntheticHexClick(nazef.currentHex, "Legacy migration", out _);
        yield return Until(() => action == "head" ? header.isWaitingForHeaderTargetSelection : header.isWaitingForControlRoll,
            "Nazef continues to the selected unchallenged header/control action");
        Assert.That(header.isWaitingForControlOrHeaderDecision || header.iswaitingForChallengeWinnerSelection, Is.False);
        Assert.That(header.challengeWinner, Is.SameAs(nazef));
        if (action == "head") Assert.That(header.attackerWillJump, Is.EqualTo(new[] { nazef }));
    }

    private IEnumerator PrepareLegacyInaccurateHeader()
    {
        var players = LoadLegacyRoster(); var header = match.shotManager.headerManager;
        Subscribe(high); Subscribe(movement); Subscribe(match.finalThirdManager); Subscribe(header);
        LegacyKey("C");
        input.TryExecuteSyntheticHexClick(players["Nazef"].currentHex, "Legacy migration", out _);
        Assert.That(high.eligibleAttackers.Count, Is.EqualTo(1));
        Assert.That(high.currentTargetHex, Is.SameAs(Hex(4, 4)));
        input.TryExecuteSyntheticHexClick(players["Nazef"].currentHex, "Legacy migration", out _);
        yield return Until(() => high.isWaitingForAttackerSelection, "inaccurate-header audit commits its high pass");
        Assert.That(high.intendedTargetHex, Is.SameAs(Hex(4, 4)));
        Assert.That(high.lockedAttacker, Is.SameAs(players["Nazef"]));
        input.TryExecuteSyntheticHexClick(players["Cafferata"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(true, true);
        input.TryExecuteSyntheticHexClick(Hex(-1, -1), "Legacy migration", out _);
        yield return Until(() => players["Cafferata"].currentHex == Hex(-1, -1) && high.isWaitingForDefenderSelection,
            "passer moves while the ball remains at the original high-pass source");
        AssertHighPassMovementSelection(false, false);
        input.TryExecuteSyntheticHexClick(players["Vladoiu"].currentHex, "Legacy migration", out _);
        AssertHighPassMovementSelection(false, true);
        input.TryExecuteSyntheticHexClick(Hex(1, 3), "Legacy migration", out _);
        yield return Until(() => players["Vladoiu"].currentHex == Hex(1, 3) && high.isWaitingForAccuracyRoll,
            "defender movement completes before accuracy resolution");
        Assert.That(high.isWaitingForAttackerSelection || high.isWaitingForAttackerMove
            || high.isWaitingForDefenderSelection || high.isWaitingForDefenderMove, Is.False);
        high.PerformAccuracyRoll(1);
        yield return Until(() => high.isWaitingForDirectionRoll, "failed accuracy clears the committed target and opens direction");
        Assert.That(grid.highlightedHexes, Has.No.Member(high.currentTargetHex));
        Assert.That(high.isWaitingForAccuracyRoll, Is.False);
        high.PerformDirectionRoll(5);
        yield return Until(() => high.isWaitingForDistanceRoll, "northeast scatter opens distance");
        Assert.That(high.isWaitingForDirectionRoll, Is.False);
        high.PerformDistanceRoll(1);
        yield return Until(() => !high.isActivated && header.isWaitingForAttackerSelection, "inaccurate high-pass landing transfers to contested header selection", 120);
        Assert.That(high.isWaitingForDistanceRoll || match.finalThirdManager.isActivated, Is.False);
        Assert.That(ball.GetCurrentHex(), Is.SameAs(Hex(5, 4)));
    }
}
