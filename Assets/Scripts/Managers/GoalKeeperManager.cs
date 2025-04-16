using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class GoalKeeperManager : MonoBehaviour
{
    public MovementPhaseManager movementPhaseManager;
    public HexGrid hexGrid;
    public Ball ball;
    public bool isWaitingForDefGKBoxMove = false;

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

    private async Task StartCoroutineAndWait(IEnumerator coroutine)
    {
        bool isDone = false;
        StartCoroutine(WrapCoroutine(coroutine, () => isDone = true));
        await Task.Run(() => { while (!isDone) { } }); // Wait until coroutine completes
    }

    private IEnumerator WrapCoroutine(IEnumerator coroutine, System.Action onComplete)
    {
        yield return StartCoroutine(coroutine);
        onComplete?.Invoke();
    }
    
    private void OnClickReceived(PlayerToken token, HexCell hex)
    {
        if (isWaitingForDefGKBoxMove)
        {
            MoveGKforBox(hex);
        }
    }


    private void OnKeyReceived(KeyCode key)
    {
        if (isWaitingForDefGKBoxMove && key == KeyCode.X)
        {
            hexGrid.ClearHighlightedHexes();
            Debug.Log($"GK chooses to not rush out for the High Pass, moving on!");
            isWaitingForDefGKBoxMove = false;
        }
    }

    private async void MoveGKforBox(HexCell hex)
    {
        hexGrid.ClearHighlightedHexes();
        await StartCoroutineAndWait(movementPhaseManager.MoveTokenToHex(hex, hexGrid.GetDefendingGK(), false));
        isWaitingForDefGKBoxMove = false;
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
            return true;
        }

        return false;
    }

    public IEnumerator HandleGKFreeMove()
    {
        isWaitingForDefGKBoxMove = true;
        PlayerToken defenderGK = hexGrid.GetDefendingGK();

        if (defenderGK == null)
        {
            Debug.LogError("No defending goalkeeper found!");
            yield break;
        }

        HexCell gkHex = defenderGK.GetCurrentHex();
        movementPhaseManager.HighlightValidMovementHexes(defenderGK, 1);

        if (hexGrid.highlightedHexes.Count == 0)
        {
            Debug.Log("GK has no valid move options. Skipping free move.");
            yield break;
        }

        Debug.Log("🧤 GK Free Move: Click on a highlighted hex to move, or press [X] to skip.");

        while (isWaitingForDefGKBoxMove)
        {
            yield return null;
        }
    }
}
