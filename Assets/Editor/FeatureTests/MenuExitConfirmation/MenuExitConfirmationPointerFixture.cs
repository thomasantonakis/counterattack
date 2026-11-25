using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MenuExitConfirmationPointerFixture
{
    public static IEnumerator QuitFooterLabelsReceivePointerClicks()
    {
        const string assetPath = "Assets/Resources/UI/Common/QuitGameConfirmation.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        var canvasObject = new GameObject("Confirmation test canvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var instance = Object.Instantiate(prefab, canvasObject.transform);
        var quitPanel = instance.GetComponent<MenuConfirmation>();
        var serialized = new SerializedObject(quitPanel);
        var quitBack = (Button)serialized.FindProperty("backButton").objectReferenceValue;
        var quitConfirm = (Button)serialized.FindProperty("confirmButton").objectReferenceValue;
        int confirmations = 0;

        try
        {
            quitPanel.Show(() => confirmations++);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            var raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            ClickCenter(quitConfirm, raycaster);
            Assert.That(confirmations, Is.EqualTo(1));
            ClickCenter(quitBack, raycaster);
            Assert.That(instance.activeSelf, Is.False);
            Assert.That(confirmations, Is.EqualTo(1));
        }
        finally
        {
            if (instance.activeSelf)
                quitPanel.Close();
            Object.Destroy(canvasObject);
        }
    }

    private static void ClickCenter(Button button, GraphicRaycaster raycaster)
    {
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
        };
        var hits = new List<RaycastResult>();
        raycaster.Raycast(pointer, hits);
        Assert.That(hits, Is.Not.Empty);
        Assert.That(ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler),
            Is.SameAs(button.gameObject), button.name);
    }
}
