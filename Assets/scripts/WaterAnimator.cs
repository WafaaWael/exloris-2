using Cainos.InteractivePixelWater;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class WaterAnimator : MonoBehaviour
{
    [SerializeField] private PixelWater _pixelWater;
    [SerializeField] private float _targetYSize = 6f;
    [SerializeField] private float _animationDuration = 1f;
    [SerializeField] private UnityEvent2 _onAnimationComplete;
    //target y size = number 18
    public void AnimateYSize()
    {
        if (_pixelWater == null)
        {
            Debug.LogError("PixelWater reference is missing!", this);
            return;
        }

        DOTween.To(
              () => _pixelWater.Size.y,
              y => _pixelWater.Size = new Vector2(_pixelWater.Size.x, y),
              _targetYSize,
              _animationDuration
          )
          .SetEase(Ease.InOutSine)
          .OnComplete(() => _onAnimationComplete.Invoke());
    }

    private void Start()
    {
        //if (_pixelWater == null)
        //{
        //    Debug.LogError("PixelWater reference is missing!", this);
        //    return;
        //}

        //_pixelWater.Size = new Vector2(
        //    _pixelWater.Size.x,
        //    _targetYSize
        //);
        AnimateYSize();
    }
}