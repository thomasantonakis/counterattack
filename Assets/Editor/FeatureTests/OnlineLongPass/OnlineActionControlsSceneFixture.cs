using System;
using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OnlineActionControlsSceneFixture
{
    private OnlineGroundPassBoard board;
    public IEnumerator Run()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=UnityEngine.Object.FindAnyObjectByType<OnlineGroundPassBoard>();
        Assert.That(board.authorityRoot.activeSelf,Is.False);
        var controls=board.controls;Assert.That(controls.commandButton,Is.Not.Null);Assert.That(controls.legalCommands,Is.Not.Null);
        var state=new OnlineGroundPassState{matchId="actions",revision=3,publicationSequence=1,decisionId="long-1",owner="Home",stage="action",
            legalActions=new[]{OnlineGroundPassAuthority.StartAction,OnlineGroundPassAuthority.LongStartAction},tokens=Array.Empty<OnlineGroundPassTokenState>()};
        OnlineGroundPassMessage command=null;int count=0;controls.Requested+=value=>{command=value;count++;};
        controls.Configure(OnlineTeam.Home);controls.Display(state);controls.legalCommands.value=1;
        controls.commandButton.onClick.Invoke();
        Assert.That(command.actionId,Is.EqualTo(OnlineGroundPassAuthority.LongStartAction));Assert.That(command.revision,Is.EqualTo(3));
        Assert.That(controls.commandButton.interactable,Is.False);
        controls.commandButton.onClick.Invoke();Assert.That(count,Is.EqualTo(1),"pending commands cannot be emitted twice");
        controls.Reply(new OnlineGroundPassMessage{status="Accepted"});
        state.stage="long.keeper";state.owner="Away";state.legalActions=new[]{OnlineGroundPassAuthority.LongKeeperDeclineAction};controls.Display(state);
        controls.commandButton.onClick.Invoke();Assert.That(count,Is.EqualTo(1),"guest cannot answer the defending team's choice");
        controls.Configure(OnlineTeam.Away);controls.Display(state);controls.commandButton.onClick.Invoke();
        Assert.That(command.actionId,Is.EqualTo(OnlineGroundPassAuthority.LongKeeperDeclineAction));Assert.That(count,Is.EqualTo(2));
        controls.Reply(new OnlineGroundPassMessage{status="Accepted"});controls.SetConnected(false);
        controls.commandButton.onClick.Invoke();Assert.That(count,Is.EqualTo(2));
        Assert.That(board.match.TimingRollSequence,Is.Zero);Assert.That(board.authorityRoot.activeSelf,Is.False);
    }
    public IEnumerator Cleanup()
    {if(board!=null) UnityEngine.Object.DestroyImmediate(board.gameObject);yield break;}
}
