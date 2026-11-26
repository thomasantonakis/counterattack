using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OnlineSubstitutionAuthoring
{
    [CliCommand("author_online_substitutions","Author own-team substitution controls in the full online Room menu")]
    public static string Prepare()
    {
        string previous=SceneManager.GetActiveScene().path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);
            var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();var menu=board.menu;if(menu.substitutionPanel!=null)return "Substitution controls already authored";
            GameObject Box(string name,Transform parent,Vector2 size,Vector2 position)
            {
                var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=size;rect.anchoredPosition=position;return obj;
            }
            Text Label(string name,Transform parent,string value,Vector2 size,Vector2 position)
            {
                var obj=Box(name,parent,size,position);var text=obj.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=20;text.color=Color.white;text.raycastTarget=false;text.text=value;text.alignment=TextAnchor.UpperCenter;return text;
            }
            Button Button(string name,Transform parent,string value,Vector2 position)
            {
                var obj=UnityEngine.Object.Instantiate(menu.resumeButton.gameObject,parent);obj.name=name;var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(240,48);rect.anchoredPosition=position;
                obj.GetComponentInChildren<Text>(true).text=value;var button=obj.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();return button;
            }
            Dropdown Dropdown(string name,Transform parent,Vector2 position)
            {
                var obj=UnityEngine.Object.Instantiate(board.controls.legalCommands.gameObject,parent);obj.name=name;obj.SetActive(true);var rect=(RectTransform)obj.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.sizeDelta=new Vector2(520,44);rect.anchoredPosition=position;
                var dropdown=obj.GetComponent<Dropdown>();dropdown.onValueChanged=new Dropdown.DropdownEvent();dropdown.ClearOptions();return dropdown;
            }
            menu.substitutionButton=Button("Open own substitutions",board.sharedPausePanel.transform,"Substitutions",new Vector2(-145,-410));
            menu.resumeButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(145,-410);
            var panel=Box("Online substitutions",board.sharedPausePanel.transform,new Vector2(640,480),Vector2.zero);panel.AddComponent<Image>().color=new Color(.07f,.11f,.16f,1);
            menu.substitutionPanel=panel;var title=Label("Substitutions title",panel.transform,"Your substitutions",new Vector2(580,45),new Vector2(0,-25));title.fontSize=28;
            Label("Player off caption",panel.transform,"Player off",new Vector2(580,35),new Vector2(0,-90));menu.outgoingPlayer=Dropdown("Own playing players",panel.transform,new Vector2(0,-130));
            Label("Player on caption",panel.transform,"Player on",new Vector2(580,35),new Vector2(0,-195));menu.incomingPlayer=Dropdown("Own eligible bench",panel.transform,new Vector2(0,-235));
            menu.substitutionStatus=Label("Substitution status",panel.transform,"Choose a playing player and a substitute.",new Vector2(580,75),new Vector2(0,-315));
            menu.substitutionBackButton=Button("Back to shared pause",panel.transform,"Back to pause",new Vector2(-145,-410));menu.substitutionCommitButton=Button("Confirm own substitution",panel.transform,"Confirm substitution",new Vector2(145,-410));
            panel.SetActive(false);EditorUtility.SetDirty(menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored own-team substitution selectors and confirmation";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
