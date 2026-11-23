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

            GetButton("WhiteBkg/ButtonsPanel/CreateNewMatchButton", nameof(menu.Host));
            GetButton("WhiteBkg/ButtonsPanel/BackToMainMenuButton", nameof(menu.Back));
            GetButton("ExitButton", nameof(menu.Exit));
            GetButton("WhiteBkg/ButtonsPanel/LoadMatchButton", nameof(menu.Join));
            Assert.That(menu.lobbyCode.readOnly,Is.False);
            Assert.That(menu.lobbyCode.targetGraphic.GetComponent<Outline>(),Is.Not.Null);
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
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(menu.gameObject),
                Is.EqualTo("Assets/Resources/UI/Cards/MenuCard_SinglePlayer.prefab"));
            var card = (RectTransform)menu.transform;
            var rows = card.Find("WhiteBkg/ButtonsPanel");
            Assert.That(rows.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            var header = (RectTransform)card.Find("WhiteBkg/HeaderPanel");
            var hostRow = (RectTransform)rows.Find("CreateNewMatchButton");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(card);
            var corners = new Vector3[4];
            header.GetWorldCorners(corners);
            float headerBottom = card.InverseTransformPoint(corners[0]).y;
            hostRow.GetWorldCorners(corners);
            float hostTop = card.InverseTransformPoint(corners[1]).y;
            Assert.That(hostTop, Is.LessThan(headerBottom), "The heading must clear the Host row");
            CollectionAssert.AreEqual(new[] { "CreateNewMatchButton", "Lobby Code", "LoadMatchButton",
                    "Connection Status", "BackToMainMenuButton" },
                rows.Cast<Transform>().Where(row => row.gameObject.activeSelf).Select(row => row.name).ToArray());

            foreach (var screen in new[] { new Vector2(2560, 1080), new Vector2(1024, 768), new Vector2(720, 960) })
            {
                float scale = Mathf.Min(screen.x / scaler.referenceResolution.x, screen.y / scaler.referenceResolution.y);
                var bounds = screen / scale;
                var origin = Vector2.Scale(card.anchorMin, bounds) + card.anchoredPosition;
                var size = card.sizeDelta;
                foreach (var corner in new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one })
                {
                    var local = Vector2.Scale(corner - card.pivot, size);
                    var offset = card.localRotation * Vector3.Scale(local, card.localScale);
                    var point = origin + (Vector2)offset;
                    Assert.That(point.x, Is.InRange(0, bounds.x));
                    Assert.That(point.y, Is.InRange(0, bounds.y));
                }
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
