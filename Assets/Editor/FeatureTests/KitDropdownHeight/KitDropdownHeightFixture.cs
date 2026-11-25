using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class KitDropdownHeightFixture
{
    private GameObject root;
    private ApplicationManager previousApp;
    private string previousMode;
    private GameObject ownedApp;

    public IEnumerator Verify(string mode, string kind)
    {
        previousApp = ApplicationManager.Instance;
        previousMode = previousApp != null ? previousApp.SelectedRoomGameMode : null;
        if (previousApp == null) { ownedApp = new GameObject("Owned setup preferences"); ownedApp.AddComponent<ApplicationManager>(); }
        ApplicationManager.Instance.SetSelectedRoomGameMode(mode);
        root = new GameObject("Owned dropdown height canvas", typeof(RectTransform), typeof(Canvas));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var managerObject = new GameObject("Owned disabled setup manager"); managerObject.transform.SetParent(root.transform);
        managerObject.SetActive(false);
        var manager = managerObject.AddComponent<CreateNewGameManager>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Buttons/KitDropDownButton.prefab");
        var dropdown = Object.Instantiate(prefab, root.transform).GetComponentInChildren<TMP_Dropdown>(true);
        dropdown.gameObject.SetActive(true);
        dropdown.ClearOptions(); dropdown.AddOptions(Enumerable.Range(0, kind == "international" ? 2 : kind.EndsWith("gk") ? 6 : 95).Select(i => "Kit " + i).ToList());
        if (kind == "home") manager.homeKitDropdown = dropdown;
        else if (kind == "away") manager.awayKitDropdown = dropdown;
        else if (kind == "homegk") manager.homeGKKitDropdown = dropdown;
        else if (kind == "awaygk") manager.awayGKKitDropdown = dropdown;
        else
        {
            manager.homeKitDropdown = dropdown;
            var match = new GameObject("Owned match type", typeof(RectTransform), typeof(TMP_Dropdown)); match.transform.SetParent(root.transform);
            manager.matchTypeDropdown = match.GetComponent<TMP_Dropdown>();
            manager.matchTypeDropdown.AddOptions(new System.Collections.Generic.List<string> { "International" });
        }
        yield return null; // TMP initializes its tween runner in Start.
        Canvas.ForceUpdateCanvases();
        float authoredHeight = dropdown.template.rect.height;
        float itemHeight = ((RectTransform)dropdown.template.Find("Viewport/Content/Item")).rect.height;
        dropdown.Show();
        Assert.That(dropdown.IsExpanded, Is.True);
        var popup = (RectTransform)dropdown.transform.Find("Dropdown List");
        var viewport = (RectTransform)popup.Find("Viewport");
        // Reproduce the short generated list independently of layout timing.
        popup.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
        var maintain = typeof(CreateNewGameManager).GetMethod("MaintainKitDropdownPopupHeight", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(TMP_Dropdown) }, null);
        maintain.Invoke(manager, new object[] { dropdown });
        float expected = kind == "home" || kind == "away" ? authoredHeight : itemHeight * dropdown.options.Count;
        Assert.That(popup.rect.height, Is.EqualTo(expected).Within(.1f), mode + " " + kind);
        Assert.That(viewport.rect.height, Is.EqualTo(expected).Within(.1f));
        Assert.That(dropdown.template.rect.height, Is.EqualTo(authoredHeight), "Preserve authored template.");
        Assert.That(dropdown.value, Is.Zero, "Sizing must not select another kit.");
        dropdown.Hide();
    }

    public IEnumerator Cleanup()
    {
        if(root != null) Object.DestroyImmediate(root);
        if(ownedApp != null) Object.DestroyImmediate(ownedApp);
        if(previousApp != null) previousApp.SetSelectedRoomGameMode(previousMode);
        yield return null;
    }
}
