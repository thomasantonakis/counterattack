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
    private IEnumerator RunRoomCoin(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool penalties,bool winnerHome,bool takeFirst,bool chooseRight,bool retie,bool checkLegacy=false)
    {
        var match=board.match;match.currentHalf=2;match.gameData.gameSettings.numberOfHalfs=2;match.gameData.gameSettings.tiebreaker=penalties?"Penalties":"Extra Time and Penalties";
        typeof(MatchManager).GetMethod("BeginHalfEndFlow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(match,null);
        var authority=new OnlineGroundPassAuthority(match,protocol);OnlineGroundPassState state=null;
        float deadline=Time.realtimeSinceStartup+8;
        while((state=pause.Capture(authority)).coin==null && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(state.coin,Is.Not.Null);Assert.That(state.coin.purpose,Is.EqualTo((penalties?CoinTossPurpose.PenaltyShootout:CoinTossPurpose.ExtraTime).ToString()));
        Assert.That(OnlineManualSessionPolicy.CanFinish(state,true),Is.False);Assert.That(match.OnlineMatchComplete,Is.False);
        var panel=match.coinTossPanel;Assert.That(panel.dicePanel.visuals.activeSelf,Is.False,"Legacy local choices must not cover authenticated controls");
        var rng=Random.state;panel.dicePanel.Roll();panel.Choose(CoinTossChoice.Kickoff);panel.AdvanceChoices();
        Assert.That(panel.dicePanel.OnlineHomeScore,Is.Zero);Assert.That(Random.state,Is.EqualTo(rng));
        if(checkLegacy)
        {
            match.SetPauseMenuOpen(true);var held=pause.Capture(authority);
            Assert.That(held.legalActions,Is.Empty,"A Room input hold cannot advertise an executable coin roll");Assert.That(panel.TryExecuteOnline("coin.roll"),Is.False);
            match.SetPauseMenuOpen(false);state=pause.Capture(authority);
        }
        string Connection(OnlineTeam side)=>side==OnlineTeam.Home?"h":"a";
        void Rig(int face)
        {
            for(int seed=0;seed<10000;seed++){Random.InitState(seed);if(match.helperFunctions.DiceRoll()._roll!=face)continue;Random.InitState(seed);return;}Assert.Fail("Missing deterministic seed");
        }
        OnlineCommandReply Submit(OnlineTeam side,string action)
        {
            var current=pause.Capture(authority);return authority.Submit(Connection(side),new OnlineMatchCommand(current.matchId,protocol.LastAcceptedSequence(side)+1,current.revision,current.decisionId,action));
        }
        void Control(OnlineTeam side,string action)
        {Assert.That(pause.Submit(Connection(side),new OnlineMatchCommand("full-room-scene",pause.LastAcceptedSequence(side)+1,pause.Revision,"shared-pause",action)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));}
        Assert.That(Submit(OnlineTeam.Away,"coin.roll").Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        board.localTeam=OnlineTeam.Home;board.controls.Configure(OnlineTeam.Home);board.Display(state,"Room coin");Assert.That(board.coinView.gameObject.activeSelf,Is.True);
        OnlineMatchCommand accepted=null;
        void Requested(OnlineGroundPassMessage message)
        {
            var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=wire.Receive(packet,packet.Length);
            accepted=decoded.ToCommand();var response=authority.Submit(Connection(board.localTeam),accepted);Assert.That(response.Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            board.controls.Reply(new OnlineGroundPassMessage{status=response.Status.ToString()});board.Display(pause.Capture(authority),"Coin reply");
        }
        void DisplayTeam(OnlineTeam side){board.localTeam=side;board.controls.Configure(side);board.Display(pause.Capture(authority),"Own toss");}
        board.controls.Requested+=Requested;
        OnlineGroundPassState pending=null,after=null;
        try
        {
            void RollTeam(bool home,int face)
            {
                DisplayTeam(home?OnlineTeam.Home:OnlineTeam.Away);Assert.That(board.coinView.roll.interactable,Is.True);Rig(face);board.coinView.roll.onClick.Invoke();
                Assert.That(authority.Submit(Connection(home?OnlineTeam.Home:OnlineTeam.Away),accepted).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
            }
            if(retie)
            {
                RollTeam(true,3);RollTeam(false,3);Control(OnlineTeam.Home,OnlineSharedPause.RequestAction);pause.SetConnectionHold(true);
                float until=Time.realtimeSinceStartup+2.2f;while(Time.realtimeSinceStartup<until){Assert.That(pause.Capture(authority).coin.stage,Is.EqualTo("result"));yield return null;}
                pause.SetConnectionHold(false);Assert.That(pause.Capture(authority).coin.stage,Is.EqualTo("result"));Control(OnlineTeam.Home,OnlineSharedPause.ResumeAction);
                until=Time.realtimeSinceStartup+4;while(pause.Capture(authority).coin.stage!="home" && Time.realtimeSinceStartup<until)yield return null;
                Assert.That(pause.Capture(authority).coin.stage,Is.EqualTo("home"));
            }
            RollTeam(true,winnerHome?6:1);RollTeam(false,winnerHome?1:6);
            deadline=Time.realtimeSinceStartup+5;while(pause.Capture(authority).coin.stage!="choice" && Time.realtimeSinceStartup<deadline)yield return null;
            state=pause.Capture(authority);Assert.That(state.coin.stage,Is.EqualTo("choice"));Assert.That(state.coin.winnerHome,Is.EqualTo(winnerHome));
            if(checkLegacy)
            {
                panel.Choose(CoinTossChoice.Kickoff);panel.AdvanceChoices();
                Assert.That(panel.Decision.CanChooseKickoff,Is.True,"Legacy buttons cannot bypass the authenticated choosing team");Assert.That(panel.IsOpen,Is.True);
            }
            Assert.That(Submit(winnerHome?OnlineTeam.Away:OnlineTeam.Home,"coin.left").Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
            DisplayTeam(winnerHome?OnlineTeam.Home:OnlineTeam.Away);
            if(takeFirst){Assert.That(board.coinView.kickoff.interactable,Is.True);board.coinView.kickoff.onClick.Invoke();DisplayTeam(winnerHome?OnlineTeam.Away:OnlineTeam.Home);}
            pending=pause.Capture(authority);Assert.That(pending.coin.choosingHome,Is.EqualTo(takeFirst?!winnerHome:winnerHome));
            if(chooseRight)board.coinView.right.onClick.Invoke();else board.coinView.left.onClick.Invoke();
            Assert.That(panel.IsOpen,Is.False);Time.timeScale=8;
            deadline=Time.realtimeSinceStartup+25;
            while((after=pause.Capture(authority)).coin!=null || (penalties?after.shootout?.selecting!=true:match.currentHalf!=3 || after.stage!="kickoff.setup"))
            {if(Time.realtimeSinceStartup>=deadline)Assert.Fail("Room coin result did not reach its real next phase: "+after.stage);yield return null;}
            bool firstHome=takeFirst?winnerHome:!winnerHome;
            if(penalties)
            {
                Assert.That(match.gameData.penaltyShootoutSetup.homeTakesFirstPenalty,Is.EqualTo(firstHome));Assert.That(match.gameData.penaltyShootoutSetup.useRightGoal,Is.EqualTo(chooseRight));
                Assert.That(after.shootout.homeFirst,Is.EqualTo(firstHome));Assert.That(after.shootout.rightGoal,Is.EqualTo(chooseRight));
            }
            else
            {
                Assert.That(match.gameData.extraTimeOpeningSetup.homeHasFirstHalfKickoff,Is.EqualTo(firstHome));Assert.That(match.teamInAttack==MatchManager.TeamInAttack.Home,Is.EqualTo(firstHome));
                bool homeRight=(takeFirst?!winnerHome:winnerHome)==!chooseRight;
                Assert.That(match.homeTeamDirection==MatchManager.TeamAttackingDirection.LeftToRight,Is.EqualTo(homeRight));Assert.That(match.OnlineExtraTimeSubstitutionCredit,Is.True);
            }
        }
        finally{board.controls.Requested-=Requested;}
        var reader=new OnlineGroundPassWire();OnlineGroundPassMessage encoded=null;
        foreach(var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage{kind="state",matchId=pending.matchId,state=pending}))encoded=reader.Receive(packet,packet.Length);
        pause.Dispose();pause=null;Time.timeScale=1;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(encoded.state),Is.True);board.Display(replica.State,"Guest toss");Assert.That(board.coinView.gameObject.activeSelf,Is.True);
        Assert.That(board.coinView.homeDie.DisplayedRoll,Is.EqualTo(winnerHome?6:1));Assert.That(board.coinView.awayDie.DisplayedRoll,Is.EqualTo(winnerHome?1:6));
        Assert.That(replica.Apply(after),Is.True);board.Display(replica.State,"Guest transition");Assert.That(board.coinView.gameObject.activeSelf,Is.False);
        if(penalties)Assert.That(board.match.gameData.penaltyShootoutSetup.coinTossWinnerIsHome,Is.EqualTo(winnerHome));
        else{Assert.That(board.match.gameData.extraTimeOpeningSetup.coinTossWinnerIsHome,Is.EqualTo(winnerHome));Assert.That(board.match.gameData.extraTimeOpeningSetup.homeHasFirstHalfKickoff,Is.EqualTo(takeFirst?winnerHome:!winnerHome));}
        Assert.That(board.authorityRoot.activeSelf,Is.False);Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.TimingRollSequence,Is.Zero);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);
    }
    private IEnumerator RunNormalOnlineHalf(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,int half)
    {
        var match=board.match;match.currentHalf=half;match.gameData.gameSettings.numberOfHalfs=2;match.gameData.gameSettings.tiebreaker="Extra Time and Penalties";
        if(half==3)match.gameData.extraTimeOpeningSetup=match.gameData.openingSetup;
        var direction=match.homeTeamDirection;bool firstHome=match.gameData.openingSetup.homeHasFirstHalfKickoff;
        typeof(MatchManager).GetMethod("BeginHalfEndFlow",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(match,null);
        var authority=new OnlineGroundPassAuthority(match,protocol);Time.timeScale=8;float until=Time.realtimeSinceStartup+25;
        while(match.currentHalf!=half+1 || pause.Capture(authority).stage!="kickoff.setup")
        {
            var current=pause.Capture(authority);Assert.That(current.coin,Is.Null,"The ordinary half swap does not roll another coin toss");
            Assert.That(OnlineManualSessionPolicy.CanFinish(current,true),Is.False);
            if(current.stage=="match.transition")Assert.That(current.legalActions,Is.Empty,"A half reset cannot expose stale kickoff inputs");
            if(Time.realtimeSinceStartup>=until)Assert.Fail("Half reset did not complete: "+current.stage);yield return null;
        }
        var state=pause.Capture(authority);Assert.That(match.homeTeamDirection,Is.Not.EqualTo(direction));Assert.That(match.teamInAttack==MatchManager.TeamInAttack.Home,Is.EqualTo(!firstHome));
        Assert.That(state.regulationSeconds,Is.Zero);Assert.That(state.owner,Is.EqualTo("Home"));Assert.That(state.legalActions,Does.Contain(OnlineGroundPassAuthority.KickoffConfirmAction));
    }

}
