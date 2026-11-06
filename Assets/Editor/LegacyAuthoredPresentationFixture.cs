using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class GoalkeeperBoxEntryFixture
{
    private Action restoreAuthoredPanelState;
    private DuelResolutionPanelController duelPanel;

    private void AttachAuthoredPanels()
    {
        Assert.That(File.ReadAllText(FrameworkAuthoredPanelAssets.HashPath).Trim(),
            Is.EqualTo(FrameworkAuthoredPanelAssets.SourceHash()),
            "Room changed: explicitly regenerate the test-only authored panels with FrameworkAuthoredPanelAssets.Export before running presentation coverage.");
        var types = new[] { typeof(DuelResolutionPanelController), typeof(HeaderChallengePanelController), typeof(StoppageActionsPanelController) };
        var statics = types.SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(f => !f.IsInitOnly && !f.IsLiteral).Select(f => (field: f, value: f.GetValue(null))).ToArray();
        restoreAuthoredPanelState = () => { foreach (var item in statics) item.field.SetValue(null, item.value); };
        foreach (var type in types)
            type.GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FrameworkAuthoredPanelAssets.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        var panels = Object.Instantiate(prefab, root.transform, false);
        duelPanel = panels.GetComponentInChildren<DuelResolutionPanelController>(true);
        Assert.That(duelPanel.HasSceneBindings, Is.True);
        movement.duelResolutionPanel = duelPanel;
        var stoppage = panels.GetComponentInChildren<StoppageActionsPanelController>(true);
        stoppage.match = match;
        match.stoppageActionsPanel = stoppage;
        match.shotManager.headerManager.headerChallengePanel = panels.GetComponentInChildren<HeaderChallengePanelController>(true);
    }

    private void EnableForAuthoredPresentation(MonoBehaviour owner)
    {
        // Single-roll panels legitimately require a live enabled owner. These
        // managers have no bootstrap Start/Awake; MatchManager remains inactive.
        if (manuallySubscribed.Remove(owner)) Invoke(owner, "OnDisable");
        owner.enabled = true;
    }

    public IEnumerator RunDuelPanelBindingsLegacy()
    {
        Board(1); AttachAuthoredPanels();
        Assert.That(movement.duelResolutionPanel, Is.SameAs(duelPanel));
        Assert.That(duelPanel.HasSceneBindings, Is.True, "Room must supply its fully wired authored duel panel.");
        Assert.That(duelPanel.resultSeconds, Is.EqualTo(5f));
        Assert.That(duelPanel.IsOpen, Is.False);
        Assert.That(duelPanel.leftPlayer.die.IsAnimating || duelPanel.rightPlayer.die.IsAnimating, Is.False);
        yield return null;
        Assert.That(duelPanel.IsOpen, Is.False);
    }
}
