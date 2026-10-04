using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections.Generic;

public class WaterAnimator : MonoBehaviour
{
    [SerializeField] private GameObject _water2D;
    [SerializeField] private List<float> _targetYSize = new List<float> { 6f };
    [SerializeField] private float _animationDuration = 1f;
    [SerializeField] private List<UnityEvent> _onAnimationComplete;
    //target y size = number 18
    public void AnimateYSize()
    {
        if (_water2D == null)
        {
            Debug.LogError("Water2D reference is missing!", this);
            return;
        }
        SpriteRenderer _water2DRenderer = _water2D.GetComponent<SpriteRenderer>();

        DOTween.To(
              () => _water2D.transform.position.y,
              y => _water2D.transform.position = new Vector2(_water2D.transform.position.x, y),
              _targetYSize[0],
              _animationDuration
          )
          .SetEase(Ease.InOutSine)
          .OnComplete(() => _onAnimationComplete[0].Invoke());
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