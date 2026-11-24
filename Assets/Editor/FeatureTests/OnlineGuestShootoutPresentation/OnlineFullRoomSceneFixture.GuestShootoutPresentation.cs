using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CounterAttack.Online;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class OnlineFullRoomSceneFixture
{
    public IEnumerator RunGuestShootoutPresentation(string check)
        => RunNativeShootoutRollsAndPause(homePauses: true, guestCheck: check);

    private static void VerifyGuestShootoutPortraits(OnlineGroundPassReplica replica, OnlineGroundPassBoard board,
        OnlineGroundPassState shooter, OnlineGroundPassState save, OnlineGroundPassState nextShooter)
    {
        var stats = board.match.matchStatsUI;
        Assert.That(stats, Is.Not.Null);
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        var input = Object.FindAnyObjectByType<GameInputManager>();
        var hover = typeof(GameInputManager).GetMethod("PresentOnlineHover", Private);
        ApplyPublication(replica, board, shooter);
        AssertContestants(shooter);
        var bystander = board.tokens.First(token => token.isPlaying && !token.IsGoalKeeper
            && OnlineRollPublication.ActorId(token) != shooter.shootout.shooter);
        hover.Invoke(input, new object[] { bystander, bystander.currentHex });
        AssertContestants(shooter);
        ApplyPublication(replica, board, save);
        AssertContestants(save);
        ApplyPublication(replica, board, nextShooter);
        Assert.That(nextShooter.shootout.shooter, Does.StartWith("away:"));
        AssertContestants(nextShooter);

        void AssertContestants(OnlineGroundPassState state)
        {
            var taker = board.tokens.Single(token => OnlineRollPublication.ActorId(token) == state.shootout.shooter);
            var keeper = board.tokens.Single(token => OnlineRollPublication.ActorId(token) == state.shootout.keeper);
            Assert.That(taker.isHomeTeam, Is.Not.EqualTo(keeper.isHomeTeam));
            Assert.That(CardTokens(taker.isHomeTeam), Is.EqualTo(new[] { taker }), "Shooter's team preview");
            Assert.That(CardTokens(keeper.isHomeTeam), Is.EqualTo(new[] { keeper }), "Saving goalkeeper's team preview");
        }

        IEnumerable<PlayerToken> CardTokens(bool home)
        {
            var field = typeof(MatchStatsUI).GetField(home ? "homeHoverCardAnchor" : "awayHoverCardAnchor", Private);
            var anchor = (RectTransform)field.GetValue(stats);
            return anchor.GetComponentsInChildren<PlayerCard>().Select(card => card.DisplayedToken)
                .Concat(anchor.GetComponentsInChildren<GoalkeeperCard>().Select(card => card.DisplayedToken));
        }
    }

    private static IEnumerator VerifyGuestShootoutRollDrag(OnlineGroundPassReplica replica, OnlineGroundPassBoard board,
        OnlineGroundPassState shooter, OnlineGroundPassState paused, OnlineGroundPassState save,
        OnlineGroundPassState betweenKicks, OnlineGroundPassState nextShooter)
    {
        var duel = Object.FindAnyObjectByType<DuelResolutionPanelController>();
        var handle = duel.visuals.GetComponentInChildren<DuelPanelDragHandle>(true);
        Assert.That(handle, Is.Not.Null);
        int guestCommands = 0;
        board.controls.Requested += _ => guestCommands++;
        ApplyPublication(replica, board, shooter);
        yield return null;
        Assert.That(duel.visuals.activeInHierarchy, Is.True);
        Assert.That(duel.IsOpen, Is.False, "Guest presentation does not enter the gameplay panel state");
        Vector2 initial = duel.Panel.anchoredPosition;
        Drag(PointerEventData.InputButton.Right, requireRaycast: true);
        Assert.That(duel.Panel.anchoredPosition, Is.EqualTo(initial), "Right drag stays available to the camera");
        Drag(PointerEventData.InputButton.Left, requireRaycast: true);
        Vector2 dragged = duel.Panel.anchoredPosition;
        Assert.That(Vector2.Distance(dragged, initial), Is.GreaterThan(1f), "Guest can drag the authored roll panel");

        ApplyPublication(replica, board, paused);
        Assert.That(duel.visuals.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
        Drag(PointerEventData.InputButton.Left, requireRaycast: false);
        AssertPosition();
        ApplyPublication(replica, board, save);
        Assert.That(duel.visuals.activeInHierarchy, Is.True);
        AssertPosition();
        ApplyPublication(replica, board, betweenKicks);
        Assert.That(duel.visuals.activeInHierarchy, Is.False, "The result closes before the opposite team's kick");
        ApplyPublication(replica, board, nextShooter);
        Assert.That(duel.visuals.activeInHierarchy, Is.True);
        AssertPosition();
        Assert.That(guestCommands, Is.Zero, "Dragging only changes the local position");

        void AssertPosition() => Assert.That(Vector2.Distance(duel.Panel.anchoredPosition, dragged), Is.LessThan(.01f));

        void Drag(PointerEventData.InputButton button, bool requireRaycast)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)handle.transform;
            var canvas = handle.GetComponentInParent<Canvas>();
            var eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = button,
                position = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.TransformPoint(rect.rect.center))
            };
            if (requireRaycast)
            {
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits, Is.Not.Empty, $"No UI raycast at {pointer.position} on {Screen.width}×{Screen.height}");
                pointer.pointerPressRaycast = hits[0];
                Assert.That(ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject), Is.SameAs(handle.gameObject),
                    "The guest can hit the authored drag handle");
            }
            ExecuteEvents.Execute(handle.gameObject, pointer, ExecuteEvents.beginDragHandler);
            pointer.position += Vector2.right * Mathf.Max(20f, Screen.width * .1f);
            ExecuteEvents.Execute(handle.gameObject, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(handle.gameObject, pointer, ExecuteEvents.endDragHandler);
        }
    }
}
