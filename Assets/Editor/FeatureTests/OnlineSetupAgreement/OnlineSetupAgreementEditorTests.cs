using System;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
[Category("OnlineSetupAgreement")]
public sealed class OnlineSetupAgreementEditorTests
{
    private OnlineMatchProtocol protocol;private OnlineSetupAuthority authority;
    private static GameSettings valid;
    private static readonly OnlineCompatibility Version=new("setup-1","rules-1","catalog-1");
    private static GameSettings Valid()
    {
        if(valid!=null)return OnlineSetupRules.Copy(valid);
        var s=new GameSettings{halfDuration=45,numberOfHalfs=2,tiebreaker="None",playerAssistance=2,matchType="Regular",squadSize="16",
            draft="Regular",gkDraft="Deal",referee="Random",weatherConditions="Clear",fixtureLighting="MidAfternoon",ballColor="White",
            includeTabletopia=true,includeTabletopiaGK=true,homeTeamName="Home club",awayTeamName="Away club"};
        var all=TokenKitCatalog.GetAllPresets();var outfield=all.Where(k=>!k.Id.StartsWith("gk")).OrderBy(k=>k.Id=="088"?0:k.Id=="021"?1:2).ToArray();
        var keepers=all.Where(k=>k.Id.StartsWith("gk")).ToArray();
        foreach(var hg in keepers)foreach(var ag in keepers.Where(k=>k!=hg))
        foreach(var h in outfield)foreach(var a in outfield.Where(k=>k!=h))
        {
            var similarity=TokenKitCatalog.GetSimilarityBreakdown(h.Id,a.Id);
            if(CreateNewGameManager.ValidateSelectedKits(h,a,hg,ag,similarity)!="")continue;
            s.homeKit=h.Id;s.awayKit=a.Id;s.homeGKKit=hg.Id;s.awayGKKit=ag.Id;
            valid=OnlineSetupRules.Normalize(s);Assert.That(OnlineSetupRules.Validate(valid),Is.Empty);return OnlineSetupRules.Copy(valid);
        }
        throw new AssertionException("Bundled catalog must offer four compatible kits");
    }
    private void Create(bool hostAway=false)
    {
        protocol=new OnlineMatchProtocol("setup-test","home","away",hostAway?"away":"home",Version);
        protocol.BindAuthenticatedAccount("home","h",Version);protocol.BindAuthenticatedAccount("away","a",Version);
        authority=new OnlineSetupAuthority(protocol,Valid());
    }
    private OnlineSetupRequest Request(string who,OnlineSetupIntent intent)
    {
        var s=authority.Capture();return new OnlineSetupRequest{matchId=s.matchId,sequence=(who=="h"?s.homeSequence:s.awaySequence)+1,revision=s.revision,intent=intent};
    }
    private void Accept(string who,OnlineSetupRequest request)=>Assert.That(authority.Submit(who,request).Status,Is.EqualTo(OnlineCommandStatus.Accepted));
    private void Ready(){Accept("h",Request("h",OnlineSetupIntent.Ready));Accept("a",Request("a",OnlineSetupIntent.Ready));}
    [Test] public void Agreement([Values(false,true)]bool hostAway,[Values("start","one-ready","team-edit","settings-edit","unready","duplicate","conflict","snapshot","rebind","disconnect","guest-settings","bad-payload")]string branch)
    {
        Create(hostAway);string host=hostAway?"a":"h",guest=hostAway?"h":"a";
        if(branch=="one-ready")
        {Accept(guest,Request(guest,OnlineSetupIntent.Ready));Assert.That(authority.Submit(host,Request(host,OnlineSetupIntent.Start)).Status,Is.EqualTo(OnlineCommandStatus.NotReady));return;}
        if(branch=="duplicate" || branch=="conflict")
        {
            var r=Request(guest,OnlineSetupIntent.Team);var before=authority.Capture();r.team=new OnlineSetupTeam{name="My own club",kit=guest=="h"?before.settings.homeKit:before.settings.awayKit,goalkeeperKit=guest=="h"?before.settings.homeGKKit:before.settings.awayGKKit};
            Accept(guest,r);long revision=authority.Capture().revision;
            if(branch=="conflict")r.team.name="Different payload";
            Assert.That(authority.Submit(guest,r).Status,Is.EqualTo(branch=="conflict"?OnlineCommandStatus.SequenceConflict:OnlineCommandStatus.Duplicate));
            Assert.That(authority.Capture().revision,Is.EqualTo(revision));return;
        }
        if(branch=="snapshot")
        {var before=authority.Capture();before.settings.homeTeamName="Injected";Assert.That(authority.Capture().settings.homeTeamName,Is.EqualTo("Home club"));return;}
        if(branch=="guest-settings")
        {var r=Request(guest,OnlineSetupIntent.Settings);r.settings=Valid();r.settings.halfDuration=30;Assert.That(authority.Submit(guest,r).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));return;}
        if(branch=="bad-payload")
        {var r=Request(guest,OnlineSetupIntent.Ready);r.team=new OnlineSetupTeam{name="Injected"};Assert.That(authority.Submit(guest,r).Status,Is.EqualTo(OnlineCommandStatus.InvalidCommand));return;}
        Ready();var ready=authority.Capture();Assert.That(ready.homeReady && ready.awayReady,Is.True);
        if(branch=="start")
        {Assert.That(authority.Submit(guest,Request(guest,OnlineSetupIntent.Start)).Status,Is.EqualTo(OnlineCommandStatus.WrongTeam));Accept(host,Request(host,OnlineSetupIntent.Start));Assert.That(authority.Capture().phase,Is.EqualTo("draft-ready"));return;}
        if(branch=="team-edit")
        {
            var r=Request(guest,OnlineSetupIntent.Team);r.team=new OnlineSetupTeam{name="  Updated club  ",kit=guest=="h"?ready.settings.homeKit:ready.settings.awayKit,goalkeeperKit=guest=="h"?ready.settings.homeGKKit:ready.settings.awayGKKit};
            Accept(guest,r);r.team.name="Mutated input";var after=authority.Capture();
            Assert.That(guest=="h"?after.settings.homeTeamName:after.settings.awayTeamName,Is.EqualTo("Updated club"));
            Assert.That(guest=="h"?after.settings.awayTeamName:after.settings.homeTeamName,Is.EqualTo(guest=="h"?ready.settings.awayTeamName:ready.settings.homeTeamName));
        }
        if(branch=="settings-edit")
        {
            var r=Request(host,OnlineSetupIntent.Settings);r.settings=Valid();r.settings.halfDuration=30;r.settings.homeTeamName="Spoofed";r.settings.awayKit="unknown";
            Accept(host,r);r.settings.halfDuration=60;var after=authority.Capture();Assert.That(after.settings.halfDuration,Is.EqualTo(30));
            Assert.That(after.settings.homeTeamName,Is.EqualTo(ready.settings.homeTeamName));Assert.That(after.settings.awayKit,Is.EqualTo(ready.settings.awayKit));
        }
        if(branch=="unready")Accept(guest,Request(guest,OnlineSetupIntent.Unready));
        if(branch=="rebind")
        {protocol.BindAuthenticatedAccount(hostAway?"home":"away","returning",Version);Assert.That(authority.Submit(guest,Request(guest,OnlineSetupIntent.Ready)).Status,Is.EqualTo(OnlineCommandStatus.UnknownConnection));}
        if(branch=="disconnect")
        {protocol.Disconnect(guest);Assert.That(authority.Submit(host,Request(host,OnlineSetupIntent.Start)).Status,Is.EqualTo(OnlineCommandStatus.Disconnected));}
        var final=authority.Capture();Assert.That(final.homeReady && final.awayReady,Is.False);
        Assert.That(final.phase,Is.EqualTo("setup"));Assert.That(final.revision,Is.GreaterThan(ready.revision));
    }
    [TestCase("unknown")][TestCase("wrong-match")][TestCase("stale")][TestCase("bad-kit")][TestCase("same-name")][TestCase("long-name")][TestCase("frozen")]
    public void RejectsBoundary(string branch)
    {
        Create();var r=Request("a",OnlineSetupIntent.Ready);var expected=OnlineCommandStatus.IllegalAction;string connection="a";
        if(branch=="unknown"){connection="intruder";expected=OnlineCommandStatus.UnknownConnection;}
        if(branch=="wrong-match"){r.matchId="other";expected=OnlineCommandStatus.WrongMatch;}
        if(branch=="stale"){Accept("h",Request("h",OnlineSetupIntent.Ready));expected=OnlineCommandStatus.StaleRevision;}
        if(branch=="frozen"){Ready();Accept("h",Request("h",OnlineSetupIntent.Start));r=Request("a",OnlineSetupIntent.Unready);expected=OnlineCommandStatus.NotReady;}
        if(branch is "bad-kit" or "same-name" or "long-name")
        {r.intent=OnlineSetupIntent.Team;r.team=new OnlineSetupTeam{name=branch=="same-name"?"Home club":branch=="long-name"?new string('x',21):"Other",kit=branch=="bad-kit"?"unknown":Valid().awayKit,goalkeeperKit=Valid().awayGKKit};}
        var before=authority.Capture();Assert.That(authority.Submit(connection,r).Status,Is.EqualTo(expected));
        Assert.That(authority.Capture().revision,Is.EqualTo(before.revision));Assert.That(authority.Capture().settings.awayTeamName,Is.EqualTo(before.settings.awayTeamName));
    }
    [TestCase("duration")][TestCase("halves")][TestCase("assistance")][TestCase("extra-time")][TestCase("pool")][TestCase("snow")][TestCase("draft")][TestCase("referee")]
    public void RejectsInvalidSharedSetting(string field)
    {
        Create();var r=Request("h",OnlineSetupIntent.Settings);r.settings=Valid();
        switch(field){case "duration":r.settings.halfDuration=0;break;case "halves":r.settings.numberOfHalfs=3;break;case "assistance":r.settings.playerAssistance=8;break;
            case "extra-time":r.settings.halfDuration=30;r.settings.tiebreaker="Extra Time";break;case "pool":r.settings.includeTabletopia=false;break;
            case "snow":r.settings.weatherConditions="Snow";break;case "draft":r.settings.draft="Remote code";break;case "referee":r.settings.referee="Unknown";break;}
        Assert.That(authority.Submit("h",r).Status,Is.EqualTo(OnlineCommandStatus.IllegalAction));Assert.That(authority.Capture().revision,Is.Zero);
    }
    [TestCase("capture")][TestCase("blank")][TestCase("same-name")][TestCase("bound-confirm")][TestCase("guest-unbound")]
    public void AuthoredCreateGameSnapshot(string branch)
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/CreateNewGameScene.unity");var random=UnityEngine.Random.state;
        try
        {
            var ui=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CreateNewGameManager>(true)).Single();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(CreateNewGameManager).GetMethod("SetDropDownOptions",flags).Invoke(ui,null);
            typeof(CreateNewGameManager).GetMethod("InitializeKitSelectionUi",flags).Invoke(ui,null);
            ui.halfDurationSlider.minValue=15;ui.halfDurationSlider.maxValue=60;ui.halfDurationSlider.value=45;
            ui.homeTeamInputField.text=branch=="blank"?"":"Home club";ui.awayTeamInputField.text=branch=="same-name"?"Home club":"Away club";
            ui.refereeDropdown.SetValueWithoutNotify(0);
            UnityEngine.Random.InitState(517);int expected=UnityEngine.Random.Range(0,int.MaxValue);UnityEngine.Random.InitState(517);
            bool captured=ui.TryCaptureOnlineSetupSettings(out var settings,out var error);
            Assert.That(captured,Is.EqualTo(branch!="blank" && branch!="same-name"),error);
            if(captured){Assert.That(settings.referee,Is.EqualTo("Random"));Assert.That(settings.homeTeamName,Is.EqualTo("Home club"));Assert.That(settings.halfDuration,Is.EqualTo(45));}
            Assert.That(UnityEngine.Random.Range(0,int.MaxValue),Is.EqualTo(expected),"capturing setup must not draw guest referee randomness");
            var activeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            if(branch=="bound-confirm")
            {
                GameSettings sent=null;ui.BindOnlineSetupConfirmation(value=>sent=value);
                ui.SaveGameSettingsToJson();Assert.That(sent,Is.Not.Null);Assert.That(sent.referee,Is.EqualTo("Random"));
                Assert.That(sent.homeTeamName,Is.EqualTo("Home club"));ui.UnbindOnlineSetupConfirmation();
            }
            if(branch=="guest-unbound")
            {
                using var guest=new OnlineRoomSession("setup-save-guard",true);ui.SaveGameSettingsToJson();
            }
            Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle,Is.EqualTo(activeScene),"setup confirmation cannot start a local Draft scene");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Random.state=random;}
    }
}
