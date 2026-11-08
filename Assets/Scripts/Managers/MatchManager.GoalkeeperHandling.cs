using System;
using UnityEngine;

public enum GoalkeeperDeliveryKind { None, Kick, Header }
public enum GoalkeeperHandlingEligibility { None, OpponentTouch, TeammateHeader, Holding, DeliberateDrop }

public partial class MatchManager
{
    private PlayerToken handlingSource;
    private PlayerToken handlingOwner;
    private GoalkeeperDeliveryKind handlingDelivery;
    private GoalkeeperHandlingEligibility handlingEligibility;

    public GoalkeeperHandlingEligibility KeeperHandlingEligibility => handlingEligibility;
    public PlayerToken KeeperHandlingOwner => handlingOwner;

    public void BeginGoalkeeperBallDelivery(GoalkeeperDeliveryKind kind, PlayerToken source)
    {
        movementPhaseManager?.goalKeeperManager?.CancelDeferredPickup();
        handlingSource = source;
        handlingDelivery = kind;
        handlingOwner = null;
        handlingEligibility = GoalkeeperHandlingEligibility.None;
    }

    private void RecordGoalkeeperBallTouch(PlayerToken token)
    {
        if (handlingOwner == token) return;
        movementPhaseManager?.goalKeeperManager?.CancelDeferredPickup();
        handlingEligibility = token.IsGoalKeeper && handlingSource != null
            ? handlingSource.isHomeTeam != token.isHomeTeam
                ? GoalkeeperHandlingEligibility.OpponentTouch
                : handlingDelivery == GoalkeeperDeliveryKind.Header
                    ? GoalkeeperHandlingEligibility.TeammateHeader : GoalkeeperHandlingEligibility.None
            : GoalkeeperHandlingEligibility.None;
        handlingOwner = token;
        handlingSource = token;
        handlingDelivery = GoalkeeperDeliveryKind.None;
    }

    public bool CanGoalkeeperHandle(PlayerToken keeper)
        => keeper != null && keeper.IsGoalKeeper && handlingOwner == keeper
            && handlingEligibility is GoalkeeperHandlingEligibility.OpponentTouch
                or GoalkeeperHandlingEligibility.TeammateHeader or GoalkeeperHandlingEligibility.Holding;

    public bool CanOfferGoalkeeperPickup(PlayerToken keeper)
        => CanGoalkeeperHandle(keeper) && handlingEligibility != GoalkeeperHandlingEligibility.Holding
            && attackHasPossession && keeper.isAttacker && keeper.isPlaying && !keeper.isSentOff
            && !isMatchComplete && !isHalfTimeFlowRunning && !isHalfEndPendingAfterGoalFlow
            && ball?.GetCurrentHex()?.GetOccupyingToken() == keeper
            && movementPhaseManager?.goalKeeperManager?.IsGoalkeeperOwnPenaltyHex(keeper, ball.GetCurrentHex()) == true;

    public void MarkGoalkeeperHolding(PlayerToken keeper)
    {
        movementPhaseManager?.goalKeeperManager?.CancelDeferredPickup();
        handlingSource = handlingOwner = keeper;
        handlingDelivery = GoalkeeperDeliveryKind.None;
        handlingEligibility = GoalkeeperHandlingEligibility.Holding;
    }

    public void MarkGoalkeeperDeliberateDrop(PlayerToken keeper)
    {
        MarkGoalkeeperHolding(keeper);
        handlingEligibility = GoalkeeperHandlingEligibility.DeliberateDrop;
    }

    public void ResetGoalkeeperHandling()
    {
        movementPhaseManager?.goalKeeperManager?.CancelDeferredPickup();
        handlingOwner = handlingSource = null;
        handlingDelivery = GoalkeeperDeliveryKind.None;
        handlingEligibility = GoalkeeperHandlingEligibility.None;
    }

    private RoomGoalkeeperHandlingSnapshot CreateGoalkeeperHandlingSnapshot() => new()
    {
        source = CreateTokenReference(handlingSource), owner = CreateTokenReference(handlingOwner),
        delivery = handlingDelivery, eligibility = handlingEligibility
    };

    private void RestoreGoalkeeperHandling(RoomGoalkeeperHandlingSnapshot state)
    {
        ResetGoalkeeperHandling();
        if (state == null) return; // Old saves never infer permission from location or last-touch strings.
        handlingSource = ResolveTokenReference(state.source);
        handlingOwner = ResolveTokenReference(state.owner);
        handlingDelivery = state.delivery;
        handlingEligibility = state.eligibility;
    }
}

[Serializable]
public sealed class RoomGoalkeeperHandlingSnapshot
{
    public RoomTokenReference source, owner;
    public GoalkeeperDeliveryKind delivery;
    public GoalkeeperHandlingEligibility eligibility;
}
