using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class KickoffManager : MonoBehaviour
{
    public PlayerTokenManager playerTokenManager;
    public HexGrid hexGrid;
    public Ball ball;
    public FreeKickManager freeKickManager;
    public MovementPhaseManager movementPhaseManager;
    public HeaderManager headerManager;
    public PlayerToken selectedToken;
    [Header("Runtime Items")]
    private bool isActivated = false;
    private int spacePressCount = 0;

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
        if (!isActivated) return;
        if (token != null && token != selectedToken)
        {
            SelectToken(token);
        }
        else if (hex != null)
        {
            StartCoroutine(TryMoveToken(hex));
        }
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (!isActivated) return;
        if (keyData.key == KeyCode.Space)
        {
            ConfirmSetup();
        }
    }

    public void StartPreKickoffPhase()
    {
        isActivated = true;
        Debug.Log("Pre-Kickoff Formation: Click tokens to reposition them. Press Space twice to start!");
        spacePressCount = 0;
    }

    private void SelectToken(PlayerToken token)
    {
        if (selectedToken != null)
        {
            Debug.Log($"Deselecting {selectedToken.name}");
        }
        if (token != selectedToken && selectedToken != null) Debug.Log($"Switching Selected token to {token.name}");
        else Debug.Log($"Selecting token {token.name}");
        selectedToken = token;
        
    }

    private IEnumerator TryMoveToken(HexCell targetHex)
    {
        if (selectedToken == null)
        {
            Debug.LogWarning($"Please select a Token first");
            yield break;
        }
        else
        {
            HexCell currentHex = selectedToken.GetCurrentHex();
            bool isSameHalf = (currentHex.coordinates.x * targetHex.coordinates.x) >= 0;

            if (!isSameHalf)
            {
                Debug.LogWarning($"{selectedToken.name} cannot move outside their half!");
                yield break;
            }
            if (!selectedToken.isAttacker && targetHex.isInCircle == 5)
            {
                Debug.LogWarning($"Defenders should not be placed on the KickOff Circle!");
                yield break;
            }
            yield return StartCoroutine(freeKickManager.MoveTokenToHex(selectedToken, targetHex));
            Debug.Log($"{selectedToken.name} moved to {targetHex.coordinates}");
            selectedToken = null; // Deselect after moving
        }
    }

    private void ConfirmSetup()
    {
        spacePressCount++;
        Debug.Log($"Player confirmed setup ({spacePressCount}/2)");

        if (spacePressCount >= 2)
        {
            ValidateAndStartGame();
        }
    }

    private void ValidateAndStartGame()
    {
        bool hasAttackerOnKickoff = playerTokenManager.allTokens
            .Any(t => t.isAttacker && t.GetCurrentHex() == ball.GetCurrentHex());

        if (!hasAttackerOnKickoff)
        {
            Debug.LogWarning("An attacker must be on the kick-off hex to start the game!");
            spacePressCount = 1; // Reset to wait for one more press after correction
            return;
        }

        Debug.Log("Kick-off confirmed! The match begins.");
        movementPhaseManager.ResetMovementPhase();
        headerManager.ResetHeader();
        MatchManager.Instance.SetLastToken(ball.GetCurrentHex().GetOccupyingToken());
        MatchManager.Instance.TriggerStandardPass();
    }

}
