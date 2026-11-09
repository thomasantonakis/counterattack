using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class RulesViewerPlayModeFixture
{
    private Scene scene, original;
    private RulesViewer viewer;
    private GameObject matchRoot;
    private MatchManager match, previousMatch;
    private float timeScale;
    private EventSystem previousEventSystem;
    private readonly Dictionary<FieldInfo, object> session = new();

    public IEnumerator Run(string scenario)
    {
        original = SceneManager.GetActiveScene(); previousMatch = MatchManager.Instance; timeScale = Time.timeScale;
        foreach (var field in typeof(RulesViewer).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if (field.FieldType == typeof(List<float>)) session[field] = new List<float>((List<float>)field.GetValue(null));
            else if (field.FieldType == typeof(string) || field.FieldType == typeof(float) || field.FieldType == typeof(int)) session[field] = field.GetValue(null);
        }
        scene = SceneManager.CreateScene("Isolated rules viewer"); SceneManager.SetActiveScene(scene);
        previousEventSystem = EventSystem.current;
        EventSystem.current = new GameObject("Isolated rules events", typeof(EventSystem)).GetComponent<EventSystem>();
        MatchManager.Instance = null;
        if (scenario.StartsWith("room"))
        {
            matchRoot = new GameObject("Isolated match"); matchRoot.SetActive(false);
            match = matchRoot.AddComponent<MatchManager>(); MatchManager.Instance = match;
            match.SetPauseMenuOpen(scenario == "room-paused");
        }
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RulesViewerEditorTools.PrefabPath));
        viewer = root.GetComponent<RulesViewer>();
        RulesViewer.OpenRules();
        yield return null;
        Assert.That(viewer.surface.activeSelf, Is.True);
        Assert.That(RulesViewer.IsOpen, Is.True);
        Assert.That(viewer.article.content.rect.height, Is.GreaterThan(viewer.article.viewport.rect.height));
        if (match != null)
        {
            Assert.That(match.IsGameplayInputBlocked, Is.EqualTo(scenario == "room-paused"));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
            Assert.That(RoomInputOwnership.BlocksCamera && RoomInputOwnership.ReservePauseEscape, Is.True);
            var key = new KeyPressData(KeyCode.Space, false, false, false);
            Assert.That(RoomInputOwnership.ConsumeKey(key, true, null), Is.True);
        }
        int count = root.GetComponentsInChildren<Transform>(true).Length;
        viewer.search.text = "snapshot";
        yield return null;
        Assert.That(viewer.matchCount.text, Does.Contain("matches"));
        Assert.That(viewer.matchCount.text, Is.Not.EqualTo("No matches"));
        Assert.That(viewer.resultsScroll.gameObject.activeSelf, Is.True);
        viewer.search.ActivateInputField(); yield return null;
        ExecuteEvents.Execute(viewer.search.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        yield return null;
        Assert.That(viewer.matchCount.text, Does.StartWith("1 /"));
        ExecuteEvents.Execute(viewer.search.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        yield return null;
        Assert.That(viewer.matchCount.text, Does.StartWith("2 /"), "Each Enter submission advances exactly once.");
        viewer.previousButton.onClick.Invoke(); yield return null;
        Assert.That(viewer.matchCount.text, Does.StartWith("1 /"));
        viewer.previousButton.onClick.Invoke(); yield return null;
        Assert.That(viewer.matchCount.text, Does.Not.StartWith("1 /"), "Previous wraps to the final occurrence.");
        viewer.search.ActivateInputField(); yield return null;
        viewer.search.ProcessEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape });
        viewer.search.DeactivateInputField(); yield return null;
        Assert.That(viewer.search.text, Is.Empty, "Escape in the search field clears the query.");
        Assert.That(RulesViewer.IsOpen, Is.True, "Clearing search with Escape must leave Rules open.");
        Assert.That(viewer.resultsScroll.gameObject.activeSelf, Is.False);
        Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(count), "Search must reuse authored rows.");
        viewer.article.verticalNormalizedPosition = 1;
        viewer.Navigate("shooting"); yield return null;
        float shooting = viewer.article.verticalNormalizedPosition;
        Assert.That(shooting, Is.LessThan(.99f));
        Click(viewer.backButton); yield return null;
        Assert.That(viewer.article.verticalNormalizedPosition, Is.EqualTo(1).Within(.01));
        Click(viewer.forwardButton); yield return null;
        Assert.That(viewer.article.verticalNormalizedPosition, Is.EqualTo(shooting).Within(.01));
        viewer.article.verticalNormalizedPosition = .45f; yield return null;
        Click(viewer.backButton); yield return null;
        Assert.That(viewer.article.verticalNormalizedPosition, Is.EqualTo(shooting).Within(.01), "Back returns from a manual scroll.");
        Click(viewer.forwardButton); yield return null;
        Assert.That(viewer.article.verticalNormalizedPosition, Is.EqualTo(.45f).Within(.01), "Forward restores the manually read passage.");
        var linkedText = viewer.entries.First(e => e.section == "Tackling vs Dribbling" && e.text.text.Contains("<link=\"loose_ball\">"));
        linkedText.text.ForceMeshUpdate();
        Assert.That(linkedText.text.textInfo.linkCount, Is.GreaterThan(0));
        viewer.Navigate("movement_phase#tackling-vs-dribbling"); yield return null;
        linkedText.text.ForceMeshUpdate();
        int linkIndex = Array.FindIndex(linkedText.text.textInfo.linkInfo, l => l.GetLinkID() == "loose_ball");
        var linkInfo = linkedText.text.textInfo.linkInfo[linkIndex];
        var firstChar = linkedText.text.textInfo.characterInfo[linkInfo.linkTextfirstCharacterIndex];
        Vector3 local = (firstChar.bottomLeft + firstChar.topRight) * .5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, linkedText.text.transform.TransformPoint(local));
        viewer.OnPointerClick(new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left });
        yield return null;
        Assert.That(viewer.article.verticalNormalizedPosition, Is.LessThan(shooting));
        viewer.search.text = "not-a-rule-927385"; yield return null;
        Assert.That(viewer.matchCount.text, Is.EqualTo("No matches"));
        viewer.search.text = "goalkeeper"; yield return null;
        float position = viewer.article.verticalNormalizedPosition;
        viewer.Close();
        Assert.That(RulesViewer.IsOpen, Is.False);
        if (match != null)
        {
            Assert.That(match.IsPauseMenuOpen, Is.EqualTo(scenario == "room-paused"));
            Assert.That(Time.timeScale, Is.EqualTo(timeScale));
        }
        viewer.Open(); yield return null;
        Assert.That(viewer.search.text, Is.EqualTo("goalkeeper"));
        Assert.That(viewer.article.verticalNormalizedPosition, Is.EqualTo(position).Within(.01));
        viewer.clearButton.onClick.Invoke(); viewer.Navigate("index"); yield return null;
        Capture(root, scenario, 1600, 900);
        viewer.search.text = "snapshot"; viewer.nextButton.onClick.Invoke(); yield return null;
        Capture(root, scenario + "-search", 1280, 720);
        Capture(root, scenario + "-compact", 1024, 768);
        viewer.Close();
    }

    private static void Click(UnityEngine.UI.Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0), button.name + " must receive pointer hits.");
        GameObject target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Assert.That(target, Is.EqualTo(button.gameObject), button.name + " is obstructed by " + hits[0].gameObject.name);
        Assert.That(button.interactable, Is.True, button.name + " should be enabled.");
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static void Capture(GameObject root, string name, int width, int height)
    {
        // Render the isolated canvas with a temporary camera; never touch production cameras.
        var cameraObject = new GameObject("Rulebook capture camera");
        var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        var canvas = root.GetComponent<Canvas>(); var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera;
        var previousTarget = RenderTexture.active; var target = new RenderTexture(width,height,24);
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            texture = new Texture2D(width,height,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
            Directory.CreateDirectory("TestResults/GameplayRegression/Runs/RulesViewerVisuals");
            File.WriteAllBytes("TestResults/GameplayRegression/Runs/RulesViewerVisuals/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = previousMode; canvas.worldCamera = previousCamera;
            RenderTexture.active = previousTarget; camera.targetTexture = null;
            if (texture != null) Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target); Object.DestroyImmediate(cameraObject);
        }
    }

    public IEnumerator Cleanup()
    {
        if (viewer != null) viewer.Close();
        MatchManager.Instance = previousMatch; Time.timeScale = timeScale;
        if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        if (previousEventSystem != null && previousEventSystem.isActiveAndEnabled) EventSystem.current = previousEventSystem;
        foreach (var pair in session)
        {
            if (pair.Value is List<float> history)
            {
                var target = (List<float>)pair.Key.GetValue(null); target.Clear(); target.AddRange(history);
            }
            else pair.Key.SetValue(null,pair.Value);
        }
    }
}
