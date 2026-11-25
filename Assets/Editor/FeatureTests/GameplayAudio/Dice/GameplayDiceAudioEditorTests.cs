using System.Linq;
using NUnit.Framework;
using UnityEngine;

[Category("GameplayDiceAudio")]
public sealed class GameplayDiceAudioEditorTests
{
    [Test]
    public void RollCueRandomizesAcrossAuthoredClipsWithoutConsumingGameplayRandom()
    {
        var clips = DiceAudioAuthoring.ClipPaths.Select((path, index) =>
            AudioClip.Create("owned dice roll " + index, 32, 1, 48000, false)).ToArray();
        var gameplayRandom = Random.state;
        try
        {
            var policy = new GameplayAudioPolicy(82741);
            var selected = Enumerable.Range(0, 30)
                .Select(_ => policy.Pick(GameplayAudioPool.Dice, clips)).ToArray();

            Assert.That(selected, Has.All.Not.Null);
            foreach (AudioClip clip in selected) Assert.That(clips, Does.Contain(clip));
            Assert.That(selected.Distinct().Count(), Is.GreaterThan(1));
            Assert.That(Random.state, Is.EqualTo(gameplayRandom));
        }
        finally
        {
            foreach (AudioClip clip in clips) Object.DestroyImmediate(clip);
            Random.state = gameplayRandom;
        }
    }
}
