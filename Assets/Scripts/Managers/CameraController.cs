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

    void Start()
    {
        hexGrid = FindFirstObjectByType<HexGrid>();
        orbitPivot = GetDefaultOrbitPivot();
        EnsurePitchBounds();

        // Initialize preset 1 as the starting position (tabletop view)
        presetPositions[0] = new Vector3(0f, 45f, -45f);
        presetRotations[0] = Quaternion.Euler(45f, 0f, 0f);

        // Initialize preset 2 as the starting position (vertical view)
        presetPositions[1] = new Vector3(0f, 60f, 0f);  // Slot 2
        presetRotations[1] = Quaternion.Euler(90f, 0f, 0f);  // Slot 2

        presetPositions[2] = new Vector3(-4f, 33f, -5f);  // Slot 3
        presetRotations[2] = Quaternion.Euler(90f, 0f, 0f);  // Slot 3

        presetPositions[3] = new Vector3(8f, 12f, -4f);  // Slot 4
        presetRotations[3] = Quaternion.Euler(55f, -60f, 0f);  // Slot 4

        for (int i = 0; i < presetPivots.Length; i++)
        {
            presetPivots[i] = GetOrbitPivotForView(presetPositions[i], presetRotations[i]);
        }

        // Set the initial camera position to Slot 1
        SetCameraToPreset(1);
    }

    void Update()
    {
        HandleCameraInput();
        // HandlePresetKeys();
    }

    private void OnEnable()
    {
        // GameInputManager.OnClick += OnClickReceived;
        GameInputManager.OnKeyPress += OnKeyReceived;
    }

    private void OnDisable()
    {
        // GameInputManager.OnClick -= OnClickReceived;
        GameInputManager.OnKeyPress -= OnKeyReceived;
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
        if (keyData.shift)
        {
            // Check if Shift is held
            // Press Shift + 1, 2, 3, or 4 to save the current camera state to the preset slots
            if (keyData.key == KeyCode.Alpha1) SaveCurrentCameraToPreset(1);
            if (keyData.key == KeyCode.Alpha2) SaveCurrentCameraToPreset(2);
            if (keyData.key == KeyCode.Alpha3) SaveCurrentCameraToPreset(3);
            if (keyData.key == KeyCode.Alpha4) SaveCurrentCameraToPreset(4);
        }
        else
        {
            // Press 1, 2, 3, or 4 to move the camera to preset slots when Shift is NOT held
            if (keyData.key == KeyCode.Alpha1) SetCameraToPreset(1);
            if (keyData.key == KeyCode.Alpha2) SetCameraToPreset(2);
            if (keyData.key == KeyCode.Alpha3) SetCameraToPreset(3);
            if (keyData.key == KeyCode.Alpha4) SetCameraToPreset(4);
        }
    }

    void SetCameraToPreset(int presetIndex)
    {
        if (presetIndex < 1 || presetIndex > 4) return;  // Safety check
        orbitPivot = ClampOrbitPivotToPitch(presetPivots[presetIndex - 1]);
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

        orbitDistance = Mathf.Clamp(orbitDistance - zoomDelta, minOrbitDistance, maxOrbitDistance);
        ApplyOrbitTransform();
    }

    void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(0) && !isRotating)
        {
            hasDragAnchor = TryGetPitchPlanePoint(Input.mousePosition, out dragAnchorWorld);
        }

        if (Input.GetMouseButtonUp(0))
        {
            hasDragAnchor = false;
        }

        // Pan only after GameInputManager has classified the left mouse as a drag,
        // so simple clicks on tokens/hexes do not move the camera.
        if (Input.GetMouseButton(0) && !isRotating && gameInputManager != null && gameInputManager.isDragging)
        {
            if (!hasDragAnchor)
            {
                hasDragAnchor = TryGetPitchPlanePoint(Input.mousePosition, out dragAnchorWorld);
            }

            if (hasDragAnchor && TryGetPitchPlanePoint(Input.mousePosition, out Vector3 currentMouseWorld))
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
        if (Input.GetMouseButtonDown(1)) isRotating = true;

        if (Input.GetMouseButton(1))
        {
            float rotX = -Input.GetAxis("Mouse X") * rotationSpeed * 0.2f * Time.deltaTime;
            float rotY = Input.GetAxis("Mouse Y") * rotationSpeed * 0.2f * Time.deltaTime;

            orbitYaw += rotX;
            orbitPitch = Mathf.Clamp(orbitPitch - rotY, minPitch, maxPitch);
            ApplyOrbitTransform();
        }

        if (Input.GetMouseButtonUp(1)) isRotating = false;
    }

    // TODO: restrict camera position to not drop under the table

    private void PanCamera(Vector3 worldDelta)
    {
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
