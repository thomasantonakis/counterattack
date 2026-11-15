using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OnlineShootoutAuthoring
{
    [CliCommand("author_online_shootout","Author team-owned online shootout orders and confirmations")]
    public static string Prepare()
    {
        string previous=SceneManager.GetActiveScene().path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
            if(board.shootoutView!=null)return "Online shootout controls already authored";
            GameObject Box(string name,Transform parent,Vector2 size,Vector2 position)
            {
                var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=size;rect.anchoredPosition=position;return obj;
            }
            Text Label(string name,Transform parent,string value,Vector2 size,Vector2 position)
            {
                var obj=Box(name,parent,size,position);var text=obj.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=20;text.color=Color.white;text.raycastTarget=false;text.text=value;text.alignment=TextAnchor.UpperCenter;return text;
            }
            var panel=Box("Online shootout orders",board.sharedPausePanel.transform.parent,new Vector2(1120,760),new Vector2(0,-140));panel.AddComponent<Image>().color=new Color(.07f,.11f,.16f,.98f);
            panel.transform.SetSiblingIndex(board.sharedPausePanel.transform.GetSiblingIndex());var view=panel.AddComponent<OnlineShootoutView>();view.board=board;board.shootoutView=view;
            Label("Shootout title",panel.transform,"Penalty shootout",new Vector2(1000,50),new Vector2(0,-20)).fontSize=30;
            Label("Home heading",panel.transform,"Home takers",new Vector2(450,40),new Vector2(-270,-80));Label("Away heading",panel.transform,"Away takers",new Vector2(450,40),new Vector2(270,-80));
            view.homeOrder=Label("Home order",panel.transform,"",new Vector2(480,370),new Vector2(-270,-125));view.awayOrder=Label("Away order",panel.transform,"",new Vector2(480,370),new Vector2(270,-125));
            view.status=Label("Shootout status",panel.transform,"",new Vector2(1000,90),new Vector2(0,-500));
            var dropdown=UnityEngine.Object.Instantiate(board.menu.outgoingPlayer.gameObject,panel.transform);dropdown.name="Own shootout taker";((RectTransform)dropdown.transform).anchoredPosition=new Vector2(0,-595);
            view.ownPlayer=dropdown.GetComponent<Dropdown>();view.ownPlayer.onValueChanged=new Dropdown.DropdownEvent();view.ownPlayer.ClearOptions();
            Button Button(string name,string value,Vector2 position)
            {
                var obj=UnityEngine.Object.Instantiate(board.menu.resumeButton.gameObject,panel.transform);obj.name=name;var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(200,45);rect.anchoredPosition=position;
                obj.GetComponentInChildren<Text>(true).text=value;var button=obj.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();return button;
            }
            view.earlier=Button("Earlier taker","Move earlier",new Vector2(-240,-655));view.later=Button("Later taker","Move later",new Vector2(0,-655));view.confirm=Button("Confirm own order","Confirm my order",new Vector2(240,-655));
            view.pause=Button("Pause shootout","Pause",new Vector2(-430,-20));view.leave=Button("Leave shootout","Leave match",new Vector2(430,-20));
            panel.SetActive(false);EditorUtility.SetDirty(board);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored online shootout orders and both-manager confirmations";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
