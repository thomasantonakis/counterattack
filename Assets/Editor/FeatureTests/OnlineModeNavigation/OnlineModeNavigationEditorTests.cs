using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Category("OnlineModeNavigation")]
public sealed class OnlineModeNavigationEditorTests
{
    private const string MenuPath = "Assets/Scenes/OnlineModeMenu.unity";

    [Test]
    public void MenuHasAuthoredNavigationAndRejectsInvalidJoin()
    {
        Assert.That(EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == MenuPath), Is.True);
        WithMenu(menu =>
        {
            Button GetButton(string label, string callback)
            {
                var button = menu.transform.Find(label).GetComponent<Button>();
                Assert.That(button.onClick.GetPersistentTarget(0), Is.SameAs(menu));
                Assert.That(button.onClick.GetPersistentMethodName(0), Is.EqualTo(callback));
                return button;
            }

            GetButton("Host", nameof(menu.Host));
            GetButton("Back", nameof(menu.Back));
            GetButton("Join", nameof(menu.Join));
            menu.lobbyCode.SetTextWithoutNotify("invalid");
            menu.Join();
            Assert.That(menu.status.text, Does.Contain("32-character"));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(MenuPath));
            Assert.That(OnlineFrontendSession.Current, Is.Null);
            Assert.That(OnlineModeMenu.TakeJoinCode(), Is.Null);
        });
    }

    [Test]
    public void PastedLobbyCodeAllowsSurroundingWhitespace()
    {
        const string expected = "ABCDEFabcdef01234567890123456789";
        Assert.That(OnlineModeMenu.TryReadLobbyCode("  " + expected + "\r\n", out var code), Is.True);
        Assert.That(code, Is.EqualTo(expected));
    }

    [Test]
    public void AuthoredControlsFitWideClassicAndPortraitCanvas()
    {
        WithMenu(menu =>
        {
            var scaler = menu.GetComponentInParent<CanvasScaler>();
            Assert.That(scaler.screenMatchMode, Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            var controls = menu.GetComponentsInChildren<Selectable>();
            Assert.That(controls.Length, Is.EqualTo(4));
            var rectangles = controls.Select(control =>
            {
                var rect = (RectTransform)control.transform;
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rect.anchorMax, Is.EqualTo(rect.anchorMin));
                return new Rect(rect.anchoredPosition - rect.sizeDelta / 2, rect.sizeDelta);
            }).ToArray();

            for (int i = 0; i < rectangles.Length; i++)
                for (int j = i + 1; j < rectangles.Length; j++)
                    Assert.That(rectangles[i].Overlaps(rectangles[j]), Is.False);

            foreach (var screen in new[] { new Vector2(2560, 1080), new Vector2(1024, 768), new Vector2(720, 960) })
            {
                float scale = Mathf.Min(screen.x / scaler.referenceResolution.x, screen.y / scaler.referenceResolution.y);
                var bounds = new Rect(-screen / (2 * scale), screen / scale);
                foreach (var rect in rectangles)
                    Assert.That(bounds.Contains(rect.min) && bounds.Contains(rect.max), Is.True);
            }
        });
    }

    private static void WithMenu(System.Action<OnlineModeMenu> validate)
    {
        var original = SceneManager.GetActiveScene();
        string previous = original.path;
        Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
        Assert.That(original.isDirty, Is.False);
        try
        {
            var scene = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            var menu = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<OnlineModeMenu>()).Single();
            validate(menu);
        }
        finally
        {
            if (string.IsNullOrEmpty(previous))
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }
    }
}
