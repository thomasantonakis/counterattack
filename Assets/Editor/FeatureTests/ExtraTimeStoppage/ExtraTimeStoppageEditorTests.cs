using NUnit.Framework;

public sealed class ExtraTimeStoppageEditorTests
{
    [Test]
    public void ActualRollUsesPeriodCap([Values(1,2,3,4)] int half, [Values(1,6)] int roll, [Values(1,3)] int leniency)
    {
        using var board = new GoalkeeperBoxEntryFixture();
        var match = board.match;
        match.gameData.gameSettings.halfDuration = 45;
        match.gameData.gameSettings.numberOfHalfs = 2;
        match.currentHalf = half;
        match.refereeLeniency = leniency;
        var forced = new RollInputOverride { hasOverride = true, roll = roll };
        GoalkeeperBoxEntryFixture.Invoke(match, "DetermineExtraActions", (RollInputOverride?)forced);
        Assert.That(match.extraActionsTotal, Is.EqualTo(System.Math.Min(leniency + roll, half > 2 ? 3 : 7)));
        Assert.That(match.extraActionsRemaining, Is.EqualTo(match.extraActionsTotal));
        Assert.That(match.extraActionsDetermined, Is.True);
    }
}
