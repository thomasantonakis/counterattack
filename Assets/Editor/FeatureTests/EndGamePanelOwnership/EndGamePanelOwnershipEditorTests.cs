using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGamePanelOwnershipEditorTests
{
    [Test]
    public void RoomContainsOnlyMainCanvasPanelWithDesignerDimensions()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var panels = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EndGamePanelManager>(true)).ToArray();
            Assert.That(panels.Length, Is.EqualTo(1));
            Assert.That(panels[0].transform.parent.name, Is.EqualTo("MainCanvas"));
            Assert.That(panels[0].gameObject.activeSelf, Is.False);
            var recap = panels[0].transform.Find("RecapPanel").GetComponent<RectTransform>();
            Assert.That(recap.sizeDelta, Is.EqualTo(new Vector2(1400, 1240)));
            Assert.That(recap.anchorMin, Is.EqualTo(new Vector2(.5f, .5f)));
            Assert.That(recap.anchorMax, Is.EqualTo(recap.anchorMin));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ResolutionNeverFallsBackToUnrelatedCanvases(bool includeMain)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var name in new[] {"RoomIntroCanvas", "RulesViewer"})
            {
                var canvas = new GameObject(name, typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvas, scene);
                var copy = new GameObject("EndGamePanel", typeof(RectTransform), typeof(EndGamePanelManager));
                copy.transform.SetParent(canvas.transform, false);
            }
            EndGamePanelManager expected = null;
            if (includeMain)
            {
                var canvas = new GameObject("MainCanvas", typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvas, scene);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/EndGamePanel.prefab");
                var panel = Object.Instantiate(prefab, canvas.transform);
                panel.name = "EndGamePanel";
                expected = panel.GetComponent<EndGamePanelManager>();
            }
            var resolve = typeof(EndGamePanelManager).GetMethod("ResolveInScene", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(resolve.Invoke(null, new object[] {scene}), Is.SameAs(expected));
            var editorResolve = typeof(EndGamePanelPrefabEditorTools).GetMethod("ResolveMainCanvas", BindingFlags.Static | BindingFlags.NonPublic);
            var resolvedCanvas = (Canvas)editorResolve.Invoke(null, new object[] {scene});
            Assert.That(resolvedCanvas == null ? null : resolvedCanvas.name, Is.EqualTo(includeMain ? "MainCanvas" : null));
            if (expected != null)
                Assert.That(expected.transform.Find("RecapPanel").GetComponent<RectTransform>().sizeDelta,
                    Is.EqualTo(new Vector2(1400, 1240)));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
