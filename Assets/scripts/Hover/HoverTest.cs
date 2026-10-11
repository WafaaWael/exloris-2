using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider2D))]
public class HoverTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer targetSprite;
    [SerializeField] private Collider2D targetCollider;

    [Header("Events")]
    [SerializeField] private UnityEvent onHoverEnter;
    [SerializeField] private UnityEvent onHoverExit;

    [Header("Color")]
    [SerializeField] private bool changeColor = true;
    [SerializeField] private Color hoverColor = Color.red;

    [Header("Scale")]
    [SerializeField] private bool changeScale = true;
    [SerializeField] private float hoverScale = 1.1f;

    private Color originalColor;
    private Vector3 originalScale;
    private bool isHovered;

    private void Awake()
    {
            Debug.Log("GameObjHover Awake: " + gameObject.name, this);
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetSprite == null)
            targetSprite = GetComponent<SpriteRenderer>();

        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        originalScale = transform.localScale;

        if (targetSprite != null)
            originalColor = targetSprite.color;

        if (targetCamera == null)
            Debug.LogError("No camera assigned or tagged MainCamera.", this);

        if (targetSprite == null)
            Debug.LogError("No SpriteRenderer found.", this);

        if (targetCollider == null)
            Debug.LogError("No Collider2D found.", this);
    }

    private void Update()
    {
        if (targetCamera == null || targetCollider == null)
            return;

        if (!TryGetPointerPosition(out Vector2 screenPosition))
        {
            SetHovered(false);
            return;
        }

        float distanceFromCamera =
            Mathf.Abs(transform.position.z -
                      targetCamera.transform.position.z);

        Vector3 screenPoint = new Vector3(
            screenPosition.x,
            screenPosition.y,
            distanceFromCamera
        );

        Vector3 worldPosition =
            targetCamera.ScreenToWorldPoint(screenPoint);

        bool pointerIsInside =
            targetCollider.OverlapPoint(worldPosition);

        SetHovered(pointerIsInside);
    }

    private bool TryGetPointerPosition(
        out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null)
        {
            screenPosition = Vector2.zero;
            return false;
        }

        screenPosition =
            Mouse.current.position.ReadValue();

        return true;
#else
        screenPosition = Input.mousePosition;
        return true;
#endif
    }

    private void SetHovered(bool hovered)
    {
        if (hovered == isHovered)
            return;

        isHovered = hovered;

        if (isHovered)
        {
            if (changeColor && targetSprite != null)
                targetSprite.color = hoverColor;

            if (changeScale)
                transform.localScale =
                    originalScale * hoverScale;

            Debug.Log("Hover entered: " + gameObject.name);
            onHoverEnter?.Invoke();
        }
        else
        {
            ResetVisuals();

            Debug.Log("Hover exited: " + gameObject.name);
            onHoverExit?.Invoke();
        }
    }

    private void ResetVisuals()
    {
        if (changeColor && targetSprite != null)
            targetSprite.color = originalColor;

        if (changeScale)
            transform.localScale = originalScale;
    }

    private void OnDisable()
    {
        isHovered = false;
        ResetVisuals();
    }
}