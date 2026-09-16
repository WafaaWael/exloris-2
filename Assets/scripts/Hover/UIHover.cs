using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Hover Events")]
    [SerializeField] private UnityEvent onHoverEnter;
    [SerializeField] private UnityEvent onHoverExit;

    [Header("Color Effect")]
    [SerializeField] private bool changeColor = true;
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Color hoverColor = Color.red;

    [Header("Scale Effect")]
    [SerializeField] private bool changeScale = true;
    [SerializeField] private float hoverScale = 1.1f;

    private Color originalColor;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetGraphic != null)
            originalColor = targetGraphic.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (changeColor && targetGraphic != null)
            targetGraphic.color = hoverColor;

        if (changeScale)
            transform.localScale =
                originalScale * hoverScale;

        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreAppearance();
        onHoverExit?.Invoke();
    }

    private void OnDisable()
    {
        RestoreAppearance();
    }

    public void RestoreAppearance()
    {
        if (targetGraphic != null)
            targetGraphic.color = originalColor;

        transform.localScale = originalScale;
    }
}