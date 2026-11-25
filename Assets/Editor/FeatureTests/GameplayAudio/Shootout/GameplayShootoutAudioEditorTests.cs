using NUnit.Framework;

[Category("GameplayShootoutAudio")]
public sealed class GameplayShootoutAudioEditorTests
{
    [TestCase(true, true, GameplayAudioPool.Cheer, GameplayAudioPool.Groan)]
    [TestCase(false, true, GameplayAudioPool.Groan, GameplayAudioPool.Cheer)]
    [TestCase(true, false, GameplayAudioPool.Groan, GameplayAudioPool.Cheer)]
    [TestCase(false, false, GameplayAudioPool.Cheer, GameplayAudioPool.Groan)]
    public void ShootoutOutcomesUseStrongTeamSpecificCrowdReaction(
        bool shooterHome, bool scored, GameplayAudioPool expectedHome, GameplayAudioPool expectedAway)
    {
        var reaction = GameplayAudioConfig.PenaltyShootoutOutcome(shooterHome, scored);

        Assert.That(reaction.Home.Pool, Is.EqualTo(expectedHome));
        Assert.That(reaction.Away.Pool, Is.EqualTo(expectedAway));
        Assert.That(reaction.Home.Intensity, Is.EqualTo(GameplayAudioConfig.GoalIntensity));
        Assert.That(reaction.Away.Intensity, Is.EqualTo(GameplayAudioConfig.GoalIntensity));
        Assert.That(reaction.Priority, Is.EqualTo(GameplayAudioConfig.GoalPriority));
        Assert.That(reaction.FillRemaining, Is.True);
    }
}
