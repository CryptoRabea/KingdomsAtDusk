using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class RTSCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float edgeScrollSpeed = 20f;
    public float panBorderThickness = 10f;
    public bool useEdgeScroll = true;
    public Vector2 minPosition;
    public Vector2 maxPosition;

    [Header("Zoom")]
    public float zoomSpeed = 50f;
    public float minZoom = 15f;
    public float maxZoom = 80f;

    private Camera cam;
    private Vector2 moveInput;
    private float zoomInput;
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        inputActions = new InputSystem_Actions();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Zoom.performed += ctx => zoomInput = ctx.ReadValue<float>();
        inputActions.Player.Zoom.canceled += ctx => zoomInput = 0f;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleTouch();
    }

    private void HandleMovement()
    {
        Vector3 dir = new Vector3(moveInput.x, 0, moveInput.y);

        // Edge scrolling
        if (useEdgeScroll)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (mousePos.y >= Screen.height - panBorderThickness) dir.z += 1;
            if (mousePos.y <= panBorderThickness) dir.z -= 1;
            if (mousePos.x >= Screen.width - panBorderThickness) dir.x += 1;
            if (mousePos.x <= panBorderThickness) dir.x -= 1;
        }

        Vector3 movement = dir.normalized * moveSpeed * Time.deltaTime;
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
        float newZoom = cam.orthographic ? cam.orthographicSize - zoomInput * zoomSpeed * Time.deltaTime
                                         : cam.fieldOfView - zoomInput * zoomSpeed * Time.deltaTime;

        if (cam.orthographic)
            cam.orthographicSize = Mathf.Clamp(newZoom, minZoom, maxZoom);
        else
            cam.fieldOfView = Mathf.Clamp(newZoom, minZoom, maxZoom);
    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null || Touchscreen.current.touches.Count == 0) return;

        if (Touchscreen.current.touches.Count == 1)
        {
            // Drag camera
            Vector2 delta = Touchscreen.current.touches[0].delta.ReadValue();
            transform.position += new Vector3(-delta.x, 0, -delta.y) * Time.deltaTime * moveSpeed * 0.5f;
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
