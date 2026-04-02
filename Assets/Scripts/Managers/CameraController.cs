using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float zoomSpeed = 15f;
    [SerializeField] private float dragSpeed = 30f;
    [SerializeField] private float rotationSpeed = 1000f;
    [SerializeField] private float minOrbitDistance = 12f;
    [SerializeField] private float maxOrbitDistance = 90f;
    [SerializeField] private float minPitch = 20f;
    [SerializeField] private float maxPitch = 90f;
    public GameInputManager gameInputManager;
    private Vector3 dragOrigin;
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

    void Start()
    {
        hexGrid = FindObjectOfType<HexGrid>();
        orbitPivot = GetDefaultOrbitPivot();

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
        orbitPivot = presetPivots[presetIndex - 1];
        transform.position = presetPositions[presetIndex - 1];
        transform.rotation = presetRotations[presetIndex - 1];
        SyncOrbitStateFromTransform();
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
        // Check if the camera is in vertical view (looking straight down)
        bool isVerticalView = Mathf.Approximately(transform.rotation.eulerAngles.x, 90f);

        if (isVerticalView)
        {
            // Handle movement differently for vertical view (top-down camera)
            if (Input.GetKey(KeyCode.UpArrow))
            {
                moveDirection += new Vector3(0, 0, moveSpeed * Time.deltaTime);  // Move "up" on the Z-axis
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                moveDirection -= new Vector3(0, 0, moveSpeed * Time.deltaTime);  // Move "down" on the Z-axis
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                moveDirection -= new Vector3(moveSpeed * Time.deltaTime, 0, 0);  // Move "left" on the X-axis
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                moveDirection += new Vector3(moveSpeed * Time.deltaTime, 0, 0);  // Move "right" on the X-axis
            }
        }
        else
        {
            // For other camera views, use forward and right directions on the XZ plane
            Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;

            if (Input.GetKey(KeyCode.UpArrow))
            {
                moveDirection += forward * moveSpeed * Time.deltaTime;  // Move forward
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                moveDirection -= forward * moveSpeed * Time.deltaTime;  // Move backward
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                moveDirection -= right * moveSpeed * Time.deltaTime;  // Move left
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                moveDirection += right * moveSpeed * Time.deltaTime;  // Move right
            }
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
            dragOrigin = Input.mousePosition;
        }

        // Pan only after GameInputManager has classified the left mouse as a drag,
        // so simple clicks on tokens/hexes do not move the camera.
        if (Input.GetMouseButton(0) && !isRotating && gameInputManager != null && gameInputManager.isDragging)
        {
            Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
            Vector3 right = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
            Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            Vector3 move = (-right * pos.x * dragSpeed) + (-forward * pos.y * dragSpeed);

            PanCamera(move);
            dragOrigin = Input.mousePosition;
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
        orbitPivot += worldDelta;
        transform.position += worldDelta;
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
        transform.rotation = Quaternion.LookRotation((orbitPivot - transform.position).normalized, Vector3.up);
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
}
