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
public static class OnlineDraftPoolAuthoring
{
    [CliCommand("author_online_draft_pool","Author local pool browsing controls in the online Draft scene")]
    public static string Prepare()
    {
        var original=SceneManager.GetActiveScene();string previous=original.path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Draft.unity",OpenSceneMode.Single);var view=UnityEngine.Object.FindAnyObjectByType<OnlineDraftView>();
            if(view.previousPageButton!=null)throw new InvalidOperationException("Pool controls already authored");
            Button Button(string name,string label,float x)
            {
                var button=UnityEngine.Object.Instantiate(view.actionButton,view.actionButton.transform.parent);button.name=name;button.onClick=new Button.ButtonClickedEvent();
                button.GetComponentInChildren<TMP_Text>(true).text=label;
                var rect=button.GetComponent<RectTransform>();rect.anchoredPosition+=new Vector2(x,90);rect.sizeDelta=new Vector2(200,rect.sizeDelta.y);
                button.gameObject.SetActive(false);return button;
            }
            view.previousPageButton=Button("OnlinePreviousPage","Previous",-110);view.nextPageButton=Button("OnlineNextPage","Next",110);
            view.onlineRoots=view.onlineRoots.Concat(new[]{view.previousPageButton.gameObject,view.nextPageButton.gameObject}).ToArray();
            EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored local pool pagination";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
