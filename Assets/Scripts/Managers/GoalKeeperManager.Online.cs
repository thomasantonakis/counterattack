using System.Collections.Generic;
using System.Linq;

public enum OnlineGoalkeeperIntent { Move, Stay, Hands, Feet }

public partial class GoalKeeperManager
{
    public PlayerToken OnlineActor => IsWaitingForPickupChoice ? pickupKeeper : GetCurrentDefendingGoalkeeper();
    public string OnlineStage => IsResolvingMove ? "resolving" : IsWaitingForPickupChoice ? "pickup" : isActivated ? "move" : null;
    public IEnumerable<HexCell> OnlineMoveTargets => !isActivated || IsResolvingMove || OnlineActor?.currentHex==null
        ? Enumerable.Empty<HexCell>() : hexGrid.highlightedHexes.Where(h=>h!=null && !h.isOutOfBounds && h.isInGoal==0
            && h.GetOccupyingToken()==null && !h.isAttackOccupied && !h.isDefenseOccupied
            && HexGridUtils.GetHexStepDistance(OnlineActor.currentHex,h)==1);

    public bool TryExecuteOnlineGoalkeeper(OnlineGoalkeeperIntent intent,HexCell target=null)
    {
        var match=MatchManager.Instance;
        if(match==null || !match.IsOnlineAuthorityControlled || match.IsGameplayInputBlocked || IsResolvingMove)return false;
        switch(intent)
        {
            case OnlineGoalkeeperIntent.Hands:case OnlineGoalkeeperIntent.Feet:
                if(!IsWaitingForPickupChoice || !match.CanOfferGoalkeeperPickup(pickupKeeper))return false;
                ChooseDeferredPickup(intent==OnlineGoalkeeperIntent.Hands);return true;
            case OnlineGoalkeeperIntent.Stay:
                if(!isActivated || IsWaitingForPickupChoice)return false;
                hexGrid.ClearHighlightedHexes();hoveredGKMoveHex=null;isActivated=false;return true;
            case OnlineGoalkeeperIntent.Move:
                if(IsWaitingForPickupChoice || !OnlineMoveTargets.Contains(target))return false;
                // This is the source free-move path, including its synchronous
                // busy claim and the parent pass/movement coroutine continuation.
                _=MoveGKforBox(target);return true;
            default:return false;
        }
    }
}
