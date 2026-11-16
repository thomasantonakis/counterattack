using System.Collections;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunContextKeyFocus(OnlineGroundPassBoard board)
    {
        var events=EventSystem.current;Assert.That(events,Is.Not.Null,"Both host and guest require an active authored EventSystem");
        Assert.That(events.isActiveAndEnabled,Is.True);var previous=events.currentSelectedGameObject;int requests=0;
        System.Action<OnlineGroundPassMessage> observe=_=>requests++;board.controls.Requested+=observe;board.boardInput.samplePhysicalInput=false;
        board.controls.Configure(board.localTeam);board.controls.Display(new OnlineGroundPassState{matchId="keyboard-focus",owner=board.localTeam.ToString(),stage="action",legalActions=new[]{"ground.start"}});
        try
        {
            events.SetSelectedGameObject(board.controls.targetX.gameObject);Assert.That(OnlineBoardInput.KeyboardOwnedByUI(KeyCode.P),Is.True);Assert.That(OnlineBoardInput.KeyboardOwnedByUI(KeyCode.Return),Is.True);
            events.SetSelectedGameObject(board.controls.legalCommands.gameObject);Assert.That(OnlineBoardInput.KeyboardOwnedByUI(KeyCode.C),Is.True);
            Assert.That(board.controls.legalCommands.isActiveAndEnabled,Is.True,"Command dropdown is active");
            Assert.That(board.controls.legalCommands.IsInteractable(),Is.True,"Own published command enables the dropdown");
            board.controls.legalCommands.Show();
            Assert.That(board.controls.legalCommands.transform.Find("Dropdown List"),Is.Not.Null,"Authored dropdown template opens");
            Assert.That(events.currentSelectedGameObject?.GetComponentInParent<Toggle>(),Is.Not.Null,"Real dropdown item owns keyboard input");
            Assert.That(OnlineBoardInput.KeyboardOwnedByUI(KeyCode.P),Is.True);board.controls.legalCommands.Hide();
            float deadline=Time.realtimeSinceStartup+2;while(board.controls.legalCommands.transform.Find("Dropdown List") != null && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(board.controls.legalCommands.transform.Find("Dropdown List"),Is.Null);
            events.SetSelectedGameObject(board.controls.commandButton.gameObject);Assert.That(OnlineBoardInput.KeyboardOwnedByUI(KeyCode.Return),Is.True);Assert.That(OnlineBoardInput.KeyboardOwnedByUI(KeyCode.R),Is.False);
            events.SetSelectedGameObject(null);Assert.That(OnlineBoardInput.KeyboardOwnedByUI(KeyCode.P),Is.False);Assert.That(requests,Is.Zero);
            Assert.That(board.authorityRoot.activeSelf,Is.EqualTo(!OnlineRoomSession.IsGuest));
        }
        finally{board.controls.legalCommands.Hide();board.controls.Requested-=observe;if(events!=null)events.SetSelectedGameObject(previous);}
    }
}
