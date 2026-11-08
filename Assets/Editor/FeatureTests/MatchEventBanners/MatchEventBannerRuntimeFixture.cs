using System;
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MatchEventBannerRuntimeFixture
{
    private readonly float previousTimeScale = Time.timeScale;
    private readonly Scene scene;
    private readonly GameObject root;
    private readonly MatchEventBannerController panel;

    public MatchEventBannerRuntimeFixture()
    {
        scene = SceneManager.CreateScene("Isolated match event banner test");
        root = new GameObject("Banner", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(root, scene);
        root.SetActive(false);
        panel = root.AddComponent<MatchEventBannerController>();
        panel.visibility = root.AddComponent<CanvasGroup>();
        panel.content = (RectTransform)root.transform;
        var image = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        image.transform.SetParent(root.transform, false);
        panel.icon = image.GetComponent<UnityEngine.UI.Image>();
        var text = new GameObject("Caption", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(root.transform, false);
        panel.caption = text.GetComponent<TextMeshProUGUI>();
        panel.displaySeconds = 3f;
        root.SetActive(true);
    }

    public IEnumerator Run()
    {
        Time.timeScale = 0f;
        MatchEventAnnouncements.Publish(MatchBannerEvent.Foul, "Test player");
        MatchEventAnnouncements.Publish(MatchBannerEvent.RedCard, "Test player");
        Assert.That(panel.CurrentEvent, Is.EqualTo(MatchBannerEvent.Foul));
        Assert.That(panel.PendingCount, Is.EqualTo(1));
        float deadline = Time.realtimeSinceStartup + 1f;
        while (panel.visibility.alpha < .99f && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(panel.visibility.alpha, Is.GreaterThan(.98f), "Banner must animate while the game is paused.");
        Assert.That(panel.visibility.blocksRaycasts, Is.False);
        deadline = Time.realtimeSinceStartup + 4f;
        while (panel.CurrentEvent != MatchBannerEvent.RedCard && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(panel.CurrentEvent, Is.EqualTo(MatchBannerEvent.RedCard));
        Assert.That(panel.caption.text, Is.EqualTo("RED CARD"));
        root.SetActive(false);
        MatchEventAnnouncements.Publish(MatchBannerEvent.Injury);
        Assert.That(panel.PendingCount, Is.Zero);
        Assert.That(panel.IsShowing, Is.False);
        Assert.That(panel.visibility.alpha, Is.Zero);
        root.SetActive(true);
        Assert.That(panel.IsShowing, Is.False, "Re-enabling cannot replay the previous queue.");
    }

    public IEnumerator Cleanup()
    {
        Time.timeScale = previousTimeScale;
        if (root != null) UnityEngine.Object.Destroy(root);
        yield return null;
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
    }
}
