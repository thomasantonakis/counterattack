using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class OnlineFullRoomSceneFixture
{
    public IEnumerator RunNativeShootoutOrder()
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        var manager = board.match.OnlineShootoutManager;
        board.match.gameData.penaltyShootoutSetup = new PenaltyShootoutSetup { homeTakesFirstPenalty = true };
        manager.ShowOrderSelection();
        Assert.That(manager.OnlineOrderSelection, Is.True);

        OnlineGroundPassState Publish()
        {
            var snapshot = pause.Capture(authority);
            board.Display(snapshot, "host");
            return snapshot;
        }
        OnlineCommandStatus Home(string action)
        {
            var command = new OnlineMatchCommand(protocol.MatchId, pause.LastAcceptedSequence(OnlineTeam.Home) + 1,
                pause.Revision, "shootout-order", action);
            return pause.Submit("h", command).Status;
        }

        board.controls.Configure(OnlineTeam.Away);
        string awayAction = null;
        board.controls.Requested += message =>
        {
            awayAction = message.actionId;
            var receipt = pause.Submit("a", message.ToCommand());
            Assert.That(receipt.Status, Is.EqualTo(OnlineCommandStatus.Accepted));
            board.controls.Reply(new OnlineGroundPassMessage { status = receipt.Status.ToString() });
            Publish();
        };
        var opened = Publish();
        var panel = Object.FindAnyObjectByType<PenaltyShootoutOrderPanelView>();
        Assert.That(panel, Is.Not.Null);
        yield return null;
        DragShootoutRow(panel, home: true);
        Assert.That(awayAction, Is.Null, "The Away manager cannot reorder Home");

        var homeRows = panel.GetRows(true);
        string homeSwap = $"shootout.swap:{homeRows[0].Token.jerseyNumber}:{homeRows[1].Token.jerseyNumber}";
        Assert.That(Home(homeSwap), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Home("shootout.ready"), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Home(homeSwap), Is.EqualTo(OnlineCommandStatus.IllegalAction));
        var homeConfirmed = Publish();
        Assert.That(homeConfirmed.shootout.homeOrder[0], Is.EqualTo(opened.shootout.homeOrder[1]));
        Assert.That(panel.gameObject.activeInHierarchy, Is.True);
        Assert.That(panel.startButton.interactable, Is.True);
        Assert.That(manager.isActive, Is.False);

        // Away can finish a drag begun before Home's reorder and confirmation arrived.
        board.Display(opened, "host");
        DragShootoutRow(panel, home: false);
        Assert.That(awayAction, Does.StartWith("shootout.swap:"));
        var awayEdited = Publish();
        Assert.That(awayEdited.shootout.awayOrder[0], Is.EqualTo(opened.shootout.awayOrder[1]));
        Assert.That(awayEdited.shootout.homeReady, Is.True, "Editing Away preserves the Home confirmation");
        Assert.That(awayEdited.shootout.awayReady, Is.False);
        Assert.That(manager.isActive, Is.False);
        panel.startButton.onClick.Invoke();
        var started = Publish();
        Assert.That(awayAction, Is.EqualTo("shootout.ready"));
        Assert.That(started.shootout.homeReady && started.shootout.awayReady, Is.True);
        Assert.That(started.shootout.active, Is.True);
        Assert.That(panel.gameObject.activeSelf, Is.False);

        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        board.controls.Configure(OnlineTeam.Home);
        var replica = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        string guestAction = null;
        board.controls.Requested += message => guestAction = message.actionId;
        ApplyPublication(replica, board, opened);
        panel = Object.FindAnyObjectByType<PenaltyShootoutOrderPanelView>();
        Assert.That(panel, Is.Not.Null);
        yield return null;
        DragShootoutRow(panel, home: true);
        Assert.That(guestAction, Is.EqualTo(homeSwap));
        board.controls.Reply(new OnlineGroundPassMessage { status = "Accepted" });
        panel.startButton.onClick.Invoke();
        Assert.That(guestAction, Is.EqualTo("shootout.ready"));
        board.controls.Reply(new OnlineGroundPassMessage { status = "Accepted" });

        ApplyPublication(replica, board, homeConfirmed);
        Assert.That(panel.homeHeaderText.text, Does.EndWith("Confirmed"));
        Assert.That(panel.startButton.interactable, Is.False);
        guestAction = null;
        DragShootoutRow(panel, home: true);
        Assert.That(guestAction, Is.Null, "A confirmed order is locked on its owner's screen");
        ApplyPublication(replica, board, awayEdited);
        Assert.That(OnlineRollPublication.ActorId(panel.GetRows(false)[0].Token), Is.EqualTo(awayEdited.shootout.awayOrder[0]));
        Assert.That(panel.gameObject.activeInHierarchy, Is.True);
        ApplyPublication(replica, board, started);
        Assert.That(panel.gameObject.activeSelf, Is.False);
    }

    public IEnumerator RunNativeShootoutRollsAndPause(bool homePauses)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        var data = Data(16, false);
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        pause = new OnlineSharedPause(protocol, board.match);
        var manager = board.match.OnlineShootoutManager;
        board.match.gameData.penaltyShootoutSetup = new PenaltyShootoutSetup { homeTakesFirstPenalty = true };
        manager.ShowOrderSelection();

        OnlineCommandStatus Control(bool home, string action)
        {
            var team = home ? OnlineTeam.Home : OnlineTeam.Away;
            var command = new OnlineMatchCommand(protocol.MatchId, pause.LastAcceptedSequence(team) + 1,
                pause.Revision, action.StartsWith("shootout.") ? "shootout-order" : "shared-pause", action);
            return pause.Submit(home ? "h" : "a", command).Status;
        }

        Assert.That(Control(true, "shootout.ready"), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(Control(false, "shootout.ready"), Is.EqualTo(OnlineCommandStatus.Accepted));
        Time.timeScale = 8f;
        var observing = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        float deadline = Time.realtimeSinceStartup + 15f;
        while (!board.match.shotManager.isWaitingForShotRoll && Time.realtimeSinceStartup < deadline)
        {
            var staging = pause.Capture(authority);
            Assert.That(observing.Apply(staging), Is.True, $"Guest accepts shootout staging frame {staging.publicationSequence}");
            yield return null;
        }
        var shooter = pause.Capture(authority);
        Assert.That(shooter.legalActions, Does.Contain(OnlineGroundPassAuthority.ShotRollAction));
        Assert.That(shooter.rollPanel, Is.Not.Null);
        Assert.That(Control(homePauses, OnlineSharedPause.RequestAction), Is.EqualTo(OnlineCommandStatus.Accepted));
        var paused = pause.Capture(authority);
        Assert.That(paused.keeperActionBlocked, Is.False, "The resting goalkeeper is intentionally off the pitch during a shootout");
        Assert.That(Control(!homePauses, OnlineSharedPause.ResumeAction), Is.EqualTo(OnlineCommandStatus.WrongTeam));
        Assert.That(Control(homePauses, OnlineSharedPause.ResumeAction), Is.EqualTo(OnlineCommandStatus.Accepted));

        manager.currentShooter.shooting = 5;
        manager.currentDefendingGoalkeeper.saving = 5;
        var shot = board.match.shotManager;
        yield return shot.StartShotRoll(new RollInputOverride { hasOverride = true, roll = 4 });
        deadline = Time.realtimeSinceStartup + 10f;
        while (!shot.isWaitingForGKDiceRoll && Time.realtimeSinceStartup < deadline) yield return null;
        var save = pause.Capture(authority);
        Assert.That(save.legalActions, Does.Contain(OnlineGroundPassAuthority.ShotSaveAction));
        Assert.That(save.rollPanel, Is.Not.Null);
        Assert.That(save.rollPanel.labels, Has.Some.Contains("goalkeeper").IgnoreCase);

        for (int seed = 0; seed < 10000; seed++)
        {
            Random.InitState(seed);
            var dice = board.match.helperFunctions.DiceRoll();
            if (dice._roll != 2 || dice._isJackPot) continue;
            Random.InitState(seed);
            break;
        }
        var saveCommand = new OnlineMatchCommand(protocol.MatchId, protocol.LastAcceptedSequence(OnlineTeam.Away) + 1,
            save.revision, save.decisionId, OnlineGroundPassAuthority.ShotSaveAction);
        Assert.That(authority.Submit("a", saveCommand).Status, Is.EqualTo(OnlineCommandStatus.Accepted));
        var result = pause.Capture(authority);
        Assert.That(result.rollPanel?.resultId, Is.GreaterThan(0));
        Assert.That(Control(homePauses, OnlineSharedPause.RequestAction), Is.EqualTo(OnlineCommandStatus.Accepted));
        var pausedResult = pause.Capture(authority);
        Assert.That(pausedResult.rollPanel.resultId, Is.EqualTo(result.rollPanel.resultId));
        Assert.That(Control(homePauses, OnlineSharedPause.ResumeAction), Is.EqualTo(OnlineCommandStatus.Accepted));
        string acknowledge = $"roll.dismiss:{result.rollPanel.resultId}";
        Assert.That(Control(true, acknowledge), Is.EqualTo(OnlineCommandStatus.Accepted));
        Assert.That(pause.Capture(authority).rollPanel?.resultId, Is.EqualTo(result.rollPanel.resultId));
        Assert.That(Control(false, acknowledge), Is.EqualTo(OnlineCommandStatus.Accepted));
        deadline = Time.realtimeSinceStartup + 15f;
        while (!shot.isWaitingForShotRoll && Time.realtimeSinceStartup < deadline)
        {
            var next = pause.Capture(authority);
            Assert.That(observing.Apply(next), Is.True, $"Guest accepts shootout transition frame {next.publicationSequence}");
            yield return null;
        }
        Assert.That(manager.homeAttempts, Is.EqualTo(1));
        Assert.That(shot.isWaitingForShotRoll && !manager.currentShooter.isHomeTeam, Is.True);
        shot.StartCoroutine(shot.StartShotRoll(new RollInputOverride { hasOverride = true, roll = 1 }));
        deadline = Time.realtimeSinceStartup + 15f;
        while (!shot.isWaitingForShotRoll && Time.realtimeSinceStartup < deadline)
        {
            var next = pause.Capture(authority);
            Assert.That(observing.Apply(next), Is.True, $"Guest accepts the missed kick and restaging frame {next.publicationSequence}");
            if (next.rollPanel?.resultId > 0)
            {
                string dismiss = $"roll.dismiss:{next.rollPanel.resultId}";
                Assert.That(Control(true, dismiss), Is.EqualTo(OnlineCommandStatus.Accepted));
                Assert.That(Control(false, dismiss), Is.EqualTo(OnlineCommandStatus.Accepted));
            }
            yield return null;
        }
        Assert.That(manager.awayAttempts, Is.EqualTo(1));
        Assert.That(shot.isWaitingForShotRoll && manager.currentShooter.isHomeTeam, Is.True);
        pause.Dispose();
        pause = null;
        Time.timeScale = 1f;

        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        board.controls.Configure(OnlineTeam.Home);
        var replica = new OnlineGroundPassReplica(protocol.MatchId, board.tokens.Select(OnlineRollPublication.ActorId), OnlineFullRoomBootstrap.Catalog);
        foreach (var snapshot in new[] { shooter, paused, save, result, pausedResult })
        {
            ApplyPublication(replica, board, snapshot);
            yield return null;
            var duel = Object.FindAnyObjectByType<DuelResolutionPanelController>();
            Assert.That(duel.visuals.activeInHierarchy, Is.True);
            Assert.That(duel.visuals.GetComponent<CanvasGroup>().alpha, Is.EqualTo(snapshot.isSharedPaused ? 0 : 1));
            if (!snapshot.isSharedPaused)
                Assert.That(duel.visuals.GetComponentsInChildren<DuelDieView>().Any(die => die.IsAnimating),
                    Is.EqualTo(snapshot.rollPanel.resultId == 0));
        }
    }

    private static void DragShootoutRow(PenaltyShootoutOrderPanelView panel, bool home)
    {
        var rows = panel.GetRows(home);
        var canvas = panel.GetComponentInParent<Canvas>();
        var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 Center(PenaltyShootoutOrderRowView row)
        {
            var rect = (RectTransform)row.transform;
            return RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(rect.rect.center));
        }

        Canvas.ForceUpdateCanvases();
        Assert.That(EventSystem.current, Is.Not.Null);
        var pointer = new PointerEventData(EventSystem.current) { position = Center(rows[0]) };
        int sibling = rows[0].transform.GetSiblingIndex();
        rows[0].OnBeginDrag(pointer);
        pointer.position = Center(rows[1]);
        rows[0].OnDrag(pointer);
        rows[0].OnEndDrag(pointer);
        Assert.That(rows[0].transform.GetSiblingIndex(), Is.EqualTo(sibling));
    }
}
