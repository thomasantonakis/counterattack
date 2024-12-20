using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutOfBoundsManager : MonoBehaviour
{
    public Ball ball;
    public HexGrid hexGrid;
    public HighPassManager highPassManager;
    public void HandleOutOfBoundsFromInaccuracy(HexCell currentTargetHex, int directionIndex)
    {
        if (currentTargetHex == null)
        {
            Debug.LogWarning("currentTargetHex is null. Cannot handle out-of-bounds scenario.");
            return;
        }

        // We need to find the last inbounds hex along the trajectory
        HexCell lastInboundsHex = currentTargetHex;

        // Move along the trajectory, using the inaccuracy direction vector
        HexCell currentHex = currentTargetHex;
        while (currentHex != null && !currentHex.isOutOfBounds)
        {
            lastInboundsHex = currentHex;  // Update the last valid inbounds hex
            currentHex = highPassManager.CalculateInaccurateTarget(currentHex, directionIndex, 1);
        }

        Debug.Log($"Last inbounds hex before ball went out of bounds: {lastInboundsHex.coordinates}");

        // Now determine where the ball went out
        string outOfBoundsSide = DetermineOutOfBoundsSide(lastInboundsHex, directionIndex);

        // Handle based on out-of-bounds type
        switch (outOfBoundsSide)
        {
            case "LeftGoal":
                Debug.Log("Goal Kick or Corner Kick for Left Side.");
                HandleGoalKickOrCorner(lastInboundsHex, outOfBoundsSide);
                break;
            case "RightGoal":
                Debug.Log("Goal Kick or Corner Kick for Right Side.");
                HandleGoalKickOrCorner(lastInboundsHex, outOfBoundsSide);
                break;
            case "Top Throw-In":
            case "Bottom Throw-In":
                Debug.Log("Handling a Throw-In.");
                HandleThrowIn(lastInboundsHex);
                break;
            default:
                Debug.LogWarning("Unknown out-of-bounds scenario.");
                break;
        }

        // Log or handle out-of-bounds scenario based on the side
        Debug.Log($"Ball went out from the {outOfBoundsSide}");
    }

    private string DetermineOutOfBoundsSide(HexCell lastInboundsHex, int directionIndex)
    {
        if ((directionIndex == 1 || directionIndex == 2) && lastInboundsHex.coordinates.x == -18)
        {
            return "LeftGoal";
        }
        else if ((directionIndex == 4 || directionIndex == 5) && lastInboundsHex.coordinates.x == 18)
        {
            return "RightGoal";
        }
        else if (
            directionIndex == 0 // South
            || (directionIndex == 1 && lastInboundsHex.coordinates.x > -18) // SouthWest 
            || (directionIndex == 5 && lastInboundsHex.coordinates.x < 18) // SouthEast 
        )
        {
            return "Bottom Throw-In";
        }
        else if (
            directionIndex == 3 // North
            || (directionIndex == 2 && lastInboundsHex.coordinates.x > -18) // NorthWest 
            || (directionIndex == 4 && lastInboundsHex.coordinates.x < 18) // NorthEast 
        )
        {
            return "Top Throw-In";
        }

        return "unknown";  // Fallback case (this shouldn't happen if the boundaries are properly checked)
    }

     private void HandleThrowIn(HexCell lastInboundsHex)
    {
        StartCoroutine(ball.MoveToCell(lastInboundsHex));
        Debug.Log("Moved the ball to last inboundHex, Changing Possession");
        MatchManager.Instance.ChangePossession();
        Debug.Log("Changed Possession, setting the GameState to WaitingForThrowInTaker");
        MatchManager.Instance.currentState = MatchManager.GameState.WaitingForThrowInTaker;
        Debug.Log("Set the GameState to WaitingForThrowInTaker");
    }
    
    private void HandleGoalKickOrCorner(HexCell lastInboundsHex, string outOfBoundsSide)
    {
        // Get the attacking team's direction
        MatchManager.TeamAttackingDirection attackingDirection;
        if (MatchManager.Instance.teamInAttack == MatchManager.TeamInAttack.Home)
        {
            attackingDirection = MatchManager.Instance.homeTeamDirection;
        }
        else
        {
            attackingDirection = MatchManager.Instance.awayTeamDirection;
        }
        if (outOfBoundsSide == "LeftGoal" && attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight)
        {
            // It is a Corner
            if (lastInboundsHex.coordinates.z > 0)  // Top half of the pitch
            {
                Debug.Log("Left Side: Corner kick from the top-left corner.");
                StartCoroutine(ball.MoveToCell(hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 12))));
                MatchManager.Instance.currentState = MatchManager.GameState.WaitingForCornerTaker;
            }
            else
            {
                Debug.Log("Left Side: Corner kick from the bottom-left corner.");
                StartCoroutine(ball.MoveToCell(hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -12))));
                MatchManager.Instance.currentState = MatchManager.GameState.WaitingForCornerTaker;
            }
        }
        else if (outOfBoundsSide == "RightGoal" && attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft)
        {
            // It is a Corner
            if (lastInboundsHex.coordinates.z > 0)  // Top half of the pitch
            {
                Debug.Log("Right Side: Corner kick from the top-right corner.");
                StartCoroutine(ball.MoveToCell(hexGrid.GetHexCellAt(new Vector3Int(18, 0, 12))));
                MatchManager.Instance.currentState = MatchManager.GameState.WaitingForCornerTaker;
            }
            else
            {
                Debug.Log("Right Side: Corner kick from the bottom-right corner.");
                StartCoroutine(ball.MoveToCell(hexGrid.GetHexCellAt(new Vector3Int(18, 0, -12))));
                MatchManager.Instance.currentState = MatchManager.GameState.WaitingForCornerTaker;
            }
        }
        else
        {
            // It is a Goal Kick
            Debug.Log("It's a Goal Kick.");
            if (outOfBoundsSide == "RightGoal")  // Top half of the pitch
            {
                Debug.Log("Right Side: Goal kick from center Hex at the 6-yard-box.");
                StartCoroutine(ball.MoveToCell(hexGrid.GetHexCellAt(new Vector3Int(16, 0, 0))));
                MatchManager.Instance.currentState = MatchManager.GameState.WaitingForGoalKickFinalThirds;
            }
            else
            {
                Debug.Log("Left Side: Goal kick from center Hex at the 6-yard-box.");
                StartCoroutine(ball.MoveToCell(hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 0))));
                MatchManager.Instance.currentState = MatchManager.GameState.WaitingForGoalKickFinalThirds;
            }
        }

        // Change possession when a goal kick or corner kick occurs
        MatchManager.Instance.ChangePossession();
    }

}
