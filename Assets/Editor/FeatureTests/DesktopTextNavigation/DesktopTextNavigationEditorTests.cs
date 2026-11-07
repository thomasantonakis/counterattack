using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Linq;

public class DesktopTextNavigationEditorTests
{
    private GameObject root;
    private DesktopTextInputField field;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Text navigation test", typeof(RectTransform), typeof(Canvas));
        var labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.rectTransform.sizeDelta = new Vector2(1000, 500);
        field = root.AddComponent<DesktopTextInputField>();
        field.textViewport = root.GetComponent<RectTransform>();
        field.textComponent = label;
        field.lineType = TMP_InputField.LineType.MultiLineNewline;
        field.text = "alpha beta\ngamma delta";
        label.ForceMeshUpdate();
        field.ForceLabelUpdate();
        field.stringPosition = 8;
        Assert.That(field.stringPosition, Is.EqualTo(8), "Fixture must have a positioned caret");
        Assert.That(label.textInfo.characterCount, Is.GreaterThan(22), "Fixture must have rendered text");
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(root);

    [TestCase(true, KeyCode.LeftArrow, EventModifiers.Command, 0)]
    [TestCase(true, KeyCode.RightArrow, EventModifiers.Command, 10)]
    [TestCase(true, KeyCode.LeftArrow, EventModifiers.Alt, 6)]
    [TestCase(true, KeyCode.RightArrow, EventModifiers.Alt, 10)]
    [TestCase(true, KeyCode.LeftArrow, EventModifiers.Control, 6)]
    [TestCase(true, KeyCode.A, EventModifiers.Control, 0)]
    [TestCase(true, KeyCode.E, EventModifiers.Control, 10)]
    [TestCase(true, KeyCode.UpArrow, EventModifiers.Command, 0)]
    [TestCase(true, KeyCode.DownArrow, EventModifiers.Command, 22)]
    [TestCase(false, KeyCode.LeftArrow, EventModifiers.Control, 6)]
    [TestCase(false, KeyCode.RightArrow, EventModifiers.Control, 11)]
    [TestCase(false, KeyCode.Home, EventModifiers.None, 0)]
    [TestCase(false, KeyCode.End, EventModifiers.None, 10)]
    [TestCase(false, KeyCode.Home, EventModifiers.Control, 0)]
    [TestCase(false, KeyCode.End, EventModifiers.Control, 22)]
    public void NavigationMovesCaretAndShiftExtendsSelection(bool mac, KeyCode key,
        EventModifiers modifiers, int expected)
    {
        Assert.That(field.HandleNavigation(new Event { keyCode = key, modifiers = modifiers }, mac), Is.True);
        Assert.That(field.stringPosition, Is.EqualTo(expected));
        Assert.That(field.selectionStringAnchorPosition, Is.EqualTo(expected));
        field.stringPosition = 8;
        field.HandleNavigation(new Event { keyCode = key, modifiers = modifiers | EventModifiers.Shift }, mac);
        Assert.That(field.selectionStringAnchorPosition, Is.EqualTo(8));
        Assert.That(field.selectionStringFocusPosition, Is.EqualTo(expected));
        Assert.That(field.text, Is.EqualTo("alpha beta\ngamma delta"));
    }

    [Test]
    [TestCase("", 0, KeyCode.RightArrow, 0)]
    [TestCase("alpha beta", 0, KeyCode.LeftArrow, 0)]
    [TestCase("alpha beta", 10, KeyCode.RightArrow, 10)]
    [TestCase("alpha beta", 5, KeyCode.RightArrow, 10)]
    [TestCase("alpha beta", 6, KeyCode.LeftArrow, 0)]
    [TestCase("alpha, beta", 5, KeyCode.RightArrow, 11)]
    [TestCase("άλφα βήτα", 7, KeyCode.LeftArrow, 5)]
    public void MacWordNavigationHandlesBoundariesAndSeparators(string text, int start, KeyCode key, int expected)
    {
        field.text = text;
        field.ForceLabelUpdate();
        field.textComponent.ForceMeshUpdate();
        field.stringPosition = start;
        field.HandleNavigation(new Event { keyCode = key, modifiers = EventModifiers.Alt }, true);
        Assert.That(field.stringPosition, Is.EqualTo(expected));
    }

    [Test]
    public void LineNavigationUsesCurrentLineAndPreservesSelectionAnchor()
    {
        field.stringPosition = 15;
        field.HandleNavigation(new Event {keyCode = KeyCode.LeftArrow, modifiers = EventModifiers.Command | EventModifiers.Shift}, true);
        Assert.That(field.selectionStringAnchorPosition, Is.EqualTo(15));
        Assert.That(field.selectionStringFocusPosition, Is.EqualTo(11));
        field.HandleNavigation(new Event {keyCode = KeyCode.RightArrow, modifiers = EventModifiers.Command | EventModifiers.Shift}, true);
        Assert.That(field.selectionStringAnchorPosition, Is.EqualTo(15));
        Assert.That(field.selectionStringFocusPosition, Is.EqualTo(22));
    }

    [TestCase(KeyCode.C, EventModifiers.Command)]
    [TestCase(KeyCode.V, EventModifiers.Control)]
    [TestCase(KeyCode.LeftArrow, EventModifiers.None)]
    [TestCase(KeyCode.Return, EventModifiers.None)]
    public void OtherKeysRetainTmpHandling(KeyCode key, EventModifiers modifiers)
    {
        Assert.That(field.HandleNavigation(new Event {keyCode = key, modifiers = modifiers}, true), Is.False);
        Assert.That(field.stringPosition, Is.EqualTo(8));
    }

    [Test]
    public void RoomFieldsUseDesktopNavigationAndKeepManagerReferences()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Room.unity");
        try
        {
            var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PauseMenuManager>(true)).Single();
            Assert.That(manager.saveNameInput, Is.TypeOf<DesktopTextInputField>());
            Assert.That(manager.tacticalNoteInput, Is.TypeOf<DesktopTextInputField>());
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
