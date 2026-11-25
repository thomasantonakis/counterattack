using System;
using NUnit.Framework;
using UnityEngine;

[Category("GameplayAudio")]
public sealed class GameplayAudioEditorTests
{
    private string prefix;
    private GameplayAudioPreferences preferences;
    private AudioClip first;
    private AudioClip second;
    private UnityEngine.Random.State randomState;

    [SetUp]
    public void SetUp()
    {
        prefix = "CounterAttack.Audio.Test." + Guid.NewGuid() + ".";
        preferences = new GameplayAudioPreferences(prefix);
        randomState = UnityEngine.Random.state;
        first = AudioClip.Create("First isolated clip", 480, 1, 48000, false);
        second = AudioClip.Create("Second isolated clip", 480, 1, 48000, false);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameplayAudioCategory category in Enum.GetValues(typeof(GameplayAudioCategory)))
        {
            PlayerPrefs.DeleteKey(prefix + category + ".Volume");
            PlayerPrefs.DeleteKey(prefix + category + ".Enabled");
        }
        PlayerPrefs.Save();
        UnityEngine.Random.state = randomState;
        UnityEngine.Object.DestroyImmediate(first);
        UnityEngine.Object.DestroyImmediate(second);
    }

    [TestCase(true, 0.7f, 0.3f)]
    [TestCase(false, 0.7f, 0.3f)]
    public void GoalsUseScoringStandAndOpposingGroanWithTheirOwnShares(bool homeScored, float homeGain, float awayGain)
    {
        var reaction = GameplayAudioConfig.Goal(homeScored);
        Assert.That(reaction.Home.Pool, Is.EqualTo(homeScored ? GameplayAudioPool.Cheer : GameplayAudioPool.Groan));
        Assert.That(reaction.Away.Pool, Is.EqualTo(homeScored ? GameplayAudioPool.Groan : GameplayAudioPool.Cheer));
        Assert.That(GameplayAudioConfig.StandGain(true, reaction.Home.Intensity, 1), Is.EqualTo(homeGain).Within(0.0001f));
        Assert.That(GameplayAudioConfig.StandGain(false, reaction.Away.Intensity, 1), Is.EqualTo(awayGain).Within(0.0001f));
    }

    [TestCase(true, 0.315f)]
    [TestCase(false, 0.135f)]
    public void ChatterMidpointKeepsStandShares(bool home, float gain) =>
        Assert.That(GameplayAudioConfig.StandGain(home, GameplayAudioConfig.ChatterIntensity, 1), Is.EqualTo(gain).Within(0.0001f));

    [TestCase(GameplayAudioCategory.Stadium)]
    [TestCase(GameplayAudioCategory.Game)]
    [TestCase(GameplayAudioCategory.Music)]
    public void MutingPersistsWithoutAffectingOtherCategoriesOrCodeVolume(GameplayAudioCategory category)
    {
        // Old slider values must not silently mute a checkbox-only interface.
        PlayerPrefs.SetFloat(prefix + category + ".Volume", 0f);
        preferences.SetEnabled(category, false);
        var reloaded = new GameplayAudioPreferences(prefix);
        Assert.That(reloaded.Gain(category), Is.Zero);
        foreach (GameplayAudioCategory other in Enum.GetValues(typeof(GameplayAudioCategory)))
            if (other != category) Assert.That(reloaded.Gain(other), Is.EqualTo(GameplayAudioConfig.CategoryVolume(other)));
        reloaded.SetEnabled(category, true);
        Assert.That(reloaded.Gain(category), Is.EqualTo(GameplayAudioConfig.CategoryVolume(category)));
    }

    [TestCase(-1f, 0f)]
    [TestCase(2f, 1f)]
    public void StandIntensityIsClamped(float supplied, float expected)
    {
        Assert.That(GameplayAudioConfig.StandGain(true, supplied, 1f), Is.EqualTo(0.7f * expected).Within(0.0001f));
    }

    [TestCase(GameplayAudioCategory.Stadium)]
    [TestCase(GameplayAudioCategory.Game)]
    [TestCase(GameplayAudioCategory.Music)]
    public void CheckboxBindingPersistsAndRefreshDoesNotEmitChanges(GameplayAudioCategory category)
    {
        var root = new GameObject("Isolated audio checkbox", typeof(RectTransform));
        root.SetActive(false);
        var toggle = root.AddComponent<UnityEngine.UI.Toggle>();
        var view = root.AddComponent<GameplayAudioSettingsView>();
        view.rows = new[] { new GameplayAudioSettingsView.Row { category = category, enabled = toggle } };
        string key = "CounterAttack.Audio." + category + ".Enabled";
        bool hadKey = PlayerPrefs.HasKey(key);
        bool original = GameplayAudioPreferences.Local.Enabled(category);
        int changeCount = 0;
        Action changed = () => changeCount++;
        var enable = typeof(GameplayAudioSettingsView).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var disable = typeof(GameplayAudioSettingsView).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        GameplayAudioPreferences.Local.Changed += changed;
        try
        {
            enable.Invoke(view, null);
            Assert.That(toggle.isOn, Is.EqualTo(original));
            Assert.That(changeCount, Is.Zero);
            toggle.isOn = !original;
            Assert.That(new GameplayAudioPreferences().Enabled(category), Is.EqualTo(!original));
            Assert.That(changeCount, Is.EqualTo(1));
            view.Refresh();
            Assert.That(changeCount, Is.EqualTo(1));
            disable.Invoke(view, null);
            toggle.isOn = original;
            Assert.That(changeCount, Is.EqualTo(1), "Disabled views must unsubscribe.");
        }
        finally
        {
            disable.Invoke(view, null);
            GameplayAudioPreferences.Local.Changed -= changed;
            GameplayAudioPreferences.Local.SetEnabled(category, original);
            if (!hadKey) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void DuplicateIncidentAndPausedIncidentCannotReplayAfterResume()
    {
        var policy = new GameplayAudioPolicy(12);
        Assert.That(policy.AcceptEvent("run:1"), Is.True);
        Assert.That(policy.AcceptEvent("run:1"), Is.False);
        policy.SetPaused(true);
        Assert.That(policy.AcceptEvent("run:2"), Is.False);
        policy.SetPaused(false);
        Assert.That(policy.AcceptEvent("run:2"), Is.False);
        Assert.That(policy.AcceptEvent("run:3"), Is.True);
        Assert.That(policy.AcceptEvent(""), Is.False);
    }

    [Test]
    public void LongMatchesBoundTheDuplicateHistory()
    {
        var policy = new GameplayAudioPolicy(12);
        for (int i = 0; i <= GameplayAudioConfig.RememberedEventCount; i++) Assert.That(policy.AcceptEvent("run:" + i), Is.True);
        Assert.That(policy.AcceptEvent("run:0"), Is.True);
        Assert.That(policy.AcceptEvent("run:512"), Is.False);
    }

    [Test]
    public void VariationSkipsNullsAndAvoidsRepeatedClipEvenWithDuplicateAssignments()
    {
        var policy = new GameplayAudioPolicy(12);
        var clips = new[] { first, null, second, first };
        AudioClip previous = null;
        for (int i = 0; i < 25; i++)
        {
            AudioClip next = policy.Pick(GameplayAudioPool.Cheer, clips);
            Assert.That(next, Is.Not.Null);
            Assert.That(next, Is.Not.EqualTo(previous));
            previous = next;
        }
        Assert.That(policy.Pick(GameplayAudioPool.Cheer, new[] { first, first }), Is.EqualTo(first));
        Assert.That(policy.Pick(GameplayAudioPool.Cheer, new AudioClip[] { null }), Is.Null);
        Assert.That(policy.Pick(GameplayAudioPool.Cheer, null), Is.Null);
    }

    [Test]
    public void AudioSelectionAndChantsNeverConsumeGameplayRandomness()
    {
        UnityEngine.Random.InitState(7341);
        float expected = UnityEngine.Random.value;
        UnityEngine.Random.InitState(7341);
        var policy = new GameplayAudioPolicy(12);
        for (int i = 0; i < 20; i++)
        {
            policy.Pick(GameplayAudioPool.Cheer, new[] { first, second });
            Assert.That(policy.NextWallaIntensity(), Is.InRange(0.4f, 0.5f));
            Assert.That(policy.NextChantDelay(), Is.InRange(GameplayAudioConfig.ChantMinIntervalSeconds, GameplayAudioConfig.ChantMaxIntervalSeconds));
        }
        Assert.That(UnityEngine.Random.value, Is.EqualTo(expected));
    }

    [Test]
    public void WallaSelectionAvoidsOwnClipAndRelaxesOtherStandAvoidanceForSmallPools()
    {
        var policy = new GameplayAudioPolicy(44);
        Assert.That(policy.Pick(GameplayAudioPool.Chatter, new[] { first, second }, first, second), Is.EqualTo(second));
        Assert.That(policy.Pick(GameplayAudioPool.Chatter, new[] { first }, first, first), Is.EqualTo(first));
        Assert.That(policy.Pick(GameplayAudioPool.Chatter, new AudioClip[] { null }, first), Is.Null);
    }

    [Test]
    public void HoverCooldownDoesNotSuppressIndependentDiceCue()
    {
        var policy = new GameplayAudioPolicy(12);
        Assert.That(policy.AcceptCue(GameplayAudioPool.MenuHover, 1), Is.True);
        Assert.That(policy.AcceptCue(GameplayAudioPool.MenuHover, 1.01), Is.False);
        Assert.That(policy.AcceptCue(GameplayAudioPool.Dice, 1.01), Is.True);
        Assert.That(policy.AcceptCue(GameplayAudioPool.MenuHover, 1.2), Is.True);
        Assert.That(policy.AcceptCue(GameplayAudioPool.MenuHover, double.NaN), Is.False);
    }

    [TestCase(0, true, 10, false)]
    [TestCase(10, true, 10, false)]
    [TestCase(100, true, 10, true)]
    [TestCase(0, false, 100, true)]
    public void PriorityPreventsChantsAndRoutineIncidentsFromReplacingMajorReactions(int incoming, bool active, int current, bool allowed) =>
        Assert.That(GameplayAudioPolicy.CanReplace(incoming, active, current), Is.EqualTo(allowed));

    [Test]
    public void DirectorWithMissingAssetsIsSilentAndPauseDoesNotCreateObjects()
    {
        var root = new GameObject("Isolated audio director");
        try
        {
            var director = root.AddComponent<GameplayAudioDirector>();
            Assert.That(director.PlayGoal("run:1", true), Is.False);
            Assert.That(director.PlayWhistle("run:1"), Is.False);
            director.SetPaused(true);
            Assert.That(director.PlayGameCue(GameplayAudioPool.Dice), Is.False);
            Assert.That(director.PlayGoal("run:2", false), Is.False);
            director.SetPaused(false);
            Assert.That(director.PlayGoal("run:2", false), Is.False);
            Assert.That(root.transform.childCount, Is.Zero);
            Assert.That(root.GetComponents<AudioSource>(), Is.Empty);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
