using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class RefereePreviewEditorTests
{
    private string previousScene;

    [SetUp]
    public void RememberScene()
    {
        var scene = SceneManager.GetActiveScene();
        Assert.That(SceneManager.sceneCount, Is.EqualTo(1));
        Assert.That(scene.isDirty, Is.False);
        previousScene = scene.path;
    }

    [TearDown]
    public void RestoreScene()
    {
        if (previousScene == null || SceneManager.GetActiveScene().path == previousScene) return;
        if (previousScene.Length == 0) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        else EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
    }

    [TestCase("Draft")]
    [TestCase("FreeDraft")]
    public void DraftBadgeUsesTheSameAnchorsAndPosition(string sceneName)
    {
        var scene = Open(sceneName);
        var panel = (RectTransform)Find<RefereePreviewPanel>(scene).transform;
        var card = (RectTransform)Find<RefereePreviewCard>(scene).transform;
        Assert.That(panel.anchorMin, Is.EqualTo(Vector2.up));
        Assert.That(panel.anchorMax, Is.EqualTo(Vector2.up));
        Assert.That(panel.anchoredPosition, Is.EqualTo(new Vector2(968f, -78f)));
        Assert.That(panel.sizeDelta, Is.EqualTo(new Vector2(200f, 64f)));
        Assert.That(card.sizeDelta, Is.EqualTo(new Vector2(432f, 648f)));
    }

    [TestCase("Draft", "Webster - 2", "Webster", "Scotland", "2")]
    [TestCase("Draft", "Castolo - 3", "Castolo", "Brazil", "3")]
    [TestCase("FreeDraft", "Bakker - 4", "Bakker", "Netherlands", "4")]
    [TestCase("FreeDraft", "Read - 5", "Read", "England", "5")]
    public void DraftCardShowsPublishedOrLocalRefereeOnlyDuringHover(
        string sceneName, string setting, string name, string country, string leniency)
    {
        var scene = Open(sceneName);
        var panel = Find<RefereePreviewPanel>(scene);
        var card = Find<RefereePreviewCard>(scene);
        var draft = Find<DraftManager>(scene);
        Refresh(panel);
        Assert.That(panel.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);

        draft.currentSettings = new GameSettings { referee = setting };
        Refresh(panel);
        Assert.That(panel.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
        Assert.That(Read(panel.transform, "RefereeName"), Is.EqualTo(name));
        Assert.That(Read(panel.transform, "LeniencyBadge/LeniencyValue"), Is.EqualTo(leniency));
        Assert.That(card.gameObject.activeSelf, Is.False);

        panel.OnPointerEnter(new PointerEventData(null));
        Assert.That(card.gameObject.activeSelf, Is.True);
        Assert.That(Read(card.transform, "RefereeName"), Is.EqualTo(name));
        Assert.That(Read(card.transform, "Nationality"), Is.EqualTo(country));
        Assert.That(Read(card.transform, "LeniencyValue"), Is.EqualTo(leniency));
        Assert.That(card.transform.Find("Flag").GetComponent<Image>().sprite, Is.EqualTo(FlagSpriteProvider.GetFlagSprite(country)).And.Not.Null);
        Assert.That(card.transform.Find("LeniencyBadge").GetComponent<Image>().color,
            Is.EqualTo(MatchStatsUI.GetInspectionAttributeColor(int.Parse(leniency))));
        panel.OnPointerExit(new PointerEventData(null));
        Assert.That(card.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void RoomUsesEqualGapsAndSharesThePlayerInspectionArea()
    {
        var scene = Open("Room");
        var panel = (RectTransform)Find<RefereePreviewPanel>(scene).transform;
        var connection = (RectTransform)panel.parent.Find("ConnectionPanel");
        var offline = (RectTransform)panel.parent.Find("RoomRulesButton");
        var online = (RectTransform)panel.parent.Find("OnlineRoomRulesButton");
        Assert.That(offline.anchoredPosition, Is.EqualTo(online.anchoredPosition));
        Assert.That(panel.anchoredPosition.x, Is.EqualTo(online.anchoredPosition.x));
        Assert.That(panel.anchoredPosition.y - online.anchoredPosition.y - online.rect.height,
            Is.EqualTo(online.anchoredPosition.y - connection.anchoredPosition.y - connection.rect.height));
        var card = (RectTransform)Find<RefereePreviewCard>(scene).transform;
        var playerCard = (RectTransform)card.parent.Find("OutfieldCard");
        Assert.That(card.rect.size, Is.EqualTo(playerCard.rect.size));

        var smallCard = panel.parent.Find("MatchStatsUI/HoverCardsRoot/HomeHoverCardAnchor/HomeOutfieldHoverCard");
        var playerBadge = smallCard.GetComponentsInChildren<Image>(true).Single(image => image.name == "PaceValueBadge");
        var playerValue = smallCard.GetComponentsInChildren<TMP_Text>(true).Single(text => text.name == "PaceValue");
        var refereeBadge = panel.Find("LeniencyBadge").GetComponent<Image>();
        var refereeValue = refereeBadge.transform.Find("LeniencyValue").GetComponent<TMP_Text>();
        Assert.That(refereeBadge.sprite, Is.EqualTo(playerBadge.sprite));
        Assert.That(refereeValue.font, Is.EqualTo(playerValue.font));
        Assert.That(ScaledDiameter(refereeBadge), Is.EqualTo(ScaledDiameter(playerBadge)).Within(.001f));
        Assert.That(refereeValue.fontSize * refereeValue.transform.lossyScale.x,
            Is.EqualTo(playerValue.fontSize * playerValue.transform.lossyScale.x).Within(.001f));
    }

    [Test]
    public void RoomDoubleClickAndBothKeyboardPathsToggleTheLocalRefereeCard()
    {
        var scene = Open("Room");
        var panel = Find<RefereePreviewPanel>(scene);
        var card = Find<RefereePreviewCard>(scene);
        var inspection = Find<RoomInspectionCard>(scene);
        var match = Find<MatchManager>(scene);
        match.gameData = new MatchManager.GameData
        {
            gameSettings = new MatchManager.GameSettings { referee = "Bakker - 4" }
        };
        Refresh(panel);
        var keyboard = new GameObject("Online referee preview keyboard test");
        InvokeLifecycle(inspection, "Awake");
        try
        {
            var onlineInput = keyboard.AddComponent<OnlineBoardInput>();
            var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left, clickCount = 1 };
            Assert.That(onlineInput.TryKey(KeyCode.Space), Is.False);
            panel.OnPointerEnter(pointer);
            panel.OnPointerClick(pointer);
            Assert.That(card.gameObject.activeSelf, Is.False);

            pointer.clickCount = 2;
            panel.OnPointerClick(pointer);
            Assert.That(inspection.IsOpen, Is.True);
            Assert.That(card.gameObject.activeSelf, Is.True);
            Assert.That(inspection.LockedPlayer, Is.Null);
            var space = new KeyPressData(KeyCode.Space, shift: false, ctrl: false, alt: false);
            Assert.That(RoomInputOwnership.ConsumeKey(space, true, null), Is.True);
            Assert.That(card.gameObject.activeSelf, Is.False);

            Assert.That(onlineInput.TryKey(KeyCode.Space), Is.True);
            Assert.That(card.gameObject.activeSelf, Is.True);
            panel.OnPointerExit(pointer);
            Assert.That(card.gameObject.activeSelf, Is.True);
            Assert.That(onlineInput.TryKey(KeyCode.Space), Is.True);
            Assert.That(inspection.IsOpen, Is.False);
        }
        finally
        {
            InvokeLifecycle(inspection, "OnDisable");
            Object.DestroyImmediate(keyboard);
        }
    }

    private static Scene Open(string name) => EditorSceneManager.OpenScene($"Assets/Scenes/{name}.unity", OpenSceneMode.Single);
    private static T Find<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
    private static string Read(Transform root, string path) => root.Find(path).GetComponent<TMP_Text>().text;
    private static float ScaledDiameter(Image badge) => badge.rectTransform.sizeDelta.x * badge.transform.lossyScale.x;
    private static void Refresh(RefereePreviewPanel panel) => InvokeLifecycle(panel, "LateUpdate");
    private static void InvokeLifecycle(MonoBehaviour component, string method) =>
        component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
}
