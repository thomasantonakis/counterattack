using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class OnlineConnectionPanelEditorTests
{
    [TestCase(180, OnlineConnectionQuality.Good)]
    [TestCase(181, OnlineConnectionQuality.Average)]
    [TestCase(350, OnlineConnectionQuality.Average)]
    [TestCase(351, OnlineConnectionQuality.Poor)]
    public void RatesSteadyLatencyForTurnBasedPlay(int milliseconds, OnlineConnectionQuality expected)
    {
        double now = 0;
        var latency = new OnlineRoundTripStatistics(() => now);
        Assert.That(latency.CaptureConnection().Quality, Is.EqualTo(OnlineConnectionQuality.Measuring));
        latency.Sent(1);
        now = milliseconds / 1000d;
        Assert.That(latency.Replied(1), Is.True);

        var reading = latency.CaptureConnection();
        Assert.That(reading.Quality, Is.EqualTo(expected));
        Assert.That(reading.PingMilliseconds, Is.EqualTo(milliseconds).Within(0.001));
    }

    [Test]
    public void RepeatedSpikesLowerTheRatingWithoutMovingTheMedian()
    {
        double now = 0;
        var latency = new OnlineRoundTripStatistics(() => now);
        var delays = new[] { .05, .22, .06, .23, .07 };
        for (int index = 0; index < delays.Length; index++)
        {
            now = index;
            latency.Sent(index + 1);
            now += delays[index];
            Assert.That(latency.Replied(index + 1), Is.True);
        }

        var reading = latency.CaptureConnection();
        Assert.That(reading.PingMilliseconds, Is.EqualTo(70).Within(0.001));
        Assert.That(reading.Quality, Is.EqualTo(OnlineConnectionQuality.Average));

        now = 8.1;
        latency.Sent(6);
        now += .08;
        Assert.That(latency.Replied(6), Is.True);
        Assert.That(latency.CaptureConnection().Quality, Is.EqualTo(OnlineConnectionQuality.Good));
    }

    [Test]
    public void SilenceDowngradesTheLinkAndEventuallyClearsPing()
    {
        double now = 0;
        var latency = new OnlineRoundTripStatistics(() => now);
        latency.Sent(1);
        now = .08;
        Assert.That(latency.Replied(1), Is.True);

        now = 2.4;
        Assert.That(latency.CaptureConnection().Quality, Is.EqualTo(OnlineConnectionQuality.Good));
        now = 2.6;
        Assert.That(latency.CaptureConnection().Quality, Is.EqualTo(OnlineConnectionQuality.Average));
        now = 3.6;
        Assert.That(latency.CaptureConnection().Quality, Is.EqualTo(OnlineConnectionQuality.Poor));
        now = 5.1;
        Assert.That(latency.CaptureConnection().PingMilliseconds, Is.Null);

        latency.Sent(2);
        now += .15;
        Assert.That(latency.Replied(2), Is.True);
        Assert.That(latency.CaptureConnection().Quality, Is.EqualTo(OnlineConnectionQuality.Good));
    }

    [Test]
    public void FirstProbeCanTimeOutBeforeAnyPingHasBeenMeasured()
    {
        double now = 0;
        var latency = new OnlineRoundTripStatistics(() => now);
        latency.Sent(1);
        now = 4;
        Assert.That(latency.CaptureConnection().Quality, Is.EqualTo(OnlineConnectionQuality.Poor));
        Assert.That(latency.CaptureConnection().PingMilliseconds, Is.Null);
    }

    [Test]
    public void StartingANewHandshakeClearsSamplesFromTheOldConnection()
    {
        double now = 0;
        var compatibility = new OnlineCompatibility("protocol", "rules", "catalog");
        var protocol = new OnlineMatchProtocol("match", "host", "guest", "host", compatibility);
        protocol.BindAuthenticatedAccount("host", "host-connection", compatibility);
        var host = new OnlineSetupHostChannel(protocol, "host-connection", "guest",
            OnlineSetupAgreementEditorTests.Valid(), () => now);
        var guest = new OnlineSetupGuestChannel("match", "host", compatibility, OnlineTeam.Home, () => now);
        host.Outgoing += packet => Assert.That(guest.Receive("host", packet, packet.Length), Is.True);
        host.Begin();
        guest.Latency.Sent(1);
        now = .15;
        Assert.That(guest.Latency.Replied(1), Is.True);
        guest.Latency.Sent(2);

        host.Begin();

        Assert.That(guest.Latency.CaptureConnection().Quality, Is.EqualTo(OnlineConnectionQuality.Measuring));
        Assert.That(guest.Latency.CaptureConnection().PingMilliseconds, Is.Null);
        Assert.That(guest.Latency.Replied(2), Is.False);
        Assert.That(guest.Latency.Capture().samples, Is.EqualTo(1));
    }

    [TestCase("CreateNewGameScene")]
    [TestCase("Draft")]
    [TestCase("Room")]
    public void SceneHasAReadableNonInteractivePanelInItsAuthoredPosition(string sceneName)
    {
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene($"Assets/Scenes/{sceneName}.unity", OpenSceneMode.Single);
            var panel = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OnlineConnectionPanel>(true)).Single();
            var rect = (RectTransform)panel.transform;
            var canvas = (RectTransform)rect.parent;
            var serialized = new SerializedObject(panel);
            var group = (CanvasGroup)serialized.FindProperty("panel").objectReferenceValue;
            var label = (TMP_Text)serialized.FindProperty("reading").objectReferenceValue;
            var background = panel.GetComponent<Image>();
            var card = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Cards/PlayerCard.prefab");

            Assert.That(canvas.name, Is.EqualTo("MainCanvas"));
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(panel.gameObject),
                Is.EqualTo("Assets/Settings/OnlineSession/ConnectionPanel.prefab"));
            Assert.That(serialized.FindProperty("showBeforeSession").boolValue, Is.EqualTo(sceneName == "CreateNewGameScene"));
            Assert.That(background.color, Is.EqualTo(card.GetComponent<Image>().color));
            Assert.That(label.color, Is.EqualTo(Color.white));
            Assert.That(group.blocksRaycasts || group.interactable || background.raycastTarget || label.raycastTarget, Is.False);

            Canvas.ForceUpdateCanvases();
            Assert.That(rect.rect.width, Is.GreaterThan(0));
            Assert.That(rect.rect.height, Is.GreaterThan(0));
            if (sceneName == "Room")
            {
                var instructions = (RectTransform)canvas.Find("InstructionsPanel");
                var preview = (RectTransform)canvas.Find("MatchStatsUI/HoverCardsRoot/HomeHoverCardAnchor");
                var bounds = InCanvas(rect, canvas);
                var instructionsBounds = InCanvas(instructions, canvas);
                Assert.That(bounds.min.x, Is.GreaterThan(instructionsBounds.max.x));
                Assert.That(bounds.max.x, Is.LessThan(InCanvas(preview, canvas).min.x));
                Assert.That(bounds.min.y, Is.EqualTo(instructionsBounds.min.y).Within(1));
                Assert.That(bounds.max.y, Is.EqualTo(instructionsBounds.max.y).Within(1));
            }
            else
            {
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(.5f, 0)));
                Assert.That(rect.anchorMax, Is.EqualTo(rect.anchorMin));
                Assert.That(rect.anchoredPosition.x, Is.Zero);
                Assert.That(rect.anchoredPosition.y, Is.LessThan(32));
            }

            foreach (var status in new[] { "Average", "Not connected", "Reconnecting" })
            {
                label.text = $"Connection: {status}\nPing: 350 ms";
                label.ForceMeshUpdate();
                Assert.That(label.textInfo.lineCount, Is.EqualTo(2), status);
                Assert.That(label.textBounds.size.x, Is.LessThanOrEqualTo(label.rectTransform.rect.width), status);
                Assert.That(label.textBounds.size.y, Is.LessThanOrEqualTo(label.rectTransform.rect.height), status);
            }
        }
        finally
        {
            if (previous.Any(scene => scene.isLoaded && scene.isActive && !string.IsNullOrEmpty(scene.path)))
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    private static Bounds InCanvas(RectTransform rect, RectTransform canvas)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        var bounds = new Bounds(canvas.InverseTransformPoint(corners[0]), Vector3.zero);
        bounds.Encapsulate(canvas.InverseTransformPoint(corners[2]));
        return bounds;
    }
}
