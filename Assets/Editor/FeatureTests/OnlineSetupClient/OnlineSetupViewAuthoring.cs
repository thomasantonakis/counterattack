using System;
using System.Linq;
using CounterAttack.Online;
using TMPro;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OnlineSetupViewAuthoring
{
    [CliCommand("author_online_setup_view","Author the online agreement view in Create New Game without changing the loaded Room")]
    public static string Prepare()
    {
        var original=SceneManager.GetActiveScene();string previous=original.path;
        if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CreateNewGameScene.unity",OpenSceneMode.Single);
            var setup=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CreateNewGameManager>(true)).Single();
            var view=setup.GetComponent<OnlineSetupView>()??setup.gameObject.AddComponent<OnlineSetupView>();view.setup=setup;
            if(view.status==null)
            {
                var canvas=setup.createGameButton.GetComponentInParent<Canvas>();
                var status=new GameObject("OnlineSetupStatus",typeof(RectTransform),typeof(TextMeshProUGUI));status.transform.SetParent(canvas.transform,false);
                var text=status.GetComponent<TextMeshProUGUI>();text.font=setup.kitValidationText!=null?setup.kitValidationText.font:setup.homeTeamInputField.textComponent.font;
                text.fontSize=22;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;
                text.text="Online setup: waiting for a session";text.color=new Color(.82f,.88f,.94f);
                var rect=status.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);
                rect.anchoredPosition=new Vector2(0,-58);rect.sizeDelta=new Vector2(1000,36);view.status=text;
                // Existing offline screens keep the authored online status hidden.
                status.SetActive(false);
            }
            var header=setup.createGameButton.GetComponentInParent<Canvas>().transform.Find("Header");
            if(header==null)throw new InvalidOperationException("Create New Game header is missing");
            view.status.transform.SetParent(header,false);
            var statusRect=view.status.GetComponent<RectTransform>();statusRect.anchorMin=new Vector2(0,1);statusRect.anchorMax=new Vector2(1,1);
            statusRect.pivot=new Vector2(.5f,1);statusRect.anchoredPosition=Vector2.zero;statusRect.sizeDelta=new Vector2(0,36);
            var layout=view.status.GetComponent<LayoutElement>()??view.status.gameObject.AddComponent<LayoutElement>();layout.minHeight=36;layout.preferredHeight=36;
            EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored OnlineSetupView; online status participates in existing Header layout";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
