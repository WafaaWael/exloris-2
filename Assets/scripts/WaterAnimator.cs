using System;
using DG.Tweening;
using UnityEngine;

/// <summary>Animates the water only. Level progression and UI feedback belong to WaterOperator and NilometerUI.</summary>
public sealed class WaterAnimator : MonoBehaviour
{
    [SerializeField] private GameObject _water2D;
    [SerializeField] private float _animationDuration = 1f;
    [SerializeField, Min(0.01f), Tooltip("World Y units the water travels per second.")]
    private float _yUnitsPerSecond = 1f;

    private Tween _waterTween;


    public void AnimateYSize(float targetY, Action onComplete = null, float duration = -1f)
    {
        if (_water2D == null)
        {
            Debug.LogError("Water2D reference is missing!", this);
            return;
        }

        _waterTween?.Kill();
        Transform waterTransform = _water2D.transform;
        _animationDuration = CalculateDuration(waterTransform.position.y, targetY, duration);
        _waterTween = DOTween.To(
                () => waterTransform.position.y,
                y => waterTransform.position = new Vector3(waterTransform.position.x, y, waterTransform.position.z),
                targetY,
                _animationDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// Moves the water to <paramref name="targetY"/>, then returns it to the Y position it had when this method was called.
    /// </summary>
    public void AnimateYSizeAndReturn(float targetY)
    {
        AnimateYSizeAndReturn(targetY, -1f, null);
    }

    /// <summary>
    /// Moves the water to <paramref name="targetY"/>, then returns it to the Y position it had when this method was called.
    /// </summary>
    public void AnimateYSizeAndReturn(float targetY, float duration = -1f, Action onComplete = null)
    {
        if (_water2D == null)
        {
            Debug.LogError("Water2D reference is missing!", this);
            return;
        }

        _waterTween?.Kill();

        Transform waterTransform = _water2D.transform;
        float returnY = waterTransform.position.y;
        float forwardDuration = CalculateDuration(returnY, targetY, duration);
        float returnDuration = CalculateDuration(targetY, returnY, duration);
        _animationDuration = forwardDuration;

        _waterTween = DOTween.Sequence()
            .Append(DOTween.To(
                    () => waterTransform.position.y,
                    y => waterTransform.position = new Vector3(waterTransform.position.x, y, waterTransform.position.z),
                    targetY,
                    forwardDuration)
                .SetEase(Ease.InOutSine))
            .Append(DOTween.To(
                    () => waterTransform.position.y,
                    y => waterTransform.position = new Vector3(waterTransform.position.x, y, waterTransform.position.z),
                    returnY,
                    returnDuration)
                .SetEase(Ease.InOutSine))
            .OnComplete(() => onComplete?.Invoke());
    }

    private float CalculateDuration(float fromY, float toY, float overrideDuration)
    {
        if (overrideDuration > 0f)
        {
            return overrideDuration;
        }

        return Mathf.Max(0.01f, Mathf.Abs(toY - fromY) / _yUnitsPerSecond);
    }
    
    private void OnDestroy()
    {
        _waterTween?.Kill();
    }
}
