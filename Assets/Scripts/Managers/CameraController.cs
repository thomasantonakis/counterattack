using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float zoomSpeed = 15f;
    [SerializeField] private float rotationSpeed = 1000f;
    [SerializeField] private float minOrbitDistance = 12f;
    [SerializeField] private float maxOrbitDistance = 90f;
    [SerializeField] private float minPitch = 20f;
    [SerializeField] private float maxPitch = 90f;
    [SerializeField] private float screenAxisSamplePixels = 96f;
    public GameInputManager gameInputManager;
    private Vector3 dragAnchorWorld;
    private bool hasDragAnchor;
    private bool tracksPointerDrag;
    private bool pointerDragThresholdPassed;
    private Vector2 pointerDragStart;
    private bool isRotating = false;
    // Arrays to store position and rotation for 4 camera presets
    private Vector3[] presetPositions = new Vector3[4];
    private Quaternion[] presetRotations = new Quaternion[4];
    private Vector3[] presetPivots = new Vector3[4];
    private Vector3 orbitPivot;
    private float orbitYaw;
    private float orbitPitch;
    private float orbitDistance;
    private HexGrid hexGrid;
    private bool hasPitchBounds;
    private Bounds pitchBounds;
    [Header("Room authored framing")]
    [SerializeField] private RoomCameraEnvelope safeEnvelope;
    [SerializeField] private RectTransform sidePanel;
    [SerializeField, Range(0f, 0.15f)] private float framingMargin = 0.035f;
    [SerializeField, Min(0.01f)] private float framingResponse = 8f;
    private readonly bool[] customPresets = new bool[4];
    private readonly float[] presetProjectionShifts = new float[4];
    private float projectionShift;
    private int adaptivePreset;
    public bool IsReady { get; private set; }

    void Start()
    {
        hexGrid = FindAnyObjectByType<HexGrid>();
        orbitPivot = GetDefaultOrbitPivot();
        EnsurePitchBounds();

        // Initialize preset 1 as the starting position (tabletop view)
        presetPositions[0] = new Vector3(0f, 45f, -45f);
        presetRotations[0] = Quaternion.Euler(45f, 0f, 0f);

        // Initialize preset 2 as the starting position (vertical view)
        presetPositions[1] = new Vector3(0f, 60f, 0f);  // Slot 2
        presetRotations[1] = Quaternion.Euler(90f, 0f, 0f);  // Slot 2

        presetPositions[2] = new Vector3(-12f, 26f, -11f);
        presetRotations[2] = Quaternion.LookRotation(-presetPositions[2], Vector3.up);

        presetPositions[3] = new Vector3(-13f, 7f, -8f);
        presetRotations[3] = Quaternion.LookRotation(new Vector3(4f, 0f, 0f) - presetPositions[3], Vector3.up);

        for (int i = 0; i < presetPivots.Length; i++)
        {
            presetPivots[i] = GetOrbitPivotForView(presetPositions[i], presetRotations[i]);
        }

        // Set the initial camera position to Slot 1
        SetCameraToPreset(1);
        IsReady = true;
    }

    void Update()
    {
        ApplyProjectionShift(projectionShift);
        if (IsCameraInputBlocked)
        {
            isRotating = false;
            hasDragAnchor = false;
            tracksPointerDrag = false;
            return;
        }

        HandleCameraInput();
        if (adaptivePreset > 0 && !customPresets[adaptivePreset - 1])
        {
            GetAdaptivePresetPose(adaptivePreset, out Vector3 position, out Quaternion rotation);
            float t = 1f - Mathf.Exp(-framingResponse * Time.unscaledDeltaTime);
            ApplyPresentationPose(Vector3.Lerp(transform.position, position, t), Quaternion.Slerp(transform.rotation, rotation, t), Mathf.Lerp(projectionShift, GetAdaptiveProjectionShift(), t));
        }
        // HandlePresetKeys();
    }

    private static bool IsCameraInputBlocked => RoomInputOwnership.BlocksCamera
        || GameInputManager.IsPresentationModalActive && !HeaderChallengePanelController.IsVisible
            && !DuelResolutionPanelController.IsVisible && !StoppageActionsPanelController.IsVisible
        || MatchManager.Instance != null && MatchManager.Instance.IsGameplayInputBlocked;

    private void OnEnable()
    {
        // GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        // GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
        tracksPointerDrag = hasDragAnchor = isRotating = false;
        GetComponent<Camera>()?.ResetProjectionMatrix();
    }

    public void HandleCameraInput()
    {
        HandleMovement();
        HandleZoom();
        HandleMouseDrag();
        HandleRotation();
    }

    private void OnKeyReceived(KeyPressData keyData)
    {
        if (keyData == null
            || keyData.isConsumed
            || (MatchManager.Instance != null && MatchManager.Instance.IsGameplayInputBlocked))
        {
            return;
        }

        if (keyData.ctrl)
        {
            return;
        }

        if (TryHandlePresetKey(keyData)) return;

        if (IsCameraNavigationKey(keyData.key))
        {
            keyData.Consume(nameof(CameraController));
        }
    }

    // Shared with modal input routing, so camera presets do not reach gameplay subscribers.
    public bool TryHandlePresetKey(KeyPressData keyData)
    {
        if (keyData == null || keyData.isConsumed || keyData.ctrl
            || (MatchManager.Instance != null && MatchManager.Instance.IsGameplayInputBlocked)
            || !TryGetPresetIndex(keyData.key, out int presetIndex)) return false;

        if (keyData.shift) SaveCurrentCameraToPreset(presetIndex);
        else SetCameraToPreset(presetIndex);
        keyData.Consume(nameof(CameraController));
        return true;
    }

    private static bool TryGetPresetIndex(KeyCode key, out int presetIndex)
    {
        presetIndex = key switch
        {
            KeyCode.Alpha1 or KeyCode.Keypad1 => 1,
            KeyCode.Alpha2 or KeyCode.Keypad2 => 2,
            KeyCode.Alpha3 or KeyCode.Keypad3 => 3,
            KeyCode.Alpha4 or KeyCode.Keypad4 => 4,
            _ => 0
        };
        return presetIndex > 0;
    }

    private static bool IsCameraNavigationKey(KeyCode key)
    {
        return key == KeyCode.UpArrow
            || key == KeyCode.DownArrow
            || key == KeyCode.LeftArrow
            || key == KeyCode.RightArrow
            || key == KeyCode.Equals
            || key == KeyCode.Minus;
    }

    public void SetCameraToPreset(int presetIndex)
    {
        if (presetIndex < 1 || presetIndex > 4) return;  // Safety check
        adaptivePreset = presetIndex <= 2 && !customPresets[presetIndex - 1] ? presetIndex : 0;
        if (adaptivePreset > 0)
        {
            GetAdaptivePresetPose(presetIndex, out Vector3 position, out Quaternion rotation);
            ApplyPresentationPose(position, rotation, GetAdaptiveProjectionShift());
            return;
        }
        orbitPivot = ClampOrbitPivotToPitch(presetPivots[presetIndex - 1]);
        ApplyProjectionShift(presetProjectionShifts[presetIndex - 1]);
        transform.position = presetPositions[presetIndex - 1];
        transform.rotation = presetRotations[presetIndex - 1];
        SyncOrbitStateFromTransform();
        ApplyOrbitTransform();
    }

    void SaveCurrentCameraToPreset(int presetIndex)
    {
        if (presetIndex < 1 || presetIndex > 4) return;  // Safety check
        presetPositions[presetIndex - 1] = transform.position;
        presetRotations[presetIndex - 1] = transform.rotation;
        presetPivots[presetIndex - 1] = orbitPivot;
        presetProjectionShifts[presetIndex - 1] = projectionShift;
        customPresets[presetIndex - 1] = true;
        adaptivePreset = 0;
        Debug.Log($"Saved camera to preset {presetIndex}");
    }

    void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        GetPlanarNavigationAxes(out Vector3 planarRight, out Vector3 planarUp);

        if (Input.GetKey(KeyCode.UpArrow))
        {
            moveDirection += planarUp * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            moveDirection -= planarUp * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveDirection -= planarRight * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveDirection += planarRight * moveSpeed * Time.deltaTime;
        }

        if (moveDirection != Vector3.zero)
        {
            PanCamera(moveDirection);
        }
    }

    void HandleZoom()
    {
        float zoomDelta = 0f;
        if (Input.GetKey(KeyCode.Equals)) zoomDelta += zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Minus)) zoomDelta -= zoomSpeed * Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        zoomDelta += scroll * zoomSpeed * 10f;

        if (Mathf.Approximately(zoomDelta, 0f))
        {
            return;
        }

        ZoomBy(zoomDelta);
    }

    public void ZoomBy(float amount)
    {
        adaptivePreset = 0;
        float distance = Mathf.Clamp(orbitDistance - amount, minOrbitDistance, maxOrbitDistance);
        Vector3 offset = transform.position - orbitPivot;
        if (offset.sqrMagnitude < .0001f) offset = -transform.forward;
        transform.position = orbitPivot + offset.normalized * distance;
        ConstrainCurrentPose();
        SyncOrbitStateFromTransform();
    }

    void HandleMouseDrag()
    {
        HandlePointerDrag(Input.mousePosition, Input.GetMouseButtonDown(0),
            Input.GetMouseButton(0), Input.GetMouseButtonUp(0), GameInputManager.IsCameraPointerOverUi);
    }

    // Camera gestures have their own lifetime: dice panels intentionally suppress
    // GameInputManager's click/drag state while awaiting gameplay roll input.
    private void HandlePointerDrag(Vector2 position, bool pressed, bool held, bool released, bool pointerOverUi)
    {
        if (IsCameraInputBlocked)
        {
            tracksPointerDrag = hasDragAnchor = false;
            return;
        }
        if (pressed)
        {
            pointerDragThresholdPassed = false;
            pointerDragStart = position;
            tracksPointerDrag = !isRotating && !pointerOverUi;
            hasDragAnchor = tracksPointerDrag && TryGetPitchPlanePoint(position, out dragAnchorWorld);
        }

        if (released || !held)
        {
            tracksPointerDrag = hasDragAnchor = false;
            return;
        }

        float threshold = gameInputManager != null ? gameInputManager.dragThreshold : 10f;
        pointerDragThresholdPassed |= tracksPointerDrag && Vector2.Distance(pointerDragStart, position) > threshold;
        if (tracksPointerDrag && !isRotating && pointerDragThresholdPassed)
        {
            if (!hasDragAnchor)
            {
                hasDragAnchor = TryGetPitchPlanePoint(position, out dragAnchorWorld);
            }

            if (hasDragAnchor && TryGetPitchPlanePoint(position, out Vector3 currentMouseWorld))
            {
                Vector3 worldDelta = dragAnchorWorld - currentMouseWorld;
                worldDelta.y = 0f;
                if (worldDelta.sqrMagnitude > 0.000001f)
                {
                    PanCamera(worldDelta);
                }
            }
        }
    }

    void HandleRotation()
    {
        HandlePointerRotation(new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")),
            Input.GetMouseButtonDown(1), Input.GetMouseButton(1), Input.GetMouseButtonUp(1), Time.unscaledDeltaTime);
    }

    private void HandlePointerRotation(Vector2 delta, bool pressed, bool held, bool released, float elapsed)
    {
        if (IsCameraInputBlocked)
        {
            isRotating = false;
            return;
        }
        if (pressed)
        {
            isRotating = true;
            tracksPointerDrag = hasDragAnchor = false;
        }
        if (released || !held) { isRotating = false; return; }
        if (isRotating)
        {
            adaptivePreset = 0;
            float rotX = -delta.x * rotationSpeed * 0.2f * elapsed;
            float rotY = delta.y * rotationSpeed * 0.2f * elapsed;

            orbitYaw += rotX;
            orbitPitch = Mathf.Clamp(orbitPitch - rotY, minPitch, maxPitch);
            ApplyOrbitTransform();
        }
    }

    // TODO: restrict camera position to not drop under the table

    private void PanCamera(Vector3 worldDelta)
    {
        adaptivePreset = 0;
        orbitPivot = ClampOrbitPivotToPitch(orbitPivot + worldDelta);
        ApplyOrbitTransform();
    }

    private void SyncOrbitStateFromTransform()
    {
        Vector3 offset = transform.position - orbitPivot;
        if (offset.sqrMagnitude < 0.0001f)
        {
            offset = new Vector3(0f, 25f, -25f);
        }

        orbitDistance = Mathf.Clamp(offset.magnitude, minOrbitDistance, maxOrbitDistance);
        float horizontalDistance = new Vector2(offset.x, offset.z).magnitude;
        orbitPitch = Mathf.Clamp(Mathf.Atan2(offset.y, horizontalDistance) * Mathf.Rad2Deg, minPitch, maxPitch);
        orbitYaw = horizontalDistance < 0.0001f
            ? transform.eulerAngles.y
            : Mathf.Atan2(offset.x, -offset.z) * Mathf.Rad2Deg;
    }

    private void ApplyOrbitTransform()
    {
        float pitchRadians = orbitPitch * Mathf.Deg2Rad;
        float yawRadians = orbitYaw * Mathf.Deg2Rad;
        float horizontalDistance = Mathf.Cos(pitchRadians) * orbitDistance;
        Vector3 offset = new Vector3(
            Mathf.Sin(yawRadians) * horizontalDistance,
            Mathf.Sin(pitchRadians) * orbitDistance,
            -Mathf.Cos(yawRadians) * horizontalDistance);
        transform.position = orbitPivot + offset;
        Vector3 lookDirection = (orbitPivot - transform.position).normalized;
        Vector3 upReference = Mathf.Abs(Vector3.Dot(lookDirection, Vector3.up)) > 0.999f
            ? Quaternion.Euler(0f, orbitYaw, 0f) * Vector3.forward
            : Vector3.up;
        transform.rotation = Quaternion.LookRotation(lookDirection, upReference);
        ConstrainCurrentPose();
    }

    private void ConstrainCurrentPose()
    {
        if (safeEnvelope == null) return;
        Vector3 position = transform.position; Quaternion rotation = transform.rotation;
        safeEnvelope.Constrain(GetComponent<Camera>(), orbitPivot, ref position, ref rotation, minViewDistance: minOrbitDistance);
        transform.SetPositionAndRotation(position, rotation);
        orbitPivot = ClampOrbitPivotToPitch(GetOrbitPivotForView(position, rotation));
        SyncOrbitStateFromTransform();
    }

    public void ApplyPresentationPose(Vector3 position, Quaternion rotation, float? horizontalShift = null)
    {
        if (horizontalShift.HasValue) ApplyProjectionShift(horizontalShift.Value);
        orbitPivot = ClampOrbitPivotToPitch(GetOrbitPivotForView(position, rotation));
        transform.SetPositionAndRotation(position, rotation);
        ConstrainCurrentPose();
        SyncOrbitStateFromTransform();
    }

    private void ApplyProjectionShift(float shift)
    {
        projectionShift = shift;
        Camera camera = GetComponent<Camera>();
        Matrix4x4 projection = Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane);
        projection[0, 2] = shift;
        camera.projectionMatrix = projection;
    }

    public float GetAdaptiveProjectionShift() => 1f - 2f * GetAvailableViewport(GetComponent<Camera>()).center.x;

    public void GetAdaptivePresetPose(int preset, out Vector3 position, out Quaternion rotation)
    {
        EnsurePitchBounds();
        Camera camera = GetComponent<Camera>();
        rotation = Quaternion.Euler(preset == 2 ? 90f : 55f, 0f, 0f);
        Vector3 center = hasPitchBounds ? pitchBounds.center : GetDefaultOrbitPivot();
        Bounds bounds = hasPitchBounds ? pitchBounds : new Bounds(center, new Vector3(28f, 0.1f, 23f));
        Rect available = GetAvailableViewport(camera);
        float shift = 1f - 2f * available.center.x;
        float tanV = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f), tanH = tanV * camera.aspect;
        float low = minOrbitDistance, high = maxOrbitDistance;
        position = center - rotation * Vector3.forward * high;
        Quaternion desiredRotation = rotation;
        for (int iteration = 0; iteration < 24; iteration++)
        {
            float distance = (low + high) * 0.5f;
            Vector3 target = center;
            Vector3 candidate = target - desiredRotation * Vector3.forward * distance;
            Quaternion candidateRotation = desiredRotation;
            safeEnvelope?.Constrain(camera, target, ref candidate, ref candidateRotation, shift, minOrbitDistance);
            bool fits = true;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 world = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                Vector3 local = Quaternion.Inverse(candidateRotation) * (world - candidate);
                Vector2 viewport = new(0.5f - shift * 0.5f + local.x / (2f * local.z * tanH), 0.5f + local.y / (2f * local.z * tanV));
                if (local.z <= 0f || !available.Contains(viewport)) fits = false;
            }
            if (fits) { high = distance; position = candidate; rotation = candidateRotation; }
            else low = distance;
        }
    }

    private Rect GetAvailableViewport(Camera camera)
    {
        Rect result = new(framingMargin, framingMargin, 1f - framingMargin * 2f, 1f - framingMargin * 2f);
        if (sidePanel == null || !sidePanel.gameObject.activeInHierarchy || camera.pixelWidth <= 0) return result;
        var corners = new Vector3[4]; sidePanel.GetWorldCorners(corners);
        Canvas canvas = sidePanel.GetComponentInParent<Canvas>();
        var canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        if (canvasRect == null || canvasRect.rect.width <= 0f) return result;
        float left = (canvasRect.InverseTransformPoint(corners[0]).x - canvasRect.rect.xMin) / canvasRect.rect.width;
        float right = (canvasRect.InverseTransformPoint(corners[2]).x - canvasRect.rect.xMin) / canvasRect.rect.width;
        if (left >= 0.5f && left < 1f) result.xMax = Mathf.Max(0.3f, left - framingMargin);
        else if (right > 0f && right <= 0.5f) result.xMin = Mathf.Min(0.7f, right + framingMargin);
        return result;
    }

    private Vector3 GetDefaultOrbitPivot()
    {
        if (hexGrid == null)
        {
            return Vector3.zero;
        }

        return hexGrid.transform.TransformPoint(hexGrid.gridCenter);
    }

    private Vector3 GetOrbitPivotForView(Vector3 cameraPosition, Quaternion cameraRotation)
    {
        Vector3 defaultPivot = GetDefaultOrbitPivot();
        Plane pitchPlane = new Plane(Vector3.up, defaultPivot);
        Ray viewRay = new Ray(cameraPosition, cameraRotation * Vector3.forward);

        if (pitchPlane.Raycast(viewRay, out float enterDistance))
        {
            return viewRay.GetPoint(enterDistance);
        }

        return defaultPivot;
    }

    private void GetPlanarNavigationAxes(out Vector3 planarRight, out Vector3 planarUp)
    {
        if (TryGetScreenPlanarAxes(out planarRight, out planarUp))
        {
            return;
        }

        planarRight = Vector3.right;
        planarUp = Vector3.forward;
    }

    private bool TryGetScreenPlanarAxes(out Vector3 planarRight, out Vector3 planarUp)
    {
        planarRight = Vector3.right;
        planarUp = Vector3.forward;

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return false;
        }

        Vector2 screenCenter = new Vector2(activeCamera.pixelWidth * 0.5f, activeCamera.pixelHeight * 0.5f);
        if (!TryGetPitchPlanePoint(screenCenter, out Vector3 centerPoint)
            || !TryGetPitchPlanePoint(screenCenter + (Vector2.right * screenAxisSamplePixels), out Vector3 rightPoint)
            || !TryGetPitchPlanePoint(screenCenter + (Vector2.up * screenAxisSamplePixels), out Vector3 upPoint))
        {
            return false;
        }

        planarRight = rightPoint - centerPoint;
        planarRight.y = 0f;
        planarUp = upPoint - centerPoint;
        planarUp.y = 0f;

        if (planarRight.sqrMagnitude < 0.000001f || planarUp.sqrMagnitude < 0.000001f)
        {
            return false;
        }

        planarRight.Normalize();
        planarUp.Normalize();
        return true;
    }

    private bool TryGetPitchPlanePoint(Vector2 screenPoint, out Vector3 worldPoint)
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            worldPoint = Vector3.zero;
            return false;
        }

        float planeHeight = hexGrid != null ? hexGrid.transform.position.y : 0f;
        Plane pitchPlane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
        Ray ray = activeCamera.ScreenPointToRay(screenPoint);
        if (pitchPlane.Raycast(ray, out float enterDistance))
        {
            worldPoint = ray.GetPoint(enterDistance);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    private Vector3 ClampOrbitPivotToPitch(Vector3 pivot)
    {
        if (!EnsurePitchBounds())
        {
            return pivot;
        }

        pivot.x = Mathf.Clamp(pivot.x, pitchBounds.min.x, pitchBounds.max.x);
        pivot.z = Mathf.Clamp(pivot.z, pitchBounds.min.z, pitchBounds.max.z);
        return pivot;
    }

    private bool EnsurePitchBounds()
    {
        if (hasPitchBounds)
        {
            return true;
        }

        if (hexGrid == null || hexGrid.cells == null)
        {
            return false;
        }

        bool foundAnyCell = false;
        Vector3 min = new Vector3(float.MaxValue, 0f, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, 0f, float.MinValue);

        foreach (HexCell cell in hexGrid.cells)
        {
            if (cell == null || (cell.isOutOfBounds && cell.isInGoal == 0))
            {
                continue;
            }

            foreach (Vector3 corner in cell.GetHexCorners())
            {
                foundAnyCell = true;
                min.x = Mathf.Min(min.x, corner.x);
                min.z = Mathf.Min(min.z, corner.z);
                max.x = Mathf.Max(max.x, corner.x);
                max.z = Mathf.Max(max.z, corner.z);
            }
        }

        if (!foundAnyCell)
        {
            return false;
        }

        Vector3 size = new Vector3(max.x - min.x, 0.1f, max.z - min.z);
        Vector3 center = new Vector3((min.x + max.x) * 0.5f, hexGrid.transform.position.y, (min.z + max.z) * 0.5f);
        pitchBounds = new Bounds(center, size);
        hasPitchBounds = true;
        return true;
    }
}
