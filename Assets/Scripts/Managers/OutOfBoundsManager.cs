using System.Collections;
using UnityEngine;

public class OutOfBoundsManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Ball ball;
    public HexGrid hexGrid;
    public FreeKickManager freeKickManager;
    public void HandleOutOfBounds(HexCell currentTargetHex, int directionIndex, string source)
    {
        if (currentTargetHex == null)
        {
            Debug.LogWarning("currentTargetHex is null. Cannot handle out-of-bounds scenario.");
            return;
        }
        Debug.Log($"HandleOutOfBounds called with currentTargetHex: {currentTargetHex.coordinates}, due to {source}");

        HexCell lastInboundsHex = currentTargetHex;
        HexCell currentHex = currentTargetHex;
        while (currentHex != null && !currentHex.isOutOfBounds)
        {
            lastInboundsHex = currentHex;  // Update the last valid inbounds hex
            currentHex = CalculateInaccurateTarget(currentHex, directionIndex, 1);
        }

        Debug.Log($"Last inbounds hex before ball went out of bounds: {lastInboundsHex.coordinates}");

        // Now determine where the ball went out
        string outOfBoundsSide = DetermineOutOfBoundsSide(lastInboundsHex, directionIndex, source);

        // Handle based on out-of-bounds type
        switch (outOfBoundsSide)
        {
            case "LeftGoalLine":
                Debug.Log("Goal Kick or Corner Kick for Left Side.");
                StartCoroutine(HandleGoalKickOrCorner(lastInboundsHex, outOfBoundsSide, source));
                break;
            case "RightGoalLine":
                Debug.Log("Goal Kick or Corner Kick for Right Side.");
                StartCoroutine(HandleGoalKickOrCorner(lastInboundsHex, outOfBoundsSide, source));
                break;
            case "Top Throw-In":
            case "Bottom Throw-In":
                Debug.Log("Handling a Throw-In.");
                HandleThrowIn(lastInboundsHex, source);
                break;
            case "LeftGoal":
            case "RightGoal":
                Debug.Log("GOAAAAALLL!!!!");
                HandleGoalScored();
                break;
            default:
                Debug.LogWarning("Unknown out-of-bounds scenario.");
                break;
        }
    }

    public HexCell CalculateInaccurateTarget(HexCell startHex, int directionIndex, int distance)
    {
        Vector3Int currentPosition = startHex.coordinates;  // Start from the current hex
        
        for (int i = 0; i < distance; i++)
        {
            // Use the GetDirectionVectors() method to get the correct direction for the current position
            Vector2Int[] directionVectors = hexGrid.GetHexCellAt(currentPosition).GetDirectionVectors();
            Vector2Int direction2D = directionVectors[directionIndex];
            // Move one step in the selected direction
            int newX = currentPosition.x + direction2D.x;
            int newZ = currentPosition.z + direction2D.y;
            // Update the current position
            currentPosition = new Vector3Int(newX, 0, newZ);
        }
        // Find the final hex based on the calculated position
        HexCell finalHex = hexGrid.GetHexCellAt(currentPosition);
        // Log the final hex for debugging
        if (finalHex != null)
        {
            // Debug.Log($"Final hex: ({finalHex.coordinates.x}, {finalHex.coordinates.z})");
        }
        else
        {
            Debug.LogWarning("Final hex is null or out of bounds!");
        }
        return finalHex;
    }

    private string DetermineOutOfBoundsSide(HexCell lastInboundsHex, int directionIndex, string source)
    {
        if ((directionIndex == 1 || directionIndex == 2) && lastInboundsHex.coordinates.x == -18)
        {
            if (source == "inaccuracy" || Mathf.Abs(lastInboundsHex.coordinates.z) > 3) 
            {
                return "LeftGoalLine";
            }
            else
            {
                return "LeftGoal";
            }
        }
        else if ((directionIndex == 4 || directionIndex == 5) && lastInboundsHex.coordinates.x == 18)
        {
            if (source == "inaccuracy" || Mathf.Abs(lastInboundsHex.coordinates.z) > 3) 
            {
                return "RightGoalLine";
            }
            else
            {
                return "RightGoal";
            }
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

    private void HandleThrowIn(HexCell lastInboundsHex, string source)
    {
        // TODO: Use Source to decide if we need to change possession or not.
        StartCoroutine(ball.MoveToCell(lastInboundsHex));
        Debug.Log("Moved the ball to last inboundHex.");
        if (source == "inaccuracy")
        {
            MatchManager.Instance.ChangePossession();
            Debug.Log("Changed Possession!");
        }
        MatchManager.Instance.currentState = MatchManager.GameState.WaitingForThrowInTaker;
        Debug.Log("Set the GameState to WaitingForThrowInTaker");
    }
    
    public IEnumerator HandleGoalKickOrCorner(HexCell lastInboundsHex, string outOfBoundsSide, string source)
    {
        Debug.Log($"Hello from OOM, {lastInboundsHex.name}, {outOfBoundsSide}, {source}");
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
        // Corner Kick conditions
        if (
            (
                outOfBoundsSide == "LeftGoalLine" // ball went out on the left side
                && attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attacking team attacks to Left
                && source == "defendertouch" // Last one to touch is the defending team
            )
            || (
                outOfBoundsSide == "LeftGoalLine" // ball went out on the left side
                && attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight // Attacking team attacks to right
                && source == "inaccuracy" // Last one to touch is the attacking team
            )
            || (
                outOfBoundsSide == "RightGoalLine" // ball went out on the right side
                && attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight // Attacking team attacks to right
                && source == "defendertouch" // Last one to touch is the defending team
            )
            || (
                outOfBoundsSide == "RightGoalLine" // ball went out on the right side
                && attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attacking team attacks to Left
                && source == "inaccuracy" // Last one to touch is the attacking team
            )
        )
        {
            Debug.Log("It's a Corner Kick");
            if (source == "inaccuracy")
            {
                MatchManager.Instance.ChangePossession();
            }
            if (outOfBoundsSide == "LeftGoalLine")
            {
                if (lastInboundsHex.coordinates.z > 0)  // Top half of the pitch
                {
                    Debug.Log("Left Side: Corner kick from the top-left corner.");
                    HexCell spot = hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 12));
                    yield return StartCoroutine(ball.MoveToCell(spot));
                    freeKickManager.StartFreeKickPreparation(spot);
                }
                else
                {
                    Debug.Log("Left Side: Corner kick from the bottom-left corner.");
                    HexCell spot = hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -12));
                    yield return StartCoroutine(ball.MoveToCell(spot));
                    freeKickManager.StartFreeKickPreparation(spot);
                }
            }
            else
            {
                if (lastInboundsHex.coordinates.z > 0)  // Top half of the pitch
                {
                    Debug.Log("Right Side: Corner kick from the top-right corner.");
                    HexCell spot = hexGrid.GetHexCellAt(new Vector3Int(18, 0, 12));
                    yield return StartCoroutine(ball.MoveToCell(spot));
                    freeKickManager.StartFreeKickPreparation(spot);
                }
                else
                {
                    Debug.Log("Right Side: Corner kick from the bottom-right corner.");
                    HexCell spot = hexGrid.GetHexCellAt(new Vector3Int(18, 0, -12));
                    yield return StartCoroutine(ball.MoveToCell(spot));
                    freeKickManager.StartFreeKickPreparation(spot);
                }
            }
        }
        // Goal Kick conditions
        else if (
            (
                outOfBoundsSide == "LeftGoalLine" // ball went out on the left side
                && attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight // Attacking team attacks to right
                && source == "defendertouch" // Last one to touch is the defending team
            )
            || (
                outOfBoundsSide == "LeftGoalLine" // ball went out on the left side
                && attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attacking team attacks to Left
                && source == "inaccuracy" // Last one to touch is the attacking team
            )
            || (
                outOfBoundsSide == "RightGoalLine" // ball went out on the right side
                && attackingDirection == MatchManager.TeamAttackingDirection.RightToLeft // Attacking team attacks to Left
                && source == "defendertouch" // Last one to touch is the defending team
            )
            || (
                outOfBoundsSide == "RightGoalLine" // ball went out on the right side
                && attackingDirection == MatchManager.TeamAttackingDirection.LeftToRight // Attacking team attacks to right
                && source == "inaccuracy" // Last one to touch is the attacking team
            )
        )
        {
            // It is a Goal Kick
            Debug.Log("It's a Goal Kick.");
            if (source == "inaccuracy")
            {
                MatchManager.Instance.ChangePossession();
            }
            if (outOfBoundsSide == "RightGoalLine")  // Top half of the pitch
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
    }

    private void HandleGoalScored()
    {
        // TODO: Score a goal (or an own goal from a LooseBall scenario)
        Debug.Log("We need to develop the Goal from a LooseBall scenario.");
    }
}
