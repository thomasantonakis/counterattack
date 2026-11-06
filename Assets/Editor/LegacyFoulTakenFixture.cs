using System;
using System.Collections;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunFoulTakenLegacy(bool alreadyBooked, bool alreadyInjured)
    {
        yield return PrepareLegacySoaresStealFoul();
        var dribbler = movement.selectedToken;
        var interceptor = movement.selectedDefender;
        var penalty = Add<PenaltyKickManager>();
        penalty.Configure(match, grid, ball, movement, match.shotManager); match.penaltyKickManager = penalty;
        Subscribe(penalty);
        if (alreadyBooked) interceptor.ReceiveYellowCard();
        if (alreadyInjured) dribbler.ReceiveInjury();
        movement.PerformLeniencyTest(6);
        var stats = match.gameData.stats.GetPlayerStats(interceptor);
        if (alreadyBooked) Assert.That(stats.redCards, Is.EqualTo(1));
        else Assert.That(stats.yellowCards, Is.EqualTo(1));
        Assert.That(movement.isWaitingForFoulDecision, Is.False);
        yield return Until(() => movement.isWaitingForInjuryRoll, "booking resolves before the foul injury check");
        movement.PerformInjuryTest(6);
        yield return DismissLegacyDuel(false);
        if (!alreadyInjured) Assert.That(match.gameData.stats.GetPlayerStats(dribbler).injuries, Is.EqualTo(1));
        if (alreadyInjured)
        {
            yield return Until(() => dribbler.requiresSubstitution && match.IsPauseMenuOpen,
                "second injury opens the mandatory substitution stoppage after the result gate");
            Assert.That(movement.isWaitingForFoulDecision || penalty.isActivated, Is.False);
            var incoming = match.playerTokenManager.benchTokens.Find(t => t.isHomeTeam && !t.IsGoalKeeper);
            Assert.That(incoming, Is.Not.Null);
            // Supply the user's selected row, then invoke the real menu confirmation.
            // The controller validates, registers and performs the substitution;
            // no placement or requirement-clearing algorithm is copied into tests.
            var menu = Add<SubstitutionMenuManager>();
            var rowType = typeof(SubstitutionMenuManager).GetNestedType("SelectionRow", System.Reflection.BindingFlags.NonPublic);
            var row = Activator.CreateInstance(rowType, true);
            rowType.GetField("isHomeTeam").SetValue(row, true);
            rowType.GetField("selectedOutgoing").SetValue(row, dribbler);
            rowType.GetField("selectedIncoming").SetValue(row, incoming);
            ((IList)typeof(SubstitutionMenuManager).GetField("selectionRows", Private).GetValue(menu)).Add(row);
            var destination = dribbler.currentHex;
            Invoke(menu, "ConfirmSubstitutions");
            Assert.That(incoming.isPlaying && !dribbler.isPlaying, Is.True);
            Assert.That(incoming.currentHex, Is.SameAs(destination));
            Assert.That(dribbler.requiresSubstitution, Is.False);
            Assert.That(match.gameData.stats.homeTeamStats.totalSubstiutions, Is.EqualTo(1));
            match.SetPauseMenuOpen(false); // same public pause-close entry point as the authored menu
        }
        bool forced = alreadyBooked || alreadyInjured;
        if (!forced)
        {
            yield return Until(() => movement.isWaitingForFoulDecision, "first booking/injury retains the Play On / Take Foul choice");
            Assert.That(movement.PendingFoulIsPenalty, Is.True);
            LegacyKey("F");
        }
        yield return Until(() => penalty.isActivated && penalty.isWaitingForKickerSelection,
            "taking the foul inside the opponent box starts the penalty preparation");
        Assert.That(movement.isWaitingForFoulDecision || movement.isActivated, Is.False);
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.PenaltyKickerSelect));
        Assert.That(penalty.penaltySpot, Is.SameAs(Hex(14)));
        Assert.That(match.freeKickManager.isActivated || match.finalThirdManager.isActivated, Is.False);
        Assert.That(interceptor.isSentOff, Is.EqualTo(alreadyBooked));
    }
}
