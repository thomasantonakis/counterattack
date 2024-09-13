using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float zoomSpeed = 15f;
    [SerializeField] private float dragSpeed = 30f;
    [SerializeField] private float rotationSpeed = 1000f;

    private Vector3 dragOrigin;
    private bool isRotating = false;

    // Arrays to store position and rotation for 4 camera presets
    private Vector3[] presetPositions = new Vector3[4];
    private Quaternion[] presetRotations = new Quaternion[4];

    // Initial setup in Start method
    void Start()
    {
        // Initialize preset 1 as the starting position (tabletop view)
        presetPositions[0] = new Vector3(0f, 45f, -45f);
        presetRotations[0] = Quaternion.Euler(45f, 0f, 0f);

        // Initialize preset 2 as the starting position (vertical view)
        presetPositions[1] = new Vector3(0f, 60f, 0f);  // Slot 2
        presetRotations[1] = Quaternion.Euler(90f, 0f, 0f);  // Slot 2

        presetPositions[2] = new Vector3(0f, 0.1f, -5f);  // Slot 3
        presetRotations[2] = Quaternion.Euler(0f, 0f, 0f);  // Slot 3

        presetPositions[3] = new Vector3(8f, 12f, -4f);  // Slot 4
        presetRotations[3] = Quaternion.Euler(55f, -60f, 0f);  // Slot 4

        // Set the initial camera position to Slot 1
        SetCameraToPreset(1);
    }

    // Public method to be called by GameInputManager
    public void HandleCameraInput()
    {
        HandleMovement();
        HandleZoom();
        HandleMouseDrag();
        HandleRotation();
        HandlePresetKeys();
    }

     // Handle saving and loading camera presets
    void HandlePresetKeys()
    {
        // Check if Shift is held
        bool isShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Press Shift + 1, 2, 3, or 4 to save the current camera state to the preset slots
        if (isShiftHeld)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SaveCurrentCameraToPreset(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SaveCurrentCameraToPreset(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SaveCurrentCameraToPreset(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SaveCurrentCameraToPreset(4);
        }
        else
        {
            // Press 1, 2, 3, or 4 to move the camera to preset slots when Shift is NOT held
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetCameraToPreset(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetCameraToPreset(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetCameraToPreset(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetCameraToPreset(4);
        }
    }

    // Set camera position and rotation to a preset slot
    void SetCameraToPreset(int presetIndex)
    {
        if (presetIndex < 1 || presetIndex > 4) return;  // Safety check
        transform.position = presetPositions[presetIndex - 1];
        transform.rotation = presetRotations[presetIndex - 1];
    }

    // Save the current camera position and rotation to a preset slot
    void SaveCurrentCameraToPreset(int presetIndex)
    {
        if (presetIndex < 1 || presetIndex > 4) return;  // Safety check
        presetPositions[presetIndex - 1] = transform.position;
        presetRotations[presetIndex - 1] = transform.rotation;
        Debug.Log($"Saved camera to preset {presetIndex}");
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 horizontalMove = transform.right * moveX + transform.forward * moveZ;
        horizontalMove.y = 0;
        transform.position += horizontalMove * moveSpeed * Time.deltaTime;
    }

    void HandleZoom()
    {
        if (Input.GetKey(KeyCode.Equals)) transform.position += transform.forward * zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Minus)) transform.position -= transform.forward * zoomSpeed * Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position += transform.forward * scroll * zoomSpeed;
    }

    void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(0) && !isRotating) dragOrigin = Input.mousePosition;

        if (Input.GetMouseButton(0) && !isRotating)
        {
            Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
            Vector3 move = new Vector3(pos.x * dragSpeed, 0, pos.y * dragSpeed);

            transform.Translate(-move, Space.World);
            dragOrigin = Input.mousePosition;
        }
    }

    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1)) isRotating = true;

        if (Input.GetMouseButton(1))
        {
            float rotX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            float rotY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            transform.Rotate(Vector3.up, rotX, Space.World);
            transform.Rotate(Vector3.right, -rotY, Space.Self);
        }

        if (Input.GetMouseButtonUp(1)) isRotating = false;
    }
}
