using UnityEngine;
using System.Collections;

public class AutomatedGameActions : MonoBehaviour
{
    public GameInputManager gameInputManager;
    public MatchManager matchManager;
    public MovementPhaseManager movementPhaseManager;
    public CameraController cameraController;

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     Debug.Log("Pressing Space to blow the whistle for Kick Off.");
        //     // matchManager.BlowWhistle();
        // }
        // else if (Input.GetKeyDown(KeyCode.Alpha2))
        // {
        //     Debug.Log("Pressing 2 to set the camera to preset 2.");
        //     // cameraManager.SetCameraPreset(2);
        // }
        // else if (Input.GetKeyDown(KeyCode.P))
        // {
        //     Debug.Log("Pressing P to start a standard pass.");
        //     // matchManager.StartPass();
        //     StartCoroutine(AutomateStandardPassSequence());
        // }
        // else if (Input.GetKeyDown(KeyCode.M))
        // {
        //     Debug.Log("Pressing M to enter Movement Phase.");
        //     // matchManager.StartMovementPhase();
        //     StartCoroutine(AutomateMovementPhaseSequence());
        // }
        // else if (Input.GetKeyDown(KeyCode.T))
        // {
        //     Debug.Log("Pressing T to choose to tackle.");
        //     StartCoroutine(AutomateTackleSequence());
        // }
    }

    // private IEnumerator AutomateStandardPassSequence()
    // {
    //     yield return new WaitForSeconds(1f);

    //     HexCell yanevaHex = FindHexForToken("8. Yaneva");
    //     Debug.Log("Clicking Hex where 8. Yaneva is to suggest GroundBall target.");
    //     gameInputManager.HandleHexClick(yanevaHex);

    //     yield return new WaitForSeconds(1f);

    //     Debug.Log("Clicking Hex where 8. Yaneva is to confirm GroundBall target.");
    //     gameInputManager.HandleHexClick(yanevaHex);

    //     yield return new WaitForSeconds(1f); // Wait for ball to arrive
    // }

    // private IEnumerator AutomateMovementPhaseSequence()
    // {
    //     yield return MoveTokenToHex("2. Cafferata", new Vector3Int(0, 0, 5), 2f);
    //     yield return MoveTokenToHex("11. Ulisses", new Vector3Int(-4, 0, -6), 2f);
    //     yield return MoveTokenToHex("7. Jansen", new Vector3Int(8, 0, 9), 2f);
    //     yield return MoveTokenToHex("5. Murphy", new Vector3Int(12, 0, 9), 2f);
    //     yield return MoveTokenToHex("5. Vladoiu", new Vector3Int(-2, 0, 0), 2f);
    // }

    // private IEnumerator AutomateTackleSequence()
    // {
    //     yield return new WaitForSeconds(0.5f);

    //     Debug.Log("Pressing R to roll defender roll for tackle.");
    //     movementPhaseManager.PerformTackleDiceRoll(isDefender: true);

    //     yield return new WaitForSeconds(0.5f);

    //     Debug.Log("Pressing R to roll attacker roll for tackle.");
    //     movementPhaseManager.PerformTackleDiceRoll(isDefender: false);

    //     yield return new WaitForSeconds(0.5f);

    //     Debug.Log("Clicking Hex (-2, 1) to reposition 8. Yaneva.");
    //     HexCell repositionHex = HexGrid.GetHexCellAt(new Vector3Int(-2, 0, 1));
    //     PlayerToken yaneva = movementPhaseManager.GetTokenByName("8. Yaneva");
    //     yield return StartCoroutine(yaneva.JumpToHex(repositionHex));
    // }

    // private IEnumerator MoveTokenToHex(string tokenName, Vector3Int hexCoordinates, float waitTime)
    // {
    //     Debug.Log($"Clicking {tokenName} to select attacker to move.");
    //     PlayerToken token = movementPhaseManager.GetTokenByName(tokenName);
    //     HexCell hex = HexGrid.GetHexCellAt(hexCoordinates);

    //     gameInputManager.HandleTokenClick(token);

    //     yield return new WaitForSeconds(0.5f);

    //     Debug.Log($"Clicking Hex {hexCoordinates} to move {tokenName} there.");
    //     gameInputManager.HandleHexClick(hex);

    //     yield return new WaitForSeconds(waitTime); // Wait for token movement
    // }

    // private HexCell FindHexForToken(string tokenName)
    // {
    //     PlayerToken token = movementPhaseManager.GetTokenByName(tokenName);
    //     return token?.GetCurrentHex();
    // }
}
