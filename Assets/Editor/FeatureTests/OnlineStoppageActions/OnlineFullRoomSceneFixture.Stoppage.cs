using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunStoppage(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool home,bool extra,int face,bool automatic)
    {
        var match=board.match;match.currentHalf=extra?3:1;match.teamInAttack=home?MatchManager.TeamInAttack.Home:MatchManager.TeamInAttack.Away;
        match.currentState=MatchManager.GameState.EndOfStandardPass;match.isClockRunning=true;match.refereeLeniency=2;
        // Isolate elapsed regulation from the authored opening's tactical allowance.
        typeof(MatchManager).GetMethod("AdvanceMatchTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(match,new object[]{100000f});
        Assert.That(match.isHalfExpired,Is.True);Assert.That(match.isClockRunning,Is.False);
        int continuations=0;match.ResolveActionBeforeFinalThird(MatchManager.MatchActionKind.StandardPass,()=>continuations++);
        Assert.That(match.IsWaitingForExtraActionsRoll,Is.True);Assert.That(continuations,Is.Zero);
        var authority=new OnlineGroundPassAuthority(match,protocol);var state=pause.Capture(authority);
        Assert.That(state.stage,Is.EqualTo("stoppage.roll"));Assert.That(state.owner,Is.EqualTo(home?"Home":"Away"));Assert.That(state.legalActions,Is.EqualTo(new[]{"stoppage.roll"}));
        var side=home?OnlineTeam.Home:OnlineTeam.Away;string own=home?"h":"a",other=home?"a":"h";
        OnlineMatchCommand Command(OnlineGroundPassState s)=>new OnlineMatchCommand(s.matchId,protocol.LastAcceptedSequence(side)+1,s.revision,s.decisionId,"stoppage.roll");
        var rng=Random.state;Assert.That(authority.Submit(other,Command(state)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));Assert.That(Random.state,Is.EqualTo(rng));
        var old=Command(state);pause.SetConnectionHold(true);Assert.That(pause.Capture(authority).legalActions,Is.Empty);Assert.That(authority.Submit(own,old).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted));Assert.That(match.extraActionsDetermined,Is.False);pause.SetConnectionHold(false);
        match.SetPauseMenuOpen(true);Assert.That(pause.Capture(authority).legalActions,Is.Empty);match.SetPauseMenuOpen(false);
        if(automatic){pause.SetConnectionHold(true);var heldRng=Random.state;match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+1);Assert.That(match.extraActionsDetermined,Is.False);Assert.That(Random.state,Is.EqualTo(heldRng));pause.SetConnectionHold(false);}
        bool rigged=false;for(int seed=0;seed<10000;seed++){Random.InitState(seed);if(Random.Range(1,7)!=face)continue;Random.InitState(seed);rigged=true;break;}Assert.That(rigged,Is.True);
        state=pause.Capture(authority);board.localTeam=side;board.controls.Configure(side);board.Display(state,"Stoppage roll");board.boardInput.samplePhysicalInput=false;
        var waiting=OnlineGroundPassState.Copy(state);string waitingClock=match.GetClockDisplayText();Assert.That(waitingClock,Does.Contain("(+?)"));
        OnlineMatchCommand accepted=null;
        void Requested(OnlineGroundPassMessage message){var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=wire.Receive(packet,packet.Length);accepted=decoded.ToCommand();Assert.That(authority.Submit(own,accepted).Status,Is.EqualTo(OnlineCommandStatus.Accepted));board.controls.Reply(new OnlineGroundPassMessage{status="Accepted"});}
        board.controls.Requested+=Requested;
        try{if(automatic){match.AdvanceOnlineAuthorityTime(DecisionTimingConfig.AutomaticRollSeconds+.1f);Assert.That(accepted,Is.Null,"The host timer rolls without pretending a client command arrived");}else Assert.That(board.boardInput.TryKey(KeyCode.R),Is.True);}finally{board.controls.Requested-=Requested;}
        Assert.That(match.IsWaitingForExtraActionsRoll,Is.False);Assert.That(match.extraActionsDetermined,Is.True);Assert.That(match.extraActionsTotal,Is.EqualTo(Mathf.Min(2+face,extra?3:7)));Assert.That(match.extraActionsRemaining,Is.EqualTo(match.extraActionsTotal));Assert.That(continuations,Is.EqualTo(1));
        var after=pause.Capture(authority);Assert.That(after.legalActions,Does.Not.Contain("stoppage.roll"));Assert.That(after.rolls.Last().context,Is.EqualTo("stoppage_extra_actions"));Assert.That(after.rolls.Last().roll,Is.EqualTo(face));
        if(accepted!=null)Assert.That(authority.Submit(own,accepted).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        else Assert.That(authority.Submit(own,Command(state)).Status,Is.Not.EqualTo(OnlineCommandStatus.Accepted),"The timer retires the old roll decision");
        Assert.That(continuations,Is.EqualTo(1));
        Assert.That(match.TryRollOnlineStoppage(),Is.False);Assert.That(after.stoppageTotal,Is.EqualTo(match.extraActionsTotal));string afterClock=match.GetClockDisplayText();
        pause.Dispose();pause=null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;board.boardInput.samplePhysicalInput=false;
        var replica=new OnlineGroundPassReplica(waiting.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        OnlineGroundPassState Decode(OnlineGroundPassState source){var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=source.matchId,state=source}))decoded=wire.Receive(packet,packet.Length);return decoded.state;}
        Assert.That(replica.Apply(Decode(waiting)),Is.True);board.controls.Configure(side);board.Display(replica.State,"Guest awaiting stoppage");Assert.That(board.match.GetClockDisplayText(),Is.EqualTo(waitingClock));
        int requests=0;void Collect(OnlineGroundPassMessage message){requests++;Assert.That(message.actionId,Is.EqualTo("stoppage.roll"));}
        var guestRng=Random.state;board.controls.Requested+=Collect;try{Assert.That(board.boardInput.TryKey(KeyCode.R),Is.True);Assert.That(board.boardInput.TryKey(KeyCode.R),Is.False);}finally{board.controls.Requested-=Collect;}
        Assert.That(requests,Is.EqualTo(1));Assert.That(board.match.TryRollOnlineStoppage(),Is.False);Assert.That(Random.state,Is.EqualTo(guestRng));Assert.That(board.match.extraActionsDetermined,Is.False);
        Assert.That(replica.Apply(Decode(after)),Is.True);board.Display(replica.State,"Guest added actions");Assert.That(board.match.GetClockDisplayText(),Is.EqualTo(afterClock));Assert.That(board.match.extraActionsRemaining,Is.EqualTo(after.stoppageRemaining));
        Assert.That(board.authorityRoot.activeSelf,Is.False);Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.TimingRollSequence,Is.Zero);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);
    }
}
