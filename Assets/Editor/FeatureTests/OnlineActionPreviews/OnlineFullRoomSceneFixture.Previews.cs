using System;
using System.Collections;
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
    private IEnumerator RunActionPreview(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,string mode)
    {
        var authority=new OnlineGroundPassAuthority(board.match,protocol);var state=authority.Capture();float deadline=Time.realtimeSinceStartup+20;
        void Submit(string action)
        {
            var s=authority.Capture();bool home=s.owner=="Home";long sequence=protocol.LastAcceptedSequence(home?OnlineTeam.Home:OnlineTeam.Away)+1;
            Assert.That(authority.Submit(home?"h":"a",OnlineGroundPassMessage.Command(s,sequence,action).ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        while(!(state.passActive && state.awaitingTarget))
        {
            Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
            if(state.stage=="kickoff.setup")Submit(OnlineGroundPassAuthority.KickoffConfirmAction);
            else if(state.stage=="kickoff.taker")Submit(OnlineGroundPassAuthority.KickoffTakerAction("away:9"));
            yield return null;state=authority.Capture();
        }
        int side=board.match.awayTeamDirection==MatchManager.TeamAttackingDirection.LeftToRight?1:-1;
        HexCell Hex(int x,int z=0)=>board.grid.GetHexCellAt(new Vector3Int(x,0,z));
        void Place(PlayerToken token,HexCell hex)
        {
            if(hex.occupyingToken!=null && hex.occupyingToken!=token)
            {
                var free=board.cells.First(c=>!c.isOutOfBounds && c.occupyingToken==null && Math.Abs(c.coordinates.x)>9 && Math.Abs(c.coordinates.z)>7);
                Place(hex.occupyingToken,free);
            }
            var old=token.GetCurrentHex();if(old!=null){old.occupyingToken=null;old.isAttackOccupied=old.isDefenseOccupied=false;}
            hex.isAttackOccupied=token.isAttacker;hex.isDefenseOccupied=!token.isAttacker;token.SetCurrentHex(hex);token.transform.position=hex.GetHexCenter()+Vector3.up*.2f;
        }
        var home=board.tokens.Where(t=>t.isHomeTeam && t.isPlaying && !t.IsGoalKeeper).OrderBy(t=>t.jerseyNumber).ToArray();
        var away=board.tokens.Where(t=>!t.isHomeTeam && t.isPlaying && !t.IsGoalKeeper && t.jerseyNumber!=9).OrderBy(t=>t.jerseyNumber).ToArray();
        for(int i=0;i<home.Length;i++)Place(home[i],Hex(side*(10+i%5),8+i/5));
        for(int i=0;i<away.Length;i++)Place(away[i],Hex(-side*(10+i%5),-8-i/5));
        var receiver=board.tokens.Single(t=>!t.isHomeTeam && t.jerseyNumber==10);Place(receiver,Hex(6*side));
        if(mode=="risk")Place(home[0],Hex(3*side,1));
        string target=OnlineGroundPassAuthority.TargetAction(6*side,0);Submit(target);
        if(mode.StartsWith("shot"))
        {
            Submit(target);deadline=Time.realtimeSinceStartup+15;
            while(board.match.groundBallManager.isActivated){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;}
            Place(receiver,Hex(13*side));board.match.ball.PlaceAtCell(receiver.GetCurrentHex());board.match.SetLastToken(receiver);board.match.UpdatePossessionAfterPass(receiver.GetCurrentHex());
            board.match.BroadcastSafeEndofMovementPhase(countMovementAction:false,triggerFinalThird:false);
            Submit(OnlineGroundPassAuthority.ShotStartAction);Submit(OnlineGroundPassAuthority.ShotConfirmAction);
            Assert.That(authority.Capture().stage,Is.EqualTo("shot.target"));Submit(OnlineGroundPassAuthority.ShotTargetAction(19*side,0));
            deadline=Time.realtimeSinceStartup+10;while(authority.Capture().stage!="shot.roll"){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;}
        }
        if(mode.EndsWith("-expiry"))
        {
            Assert.That(board.match.ball.enabled,Is.False);Assert.That(board.match.ball.CommittedGoalTarget,Is.Not.Null);
            var start=board.match.ball.transform.position;board.match.ball.StartCoroutine(board.match.ball.MoveToCell(Hex(14*side),allowGKBoxMove:false));deadline=Time.realtimeSinceStartup+3;
            while(Vector3.Distance(start,board.match.ball.transform.position)<.01f){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;}
            authority.Capture();Assert.That(board.match.ball.CommittedGoalTarget,Is.Null,"Explicit authority presentation refresh replaces disabled LateUpdate");Assert.That(Hex(19*side).IsCommittedGoalTarget,Is.False);
        }
        var reason=typeof(HexCell).GetField("currentHighlightReason",BindingFlags.Instance|BindingFlags.NonPublic);
        string Reason(HexCell h)=>(string)reason.GetValue(h);
        var expected=board.grid.highlightedHexes.Distinct().Where(h=>h!=null && Reason(h)!=null).Select(h=>new{coordinates=h.coordinates,color=h.hexRenderer.material.color,y=h.transform.position.y,reason=Reason(h)}).ToArray();
        if(mode.EndsWith("-expiry"))
        {
            var goal=Hex(19*side);expected=expected.Where(e=>e.coordinates!=goal.coordinates).Append(new{coordinates=goal.coordinates,color=goal.hexRenderer.material.color,y=goal.transform.position.y,reason=(string)null}).ToArray();
        }
        Assert.That(expected.Length,Is.GreaterThan(0));
        if(mode=="risk")Assert.That(expected.Any(e=>e.reason=="dangerousPass"),Is.True,"Real defender geometry creates a dangerous pass preview");
        if(mode=="safe" || mode.StartsWith("shot"))Assert.That(expected.Any(e=>e.reason=="ballPath"),Is.True);
        if(mode=="medium")Assert.That(expected.Any(e=>e.reason=="dangerousPass"),Is.False,"Medium assistance does not reveal uncomputed danger");
        var personal=Hex(23,17);personal.HighlightHex("hover");board.grid.highlightedHexes.Add(personal);
        state=authority.Capture();var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=state.matchId,state=state}))decoded=wire.Receive(packet,packet.Length);
        OnlineGroundPassState held=null,paused=null,recovered=null;
        if(mode.EndsWith("-hold"))
        {
            pause.SetConnectionHold(true);held=pause.Capture(authority);pause.SetConnectionHold(false);
            Assert.That(pause.Submit("a",new OnlineMatchCommand(state.matchId,1,pause.Revision,"shared-pause",OnlineSharedPause.RequestAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));paused=pause.Capture(authority);
            Assert.That(pause.Submit("a",new OnlineMatchCommand(state.matchId,2,pause.Revision,"shared-pause",OnlineSharedPause.ResumeAction)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));recovered=pause.Capture(authority);
        }
        board.grid.ClearHighlightedHexes();var withdrawn=authority.Capture();
        pause.Dispose();pause=null;yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica(state.matchId,board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);Assert.That(replica.Apply(decoded.state),Is.True);
        var rng=JsonUtility.ToJson(UnityEngine.Random.state);board.Display(replica.State,"Published action preview");var cells=board.tokens.Select(t=>t.GetCurrentHex()).ToArray();
        foreach(var item in expected)
        {
            var hex=board.grid.GetHexCellAt(item.coordinates);var actual=hex.hexRenderer.material.color;
            Assert.That(hex.transform.position.y,Is.EqualTo(item.y).Within(.001f));
            Assert.That(Vector4.Distance(actual,item.color),Is.LessThan(.001f),"Guest must reproduce source preview color at "+item.coordinates+" ("+item.reason+")");
        }
        Assert.That(Reason(board.grid.GetHexCellAt(new Vector3Int(23,0,17))),Is.Not.EqualTo("hover"),"Personal cursor hover is not a shared preview");
        if(held!=null)
        {
            Assert.That(replica.Apply(held),Is.True);board.Display(replica.State,"Connection hold");
            Assert.That(board.cells.Any(h=>h.IsCommittedGoalTarget),Is.False);foreach(var item in expected.Where(e=>e.y>.001f))Assert.That(board.grid.GetHexCellAt(item.coordinates).transform.position.y,Is.Zero.Within(.001f));
            Assert.That(replica.Apply(paused),Is.True);board.Display(replica.State,"Shared pause");Assert.That(board.cells.Any(h=>h.IsCommittedGoalTarget),Is.False);
            Assert.That(replica.Apply(recovered),Is.True);board.Display(replica.State,"Recovered goal marker");
            foreach(var item in expected){var hex=board.grid.GetHexCellAt(item.coordinates);Assert.That(Vector4.Distance(hex.hexRenderer.material.color,item.color),Is.LessThan(.001f));Assert.That(hex.transform.position.y,Is.EqualTo(item.y).Within(.001f));}
            board.boardInput.enabled=false;Assert.That(board.cells.Any(h=>h.IsCommittedGoalTarget),Is.False);foreach(var item in expected.Where(e=>e.y>.001f))Assert.That(board.grid.GetHexCellAt(item.coordinates).transform.position.y,Is.Zero.Within(.001f));
            board.boardInput.enabled=true;board.Display(replica.State,"Reenabled preview");
            var current=replica.State;var pose=board.displayedBall.position;var frame=new OnlineGroundPassPresentation{matchId=current.matchId,publicationSequence=current.publicationSequence,frameSequence=1,ballX=pose.x+.1f,ballY=pose.y,ballZ=pose.z};
            Assert.That(replica.ApplyPresentation(frame),Is.True);board.QueuePresentation(frame);float markerDeadline=Time.realtimeSinceStartup+2;
            while(board.cells.Any(h=>h.IsCommittedGoalTarget) && Time.realtimeSinceStartup<markerDeadline)yield return null;
            Assert.That(board.cells.Any(h=>h.IsCommittedGoalTarget),Is.False,"Guest display motion clears a held marker without waiting for a full snapshot");
            Assert.That(replica.State.hexPreviews.Any(p=>p.committedGoal),Is.True,"Motion presentation cannot mutate the accepted logical snapshot");
        }
        Assert.That(replica.Apply(withdrawn),Is.True);board.Display(replica.State,"Preview withdrawn");
        Assert.That(board.cells.Any(h=>Reason(h)=="dangerousPass" || Reason(h)=="ballPath"),Is.False,"Withdrawing source previews clears their markers");
        Assert.That(board.tokens.Select(t=>t.GetCurrentHex()),Is.EqualTo(cells));Assert.That(JsonUtility.ToJson(UnityEngine.Random.state),Is.EqualTo(rng));Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);Assert.That(board.authorityRoot.activeSelf,Is.False);
    }
}
