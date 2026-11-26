using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using CounterAttack.Online;

public enum OnlineHighPassIntent { Target, SelectAttacker, MoveAttacker, SkipAttacker, SelectDefender, MoveDefender, SkipDefender,
    Accuracy, Direction, Distance, KeeperMove, KeeperStay, KeeperDecline }

public partial class HighPassManager
{
    private int onlineResolutionVersion, onlineRollingVersion, onlineFlightVersion;
    private string onlineRollingPhase, onlineTargetFingerprint;
    private bool onlineFlightInProgress, onlineKeeperMoving;
    private HexCell[] onlineTargets=Array.Empty<HexCell>();
    public bool OnlineFlightInProgress=>onlineFlightInProgress && onlineFlightVersion==onlineResolutionVersion;
    private string OnlineRawStage=>OnlineFlightInProgress || onlineKeeperMoving || movementPhaseManager.isPlayerMoving ? "resolving"
        : isWaitingForDefGKChallengeDecision?"keeper":isWaitingForAccuracyRoll?"accuracy":isWaitingForDirectionRoll?"direction"
        : isWaitingForDistanceRoll?"distance":isWaitingForAttackerSelection?(isWaitingForAttackerMove?"attacker-target":"attacker")
        :isWaitingForDefenderSelection?(isWaitingForDefenderMove?"defender-target":"defender"):isWaitingForConfirmation?"target":"resolving";
    public string OnlineDecisionStage=>onlineRollingVersion==onlineResolutionVersion && onlineRollingPhase==OnlineRawStage?"resolving":OnlineRawStage;
    public bool OnlineCanSkipAttacker=>lockedAttacker!=null;
    public bool OnlineCanKeeperStay=>canDefGKRushWithoutMoving;
    public PlayerToken[] OnlineAttackers=>lockedAttacker==null?eligibleAttackers.Where(t=>t!=null).ToArray()
        :hexGrid.GetAttackerHexes().Select(h=>h.GetOccupyingToken()).Where(t=>t!=null && t!=lockedAttacker).ToArray();
    public PlayerToken[] OnlineDefenders=>hexGrid.GetDefenderHexes().Select(h=>h.GetOccupyingToken()).Where(t=>t!=null).ToArray();
    public HexCell[] OnlineActorDestinations=>selectedToken==null?Array.Empty<HexCell>():hexGrid.highlightedHexes
        .Where(h=>h!=null && h.GetOccupyingToken()==null && (isWaitingForAttackerMove?IsValidAttackerHighPassMoveHex(h):IsValidDefenderHighPassMoveHex(h))).ToArray();

    private bool IsOnlineHighResolutionCurrent(int version,bool online)=>!online || MatchManager.Instance?.IsOnlineAuthorityControlled==true
        && isActivated && version==onlineResolutionVersion;

    public HexCell[] GetLegalOnlineHighPassTargets()
    {
        if(ball?.GetCurrentHex()==null || hexGrid?.cells==null)return Array.Empty<HexCell>();
        var match=MatchManager.Instance;
        string key=hexGrid.GetEntityId()+":"+ball.GetCurrentHex().coordinates+":"+ball.ballRadius+":"+isGoalkeeperKick+":"+isCornerKick
            +":"+minPassDistance+":"+match.difficulty_level+":"+OnlineRollPublication.ActorId(activeHighPassKickerForTargetExclusion)
            +":"+OnlineRollPublication.ActorId(match.setPieceTakerExcludedFromNextTouch)+":"+OnlineRollPublication.ActorId(match.hangingPassExcludedCollector)
            +":"+string.Join(";",hexGrid.cells.Cast<HexCell>().Where(h=>h!=null && (h.isAttackOccupied||h.isDefenseOccupied))
                .Select(h=>h.coordinates+":"+h.isAttackOccupied+":"+h.isDefenseOccupied))
            +":"+string.Join(";",match.offsideManager!=null?match.offsideManager.OffsideTokens.Select(OnlineRollPublication.ActorId):Enumerable.Empty<string>());
        if(key!=onlineTargetFingerprint)
        {
            onlineTargets=hexGrid.cells.Cast<HexCell>().Where(h=>h!=null && IsHighPassTargetAvailableForPreview(h))
                .OrderBy(h=>h.coordinates.x).ThenBy(h=>h.coordinates.z).ToArray();onlineTargetFingerprint=key;
        }
        return onlineTargets.ToArray();
    }

    public bool TryExecuteOnlineHighPass(OnlineHighPassIntent intent,PlayerToken player=null,HexCell target=null)
    {
        if(OnlineRoomSession.IsGuest || MatchManager.Instance==null || MatchManager.Instance.IsGameplayInputBlocked || !isActivated
            || OnlineDecisionStage=="resolving" || goalKeeperManager.isActivated)return false;
        switch(intent)
        {
            case OnlineHighPassIntent.Target when isWaitingForConfirmation && IsHighPassTargetAvailableForPreview(target):
                HandleHighPassProcess(target,isGoalkeeperKick);return true;
            case OnlineHighPassIntent.SelectAttacker when isWaitingForAttackerSelection && OnlineAttackers.Contains(player):
                OnClickReceived(player,player.GetCurrentHex());return true;
            case OnlineHighPassIntent.MoveAttacker when isWaitingForAttackerMove && OnlineActorDestinations.Contains(target):
                StartCoroutine(MoveSelectedAttackerToHex(target));return true;
            case OnlineHighPassIntent.SkipAttacker when isWaitingForAttackerSelection && lockedAttacker!=null:
                ForfeitAttackerHighPassMove();return true;
            case OnlineHighPassIntent.SelectDefender when isWaitingForDefenderSelection && OnlineDefenders.Contains(player):
                OnClickReceived(player,player.GetCurrentHex());return true;
            case OnlineHighPassIntent.MoveDefender when isWaitingForDefenderMove && OnlineActorDestinations.Contains(target):
                StartCoroutine(MoveSelectedDefenderToHex(target));return true;
            case OnlineHighPassIntent.SkipDefender when isWaitingForDefenderSelection:
                ForfeitDefenderHighPassMove();return true;
            case OnlineHighPassIntent.Accuracy when isWaitingForAccuracyRoll:
                StartOnlineHighRoll("accuracy",PresentAccuracyRoll(null));return true;
            case OnlineHighPassIntent.Direction when isWaitingForDirectionRoll:
                StartOnlineHighRoll("direction",PresentInaccuracyDirection(null));return true;
            case OnlineHighPassIntent.Distance when isWaitingForDistanceRoll:
                StartOnlineHighRoll("distance",PresentInaccuracyDistance(null));return true;
            case OnlineHighPassIntent.KeeperDecline when isWaitingForDefGKChallengeDecision:
                hexGrid.ClearHighlightedHexes();isWaitingForDefGKChallengeDecision=false;return true;
            case OnlineHighPassIntent.KeeperStay when isWaitingForDefGKChallengeDecision && canDefGKRushWithoutMoving:
                ConfirmGKHighPassRushWithoutMove();return true;
            case OnlineHighPassIntent.KeeperMove when isWaitingForDefGKChallengeDecision && gkReachableHexes.Contains(target):
                _=MoveOnlineHighKeeper(target);return true;
            default:return false;
        }
    }
    private void StartOnlineHighRoll(string phase,IEnumerator resolution)
    {onlineRollingPhase=phase;onlineRollingVersion=onlineResolutionVersion;StartCoroutine(ResolveOnlineHighRoll(phase,onlineRollingVersion,resolution));}
    private IEnumerator ResolveOnlineHighRoll(string phase,int version,IEnumerator resolution)
    {try{yield return resolution;}finally{if(onlineRollingPhase==phase && onlineRollingVersion==version)onlineRollingPhase=null;}}
    private async Task MoveOnlineHighKeeper(HexCell target)
    {int version=onlineResolutionVersion;onlineKeeperMoving=true;try{await MoveGKForHP(target);}finally{if(version==onlineResolutionVersion)onlineKeeperMoving=false;}}
}
