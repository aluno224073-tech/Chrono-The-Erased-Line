// using System.Diagnostics;
using System.Security.AccessControl;
using System.Threading.Tasks.Dataflow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCamera;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    [Header("Crouch")]
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private float rotationX;
    private float originalCenterY;
    private float mouseX;
    private bool canMove = true;
    private bool isCrouching;
    private bool jumpRequested;
    private Vector2 movementInput;
    private string past = "Passado";
    private string future = "Futuro";
    private string present = "Presente";
    [SerializeField] private Canvas Canvas;
    Scene cenaAtual;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        rb.useGravity = true;
        rb.freezeRotation = true;

        originalCenterY = capsuleCollider.center.y;
    }

    void Start()
    {
        cenaAtual = SceneManager.GetActiveScene();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        capsuleCollider.height = defaultHeight;

        if(cenaAtual.name != "SampleScene")
        {
            Canvas.enabled = false;
        }
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (Canvas.enabled == true)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Canvas.enabled == false)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (keyboard.f3Key.wasPressedThisFrame && cenaAtual.name == "SampleScene")
        {
            Canvas.enabled = !Canvas.enabled;
        }

        if (keyboard.f11Key.wasPressedThisFrame)
        {
            Screen.fullScreen = !Screen.fullScreen;
        }

        if (keyboard == null || playerCamera == null)
            return;

        movementInput = Vector2.zero;

        if (keyboard.wKey.isPressed)
            movementInput.y += 1f;

        if (keyboard.sKey.isPressed)
            movementInput.y -= 1f;

        if (keyboard.dKey.isPressed)
            movementInput.x += 1f;

        if (keyboard.aKey.isPressed)
            movementInput.x -= 1f;

        movementInput = Vector2.ClampMagnitude(
            movementInput, 1f
        );

        isCrouching = keyboard.leftShiftKey.isPressed;

        if (keyboard.spaceKey.wasPressedThisFrame &&
            canMove && !isCrouching)
        {
            jumpRequested = true;
        }

        if (mouse != null && canMove)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();

            rotationX -= mouseDelta.y * lookSpeed * 0.02f;
            rotationX = Mathf.Clamp(
                rotationX, -lookXLimit, lookXLimit
            );

            playerCamera.transform.localRotation =
                Quaternion.Euler(rotationX, 0f, 0f);

            mouseX = mouseDelta.x * lookSpeed * 0.02f;
        }
        else
        {
            mouseX = 0f;
        }

        HandleCrouch();

        if (keyboard.tKey.isPressed && keyboard.leftArrowKey.isPressed)
        {
            saltoTemporal(past);
        }
        else if(keyboard.tKey.isPressed && keyboard.rightArrowKey.isPressed)
        {
            saltoTemporal(future);
        }
        else if (keyboard.tKey.isPressed && keyboard.upArrowKey.isPressed)
        {
            saltoTemporal(present);
        }
    }

    void FixedUpdate()
    {
        if (canMove)
        {
            rb.MoveRotation(
                rb.rotation *
                Quaternion.Euler(0f, mouseX, 0f)
            );
        }

        if (!canMove)
        {
            Vector3 velocity = rb.linearVelocity;

            rb.linearVelocity = new Vector3(
                0f, velocity.y, 0f
            );

            jumpRequested = false;
            return;
        }

        HandleMovement();
        HandleJump();
    }

    private void HandleMovement()
    {
        Keyboard keyboard = Keyboard.current;

        bool isRunning = keyboard != null &&
            keyboard.leftCtrlKey.isPressed &&
            !isCrouching;

        float currentSpeed = isCrouching
            ? crouchSpeed
            : isRunning ? runSpeed : walkSpeed;

        Vector3 movement =
            transform.forward * movementInput.y +
            transform.right * movementInput.x;

        movement = Vector3.ClampMagnitude(movement, 1f);

        Vector3 velocity = rb.linearVelocity;

        rb.linearVelocity = new Vector3(
            movement.x * currentSpeed,
            velocity.y,
            movement.z * currentSpeed
        );
    }

    private void HandleJump()
    {
        if (!jumpRequested)
            return;

        jumpRequested = false;

        if (IsGrounded() && !isCrouching)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = jumpPower;
            rb.linearVelocity = velocity;
        }
    }

    private bool IsGrounded()
    {
        Bounds bounds = capsuleCollider.bounds;

        float radius = Mathf.Min(
            bounds.extents.x,
            bounds.extents.z
        ) * 0.9f;

        Vector3 origin = new Vector3(
            bounds.center.x,
            bounds.min.y + radius + 0.02f,
            bounds.center.z
        );

        return Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out _,
            0.12f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private void HandleCrouch()
    {
        float targetHeight = isCrouching
            ? crouchHeight
            : defaultHeight;

        capsuleCollider.height = Mathf.Lerp(
            capsuleCollider.height,
            targetHeight,
            Time.deltaTime * 10f
        );

        capsuleCollider.center = new Vector3(
            capsuleCollider.center.x,
            originalCenterY -
                (defaultHeight - capsuleCollider.height) / 2f,
            capsuleCollider.center.z
        );
    }

    private void saltoTemporal(string when)
    {
        if(when == past)
        {
            if(cenaAtual.name != "Past")
            {
                SceneManager.LoadScene("Past");
            }
            else
            {
                Debug.Log("A cena atual já é o passado");
            }
        }
        else if(when == present)
        {
            if(cenaAtual.name != "SampleScene")
            {
                SceneManager.LoadScene("SampleScene");
            }
            else
            {
                Debug.Log("A cena atual já é o presente");
            }
        }
        else if(when == future)
        {
            if(cenaAtual.name != "Future")
            {
                SceneManager.LoadScene("Future");
            }
            else
            {
                Debug.Log("A cena atual já é o futuro");
            }
        }
    }
}