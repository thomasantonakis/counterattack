using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public sealed partial class OnlineRoomActionSceneFixture
{
    public IEnumerator RunRoomRosterPresentation(bool guest,bool restore)
    {
        host=new OnlineRoomSession("room-roster",false);
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(OnlineRoomProofAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));
        board=Object.FindAnyObjectByType<OnlineGroundPassBoard>();board.InitializeAuthority();
        var version=new OnlineCompatibility("roster","rules","catalog");
        var protocol=new OnlineMatchProtocol("room-roster","home","away","away",version);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        var initial=authority.Capture();var token=board.tokens.First(t=>!t.isHomeTeam && !t.IsGoalKeeper);
        int index=System.Array.IndexOf(board.tokens,token);token.MarkSentOff();
        var removed=authority.Capture();Assert.That(removed.tokens.Single(t=>t.id==OnlineRollPublication.ActorId(token)).onBoard,Is.False);
        if(guest){host.Dispose();host=new OnlineRoomSession("room-roster-guest",true);board.authorityRoot.SetActive(false);}
        removed.ballOnBoard=false;
        var actualPosition=token.transform.position;
        board.Display(initial,guest?"Guest":"Host");Assert.That(board.displayedTokens[index].gameObject.activeSelf,Is.True);
        board.Display(removed,guest?"Guest":"Host");
        Assert.That(board.displayedTokens[index].gameObject.activeSelf,Is.False,"sent-off replica must leave the board");
        Assert.That(board.displayedBall.gameObject.activeSelf,Is.False);
        board.DisplayPresentation(new OnlineGroundPassPresentation{players=new[]{new OnlinePlayerPresentation{id=OnlineRollPublication.ActorId(token),x=3,y=.5f,z=4}}});
        Assert.That(board.displayedTokens[index].gameObject.activeSelf,Is.False,"position frames cannot resurrect removed roster entries");
        Assert.That(board.displayedBall.gameObject.activeSelf,Is.False);
        if(restore){board.Display(initial,guest?"Guest":"Host");Assert.That(board.displayedTokens[index].gameObject.activeSelf,Is.True);Assert.That(board.displayedBall.gameObject.activeSelf,Is.True);}
        Assert.That(token.isSentOff,Is.True);Assert.That(token.transform.position,Is.EqualTo(actualPosition));
        if(guest)Assert.That(board.authorityRoot.activeSelf,Is.False);
    }
}
