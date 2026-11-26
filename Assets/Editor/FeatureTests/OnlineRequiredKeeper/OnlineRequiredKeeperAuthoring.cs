using System;
using CounterAttack.Online;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OnlineRequiredKeeperAuthoring
{
    [CliCommand("author_online_required_keeper","Author emergency goalkeeper nomination in the online match menu")]
    public static string Prepare()
    {
        string previous=SceneManager.GetActiveScene().path;
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty || SceneManager.sceneCount!=1)throw new InvalidOperationException("Expected one clean stopped scene");
        try
        {
            var scene=EditorSceneManager.OpenScene(OnlineFullRoomAuthoring.ScenePath,OpenSceneMode.Single);
            var board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();var menu=board.menu;if(menu.nominationPanel!=null)return "Emergency keeper controls already authored";
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
            menu.nominationButton=Button("Choose emergency goalkeeper",board.sharedPausePanel.transform,"Choose goalkeeper",new Vector2(-145,-410));
            var panel=Box("Emergency goalkeeper choice",board.sharedPausePanel.transform,new Vector2(640,480),Vector2.zero);panel.AddComponent<Image>().color=new Color(.07f,.11f,.16f,1);menu.nominationPanel=panel;
            var title=Label("Emergency keeper title",panel.transform,"Your emergency goalkeeper",new Vector2(580,45),new Vector2(0,-25));title.fontSize=28;
            menu.nominationReason=Label("Keeper requirement",panel.transform,"Choose a playing outfield player.",new Vector2(580,100),new Vector2(0,-85));
            var dropdownObject=UnityEngine.Object.Instantiate(menu.outgoingPlayer.gameObject,panel.transform);dropdownObject.name="Eligible own outfield players";var dropdownRect=(RectTransform)dropdownObject.transform;dropdownRect.anchoredPosition=new Vector2(0,-200);
            menu.emergencyKeeper=dropdownObject.GetComponent<Dropdown>();menu.emergencyKeeper.onValueChanged=new Dropdown.DropdownEvent();menu.emergencyKeeper.ClearOptions();
            Label("Emergency keeper ratings",panel.transform,"This uses no substitution. Saving, handling, aerial, heading and shooting become 0.",new Vector2(580,105),new Vector2(0,-275));
            menu.nominationBackButton=Button("Back from keeper choice",panel.transform,"Back to pause",new Vector2(-145,-410));menu.nominationCommitButton=Button("Confirm emergency keeper",panel.transform,"Confirm goalkeeper",new Vector2(145,-410));
            panel.SetActive(false);menu.nominationButton.gameObject.SetActive(false);EditorUtility.SetDirty(menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Authored emergency goalkeeper selection and confirmation";
        }
        finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
    }
}
