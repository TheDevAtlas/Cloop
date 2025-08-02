using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float moveSmoothing = 10f;
    
    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float zoomSmoothing = 8f;
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;
    public float rotationSmoothing = 12f;
    
    private Camera cam;
    private Vector3 targetPosition;
    private float targetZoom = -10f;
    private float targetRotationY = 0f;
    private bool isRotating = false;
    
    void Start()
    {
        cam = GetComponentInChildren<Camera>();
        targetPosition = transform.position;
        
        if (cam != null)
        {
            cam.transform.localPosition = new Vector3(0, 0, targetZoom);
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
        HandleReset();
        
        ApplyMovement();
        ApplyZoom();
        ApplyRotation();
    }
    
    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        Vector3 moveDirection = (right * horizontal + forward * vertical) * moveSpeed * Time.deltaTime;
        targetPosition += moveDirection;
        
        targetPosition.x = Mathf.Clamp(targetPosition.x, -20f, 20f);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -20f, 20f);
    }
    
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        targetZoom += scroll * zoomSpeed;
        targetZoom = Mathf.Clamp(targetZoom, -20f, -5f);
    }
    
    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(2))
        {
            isRotating = true;
        }
        
        if (Input.GetMouseButtonUp(2))
        {
            isRotating = false;
            SnapToNearestAngle();
        }
        
        if (isRotating)
        {
            float mouseX = Input.GetAxis("Mouse X");
            targetRotationY += mouseX * rotationSpeed * Time.deltaTime;
        }
    }
    
    void SnapToNearestAngle()
    {
        float snapAngle = Mathf.Round(targetRotationY / 30f) * 30f;
        targetRotationY = snapAngle;
    }
    
    void HandleReset()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            targetPosition = Vector3.zero;
            targetZoom = -10f;
            targetRotationY = 0f;
        }
    }
    
    void ApplyMovement()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, moveSmoothing * Time.deltaTime);
    }
    
    void ApplyZoom()
    {
        if (cam != null)
        {
            Vector3 currentLocalPos = cam.transform.localPosition;
            currentLocalPos.z = Mathf.Lerp(currentLocalPos.z, targetZoom, zoomSmoothing * Time.deltaTime);
            cam.transform.localPosition = currentLocalPos;
        }
    }
    
    void ApplyRotation()
    {
        Vector3 currentRotation = transform.eulerAngles;
        currentRotation.y = Mathf.LerpAngle(currentRotation.y, targetRotationY, rotationSmoothing * Time.deltaTime);
        transform.rotation = Quaternion.Euler(currentRotation);
    }
}