using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    public float moveSpeed = 10f;
    public bool useUnscaledTime = false;

    public float dragSpeed = 1f;

    public bool enableZoom = true;
    public float zoomSpeed = 3f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    public Vector2 minBounds = Vector2.zero;
    public Vector2 maxBounds = Vector2.zero;
    public bool useBounds = false;

    private Camera cam;
    private Vector3 dragOrigin;
    private bool isDragging;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        HandleWASD();
        HandleDrag();

        if (enableZoom)
            HandleZoom();

        if (useBounds)
            ClampToBounds();
    }

    void HandleWASD()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(h, v, 0f).normalized;
        transform.position += direction * moveSpeed * dt;
    }

    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
            isDragging = false;

        if (!isDragging) return;

        Vector3 currentPos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 delta = dragOrigin - currentPos;
        transform.position += delta * dragSpeed;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        cam.orthographicSize -= scroll * zoomSpeed;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    void ClampToBounds()
    {
        if (minBounds == Vector2.zero && maxBounds == Vector2.zero) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        transform.position = pos;
    }
}


