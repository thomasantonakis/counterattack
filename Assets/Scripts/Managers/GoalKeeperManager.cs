using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

public class GoalKeeperManager : MonoBehaviour
{
    public MovementPhaseManager movementPhaseManager;
    public HelperFunctions helperFunctions;
    public HexGrid hexGrid;
    public Ball ball;
    public bool isActivated = false;

    private void OnEnable()
    {
        GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
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
            MoveGKforBox(hex);
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData.isConsumed) return;
        if (isActivated && keyData.key == KeyCode.X)
        {
            hexGrid.ClearHighlightedHexes();
            Debug.Log($"GK chooses to not rush out for the High Pass, moving on!");
            isActivated = false;
            keyData.isConsumed = true;
        }
    }

    private async void MoveGKforBox(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        await helperFunctions.StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(hex, hexGrid.GetDefendingGK(), false));
        isActivated = false;
        Debug.Log($"🧤 {hexGrid.GetDefendingGK().name} moved to {hex.name}");
    }

    public bool ShouldGKMove(HexCell targetHex)
    {
        if (targetHex == null)
        {
            Debug.LogError("ShouldGKMove called with null targetHex!");
            return false;
        }

        PlayerToken passer = MatchManager.Instance.LastTokenToTouchTheBallOnPurpose;
        if (passer == null)
        {
            Debug.LogError("Ball hex has no occupying token! Cannot determine passer.");
            return false;
        }

        bool isHomeTeam = passer.isHomeTeam;
        bool isAttacker = passer.isAttacker;
        MatchManager.TeamAttackingDirection attackingDirection = isHomeTeam ? MatchManager.Instance.homeTeamDirection : MatchManager.Instance.awayTeamDirection;

        int penaltyBoxValue = targetHex.isInPenaltyBox;

        if (penaltyBoxValue == 0) return false; // Not in a penalty box

        bool isTargetPenaltyBoxOfDefenders = 
            (attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight && penaltyBoxValue == 1) || 
            (attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft && penaltyBoxValue == -1);

        if (isAttacker && isTargetPenaltyBoxOfDefenders)
        {
            Debug.Log("⚽ Ball has entered the opponent's penalty box! 🧤 GK gets a free move.");
            isActivated = true;
            return true;
        }


        return false;
    }

    public IEnumerator HandleGKFreeMove()
    {
        PlayerToken defenderGK = hexGrid.GetDefendingGK();

        if (defenderGK == null)
        {
            Debug.LogError("No defending goalkeeper found!");
            yield break;
        }

        movementPhaseManager.HighlightValidMovementHexes(defenderGK, 1);

        if (hexGrid.highlightedHexes.Count == 0)
        {
            Debug.Log("GK has no valid move options. Skipping free move.");
            yield break;
        }

        Debug.Log("🧤 GK Free Move: Click on a highlighted hex to move, or press [X] to skip.");

        while (isActivated)
        {
            yield return null;
        }
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("GK: ");

        if (isActivated) sb.Append("isActivated, ");

        if (sb[sb.Length - 2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }

    public string GetInstructions()
    {
        StringBuilder sb = new();
        if (isActivated) sb.Append("Defending GK is awarded a free move! Click on a highlighted hex to move, or Press [X] to stay there, ");
        
        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Safely trim trailing comma + space
        return sb.ToString();
    }
}
