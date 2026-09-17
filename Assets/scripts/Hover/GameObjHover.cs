using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D))]
public class GameObjHover : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer targetSprite;

    [Header("Events")]
    [SerializeField] private UnityEvent onHoverEnter;
    [SerializeField] private UnityEvent onHoverExit;

    [Header("Color Effect")]
    [SerializeField] private bool changeColor = true;
    [SerializeField] private Color hoverColor = Color.red;

    [Header("Scale Effect")]
    [SerializeField] private bool changeScale = true;
    [SerializeField] private float hoverScale = 1.1f;

    private Color originalColor;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (targetSprite == null)
        {
            targetSprite =
                GetComponentInChildren<SpriteRenderer>(true);
        }

        if (targetSprite != null)
        {
            originalColor = targetSprite.color;
        }
        else
        {
            Debug.LogError(
                "No SpriteRenderer found on " + gameObject.name,
                this
            );
        }
    }

    private void OnMouseEnter()
    {
        Debug.Log("Mouse entered: " + gameObject.name);

        if (changeColor && targetSprite != null)
            targetSprite.color = hoverColor;

        if (changeScale)
            transform.localScale = originalScale * hoverScale;

        onHoverEnter?.Invoke();
    }

    private void OnMouseExit()
    {
        ResetHover();
        onHoverExit?.Invoke();
    }

    private void OnDisable()
    {
        ResetHover();
    }

    private void ResetHover()
    {
        if (changeColor && targetSprite != null)
            targetSprite.color = originalColor;

        if (changeScale)
            transform.localScale = originalScale;
    }
}