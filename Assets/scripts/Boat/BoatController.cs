using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class BoatController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("UI Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float movementSpeed = 8f;
    [SerializeField] private float minimumX = -7f;
    [SerializeField] private float maximumX = 7f;

    [Header("Stick Sensitivity")]
    [SerializeField, Range(0.1f, 1f)]
    private float pressThreshold = 0.5f;

    [SerializeField, Range(0f, 0.4f)]
    private float releaseThreshold = 0.2f;

    private Rigidbody2D body;

    private float startX;
    private float fixedY;
    private float targetX;

    // -1 = left, 0 = starting position, 1 = right.
    private int currentLane;
    private bool readyForStep = true;

    private Coroutine movementRoutine;

    private readonly WaitForFixedUpdate physicsWait =
        new WaitForFixedUpdate();

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        // Store values, not a reference to the moving Transform.
        startX = body.position.x;
        fixedY = body.position.y;

        targetX = startX;
        currentLane = 0;
    }

    private void OnEnable()
    {
        readyForStep = true;

        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed += HandleMove;
            moveAction.action.canceled += HandleMoveCanceled;
            moveAction.action.Enable();
        }

        if (leftButton != null)
            leftButton.onClick.AddListener(MoveLeft);

        if (rightButton != null)
            rightButton.onClick.AddListener(MoveRight);

        // Resume movement if the component was disabled mid-step.
        if (movementRoutine == null &&
            Mathf.Abs(body.position.x - targetX) > 0.001f)
        {
            movementRoutine = StartCoroutine(Move());
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.performed -= HandleMove;
            moveAction.action.canceled -= HandleMoveCanceled;
            moveAction.action.Disable();
        }

        if (leftButton != null)
            leftButton.onClick.RemoveListener(MoveLeft);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(MoveRight);

        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }

        body.linearVelocity = Vector2.zero;
        readyForStep = true;
    }

    private void HandleMove(InputAction.CallbackContext context)
    {
        OnMove(context.ReadValue<Vector2>());
    }

    private void HandleMoveCanceled(
        InputAction.CallbackContext context)
    {
        readyForStep = true;
    }

    private void OnMove(Vector2 input)
    {
        float horizontal = input.x;

        // Centering the stick allows another step.
        if (Mathf.Abs(horizontal) <= releaseThreshold)
        {
            readyForStep = true;
            return;
        }

        if (!readyForStep ||
            Mathf.Abs(horizontal) < pressThreshold)
        {
            return;
        }

        readyForStep = false;

        RequestStep(horizontal > 0f ? 1 : -1);
    }

    public void MoveLeft()
    {
        RequestStep(-1);
    }

    public void MoveRight()
    {
        RequestStep(1);
    }

    private void RequestStep(int direction)
    {
        if (!isActiveAndEnabled)
            return;

        int nextLane = Mathf.Clamp(
            currentLane + direction,
            -1,
            1
        );

        // Prevent another outward step at either edge.
        if (nextLane == currentLane)
            return;

        currentLane = nextLane;

        switch (currentLane)
        {
            case -1:
                targetX = minimumX;
                break;

            case 0:
                targetX = startX;
                break;

            case 1:
                targetX = maximumX;
                break;
        }

        // An existing coroutine follows the updated destination.
        if (movementRoutine == null)
            movementRoutine = StartCoroutine(Move());
    }

    private IEnumerator Move()
    {
        yield return physicsWait;

        while (Mathf.Abs(body.position.x - targetX) > 0.001f)
        {
            float nextX = Mathf.MoveTowards(
                body.position.x,
                targetX,
                movementSpeed * Time.fixedDeltaTime
            );

            body.MovePosition(new Vector2(nextX, fixedY));

            yield return physicsWait;
        }

        body.linearVelocity = Vector2.zero;
        movementRoutine = null;
    }
}