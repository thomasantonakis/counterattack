using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoalFlowManager : MonoBehaviour
{
    public HexGrid hexGrid;
    public PlayerTokenManager playerTokenManager;
    public MovementPhaseManager movementPhaseManager;
    public GroundBallManager groundBallManager;
    public LongBallManager longBallManager;
    public KickoffManager kickoffManager;
    // Celebration hex lists
    private List<HexCell> celebrationTopLeft;
    private List<HexCell> celebrationTopRight;
    private List<HexCell> celebrationBottomLeft;
    private List<HexCell> celebrationBottomRight;
    // Reset formation lists
    private List<HexCell> resetFormationLeft;
    private List<HexCell> resetFormationRight;
    public bool defendersAreBack = false;
    public bool attackersAreBack = false;
    private void Start()
    {
        StartCoroutine(WaitUntilHexGridIsReady());
    }
    private IEnumerator WaitUntilHexGridIsReady()
    {
        HexGrid hexGriditem = this.hexGrid;
        yield return new WaitUntil(() => hexGriditem != null && hexGriditem.IsGridInitialized());  // Check if grid is ready
        // Initialize hex lists (Replace with actual predefined lists)
        celebrationTopLeft = GenerateTopLeftHexList();
        celebrationTopRight = GenerateTopRightHexList();
        celebrationBottomLeft = GenerateBottomLeftHexList();
        celebrationBottomRight = GenerateBottomRightHexList();
        resetFormationLeft = GenerateResetLeft();
        resetFormationRight = GenerateResetRight();
    }
    private List<HexCell> GenerateTopLeftHexList()
    {
        List<HexCell> list = new()
        {
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 11)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, 9)),
            hexGrid.GetHexCellAt(new Vector3Int(-17, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(-17, 0, 11)),
            hexGrid.GetHexCellAt(new Vector3Int(-17, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(-17, 0, 9)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 11)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 10))
        };
        return list;
    }
    private List<HexCell> GenerateTopRightHexList()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 8)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, 6)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 8)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 6)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, 12)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, 10)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, 8))
        };
        return list;
    }
    private List<HexCell> GenerateBottomLeftHexList()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -8)),
            hexGrid.GetHexCellAt(new Vector3Int(-18, 0, -6)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -8)),
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, -6)),
            hexGrid.GetHexCellAt(new Vector3Int(-14, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(-14, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(-14, 0, -8))
        };
        return list;
    }
    private List<HexCell> GenerateBottomRightHexList()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -8)),
            hexGrid.GetHexCellAt(new Vector3Int(18, 0, -6)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -8)),
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, -6)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, -12)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, -10)),
            hexGrid.GetHexCellAt(new Vector3Int(14, 0, -8))
        };
        return list;
    }
    private List<HexCell> GenerateResetLeft()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(-16, 0, 0)), // 1
            hexGrid.GetHexCellAt(new Vector3Int(-10, 0, -8)), // 2
            hexGrid.GetHexCellAt(new Vector3Int(-10, 0, 8)), // 3
            hexGrid.GetHexCellAt(new Vector3Int(-10, 0, 4)), // 4
            hexGrid.GetHexCellAt(new Vector3Int(-10, 0, -4)), // 5
            hexGrid.GetHexCellAt(new Vector3Int(-4, 0, -4)), // 6
            hexGrid.GetHexCellAt(new Vector3Int(-4, 0, -8)), // 7 RM
            hexGrid.GetHexCellAt(new Vector3Int(-4, 0, 4)), // 8
            hexGrid.GetHexCellAt(new Vector3Int(-2, 0, 3)), // 9
            hexGrid.GetHexCellAt(new Vector3Int(-2, 0, -3)), // 10
            hexGrid.GetHexCellAt(new Vector3Int(-4, 0, 8)) // 11 LM
        };
        return list;
    }
    private List<HexCell> GenerateResetRight()
    {
        List<HexCell> list = new List<HexCell>
        {
            hexGrid.GetHexCellAt(new Vector3Int(16, 0, 0)), // 1
            hexGrid.GetHexCellAt(new Vector3Int(10, 0, 8)), // 2
            hexGrid.GetHexCellAt(new Vector3Int(10, 0, -8)), // 3
            hexGrid.GetHexCellAt(new Vector3Int(10, 0, -4)), // 4
            hexGrid.GetHexCellAt(new Vector3Int(10, 0, 4)), // 5
            hexGrid.GetHexCellAt(new Vector3Int(4, 0, 4)), // 6
            hexGrid.GetHexCellAt(new Vector3Int(4, 0, 8)), // 7 RM
            hexGrid.GetHexCellAt(new Vector3Int(4, 0, -4)), // 8
            hexGrid.GetHexCellAt(new Vector3Int(2, 0, 3)), // 9
            hexGrid.GetHexCellAt(new Vector3Int(2, 0, -3)), // 10
            hexGrid.GetHexCellAt(new Vector3Int(4, 0, -8)) // 11 LM
        };
        return list;
    }
  
  
    public void StartGoalFlow(PlayerToken shooterToken)
    {
        Debug.Log($"GOAL! {shooterToken.name} scores! Starting celebration...");
        // foreach (var token in playerTokenManager.allTokens)
        // {
        //     Debug.Log($"[BEFORE Celebration] {token.name} - isAttacker: {token.isAttacker}");
        // }
        StartCoroutine(DefenseCelebrationFlow(shooterToken));
        StartCoroutine(AttackCelebrationFlow(shooterToken));
    }

    private IEnumerator AttackCelebrationFlow(PlayerToken shooterToken)
    {
        // 1️⃣ Determine which corner flag the players should run to
        List<HexCell> celebrationHexes = GetCelebrationHexes(shooterToken);
        List<HexCell> attackerResetHexes = (shooterToken.GetCurrentHex().coordinates.x > 0) ? resetFormationLeft : resetFormationRight;
        HexCell defGkHex = (attackerResetHexes == resetFormationLeft) ? resetFormationRight[0] : resetFormationLeft[0];
        // 2️⃣ Get all attacking teammates
        List<PlayerToken> attackers = GetAttackTokens(shooterToken.isHomeTeam);
        // 3️⃣ Move all attackers to their celebration positions
        yield return StartCoroutine(MovePlayersToHexes(attackers, celebrationHexes));
        // 4️⃣ Wait a bit to celebrate
        yield return new WaitForSeconds(1); // Small pause for celebration
        Debug.Log("Waited for 1 second, going back!");
        // 6️⃣ Move attackers back to their reset positions
        // TeleportPlayersToHexes(attackers, attackerResetHexes);
        yield return StartCoroutine(MovePlayersToHexes(attackers, attackerResetHexes));
        attackersAreBack = true;
        // foreach (var token in playerTokenManager.allTokens)
        // {
        //     Debug.Log($"[AFTER Celebration] {token.name} - isAttacker: {token.isAttacker}");
        // }
        MatchManager.Instance.MakeSureEveryOneIsCorrectlyAssigned();
        StartCoroutine(MoveBallBackToKickOffHex(defGkHex));
        MatchManager.Instance.ChangePossession();
    }

    private IEnumerator DefenseCelebrationFlow(PlayerToken shooterToken)
    {
        // 1️⃣ Get all defender Tokens
        List<PlayerToken> defenders = GetAttackTokens(!shooterToken.isHomeTeam);
        // 2️⃣ Get the hexes where they should reset
        List<HexCell> defenderResetHexes = (shooterToken.GetCurrentHex().coordinates.x < 0) ? resetFormationLeft : resetFormationRight;
        // 3️⃣ Wait and cry!
        yield return new WaitForSeconds(0); // Small pause for disappointment
        // 4️⃣ Move defenders to their reset positions
        // TeleportPlayersToHexes(defenders, defenderResetHexes);
        yield return StartCoroutine(MovePlayersToHexes(defenders, defenderResetHexes));
        defendersAreBack = true;
    }

    // Determines the celebration hex list based on scorer's position
    private List<HexCell> GetCelebrationHexes(PlayerToken scorer)
    {
        if (scorer.GetCurrentHex().coordinates.z > 0)
            return (scorer.GetCurrentHex().coordinates.x > 0) ? celebrationTopRight : celebrationTopLeft;
        else
            return (scorer.GetCurrentHex().coordinates.x > 0) ? celebrationBottomRight : celebrationBottomLeft;
    }

    private void TeleportPlayersToHexes(List<PlayerToken> players, List<HexCell> targetHexes)
    {
        if (players.Count > targetHexes.Count)
        {
            Debug.LogWarning($"[GoalFlow] Not enough target hexes ({targetHexes.Count}) for all players ({players.Count})!");
        }

        Debug.Log($"[GoalFlow] Instantly moving {players.Count} players to {targetHexes.Count} hexes.");

        for (int i = 0; i < players.Count; i++)
        {
            PlayerToken player = players[i];

            if (player == null)
            {
                Debug.LogError($"[GoalFlow] Player at index {i} is NULL!");
                continue;
            }

            HexCell targetHex = targetHexes[Mathf.Min(i, targetHexes.Count - 1)];

            Debug.Log($"[GoalFlow] Teleporting {player.name} to Hex {targetHex.coordinates}");

            player.SetCurrentHex(targetHex);
            player.transform.position = targetHex.transform.position; // Instantly move the GameObject
        }
    }

    // Moves all players in a team to their target hexes
    private IEnumerator MovePlayersToHexes(List<PlayerToken> players, List<HexCell> targetHexes)
    {
        if (players.Count > targetHexes.Count)
        {
            Debug.LogWarning($"[GoalFlow] Not enough target hexes ({targetHexes.Count}) for all players ({players.Count})!");
        }
        // 🛠 Shuffle the hex list so it's randomized (avoids unnatural movement patterns)
        List<HexCell> shuffledHexes = targetHexes.OrderBy(h => Random.value).ToList();

        // Store assigned hexes to prevent double assignments
        HashSet<HexCell> assignedHexes = new HashSet<HexCell>();
        List<Coroutine> movementCoroutines = new List<Coroutine>();
        for (int i = 0; i < players.Count; i++)
        {
            PlayerToken player = players[i];
            if (player == null)
            {
                Debug.LogError($"[GoalFlow] Player at index {i} is NULL!");
                continue;
            }
            // Find the first available hex that is not occupied
            HexCell targetHex = shuffledHexes.FirstOrDefault(h => !assignedHexes.Contains(h));

            if (targetHex == null)
            {
                Debug.LogError($"[GoalFlow] Target Hex at index {i} is NULL!");
                continue;
            }
            assignedHexes.Add(targetHex); // Mark this hex as taken
            Debug.Log($"[GoalFlow] Moving {players[i].name} to Hex {targetHexes[i].coordinates}");
            // Start moving everyone at the same time
            Coroutine moveCoroutine = StartCoroutine(movementPhaseManager.MoveTokenToHex(targetHexes[i], players[i], false, false));
            movementCoroutines.Add(moveCoroutine);
        }
        // Wait for all coroutines to finish before moving forward
        foreach (var coroutine in movementCoroutines)
        {
            yield return coroutine;
        }
    }

    // Retrieves all tokens belonging to a specific team
    private List<PlayerToken> GetAttackTokens(bool teamID)
    {
        return playerTokenManager.allTokens.Where(token => token.isHomeTeam == teamID).ToList();
    }

    private IEnumerator MoveBallBackToKickOffHex(HexCell hex)
    {
      yield return groundBallManager.HandleGroundBallMovement(hex);
      yield return longBallManager.HandleLongBallMovement(hexGrid.GetHexCellAt(new Vector3Int(0, 0, 0)), true);
      kickoffManager.StartPreKickoffPhase();
    }
}
