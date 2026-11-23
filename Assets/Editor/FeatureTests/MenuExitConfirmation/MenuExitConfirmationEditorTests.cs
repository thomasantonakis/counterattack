using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MenuExitConfirmationEditorTests
{
    private GameObject root;
    private MenuConfirmation panel;
    private Button back;
    private Button confirm;
    private Button alternative;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Confirmation", typeof(RectTransform));
        root.SetActive(false);
        panel = root.AddComponent<MenuConfirmation>();
        back = AddButton("Back");
        confirm = AddButton("Confirm");
        alternative = AddButton("Alternative");
        var serialized = new SerializedObject(panel);
        serialized.FindProperty("backButton").objectReferenceValue = back;
        serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
        serialized.FindProperty("alternativeButton").objectReferenceValue = alternative;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        typeof(MenuConfirmation).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(panel, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (root.activeSelf)
            panel.Close();
        Object.DestroyImmediate(root);
    }

    [Test]
    public void BackClosesThePanelWithoutTakingEitherAction()
    {
        int actions = 0;
        panel.Show(() => actions++, () => actions++);
        Assert.That(root.activeSelf, Is.True);
        Assert.That(actions, Is.Zero);

        back.onClick.Invoke();
        Assert.That(root.activeSelf, Is.False);
        confirm.onClick.Invoke();
        alternative.onClick.Invoke();
        Assert.That(actions, Is.Zero);
    }

    [Test]
    public void ConfirmAndAlternativeInvokeOnlyTheirOwnAction()
    {
        int confirmations = 0;
        int alternatives = 0;
        panel.Show(() => confirmations++, () => alternatives++);
        confirm.onClick.Invoke();
        Assert.That(confirmations, Is.EqualTo(1));
        Assert.That(alternatives, Is.Zero);
        Assert.That(root.activeSelf, Is.True, "A failed save can keep the confirmation open.");

        alternative.onClick.Invoke();
        Assert.That(alternatives, Is.EqualTo(1));
        Assert.That(confirmations, Is.EqualTo(1));
    }

    private Button AddButton(string name)
    {
        var button = new GameObject(name, typeof(RectTransform), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(root.transform, false);
        return button;
    }
}
