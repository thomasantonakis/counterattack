using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnlineManualControlsAuthoring
{
    [CliCommand("author_online_target_choices", "Author the legal target dropdown in the owned controls and shared Room prefab")]
    public static string Targets()
    {
        var previous = SceneManager.GetActiveScene(); var path = previous.path;
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || previous.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene = EditorSceneManager.OpenScene(OnlineGroundPassSceneAuthoring.ScenePath, OpenSceneMode.Single);
            var board = UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>(); var controls = board.controls;
            if (controls.legalTargets == null)
            {
                var obj = DefaultControls.CreateDropdown(new DefaultControls.Resources()); obj.name = "Authoritative legal targets";
                obj.transform.SetParent(controls.transform, false);
                var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(0,80); rect.sizeDelta = new Vector2(570,28);
                foreach (var text in obj.GetComponentsInChildren<Text>(true)) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                controls.legalTargets = obj.GetComponent<Dropdown>(); controls.legalTargets.ClearOptions();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save legal target control");
            }
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(controls.gameObject,"Assets/Settings/OnlineGroundPassProof/ManualControls.prefab");
            return scene.path;
        }
        finally { EditorSceneManager.OpenScene(path,OpenSceneMode.Single); }
    }
    [CliCommand("author_online_manual_controls", "Author semantic manual controls in the owned online proof scene")]
    public static string Prepare()
    {
        var previous = SceneManager.GetActiveScene();
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1 || previous.isDirty)
            throw new InvalidOperationException("Expected one clean stopped scene");
        var previousPath = previous.path;
        try
        {
            var scene = EditorSceneManager.OpenScene(OnlineGroundPassSceneAuthoring.ScenePath, OpenSceneMode.Single);
            var board = UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
            if (board.controls != null) return scene.path;
            var canvas = board.statusText.GetComponentInParent<Canvas>();
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("Online input events", typeof(EventSystem), typeof(StandaloneInputModule));
            var panel = new GameObject("Manual semantic commands", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            var bounds = panel.GetComponent<RectTransform>(); bounds.anchorMin = bounds.anchorMax = new Vector2(0,0);
            bounds.pivot = Vector2.zero; bounds.anchoredPosition = new Vector2(12,12); bounds.sizeDelta = new Vector2(600,110);
            var controls = panel.AddComponent<OnlineGroundPassControls>(); board.controls = controls;
            Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
                var text = obj.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 16; text.text = value; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter;
                var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.anchoredPosition = position; rect.sizeDelta = size; return text;
            }
            Button Button(string name, int index)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(panel.transform, false);
                var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(index * 115,0); rect.sizeDelta = new Vector2(110,32);
                obj.GetComponent<Image>().color = new Color(.12f,.18f,.24f,.95f);
                Label(obj.transform, "Label", name, Vector2.zero, rect.sizeDelta);
                return obj.GetComponent<Button>();
            }
            InputField Input(string name, string value, float x)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField)); obj.transform.SetParent(panel.transform, false);
                var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(x,40); rect.sizeDelta = new Vector2(60,28);
                obj.GetComponent<Image>().color = new Color(.12f,.18f,.24f,.95f);
                var input = obj.GetComponent<InputField>(); input.textComponent = Label(obj.transform, "Value", value, Vector2.zero, rect.sizeDelta);
                input.contentType = InputField.ContentType.IntegerNumber; input.text = value; return input;
            }
            controls.startButton = Button("Start pass",0); controls.targetButton = Button("Preview/confirm",1);
            controls.rollButton = Button("Defender roll",2); controls.pauseButton = Button("Pause",3); controls.resumeButton = Button("Resume",4);
            controls.targetX = Input("Target X", "0", 0); controls.targetZ = Input("Target Z", "6", 70);
            controls.commandStatus = Label(panel.transform, "Command status", "Connect to submit commands", new Vector2(150,40), new Vector2(420,28));
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save controls");
            return scene.path;
        }
        finally { EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single); }
    }
}
