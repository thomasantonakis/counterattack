using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

[Category("MenuAudio")]
public sealed class MenuAudioEditorTests
{
    [Test] public void HoverIsGloballyThrottledAndNavigationSharesACooldown()
    {
        var policy = new MenuAudioPolicy();
        Assert.That(policy.Accept(GameplayAudioPool.MenuHover, 1, false, default), Is.True);
        Assert.That(policy.Accept(GameplayAudioPool.MenuHover, 1.1, false, default), Is.False);
        Assert.That(policy.Accept(GameplayAudioPool.MenuHover, 1.2, false, default), Is.True);
        Assert.That(policy.Accept(GameplayAudioPool.MenuAdvance, 2, false, default), Is.True);
        Assert.That(policy.Accept(GameplayAudioPool.MenuBack, 2.05, false, default), Is.False);
        Assert.That(policy.Accept(GameplayAudioPool.MenuBack, 2.12, false, default), Is.True);
    }

    [TestCase(GameplayAudioPool.MenuHover, GameplayAudioPool.MenuAdvance, false)]
    [TestCase(GameplayAudioPool.MenuSelect, GameplayAudioPool.MenuBack, false)]
    [TestCase(GameplayAudioPool.MenuHover, GameplayAudioPool.MenuSelect, false)]
    [TestCase(GameplayAudioPool.MenuAdvance, GameplayAudioPool.MenuHover, true)]
    public void ImportantCuesOwnTheVoice(GameplayAudioPool next, GameplayAudioPool current, bool allowed) =>
        Assert.That(new MenuAudioPolicy().Accept(next, 1, true, current), Is.EqualTo(allowed));

    [Test] public void UnsupportedAndInvalidTimesAreSilent()
    {
        var policy = new MenuAudioPolicy();
        Assert.That(policy.Accept(GameplayAudioPool.Dice, 1, false, default), Is.False);
        Assert.That(policy.Accept(GameplayAudioPool.MenuSelect, double.NaN, false, default), Is.False);
        Assert.That(policy.Accept(GameplayAudioPool.MenuHover, double.PositiveInfinity, false, default), Is.False);
    }

    [TestCase(GameplayAudioPool.MenuHover, .065f, .2f)]
    [TestCase(GameplayAudioPool.MenuSelect, .085f, .24f)]
    [TestCase(GameplayAudioPool.MenuAdvance, .18f, .3f)]
    [TestCase(GameplayAudioPool.MenuBack, .16f, .27f)]
    public void GeneratedClipsAreShortMonoAndHaveQuietBoundaries(GameplayAudioPool pool, float seconds, float peak)
    {
        var library = AssetDatabase.LoadAssetAtPath<GameplayAudioLibrary>(GameplayAudioEditorTools.LibraryPath);
        var clips = library.Get(pool);
        Assert.That(clips.Length, Is.EqualTo(1));
        var clip = clips[0];
        Assert.That(clip.channels, Is.EqualTo(1)); Assert.That(clip.frequency, Is.EqualTo(48000));
        Assert.That(clip.length, Is.EqualTo(seconds).Within(.001f));
        var samples = new float[clip.samples];
        Assert.That(clip.GetData(samples, 0), Is.True);
        Assert.That(samples.Max(s => Mathf.Abs(s)), Is.EqualTo(peak).Within(.001f));
        Assert.That(Mathf.Abs(samples[0]) + Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(.0001f));
        Assert.That(Mathf.Abs(samples.Average()), Is.LessThan(.005f));
    }

    [Test] public void AuthoredPlayerUsesLocalLibraryAndPauseSafeSource()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuAudioAuthoring.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        var player = prefab.GetComponent<MenuAudioPlayer>();
        Assert.That(player.voice, Is.Not.Null); Assert.That(player.library, Is.Not.Null);
        Assert.That(player.voice.playOnAwake || player.voice.loop, Is.False);
        Assert.That(player.voice.spatialBlend, Is.Zero);
        Assert.That(prefab.GetComponentsInChildren<AudioSource>(true).Length, Is.EqualTo(1));
    }
}
