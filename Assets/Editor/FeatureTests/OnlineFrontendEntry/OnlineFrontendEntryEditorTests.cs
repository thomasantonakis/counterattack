using System;
using System.Linq;
using CounterAttack.Online;
using Epic.OnlineServices.P2P;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[Category("OnlineFrontendEntry")]
public sealed class OnlineFrontendEntryEditorTests
{
    [TestCase(null,false)] [TestCase("",false)] [TestCase("short",false)]
    [TestCase("01234567890123456789012345678901",false)]
    [TestCase("7A3F9C2B",true)] [TestCase("7a3f9c2b",true)]
    [TestCase("7A3F9C2-",false)] [TestCase("7A3F9C2G",false)] [TestCase("7A3F9C2é",false)]
    public void LobbyCodeIsValidatedBeforeNativeLogin(string code,bool valid)
        =>Assert.That(OnlineFrontendSessionPolicy.ValidLobbyCode(code),Is.EqualTo(valid));

    [TestCase(false,RelayControl.AllowRelays)] [TestCase(true,RelayControl.ForceRelays)]
    public void RoutingUsesDirectFirstWithRelayFallback(bool forced,RelayControl expected)
        =>Assert.That(OnlineFrontendSessionPolicy.Route(forced),Is.EqualTo(expected));

    [Test] public void CompatibilityIncludesBundledCatalog()
    {
        var a=OnlineFrontendSessionPolicy.Compatibility();var b=OnlineFrontendSessionPolicy.Compatibility();
        Assert.That(a.Catalog,Is.EqualTo(b.Catalog));
        Assert.That(a.Catalog,Does.StartWith("catalog-"));Assert.That(a.Catalog.Length,Is.EqualTo(72));
    }

    [Test] public void AuthoredControlsHavePersistentTransportAndNoOfflineOverlay()
    {
        string previous=SceneManager.GetActiveScene().path;
        Assert.That(SceneManager.GetActiveScene().isDirty,Is.False,"Do not replace unsaved scene changes");
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CreateNewGameScene.unity",OpenSceneMode.Single);
            var ui=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<OnlineSetupNetworkPanel>(true)).Single();
            Assert.That(ui.setup,Is.Not.Null);Assert.That(ui.view,Is.Not.Null);Assert.That(ui.panel.activeSelf,Is.False);
            Assert.That(ui.hostButton,Is.Not.Null);Assert.That(ui.joinButton,Is.Not.Null);Assert.That(ui.leaveButton,Is.Not.Null);Assert.That(ui.copyButton,Is.Not.Null);
            Assert.That(ui.lobbyCode.characterLimit,Is.EqualTo(32));Assert.That(ui.lobbyCode.onValueChanged.GetPersistentEventCount(),Is.Zero);
            Assert.That(ui.connectionStatus.transform.IsChildOf(ui.panel.transform),Is.True);
            Assert.That(AssetDatabase.GetAssetPath(ui.sessionPrefab),Is.EqualTo(OnlineFrontendEntryAuthoring.PrefabPath));
            Assert.That(ui.sessionPrefab.GetComponentsInChildren<Canvas>(true),Is.Empty,"Persistent transport is nonvisual");
            Assert.That(ui.panel.transform.parent.name,Is.EqualTo("Header"));
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
