using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

public class GoalKeeperManager : MonoBehaviour
{
    public MovementPhaseManager movementPhaseManager;
    public HelperFunctions helperFunctions;
    public HexGrid hexGrid;
    public Ball ball;
    public ShotManager shotManager;
    public GKPushManager gkPushManager;
    public bool isActivated = false;
    private PlayerToken activeDefendingGK;
    private int consumedBoxMovePenaltyBox;
    private HexCell hoveredGKMoveHex;

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnHover += OnHoverReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }


    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnHover -= OnHoverReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
    }

    private void OnHoverReceived(PlayerToken token, HexCell hex)
    {
        if (!isActivated || MatchManager.Instance == null || MatchManager.Instance.difficulty_level >= 3)
        {
            if (hoveredGKMoveHex != null)
            {
                hoveredGKMoveHex.HighlightHex("PaceAvailable");
                hoveredGKMoveHex = null;
            }

            return;
        }

        HexCell nextHoveredHex = hexGrid.highlightedHexes.Contains(hex) ? hex : null;
        if (hoveredGKMoveHex == nextHoveredHex)
        {
            return;
        }

        if (hoveredGKMoveHex != null)
        {
            hoveredGKMoveHex.HighlightHex("PaceAvailable");
        }

        hoveredGKMoveHex = nextHoveredHex;
        if (hoveredGKMoveHex != null)
        {
            hoveredGKMoveHex.HighlightHex("MovementDestinationHover");
        }
    }
    
    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (isActivated)
        {
            if (!hexGrid.highlightedHexes.Contains(hex))
            {
                Debug.Log($"🧤 Invalid move! {hex.name} is not a highlighted hex.");
                return;
            }
            // MoveGKforBox(hex);
            _ = MoveGKforBox(hex); // Explicitly discard the task to silence the warning 
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (isActivated && keyData.key == KeyCode.X)
        {
            hexGrid.ClearHighlightedHexes();
            hoveredGKMoveHex = null;
            Debug.Log($"GK chooses to not rush out for the High Pass, moving on!");
            isActivated = false;
            keyData.isConsumed = true;
        }
    }

    private async Task MoveGKforBox(HexCell hex)
    {
        PlayerToken defenderGK = GetActiveDefendingGK();
        if (defenderGK == null)
        {
            Debug.LogError("Cannot move defending GK because no active defending goalkeeper was captured for this box move.");
            isActivated = false;
            return;
        }

        hexGrid.ClearHighlightedHexes();
        hoveredGKMoveHex = null;
        await helperFunctions.StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(hex, defenderGK, false));
        isActivated = false;
        Debug.Log($"🧤 {defenderGK.name} moved to {hex.name}");
    }

    public void NotifyBallPosition(HexCell ballHex)
    {
        if (ballHex == null || ballHex.isInPenaltyBox == 0)
        {
            if (consumedBoxMovePenaltyBox != 0)
            {
                Debug.Log("GK box move memory reset because the ball is outside the penalty box.");
            }

            consumedBoxMovePenaltyBox = 0;
            activeDefendingGK = null;
        }
    }

    public bool ShouldGKMove(HexCell targetHex)
    {
        if (targetHex == null)
        {
            Debug.LogError("ShouldGKMove called with null targetHex!");
            return false;
        }

        return ShouldGKMoveForPenaltyBox(targetHex.isInPenaltyBox, targetHex);
    }

    public bool CanOfferGKMoveForPenaltyBox(int penaltyBoxValue, HexCell referenceHex, out PlayerToken defendingGK)
    {
        defendingGK = null;
        PlayerToken passer = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        if (passer == null)
        {
            Debug.LogError("Ball hex has no occupying token! Cannot determine passer.");
            return false;
        }

        bool isHomeTeam = passer.isHomeTeam;
        bool isAttacker = passer.isAttacker;
        MatchManager.TeamAttackingDirection attackingDirection = isHomeTeam ? MatchManager.Instance.homeTeamDirection : MatchManager.Instance.awayTeamDirection;

        if (penaltyBoxValue == 0)
        {
            NotifyBallPosition(referenceHex);
            Debug.Log($"GK box move not offered: reference hex {referenceHex?.coordinates.ToString() ?? "<none>"} is not in a penalty box.");
            return false;
        }

        if (consumedBoxMovePenaltyBox == penaltyBoxValue)
        {
            Debug.Log($"GK box move not offered: a defending GK box move has already been offered for this penalty-box entry ({penaltyBoxValue}).");
            return false;
        }

        bool isTargetPenaltyBoxOfDefenders = 
            (attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight && penaltyBoxValue == 1) || 
            (attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft && penaltyBoxValue == -1);

        if (isAttacker && isTargetPenaltyBoxOfDefenders)
        {
            defendingGK = FindDefendingGoalkeeperForAttackingTeam(isHomeTeam);
            if (defendingGK == null)
            {
                Debug.LogError($"GK box move not offered: no defending goalkeeper found against attacker {passer.name}.");
                return false;
            }

            return true;
        }

        Debug.Log(
            $"GK box move not offered: passer={passer.name}, passerIsAttacker={isAttacker}, " +
            $"attackingDirection={attackingDirection}, penaltyBox={penaltyBoxValue}, referenceHex={referenceHex?.coordinates.ToString() ?? "<none>"}.");
        return false;
    }

    public bool ShouldGKMoveForPenaltyBox(int penaltyBoxValue, HexCell referenceHex = null)
    {
        if (!CanOfferGKMoveForPenaltyBox(penaltyBoxValue, referenceHex, out PlayerToken defendingGK))
        {
            return false;
        }

        activeDefendingGK = defendingGK;
        Debug.Log($"⚽ Ball has entered the opponent's penalty box ({penaltyBoxValue}) at {referenceHex?.coordinates.ToString() ?? "<boundary>"}. 🧤 GK gets a free move.");
        isActivated = true;
        consumedBoxMovePenaltyBox = penaltyBoxValue;
        return true;
    }

    public bool TryStartGKMoveForPenaltyBox(int penaltyBoxValue, HexCell referenceHex, out PlayerToken defendingGK)
    {
        defendingGK = null;
        if (!CanOfferGKMoveForPenaltyBox(penaltyBoxValue, referenceHex, out PlayerToken resolvedGK))
        {
            return false;
        }

        defendingGK = resolvedGK;
        activeDefendingGK = resolvedGK;
        Debug.Log($"⚽ Ball has entered the opponent's penalty box ({penaltyBoxValue}) at {referenceHex?.coordinates.ToString() ?? "<boundary>"}. 🧤 GK gets a free move.");
        isActivated = true;
        consumedBoxMovePenaltyBox = penaltyBoxValue;
        return true;
    }

    public PlayerToken GetCurrentDefendingGoalkeeper()
    {
        return GetActiveDefendingGK();
    }

    public bool IsGoalkeeperOwnPenaltyHex(PlayerToken goalkeeper, HexCell hex)
    {
        if (goalkeeper == null || hex == null || hex.isInPenaltyBox == 0 || MatchManager.Instance == null)
        {
            return false;
        }

        MatchManager.TeamAttackingDirection direction = goalkeeper.isHomeTeam
            ? MatchManager.Instance.homeTeamDirection
            : MatchManager.Instance.awayTeamDirection;
        int ownPenaltyBox = direction == MatchManager.TeamAttackingDirection.LeftToRight ? -1 : 1;
        return hex.isInPenaltyBox == ownPenaltyBox;
    }

    public List<HexCell> GetGoalkeeperWallHexes(PlayerToken goalkeeper, bool includeGoalkeeperHex)
    {
        List<HexCell> wallHexes = new();
        HexCell gkHex = goalkeeper != null ? goalkeeper.GetCurrentHex() : null;
        if (gkHex == null || hexGrid == null)
        {
            return wallHexes;
        }

        for (int offset = -3; offset <= 3; offset++)
        {
            if (!includeGoalkeeperHex && offset == 0)
            {
                continue;
            }

            HexCell candidate = hexGrid.GetHexCellAt(new Vector3Int(gkHex.coordinates.x, 0, gkHex.coordinates.z + offset));
            if (candidate != null && IsGoalkeeperOwnPenaltyHex(goalkeeper, candidate))
            {
                wallHexes.Add(candidate);
            }
        }

        return wallHexes;
    }

    public int CalculateGoalkeeperWallPenalty(PlayerToken goalkeeper, HexCell wallHex)
    {
        HexCell gkHex = goalkeeper != null ? goalkeeper.GetCurrentHex() : null;
        if (gkHex == null || wallHex == null)
        {
            return 0;
        }

        int distance = HexGridUtils.GetHexStepDistance(gkHex, wallHex);
        return distance == 3 ? -1 : 0;
    }

    public bool TryFindFirstGoalkeeperWallHexOnPath(
        List<HexCell> path,
        PlayerToken goalkeeper,
        bool includeGoalkeeperHex,
        out HexCell wallHex,
        out int pathIndex,
        out int savingPenalty)
    {
        wallHex = null;
        pathIndex = -1;
        savingPenalty = 0;

        if (path == null || goalkeeper == null)
        {
            return false;
        }

        HashSet<HexCell> wallHexes = GetGoalkeeperWallHexes(goalkeeper, includeGoalkeeperHex).ToHashSet();
        for (int i = 0; i < path.Count; i++)
        {
            HexCell candidate = path[i];
            if (candidate == null || !wallHexes.Contains(candidate))
            {
                continue;
            }

            wallHex = candidate;
            pathIndex = i;
            savingPenalty = CalculateGoalkeeperWallPenalty(goalkeeper, candidate);
            return true;
        }

        return false;
    }

    public HexCell FindClosestGoalkeeperWallHexOnPath(List<HexCell> path, PlayerToken goalkeeper, bool includeGoalkeeperHex)
    {
        HexCell gkHex = goalkeeper != null ? goalkeeper.GetCurrentHex() : null;
        if (path == null || gkHex == null)
        {
            return null;
        }

        HashSet<HexCell> wallHexes = GetGoalkeeperWallHexes(goalkeeper, includeGoalkeeperHex).ToHashSet();
        return path
            .Select((hex, index) => new { hex, index })
            .Where(entry => entry.hex != null && wallHexes.Contains(entry.hex))
            .OrderBy(entry => HexGridUtils.GetHexStepDistance(gkHex, entry.hex))
            .ThenBy(entry => entry.hex == gkHex ? 0 : 1)
            .ThenBy(entry => entry.index)
            .Select(entry => entry.hex)
            .FirstOrDefault();
    }

    public IEnumerator ResolveGoalkeeperSaveAndHold(PlayerToken goalkeeper, HexCell saveHex, string recoveryType)
    {
        if (goalkeeper == null || saveHex == null)
        {
            Debug.LogError("Cannot resolve goalkeeper Save and Hold because goalkeeper or save hex is null.");
            yield break;
        }

        if (ball != null && ball.GetCurrentHex() != saveHex)
        {
            yield return StartCoroutine(ball.MoveToCell(saveHex, null, allowGKBoxMove: false));
        }

        GKPushManager manager = ResolveGKPushManager();
        if (manager != null)
        {
            yield return StartCoroutine(manager.ResolveGKPush(goalkeeper, saveHex));
        }

        PlayerToken recoveredFrom = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;

        if (!goalkeeper.isAttacker)
        {
            MatchManager.Instance.ChangePossession();
        }

        MatchManager.Instance.SetLastToken(goalkeeper);
        ball?.PlaceAtCell(saveHex);
        MatchManager.Instance.UpdatePossessionAfterPass(saveHex);
        MatchManager.Instance.gameData.gameLog.LogEvent(
            goalkeeper,
            MatchManager.ActionType.SaveMade,
            saveType: "held"
        );
        MatchManager.Instance.gameData.gameLog.LogEvent(
            goalkeeper,
            MatchManager.ActionType.BallRecovery,
            connectedToken: recoveredFrom,
            recoveryType: recoveryType
        );

        MatchManager.Instance.BroadcastDefensiveRecoveryOutcome(goalkeeper, saveHex, triggerFinalThirdsForAnyOther: false);
    }

    private GKPushManager ResolveGKPushManager()
    {
        if (gkPushManager == null)
        {
            gkPushManager = FindAnyObjectByType<GKPushManager>();
        }

        if (gkPushManager == null)
        {
            gkPushManager = gameObject.AddComponent<GKPushManager>();
        }

        gkPushManager.Configure(hexGrid, ball);
        return gkPushManager;
    }

    public IEnumerator HandleGKFreeMove()
    {
        PlayerToken defenderGK = GetActiveDefendingGK();

        if (defenderGK == null)
        {
            Debug.LogError("No defending goalkeeper found!");
            isActivated = false;
            yield break;
        }

        movementPhaseManager.HighlightValidMovementHexes(defenderGK, 1);

        if (hexGrid.highlightedHexes.Count == 0)
        {
            Debug.Log("GK has no valid move options. Skipping free move.");
            isActivated = false;
            yield break;
        }

        Debug.Log("🧤 GK Free Move: Click on a highlighted hex to move, or press [X] to skip.");

        while (isActivated)
        {
            yield return null;
        }
    }

    private PlayerToken FindDefendingGoalkeeperForAttackingTeam(bool attackingTeamIsHome)
    {
        return FindObjectsByType<PlayerToken>()
            .FirstOrDefault(token => token != null
                && token.isPlaying
                && token.IsGoalKeeper
                && token.isHomeTeam != attackingTeamIsHome);
    }

    private PlayerToken GetActiveDefendingGK()
    {
        if (activeDefendingGK != null)
        {
            return activeDefendingGK;
        }

        return hexGrid.GetDefendingGK();
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("GK: ");

        if (isActivated) sb.Append("isActivated, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (isActivated) sb.Append("Defending GK is awarded a free move! Click on a highlighted hex to move, or Press [X] to stay there, ");
        
        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Safely trim trailing comma + space
        return sb.ToString();
    }

    public bool? IsInstructionExpectingHomeTeam()
    {
        if (!isActivated || MatchManager.Instance == null)
        {
            return null;
        }

        PlayerToken defenderGK = GetActiveDefendingGK();
        return defenderGK != null ? defenderGK.isHomeTeam : MatchManager.Instance.teamInAttack != MatchManager.TeamInAttack.Home;
    }
}
