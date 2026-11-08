using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class IntroClockVisibilityEditorTests
{
    private GameObject root;
    private ScoreboardManager scoreboard;
    private RoomMatchIntro intro;
    private RoomMatchIntro previousIntro;
    private GameObject timer;
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static PropertyInfo InstanceProperty => typeof(RoomMatchIntro).GetProperty(nameof(RoomMatchIntro.Instance));

    [SetUp]
    public void SetUp()
    {
        previousIntro = RoomMatchIntro.Instance;
        root = new GameObject("Isolated intro clock presentation test");
        root.SetActive(false);
        scoreboard = root.AddComponent<ScoreboardManager>();
        intro = root.AddComponent<RoomMatchIntro>();
        InstanceProperty.SetValue(null, intro);
        typeof(RoomMatchIntro).GetProperty(nameof(RoomMatchIntro.IsHoldingMatch)).SetValue(intro, true);
        timer = new GameObject("Timer", typeof(RectTransform));
        timer.transform.SetParent(root.transform, false);
        var text = new GameObject("TimeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(timer.transform, false);
        scoreboard.timeText = text.GetComponent<TextMeshProUGUI>();
        scoreboard.timeText.text = "12:34";
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        InstanceProperty.SetValue(null, previousIntro);
    }

    [Test]
    public void ScoreboardRefreshCannotRevealClockDuringIntro()
    {
        typeof(ScoreboardManager).GetMethod("RefreshPenaltyDots", PrivateInstance).Invoke(scoreboard, new object[] { false });
        Assert.That(timer.activeSelf, Is.False);
        timer.SetActive(true);
        Frame();
        Assert.That(timer.activeSelf, Is.False);
        Assert.That(scoreboard.timeText.text, Is.EqualTo("12:34"));
    }

    [TestCase("Finish")]
    [TestCase("Skip")]
    [TestCase("OnDisable")]
    public void ClockReturnsOnFirstFrameAfterEveryIntroExit(string exit)
    {
        Frame();
        Assert.That(timer.activeSelf, Is.False);
        typeof(RoomMatchIntro).GetMethod(exit, BindingFlags.Public | PrivateInstance).Invoke(intro, null);
        Frame();
        Assert.That(intro.IsHoldingMatch, Is.False);
        Assert.That(timer.activeSelf, Is.True);
        Assert.That(scoreboard.timeText.text, Is.EqualTo("12:34"));
    }

    [Test]
    public void IntroExitDoesNotRevealClockDuringPenaltyShootout()
    {
        typeof(ScoreboardManager).GetMethod("RefreshPenaltyDots", PrivateInstance).Invoke(scoreboard, new object[] { true });
        intro.Skip();
        Frame();
        Assert.That(timer.activeSelf, Is.False);
        typeof(ScoreboardManager).GetMethod("RefreshPenaltyDots", PrivateInstance).Invoke(scoreboard, new object[] { false });
        Assert.That(timer.activeSelf, Is.True);
    }

    private void Frame() => typeof(ScoreboardManager).GetMethod("LateUpdate", PrivateInstance).Invoke(scoreboard, null);
}
