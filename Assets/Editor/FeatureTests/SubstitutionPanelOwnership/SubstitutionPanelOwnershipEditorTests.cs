using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SubstitutionPanelOwnershipEditorTests
{
    [Test]
    public void RoomHasOneCompleteMainCanvasSubstitutionPanel()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var panels = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SubstitutionMenuView>(true)).ToArray();
            Assert.That(panels.Length, Is.EqualTo(1));
            Assert.That(panels[0].transform.parent.name, Is.EqualTo("MainCanvas"));
            Assert.That(panels[0].HasRequiredReferences(), Is.True);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RuntimeAndEditorIgnoreUnrelatedCanvases(bool includeMain)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            SubstitutionMenuView expected = null;
            foreach (var name in new[] {"RoomIntroCanvas", "RulesViewer", "MainCanvas"})
            {
                if (name == "MainCanvas" && !includeMain) continue;
                var canvas = new GameObject(name, typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvas, scene);
                var panel = new GameObject("SubstitutionPanel", typeof(RectTransform), typeof(SubstitutionMenuView));
                panel.transform.SetParent(canvas.transform, false);
                if (name == "MainCanvas") expected = panel.GetComponent<SubstitutionMenuView>();
            }
            var runtime = typeof(SubstitutionMenuManager).GetMethod("ResolveScenePanel", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(runtime.Invoke(null, new object[] {scene}), Is.SameAs(expected));
            var editor = typeof(CounterAttack.Editor.SubstitutionPanelPrefabEditorTools).GetMethod("ResolveMainCanvas", BindingFlags.Static | BindingFlags.NonPublic);
            var result = (Canvas)editor.Invoke(null, new object[] {scene});
            Assert.That(result == null ? null : result.name, Is.EqualTo(includeMain ? "MainCanvas" : null));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
