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
            Assert.That(GetButton("WhiteBkg/ButtonsPanel/ContinueToMatchSetupButton",
                nameof(menu.ContinueToMatchSetup)), Is.SameAs(menu.continueButton));
            Assert.That(menu.joinButton.interactable, Is.False);
            Assert.That(menu.lobbyCode.readOnly,Is.False);
            Assert.That(menu.lobbyCode.targetGraphic.GetComponent<Outline>(),Is.Not.Null);
            Assert.That(menu.hostCode.readOnly,Is.True);
            Assert.That(menu.sessionPrefab,Is.Not.Null);
            Assert.That(OnlineSetupRules.Validate(OnlineModeMenu.HostDefaults()),Is.Empty);
            menu.lobbyCode.SetTextWithoutNotify("invalid");
            menu.Join();
            Assert.That(menu.status.text, Does.Contain("8-character"));
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(MenuPath));
            Assert.That(OnlineFrontendSession.Current, Is.Null);
            Assert.That(OnlineModeMenu.TakeJoinCode(), Is.Null);
        });
    }

    [Test]
    public void PastedLobbyCodeAllowsSurroundingWhitespace()
    {
        const string expected = "7A3F9C2B";
        Assert.That(OnlineModeMenu.TryReadLobbyCode("  " + expected.ToLowerInvariant() + "\r\n", out var code), Is.True);
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
            CollectionAssert.AreEqual(new[] { "CreateNewMatchButton", "Connection Status", "LoadMatchButton",
                    "Lobby Code", "OnlineLoadMatchButton", "BackToMainMenuButton" },
                rows.Cast<Transform>().Where(row => row.gameObject.activeSelf).Select(row => row.name).ToArray());
            Assert.That(menu.loadMatchButton, Is.SameAs(rows.Find("OnlineLoadMatchButton").GetComponent<Button>()));

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

    [Test]
    public void AuthoredJoinCardOffersOnlyAvailableRoles()
    {
        WithMenu(menu =>
        {
            Assert.That(menu.roleCard, Is.Not.Null);
            Assert.That(menu.roleCard.activeSelf, Is.False);
            var rows = menu.roleCard.transform.Find("WhiteBkg/ButtonsPanel");
            Assert.That(rows.GetComponent<VerticalLayoutGroup>(), Is.Not.Null);
            Assert.That(menu.awayRoleButton.transform.parent, Is.SameAs(rows));
            Assert.That(menu.spectatorRoleButton.transform.parent, Is.SameAs(rows));
            Assert.That(menu.awayRoleButton.onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(menu.SelectAway)));
            Assert.That(menu.spectatorRoleButton.onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(menu.SelectSpectator)));
            var back = rows.Find("BackToCodeButton").GetComponent<Button>();
            Assert.That(back.onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(menu.CancelRoleChoice)));
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var show = typeof(OnlineModeMenu).GetMethod("SetRoleCardVisible", flags);
            var options = typeof(OnlineModeMenu).GetMethod("SetRoleOptions", flags);
            show.Invoke(menu, new object[] { true });
            options.Invoke(menu, new object[] { new OnlineMatchRoleOptions(true, true, true) });
            Assert.That(menu.awayRoleButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(menu.spectatorRoleButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(menu.awayRoleButton.IsInteractable(), Is.True);
            Assert.That(menu.spectatorRoleButton.IsInteractable(), Is.True);
            Assert.That(menu.mainCard.interactable, Is.False);

            options.Invoke(menu, new object[] { new OnlineMatchRoleOptions(true, false, true) });
            Assert.That(menu.awayRoleButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(menu.spectatorRoleButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(menu.spectatorRoleButton.IsInteractable(), Is.True);
            options.Invoke(menu, new object[] { new OnlineMatchRoleOptions(true, true, false) });
            Assert.That(menu.awayRoleButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(menu.spectatorRoleButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(menu.roleStatus.text, Does.Contain("Spectating is unavailable"));
            options.Invoke(menu, new object[] { new OnlineMatchRoleOptions(false, false, false) });
            Assert.That(menu.spectatorRoleButton.gameObject.activeInHierarchy, Is.False);
            Assert.That(menu.roleStatus.text, Does.Contain("No active match"));
            menu.CancelRoleChoice();
            Assert.That(menu.roleCard.activeSelf, Is.False);
            Assert.That(menu.mainCard.interactable, Is.True);
        });
    }

    [Test]
    public void HostControlsExpandBelowHostWithoutHidingJoin()
    {
        WithMenu(menu =>
        {
            var panel = (RectTransform)menu.lobbyCode.transform.parent;
            var card = (RectTransform)panel.parent;
            var host = (RectTransform)panel.Find("CreateNewMatchButton");
            var back = (RectTransform)panel.Find("BackToMainMenuButton");
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            var hostPosition = host.position;
            var joinPosition = menu.lobbyCode.transform.position;
            try
            {
                menu.hostCode.gameObject.SetActive(true);
                menu.copyButton.gameObject.SetActive(true);
                menu.continueButton.gameObject.SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

                Assert.That(host.position, Is.EqualTo(hostPosition));
                Assert.That(menu.lobbyCode.gameObject.activeInHierarchy, Is.True);
                Assert.That(menu.lobbyCode.transform.position.y, Is.LessThan(joinPosition.y));
                Assert.That(card.InverseTransformPoint(back.TransformPoint(back.rect.min)).y,
                    Is.GreaterThanOrEqualTo(card.rect.yMin));
            }
            finally
            {
                menu.hostCode.gameObject.SetActive(false);
                menu.copyButton.gameObject.SetActive(false);
                menu.continueButton.gameObject.SetActive(false);
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
