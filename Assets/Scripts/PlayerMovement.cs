// using System.Diagnostics;
// using System.Security.AccessControl;
// using System.Threading.Tasks.Dataflow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(coordinates))]
public class PlayerMovement : MonoBehaviour
{
    private class ObjectTransformSnapshot
    {
        public Vector3[] positions;
        public Quaternion[] rotations;
    }

    private static readonly Dictionary<string, ObjectTransformSnapshot> savedObjectTransformsByScene =
        new Dictionary<string, ObjectTransformSnapshot>();
    private static Vector3 pendingPosition;
    private static Vector3[] pendingObjectPositions;
    private static Quaternion[] pendingObjectRotations;
    private static bool hasPendingObjectTransforms;
    private static Quaternion pendingCameraRotation;
    private static Quaternion pendingRotation;
    private static bool hasPendingPosition;
    private static bool hasPendingRotation;
    private static bool hasPendingCameraRotation;
    public Camera playerCamera;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
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
    Scene cenaPassada;
    public GameObject[] gameObjects;
    public string[] tagsDeExcecao;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        foreach (Collider childCollider in GetComponentsInChildren<Collider>())
        {
            if (childCollider != capsuleCollider)
                childCollider.enabled = false;
        }

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        rb.useGravity = true;
        rb.freezeRotation = true;

        originalCenterY = capsuleCollider.center.y;
    }

    void Start()
    {
        cenaAtual = SceneManager.GetActiveScene();

        if (hasPendingPosition)
        {
            GetComponent<coordinates>().updatePosition(pendingPosition.x, pendingPosition.y, pendingPosition.z);
            hasPendingPosition = false;
            Canvas.enabled = false;
        }

        if (hasPendingRotation)
        {
            GetComponent<coordinates>().updateRotation(pendingRotation);
            hasPendingRotation = false;
        }

        if (hasPendingCameraRotation)
        {
            playerCamera.transform.rotation = pendingCameraRotation;
            rotationX = Mathf.DeltaAngle(0f, playerCamera.transform.localEulerAngles.x);

            hasPendingCameraRotation = false;
        }

        RestoreObjectsInCurrentScene();

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
            cenaPassada = cenaAtual;
        }
        else if(keyboard.tKey.isPressed && keyboard.rightArrowKey.isPressed)
        {
            saltoTemporal(future);
            cenaPassada = cenaAtual;
        }
        else if (keyboard.tKey.isPressed && keyboard.enterKey.isPressed)
        {
            saltoTemporal(present);
            cenaPassada = cenaAtual;
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
            LoadSceneAtPosition("Past");
        }
        else if(when == present)
        {
            LoadSceneAtPosition("SampleScene");
        }
        else if(when == future)
        {
            LoadSceneAtPosition("Future");
        }
    }

    private void LoadSceneAtPosition(string sceneName)
    {
        if (cenaAtual.name == sceneName)
        {
            Debug.Log($"A cena atual já é {sceneName}");
            return;
        }

        bool deveTransferirObjetos = (cenaAtual.name == "Past" && (sceneName == "SampleScene" || sceneName == "Future")) || (cenaAtual.name == "SampleScene" && sceneName == "Future");

        SaveObjectsInCurrentScene(deveTransferirObjetos);

        pendingCameraRotation = playerCamera.transform.rotation;
        hasPendingCameraRotation = true;
        pendingPosition = transform.position;
        pendingRotation = transform.rotation;
        hasPendingPosition = true;
        hasPendingRotation = true;
        SceneManager.LoadScene(sceneName);
    }

    private void SaveObjectsInCurrentScene(bool shouldTransferObjects)
    {
        if (gameObjects == null)
        {
            hasPendingObjectTransforms = false;
            return;
        }

        ObjectTransformSnapshot snapshot = new ObjectTransformSnapshot
        {
            positions = new Vector3[gameObjects.Length],
            rotations = new Quaternion[gameObjects.Length]
        };

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] == null)
                continue;

            snapshot.positions[i] = gameObjects[i].transform.position;
            snapshot.rotations[i] = gameObjects[i].transform.rotation;
        }

        savedObjectTransformsByScene[cenaAtual.name] = snapshot;

        hasPendingObjectTransforms = shouldTransferObjects;
        if (shouldTransferObjects)
        {
            pendingObjectPositions = snapshot.positions;
            pendingObjectRotations = snapshot.rotations;
        }
    }

    private void RestoreObjectsInCurrentScene()
    {
        if (gameObjects == null)
            return;

        if (savedObjectTransformsByScene.TryGetValue(cenaAtual.name, out ObjectTransformSnapshot savedSnapshot))
        {
            ApplyObjectTransformSnapshot(savedSnapshot.positions, savedSnapshot.rotations, false);
        }

        if (hasPendingObjectTransforms && pendingObjectPositions != null && pendingObjectRotations != null)
        {
            ApplyObjectTransformSnapshot(pendingObjectPositions, pendingObjectRotations, true);
        }

        hasPendingObjectTransforms = false;

        ObjectTransformSnapshot currentSnapshot = new ObjectTransformSnapshot
        {
            positions = new Vector3[gameObjects.Length],
            rotations = new Quaternion[gameObjects.Length]
        };

        for (int i = 0; i < gameObjects.Length; i++)
        {
            if (gameObjects[i] == null)
                continue;

            currentSnapshot.positions[i] = gameObjects[i].transform.position;
            currentSnapshot.rotations[i] = gameObjects[i].transform.rotation;
        }

        savedObjectTransformsByScene[cenaAtual.name] = currentSnapshot;
    }

    private void ApplyObjectTransformSnapshot(Vector3[] positions, Quaternion[] rotations, bool respectExceptionTags)
    {
        if (positions == null || rotations == null)
            return;

        int count = Mathf.Min(gameObjects.Length, positions.Length, rotations.Length);

        for (int i = 0; i < count; i++)
        {
            if (gameObjects[i] == null)
                continue;

            bool isException = respectExceptionTags && tagsDeExcecao != null &&
                System.Array.IndexOf(tagsDeExcecao, gameObjects[i].tag) >= 0;

            if (isException) continue;

            gameObjects[i].transform.position = positions[i];
            gameObjects[i].transform.rotation = rotations[i];
        }
    }
}
