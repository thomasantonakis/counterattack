using System.Collections.Generic;
using System.Linq;

public enum OnlineKickoffIntent { Select, Move, Confirm, Taker }

public partial class KickoffManager
{
    public bool OnlineActive => isActivated;
    private bool OnlineReady => isActivated && MatchManager.Instance != null
        && MatchManager.Instance.IsOnlineAuthorityControlled && !MatchManager.Instance.IsGameplayInputBlocked
        && !MatchManager.Instance.IsWaitingForExtraActionsRoll;
    public string OnlineStage => IsWaitingForMovementToComplete() ? "resolving"
        : !OnlineReady ? "pending" : flowPhase == KickoffFlowPhase.TakerSelection ? "taker" : "setup";
    // One admission decision owns the setup at a time. Both teams retain their
    // existing formation rules; Home confirms before Away, then the conceding
    // team chooses the taker. A confirmed team cannot alter the other setup.
    public string OnlineOwner => flowPhase == KickoffFlowPhase.TakerSelection
        ? MatchManager.Instance.teamInAttack.ToString() : !homeSetupConfirmed ? "Home" : "Away";
    public IEnumerable<PlayerToken> OnlinePlayers => !OnlineReady || IsWaitingForMovementToComplete()
        ? Enumerable.Empty<PlayerToken>() : playerTokenManager.allTokens.Where(t=>t!=null && t.isPlaying
            && (t.isHomeTeam ? "Home" : "Away")==OnlineOwner
            && (flowPhase!=KickoffFlowPhase.TakerSelection || t.isAttacker));
    public IEnumerable<HexCell> OnlineDestinations => OnlineStage!="setup" || selectedToken==null
        || (selectedToken.isHomeTeam?"Home":"Away")!=OnlineOwner ? Enumerable.Empty<HexCell>()
        : GetAllGridHexes().Where(h=>h!=selectedToken.currentHex && (flowPhase==KickoffFlowPhase.PostGoalSetup
            ? IsValidPostGoalSetupDecisionDestination(selectedToken,h) : IsValidInitialSetupDecisionDestination(selectedToken,h)));

    public bool TryExecuteOnlineKickoff(OnlineKickoffIntent intent,PlayerToken player=null,HexCell target=null)
    {
        if(!OnlineReady || IsWaitingForMovementToComplete())return false;
        switch(intent)
        {
            case OnlineKickoffIntent.Select:
                if(OnlineStage!="setup" || !OnlinePlayers.Contains(player))return false;
                // Selecting is distinct from swapping: destinations carry the
                // actual move, so choosing a teammate cannot silently swap two.
                SelectToken(player);return true;
            case OnlineKickoffIntent.Move:
                if(!OnlineDestinations.Contains(target))return false;
                StartCoroutine(flowPhase==KickoffFlowPhase.PostGoalSetup
                    ? TryMovePostGoalSetupToken(target) : TryMoveInitialSetupToken(target));return true;
            case OnlineKickoffIntent.Confirm:
                if(OnlineStage!="setup")return false;
                SetPendingAutomatedConfirmationTeam(OnlineOwner);
                try { if(flowPhase==KickoffFlowPhase.PostGoalSetup)ConfirmPostGoalSetup();else ConfirmInitialSetup(); }
                finally { SetPendingAutomatedConfirmationTeam(null); }
                return true;
            case OnlineKickoffIntent.Taker:
                if(OnlineStage!="taker" || !OnlinePlayers.Contains(player))return false;
                var center=hexGrid.GetHexCellAt(new UnityEngine.Vector3Int(0,0,0));
                if(center==null || center.GetOccupyingToken()!=null && center.GetOccupyingToken()!=player)return false;
                HandleKickoffTakerSelectionClick(player);return true;
            default:return false;
        }
    }
}
