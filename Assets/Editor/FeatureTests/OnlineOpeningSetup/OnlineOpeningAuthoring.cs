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
public static class OnlineOpeningAuthoring
{
    [CliCommand("author_online_opening_choices","Author team-owned kickoff/direction choice buttons in online Draft")]
    public static string Prepare()
    {
        var original=SceneManager.GetActiveScene();string previous=original.path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Draft.unity",OpenSceneMode.Single);var view=UnityEngine.Object.FindAnyObjectByType<OnlineDraftView>();
            if(view.kickoffChoiceButton!=null)throw new InvalidOperationException("Opening choices already authored");
            Button Choice(string label,float x)
            {
                var button=UnityEngine.Object.Instantiate(view.actionButton,view.actionButton.transform.parent);button.name="OnlineChoose"+label;button.onClick=new Button.ButtonClickedEvent();
                button.GetComponentInChildren<TMP_Text>(true).text=label;
                var rect=button.GetComponent<RectTransform>();rect.anchoredPosition+=new Vector2(x,90);rect.sizeDelta=new Vector2(350,rect.sizeDelta.y);button.gameObject.SetActive(false);return button;
            }
            view.kickoffChoiceButton=Choice("Kickoff",-370);view.leftChoiceButton=Choice("Attack left to right",0);view.rightChoiceButton=Choice("Attack right to left",370);
            view.onlineRoots=view.onlineRoots.Concat(new[]{view.kickoffChoiceButton.gameObject,view.leftChoiceButton.gameObject,view.rightChoiceButton.gameObject}).ToArray();
            EditorUtility.SetDirty(view);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored opening kickoff/direction choices";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
