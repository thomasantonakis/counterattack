using System.Collections;
using System.Linq;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

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
