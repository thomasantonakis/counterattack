using System;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
[Category("OnlineSetupView")]
public sealed class OnlineSetupViewEditorTests
{
    [Test] public void AuthoredView([Values(false,true)]bool hostAway,[Values(false,true)]bool localHost,[Values("ownership","confirm","edit","invalid","remote-update","start")]string branch)
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/CreateNewGameScene.unity");
        try
        {
            var ui=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CreateNewGameManager>(true)).Single();
            var view=ui.GetComponent<OnlineSetupView>();Assert.That(view,Is.Not.Null);Assert.That(view.status,Is.Not.Null);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            foreach(var method in new[]{"SetDropDownOptions","ConfigurePlayerAssistanceDropdown","InitializeKitSelectionUi"})typeof(CreateNewGameManager).GetMethod(method,flags).Invoke(ui,null);
            ui.halfDurationSlider.minValue=15;ui.halfDurationSlider.maxValue=60;
            var version=new OnlineCompatibility("setup-view","rules","catalog");
            var protocol=new OnlineMatchProtocol("setup-view","home","away",hostAway?"away":"home",version);
            protocol.BindAuthenticatedAccount("home","h",version);protocol.BindAuthenticatedAccount("away","a",version);
            var authority=new OnlineSetupAuthority(protocol,OnlineSetupAgreementEditorTests.Valid());
            var team=localHost?protocol.HostTeam:protocol.HostTeam==OnlineTeam.Home?OnlineTeam.Away:OnlineTeam.Home;
            string connection=team==OnlineTeam.Home?"h":"a",other=team==OnlineTeam.Home?"a":"h";
            var client=new OnlineSetupClient(protocol.MatchId,team);client.Apply(authority.Capture());
            int sent=0,agreed=0;client.DraftAgreed+=_=>agreed++;
            client.Requested+=request=>{sent++;var reply=authority.Submit(connection,request);client.Receive(request.sequence,reply.Status,authority.Capture());};
            view.Configure(client);
            var own=team==OnlineTeam.Home?ui.homeTeamInputField:ui.awayTeamInputField;
            var opponent=team==OnlineTeam.Home?ui.awayTeamInputField:ui.homeTeamInputField;
            Assert.That(own.interactable,Is.True);Assert.That(opponent.interactable,Is.False);
            Assert.That(ui.halfDurationSlider.interactable,Is.EqualTo(localHost));Assert.That(view.status.gameObject.activeSelf,Is.True);
            Assert.That((team==OnlineTeam.Home?ui.homeGKKitDropdown:ui.awayGKKitDropdown).interactable,Is.True);
            Assert.That((team==OnlineTeam.Home?ui.awayKitDropdown:ui.homeKitDropdown).interactable,Is.False);
            Assert.That(ui.TryCaptureOnlineSetupSettings(out var selected,out var error),Is.True,error);Assert.That(selected.playerAssistance,Is.EqualTo(2));
            if(branch=="ownership")return;
            if(branch=="invalid")
            {
                own.text="";Assert.That(ui.createGameButton.interactable,Is.False);ui.SaveGameSettingsToJson();Assert.That(sent,Is.Zero);return;
            }
            if(branch=="remote-update")
            {
                own.text="My unsent selection";var state=authority.Capture();
                var request=new OnlineSetupRequest{matchId=state.matchId,sequence=1,revision=state.revision,intent=OnlineSetupIntent.Team,
                    team=new OnlineSetupTeam{name="Remote selection",kit=team==OnlineTeam.Home?state.settings.awayKit:state.settings.homeKit,goalkeeperKit=team==OnlineTeam.Home?state.settings.awayGKKit:state.settings.homeGKKit}};
                Assert.That(authority.Submit(other,request).Status,Is.EqualTo(OnlineCommandStatus.Accepted));client.Apply(authority.Capture());
                Assert.That(own.text,Is.EqualTo("My unsent selection"));Assert.That(opponent.text,Is.EqualTo("Remote selection"));return;
            }
            ui.SaveGameSettingsToJson();Assert.That(client.LocalReady,Is.True);Assert.That(sent,Is.EqualTo(1));
            if(branch=="confirm"){Assert.That(ui.createGameButton.interactable,Is.False);return;}
            if(branch=="edit")
            {
                own.text="Changed after ready";Assert.That(client.LocalReady,Is.False);Assert.That(own.text,Is.EqualTo("Changed after ready"));Assert.That(sent,Is.EqualTo(2));return;
            }
            var current=authority.Capture();var otherReady=new OnlineSetupRequest{matchId=current.matchId,sequence=1,revision=current.revision,intent=OnlineSetupIntent.Ready};
            Assert.That(authority.Submit(other,otherReady).Status,Is.EqualTo(OnlineCommandStatus.Accepted));client.Apply(authority.Capture());
            Assert.That(ui.createGameButton.interactable,Is.EqualTo(localHost));ui.SaveGameSettingsToJson();
            if(localHost){Assert.That(authority.Capture().phase,Is.EqualTo("draft-ready"));Assert.That(agreed,Is.EqualTo(1));}
            else{Assert.That(authority.Capture().phase,Is.EqualTo("setup"));Assert.That(sent,Is.EqualTo(1));}
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
