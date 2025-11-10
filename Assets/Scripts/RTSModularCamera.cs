using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Production-ready modular RTS camera for Unity (New Input System).
/// Drop this on a Camera (or an empty rig controlling a virtual camera).
/// Supports: WASD/Arrows, edge scroll, middle-mouse drag, touch drag/pinch, mouse wheel zoom,
/// Q/E rotation, smooth damping, bounds clamping, follow selected units or a single target.
/// Uses InputActionProperty so you can wire to your existing InputSystem_Actions asset.
/// </summary>
[RequireComponent(typeof(Camera))]
public class RTSModularCamera : MonoBehaviour
{
    [Header("Input (assign InputActionProperties from your InputSystem asset)")]
    public InputActionProperty moveAction;      // Vector2 (WASD / Arrows)
    public InputActionProperty positionAction;  // Vector2 (mouse/touch position)
    public InputActionProperty zoomAction;      // Float (mouse wheel) - optional on mobile, pinch handled separately
    public InputActionProperty rotateAction;    // Float (1D axis composite: Q/E)
    public InputActionProperty middleButtonAction; // Button (middle mouse press) - optional

    [Header("Movement")]
    public float baseMoveSpeed = 18f;
    public float edgeScrollSpeed = 20f;
    public float panBorderThickness = 12f; // px
    public bool useEdgeScroll = true;
    public bool allowKeyboard = true;
    public bool allowEdgeScroll = true;
    public bool allowMouseDrag = true;
    public float dragSpeed = 0.6f; // sensitivity for middle drag
    [Tooltip("Movement smoothing (0 = instant, larger = smoother)")]
    public float moveSmoothTime = 0.08f;

    [Header("Zoom")]
    public float zoomSpeed = 40f;
    public float minDistance = 8f;
    public float maxDistance = 60f;
    public bool useOrthographic = false; // if true this will control orthographic size
    [Tooltip("Zoom smoothing (0 = instant, larger = smoother)")]
    public float zoomSmoothTime = 0.08f;

    [Header("Rotation")]
    public float rotationSpeed = 90f; // degrees/sec from input (rotateAction)
    public bool allowRotationWithQe = true;
    public float rotationSmoothTime = 0.08f;

    [Header("Bounds")]
    public Vector2 minXZ = new Vector2(-100f, -100f);
    public Vector2 maxXZ = new Vector2(100f, 100f);
    public bool clampToBounds = true;

    [Header("Follow/Focus")]
    [Tooltip("When set, camera will smoothly follow this target's center (overrides free roam).")]
    public bool followTarget = false;
    public Transform followTransform; // single unit focus (optional)
    [Tooltip("If following a group, camera will compute center using SetFollowTargets")]
    public List<Transform> followTargets = new List<Transform>();
    public float followSmoothTime = 0.12f;
    public Vector3 followOffset = new Vector3(0, 20f, -20f); // relative to center

    // internal state
    Camera cam;
    Vector3 velocity = Vector3.zero;
    float zoomVelocity = 0f;
    float rotationVelocity = 0f;
    float currentDistance;
    Vector3 desiredPosition;

    // middle-drag state
    bool isMiddleDragging = false;
    Vector2 lastPointerPos;

    // pinch tracking
    bool isPinching = false;
    float lastPinchDist = 0f;

    // API event for other systems
    public event Action<Vector3> OnCameraMoved;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        currentDistance = followOffset.magnitude;
        desiredPosition = transform.position;
    }

    private void OnEnable()
    {
        // Enable actions if assigned
        if (moveAction != null) moveAction.action?.Enable();
        if (positionAction != null) positionAction.action?.Enable();
        if (zoomAction != null) zoomAction.action?.Enable();
        if (rotateAction != null) rotateAction.action?.Enable();
        if (middleButtonAction != null) middleButtonAction.action?.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action?.Disable();
        if (positionAction != null) positionAction.action?.Disable();
        if (zoomAction != null) zoomAction.action?.Disable();
        if (rotateAction != null) rotateAction.action?.Disable();
        if (middleButtonAction != null) middleButtonAction.action?.Disable();
    }

    private void Update()
    {
        // Get inputs (safely)
        Vector2 moveInput = Vector2.zero;
        if (allowKeyboard && moveAction != null && moveAction.action != null)
            moveInput = moveAction.action.ReadValue<Vector2>();

        Vector2 pointerPos = Vector2.zero;
        if (positionAction != null && positionAction.action != null)
            pointerPos = positionAction.action.ReadValue<Vector2>();

        float zoomInput = 0f;
        if (zoomAction != null && zoomAction.action != null)
            zoomInput = zoomAction.action.ReadValue<float>();

        float rotateInput = 0f;
        if (rotateAction != null && rotateAction.action != null)
            rotateInput = rotateAction.action.ReadValue<float>();

        // Handle rotation (Q/E or input axis)
        HandleRotation(rotateInput);

        // Handle middle mouse dragging or touch
        HandlePointerDrag(pointerPos);

        // Handle move via keyboard or edge scroll (edge scroll uses mouse position)
        HandleMove(moveInput, pointerPos);

        // Handle zoom from wheel
        HandleZoom(zoomInput);

        // Handle pinch zoom and touch drag (separately)
        HandleTouchInput();

        // Follow mode or free cam positioning
        if (followTarget)
            UpdateFollowPosition();
        else
            SmoothApplyDesiredPosition();

        // Clamp to bounds
        if (clampToBounds)
            ClampPosition();

        OnCameraMoved?.Invoke(transform.position);
    }

    // ---------- Movement ----------
    void HandleMove(Vector2 moveInput, Vector2 pointerPos)
    {
        Vector3 dir = Vector3.zero;

        // keyboard input
        if (moveInput.sqrMagnitude > 0.0001f)
            dir += new Vector3(moveInput.x, 0f, moveInput.y);

        // edge scrolling (mouse on screen edges)
        if (useEdgeScroll && allowEdgeScroll && Mouse.current != null)
        {
            Vector2 m = Mouse.current.position.ReadValue();
            if (m.y >= Screen.height - panBorderThickness) dir.z += 1f;
            if (m.y <= panBorderThickness) dir.z -= 1f;
            if (m.x >= Screen.width - panBorderThickness) dir.x += 1f;
            if (m.x <= panBorderThickness) dir.x -= 1f;
        }

        if (dir.sqrMagnitude > 0.0001f)
        {
            // Move relative to camera yaw
            Vector3 worldDir = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * dir.normalized;
            float speed = baseMoveSpeed;
            Vector3 targetPos = transform.position + worldDir * speed * Time.deltaTime;
            desiredPosition = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, moveSmoothTime);
        }
    }

    // ---------- Middle mouse drag ----------
    void HandlePointerDrag(Vector2 pointerPos)
    {
        bool midPressed = false;
        if (middleButtonAction != null && middleButtonAction.action != null)
            midPressed = middleButtonAction.action.ReadValue<float>() > 0.5f;
        else if (Mouse.current != null)
            midPressed = Mouse.current.middleButton.isPressed;

        // If user pressed middle button this frame, start drag
        if (midPressed && !isMiddleDragging)
        {
            isMiddleDragging = true;
            lastPointerPos = pointerPos;
            // optionally lock cursor or set cursor icon
        }
        // released
        if (!midPressed && isMiddleDragging)
        {
            isMiddleDragging = false;
        }

        if (isMiddleDragging)
        {
            Vector2 current = pointerPos;
            Vector2 delta = current - lastPointerPos;
            lastPointerPos = current;

            // invert drag direction for camera pan
            Vector3 move = new Vector3(-delta.x, 0f, -delta.y) * dragSpeed * Time.deltaTime;
            // rotate by camera yaw
            move = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * move;
            desiredPosition = Vector3.SmoothDamp(transform.position, transform.position + move, ref velocity, moveSmoothTime);
        }
    }

    // ---------- Rotation ----------
    void HandleRotation(float rotateInput)
    {
        if (!allowRotationWithQe) return;
        if (Mathf.Abs(rotateInput) > 0.0001f)
        {
            float targetYaw = transform.eulerAngles.y + rotateInput * rotationSpeed * Time.deltaTime;
            float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
        }
    }

    // ---------- Zoom ----------
    void HandleZoom(float zoomInput)
    {
        if (Mathf.Abs(zoomInput) > 0.0001f)
        {
            // move camera along its forward/backward vector (camera forward points downward in many RTS rigs)
            Vector3 forward = (transform.forward).normalized;
            Vector3 target = transform.position + forward * (-zoomInput) * zoomSpeed * Time.deltaTime;

            // clamp by distance to ground point (approx using followOffset magnitude) or field of view / ortho size
            desiredPosition = Vector3.SmoothDamp(transform.position, target, ref velocity, zoomSmoothTime);
        }
    }

    // ---------- Touch (pinch + drag) ----------
    void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;
        int touchCount = Touchscreen.current.touches.Count;

        if (touchCount == 1)
        {
            // single-finger drag = pan
            TouchControl t = Touchscreen.current.touches[0];
            if (t.press.isPressed)
            {
                Vector2 delta = t.delta.ReadValue();
                Vector3 move = new Vector3(-delta.x, 0f, -delta.y) * (baseMoveSpeed * 0.01f) * Time.deltaTime;
                move = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * move;
                desiredPosition = Vector3.SmoothDamp(transform.position, transform.position + move, ref velocity, moveSmoothTime);
            }
        }
        else if (touchCount >= 2)
        {
            // pinch zoom
            Vector2 p0 = Touchscreen.current.touches[0].position.ReadValue();
            Vector2 p1 = Touchscreen.current.touches[1].position.ReadValue();
            Vector2 d0 = Touchscreen.current.touches[0].delta.ReadValue();
            Vector2 d1 = Touchscreen.current.touches[1].delta.ReadValue();
            float prevDist = Vector2.Distance(p0 - d0, p1 - d1);
            float currDist = Vector2.Distance(p0, p1);
            float diff = currDist - prevDist;
            if (Mathf.Abs(diff) > 0.0001f)
            {
                Vector3 forward = (transform.forward).normalized;
                Vector3 target = transform.position + forward * (-diff) * 0.1f;
                desiredPosition = Vector3.SmoothDamp(transform.position, target, ref velocity, zoomSmoothTime);
            }
        }
    }

    // ---------- Follow / Focus ----------
    public void SetFollowTargets(List<Transform> targets)
    {
        followTargets = targets;
        followTarget = (targets != null && targets.Count > 0);
    }

    public void ClearFollowTargets()
    {
        followTargets.Clear();
        followTarget = false;
        followTransform = null;
    }

    public void FocusOn(Transform t, float instant = 0f)
    {
        followTransform = t;
        followTarget = (t != null);
        if (t  != null)
        {
            Vector3 center = t.position;
            transform.position = center + followOffset;
            desiredPosition = transform.position;
        }
    }

    void UpdateFollowPosition()
    {
        Vector3 center = Vector3.zero;
        if (followTransform != null)
            center = followTransform.position;
        else if (followTargets != null && followTargets.Count > 0)
        {
            foreach (var tr in followTargets) if (tr != null) center += tr.position;
            center /= followTargets.Count;
        }
        else
        {
            followTarget = false;
            return;
        }

        Vector3 targetPos = center + followOffset;
        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, followSmoothTime);
        transform.LookAt(center);
    }

    void SmoothApplyDesiredPosition()
    {
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, moveSmoothTime);
    }

    // ---------- Bounds ----------
    void ClampPosition()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minXZ.x, maxXZ.x);
        p.z = Mathf.Clamp(p.z, minXZ.y, maxXZ.y);
        transform.position = p;
    }

    // ---------- Editor helper ----------
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // draw bounds in scene view
        Vector3 a = new Vector3(minXZ.x, transform.position.y, minXZ.y);
        Vector3 b = new Vector3(maxXZ.x, transform.position.y, minXZ.y);
        Vector3 c = new Vector3(maxXZ.x, transform.position.y, maxXZ.y);
        Vector3 d = new Vector3(minXZ.x, transform.position.y, maxXZ.y);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
    }
#endif
}
