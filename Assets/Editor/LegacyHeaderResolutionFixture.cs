using System.Collections;
using System.Linq;
using NUnit.Framework;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunHeaderResolutionLegacy(string branch)
    {
        yield return PrepareLegacyInaccurateHeader();
        var players = match.playerTokenManager.allTokens.ToDictionary(t => t.playerName);
        var header = match.shotManager.headerManager;
        AttachAuthoredPanels(); EnableForAuthoredPresentation(header); EnableForAuthoredPresentation(loose);
        bool two = branch.EndsWith("/two");
        input.TryExecuteSyntheticHexClick(players["Nazef"].currentHex, "Legacy migration", out _);
        if (two) input.TryExecuteSyntheticHexClick(players["Kalla"].currentHex, "Legacy migration", out _);
        LegacyKey("Return");
        yield return Until(() => header.isWaitingForDefenderSelection, "header resolution attack confirms nominations");
        input.TryExecuteSyntheticHexClick(players["McNulty"].currentHex, "Legacy migration", out _);
        input.TryExecuteSyntheticHexClick(players["Stewart"].currentHex, "Legacy migration", out _);
        LegacyKey("Return");
        bool attackWin = branch == "attack_space";
        bool defenseWin = branch.StartsWith("defense_");
        var order = two ? new[] { players["Nazef"], players["Kalla"], players["McNulty"], players["Stewart"] }
            : new[] { players["Nazef"], players["McNulty"], players["Stewart"] };
        int[] rolls = attackWin ? new[] { 6, 2, 2 } : defenseWin ? new[] { 1, 6, 6 }
            : two ? new[] { 3, 2, 1, 4 } : new[] { 3, 1, 4 };
        for (int i = 0; i < order.Length; i++)
        {
            var expected = order[i];
            yield return Until(() => header.isWaitingForHeaderRoll && header.tokenRolling == expected,
                $"actual header roll {i + 1} belongs to {expected.playerName}");
            Assert.That(header.headerChallengePanel.IsOpen, Is.True);
            header.PerformHeaderRoll(new RollInputOverride { hasOverride = true, roll = rolls[i], isJackpot = false });
        }
        if (attackWin || defenseWin)
        {
            yield return DismissLegacyHeaderResult();
            yield return Until(() => header.isWaitingForHeaderTargetSelection, "won header exposes headed-pass target selection after its result gate");
            var winner = attackWin ? players["Nazef"] : players["McNulty"];
            Assert.That(header.challengeWinner, Is.SameAs(winner));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(winner));
            var target = attackWin ? Hex(2, 3) : branch == "defense_player" ? players["Vladoiu"].currentHex : Hex(11, 3);
            input.TryExecuteSyntheticHexClick(target, "Legacy migration", out _);
            if (attackWin)
            {
                foreach (var interceptor in new[] { players["Vladoiu"], players["Gilbert"] })
                {
                    yield return Until(() => header.isWaitingForInterceptionRoll, "headed pass offers its next interception");
                    Assert.That(header.interceptingDefender, Is.SameAs(interceptor));
                    header.PerformInterceptionRoll(2);
                    yield return DismissLegacyDuel(false);
                    yield return Until(() => !header.isWaitingForInterceptionRoll || header.interceptingDefender != interceptor,
                        "failed headed-pass interception advances");
                }
            }
            yield return Until(() => !header.isActivated && ball.GetCurrentHex() == target, "headed pass reaches its selected player or space and cleans up");
            yield return ForfeitLegacyFinalThirds();
            if (branch == "defense_player")
            {
                Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(players["Vladoiu"]));
                Assert.That(movement.isAvailable && ftp.isAvailable && longBall.isAvailable, Is.True);
                Assert.That(movement.isActivated || ftp.isActivated || ground.isAvailable || ground.isActivated
                    || high.isAvailable || high.isActivated || longBall.isActivated, Is.False);
            }
            else AssertLegacyHeaderSpaceContinuation();
            yield break;
        }
        yield return Until(() => !header.isActivated && loose.isActivated && loose.isWaitingForDirectionRoll,
            "tied header transfers directly into actual loose-ball direction selection");
        Assert.That(header.headerChallengePanel.IsOpen && header.headerChallengePanel.scatter.activeSelf, Is.True);
        Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(players["Cafferata"]));
        Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
        if (branch.StartsWith("tie/")) yield break;
        int direction = branch.StartsWith("scatter_jumper") || branch == "scatter_attacker" ? 5 : branch == "scatter_defender" ? 1 : 2;
        int distance = branch.StartsWith("scatter_jumper") ? 1 : branch is "scatter_attacker" or "scatter_defender" ? 2 : 4;
        loose.PerformDirectionRoll(direction);
        yield return Until(() => loose.isWaitingForDistanceRoll, "tied-header scatter consumes direction before distance");
        Assert.That(loose.isWaitingForDirectionRoll, Is.False);
        loose.PerformDistanceRoll(distance);
        yield return DismissLegacyHeaderResult();
        Assert.That(loose.isWaitingForDistanceRoll, Is.False);
        if (branch is "scatter_offer" or "scatter_recover" or "scatter_miss")
        {
            yield return Until(() => loose.isWaitingForInterceptionRoll, "Vladoiu can intercept the southwest loose header");
            Assert.That(loose.potentialInterceptor, Is.SameAs(players["Vladoiu"]));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(players["Cafferata"]));
            if (branch == "scatter_offer") yield break;
            Assert.That(header.headerChallengePanel.IsOpen && header.headerChallengePanel.recoveryContent.activeSelf, Is.True);
            loose.PerformInterceptionRoll(branch == "scatter_recover" ? 6 : 1);
            yield return DismissLegacyHeaderResult();
        }
        yield return Until(() => !loose.isActivated, "tied-header loose ball completes its actual recovery or movement continuation");
        yield return ForfeitLegacyFinalThirds();
        if (branch is "scatter_recover" or "scatter_defender")
        {
            var receiver = players[branch == "scatter_recover" ? "Vladoiu" : "Paterson"];
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Away));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(receiver));
            Assert.That(ball.GetCurrentHex(), Is.SameAs(receiver.currentHex));
            Assert.That(match.attackHasPossession, Is.True);
            Assert.That(ground.isAvailable && movement.isAvailable && longBall.isAvailable, Is.True);
            Assert.That(ground.imposedDistance, Is.EqualTo(6));
            Assert.That(ftp.isAvailable || high.isAvailable || ground.isActivated || movement.isActivated || longBall.isActivated, Is.False);
        }
        else if (branch == "scatter_attacker" || branch == "scatter_jumper/one")
        {
            Assert.That(ball.GetCurrentHex(), Is.SameAs(players["Kalla"].currentHex));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(players["Kalla"]));
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
        }
        else
        {
            Assert.That(match.teamInAttack, Is.EqualTo(MatchManager.TeamInAttack.Home));
            Assert.That(match.LastTokenToTouchTheBallOnPurpose, Is.SameAs(players["Cafferata"]));
            Assert.That(ball.GetCurrentHex(), Is.SameAs(branch == "scatter_miss" ? Hex(0, 3) : Hex(7, 6)));
            AssertLegacyHeaderSpaceContinuation();
        }
    }

    private IEnumerator DismissLegacyHeaderResult()
    {
        var panel = match.shotManager.headerManager.headerChallengePanel;
        yield return Until(() => (bool)typeof(HeaderChallengePanelController).GetField("complete", Private).GetValue(panel),
            "authored aerial result completes all required rolls before dismissal");
        Assert.That(panel.IsOpen, Is.True);
        LegacyKey("Escape");
        yield return Until(() => !panel.IsOpen || panel.recoveryContent.activeSelf, "completed aerial result releases its owning continuation");
    }

    private void AssertLegacyHeaderSpaceContinuation()
    {
        Assert.That(match.attackHasPossession, Is.False);
        Assert.That(movement.isActivated && movement.isCommitted && !movement.isAvailable, Is.True);
        Assert.That(ground.isAvailable || ftp.isAvailable || high.isAvailable || longBall.isAvailable
            || ground.isActivated || ftp.isActivated || high.isActivated || longBall.isActivated, Is.False);
    }
}
