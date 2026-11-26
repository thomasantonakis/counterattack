using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunPublicationCadence(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,string branch)
    {
        protocol.BindAuthenticatedAccount("home","guest-home",protocol.Compatibility);protocol.BindAuthenticatedAccount("away","host-away",protocol.Compatibility);
        var authority=new OnlineGroundPassAuthority(board.match,protocol);
        var type=typeof(OnlineGroundPassBoard).Assembly.GetType("CounterAttack.Online.EOSGroundPassExchange",true);
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var incoming=new Queue<OnlineGroundPassMessage>();var outgoing=new List<OnlineGroundPassMessage>();
        OnlineGroundPassMessage Copy(OnlineGroundPassMessage message)
        {
            message.matchId="full-room-scene";var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;
            foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=wire.Receive(packet,packet.Length);
            Assert.That(decoded,Is.Not.Null);return decoded;
        }
        object exchange=null;double now=0;string nonce=Guid.NewGuid().ToString("N");long generation=0,sequence=0,heartbeat=0;
        exchange=type.GetConstructors(flags).Single(c=>c.GetParameters().Length==7).Invoke(new object[]{board,directory,
            (Action<OnlineGroundPassMessage>)(m=>outgoing.Add(Copy(m))),
            (Action)(()=>{var queue=(Queue<OnlineGroundPassMessage>)type.GetField("messages",flags).GetValue(exchange);while(incoming.Count>0)queue.Enqueue(incoming.Dequeue());}),
            "home","full-room-scene",(Func<double>)(()=>now)});
        type.GetMethod("PrepareHostBoard",flags).Invoke(exchange,null);
        var flow=(IEnumerator)type.GetMethod("ManualHost",flags).Invoke(exchange,new object[]{authority,protocol,pause});
        void Send(OnlineGroundPassMessage message){message.peerNonce=nonce;message.connectionGeneration=generation;incoming.Enqueue(Copy(message));}
        void Step(double seconds)
        {
            now+=seconds;outgoing.Clear();Send(new OnlineGroundPassMessage{kind="hello",status="heartbeat",sequence=++heartbeat});
            Assert.That(flow.MoveNext(),Is.True,"Live full Room stays open");
        }
        OnlineGroundPassState State()=>outgoing.Last(m=>m.kind=="state").state;
        try
        {
            Send(new OnlineGroundPassMessage{kind="hello"});Step(0);
            var handshake=outgoing.Last(m=>m.kind=="state" && m.status=="resync");generation=handshake.connectionGeneration;
            Send(new OnlineGroundPassMessage{kind="hello",status="resync-ack",digest=(string)type.GetMethod("Digest",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{handshake.state})});
            Step(.001);var current=State();Assert.That(current.connectionHeld,Is.False);Assert.That(current.stage,Is.EqualTo("kickoff.setup"));
            if(branch=="selection")
            {
                var players=current.legalActions.Where(a=>a.StartsWith("kickoff.player:")).Take(2).ToArray();Assert.That(players.Length,Is.EqualTo(2));
                for(int i=0;i<2;i++)
                {
                    var old=current;Send(OnlineGroundPassMessage.Command(old,++sequence,players[i]));Step(.01);
                    Assert.That(outgoing.Single(m=>m.kind=="reply").status,Is.EqualTo("Accepted"));current=State();
                    Assert.That(current.stage,Is.EqualTo(old.stage));Assert.That(current.owner,Is.EqualTo(old.owner));Assert.That(current.revision,Is.GreaterThan(old.revision));
                    Assert.That(current.decisionId,Is.Not.EqualTo(old.decisionId));Assert.That(current.selectedPlayer,Is.EqualTo(players[i].Substring("kickoff.player:".Length)));
                    Assert.That(current.legalActions.Any(a=>a.StartsWith("kickoff.move:")),Is.True);
                    Send(OnlineGroundPassMessage.Command(old,++sequence,players[i]));Step(.001);
                    Assert.That(outgoing.Single(m=>m.kind=="reply").status,Is.EqualTo("StaleRevision"));
                    Assert.That(authority.Capture().selectedPlayer,Is.EqualTo(current.selectedPlayer));
                }
            }
            else if(branch=="pause")
            {
                foreach(var action in new[]{OnlineSharedPause.RequestAction,OnlineSharedPause.ResumeAction})
                {
                    var old=current;Send(new OnlineGroundPassMessage{kind="control",sequence=++sequence,revision=old.sharedPauseRevision,decisionId="shared-pause",actionId=action});Step(.01);
                    Assert.That(outgoing.Single(m=>m.kind=="control-reply").status,Is.EqualTo("Accepted"));current=State();
                    Assert.That(current.sharedPauseRevision,Is.GreaterThan(old.sharedPauseRevision));Assert.That(current.isSharedPaused,Is.EqualTo(action==OnlineSharedPause.RequestAction));
                    Assert.That(current.legalActions.Length,action==OnlineSharedPause.RequestAction?Is.EqualTo(0):Is.GreaterThan(0));
                }
            }
            else if(branch=="clock")
            {
                string sentDecision=null;int commands=0;double deadline=UnityEngine.Time.realtimeSinceStartupAsDouble+20;
                while(!(current.passActive && current.awaitingTarget))
                {
                    Assert.That(UnityEngine.Time.realtimeSinceStartupAsDouble,Is.LessThan(deadline),"Real kickoff reaches its required pass");
                    string action=current.stage=="kickoff.setup"?OnlineGroundPassAuthority.KickoffConfirmAction
                        :current.stage=="kickoff.taker"?current.legalActions.FirstOrDefault(a=>a.StartsWith("kickoff.taker:")):null;
                    string decision=current.decisionId+":"+current.revision;
                    if(action!=null && decision!=sentDecision)
                    {
                        sentDecision=decision;commands++;
                        if(current.owner=="Home")Send(OnlineGroundPassMessage.Command(current,++sequence,action));
                        else{board.controls.legalCommands.value=Array.IndexOf(current.legalActions,action);board.controls.commandButton.onClick.Invoke();}
                    }
                    yield return null;Step(.01);current=authority.Capture();
                    foreach(var reply in outgoing.Where(m=>m.kind=="reply"))Assert.That(reply.status,Is.EqualTo("Accepted"));
                }
                Assert.That(commands,Is.EqualTo(3));board.match.ResumeMatchClockForLivePlay();Step(.26);var baseline=State();
                string target=baseline.legalActions.First(a=>a.StartsWith("ground.target:"));
                for(int cycle=0;cycle<2;cycle++)
                {
                    board.match.AdvanceOnlineAuthorityTime(.05f);var progressed=authority.Capture();
                    Assert.That(progressed.publicationSequence,Is.GreaterThan(current.publicationSequence),"Real clock/budget progresses");
                    Assert.That(progressed.revision,Is.EqualTo(baseline.revision));Assert.That(progressed.decisionId,Is.EqualTo(baseline.decisionId));
                    Step(.1);Assert.That(outgoing.Any(m=>m.kind=="state"),Is.False,"Clock-only snapshots do not send every frame");
                    Step(.14);Assert.That(outgoing.Any(m=>m.kind=="state"),Is.False,"Clock-only refresh waits at least 250 ms");
                    Step(.02);current=State();Assert.That(current.publicationSequence,Is.GreaterThan(baseline.publicationSequence));
                    Assert.That(current.revision,Is.EqualTo(baseline.revision));Assert.That(current.decisionId,Is.EqualTo(baseline.decisionId));
                }
                var delayed=OnlineGroundPassMessage.Command(baseline,baseline.owner=="Home"?++sequence:baseline.awayAcceptedSequence+1,target);
                if(baseline.owner=="Home")
                {
                    Send(delayed);Step(.001);
                    Assert.That(outgoing.Single(m=>m.kind=="reply").status,Is.EqualTo("Accepted"),"Time refresh cannot stale an unchanged legal command");
                }
                else
                {
                    Assert.That(authority.Submit("host-away",delayed.ToCommand()).Status,Is.EqualTo(OnlineCommandStatus.Accepted),"Host command captured before clock refresh remains legal");Step(.001);
                }
                Assert.That(State().hasSelectedTarget,Is.True);
            }
            else
            {
                Step(.1);Assert.That(outgoing.Any(m=>m.kind=="state"),Is.False,"Idle state must not flood snapshots");
                Step(.89);Assert.That(outgoing.Any(m=>m.kind=="state"),Is.False,"Unchanged state waits for heartbeat");
                Step(.02);var next=State();Assert.That(next.decisionId,Is.EqualTo(current.decisionId));Assert.That(next.revision,Is.EqualTo(current.revision));
                Assert.That(next.tokens.Length,Is.EqualTo(board.tokens.Length));Assert.That(next.legalActions,Is.EqualTo(current.legalActions));
            }
        }
        finally{(flow as IDisposable)?.Dispose();}
        yield return null;
    }
}
