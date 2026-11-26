using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnlineSharedPauseAuthoring
{
    [CliCommand("author_online_shared_pause", "Author the isolated proof's requester/passive shared-pause panel")]
    public static string Prepare()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Expected one clean stopped Editor scene");
        var previous = SceneManager.GetActiveScene();
        if (previous.isDirty || string.IsNullOrEmpty(previous.path)) throw new InvalidOperationException("Save the current scene first");
        var path = previous.path;
        try
        {
            var scene = EditorSceneManager.OpenScene(OnlineGroundPassSceneAuthoring.ScenePath, OpenSceneMode.Single);
            var board = UnityEngine.Object.FindFirstObjectByType<OnlineGroundPassBoard>();
            if (board.sharedPausePanel == null)
            {
                var canvas = board.statusText.GetComponentInParent<Canvas>();
                var panel = new GameObject("Shared pause", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(canvas.transform, false); panel.GetComponent<Image>().color = new Color(.03f,.04f,.06f,.95f);
                var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
                rect.sizeDelta = new Vector2(440,120);
                GameObject Label(string name, string message)
                {
                    var obj = new GameObject(name, typeof(RectTransform), typeof(Text)); obj.transform.SetParent(panel.transform, false);
                    var label = obj.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.fontSize = 22; label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; label.text = message;
                    var bounds = obj.GetComponent<RectTransform>(); bounds.anchorMin = Vector2.zero; bounds.anchorMax = Vector2.one;
                    bounds.offsetMin = new Vector2(12,12); bounds.offsetMax = new Vector2(-12,-12);
                    return obj;
                }
                board.sharedPausePanel = panel;
                board.requesterPauseMessage = Label("Requester pause", "You paused the match");
                board.passivePauseMessage = Label("Passive pause", "Game paused");
                board.requesterPauseMessage.SetActive(false); board.passivePauseMessage.SetActive(false); panel.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save authored pause panel");
            }
        }
        finally { EditorSceneManager.OpenScene(path, OpenSceneMode.Single); }
        return OnlineGroundPassSceneAuthoring.ScenePath;
    }
}
