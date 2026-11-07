using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class RulesViewerEditorTests
{
    [Test] public void BundledDocumentHasValidLinksAndAllChapters()
    {
        var blocks = RulesViewerEditorTools.LoadDocument();
        foreach (string page in RulesViewerEditorTools.Pages) Assert.That(blocks.Any(b => b.anchor == page), Is.True, page);
        Assert.That(blocks.Count(b => b.image != null), Is.EqualTo(8));
        Assert.That(blocks.Any(b => b.plain.IndexOf("reckless", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
    }

    [Test] public void RepeatedHeadingsHaveUniqueStableAnchors()
    {
        var blocks = RulesDocument.Parse("passing", "# Passing\n## Standard Pass\nText\n## Standard Pass\nText");
        Assert.That(blocks.Where(b => b.level > 0).Select(b => b.anchor), Is.EqualTo(new[] { "passing", "passing#standard-pass", "passing#standard-pass-1" }));
    }

    [TestCase("https://counterattackgame.github.io/wiki/en/heading#headed-passes", "heading#headed-passes")]
    [TestCase("#headed-passes", "passing#headed-passes")]
    [TestCase("heading.md", "heading")]
    [TestCase("https://github.com/counterattackgame/wiki", "https://github.com/counterattackgame/wiki")]
    public void LinksResolveToLocalAnchorsOrExplicitExternalUrls(string url, string expected)
        => Assert.That(RulesDocument.LinkTarget(url, "passing"), Is.EqualTo(expected));

    [Test] public void BrokenLinksFailGeneration()
        => Assert.Throws<InvalidOperationException>(() => RulesDocument.ValidateLinks(RulesDocument.Parse("test", "# Test\n[Missing](missing)")));

    [Test] public void SearchFindsLiteralPhraseAcrossWholeDocumentIgnoringCase()
    {
        var matches = RulesDocument.Search(new[] { "First PASS and pass.", "Pass?", "Pace" }, "pass");
        Assert.That(matches.Select(m => m.block), Is.EqualTo(new[] { 0,0,1 }));
        Assert.That(matches.Select(m => m.start), Is.EqualTo(new[] { 6,15,0 }));
        Assert.That(RulesDocument.Search(new[] { "a.b axb" }, "a.b").Count, Is.EqualTo(1));
        Assert.That(RulesDocument.Search(new[] { "anything" }, "  "), Is.Empty);
    }

    [Test] public void PrefabContainsAuthoredArticleContentsResultsAndDiagrams()
    {
        var viewer = AssetDatabase.LoadAssetAtPath<GameObject>(RulesViewerEditorTools.PrefabPath).GetComponent<RulesViewer>();
        Assert.That(viewer.surface.activeSelf, Is.False);
        Assert.That(viewer.entries.Length, Is.EqualTo(RulesViewerEditorTools.LoadDocument().Count));
        Assert.That(viewer.entries.All(e => e.resultButton != null && e.text != null && e.rect != null), Is.True);
        Assert.That(viewer.entries.Where(e => e.heading).All(e => e.contentsButton != null), Is.True);
        Assert.That(viewer.GetComponentsInChildren<RawImage>(true).Length, Is.EqualTo(8));
        Assert.That(viewer.surface.GetComponent<GameplayInputConsumer>().BlocksGameplayWhileActive, Is.True);
    }

    [TestCase("Assets/Scenes/MainMenu.unity")]
    [TestCase("Assets/Scenes/Room.unity")]
    public void BothScenesExposeRulesUsingTheSharedPrefab(string path)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(path);
        try
        {
            var viewers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RulesViewer>(true)).ToArray();
            Assert.That(viewers.Length, Is.EqualTo(1));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(viewers[0]).gameObject,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(RulesViewerEditorTools.PrefabPath)));
            var buttons = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Button>(true)).ToArray();
            foreach (string name in path.Contains("MainMenu") ? new[] { "Rules" } : new[] { "RulesButton", "RoomRulesButton" })
            {
                var button = buttons.Single(b => b.name == name);
                Assert.That(button.onClick.GetPersistentEventCount(), Is.EqualTo(1), name);
                Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo("OpenRules"), name);
            }
            if (path.Contains("Room"))
            {
                var pause = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PauseMenuManager>(true)).Single();
                var rules = (RectTransform)buttons.Single(b => b.name == "RulesButton").transform;
                var back = (RectTransform)pause.backToMainMenuButton.transform;
                var settings = (RectTransform)pause.editSettings.transform;
                Assert.That(rules.GetSiblingIndex(), Is.EqualTo(back.GetSiblingIndex() - 1));
                Assert.That(rules.anchoredPosition.y, Is.LessThan(settings.anchoredPosition.y - settings.rect.height / 2));
                Assert.That(back.anchoredPosition.y, Is.LessThan(rules.anchoredPosition.y - rules.rect.height / 2));
                var direct = buttons.Single(b => b.name == "RoomRulesButton");
                var monitor = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameDebugMonitor>(true)).Single();
                var instructions = (RectTransform)monitor.instructionText.transform.parent;
                var directRect = (RectTransform)direct.transform;
                Assert.That(direct.transform.parent, Is.SameAs(instructions.parent));
                Assert.That(directRect.anchoredPosition.y, Is.GreaterThan(instructions.anchoredPosition.y + instructions.rect.height));
                Assert.That(direct.navigation.mode, Is.EqualTo(Navigation.Mode.None), "No automatic links to MatchStatsUI or other HUD controls.");
            }
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
    }
}
