using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;


public class GameInputManager : MonoBehaviour
{
    public CameraController cameraController;  // Reference to the camera controller
    public GroundBallManager groundBallManager;
    public LongBallManager longBallManager;
    public Ball ball;  // Reference to the ball
    public HexGrid hexGrid;  // Add a reference to the HexGrid
    public MatchManager matchManager;
    // List to store highlighted hexes
    private List<HexCell> highlightedHexes = new List<HexCell>();
    // private HexCell currentTargetHex = null;   // The currently selected target hex
    // private HexCell lastClickedHex = null;     // The last hex that was clicked
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
                longBallManager.HandleLongBallProcess();
            }
            // Reset dragging state
            isDragging = false;
        }
    }

    public void TestHexConversions()
    {
        Vector3Int cubeCoords = new Vector3Int(3, -3, 0); // Example cube coordinates
        Vector2Int offsetCoords = HexGridUtils.CubeToOffset(cubeCoords);  // Convert cube to offset (even-q)
        Vector3Int convertedBackToCube = HexGridUtils.OffsetToCube(offsetCoords.x, offsetCoords.y);  // Convert offset back to cube

        Debug.Log($"Cube: {cubeCoords}, Offset: {offsetCoords}, Converted Back to Cube: {convertedBackToCube}");
    }

}
