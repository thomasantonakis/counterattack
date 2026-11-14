using System.Linq;

public enum OnlineFtpIntent { Target, Select, Move, Skip, Roll }

public partial class FirstTimePassManager
{
    private int onlineFtpVersion;
    private bool IsFtpContinuationCurrent(int version)=>this!=null && isActivated && version==onlineFtpVersion;
    public string OnlineFtpStage=>movementPhaseManager.isPlayerMoving?"resolving":isAwaitingTargetSelection?"target"
        :isWaitingForAttackerSelection?"attacker":isWaitingForDefenderSelection?"defender":isWaitingForDiceRoll?"roll":"resolving";
    public PlayerToken OnlineFtpActor=>isWaitingForDiceRoll?currentPathInteraction?.DefenderToken:selectedToken;
    public PlayerToken[] OnlineFtpPlayers=>MatchManager.Instance.playerTokenManager.allTokens
        .Where(t=>t!=null && t.isPlaying && t.isAttacker==isWaitingForAttackerSelection
            && (MatchManager.Instance.difficulty_level!=3 || selectedToken==null || selectedToken==t)).ToArray();
    public HexCell[] OnlineFtpDestinations=>selectedToken==null?System.Array.Empty<HexCell>():GetValidFtpMovementDestinations(selectedToken)
        .Where(h=>!isWaitingForAttackerSelection || !IsDifficultyOneOffsideMoveOntoTarget(selectedToken,h)).ToArray();
    public bool OnlineCanSkipFtp=>isWaitingForDefenderSelection || isWaitingForAttackerSelection && !IsDifficultyOneOffsideTokenOnTarget();
    public bool TryExecuteOnlineFtp(OnlineFtpIntent intent,PlayerToken player=null,HexCell target=null)
    {
        if(MatchManager.Instance==null || MatchManager.Instance.IsGameplayInputBlocked || !isActivated
            || IsFinalThirdRunning() || movementPhaseManager.isPlayerMoving)return false;
        switch(intent)
        {
            case OnlineFtpIntent.Target:
                if(!isAwaitingTargetSelection || !GetLegalFirstTimePassTargetsForDecisionPreview().Contains(target))return false;
                HandleFTPBallPath(target);return true;
            case OnlineFtpIntent.Select:
                if(OnlineFtpStage is not ("attacker" or "defender") || !OnlineFtpPlayers.Contains(player))return false;
                if(isWaitingForAttackerSelection)HandleAttackerSelectionClick(player,null);else HandleDefenderSelectionClick(player,null);return true;
            case OnlineFtpIntent.Move:
                if(OnlineFtpStage is not ("attacker" or "defender") || !OnlineFtpDestinations.Contains(target))return false;
                if(isWaitingForAttackerSelection)HandleAttackerSelectionClick(null,target);else HandleDefenderSelectionClick(null,target);return true;
            case OnlineFtpIntent.Skip:
                if(!OnlineCanSkipFtp)return false;
                if(isWaitingForAttackerSelection)SkipAttackerMovementPhase();else SkipDefenderMovementPhase();return true;
            case OnlineFtpIntent.Roll:
                if(!isWaitingForDiceRoll || currentPathInteraction==null)return false;
                PerformFTPInterceptionRolls((RollInputOverride?)null);return true;
            default:return false;
        }
    }
}
