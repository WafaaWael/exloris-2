using UnityEngine;
using DG.Tweening;

public class BoatBackground : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.5f;
    [SerializeField] private float moveDistance = 1.5f;
    [SerializeField] private float moveDuration = 0.5f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
        MoveBackground();
    }

    public void MoveBackground(float distance)
    {
        Vector3 targetPosition = startPosition + new Vector3(0, distance, 0f);

        transform.DOMove(targetPosition, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                transform.DOMove(startPosition, moveDuration)
                    .SetEase(Ease.Linear);
            });
    }

    public void MoveBackground()
    {
        MoveBackground(moveDistance);
    }
}
