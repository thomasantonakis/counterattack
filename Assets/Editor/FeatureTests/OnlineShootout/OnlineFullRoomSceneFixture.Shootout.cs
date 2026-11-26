using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunShootout(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool homeFirst,bool uneven,string outcome=null)
    {
        var manager=board.match.OnlineShootoutManager;Assert.That(manager,Is.Not.Null);manager.Configure(board.match);
        board.match.gameData.penaltyShootoutSetup=new PenaltyShootoutSetup{homeTakesFirstPenalty=homeFirst,useRightGoal=board.match.homeTeamDirection==MatchManager.TeamAttackingDirection.RightToLeft};
        if(uneven)board.tokens.Single(t=>t.isHomeTeam && t.jerseyNumber==11).MarkSentOff();
        manager.ShowOrderSelection();Assert.That(manager.OnlineOrderSelection,Is.True);int count=uneven?10:11;
        Assert.That(manager.homeOrder.Count,Is.EqualTo(count));Assert.That(manager.awayOrder.Count,Is.EqualTo(count));
        var authority=new OnlineGroundPassAuthority(board.match,protocol);string homeConnection="h";
        string Connection(OnlineTeam team)=>team==OnlineTeam.Home?homeConnection:"a";
        OnlineMatchCommand Command(OnlineTeam team,string action)=>new("full-room-scene",pause.LastAcceptedSequence(team)+1,pause.Revision,action.StartsWith("shootout.")?"shootout-order":"shared-pause",action);
        OnlineCommandReply Submit(OnlineTeam team,string action)=>pause.Submit(Connection(team),Command(team,action));
        if(outcome?.StartsWith("roster-")==true)
        {
            var side=outcome.EndsWith("home")?OnlineTeam.Home:OnlineTeam.Away;
            Assert.That(Submit(OnlineTeam.Home,"shootout.ready").Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(Submit(side,OnlineSharedPause.RequestAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            board.match.SetSubstitutionsAvailable(true,"Owned stoppage while choosing shootout order");
            Assert.That(Submit(side,OnlineSharedPause.SubstitutionAction(2,13)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            var updated=pause.Capture(authority);var order=side==OnlineTeam.Home?updated.shootout.homeOrder:updated.shootout.awayOrder;
            string prefix=side==OnlineTeam.Home?"home:":"away:";
            Assert.That(order,Does.Contain(prefix+13));Assert.That(order,Does.Not.Contain(prefix+2));
            Assert.That(updated.shootout.homeReady || updated.shootout.awayReady,Is.False,"A roster change invalidates prior agreement");
            Assert.That(Submit(side,OnlineSharedPause.ResumeAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));outcome=null;
        }
        var original=manager.homeOrder.ToArray();manager.UpdateOrdersFromPanel(original.Reverse().ToList(),manager.awayOrder);manager.BeginShootoutFromOrderPanel(manager.homeOrder,manager.awayOrder);
        Assert.That(manager.isActive,Is.False,"A local start button cannot bypass two-team agreement");Assert.That(manager.homeOrder,Is.EqualTo(original));
        Assert.That(Submit(OnlineTeam.Home,"shootout.swap:1:18").Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Assert.That(Submit(OnlineTeam.Home,"shootout.swap:01:2").Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
        Assert.That(Submit(OnlineTeam.Away,OnlineSharedPause.RequestAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Submit(OnlineTeam.Home,"shootout.ready").Status,Is.EqualTo(OnlineCommandStatus.NotReady));
        Assert.That(Submit(OnlineTeam.Away,OnlineSharedPause.ResumeAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        pause.SetConnectionHold(true);Assert.That(Submit(OnlineTeam.Home,"shootout.ready").Status,Is.EqualTo(OnlineCommandStatus.NotReady));pause.SetConnectionHold(false);
        var state=pause.Capture(authority);board.localTeam=OnlineTeam.Home;board.controls.Configure(OnlineTeam.Home);board.Display(state,"Shootout order");
        Assert.That(board.shootoutView,Is.Not.Null);Assert.That(board.shootoutView.gameObject.activeSelf,Is.True);
        OnlineMatchCommand last=null;OnlineCommandStatus? replyStatus=null;
        void Requested(OnlineGroundPassMessage message)
        {
            var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=wire.Receive(packet,packet.Length);
            last=decoded.ToCommand();var reply=pause.Submit(Connection(board.localTeam),last);replyStatus=reply.Status;
            board.controls.Reply(new OnlineGroundPassMessage{status=reply.Status.ToString()});board.Display(pause.Capture(authority),"Order reply");
        }
        void DisplayTeam(OnlineTeam team){board.localTeam=team;board.controls.Configure(team);board.Display(pause.Capture(authority),"Own order");}
        board.controls.Requested+=Requested;
        OnlineGroundPassState pending=null;OnlineGroundPassState started=null;
        try
        {
            board.shootoutView.ownPlayer.value=0;Assert.That(board.shootoutView.later.interactable,Is.True);board.shootoutView.later.onClick.Invoke();Assert.That(replyStatus,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(manager.homeOrder[1],Is.SameAs(original[0]));var replay=last;
            Assert.That(protocol.BindAuthenticatedAccount("home","h2",protocol.Compatibility),Is.True);homeConnection="h2";
            Assert.That(pause.Submit("h",replay).Status,Is.EqualTo(OnlineCommandStatus.UnknownConnection));Assert.That(pause.Submit("h2",replay).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
            Assert.That(pause.Submit("h2",new OnlineMatchCommand(replay.MatchId,replay.Sequence,replay.Revision,replay.DecisionId,"shootout.ready")).Status,Is.EqualTo(OnlineCommandStatus.SequenceConflict));
            board.shootoutView.confirm.onClick.Invoke();Assert.That(pause.Capture(authority).shootout.homeReady,Is.True);Assert.That(manager.isActive,Is.False);
            var stale=Command(OnlineTeam.Home,"shootout.ready");DisplayTeam(OnlineTeam.Away);board.shootoutView.ownPlayer.value=0;board.shootoutView.later.onClick.Invoke();
            Assert.That(pause.Capture(authority).shootout.homeReady,Is.False,"Changing an order invalidates agreement to the prior state");Assert.That(pause.Submit("h2",stale).Status,Is.EqualTo(OnlineCommandStatus.StaleRevision));
            DisplayTeam(OnlineTeam.Home);board.shootoutView.confirm.onClick.Invoke();pending=pause.Capture(authority);
            DisplayTeam(OnlineTeam.Away);board.shootoutView.confirm.onClick.Invoke();Assert.That(replyStatus,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(manager.isActive,Is.True);Assert.That(pause.Submit("a",last).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
            Time.timeScale=8;float deadline=Time.realtimeSinceStartup+30;
            while(board.match.currentState!=MatchManager.GameState.PenaltyShootoutKickExecution && Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(board.match.currentState,Is.EqualTo(MatchManager.GameState.PenaltyShootoutKickExecution));Assert.That(manager.currentShooter.isHomeTeam,Is.EqualTo(homeFirst));
            Assert.That(manager.currentShooter,Is.SameAs((homeFirst?manager.homeOrder:manager.awayOrder)[0]));Assert.That(manager.currentDefendingGoalkeeper.IsGoalKeeper,Is.True);
            Assert.That(board.match.shotManager.targetHex.isInGoal,Is.Not.Zero);
            started=pause.Capture(authority);Assert.That(started.legalActions.Contains(OnlineGroundPassAuthority.ShotRollAction),Is.True,"The actual shootout must expose its shooter roll after the host selects a random goal target");
        }
        finally{board.controls.Requested-=Requested;}

        bool finishing=outcome?.StartsWith("finish-")==true || outcome?.StartsWith("sudden-")==true;
        if(outcome!=null)
        {
            var shot=board.match.shotManager;board.match.movementPhaseManager.duelResolutionPanel.resultSeconds=.01f;
            void Rig(int face,bool jackpot=false)
            {
                for(int seed=0;seed<10000;seed++)
                {
                    Random.InitState(seed);var roll=board.match.helperFunctions.DiceRoll();
                    if(roll._roll!=face || roll._isJackPot!=jackpot)continue;Random.InitState(seed);return;
                }
                Assert.Fail("Missing deterministic die seed");
            }
            void Roll(string action,int face,bool jackpot=false)
            {
                Rig(face,jackpot);var current=pause.Capture(authority);var side=current.owner=="Home"?OnlineTeam.Home:OnlineTeam.Away;
                var command=new OnlineMatchCommand(current.matchId,protocol.LastAcceptedSequence(side)+1,current.revision,current.decisionId,action);
                Assert.That(authority.Submit(Connection(side),command).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            }
            IEnumerator Kick(string kind)
            {
                bool kickingHome=manager.currentShooter.isHomeTeam;int before=manager.homeAttempts+manager.awayAttempts;bool goal=kind=="goal";
                manager.currentShooter.shooting=5;manager.currentDefendingGoalkeeper.saving=kind=="tie"?7:5; // Existing penalty save modifier is -2.
                Roll(OnlineGroundPassAuthority.ShotRollAction,kind=="offtarget"?1:goal?6:3,goal);
                if(kind!="offtarget")
                {
                    float until=Time.realtimeSinceStartup+10;
                    while(authority.Capture().stage!="shot.save" && Time.realtimeSinceStartup<until)yield return null;
                    Assert.That(authority.Capture().stage,Is.EqualTo("shot.save"));Roll(OnlineGroundPassAuthority.ShotSaveAction,goal?1:kind=="save"?6:3,kind=="save");
                    if(kind=="tie")
                    {
                        until=Time.realtimeSinceStartup+10;
                        while(authority.Capture().stage!="loose.direction" && Time.realtimeSinceStartup<until)yield return null;
                        Assert.That(authority.Capture().stage,Is.EqualTo("loose.direction"));Roll(OnlineGroundPassAuthority.LooseRollAction,1);
                    }
                }
                float deadline=Time.realtimeSinceStartup+25;
                bool Ready()=>manager.homeAttempts+manager.awayAttempts==before+1 && (manager.isComplete ||
                    manager.currentShooter!=null && manager.currentShooter.isHomeTeam!=kickingHome && board.match.currentState==MatchManager.GameState.PenaltyShootoutKickExecution && shot.isWaitingForShotRoll);
                while(!Ready() && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(Ready(),Is.True,"A resolved kick must reach the actual next shooter or final result");
                Assert.That((kickingHome?manager.HomeResults:manager.AwayResults).Last(),Is.EqualTo(goal));
                Assert.That(board.match.gameData.stats.homeTeamStats.totalGoals,Is.Zero);Assert.That(board.match.gameData.stats.awayTeamStats.totalGoals,Is.Zero);
            }
            if(!finishing)
            {
                yield return Kick(outcome);Assert.That(manager.homeAttempts+manager.awayAttempts,Is.EqualTo(1));
                Assert.That(manager.homeShootoutScore+manager.awayShootoutScore,Is.EqualTo(outcome=="goal"?1:0));
                Assert.That(manager.currentShooter.isHomeTeam,Is.EqualTo(!homeFirst));
            }
            else
            {
                bool winnerHome=outcome.EndsWith("home"),sudden=outcome.StartsWith("sudden");int kicks=0;
                while(!manager.isComplete && kicks++<14)
                {
                    int attempts=manager.homeAttempts+manager.awayAttempts;
                    bool score=(!sudden || attempts>=10) && manager.currentShooter.isHomeTeam==winnerHome;
                    yield return Kick(score?"goal":"offtarget");
                }
                Assert.That(manager.isComplete,Is.True);Assert.That(manager.winnerIsHome,Is.EqualTo(winnerHome));
                Assert.That(manager.homeAttempts+manager.awayAttempts,Is.EqualTo(sudden?12:6));
                float deadline=Time.realtimeSinceStartup+15;
                while(!board.match.OnlineMatchComplete && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(board.liveScoreboard.gameObject.activeSelf,Is.True,"Online shootout completion must preserve the authored live scoreboard");
                Assert.That(board.match.OnlineMatchComplete,Is.True);Assert.That(OnlineManualSessionPolicy.CanFinish(pause.Capture(authority),true),Is.True);
            }
            started=pause.Capture(authority);
        }
        // Retain actual real staging/shot-entry state, then verify a new inert guest.
        var reader=new OnlineGroundPassWire();OnlineGroundPassMessage encoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=started.matchId,state=started}))encoded=reader.Receive(packet,packet.Length);
        pause.Dispose();pause=null;Time.timeScale=1;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        Assert.That(PenaltyShootoutManager.ActiveShootout,Is.Null,"Leaving authority Room must release its shootout singleton");
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(pending),Is.True);board.Display(replica.State,"Guest order");Assert.That(board.shootoutView.gameObject.activeSelf,Is.True);Assert.That(board.shootoutView.ownPlayer.options.Count,Is.EqualTo(count));
        Assert.That(replica.Apply(encoded.state),Is.True);board.Display(replica.State,"Guest first kick");
        if(finishing){Assert.That(board.match.OnlineShootoutManager.isComplete,Is.True);Assert.That(board.match.OnlineMatchComplete,Is.True);Assert.That(board.shootoutView.gameObject.activeSelf,Is.True);}
        else Assert.That(board.match.OnlineShootoutManager.currentShooter.isHomeTeam,Is.EqualTo(outcome==null?homeFirst:!homeFirst));if(!finishing)Assert.That(board.shootoutView.gameObject.activeSelf,Is.False,"Order controls must clear for the actual kick controls");Assert.That(board.authorityRoot.activeSelf,Is.False);
        Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.TimingRollSequence,Is.Zero);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);
    }
}
