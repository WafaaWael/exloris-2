using UnityEngine;
using UnityEngine.UI;

namespace BoatGame.Presentation.BoatLevelUI
{
    /// <summary>Row of star images. Lost stars swap to the empty sprite with a little shake-and-pop.</summary>
    public class StarsDisplay : MonoBehaviour
    {
        private const float PopDuration = 0.45f;

        [SerializeField] private Image[] _stars = new Image[0];
        [SerializeField] private Sprite _fullSprite;
        [SerializeField] private Sprite _emptySprite;
        [SerializeField] private int _maxStars = 3;

        private int _shown = -1;
        private int _poppingIndex = -1;
        private float _popTime;
        private int _currentStars=3;
        public void CountStars(int count, bool isAnimated)
        {
            _currentStars = _currentStars - count;

            if (count < 0 || count > _stars.Length)
            {
                Debug.LogError($"Invalid star count {count} for {name}");
                return;
            }
            if (_currentStars >= 0)
            {
                //_currentStars = _maxStars - count;
                ShowStars(_currentStars, isAnimated);
            }
        }
        public void ShowStars(int count, bool isAnimated)
        {
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].sprite = i < count ? _fullSprite : _emptySprite;
                _stars[i].rectTransform.localScale = Vector3.one;
                _stars[i].rectTransform.localRotation = Quaternion.identity;
            }

            if (isAnimated && count < _shown && count >= 0 && count < _stars.Length)
            {
                _poppingIndex = count;
                _popTime = 0f;
            }

            _shown = count;
        }

        private void Update()
        {
            if (_poppingIndex < 0)
            {
                return;
            }

            _popTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_popTime / PopDuration);
            RectTransform star = _stars[_poppingIndex].rectTransform;
            star.localScale = Vector3.one * ComputePopScale(t);
            star.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 18f * (1f - t));

            if (t >= 1f)
            {
                _poppingIndex = -1;
            }
        }

        private static float ComputePopScale(float t)
        {
            return 1f + Mathf.Sin(t * Mathf.PI) * 0.45f;
        }
    }
}
