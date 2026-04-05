using UnityEngine;
using System;
using System.Text;
using System.Collections;
using TMPro;
using UnityEngine.UI;

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
    private static readonly Color HoverNameDefaultColor = Color.white;
    private static readonly Color HoverNameBookedColor = new(1f, 0.92f, 0.35f, 1f);
    private static readonly Color HoverNameInjuredColor = new(1f, 0.6f, 0.2f, 1f);

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
    [Header("Hover Name Label")]
    [SerializeField] private float hoverNameFontToTokenHeightRatio = 0.26f;
    [SerializeField] private float hoverNameOffsetToTokenHeightRatio = 0.08f;
    [SerializeField] private float hoverNameMinFontSize = 8f;
    [SerializeField] private float hoverNameMaxFontSize = 28f;
    [SerializeField] private float hoverNameMinOffsetPixels = 3f;
    [SerializeField] private float hoverNameMinWidthPixels = 52f;
    [SerializeField] private float hoverNameWidthToTokenRatio = 1.9f;
    [SerializeField] private float hoverNameHorizontalPaddingPixels = 8f;
    [SerializeField] private float hoverNameVerticalPaddingPixels = 2f;
    [SerializeField] private float hoverNameHeightToFontRatio = 1.05f;
    [Header("Clicks")]
    public PlayerToken clickedToken = null;
    public HexCell clickedHex = null;
    private Vector3 mouseDownPosition;  
    public bool isDragging = false;    
    [SerializeField]
    private bool logIsOn = false;    
    public float dragThreshold = 10f;
    private Canvas hoverNameCanvas;
    private RectTransform hoverNameRect;
    private TextMeshProUGUI hoverNameLabel;

    void Update()
    {
        // cameraController.HandleCameraInput();
        ProcessInputs();
    }

    private void ProcessInputs()
    {
      HandleMouseHover();
      UpdateHoverNameLabel();
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
                // CameraController polls its own drag/move/zoom inputs each frame.
                // Keep GameInputManager responsible only for classifying click vs drag.
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
        HideHoverNameLabel();
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
        // Scene-owned blockers explicitly mark out-of-bounds click catchers.
        if (hit.collider.GetComponentInParent<OutOfBoundsClickBlocker>() != null)
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

    private void UpdateHoverNameLabel()
    {
        if (hoveredToken == null)
        {
            HideHoverNameLabel();
            return;
        }

        EnsureHoverNameLabel();
        if (hoverNameLabel == null)
        {
            return;
        }

        hoverNameCanvas.gameObject.SetActive(true);
        Camera activeCamera = Camera.main;
        hoverNameLabel.text = hoveredToken.playerName;
        hoverNameLabel.color = GetHoverNameLabelColor(hoveredToken);
        if (activeCamera != null)
        {
            if (!TryGetHoverNameScreenMetrics(activeCamera, hoveredToken, out Vector3 screenPoint, out float tokenScreenWidth, out float tokenScreenHeight))
            {
                HideHoverNameLabel();
                return;
            }

            float fontSize = Mathf.Clamp(tokenScreenHeight * hoverNameFontToTokenHeightRatio, hoverNameMinFontSize, hoverNameMaxFontSize);
            hoverNameLabel.fontSize = fontSize;

            Vector2 preferredSize = hoverNameLabel.GetPreferredValues(hoverNameLabel.text);
            float labelWidth = Mathf.Max(hoverNameMinWidthPixels, preferredSize.x + hoverNameHorizontalPaddingPixels, tokenScreenWidth * hoverNameWidthToTokenRatio);
            float labelHeight = Mathf.Max(preferredSize.y + hoverNameVerticalPaddingPixels, fontSize * hoverNameHeightToFontRatio);
            hoverNameRect.sizeDelta = new Vector2(labelWidth, labelHeight);

            float pixelOffset = Mathf.Max(hoverNameMinOffsetPixels, tokenScreenHeight * hoverNameOffsetToTokenHeightRatio);
            hoverNameRect.position = screenPoint + new Vector3(0f, pixelOffset, 0f);
        }
        else
        {
            HideHoverNameLabel();
        }
    }

    private void EnsureHoverNameLabel()
    {
        if (hoverNameLabel != null)
        {
            return;
        }

        GameObject canvasObject = new("HoveredTokenNameCanvas");
        hoverNameCanvas = canvasObject.AddComponent<Canvas>();
        hoverNameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hoverNameCanvas.sortingOrder = 500;
        hoverNameCanvas.pixelPerfect = true;
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasScaler.scaleFactor = 1f;
        canvasScaler.referencePixelsPerUnit = 100f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject labelObject = new("HoveredTokenNameLabel");
        labelObject.transform.SetParent(canvasObject.transform, false);
        hoverNameRect = labelObject.AddComponent<RectTransform>();
        hoverNameRect.sizeDelta = new Vector2(120f, 24f);
        hoverNameRect.pivot = new Vector2(0.5f, 0f);
        hoverNameLabel = labelObject.AddComponent<TextMeshProUGUI>();
        hoverNameLabel.text = string.Empty;
        hoverNameLabel.enableAutoSizing = false;
        hoverNameLabel.fontSize = 14f;
        hoverNameLabel.alignment = TextAlignmentOptions.Bottom;
        hoverNameLabel.textWrappingMode = TextWrappingModes.NoWrap;
        hoverNameLabel.overflowMode = TextOverflowModes.Overflow;
        hoverNameLabel.outlineWidth = 0.1f;
        hoverNameLabel.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        hoverNameLabel.color = HoverNameDefaultColor;
        hoverNameLabel.raycastTarget = false;
        hoverNameCanvas.gameObject.SetActive(false);
    }

    private void HideHoverNameLabel()
    {
        if (hoverNameCanvas != null)
        {
            hoverNameCanvas.gameObject.SetActive(false);
        }
    }

    private static Color GetHoverNameLabelColor(PlayerToken token)
    {
        if (token == null)
        {
            return HoverNameDefaultColor;
        }

        if (token.isInjured)
        {
            return HoverNameInjuredColor;
        }

        if (token.isBooked)
        {
            return HoverNameBookedColor;
        }

        return HoverNameDefaultColor;
    }

    private static bool TryGetHoverNameScreenMetrics(
        Camera activeCamera,
        PlayerToken token,
        out Vector3 screenPoint,
        out float tokenScreenWidth,
        out float tokenScreenHeight)
    {
        screenPoint = Vector3.zero;
        tokenScreenWidth = 0f;
        tokenScreenHeight = 0f;

        if (activeCamera == null || token == null)
        {
            return false;
        }

        Renderer tokenRenderer = token.GetComponent<Renderer>();
        if (tokenRenderer != null)
        {
            Bounds bounds = tokenRenderer.bounds;
            Vector3[] corners =
            {
                new(bounds.min.x, bounds.min.y, bounds.min.z),
                new(bounds.min.x, bounds.min.y, bounds.max.z),
                new(bounds.min.x, bounds.max.y, bounds.min.z),
                new(bounds.min.x, bounds.max.y, bounds.max.z),
                new(bounds.max.x, bounds.min.y, bounds.min.z),
                new(bounds.max.x, bounds.min.y, bounds.max.z),
                new(bounds.max.x, bounds.max.y, bounds.min.z),
                new(bounds.max.x, bounds.max.y, bounds.max.z),
            };

            bool foundVisiblePoint = false;
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            foreach (Vector3 corner in corners)
            {
                Vector3 projectedCorner = activeCamera.WorldToScreenPoint(corner);
                if (projectedCorner.z <= 0f)
                {
                    continue;
                }

                foundVisiblePoint = true;
                minX = Mathf.Min(minX, projectedCorner.x);
                maxX = Mathf.Max(maxX, projectedCorner.x);
                minY = Mathf.Min(minY, projectedCorner.y);
                maxY = Mathf.Max(maxY, projectedCorner.y);
            }

            if (foundVisiblePoint)
            {
                tokenScreenWidth = Mathf.Max(1f, maxX - minX);
                tokenScreenHeight = Mathf.Max(1f, maxY - minY);
                screenPoint = new Vector3((minX + maxX) * 0.5f, maxY, 0f);
                return true;
            }
        }

        Vector3 fallbackPoint = activeCamera.WorldToScreenPoint(token.transform.position + Vector3.up);
        if (fallbackPoint.z <= 0f)
        {
            return false;
        }

        screenPoint = fallbackPoint;
        tokenScreenWidth = 32f;
        tokenScreenHeight = 32f;
        return true;
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

        Debug.Log($"🧪 Simulated click -> hex: {hex.name} @ {coords}, token: {(token != null ? token.name : "None")}");
        OnClick?.Invoke(token, hex);
    }

    public void SimulateKeyDataPress(KeyCode key, bool shift = false, bool ctrl = false, bool alt = false)
    {
        var keyData = new KeyPressData(key,shift,ctrl,alt);
        Debug.Log($"🧪 Simulated key press -> {FormatKeyChord(keyData)}");
        OnKeyPress?.Invoke(keyData);
    }

    public IEnumerator DelayedClick(Vector2Int pos, float delay)
    {
        Debug.Log($"🧪 Queue click after {delay:0.00}s -> ({pos.x}, {pos.y})");
        yield return new WaitForSeconds(delay);
        SimulateClickAt(pos);
    }

    public IEnumerator DelayedKeyDataPress(KeyCode key, float delay, bool shift = false, bool ctrl = false, bool alt = false)
    {
        Debug.Log($"🧪 Queue key press after {delay:0.00}s -> {FormatKeyChord(new KeyPressData(key, shift, ctrl, alt))}");
        yield return new WaitForSeconds(delay);
        SimulateKeyDataPress(key);
    }

    private static string FormatKeyChord(KeyPressData keyData)
    {
        if (keyData == null)
        {
            return "None";
        }

        StringBuilder builder = new();
        if (keyData.ctrl) builder.Append("Ctrl+");
        if (keyData.alt) builder.Append("Alt+");
        if (keyData.shift) builder.Append("Shift+");
        builder.Append(keyData.key);
        return builder.ToString();
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
