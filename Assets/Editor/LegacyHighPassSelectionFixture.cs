using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunHighPassSelectionLegacy(string branch)
    {
        var players = LoadLegacyRoster();
        Subscribe(high); Subscribe(movement); Subscribe(match.finalThirdManager);
        var target = players["Yaneva"].currentHex;
        if (branch == "keeper_kick")
        {
            match.ChangePossession();
            var gk = players["Kuzmic"]; var teammate = players["Abraham"];
            Place(players["Cafferata"], Hex(-2));
            Place(teammate, Hex(0)); ball.PlaceAtCell(gk.currentHex);
            match.ClearLastTokenChain(); match.SetLastToken(gk);
            match.MarkSetPieceTakerForNextTouchExclusion(gk);
            match.BroadcastGoalKickRestartOptions(gk);
            high.CleanUpHighPass(); high.ActivateGoalkeeperKick();
            Assert.That(high.isGoalkeeperKick && high.isWaitingForConfirmation, Is.True);
            high.HandleHighPassProcess(Hex(15, -1), true);
            Assert.That(high.currentTargetHex, Is.Null);
            Assert.That(high.isWaitingForConfirmation, Is.True);
            Place(teammate, Hex(-12)); high.HandleHighPassProcess(teammate.currentHex, true);
            Assert.That(high.currentTargetHex, Is.Null, "Keeper kick rejects the opposite final third even with an attacker there.");
            Place(teammate, Hex(0));
            Assert.That(HexGridUtils.GetHexStepDistance(gk.currentHex, teammate.currentHex), Is.GreaterThan(15));
            high.HandleHighPassProcess(teammate.currentHex, true);
            Assert.That(high.currentTargetHex, Is.SameAs(teammate.currentHex));
            high.HandleHighPassProcess(teammate.currentHex, true);
            yield return Until(() => high.isWaitingForAttackerSelection, "keeper kick enters normal high-pass attacker movement");
            Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.HighPass));
            Assert.That(high.lockedAttacker, Is.SameAs(teammate));
            LegacyKey("X");
            yield return Until(() => high.isWaitingForDefenderSelection, "keeper kick uses normal defender movement after attacker forfeit");
            yield break;
        }
        bool easy = branch == "easy";
        match.difficulty_level = easy ? 1 : 3;
        LegacyKey("C");
        Assert.That(high.isActivated && high.isWaitingForConfirmation, Is.True);
        Assert.That(match.currentState == MatchManager.GameState.HighPass, Is.EqualTo(!easy));
        if (easy)
        {
            Assert.That(high.GetInstructions(), Does.Contain("3-15"));
            yield return Until(() => high.isAvailableTargetsReady, "easy high-pass target cache is ready before checking highlights");
            Assert.That(grid.highlightedHexes, Does.Contain(target));
            AssertLegacyColor(target, Color.yellow);
            Invoke(high, "OnHoverReceived", players["Yaneva"], target); yield return null;
            AssertLegacyColor(target, new Color(1, .55f, 0));
            Invoke(high, "OnHoverReceived", null, null); yield return null;
            AssertLegacyColor(target, Color.yellow);
            input.TryExecuteSyntheticHexClick(Hex(0, 1), "Legacy migration", out _);
            Assert.That(high.currentTargetHex, Is.Null);
            Assert.That(high.isWaitingForConfirmation, Is.True);
            Assert.That(grid.highlightedHexes, Does.Contain(target));
            input.TryExecuteSyntheticHexClick(players["Abraham"].currentHex, "Legacy migration", out _);
            Assert.That(high.currentTargetHex, Is.Null);
            Assert.That(high.isWaitingForConfirmation, Is.True);
            Assert.That(grid.highlightedHexes, Does.Contain(target));
            var minimum = grid.cells.Cast<HexCell>().Where(h => h != null && !h.isOutOfBounds && h != target)
                .OrderBy(h => HexGridUtils.GetHexStepDistance(ball.GetCurrentHex(), h))
                .First(h => HexGridUtils.GetHexStepDistance(ball.GetCurrentHex(), h) >= 3
                    && (bool)Invoke(high, "TryValidateHighPassTarget", h, false, false, false));
            Assert.That(HexGridUtils.GetHexStepDistance(ball.GetCurrentHex(), minimum), Is.EqualTo(3));
            input.TryExecuteSyntheticHexClick(minimum, "Legacy migration", out _);
            Assert.That(high.currentTargetHex, Is.SameAs(minimum));
            Assert.That(high.isWaitingForConfirmation, Is.True);
            Assert.That(match.currentState, Is.Not.EqualTo(MatchManager.GameState.HighPass));
            AssertLegacyColor(minimum, new Color(1, .55f, 0));
            input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
            Assert.That(high.currentTargetHex, Is.SameAs(target));
            Assert.That(high.isWaitingForConfirmation, Is.True);
            AssertLegacyColor(target, new Color(1, .55f, 0));
        }
        else Assert.That(high.GetInstructions(), Does.Contain("already committed"));
        input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
        yield return Until(() => high.isWaitingForAttackerSelection, "confirmed high-pass target enters attacker movement");
        Assert.That(high.currentTargetHex, Is.SameAs(target));
        Assert.That(high.isWaitingForConfirmation, Is.False);
        Assert.That(high.lockedAttacker, Is.SameAs(players["Yaneva"]));
        Assert.That(match.currentState, Is.EqualTo(MatchManager.GameState.HighPass));
        if (easy) Assert.That(high.GetInstructions(), Does.Contain("[X]"));
        LegacyKey("X");
        yield return Until(() => high.isWaitingForDefenderSelection, "attacker high-pass forfeit hands input to defense");
        Assert.That(high.isWaitingForAttackerSelection, Is.False);
        if (easy)
        {
            input.TryExecuteSyntheticHexClick(players["Soares"].currentHex, "Legacy migration", out _);
            yield return Until(() => high.isWaitingForDefenderSelection && high.isWaitingForDefenderMove, "selected defender can preview a move before forfeiting");
            Assert.That(high.GetInstructions(), Does.Contain("click another defender"));
        }
        LegacyKey("X");
        yield return Until(() => high.isWaitingForAccuracyRoll, "defender high-pass forfeit opens accuracy roll");
        Assert.That(high.isWaitingForDefenderSelection, Is.False);
    }

    private static void AssertLegacyColor(HexCell hex, Color expected)
        => Assert.That(Vector4.Distance(hex.hexRenderer.material.color, expected), Is.LessThan(.06f));
}
