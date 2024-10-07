using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;


public class GameInputManager : MonoBehaviour
{
    public CameraController cameraController;  
    public GroundBallManager groundBallManager;
    public LongBallManager longBallManager;
    public Ball ball;  
    public HexGrid hexGrid; 
    public MatchManager matchManager;

    public LayerMask tokenLayerMask;  // Layer for player tokens
    public LayerMask hexLayerMask;    // Layer for hex grid

    private Vector3 mouseDownPosition;  
    private bool isDragging = false;    
    public float dragThreshold = 10f;   

    void Start()
    {
        // TestHexConversions();
    }

    void Update()
    {
        cameraController.HandleCameraInput();
        HandleMouseInput();

        if (MatchManager.Instance.currentState == MatchManager.GameState.KickOffSetup && Input.GetKeyDown(KeyCode.Space))
        {
            MatchManager.Instance.StartMatch();
        }

        HandleSpecialInputs();
    }

    void HandleSpecialInputs()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerStandardPass();
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            MatchManager.Instance.TriggerMovement();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerHighPass();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            hexGrid.ClearHighlightedHexes(); 
            MatchManager.Instance.TriggerLongPass();
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;
            isDragging = false;
        }
        
        if (Input.GetMouseButton(0))
        {
            if (!isDragging && Vector3.Distance(mouseDownPosition, Input.mousePosition) > dragThreshold)
            {
                isDragging = true;
            }

            if (isDragging)
            {
                cameraController.HandleCameraInput();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!isDragging)
            {
                HandleClick();
            }
            isDragging = false;
        }
    }

    // This method handles the click logic for tokens and hexes
    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Check if we hit a token first
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, tokenLayerMask))
        {
            Ray hexRay = new Ray(hit.point + Vector3.up * 0.1f, Vector3.down);
            RaycastHit hexHit;

            // Raycast down to detect the hex beneath the token
            if (Physics.Raycast(hexRay, out hexHit, Mathf.Infinity, hexLayerMask))
            {
                HexCell clickedHex = hexHit.collider.GetComponent<HexCell>();
                if (clickedHex != null)
                {
                    HandleHexClick(clickedHex);
                }
            }
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, hexLayerMask))
        {
            HexCell clickedHex = hit.collider.GetComponent<HexCell>();
            if (clickedHex != null)
            {
                HandleHexClick(clickedHex);
            }
        }
    }

    private void HandleHexClick(HexCell hex)
    {
        Debug.Log($"Hex clicked: {hex.name}");

        // Check for Ground Ball and Long Ball state handling
        if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.StandardPassAttempt)
        {
            groundBallManager.HandleGroundBallPath(hex);
        }
        else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.LongBallAttempt)
        {
            longBallManager.HandleLongBallProcess(hex);
        }
    }

    // public void TestHexConversions()
    // {
    //     Vector3Int cubeCoords = new Vector3Int(3, -3, 0); 
    //     Vector2Int offsetCoords = HexGridUtils.CubeToOffset(cubeCoords);
    //     Vector3Int convertedBackToCube = HexGridUtils.OffsetToCube(offsetCoords.x, offsetCoords.y);

    //     Debug.Log($"Cube: {cubeCoords}, Offset: {offsetCoords}, Converted Back to Cube: {convertedBackToCube}");
    // }
}