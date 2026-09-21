using UnityEngine;
using DG.Tweening;

public class BoatBackground : MonoBehaviour
{
    [SerializeField] private Transform _targetTransform;
    [SerializeField] private Transform _endTransform;
    [SerializeField] private float scrollSpeed = 0.5f;
    [SerializeField] private float moveDistance = 1.5f;
    [SerializeField] private float moveDuration = 0.5f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
      
        CalculateMoveDuration(startPosition);
          MoveBackground();
        
    }
    private void CalculateMoveDuration(Vector3 target)
    {
        float distance = Vector3.Distance(target, _targetTransform.position);
        moveDuration = distance / scrollSpeed;  
    
    }
    public void MoveBackground(float distance)
    {
        //Vector3 targetPosition = startPosition + new Vector3(0, distance, 0f);

        transform.DOMove(_targetTransform.position, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                CalculateMoveDuration(_endTransform.position);
                transform.position = _endTransform.position;
                MoveBackground();
            }
            );
    }

        public void MoveBackground()
    {
        MoveBackground(moveDistance);
    }
}
