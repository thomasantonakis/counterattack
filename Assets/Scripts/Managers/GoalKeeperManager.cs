using System.Collections;
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

    public bool ShouldGKMoveForPenaltyBox(int penaltyBoxValue, HexCell referenceHex = null)
    {
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
            activeDefendingGK = FindDefendingGoalkeeperForAttackingTeam(isHomeTeam);
            if (activeDefendingGK == null)
            {
                Debug.LogError($"GK box move not offered: no defending goalkeeper found against attacker {passer.name}.");
                return false;
            }

            Debug.Log($"⚽ Ball has entered the opponent's penalty box ({penaltyBoxValue}) at {referenceHex?.coordinates.ToString() ?? "<boundary>"}. 🧤 GK gets a free move.");
            isActivated = true;
            consumedBoxMovePenaltyBox = penaltyBoxValue;
            return true;
        }

        Debug.Log(
            $"GK box move not offered: passer={passer.name}, passerIsAttacker={isAttacker}, " +
            $"attackingDirection={attackingDirection}, penaltyBox={penaltyBoxValue}, referenceHex={referenceHex?.coordinates.ToString() ?? "<none>"}.");
        return false;
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
        return FindObjectsByType<PlayerToken>(FindObjectsSortMode.None)
            .FirstOrDefault(token => token != null
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
