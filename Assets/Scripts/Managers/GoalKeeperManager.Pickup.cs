using System;
using UnityEngine;

public partial class GoalKeeperManager
{
    public const string PickupChoiceGuidance = "[H] Pick up: Quick Throw [Q] or GK distribution [K]\n[F] Feet: [M] Move · [P] Pass · [C] High Pass · [L] Long Ball";
    public bool IsWaitingForPickupChoice { get; private set; }
    private PlayerToken pickupKeeper;
    private Action continueMovementAtFeet;

    public bool TryOfferDeferredPickup(Action feetContinuation)
    {
        if (IsWaitingForPickupChoice) return true;
        var match = MatchManager.Instance;
        var keeper = ball?.GetCurrentHex()?.GetOccupyingToken();
        if (match == null || !match.CanOfferGoalkeeperPickup(keeper)) return false;
        pickupKeeper = keeper;
        continueMovementAtFeet = feetContinuation;
        IsWaitingForPickupChoice = true;
        PresentDeferredPickup();
        return true;
    }

    private void PresentDeferredPickup()
        => DuelResolutionPanelController.Instance?.BeginSingleChoice(this,
            () => IsWaitingForPickupChoice, "Goalkeeper · Pick Up or Feet", pickupKeeper,
            PickupChoiceGuidance,
            "[H] Pick up · [F] Feet", KeyCode.H, KeyCode.F);

    private void Update()
    {
        if (!IsWaitingForPickupChoice) return;
        if (MatchManager.Instance?.CanOfferGoalkeeperPickup(pickupKeeper) != true)
        {
            CancelDeferredPickup();
            return;
        }
        PresentDeferredPickup();
    }

    public void ChooseDeferredPickup(bool hands)
    {
        if (!IsWaitingForPickupChoice) return;
        var match = MatchManager.Instance;
        var keeper = pickupKeeper;
        var continuation = continueMovementAtFeet;
        bool legal = match != null && match.CanOfferGoalkeeperPickup(keeper);
        CancelDeferredPickup();
        if (!legal) return;
        if (!hands) { continuation?.Invoke(); return; }
        match.MarkGoalkeeperHolding(keeper);
        match.BroadcastDefensiveRecoveryOutcome(keeper, keeper.GetCurrentHex(), false);
    }

    public void CancelDeferredPickup()
    {
        if (IsWaitingForPickupChoice) DuelResolutionPanelController.Instance?.CancelSingle(this);
        IsWaitingForPickupChoice = false;
        pickupKeeper = null;
        continueMovementAtFeet = null;
    }
}
