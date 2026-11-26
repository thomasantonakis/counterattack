using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OnlineMatchConclusionAuthoring
{
    [CliCommand("author_online_match_conclusion","Author shared full-time result and safe Leave")]
    public static string Prepare()
    {
        string previous=SceneManager.GetActiveScene().path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
            if(board.conclusionView!=null)return "Online full-time result already authored";
            GameObject Box(string name,Transform parent,Vector2 size,Vector2 position)
            {
                var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=size;rect.anchoredPosition=position;return obj;
            }
            Text Label(string name,Transform parent,string value,Vector2 size,Vector2 position,int font=24)
            {
                var obj=Box(name,parent,size,position);var text=obj.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=font;text.color=Color.white;text.raycastTarget=false;text.text=value;text.alignment=TextAnchor.UpperCenter;return text;
            }
            var panel=Box("Online full-time result",board.sharedPausePanel.transform.parent,new Vector2(960,600),new Vector2(0,-180));panel.AddComponent<Image>().color=new Color(.07f,.11f,.16f,.98f);
            panel.transform.SetSiblingIndex(board.sharedPausePanel.transform.GetSiblingIndex());var view=panel.AddComponent<OnlineMatchConclusionView>();view.board=board;board.conclusionView=view;
            view.title=Label("Full-time title",panel.transform,"Full time",new Vector2(860,55),new Vector2(0,-25),32);
            view.homeName=Label("Home result identity",panel.transform,"Home",new Vector2(310,85),new Vector2(-300,-115));view.awayName=Label("Away result identity",panel.transform,"Away",new Vector2(310,85),new Vector2(300,-115));
            view.score=Label("Final score",panel.transform,"0 - 0",new Vector2(220,80),new Vector2(0,-105),44);
            view.result=Label("Match outcome",panel.transform,"",new Vector2(860,70),new Vector2(0,-210),28);
            view.summary=Label("Published match totals",panel.transform,"",new Vector2(860,180),new Vector2(0,-310));
            var buttonObject=UnityEngine.Object.Instantiate(board.menu.leaveButton.gameObject,panel.transform);buttonObject.name="Leave completed match";
            var buttonRect=(RectTransform)buttonObject.transform;buttonRect.anchorMin=buttonRect.anchorMax=new Vector2(.5f,1);buttonRect.pivot=new Vector2(.5f,1);buttonRect.sizeDelta=new Vector2(260,48);buttonRect.anchoredPosition=new Vector2(0,-525);
            buttonObject.GetComponentInChildren<Text>(true).text="Return to main menu";view.leave=buttonObject.GetComponent<Button>();view.leave.onClick=new Button.ButtonClickedEvent();
            panel.SetActive(false);EditorUtility.SetDirty(board);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored online full-time summary and safe return";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
