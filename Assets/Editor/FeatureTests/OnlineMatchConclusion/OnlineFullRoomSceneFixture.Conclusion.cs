using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunConclusion(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,int margin,int halves,bool leaveAsGuest)
    {
        var match=board.match;match.currentHalf=halves;match.gameData.gameSettings.numberOfHalfs=Mathf.Min(halves,2);match.gameData.gameSettings.tiebreaker=halves>2?"Extra Time":"None";
        if(halves>2)match.gameData.extraTimeOpeningSetup=match.gameData.openingSetup;
        match.gameData.stats.homeTeamStats.totalGoals=2+Mathf.Max(margin,0);match.gameData.stats.awayTeamStats.totalGoals=2+Mathf.Max(-margin,0);
        match.gameData.stats.homeTeamStats.totalShots=7;match.gameData.stats.awayTeamStats.totalShots=9;match.gameData.stats.homeTeamStats.totalPassesCompleted=12;match.gameData.stats.awayTeamStats.totalPassesCompleted=14;
        var authority=new OnlineGroundPassAuthority(match,protocol);var prior=pause.Capture(authority);
        var old=new OnlineMatchCommand(prior.matchId,1,prior.revision,prior.decisionId,OnlineGroundPassAuthority.KickoffConfirmAction);
        typeof(MatchManager).GetMethod("BeginHalfEndFlow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(match,null);
        Assert.That(board.liveScoreboard.gameObject.activeSelf,Is.True,"The offline full-time panel must not hide the online scoreboard or relocate Stats");
        var state=pause.Capture(authority);Assert.That(state.matchComplete,Is.True);Assert.That(state.stage,Is.EqualTo("match.complete"));Assert.That(state.legalActions,Is.Empty);
        Assert.That(authority.Submit("h",old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));Assert.That(OnlineManualSessionPolicy.CanFinish(state,true),Is.True);
        board.Display(state,"Completed match");Assert.That(board.conclusionView.gameObject.activeSelf,Is.True);Assert.That(board.conclusionView.score.text,Is.EqualTo(state.homeStats.totalGoals+" - "+state.awayStats.totalGoals));
        Assert.That(board.conclusionView.summary.text,Does.Contain("Shots: 7 - 9"));Assert.That(board.conclusionView.summary.text,Does.Contain("Completed passes: 12 - 14"));
        string expected=margin==0?"Match drawn":(margin>0?match.gameData.gameSettings.homeTeamName:match.gameData.gameSettings.awayTeamName)+" win";
        Assert.That(board.conclusionView.result.text,Is.EqualTo(expected));
        var reader=new OnlineGroundPassWire();OnlineGroundPassMessage encoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=state.matchId,state=state}))encoded=reader.Receive(packet,packet.Length);
        pause.Dispose();pause=null;
        if(!leaveAsGuest)
        {
            string path=Path.Combine(directory,"authority.json"),before=File.ReadAllText(path);board.conclusionView.leave.onClick.Invoke();yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));Assert.That(OnlineRoomSession.Current,Is.Null);Assert.That(File.ReadAllText(path),Is.EqualTo(before));session=null;
        }
        else{yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;}
        MatchManager.Instance=null;yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica(state.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(encoded.state),Is.True);board.Display(replica.State,"Guest full time");Assert.That(board.conclusionView.gameObject.activeSelf,Is.True);
        Assert.That(board.conclusionView.result.text,Is.EqualTo(expected));Assert.That(board.conclusionView.score.text,Is.EqualTo(state.homeStats.totalGoals+" - "+state.awayStats.totalGoals));
        Assert.That(board.authorityRoot.activeSelf,Is.False);Assert.That(board.liveScoreboard.gameObject.activeSelf,Is.True);Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);
        if(leaveAsGuest)
        {
            string path=Path.Combine(directory,"replica.json"),before=File.ReadAllText(path);board.conclusionView.leave.onClick.Invoke();yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));Assert.That(OnlineRoomSession.Current,Is.Null);Assert.That(File.ReadAllText(path),Is.EqualTo(before));session=null;
        }
    }
}
