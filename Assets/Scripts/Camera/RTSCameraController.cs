using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class RTSCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float sprintMultiplier = 2.5f; // Speed multiplier when sprinting
    public float dragSpeed = 0.5f;
    public float edgeScrollSpeed = 20f;
    public float panBorderThickness = 10f;
    public bool useEdgeScroll = true;
    public Vector2 minPosition;
    public Vector2 maxPosition;
    public bool isCamInverted = false;

    [Header("Viewport Settings")]
    [Tooltip("Camera viewport height (0-1). If viewport is smaller than screen, UI below viewport counts as edge.")]
    public float viewportHeight = 0.8f;
    [Tooltip("Camera viewport Y offset (0-1). Bottom of viewport where edge scrolling starts.")]
    public float viewportYOffset = 0.2f;

    [Header("Zoom")]
    public float zoomSpeed = 50f;
    public float minZoom = 15f;
    public float maxZoom = 80f;

    [Header("Rotation")]
    public float rotationSpeed = 60f; // degrees per second
    private float initialRotation; // Store initial Y rotation for reset

    private Camera cam;
    private Vector2 moveInput;
    private float zoomInput;
    private float rotationInput;
    private bool isSprinting = false;
    private bool isDragging = false;
    private Vector3 lastMousePos;

    private InputSystem_Actions inputActions;

    // Building placement reference
    private RTS.Managers.BuildingManager buildingManager;

    private PointerEventData cachedPointerEventData;
    private List<RaycastResult> cachedRaycastResults = new List<RaycastResult>();
    private void Awake()
    {
        if (EventSystem.current != null)
        {
            cachedPointerEventData = new PointerEventData(EventSystem.current);
        }

        if (minPosition == Vector2.zero)
        {  minPosition =new Vector2(-1000f,-1000f); }
        if (maxPosition == Vector2.zero)
        {  maxPosition =new Vector2(1000f,1000f); }
            cam = GetComponent<Camera>();
        inputActions = new InputSystem_Actions();

        // Store initial rotation
        initialRotation = transform.eulerAngles.y;

        // WASD / Arrow movement
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Scroll zoom
        inputActions.Player.Zoom.performed += ctx => zoomInput = ctx.ReadValue<float>();
        inputActions.Player.Zoom.canceled += ctx => zoomInput = 0f;

        // Rotation (Q / E)
        inputActions.Player.Rotate.performed += ctx => rotationInput = ctx.ReadValue<float>();
        inputActions.Player.Rotate.canceled += ctx => rotationInput = 0f;

        // Sprint (Shift)
        inputActions.Player.Sprint.performed += ctx => isSprinting = true;
        inputActions.Player.Sprint.canceled += ctx => isSprinting = false;
    }

    private void Start()
    {
        // Find BuildingManager to check placement state
        buildingManager = UnityEngine.Object.FindAnyObjectByType<RTS.Managers.BuildingManager>();
        if (buildingManager == null)
        {
            Debug.LogWarning("RTSCameraController: BuildingManager not found. Camera zoom will not be disabled during placement.");
        }
    }

    private bool IsMouseOverUI()
    {
        if (EventSystem.current == null)
            return false;

        // Initialize if needed (in case EventSystem wasn't ready at Awake)
        if (cachedPointerEventData == null)
        {
            cachedPointerEventData = new PointerEventData(EventSystem.current);
        }

        // Update position
        cachedPointerEventData.position = Mouse.current.position.ReadValue();

        // Clear previous results and raycast
        cachedRaycastResults.Clear();
        EventSystem.current.RaycastAll(cachedPointerEventData, cachedRaycastResults);

        return cachedRaycastResults.Count > 0;
    }

    private bool IsMouseOutsideViewport()
    {
        if (Mouse.current == null)
            return true;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Calculate viewport boundaries in screen space
        float viewportBottomY = Screen.height * viewportYOffset;
        float viewportTopY = Screen.height * (viewportYOffset + viewportHeight);

        // Check if mouse is outside screen bounds or outside viewport bounds
        if (mousePos.x < 0 || mousePos.x > Screen.width ||
            mousePos.y < 0 || mousePos.y > Screen.height)
        {
            return true;
        }

        // Check if mouse is below viewport (in UI area) or above viewport
        if (mousePos.y < viewportBottomY || mousePos.y > viewportTopY)
        {
            return true;
        }

        return false;
    }
    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleTouch();
        HandleRotation();
        HandleMiddleMouseDrag();
    }

    private void HandleMovement()
    {
        Vector3 dir = new Vector3(moveInput.x, 0, moveInput.y);

        // Edge scrolling - Check UI and viewport bounds
        if (useEdgeScroll && Mouse.current != null && !IsMouseOverUI())
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // Don't edge scroll if mouse is outside game view (especially in Unity Editor)
            if (mousePos.x < 0 || mousePos.x > Screen.width ||
                mousePos.y < 0 || mousePos.y > Screen.height)
            {
                // Mouse is outside the game view (e.g., over Inspector in editor)
                return;
            }

            // Calculate viewport boundaries in screen space
            float viewportBottomY = Screen.height * viewportYOffset;
            float viewportTopY = Screen.height * (viewportYOffset + viewportHeight);

            // Check if mouse is within the viewport's Y range (not in UI area below)
            bool isInViewportY = mousePos.y >= viewportBottomY && mousePos.y <= viewportTopY;

            if (isInViewportY)
            {
                // Vertical scrolling - Check top and bottom edges of viewport
                if (mousePos.y >= viewportTopY - panBorderThickness)
                {
                    dir.z += 1; // Scroll forward (camera moves up)
                }

                if (mousePos.y <= viewportBottomY + panBorderThickness)
                {
                    dir.z -= 1; // Scroll backward (camera moves down)
                }

                // Horizontal scrolling - Check left and right edges of screen
                if (mousePos.x >= Screen.width - panBorderThickness)
                {
                    dir.x += 1; // Scroll right
                }

                if (mousePos.x <= panBorderThickness)
                {
                    dir.x -= 1; // Scroll left
                }
            }
        }

        // Apply sprint multiplier when shift is held
        float currentSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

        Vector3 movement = Quaternion.Euler(0, transform.eulerAngles.y, 0) * dir.normalized * currentSpeed * Time.deltaTime;
        transform.position += movement;

        // Clamp position
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, minPosition.x, maxPosition.x),
            transform.position.y,
            Mathf.Clamp(transform.position.z, minPosition.y, maxPosition.y)
        );
    }
    private void HandleZoom()
    {
        // Disable zoom if mouse is over UI or outside viewport bounds
        if (IsMouseOverUI() || IsMouseOutsideViewport())
        {
            return;
        }

        // Disable zoom during building placement
        if (buildingManager != null && buildingManager.IsPlacing)
        {
            return;
        }

        float newZoom = cam.orthographic ? cam.orthographicSize - zoomInput * zoomSpeed * Time.deltaTime
                                         : cam.fieldOfView - zoomInput * zoomSpeed * Time.deltaTime;

        if (cam.orthographic)
            cam.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
        else
            cam.fieldOfView = Mathf.Clamp(newZoom, minZoom, maxZoom);
    }

    private void HandleRotation()
    {
        // Check for Q (rotate left) and E (rotate right) keys directly
        if (Keyboard.current != null)
        {
            // Check for Shift + Q/E for instant 90-degree snaps
            bool isShiftPressed = Keyboard.current.shiftKey.isPressed;

            if (isShiftPressed && Keyboard.current.qKey.wasPressedThisFrame)
            {
                // Snap 90 degrees left (counter-clockwise)
                transform.Rotate(Vector3.up, -90f, Space.World);
            }
            else if (isShiftPressed && Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Snap 90 degrees right (clockwise)
                transform.Rotate(Vector3.up, 90f, Space.World);
            }
            else
            {
                // Continuous rotation when just Q or E is held (without Shift)
                float rotation = 0f;

                if (Keyboard.current.qKey.isPressed)
                    rotation -= 1f; // Rotate left (counter-clockwise)

                if (Keyboard.current.eKey.isPressed)
                    rotation += 1f; // Rotate right (clockwise)

                if (Mathf.Abs(rotation) > 0.01f)
                {
                    transform.Rotate(Vector3.up, rotation * rotationSpeed * Time.deltaTime, Space.World);
                }
            }

            // Reset rotation to initial angle when Space is pressed
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Vector3 currentEuler = transform.eulerAngles;
                transform.eulerAngles = new Vector3(currentEuler.x, initialRotation, currentEuler.z);
            }
        }
    }

    private void HandleMiddleMouseDrag()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            isDragging = true;
            lastMousePos = Mouse.current.position.ReadValue();
        }
        else if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 mousePos = Mouse.current.position.ReadValue();
            Vector3 delta = mousePos - lastMousePos;
            lastMousePos = mousePos;

            if (isCamInverted)
                delta = -delta;

            // Convert screen drag to world-space motion relative to camera
            Vector3 right = Camera.main.transform.right;
            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0; // Keep movement horizontal
            forward.Normalize();

            Vector3 move = dragSpeed * Time.deltaTime * (right * delta.x + forward * delta.y);
            transform.position += move;
        }

    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null || Touchscreen.current.touches.Count == 0) return;

        if (Touchscreen.current.touches.Count == 1)
        {
            // Drag camera
            Vector2 delta = Touchscreen.current.touches[0].delta.ReadValue();
            transform.position += Quaternion.Euler(0, transform.eulerAngles.y, 0)
                                * new Vector3(-delta.x, 0, -delta.y)
                                * Time.deltaTime * moveSpeed * 0.5f;
        }

        if (Touchscreen.current.touches.Count >= 2)
        {
            // Pinch zoom
            Vector2 pos0 = Touchscreen.current.touches[0].position.ReadValue();
            Vector2 pos1 = Touchscreen.current.touches[1].position.ReadValue();
            Vector2 prev0 = pos0 - Touchscreen.current.touches[0].delta.ReadValue();
            Vector2 prev1 = pos1 - Touchscreen.current.touches[1].delta.ReadValue();

            float prevDist = Vector2.Distance(prev0, prev1);
            float currDist = Vector2.Distance(pos0, pos1);
            float pinch = currDist - prevDist;

            float newZoom = cam.orthographic ? cam.orthographicSize - pinch * 0.1f
                                             : cam.fieldOfView - pinch * 0.1f;

            if (cam.orthographic)
                cam.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
            else
                cam.fieldOfView = Mathf.Clamp(newZoom, minZoom, maxZoom);
        }
    }
}
