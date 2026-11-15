using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunRequiredKeeper(OnlineGroundPassBoard board,OnlineMatchProtocol protocol,MatchManager.GameData data,bool home,string scenario,bool opponentPause)
    {
        var team=home?OnlineTeam.Home:OnlineTeam.Away;var other=home?OnlineTeam.Away:OnlineTeam.Home;
        string Connection(OnlineTeam side)=>side==OnlineTeam.Home?"h":"a";
        OnlineCommandReply Submit(OnlineTeam side,string action)=>pause.Submit(Connection(side),new OnlineMatchCommand("full-room-scene",pause.LastAcceptedSequence(side)+1,pause.Revision,"shared-pause",action));
        PlayerToken Token(int number)=>board.tokens.Single(t=>t.isHomeTeam==home && t.jerseyNumber==number);
        if(scenario=="no-subs")
        {
            board.match.SetSubstitutionsAvailable(true,"Owned exhausted-allowance setup");Assert.That(Submit(team,OnlineSharedPause.RequestAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            for(int i=0;i<MatchManager.MaxSubstitutionsPerTeam;i++)Assert.That(Submit(team,OnlineSharedPause.SubstitutionAction(3+i,13+i)).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(Submit(team,OnlineSharedPause.ResumeAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        int used=board.match.GetSubstitutionsUsed(home);
        if(opponentPause)Assert.That(Submit(other,OnlineSharedPause.RequestAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
        var original=Token(1);var goalkeeperHex=original.GetCurrentHex().coordinates;var nomineeHex=Token(2).GetCurrentHex().coordinates;
        if(scenario=="no-bench")Token(12).MarkSentOff();
        if(scenario=="double"){original.ReceiveInjury();original.ReceiveInjury();}
        else board.match.HandleSentOff(original,original.GetCurrentHex());
        var legacy=Object.FindAnyObjectByType<PauseMenuManager>();if(legacy!=null)Assert.That(legacy.pausePanel.activeSelf,Is.False,"Online required decisions must not activate the offline pause panel");
        var authority=new OnlineGroundPassAuthority(board.match,protocol);var before=pause.Capture(authority);
        Assert.That(pause.IsPaused,Is.True);Assert.That(pause.Requester,Is.EqualTo(opponentPause?other:team));Assert.That(Time.timeScale,Is.Zero);
        Assert.That(Submit(pause.Requester.Value,OnlineSharedPause.ResumeAction).Status,Is.EqualTo(OnlineCommandStatus.NotReady));
        bool emergency=scenario is "no-subs" or "no-bench";int outgoing=scenario=="double"?1:2;
        string action=emergency?"keeper.nominate:2":OnlineSharedPause.SubstitutionAction(outgoing,12);
        Assert.That(Submit(other,action).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(Submit(team,emergency?"keeper.nominate:16":"roster.substitute:2:13").Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));
        board.localTeam=team;board.controls.Configure(team);board.Display(before,"Required goalkeeper");Assert.That(board.menu.resumeButton.interactable,Is.False);
        OnlineMatchCommand accepted=null;OnlineCommandStatus? result=null;
        void Requested(OnlineGroundPassMessage message)
        {
            var wire=new OnlineGroundPassWire();OnlineGroundPassMessage decoded=null;foreach(var packet in OnlineGroundPassWire.Encode(message))decoded=wire.Receive(packet,packet.Length);
            accepted=decoded.ToCommand();var reply=pause.Submit(Connection(team),accepted);result=reply.Status;
            board.controls.Reply(new OnlineGroundPassMessage{status=reply.Status.ToString()});board.Display(pause.Capture(authority),"Required goalkeeper");
        }
        board.controls.Requested+=Requested;
        try
        {
            T Field<T>(string name) where T:Object
            {var field=typeof(OnlineRoomMenu).GetField(name);Assert.That(field,Is.Not.Null,name+" must be authored");var value=(T)field.GetValue(board.menu);Assert.That(value,Is.Not.Null);return value;}
            if(emergency)
            {
                var open=Field<Button>("nominationButton");Assert.That(open.interactable,Is.True);open.onClick.Invoke();
                var dropdown=Field<Dropdown>("emergencyKeeper");dropdown.value=dropdown.options.FindIndex(o=>o.text.StartsWith("2. "));
                var confirm=Field<Button>("nominationCommitButton");Assert.That(confirm.interactable,Is.True);confirm.onClick.Invoke();
            }
            else
            {
                Assert.That(board.menu.substitutionButton.interactable,Is.True);board.menu.substitutionButton.onClick.Invoke();
                board.menu.outgoingPlayer.value=board.menu.outgoingPlayer.options.FindIndex(o=>o.text.StartsWith(outgoing+". "));
                board.menu.incomingPlayer.value=board.menu.incomingPlayer.options.FindIndex(o=>o.text.StartsWith("12. "));
                Assert.That(board.menu.substitutionCommitButton.interactable,Is.True);board.menu.substitutionCommitButton.onClick.Invoke();
            }
            Assert.That(result,Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        finally{board.controls.Requested-=Requested;}
        Assert.That(board.match.HasGoalkeeperActionBlockingResume(),Is.False);Assert.That(pause.IsPaused,Is.True);Assert.That(pause.Requester,Is.EqualTo(opponentPause?other:team));Assert.That(Time.timeScale,Is.Zero);
        Assert.That(pause.Submit(Connection(team),accepted).Status,Is.EqualTo(OnlineCommandStatus.Duplicate));
        Assert.That(board.match.GetSubstitutionsUsed(home),Is.EqualTo(used+(emergency?0:1)));
        int number=emergency?2:12;var current=Token(number);Assert.That(current.IsGoalKeeper,Is.True);Assert.That(current.GetCurrentHex().coordinates,Is.EqualTo(emergency?nomineeHex:goalkeeperHex));
        if(emergency){Assert.That(current.saving,Is.Zero);Assert.That(current.handling,Is.Zero);Assert.That(current.aerial,Is.Zero);Assert.That(current.heading,Is.Zero);Assert.That(current.shooting,Is.Zero);}
        var after=pause.Capture(authority);
        if(opponentPause)Assert.That(Submit(team,OnlineSharedPause.ResumeAction).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(Submit(pause.Requester.Value,OnlineSharedPause.ResumeAction).Status,Is.EqualTo(OnlineCommandStatus.Accepted));pause.Dispose();pause=null;
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/MainMenu.unity",new LoadSceneParameters(LoadSceneMode.Single));session.Dispose();session=null;MatchManager.Instance=null;
        yield return Load(data,true);board=Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica=new OnlineGroundPassReplica("full-room-scene",board.tokens.Select(OnlineRollPublication.ActorId),OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(before),Is.True);board.Display(replica.State,"Keeper requirement replica");Assert.That(board.match.HasGoalkeeperActionBlockingResume(),Is.True);
        var converted=board.tokens.Single(t=>t.isHomeTeam==home && t.jerseyNumber==number);int children=converted.transform.childCount;
        Assert.That(replica.Apply(after),Is.True);board.Display(replica.State,"Keeper outcome replica");
        Assert.That(board.match.HasGoalkeeperActionBlockingResume(),Is.False);Assert.That(converted.IsGoalKeeper,Is.True);Assert.That(converted.GetCurrentHex().coordinates,Is.EqualTo(emergency?nomineeHex:goalkeeperHex));
        Assert.That(converted.transform.childCount,Is.EqualTo(children),"Keeper appearance must use authored visual parts");
        var properties=new MaterialPropertyBlock();converted.GetComponent<MeshRenderer>().GetPropertyBlock(properties);
        var style=TokenKitCatalog.ResolveStyle(home?data.gameSettings.homeGKKit:data.gameSettings.awayGKKit);Assert.That(Vector4.Distance(properties.GetColor("_BaseColor"),style.bodyColor),Is.LessThan(1e-5f),"Published keeper kit must match within material float precision");
        Assert.That(board.match.gameData.events,Is.Empty);Assert.That(board.match.CaptureOnlineDecisionTiming(),Is.Null);Assert.That(board.match.TimingRollSequence,Is.Zero);
    }
}
