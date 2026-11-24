using System.Collections;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class OnlineFullRoomSceneFixture
{
    public IEnumerator RunCommittedShootoutTargetPublication()
    {
        yield return RunShootoutStagingWithSwappedTokens(online: true);
        Time.timeScale = 1f;
        var data = OnlineRoomSession.Current.CaptureInitialData();
        var protocol = NativePresentationProtocol();
        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var authority = new OnlineGroundPassAuthority(board.match, protocol);
        var marker = board.match.ball.CommittedGoalTarget;
        Assert.That(marker, Is.Not.Null, "The shootout stages a committed target before the shot roll");
        Vector3Int coordinates = marker.coordinates;
        var committed = authority.Capture();
        Assert.That(committed.hexPreviews.Count(preview => preview.committedGoal), Is.EqualTo(1));
        Assert.That(committed.hexPreviews.Where(preview => preview.x == coordinates.x && preview.z == coordinates.z)
            .Select(preview => preview.committedGoal), Is.EqualTo(new[] { true }));

        board.match.ball.transform.position += Vector3.right * .1f;
        var cleared = authority.Capture();
        Assert.That(board.match.ball.CommittedGoalTarget, Is.Null);
        pause.Dispose();
        pause = null;
        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var replica = CreateResetReplica(protocol);
        var guestTarget = board.grid.GetHexCellAt(coordinates);
        float originalHeight = guestTarget.transform.position.y;
        ApplyPublication(replica, board, committed);
        Assert.That(guestTarget.IsCommittedGoalTarget, Is.True);
        Assert.That(guestTarget.transform.position.y, Is.GreaterThan(originalHeight));
        ApplyPublication(replica, board, cleared);
        Assert.That(guestTarget.IsCommittedGoalTarget, Is.False);
        Assert.That(guestTarget.transform.position.y, Is.EqualTo(originalHeight));
    }

    public IEnumerator RunRaisedGoalTargetPublication(string mode)
    {
        InitializeFixture();
        SuppressLegacyRoomRunner();
        bool mirror = mode is "snapshot" or "free-kick" or "reposition";
        var data = Data(16, mirror);
        data.gameSettings.playerAssistance = 1;
        var protocol = NativePresentationProtocol();
        yield return LoadNativeRoom(data, false, protocol.MatchId);

        var board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var match = board.match;
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(KickoffManager).GetMethod("DeactivateSetup", Private).Invoke(match.kickoffManager, null);
        match.teamInAttack = MatchManager.TeamInAttack.Home;
        match.attackHasPossession = true;
        match.currentState = MatchManager.GameState.EndOfMovementPhase;

        int side = mirror ? -1 : 1;
        var shot = match.shotManager;
        var header = match.highPassManager.headerManager;
        var movement = match.movementPhaseManager;
        bool isMovement = mode is "reposition" or "walk";
        var origin = mode == "header"
            ? board.cells.First(cell => cell.HeadingPaths?.Keys.Any(target => target.isInGoal == side) == true)
            : board.grid.GetHexCellAt(new Vector3Int((isMovement ? 18 : mode == "penalty" ? 14 : 13) * side, 0, 0));
        var attacker = board.tokens.Single(token => token.isHomeTeam && token.jerseyNumber == 9);
        Place(attacker, origin);
        match.ball.PlaceAtCell(origin);
        match.SetLastToken(attacker);
        match.UpdatePossessionAfterPass(origin);

        MonoBehaviour source = shot;
        string baseReason = isMovement ? "MovementGoal" : "CanShootFrom";
        string hoverReason = isMovement ? "MovementDestinationHover" : "ShotTargetHover";
        var goalCells = board.cells.Where(cell => cell.isInGoal != 0).ToArray();
        var originalHeights = goalCells.ToDictionary(cell => cell, cell => cell.transform.position.y);
        switch (mode)
        {
            case "header":
                source = header;
                header.isActivated = true;
                header.isWaitingForHeaderAtGoal = true;
                header.LiftHexesInGoal();
                break;
            case "shot-preview":
                shot.isAvailable = true;
                shot.PreviewShotCommit();
                break;
            case "shot":
                shot.StartShotProcess(attacker, "fullPower");
                break;
            case "snapshot":
                shot.StartShotProcess(attacker, "snapshot");
                Assert.That(shot.OnlineShotStage, Is.EqualTo("snapshot.select"));
                shot.CompleteDefenderMovement();
                break;
            case "free-kick-preview":
                match.currentState = MatchManager.GameState.FreeKickExecution;
                shot.PreviewFreeKickShotCommit();
                break;
            case "free-kick":
                shot.StartFreeKickShotProcess(attacker);
                break;
            case "penalty":
                shot.StartPenaltyShotProcess(attacker, origin);
                break;
            case "walk":
                source = movement;
                movement.ActivateMovementPhase();
                Assert.That(movement.TryExecuteOnlineMovement(OnlineMovementIntent.SelectPlayer, attacker), Is.True);
                break;
            case "reposition":
                source = movement;
                movement.ActivateMovementPhase();
                var defender = board.tokens.Single(token => !token.isHomeTeam && token.jerseyNumber == 5);
                Place(defender, board.grid.GetHexCellAt(new Vector3Int(18 * side, 0, 1)));
                movement.repositionWinner = attacker;
                movement.repositionLoser = defender;
                movement.StartCoroutine((IEnumerator)typeof(MovementPhaseManager).GetMethod("HandlePostTackleReposition", Private).Invoke(movement, null));
                break;
        }

        var raised = goalCells.Where(cell => cell.HighlightReason == baseReason
            && cell.transform.position.y > originalHeights[cell]).ToArray();
        Assert.That(raised, Is.Not.Empty, $"{mode} raises goal-line targets");
        var hovered = raised.First(cell => cell.isInGoal == side);
        var authority = new OnlineGroundPassAuthority(match, protocol);
        var initial = authority.Capture();
        var input = Object.FindAnyObjectByType<GameInputManager>();
        input.hoveredHex = hovered;
        source.GetType().GetMethod("OnHoverReceived", Private).Invoke(source, new object[] { null, hovered });
        Assert.That(hovered.HighlightReason, Is.EqualTo(hoverReason));
        var duringHover = authority.Capture();

        input.hoveredHex = null;
        switch (mode)
        {
            case "header":
                header.isActivated = false;
                header.isWaitingForHeaderAtGoal = false;
                typeof(HeaderManager).GetMethod("ClearHeaderAtGoalTargets", Private).Invoke(header, null);
                break;
            case "shot-preview":
            case "free-kick-preview":
                shot.CancelShotCommitPreview();
                break;
            case "walk":
            case "reposition":
                movement.ResetMovementPhase();
                break;
            default:
                typeof(ShotManager).GetMethod("ResetShotProcess", Private).Invoke(shot, null);
                break;
        }
        var withdrawn = authority.Capture();
        var coordinates = raised.Select(cell => cell.coordinates).ToArray();
        var hostHeights = raised.Select(cell => cell.transform.position.y).ToArray();
        var unsetHeights = raised.Select(cell => originalHeights[cell]).ToArray();

        yield return LoadPresentationGuest(data, protocol.MatchId, nativeRoom: true);
        board = Object.FindAnyObjectByType<OnlineRoomStartup>().Board;
        var replica = CreateResetReplica(protocol);
        var targets = coordinates.Select(position => board.grid.GetHexCellAt(position)).ToArray();
        var guestHeights = targets.Select(cell => cell.transform.position.y).ToArray();
        ApplyPublication(replica, board, initial);
        var raisedHeights = targets.Select(cell => cell.transform.position.y).ToArray();
        Assert.That(raisedHeights.Zip(guestHeights, (raisedHeight, originalHeight) => raisedHeight > originalHeight), Is.All.True);
        ApplyPublication(replica, board, duringHover);
        Assert.That(targets.Select(cell => cell.transform.position.y), Is.EqualTo(raisedHeights), $"{mode} loses a raised goal target on hover");
        Assert.That(targets.Select(cell => cell.HighlightReason), Is.All.EqualTo(baseReason));
        ApplyPublication(replica, board, withdrawn);
        Assert.That(targets.Select(cell => cell.transform.position.y), Is.EqualTo(guestHeights));
        Assert.That(targets.Select(cell => cell.HighlightReason), Has.None.EqualTo(baseReason));
        Assert.That(hostHeights, Is.EqualTo(unsetHeights), "The host restores the authored goal heights");

        void Place(PlayerToken token, HexCell destination)
        {
            if (destination.GetOccupyingToken() is PlayerToken occupant && occupant != token)
            {
                var free = board.cells.First(cell => !cell.isOutOfBounds && cell.GetOccupyingToken() == null
                    && Mathf.Abs(cell.coordinates.z) > 8);
                Place(occupant, free);
            }

            token.SetCurrentHex(destination);
            destination.isAttackOccupied = token.isHomeTeam;
            destination.isDefenseOccupied = !token.isHomeTeam;
            token.isAttacker = token.isHomeTeam;
            token.transform.position = destination.GetHexCenter() + Vector3.up * .2f;
        }
    }
}
