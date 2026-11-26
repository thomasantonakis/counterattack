using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MenuExitConfirmationPointerFixture
{
    public static IEnumerator BackArrowShowsHoverWhileItHasFocus()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Common/QuitGameConfirmation.prefab");
        var canvasObject = new GameObject("Confirmation hover test", typeof(Canvas), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var ownedEventSystem = EventSystem.current == null
            ? new GameObject("Confirmation hover test events", typeof(EventSystem))
            : null;
        var events = EventSystem.current;
        var panel = Object.Instantiate(prefab, canvasObject.transform).GetComponent<MenuConfirmation>();
        var back = (Button)new SerializedObject(panel).FindProperty("backButton").objectReferenceValue;

        try
        {
            panel.Show(() => Assert.Fail("Back must not confirm."));
            yield return WaitForImmediateAnimation(back.animator, "Normal");
            var arrow = back.transform.Find("ArrowPanel");
            Assert.That(arrow.localScale.x, Is.Zero.Within(0.01f));
            Assert.That(events.currentSelectedGameObject, Is.SameAs(back.gameObject));

            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)back.transform;
            var pointer = new PointerEventData(events)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            var hits = new List<RaycastResult>();
            canvasObject.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerEnterHandler),
                Is.SameAs(back.gameObject));
            yield return WaitForImmediateAnimation(back.animator, "Highlighted");
            Assert.That(events.currentSelectedGameObject, Is.SameAs(back.gameObject));

            ExecuteEvents.Execute(back.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return WaitForAnimation(back.animator, "Pressed");
            ExecuteEvents.Execute(back.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return WaitForImmediateAnimation(back.animator, "Highlighted");

            ExecuteEvents.Execute(back.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            yield return WaitForImmediateAnimation(back.animator, "Normal");
            Assert.That(arrow.localScale.x, Is.Zero.Within(0.01f));
            Assert.That(events.currentSelectedGameObject, Is.SameAs(back.gameObject));

            ExecuteEvents.Execute(back.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            yield return WaitForImmediateAnimation(back.animator, "Highlighted");
            ExecuteEvents.Execute(back.gameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
            Assert.That(panel.IsOpen, Is.False);

            panel.Show(() => Assert.Fail("Back must not confirm."));
            yield return WaitForImmediateAnimation(back.animator, "Normal");
            Assert.That(arrow.localScale.x, Is.Zero.Within(0.01f));
        }
        finally
        {
            if (panel.IsOpen)
                panel.Close();
            Object.Destroy(canvasObject);
            if (ownedEventSystem != null)
                Object.Destroy(ownedEventSystem);
        }
    }

    private static IEnumerator WaitForImmediateAnimation(Animator animator, string state)
    {
        // The pointer handler runs in LateUpdate and the Animator samples on
        // the following frame. A blend would still be running at this point.
        yield return null;
        yield return null;
        Assert.That(animator.IsInTransition(0), Is.False, $"Back blended into {state}.");
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(state), Is.True, state);
    }

    private static IEnumerator WaitForAnimation(Animator animator, string state)
    {
        float deadline = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).IsName(state))
                yield break;
            yield return null;
        }
        Assert.Fail($"Back button did not reach the {state} animation.");
    }

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
