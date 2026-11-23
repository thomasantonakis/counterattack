using System;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
[Category("OnlineSetupView")]
public sealed class OnlineSetupViewEditorTests
{
    [Test] public void AuthoredView([Values(false,true)]bool hostAway,[Values(false,true)]bool localHost,[Values("ownership","confirm","edit","invalid","remote-update","start","focused-name","mode-round-trip")]string branch)
    {
        var scene=EditorSceneManager.OpenPreviewScene("Assets/Scenes/CreateNewGameScene.unity");
        try
        {
            var ui=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CreateNewGameManager>(true)).Single();
            var view=ui.GetComponent<OnlineSetupView>();Assert.That(view,Is.Not.Null);Assert.That(view.status,Is.Not.Null);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            foreach(var method in new[]{"SetDropDownOptions","ConfigurePlayerAssistanceDropdown","InitializeInternationalTeamSelectionUi","InitializeKitSelectionUi"})typeof(CreateNewGameManager).GetMethod(method,flags).Invoke(ui,null);
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
            if (branch=="focused-name")
            {
                var allowInput = typeof(TMPro.TMP_InputField).GetField("m_AllowInput", flags);
                Assert.That(allowInput, Is.Not.Null);
                allowInput.SetValue(own, true);
                own.text = "My Club ";
                Assert.That(own.text, Is.EqualTo("My Club "), "A live acknowledgement must preserve an in-progress word boundary");
                var published = authority.Capture().settings;
                Assert.That(team == OnlineTeam.Home ? published.homeTeamName : published.awayTeamName, Is.EqualTo("My Club"));
                own.text = "My Club FC";
                Assert.That(own.text, Is.EqualTo("My Club FC"));
                allowInput.SetValue(own, false);
                return;
            }
            if (branch=="mode-round-trip")
            {
                var regular = authority.Capture().settings;
                var international = OnlineSetupRules.Copy(regular);
                international.matchType = international.draft = international.gkDraft = "International";
                international.squadSize = "18";
                international.homeTeamName = "France";
                international.awayTeamName = "Argentina";
                international.homeKit = "France";
                international.awayKit = "Argentina";
                view.Unbind();
                ui.ApplyOnlineSetupSettings(international);
                international = ui.CaptureOnlineSetupDraft();
                Assert.That(international.matchType, Is.EqualTo("International"));
                ui.ApplyOnlineSetupSettings(regular);
                view.Configure(client);
                client.SetTransportAvailable(false);
                own.text = team == OnlineTeam.Home ? "Argentina" : "France";
                Assert.That(client.Dirty, Is.True);
                string hostConnection = hostAway ? "a" : "h";
                void Switch(GameSettings settings)
                {
                    var before = authority.Capture();
                    var request = new OnlineSetupRequest
                    {
                        matchId = before.matchId, revision = before.revision, intent = OnlineSetupIntent.Edit,
                        sequence = (hostAway ? before.awaySequence : before.homeSequence) + 1,
                        settings = settings, team = new OnlineSetupTeam
                        {
                            name = hostAway ? settings.awayTeamName : settings.homeTeamName,
                            kit = hostAway ? settings.awayKit : settings.homeKit,
                            goalkeeperKit = hostAway ? settings.awayGKKit : settings.homeGKKit
                        }
                    };
                    Assert.That(authority.Submit(hostConnection, request).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
                    Assert.That(client.Apply(authority.Capture()), Is.True);
                }
                Switch(international);
                Assert.That(ui.homeInternationalTeamDropdown.captionText.text, Is.EqualTo("France"));
                Assert.That(ui.awayInternationalTeamDropdown.captionText.text, Is.EqualTo("Argentina"));
                Assert.That(client.Dirty, Is.False);
                Switch(regular);
                var observed = ui.CaptureOnlineSetupDraft();
                Assert.That(observed.matchType, Is.EqualTo("Regular"));
                Assert.That(observed.homeTeamName, Is.EqualTo(regular.homeTeamName));
                Assert.That(observed.awayTeamName, Is.EqualTo(regular.awayTeamName));
                Assert.That(new[] { observed.homeKit, observed.awayKit, observed.homeGKKit, observed.awayGKKit },
                    Is.EqualTo(new[] { regular.homeKit, regular.awayKit, regular.homeGKKit, regular.awayGKKit }));
                return;
            }
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
