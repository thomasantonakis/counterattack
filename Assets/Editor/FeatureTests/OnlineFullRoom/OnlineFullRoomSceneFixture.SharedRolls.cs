using System;
using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed partial class OnlineFullRoomSceneFixture
{
    private IEnumerator RunSharedRollPresentation(OnlineGroundPassBoard board, OnlineMatchProtocol protocol,
        MatchManager.GameData data, bool timeout, bool nativeRoom = false)
    {
        var match = board.match;
        match.teamInAttack = MatchManager.TeamInAttack.Away;
        match.currentState = MatchManager.GameState.EndOfStandardPass;
        match.isClockRunning = true;
        typeof(MatchManager).GetMethod("AdvanceMatchTime",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(match, new object[] { 100000f });
        int continuations = 0;
        match.ResolveActionBeforeFinalThird(MatchManager.MatchActionKind.StandardPass, () => continuations++);
        var authority = new OnlineGroundPassAuthority(match, protocol);
        var awaiting = pause.Capture(authority);
        Assert.That(awaiting.stage, Is.EqualTo("stoppage.roll"));
        Assert.That(awaiting.rollPanel?.panel, Is.EqualTo(2));
        Assert.That(awaiting.rollPanel.resultId, Is.Zero);
        var roll = new OnlineMatchCommand(awaiting.matchId, 1, awaiting.revision, awaiting.decisionId, "stoppage.roll");
        Assert.That(authority.Submit("h", roll).Status, Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(authority.Submit("a", roll).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        var resolved = pause.Capture(authority);
        Assert.That(resolved.rollPanel?.resultId, Is.GreaterThan(0));
        Assert.That(match.stoppageActionsPanel.die.DisplayedRoll, Is.InRange(1, 6));
        int expectedRoll = match.stoppageActionsPanel.die.DisplayedRoll;
        Assert.That(continuations, Is.Zero);
        Assert.That(resolved.legalActions, Is.Empty);
        board.controls.Configure(OnlineTeam.Away);
        board.Display(resolved, "Shared stoppage");
        OnlineMatchCommand ack = null;
        void Local(OnlineGroundPassMessage message)
        {
            Assert.That(message.kind, Is.EqualTo("control"));
            ack = message.ToCommand();
            var reply = pause.Submit("a", ack);
            Assert.That(reply.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            board.controls.Reply(new OnlineGroundPassMessage { status = reply.Status.ToString() });
        }
        board.controls.Requested += Local;
        try
        {
            board.controls.Escape();
            Assert.That(ack, Is.Not.Null);
            Assert.That(board.rollPanels.stoppageOverlay.alpha, Is.Zero);
            Assert.That(match.stoppageActionsPanel.IsOpen, Is.True);
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(continuations, Is.Zero);
            Assert.That(pause.Submit("a", ack).Status, Is.EqualTo(OnlineCommandStatus.Duplicate));
            board.controls.Escape();
            Assert.That(pause.IsPaused, Is.False);
            if (!timeout)
            {
                var peerAck = new OnlineMatchCommand(resolved.matchId, 1, resolved.sharedPauseRevision, "shared-pause", ack.ActionId);
                Assert.That(pause.Submit("h", peerAck).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
                Assert.That(match.stoppageActionsPanel.IsOpen, Is.False);
            }

            float deadline = Time.realtimeSinceStartup + (timeout ? 5.8f : 0.75f);
            while (continuations == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(continuations, Is.EqualTo(1));
            Assert.That(match.stoppageActionsPanel.IsOpen, Is.False);
            var lateAck = new OnlineMatchCommand(resolved.matchId, 2, resolved.sharedPauseRevision, "shared-pause", ack.ActionId);
            Assert.That(pause.Submit("h", lateAck).Status, Is.EqualTo(OnlineCommandStatus.IllegalAction));
        }
        finally { board.controls.Requested -= Local; }
        var closed = pause.Capture(authority);
        Assert.That(closed.rollPanel, Is.Null);
        pause.Dispose(); pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom);
        board = nativeRoom ? Object.FindAnyObjectByType<OnlineRoomStartup>().Board : Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var replica = new OnlineGroundPassReplica(awaiting.matchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        foreach (var publication in new[] { awaiting, resolved, closed })
        {
            var wire = new OnlineGroundPassWire();
            OnlineGroundPassMessage decoded = null;
            foreach (var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage
                { kind = "state", matchId = publication.matchId, state = publication }))
                decoded = wire.Receive(packet, packet.Length) ?? decoded;
            Assert.That(replica.Apply(decoded?.state), Is.True);
            board.Display(replica.State, "Replica shared stoppage");
            var panel = board.rollPanels.stoppage;
            Assert.That(panel.visuals.activeInHierarchy, Is.EqualTo(publication != closed));
            if (publication == resolved) Assert.That(panel.die.DisplayedRoll, Is.EqualTo(expectedRoll));
        }
        Assert.That(board.match.TimingRollSequence, Is.Zero);
    }
    private IEnumerator RunSharedDuelOrHeader(OnlineGroundPassBoard board, OnlineMatchProtocol protocol,
        MatchManager.GameData data, bool header, bool nativeRoom = false)
    {
        var match = board.match;
        var view = board.rollPanels;
        var player = board.tokens.First(token => token.isHomeTeam && token.jerseyNumber == 9);
        var authority = new OnlineGroundPassAuthority(match, protocol);
        IEnumerator result;
        if (header)
        {
            var manager = match.highPassManager.headerManager;
            manager.attackerWillJump.Add(player);
            manager.defenderWillJump.Add(board.tokens.First(token => !token.isHomeTeam && token.jerseyNumber == 4));
            Assert.That(view.header.Begin(manager), Is.True);
            result = view.header.Finish(manager, player, "Header won", false);
        }
        else
        {
            Assert.That(view.duel.BeginSingle(match, () => true, "Roll Test", player, "Pace", "4+", true), Is.True);
            var die = match.ResolveGameplayDiceRoll("online_shared_panel", new RollInputOverride
                { hasOverride = true, roll = 4 }, player);
            result = view.duel.ResolveSingle(match, die, "Check passed");
        }
        board.StartCoroutine(result);
        float deadline = Time.realtimeSinceStartup + 0.5f;
        while (pause.Capture(authority).rollPanel?.resultId is null or 0 && Time.realtimeSinceStartup < deadline)
            yield return null;
        var published = pause.Capture(authority);
        Assert.That(published.rollPanel?.panel, Is.EqualTo(header ? 1 : 0));
        Assert.That(published.rollPanel.resultId, Is.GreaterThan(0));
        Assert.That(published.legalActions, Is.Empty);
        OnlineRollPanelFrame expandedFoul=null,recovery=null;
        float expectedRecoveryY=0;
        if(!header)
        {
            view.duel.SetExpandedLayout(true);
            expandedFoul=view.Capture(published.rollSequence);
            view.duel.SetLooseRecoveryLayout(true);
            recovery=view.Capture(published.rollSequence);
            expectedRecoveryY=((RectTransform)view.duel.looseRecoveryContent.transform).anchoredPosition.y;
        }
        pause.Dispose(); pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom);
        board = nativeRoom ? Object.FindAnyObjectByType<OnlineRoomStartup>().Board : Object.FindAnyObjectByType<OnlineFullRoomBootstrap>().board;
        var wire = new OnlineGroundPassWire();
        OnlineGroundPassMessage decoded = null;
        foreach (var packet in OnlineGroundPassWire.Encode(new OnlineGroundPassMessage
            { kind = "state", matchId = published.matchId, state = published }))
            decoded = wire.Receive(packet, packet.Length) ?? decoded;
        var replica = new OnlineGroundPassReplica(published.matchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        Assert.That(replica.Apply(decoded?.state), Is.True);
        board.Display(replica.State, "Shared roll panel");
        view = board.rollPanels;
        Assert.That(view.duel.visuals.activeInHierarchy, Is.EqualTo(!header));
        Assert.That(view.header.visuals.activeInHierarchy, Is.EqualTo(header));
        Assert.That(header ? view.header.outcome.text : view.duel.outcome.text, Is.EqualTo(header ? "Header won" : "Check passed"));
        if (!header)
        {
            Assert.That(view.duel.singlePlayer.die.DisplayedRoll, Is.EqualTo(4));
            float compactHeight=view.duel.panel.rect.height;
            view.Apply(expandedFoul,false);
            Assert.That(view.duel.foulChecks.activeInHierarchy,Is.True);
            Assert.That(view.duel.panel.rect.height,Is.GreaterThan(compactHeight));
            view.Apply(recovery,false);
            Assert.That(view.duel.looseRecoveryContent.activeInHierarchy,Is.True);
            Assert.That(((RectTransform)view.duel.looseRecoveryContent.transform).anchoredPosition.y,Is.EqualTo(expectedRecoveryY));
            Assert.That(view.duel.panel.rect.height,Is.GreaterThan(compactHeight));
            view.Apply(published.rollPanel,false);
            Assert.That(view.duel.panel.rect.height,Is.EqualTo(compactHeight));
        }
        Assert.That(board.match.TimingRollSequence, Is.Zero);
    }
}
