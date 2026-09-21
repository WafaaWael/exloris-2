using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class BoatController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Step Movement")]
    [SerializeField, Min(0.01f)] private float stepDistance = 2f;
    [SerializeField, Min(0.01f)] private float movementSpeed = 8f;
    [SerializeField] private float minimumX = -7f;
    [SerializeField] private float maximumX = 7f;

    [Header("Stick Sensitivity")]
    [SerializeField, Range(0.1f, 1f)]
    private float pressThreshold = 0.5f;

    [SerializeField, Range(0f, 0.4f)]
    private float releaseThreshold = 0.2f;

    private Rigidbody2D body;
    private float targetX;
    private float fixedY;
    private bool readyForStep = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    private void OnEnable()
    {
        targetX = body.position.x;
        fixedY = body.position.y;
        readyForStep = true;

        if (moveAction != null)
            moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();
    }

    private void Update()
    {
        if (moveAction == null)
            return;

        float horizontal =
            moveAction.action.ReadValue<Vector2>().x;

        // Releasing the key or centering the stick allows another step.
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

        float direction = horizontal > 0f ? 1f : -1f;

        targetX = Mathf.Clamp(
            targetX + direction * stepDistance,
            minimumX,
            maximumX
        );
    }

    private void FixedUpdate()
    {
        float nextX = Mathf.MoveTowards(
            body.position.x,
            targetX,
            movementSpeed * Time.fixedDeltaTime
        );

        body.MovePosition(new Vector2(nextX, fixedY));
    }
}