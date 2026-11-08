using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MatchEventBannerEditorTests
{
    [Test]
    public void RealCardsDistinguishSecondYellowFromStraightRedAndAvoidDuplicateDismissal()
    {
        var events = new List<MatchBannerEvent>();
        Action<MatchBannerEvent, string> capture = (kind, _) => events.Add(kind);
        var go = new GameObject("BannerCardTest");
        var straight = new GameObject("BannerStraightRedTest");
        MatchEventAnnouncements.Published += capture;
        try
        {
            var token = go.AddComponent<PlayerToken>();
            token.ReceiveYellowCard();
            token.ReceiveYellowCard();
            token.MarkSentOff();
            straight.AddComponent<PlayerToken>().MarkSentOff();
            Assert.That(events, Is.EqualTo(new[] { MatchBannerEvent.YellowCard, MatchBannerEvent.SecondYellow, MatchBannerEvent.RedCard }));
            Assert.That(token.isSentOff, Is.True);
            Assert.That(go.activeSelf, Is.False);
        }
        finally
        {
            MatchEventAnnouncements.Published -= capture;
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(straight);
        }
    }

    [Test]
    public void RealFirstInjuryAnnouncesWithoutChangingItsAttributePenalty()
    {
        var events = new List<MatchBannerEvent>();
        Action<MatchBannerEvent, string> capture = (kind, _) => events.Add(kind);
        var go = new GameObject("BannerInjuryTest");
        MatchEventAnnouncements.Published += capture;
        try
        {
            var token = go.AddComponent<PlayerToken>();
            token.pace = 4;
            token.ReceiveInjury();
            Assert.That(events, Is.EqualTo(new[] { MatchBannerEvent.Injury }));
            Assert.That(token.isInjured, Is.True);
            Assert.That(token.pace, Is.EqualTo(3));
        }
        finally
        {
            MatchEventAnnouncements.Published -= capture;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SecondInjuryAnnouncesAndStillRequiresSubstitution()
    {
        var previous = MatchManager.Instance;
        var events = new List<MatchBannerEvent>();
        Action<MatchBannerEvent, string> capture = (kind, _) => events.Add(kind);
        var go = new GameObject("BannerSecondInjuryTest");
        MatchEventAnnouncements.Published += capture;
        try
        {
            MatchManager.Instance = null;
            var token = go.AddComponent<PlayerToken>();
            token.pace = 4;
            token.ReceiveInjury();
            token.ReceiveInjury();
            Assert.That(events, Is.EqualTo(new[] { MatchBannerEvent.Injury, MatchBannerEvent.SecondInjury }));
            Assert.That(token.requiresSubstitution, Is.True);
            Assert.That(token.pace, Is.EqualTo(3));
        }
        finally
        {
            MatchManager.Instance = previous;
            MatchEventAnnouncements.Published -= capture;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PanelQueuesEventsForFourSecondsAndClearsExplicitly()
    {
        var go = new GameObject("BannerQueueTest", typeof(RectTransform));
        go.SetActive(false);
        try
        {
            var panel = go.AddComponent<MatchEventBannerController>();
            panel.visibility = go.AddComponent<CanvasGroup>();
            panel.content = (RectTransform)go.transform;
            panel.icon = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            panel.icon.transform.SetParent(go.transform, false);
            panel.caption = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            panel.caption.transform.SetParent(go.transform, false);
            go.SetActive(true);
            panel.Enqueue(MatchBannerEvent.Foul, "Player");
            panel.Enqueue(MatchBannerEvent.RedCard, "Player");
            panel.Advance(3.9f);
            Assert.That(panel.CurrentEvent, Is.EqualTo(MatchBannerEvent.Foul));
            Assert.That(panel.PendingCount, Is.EqualTo(1));
            panel.Advance(0.11f);
            Assert.That(panel.CurrentEvent, Is.EqualTo(MatchBannerEvent.RedCard));
            Assert.That(panel.caption.text, Is.EqualTo("RED CARD"));
            panel.Advance(4f);
            Assert.That(panel.IsShowing, Is.False);
            Assert.That(panel.visibility.alpha, Is.Zero);
            panel.Enqueue(MatchBannerEvent.Injury, "");
            panel.Clear();
            Assert.That(panel.PendingCount, Is.Zero);
            Assert.That(panel.IsShowing, Is.False);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void RoomHasSceneOwnedSquareWithEveryOccasionAndNoInputBlocking()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var panel = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<MatchEventBannerController>(true)).Single();
            var rect = (RectTransform)panel.transform;
            Assert.That(rect.sizeDelta.x, Is.EqualTo(rect.sizeDelta.y));
            Assert.That(panel.transform.parent.GetComponent<ScoreboardManager>(), Is.Not.Null);
            Assert.That(panel.artwork.Select(x => x.occasion), Is.EquivalentTo(Enum.GetValues(typeof(MatchBannerEvent))));
            Assert.That(panel.artwork.All(x => x.sprite != null), Is.True);
            Assert.That(panel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).All(x => !x.raycastTarget), Is.True);
            Assert.That(panel.visibility.alpha, Is.Zero);
            Assert.That(panel.displaySeconds, Is.EqualTo(4f));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
