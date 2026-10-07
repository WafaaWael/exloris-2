using System;
using DG.Tweening;
using UnityEngine;

/// <summary>Animates the water only. Level progression and UI feedback belong to WaterOperator and NilometerUI.</summary>
public sealed class WaterAnimator : MonoBehaviour
{
    [SerializeField] private GameObject _water2D;
    [SerializeField] private float _animationDuration = 1f;

    private Tween _waterTween;


    public void AnimateYSize(float targetY, Action onComplete = null, float duration = 1f)
    {
        if (_water2D == null)
        {
            Debug.LogError("Water2D reference is missing!", this);
            return;
        }

        _waterTween?.Kill();
        _animationDuration = duration;
        Transform waterTransform = _water2D.transform;
        _waterTween = DOTween.To(
                () => waterTransform.position.y,
                y => waterTransform.position = new Vector3(waterTransform.position.x, y, waterTransform.position.z),
                targetY,
                _animationDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => onComplete?.Invoke());
    }
    
    private void OnDestroy()
    {
        _waterTween?.Kill();
    }
}
