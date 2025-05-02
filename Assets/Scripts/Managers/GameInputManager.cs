using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;

public class KeyPressData
    {
        public KeyCode key;
        public bool shift;
        public bool ctrl;
        public bool alt;
        public bool isConsumed;

        public KeyPressData(KeyCode key, bool shift, bool ctrl, bool alt)
        {
            this.key = key;
            this.shift = shift;
            this.ctrl = ctrl;
            this.alt = alt;
            this.isConsumed = false; // set default here
        }

        public override string ToString()
        {
            return $"{(ctrl ? "Ctrl+" : "")}{(alt ? "Alt+" : "")}{(shift ? "Shift+" : "")}{key}, , Consumed: {isConsumed}";
        }
    }

public class GameInputManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CameraController cameraController;  
    public GroundBallManager groundBallManager;
    public FirstTimePassManager firstTimePassManager;
    public LongBallManager longBallManager;
    public HighPassManager highPassManager;
    public MovementPhaseManager movementPhaseManager;
    public LooseBallManager looseBallManager;
    public HeaderManager headerManager;
    public FreeKickManager freeKickManager;
    public ShotManager shotManager;
    public FinalThirdManager finalThirdManager;
    public KickoffManager kickoffManager;
    public GoalKeeperManager goalKeeperManager;
    public Ball ball;  
    public HexGrid hexGrid; 
    public MatchManager matchManager;
    public static event Action<PlayerToken, HexCell> OnClick;
    public static event Action<PlayerToken, HexCell> OnHover;
    // public static event Action<KeyCode> OnKeyPress;
    public static event Action<KeyPressData> OnKeyPress;

    [Header("Layers")]
    public LayerMask tokenLayerMask;  // Layer for player tokens
    public LayerMask hexLayerMask;    // Layer for hex grid

    [Header("Hovers")]
    public PlayerToken hoveredToken = null;
    public HexCell hoveredHex = null;
    [Header("Clicks")]
    public PlayerToken clickedToken = null;
    public HexCell clickedHex = null;
    private Vector3 mouseDownPosition;  
    public bool isDragging = false;    
    [SerializeField]
    private bool logIsOn = false;    
    public float dragThreshold = 10f;

    void Update()
    {
        // cameraController.HandleCameraInput();
        ProcessInputs();
    }

    private void ProcessInputs()
    {
      HandleMouseHover();
      DetectMouseDrag();     // Drag overrides click
      HandleKeyPresses();
    }

    private void HandleMouseHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hover))
        {
            ClearHover();
            return;
        }
        
        var (newHoveredToken, newHoveredHex, isOOBClick) = DetectTokenOrHexClicked(hover);

        if (isOOBClick)
        {
            ClearHover();
            return;
        }

        // ✅ Only update hover state when it changes
        if (newHoveredToken != hoveredToken || newHoveredHex != hoveredHex)
        {
            hoveredToken = newHoveredToken;
            hoveredHex = newHoveredHex;
            // HandleHover(hoveredToken, hoveredHex);
            OnHover?.Invoke(hoveredToken, hoveredHex);  // 📣 Broadcast hover updates
        }
    }

    private void DetectMouseDrag()
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
                if (logIsOn) Debug.Log("🖱️ No drag → treat as click.");
                HandleMouseClick();
            }
            else
            {
                if (logIsOn) Debug.Log("🖱️ Drag ended.");
            }
            isDragging = false;
        }
    }

    private void HandleMouseClick()
    {
        if (logIsOn) Debug.Log("HandleMouseClick called!");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit click))
        {
            return;
        }
        else
        {
            if (logIsOn) Debug.Log("Raycast was successful");
            var (newClickedToken, newClickedHex, isOOBClick) = DetectTokenOrHexClicked(click);
            if (isOOBClick)
            {
                clickedToken = null;
                clickedHex = null;
                Debug.LogWarning("Out Of Bounds Plane hit, Sending empty Notification");
            }
            else
            {
                if (logIsOn) Debug.Log("Updating Clicked Items");
                // ✅ Only update clicked items when they change
                if (newClickedToken != clickedToken || newClickedHex != clickedHex)
                {
                    clickedToken = newClickedToken;
                    clickedHex = newClickedHex;
                }
            }
            OnClick?.Invoke(clickedToken, clickedHex);  // 📣 Broadcast the click event
        }
    }
    
    private void ClearHover()
    {
        if (hoveredToken != null || hoveredHex != null)
        {
            Debug.Log("🏁 Hover cleared.");
        }
        hoveredToken = null;
        hoveredHex = null;
    }
    
    private void HandleKeyPresses()
    {
        if (!Input.anyKeyDown) return;
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(kcode)) continue;
            if (kcode is
                KeyCode.LeftShift or KeyCode.RightShift or 
                KeyCode.LeftControl or KeyCode.RightControl or 
                KeyCode.LeftAlt or KeyCode.RightAlt or
                KeyCode.LeftCommand or KeyCode.RightCommand
            )
            {
                if (logIsOn) Debug.Log($"🔒 Modifier key ignored: {kcode}");
                continue;
            }
            KeyPressData data = new KeyPressData
            (
                kcode,
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand),
                Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
            );
            if (logIsOn) Debug.Log($"📢 KeyPress: {data}");
            OnKeyPress?.Invoke(data);
            break; // Stop after first match
        }
    }

    private void HandleHexClick(HexCell hex)
    {
        if (MatchManager.Instance.currentState == MatchManager.GameState.QuickThrow)
        {
            groundBallManager.HandleGroundBallPath(hex, true); // QuickThrow
        }
        else if (ball.IsBallSelected() && MatchManager.Instance.currentState == MatchManager.GameState.GoalKick)
        {
            highPassManager.HandleHighPassProcess(hex, true);
        }
    }
    
    public (PlayerToken inferredTokenFromClick, HexCell inferredHexCellFromClick, bool isOutOfBoundsClick) DetectTokenOrHexClicked(RaycastHit hit)
    {
        // Detect if we hit an Out-of-Bounds Plane
        if (hit.collider.name.Contains("Out-of-Bounds"))
        {
            if (logIsOn) Debug.Log($"🚫 Clicked on {hit.collider.name} - Out of Bounds area.");
            return (null, null, true);  // ✅ Indicate this was an OOB click
        }
        Ball clickedBall = hit.collider.GetComponent<Ball>();
        HexCell clickedBallHex = null;
        PlayerToken clickedBallToken = null;
        if (clickedBall != null)
        {
            clickedBallHex = clickedBall.GetCurrentHex(); 
            clickedBallToken = clickedBallHex?.GetOccupyingToken();
            if (logIsOn)
            {
                if (clickedBallToken != null) {Debug.Log($"Raycast hit the Ball, and {clickedBallToken.name} controls it, on {clickedBallHex.name}");}
                else {Debug.Log($"Raycast hit the Ball, on {clickedBallHex.name}");}
            }
        }
        PlayerToken clickedToken = hit.collider.GetComponent<PlayerToken>();
        HexCell clickedTokenHex = null;
        if (clickedToken != null)
        {
            clickedTokenHex = clickedToken.GetCurrentHex();
            if (logIsOn) Debug.Log($"Raycast hit {clickedToken.name} on {clickedTokenHex.name}.");
        }
        HexCell clickedHex = hit.collider.GetComponent<HexCell>();
        PlayerToken tokenOnClickedHex = null;
        if (clickedHex != null)
        {
            tokenOnClickedHex = clickedHex.GetOccupyingToken();
            if (logIsOn)
            {
                if (tokenOnClickedHex != null)
                {
                    Debug.Log($"Raycast hit {clickedHex.name}, where {tokenOnClickedHex.name} is on");
                }
                else 
                {
                    Debug.Log($"Raycast hit {clickedHex.name}, which is not occupied ");
                }
            }
        }
        PlayerToken inferredToken = clickedBallToken ?? clickedToken ?? tokenOnClickedHex ?? null;
        HexCell inferredHexCell = clickedBallHex ?? clickedTokenHex ?? clickedHex ?? null;
        if (logIsOn)
        {
            Debug.Log($"Inferred Clicked Token: {inferredToken?.name}");
            Debug.Log($"Inferred Clicked Hex: {inferredHexCell.name}"); 
        }
        return (inferredToken, inferredHexCell, false);      
    }

    public void SimulateClickAt(Vector2Int hexCoords)
    {
        Vector3Int coords = new Vector3Int(hexCoords.x, 0, hexCoords.y);
        HexCell hex = hexGrid.GetHexCellAt(coords);
        if (hex == null)
        {
            Debug.LogWarning($"No HexCell found at {coords}");
            return;
        }

        PlayerToken token = hex.GetOccupyingToken();

        Debug.Log($"🧪 Simulated click at {coords}. Hex: {hex.name}, Token: {(token != null ? token.name : "None")}");
        OnClick?.Invoke(token, hex);
    }

    public void SimulateKeyDataPress(KeyCode key, bool shift = false, bool ctrl = false, bool alt = false)
    {
        var keyData = new KeyPressData(key,shift,ctrl,alt);
        OnKeyPress?.Invoke(keyData);
    }

    public IEnumerator DelayedClick(Vector2Int pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        SimulateClickAt(pos);
    }

    public IEnumerator DelayedKeyDataPress(KeyCode key, float delay, bool shift = false, bool ctrl = false, bool alt = false)
    {
        yield return new WaitForSeconds(delay);
        SimulateKeyDataPress(key);
    }

    public string GetDebugStatus()
    {
        StringBuilder sb = new();
        sb.Append("GIM: ");

        if (clickedToken != null) sb.Append($"clickedToken: {clickedToken.name}, ");
        if (clickedHex != null) sb.Append($"clickedHex: {clickedHex.name}, ");
        if (hoveredToken != null) sb.Append($"hoveredToken: {hoveredToken.name}, ");
        if (hoveredHex != null) sb.Append($"hoveredHex: {hoveredHex.name}, ");

        if (sb.Length >= 2 && sb[^2] == ',') sb.Length -= 2; // Trim trailing comma
        return sb.ToString();
    }
}