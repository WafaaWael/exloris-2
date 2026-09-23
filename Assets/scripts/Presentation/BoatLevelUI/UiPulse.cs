using UnityEngine;

namespace BoatGame.Presentation.BoatLevelUI
{
    /// <summary>Gentle looping scale pulse (and optional alpha pulse) on unscaled time, for hints and buttons.</summary>
    public class UiPulse : MonoBehaviour
    {
        [SerializeField] private float _scaleAmount = 0.08f;
        [SerializeField] private float _speed = 3f;
        [SerializeField] private CanvasGroup _fadeGroup;
        [SerializeField] private float _minAlpha = 1f;

        private Transform _transform;
        private Vector3 _baseScale;
        private float _phase;

        private void Awake()
        {
            _transform = transform;
            _baseScale = _transform.localScale;
        }

        private void OnEnable()
        {
            _phase = 0f;
        }

        private void OnDisable()
        {
            if (_transform != null)
            {
                _transform.localScale = _baseScale;
            }
        }

        private void Update()
        {
            _phase += Time.unscaledDeltaTime * _speed;
            float wave = 0.5f + 0.5f * Mathf.Sin(_phase);
            _transform.localScale = _baseScale * (1f + wave * _scaleAmount);

            if (_fadeGroup != null)
            {
                _fadeGroup.alpha = Mathf.Lerp(_minAlpha, 1f, wave);
            }
        }
    }
}
