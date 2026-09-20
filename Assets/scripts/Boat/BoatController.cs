using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class BoatController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Movement")]
    [SerializeField] private float sidewaysSpeed = 6f;
    [SerializeField] private float minimumX = -7f;
    [SerializeField] private float maximumX = 7f;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private float targetX;
    private float fixedY;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        body.gravityScale = 0f;
        body.freezeRotation = true;

        targetX = transform.position.x;
        fixedY = transform.position.y;
    }

    private void OnEnable()
    {
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

        moveInput =
            moveAction.action.ReadValue<Vector2>();

        targetX += moveInput.x *
                   sidewaysSpeed *
                   Time.deltaTime;

        targetX = Mathf.Clamp(
            targetX,
            minimumX,
            maximumX
        );
    }

    private void FixedUpdate()
    {
        body.MovePosition(
            new Vector2(targetX, fixedY)
        );
    }
}