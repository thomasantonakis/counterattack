using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunTimingJumperOverflow(int side)
    {
        Board(side); match.isClockRunning = true;
        var header = match.shotManager.headerManager;
        Place(attacker, Hex(4 * side, 4));
        var second = Token("Second attacker", true, false, 3); Place(second, Hex(6 * side, 5));
        var third = Token("Third attacker", true, false, 4); Place(third, Hex(5 * side, 4));
        Place(defender, Hex(4 * side, 5));
        var secondDef = Token("Second defender", false, false, 3); Place(secondDef, Hex(3 * side, 3));
        var thirdDef = Token("Third defender", false, false, 4); Place(thirdDef, Hex(3 * side, 4));
        Place(awayGK, Hex(6 * side, 4));
        var passer = Token("Passer", true, false, 2); Place(passer, Hex(0));
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(passer); match.SetHangingPass("aerial");
        AttachAuthoredPanels(); EnableForAuthoredPresentation(header);
        match.ClearLiveActionAvailabilityForStopPlay();
        yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
        Assert.That(header.attEligibleToHead, Is.EquivalentTo(new[] {attacker, second, third}));
        match.AdvanceDecisionTiming(3);
        var attack = match.DecisionTiming.Current;
        string attackId = attack.id; float remaining = attack.remaining;
        LegacyKey("A"); // Three eligible attackers cannot all be nominated.
        Invoke(header, "OnClickReceived", null, Hex(0, 10));
        Assert.That(header.attackerWillJump, Is.Empty);
        foreach (var token in new[] {attacker, second, third}) Invoke(header, "OnClickReceived", token, token.currentHex);
        Assert.That(header.attackerWillJump, Is.EquivalentTo(new[] {attacker, second}));
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(attackId));
        Assert.That(attack.remaining, Is.EqualTo(remaining));
        LegacyKey("Return");
        yield return Until(() => header.isWaitingForDefenderSelection, "overflow attack confirms its legal two-player list");
        Assert.That(header.defEligibleToHead, Does.Contain(awayGK));
        match.AdvanceDecisionTiming(3);
        var defense = match.DecisionTiming.Current;
        string defenseId = defense.id; remaining = defense.remaining;
        LegacyKey("A");
        Invoke(header, "OnClickReceived", null, Hex(0, 10));
        Assert.That(header.defenderWillJump, Is.Empty);
        foreach (var token in new[] {defender, secondDef, thirdDef, awayGK}) Invoke(header, "OnClickReceived", token, token.currentHex);
        Assert.That(header.defenderWillJump, Is.EquivalentTo(new[] {defender, secondDef, awayGK}));
        Invoke(header, "OnClickReceived", awayGK, awayGK.currentHex);
        Assert.That(header.defenderWillJump, Has.No.Member(awayGK));
        Invoke(header, "OnClickReceived", awayGK, awayGK.currentHex);
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(defenseId));
        Assert.That(defense.remaining, Is.EqualTo(remaining));
        LegacyKey("Return");
        yield return Until(() => header.isWaitingForHeaderRoll, "legal capped list enters rolls");
        match.RefreshDecisionTiming();
        Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
        Assert.That(match.DecisionTiming.Current, Is.Null);
    }

    public IEnumerator RunTimingJumperSelection(int side, bool selectAll, bool holdResult, string continuation)
    {
        Board(side); match.isClockRunning = true;
        var header = match.shotManager.headerManager;
        Place(attacker, Hex(4 * side, 4)); attacker.heading = 6;
        var second = Token("Second jumper", true, false, 3); second.heading = 5;
        Place(second, Hex(6 * side, 5));
        Place(defender, Hex(4 * side, 5)); defender.heading = 1;
        var secondDefender = Token("Second defender", false, false, 4);
        Place(secondDefender, Hex(3 * side, 3)); secondDefender.heading = 1;
        var passer = Token("Passer", true, false, 2); Place(passer, Hex(0));
        if (continuation == "goal")
        {
            Invoke(grid, "LoadPrecomputedShootingPaths");
            Place(attacker, Hex(13 * side, 1)); Place(second, Hex(14 * side, 2));
            Place(defender, Hex(14 * side, 1)); Place(secondDefender, Hex(12 * side, 1));
        }
        ball.PlaceAtCell(attacker.currentHex); match.SetLastToken(passer); match.SetHangingPass("aerial");
        AttachAuthoredPanels(); EnableForAuthoredPresentation(header);
        if (holdResult) header.headerChallengePanel.resultSeconds = .1f;
        match.ClearLiveActionAvailabilityForStopPlay();
        yield return header.FindEligibleHeaderTokens(ball.GetCurrentHex());
        Assert.That(header.isWaitingForAttackerSelection, Is.True);
        Assert.That(header.attEligibleToHead, Is.EquivalentTo(new[] {attacker, second}));
        match.RefreshDecisionTiming();
        var attack = match.DecisionTiming.Current;
        Assert.That(attack.kind, Is.EqualTo(DecisionKind.HighPassAttackingJumpers));
        Assert.That(attack.owner, Is.EqualTo("Home"));
        match.AdvanceDecisionTiming(3);
        float remaining = attack.remaining;
        LegacyKey("Return"); // Empty attack cannot confirm.
        Invoke(header, "OnClickReceived", defender, defender.currentHex);
        Invoke(header, "OnClickReceived", attacker, attacker.currentHex);
        Invoke(header, "OnClickReceived", attacker, attacker.currentHex); // Deselect.
        match.RefreshDecisionTiming();
        Assert.That(header.attackerWillJump, Is.Empty);
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(attack.id));
        Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(remaining));
        match.AdvanceDecisionTiming(remaining);
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.True);
        if (selectAll) LegacyKey("A");
        else
        {
            Invoke(header, "OnClickReceived", attacker, attacker.currentHex);
            Invoke(header, "OnClickReceived", second, second.currentHex);
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(attack.id));
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.True);
            LegacyKey("Return");
        }
        if (continuation == "goal")
        {
            yield return Until(() => header.isWaitingForHeaderAtGoal, "confirmed jumpers offer header at goal");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.HeadedPassOrHeaderAtGoal));
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
            match.AdvanceDecisionTiming(2);
            LegacyKey("H");
        }
        yield return Until(() => header.isWaitingForDefenderSelection, "defense receives its whole-list allowance");
        Assert.That(header.attackerWillJump, Is.EquivalentTo(new[] {attacker, second}));
        match.RefreshDecisionTiming();
        var defense = match.DecisionTiming.Current;
        Assert.That(defense.kind, Is.EqualTo(DecisionKind.HighPassDefensiveJumpers));
        Assert.That(defense.owner, Is.EqualTo("Away"));
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
        match.AdvanceDecisionTiming(4); remaining = defense.remaining;
        Invoke(header, "OnClickReceived", attacker, attacker.currentHex);
        Invoke(header, "OnClickReceived", defender, defender.currentHex);
        Invoke(header, "OnClickReceived", defender, defender.currentHex);
        match.RefreshDecisionTiming();
        Assert.That(header.defenderWillJump, Is.Empty);
        Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(defense.id));
        Assert.That(match.DecisionTiming.Current.remaining, Is.EqualTo(remaining));
        match.AdvanceDecisionTiming(remaining);
        if (continuation == "forfeit")
        {
            LegacyKey("Return");
            yield return Until(() => header.isWaitingForControlOrHeaderDecision, "defense forfeit reopens header/control");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.HeaderOrBallControl));
            Assert.That(match.DecisionTiming.Current.owner, Is.EqualTo("Home"));
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
            LegacyKey("H");
            yield return Until(() => header.iswaitingForChallengeWinnerSelection, "free header asks for its winner");
            Invoke(header, "OnClickReceived", attacker, attacker.currentHex);
            yield return Until(() => header.isWaitingForHeaderTargetSelection, "selected free header continues");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.HeadedPassTarget));
            Assert.That(header.challengeWinner, Is.SameAs(attacker));
            yield break;
        }
        if (selectAll) LegacyKey("A");
        else
        {
            Invoke(header, "OnClickReceived", defender, defender.currentHex);
            Invoke(header, "OnClickReceived", secondDefender, secondDefender.currentHex);
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.Current.id, Is.EqualTo(defense.id));
            Assert.That(match.DecisionTiming.HoldsMatchClock, Is.True);
            LegacyKey("Return");
        }
        Assert.That(header.defenderWillJump, Is.EquivalentTo(new[] {defender, secondDefender}));
        foreach (var token in new[] {attacker, second, defender, secondDefender})
        {
            yield return Until(() => header.isWaitingForHeaderRoll && header.tokenRolling == token, "successive jumper roll");
            match.RefreshDecisionTiming();
            Assert.That(match.DecisionTiming.HasPendingRoll, Is.True);
            Assert.That(match.DecisionTiming.Current, Is.Null);
            header.PerformHeaderRoll(new RollInputOverride {hasOverride = true, roll = token.isAttacker ? 6 : 1});
        }
        yield return Until(() => (bool)typeof(HeaderChallengePanelController).GetField("complete", Private).GetValue(header.headerChallengePanel), "blocking header result completes");
        if (holdResult)
        {
            LegacyKey("Return");
            Assert.That(header.headerChallengePanel.IsResultHeld, Is.True);
            float possession = match.gameData.stats.homeTeamStats.totalPossessionSeconds;
            yield return new UnityEngine.WaitForSecondsRealtime(.25f);
            match.AdvanceDecisionTiming(100);
            Invoke(match, "AdvanceMatchTime", 10f);
            Assert.That(header.headerChallengePanel.IsOpen, Is.True);
            Assert.That(header.isWaitingForHeaderTargetSelection, Is.False);
            Assert.That(match.DecisionTiming.BlockingResult, Is.True);
            Assert.That(match.gameData.stats.homeTeamStats.totalPossessionSeconds, Is.EqualTo(possession));
            LegacyKey("Return");
            Assert.That(header.headerChallengePanel.IsResultHeld, Is.False);
            yield return Until(() => header.isWaitingForHeaderTargetSelection, "resumed result expires and continues");
        }
        else
        LegacyKey("Escape");
        yield return Until(() => header.isWaitingForHeaderTargetSelection, "winning jumper reaches headed pass");
        match.RefreshDecisionTiming();
        Assert.That(header.challengeWinner, Is.SameAs(attacker));
        Assert.That(match.DecisionTiming.Current.kind, Is.EqualTo(DecisionKind.HeadedPassTarget));
        Assert.That(match.DecisionTiming.HoldsMatchClock, Is.False);
    }
}
