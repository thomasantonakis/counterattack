using System.Collections;
using NUnit.Framework;
using TMPro;
public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunShootoutInstructionVisibility(bool shootoutMode, bool result)
    {
        yield return RunShootoutRollPanel(1, "keeper");
        var shootout = PenaltyShootoutManager.ActiveShootout;
        shootout.isActive = shootoutMode;
        shootout.homeShootoutScore = 0; shootout.awayShootoutScore = 3;
        shootout.homeAttempts = 2; shootout.awayAttempts = 3;
        match.currentState = shootoutMode ? MatchManager.GameState.PenaltyShootoutKickExecution : MatchManager.GameState.AnyOtherScenario;
        if (result)
        {
            var dismissal = duelPanel.ResolveSingle(match.shotManager,
                new GameplayDiceRollResult { roll = 2 }, "SAVE FAILED · Shot continues",
                resolvedTotal: 5, continueWhileResultVisible: shootoutMode);
            dismissal.MoveNext();
            match.shotManager.isWaitingForGKDiceRoll = false;
            if (shootoutMode)
            {
                shootout.currentShooter = null;
                Set(shootout, "goalFlashInstructionActive", true);
                Set(shootout, "goalFlashInstructionHome", true);
            }
            Assert.That(duelPanel.IsComplete, Is.True);
        }
        var previousMonitor = GameDebugMonitor.Instance;
        var restore = restoreAuthoredPanelState;
        restoreAuthoredPanelState = () => { restore?.Invoke(); GameDebugMonitor.Instance = previousMonitor; };
        var monitor = Add<GameDebugMonitor>(); monitor.matchManager = match;
        monitor.shotManager = match.shotManager; monitor.penaltyShootoutManager = shootout;
        // Reuse an authored text element; exercise the real display setter, not just its snapshot.
        monitor.instructionText = duelPanel.footer as TextMeshProUGUI;
        Assert.That(monitor.instructionText, Is.Not.Null);
        var snapshot = monitor.GetCurrentInstructionSnapshotForLog();
        Assert.That(snapshot?.instructionText, Is.Not.Null.And.Not.Empty);
        Invoke(monitor, "UpdateInstructionText");
        Assert.That(duelPanel.IsOpen, Is.True);
        Assert.That(monitor.instructionText.text, shootoutMode
            ? Is.EqualTo(snapshot.instructionText) : Is.Empty);
        if (shootoutMode)
            Assert.That(monitor.instructionText.text, Does.Contain(result ? "GOAL FOR Fixture Home" : "lose if"));
    }
}
