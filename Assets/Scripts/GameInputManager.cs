using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;


public class GameInputManager : MonoBehaviour
{
    public CameraController cameraController;  // Reference to the camera controller
    public GroundBallManager groundBallManager;
    public Ball ball;  // Reference to the ball
    public HexGrid hexGrid;  // Add a reference to the HexGrid
    public MatchManager matchManager;
    // List to store highlighted hexes
    private List<HexCell> highlightedHexes = new List<HexCell>();
    private HexCell currentTargetHex = null;   // The currently selected target hex
    private HexCell lastClickedHex = null;     // The last hex that was clicked
    // Variables to track mouse movement for dragging
    private Vector3 mouseDownPosition;  // Where the mouse button was pressed
    private bool isDragging = false;    // Whether a drag is happening
    public float dragThreshold = 10f;   // Sensitivity to detect dragging vs. clicking (in pixels)
    
    void Start()
    {
        TestHexConversions();
    }

    void Update()
    {
        // Always handle camera movement with the keyboard, regardless of mouse input
        cameraController.HandleCameraInput();
        HandleMouseInput();
        // Handle game-specific inputs based on the current match state
        if (MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup && Input.GetKeyDown(KeyCode.Space))
        {
            MatchManager.Instance.StartMatch();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            MatchManager.Instance.TriggerStandardPass();
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            MatchManager.Instance.TriggerMovement();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            MatchManager.Instance.TriggerHighPass();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            MatchManager.Instance.TriggerLongPass();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            MatchManager.Instance.TriggerShot();
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            MatchManager.Instance.TriggerHeader();
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            MatchManager.Instance.TriggerFTP();
        }
    }

    void HandleMouseInput()
    {
        // When the left mouse button is pressed down
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;  // Store the initial mouse position
            isDragging = false;  // Reset dragging flag
        }
        // While the left mouse button is held down
        if (Input.GetMouseButton(0))
        {
            // Check if mouse movement exceeds the drag threshold
            if (!isDragging && Vector3.Distance(mouseDownPosition, Input.mousePosition) > dragThreshold)
            {
                isDragging = true;  // Consider this a drag
            }

            // If dragging, handle camera movement
            if (isDragging)
            {
                cameraController.HandleCameraInput();  // Move the camera
            }
        }
        // When the left mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            if (!isDragging && ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.StandardPassAttempt)
            {
                // Handle ball movement or path highlighting
                groundBallManager.HandleGroundBallPath();
            }
            if (!isDragging && ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.LongBallAttempt)
            {
                // Handle ball movement or path highlighting
                HandleLongBallPath();
            }
            // Reset dragging state
            isDragging = false;
        }
    }
    
    void HandleLongBallPath()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            HexCell clickedHex = hit.collider.GetComponent<HexCell>();
            if (clickedHex != null)
            {
                HexCell ballHex = ball.GetCurrentHex();
                if (ballHex == null)
                {
                    Debug.LogError("Ball's current hex is null! Ensure the ball has been placed on the grid.");
                    return;
                }
                else {
                    if (clickedHex == currentTargetHex && clickedHex == lastClickedHex)
                    {
                        // Double click on the same hex: confirm the move
                        StartCoroutine(HandleAirBallMovement(currentTargetHex));
                        ball.DeselectBall();
                    }
                    else
                    {
                        // First or new click on a different hex: highlight the path
                        // ClearHighlightedHexes();
                        // Highlight only the Target Hex
                        // TODO: Highlight 6 hexes the Target Hex
                        HighlightLongPassArea(clickedHex);
                        // clickedHex.HighlightHex("ballPath");
                        currentTargetHex = clickedHex;
                    }
                }
                lastClickedHex = clickedHex;  // Track the last clicked hex
            }
        }
    }
    
    private IEnumerator HandleAirBallMovement(HexCell targetHex)
    {
        // Ensure the ball and targetHex are valid
        if (ball == null)
        {
            Debug.LogError("Ball reference is null!");
            yield break;
        }

        if (targetHex == null)
        {
            Debug.LogError("Target Hex is null in HandleAirBallMovement!");
            yield break;
        }
        // Set thegame status to StandardPassMoving
        MatchManager.Instance.currentState = MatchManager.GameState.LongPassMoving;
        // Wait for the ball movement to complete
        yield return StartCoroutine(ball.MoveToCell(targetHex));
        // Set the game status to StandardPassCompleted
        MatchManager.Instance.currentState = MatchManager.GameState.LongPassCompleted;
        // Now clear the highlights after the movement
        // ClearHighlightedHexes();
        Debug.Log("Highlights cleared after ball movement.");
    }
    
    public void HighlightLongPassArea(HexCell targetHex)
    {
        // Get hexes within a radius (e.g., 6 hexes) around the targetHex
        int radius = 5;  // You can tweak this value as needed
        List<HexCell> hexesInRange = HexGrid.GetHexesInRange(hexGrid, targetHex, radius);

        // Loop through the hexes and highlight each one
        foreach (HexCell hex in hexesInRange)
        {
            // Highlight hexes (pass a specific color for Long Pass)
            hex.HighlightHex("longPass");  // Assuming HexHighlightReason.LongPass is defined for long pass highlights
            highlightedHexes.Add(hex);  // Track the highlighted hexes for later clearing
        }

        // Log the highlighted hexes if needed (optional)
        Debug.Log($"Highlighted {hexesInRange.Count} hexes around the target for a Long Pass.");
    }

    public void TestHexConversions()
    {
        Vector3Int cubeCoords = new Vector3Int(3, -3, 0); // Example cube coordinates
        Vector2Int offsetCoords = HexGridUtils.CubeToOffset(cubeCoords);  // Convert cube to offset (even-q)
        Vector3Int convertedBackToCube = HexGridUtils.OffsetToCube(offsetCoords.x, offsetCoords.y);  // Convert offset back to cube

        Debug.Log($"Cube: {cubeCoords}, Offset: {offsetCoords}, Converted Back to Cube: {convertedBackToCube}");
    }

}
