namespace URPStylizedNatureEssentials.Rendering
{
    using UnityEngine;

public class SmoothCameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float acceleration = 10f;

    [Header("Rotation Settings")]
    public float mouseSensitivity = 2f;
    public float rotationSmoothTime = 0.1f;

    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private Vector2 currentRotation;
    private Vector2 targetRotation;
    private Vector2 rotationVelocity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    void HandleMovement()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

        // Vertical input (E/Q)
        if (Input.GetKey(KeyCode.E)) input.y += 1f;
        if (Input.GetKey(KeyCode.Q)) input.y -= 1f;

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        targetVelocity = transform.TransformDirection(input) * speed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

        transform.position += currentVelocity * Time.deltaTime;
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        targetRotation.x += mouseX;
        targetRotation.y -= mouseY;
        targetRotation.y = Mathf.Clamp(targetRotation.y, -89f, 89f);

        currentRotation = Vector2.SmoothDamp(currentRotation, targetRotation, ref rotationVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0f);
    }
}
}
