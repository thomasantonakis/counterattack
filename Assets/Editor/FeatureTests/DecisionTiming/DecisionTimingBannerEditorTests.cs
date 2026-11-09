using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEditor;

[Category("DecisionTiming")]
public sealed class DecisionTimingBannerEditorTests
{
    [TestCase(DecisionKind.SnapshotOrForfeitPace)]
    [TestCase(DecisionKind.StandaloneNutmegDecision)]
    [TestCase(DecisionKind.StandaloneTackleOrStand)]
    public void ChoiceCountdownTakesPrecedenceAtConfiguredThreshold(DecisionKind kind)
    {
        var root = new GameObject("Concurrent timing display", typeof(RectTransform)); root.SetActive(false);
        try
        {
            var banner = root.AddComponent<MatchEventBannerController>();
            banner.timingVisibility = root.AddComponent<CanvasGroup>();
            banner.timingCaption = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            banner.timingCaption.transform.SetParent(root.transform);
            banner.timingDetail = new GameObject("Detail", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            banner.timingDetail.transform.SetParent(root.transform);
            banner.timingIcon = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            banner.timingIcon.transform.SetParent(root.transform);
            var t = new DecisionTimingService();
            t.BeginDecision("phase", DecisionKind.AttackingMovement, "Home", 3); t.Select("phase"); t.Advance(31);
            t.BeginRegistration(); t.Register("phase", DecisionKind.AttackingMovement, "Home", 3, true, true);
            t.Register("choice", kind, "Home", 3, true); t.EndRegistration();
            banner.SetTimingStatus(t);
            Assert.That(banner.IsTimingVisible, Is.True);
            bool immediateChoice = DecisionTimingConfig.CountdownDisplaySpentFraction == 0;
            Assert.That(banner.timingDetail.text.Contains("Attacking Movement"), Is.EqualTo(!immediateChoice));
            t.Advance(2.49f); banner.SetTimingStatus(t);
            Assert.That(banner.timingDetail.text.Contains("Attacking Movement"), Is.EqualTo(!immediateChoice));
            t.Advance(.01f); banner.SetTimingStatus(t);
            Assert.That(banner.timingDetail.text, Does.Not.Contain("Attacking Movement"));
            Assert.That(banner.timingCaption.text, Is.EqualTo("3\" remaining"));
            t.BeginRegistration(); t.Register("phase", DecisionKind.AttackingMovement, "Home", 3, true); t.EndRegistration();
            banner.SetTimingStatus(t);
            Assert.That(banner.timingDetail.text, Does.Contain("Attacking Movement"));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test] public void AnnouncementsAndTimingStayVisibleInSeparateDisplays([Values] MatchBannerEvent occasion)
    {
        var root = new GameObject("Timing banner test", typeof(RectTransform)); root.SetActive(false);
        try
        {
            var banner = root.AddComponent<MatchEventBannerController>();
            banner.visibility = root.AddComponent<CanvasGroup>(); banner.content = (RectTransform)root.transform;
            var image = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image)); image.transform.SetParent(root.transform);
            banner.icon = image.GetComponent<UnityEngine.UI.Image>();
            var text = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(root.transform);
            banner.caption = text.GetComponent<TMP_Text>();
            var detail = new GameObject("Detail", typeof(RectTransform), typeof(TextMeshProUGUI)); detail.transform.SetParent(root.transform);
            banner.detail = detail.GetComponent<TMP_Text>();
            var timingRoot = Object.Instantiate(root, root.transform);
            Object.DestroyImmediate(timingRoot.GetComponent<MatchEventBannerController>());
            banner.timingVisibility = timingRoot.GetComponent<CanvasGroup>();
            banner.timingContent = (RectTransform)timingRoot.transform;
            banner.timingIcon = timingRoot.GetComponentInChildren<UnityEngine.UI.Image>(true);
            var labels = timingRoot.GetComponentsInChildren<TMP_Text>(true);
            banner.timingCaption = labels[0]; banner.timingDetail = labels[1];
            banner.Clear();
            banner.clockArtwork = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/DecisionClock.png");
            Assert.That(banner.clockArtwork, Is.Not.Null);
            var timing = new DecisionTimingService(); timing.BeginDecision("target", DecisionKind.StandardPassTarget, "Away", 3); timing.Select("target");
            banner.SetTimingStatus(timing); Assert.That(banner.IsTimingVisible, Is.EqualTo(DecisionTimingConfig.CountdownDisplaySpentFraction <= 0));
            timing.Advance(4.75f); banner.SetTimingStatus(timing); Assert.That(banner.IsTimingVisible, Is.EqualTo(DecisionTimingConfig.CountdownDisplaySpentFraction <= .475f));
            timing.Advance(.25f); banner.SetTimingStatus(timing); Assert.That(banner.IsTimingVisible, Is.True);
            Assert.That(banner.timingIcon.sprite, Is.SameAs(banner.clockArtwork));
            Assert.That(banner.timingCaption.color.g, Is.EqualTo(.68f));
            Assert.That(banner.visibility.alpha, Is.Zero);
            banner.Enqueue(occasion, "Original"); banner.Advance(2);
            banner.Enqueue(MatchBannerEvent.RedCard, "Queued");
            timing.Advance(5); banner.SetTimingStatus(timing);
            Assert.That(banner.IsTimingVisible, Is.True);
            Assert.That(banner.timingVisibility.alpha, Is.EqualTo(1));
            Assert.That(banner.caption.text, Is.EqualTo(MatchEventAnnouncements.Caption(occasion)));
            Assert.That(banner.detail.text, Is.EqualTo("Original"));
            Assert.That(banner.PendingCount, Is.EqualTo(1));
            Assert.That(banner.visibility.blocksRaycasts || banner.visibility.interactable, Is.False);
            banner.Advance(1.9f); Assert.That(banner.CurrentEvent, Is.EqualTo(occasion));
            banner.Advance(.2f); Assert.That(banner.CurrentEvent, Is.EqualTo(MatchBannerEvent.RedCard));
            Assert.That(banner.IsTimingVisible, Is.True);
            banner.Advance(4.1f);
            Assert.That(banner.visibility.alpha, Is.Zero);
            Assert.That(banner.IsTimingVisible, Is.True);
            Assert.That(banner.timingCaption.text, Is.EqualTo("Match clock stopped"));
            Assert.That(banner.timingDetail.text, Does.Contain("Away"));
            timing.Clear(); banner.SetTimingStatus(timing);
            Assert.That(banner.timingVisibility.alpha, Is.Zero);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
