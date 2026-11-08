using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DraftSettingsRulesEditorTests
{
    [Test]
    public void AuthoredRuleLinksResolveInsideTheBundledDocument()
    {
        var blocks = RulesViewerEditorTools.LoadDocument();
        RulesDocument.ValidateLinks(blocks);
        var viewer = AssetDatabase.LoadAssetAtPath<GameObject>(RulesViewerEditorTools.PrefabPath).GetComponent<RulesViewer>();
        var targets = viewer.entries.SelectMany(entry =>
            System.Text.RegularExpressions.Regex.Matches(entry.text.text, "<link=\"([^\"]+)\">")
                .Cast<System.Text.RegularExpressions.Match>().Select(match => match.Groups[1].Value)).ToArray();
        Assert.That(targets, Does.Contain("getting_started"));
        Assert.That(targets, Does.Contain("goalkeeper"));
        Assert.That(targets.Any(target => target.Contains("counterattackgame.github.io")), Is.False);
        Assert.That(targets.Where(target => target.StartsWith("http")),
            Is.EqualTo(new[] { "https://github.com/counterattackgame/wiki" }), "Only source attribution opens an external site.");
        foreach (string target in targets.Where(target => !target.StartsWith("http")))
            Assert.That(viewer.entries.Any(entry => entry.anchor == target), Is.True, target);
    }

    [Test]
    public void TableRowsKeepTheirColumnsAndSearchableDescriptions()
    {
        var blocks = RulesDocument.Parse("setup", "# Setup\n| Setting | Behavior |\n| --- | --- |\n| GK Free | Managers alternate selections. |");
        Assert.That(blocks.Count, Is.EqualTo(3), "The separator is not article content.");
        Assert.That(blocks[2].text, Does.Contain("<indent=260>Managers alternate selections.</indent>"));
        Assert.That(blocks[2].plain, Is.EqualTo("GK Free Managers alternate selections."));
        Assert.That(RulesDocument.Search(blocks.Select(b => b.plain).ToArray(), "alternate selections"), Has.Count.EqualTo(1));
    }

    [Test]
    public void AuthoredRulesIncludeEveryDraftSettingAndInternationalExplanation()
    {
        var blocks = RulesViewerEditorTools.LoadDocument();
        var viewer = AssetDatabase.LoadAssetAtPath<GameObject>(RulesViewerEditorTools.PrefabPath).GetComponent<RulesViewer>();
        Assert.That(viewer.entries.Length, Is.EqualTo(blocks.Count));
        foreach (string setting in new[] { "Outfield Regular", "Outfield Free Regular", "Outfield Arcade", "GK Deal", "GK Free", "GK Arcade" })
            Assert.That(viewer.entries.Any(e => e.text.text.StartsWith("<b>" + setting + "</b><indent=260>")), Is.True, setting);
        Assert.That(viewer.entries.Any(e => e.text.text.Contains("both draft fields show <b>International</b> and cannot be changed")), Is.True);
    }

    [Test]
    public void InternationalFieldsAreLockedAndRegularFieldsBecomeSelectableAgain()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/CreateNewGameScene.unity");
        try
        {
            var manager = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CreateNewGameManager>(true)).Single();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(CreateNewGameManager).GetMethod("SetInternationalDraftDropdownOptions", flags).Invoke(manager, null);
            foreach (var dropdown in new[] { manager.draftDropdown, manager.gkDraftDropdown })
            {
                Assert.That(dropdown.options.Select(option => option.text), Is.EqualTo(new[] { "International" }));
                Assert.That(dropdown.captionText.text, Is.EqualTo("International"));
                Assert.That(dropdown.interactable, Is.False);
            }
            typeof(CreateNewGameManager).GetMethod("SetRegularDraftDropdownOptions", flags).Invoke(manager, null);
            Assert.That(manager.draftDropdown.options.Select(option => option.text), Is.EqualTo(new[] { "Regular", "Free Regular", "Arcade" }));
            Assert.That(manager.gkDraftDropdown.options.Select(option => option.text), Is.EqualTo(new[] { "Deal", "Free", "Arcade" }));
            Assert.That(manager.draftDropdown.interactable && manager.gkDraftDropdown.interactable, Is.True);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
