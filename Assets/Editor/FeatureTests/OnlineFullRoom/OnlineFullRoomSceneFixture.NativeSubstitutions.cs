using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed partial class OnlineFullRoomSceneFixture
{
    public IEnumerator RunNativeSubstitutionReview(string scenario)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        board.match.SetSubstitutionsAvailable(true, "Test stoppage");
        bool both = scenario != "solo";
        long homeSequence = 0;
        OnlineGroundPassState Publish()
        {
            var snapshot = pause.Capture(authority);
            board.Display(snapshot, "host");
            return snapshot;
        }
        OnlineCommandStatus Home(string action, long? revision = null)
        {
            var command = new OnlineMatchCommand(protocol.MatchId, ++homeSequence, revision ?? pause.Revision, "shared-pause", action);
            return pause.Submit("h", command).Status;
        }
        board.controls.Configure(OnlineTeam.Away);
        board.controls.Requested += message =>
        {
            var receipt = pause.Submit("a", message.ToCommand());
            Assert.That(receipt.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            board.controls.Reply(new OnlineGroundPassMessage { status = receipt.Status.ToString() });
            Publish();
        };
        Assert.That(Home(OnlineSharedPause.RequestAction), Is.EqualTo(OnlineCommandStatus.Accepted));
        Publish();
        var menu = Object.FindAnyObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        var roster = Object.FindAnyObjectByType<SubstitutionMenuView>(FindObjectsInactive.Include);
        Assert.That(menu.substitutionsButton.interactable, Is.True, "The other manager can open substitutions too");
        menu.substitutionsButton.onClick.Invoke();
        var opened = Publish();
        long reviewId = opened.substitutionReview.id;
        Assert.That(roster.gameObject.activeInHierarchy, Is.True);
        Assert.That(roster.GetRows(false)[0].outgoingDropdown.interactable, Is.True);
        Assert.That(roster.GetRows(true)[0].outgoingDropdown.interactable, Is.False);

        var away = roster.GetRows(false)[0];
        SelectJersey(away.outgoingDropdown, 2);
        SelectJersey(away.incomingDropdown, 13);
        if (both)
        {
            Assert.That(Home($"roster.review:pick:{reviewId}:0:2:0", opened.sharedPauseRevision), Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(Home($"roster.review:confirm:{reviewId}"), Is.EqualTo(OnlineCommandStatus.IllegalAction));
            Assert.That(Home($"roster.review:pick:{reviewId}:0:2:13"), Is.EqualTo(OnlineCommandStatus.Accepted));
            Assert.That(Home($"roster.review:pick:{reviewId}:1:2:14"), Is.EqualTo(OnlineCommandStatus.IllegalAction));
        }
        var ordered = Publish();
        Assert.That(away.incomingDropdown.captionText.text, Does.StartWith("13."));
        if (both) Assert.That(roster.GetRows(true)[0].incomingDropdown.captionText.text, Does.StartWith("13."));
        Assert.That(Home(OnlineSharedPause.ResumeAction), Is.EqualTo(OnlineCommandStatus.NotReady));
        Assert.That(roster.confirmButton.interactable, Is.True);
        roster.confirmButton.onClick.Invoke();
        var firstConfirmed = Publish();
        OnlineGroundPassState cleared = null;
        if (both)
        {
            Assert.That(firstConfirmed.substitutionReview.awayConfirmed, Is.True);
            Assert.That(roster.gameObject.activeInHierarchy, Is.True);
            Assert.That(roster.confirmButton.interactable, Is.False);
            Assert.That(board.match.GetSubstitutionsUsed(false), Is.Zero, "Both ordered teams must confirm before anything moves");
            if (scenario == "cancel")
            {
                Assert.That(Home($"roster.review:pick:{reviewId}:0:0:0"), Is.EqualTo(OnlineCommandStatus.Accepted));
                cleared = Publish();
            }
            Assert.That(Home($"roster.review:{(scenario == "cancel" ? "back" : "confirm")}:{reviewId}"),
                Is.EqualTo(OnlineCommandStatus.Accepted));
        }
        var completed = Publish();
        Assert.That(completed.substitutionReview, Is.Null);
        Assert.That(completed.isSharedPaused, Is.True);
        Assert.That(roster.gameObject.activeSelf, Is.False);
        Assert.That(board.match.GetSubstitutionsUsed(false), Is.EqualTo(1));
        Assert.That(board.match.GetSubstitutionsUsed(true), Is.EqualTo(scenario == "both" ? 1 : 0));
        Assert.That(board.tokens.Single(token => !token.isHomeTeam && token.jerseyNumber == 13).isPlaying, Is.True);

        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        board.controls.Configure(OnlineTeam.Home);
        roster = Object.FindAnyObjectByType<SubstitutionMenuView>(FindObjectsInactive.Include);
        var replica = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        ApplyPublication(replica, board, opened);
        Assert.That(roster.gameObject.activeInHierarchy, Is.True);
        Assert.That(roster.GetRows(true)[0].outgoingDropdown.interactable, Is.True);
        Assert.That(roster.GetRows(false)[0].outgoingDropdown.interactable, Is.False);
        OnlineGroundPassMessage choice = null;
        board.controls.Requested += message => choice = message;
        SelectJersey(roster.GetRows(true)[0].outgoingDropdown, 3);
        Assert.That(choice?.actionId, Is.EqualTo($"roster.review:pick:{reviewId}:0:3:0"));
        board.controls.Reply(new OnlineGroundPassMessage { status = "Accepted" });
        ApplyPublication(replica, board, ordered);
        Assert.That(roster.GetRows(false)[0].incomingDropdown.captionText.text, Does.StartWith("13."));
        if (both)
        {
            ApplyPublication(replica, board, firstConfirmed);
            Assert.That(roster.gameObject.activeInHierarchy, Is.True);
            Assert.That(roster.confirmButton.interactable, Is.True);
        }
        if (cleared != null)
        {
            ApplyPublication(replica, board, cleared);
            Assert.That(roster.confirmButton.interactable, Is.False);
            Assert.That(roster.backButton.interactable, Is.True, "The manager who cleared their draft can finish withdrawing it");
            roster.backButton.onClick.Invoke();
            Assert.That(choice?.actionId, Is.EqualTo($"roster.review:back:{reviewId}"));
        }
        ApplyPublication(replica, board, completed);
        Assert.That(roster.gameObject.activeSelf, Is.False);
        Assert.That(board.tokens.Single(token => !token.isHomeTeam && token.jerseyNumber == 13).isPlaying, Is.True);
    }

    private static OnlineMatchProtocol NativePresentationProtocol()
    {
        var version = new OnlineCompatibility("ground-wire-v7", "ground-rules-v1", OnlineFullRoomBootstrap.Catalog);
        var protocol = new OnlineMatchProtocol("native-room-scene", "home", "away", "away", version);
        protocol.BindAuthenticatedAccount("home", "h", version);
        protocol.BindAuthenticatedAccount("away", "a", version);
        return protocol;
    }

    private static void ApplyPublication(OnlineGroundPassReplica replica, OnlineGroundPassBoard board, OnlineGroundPassState state)
    {
        var wire = new OnlineGroundPassWire();
        OnlineGroundPassMessage decoded = null;
        foreach (var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage { kind = "state", matchId = state.matchId, state = state }))
            decoded = wire.Receive(packet, packet.Length) ?? decoded;
        Assert.That(replica.Apply(decoded?.state), Is.True);
        board.Display(replica.State, "guest");
    }

    private static void SelectJersey(TMP_Dropdown dropdown, int jersey)
    {
        int option = dropdown.options.FindIndex(item => item.text.StartsWith(jersey + ". "));
        Assert.That(option, Is.GreaterThanOrEqualTo(0), $"Jersey {jersey} must be selectable");
        dropdown.value = option;
    }
}
