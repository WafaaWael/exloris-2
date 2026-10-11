using UnityEngine;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>Keeps the boat in the lower part of the screen, following it up the river.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _leadDistance = 3.5f;
        [SerializeField] private float _smoothTime = 0.15f;
        [SerializeField] private float _maxY = float.MaxValue;

        private Transform _transform;
        private float _velocity;
        private float _shakeAmplitude;
        private float _shakeLeft;
        private float _shakeDuration;

        public float MaxY
        {
            get => _maxY;
            set => _maxY = value;
        }

        /// <summary>Short, decaying shake (e.g. a bump). Kept small: this is a game for young children.</summary>
        public void Shake(float amplitude, float duration)
        {
            _shakeAmplitude = amplitude;
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeLeft = _shakeDuration;
        }

        public void SnapToTarget()
        {
            Vector3 position = _transform.position;
            position.y = ComputeTargetY(_target.position.y, _leadDistance, _maxY);
            _transform.position = position;
            _velocity = 0f;
        }

        private void Awake()
        {
            _transform = transform;
        }

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            Vector3 position = _transform.position;
            float targetY = ComputeTargetY(_target.position.y, _leadDistance, _maxY);
            position.y = Mathf.SmoothDamp(position.y, targetY, ref _velocity, _smoothTime);
            position.x = 0f;

            if (_shakeLeft > 0f)
            {
                _shakeLeft -= Time.deltaTime;
                position += ComputeShakeOffset(_shakeAmplitude * (_shakeLeft / _shakeDuration), Time.time);
            }

            _transform.position = position;
        }

        private static Vector3 ComputeShakeOffset(float amplitude, float time)
        {
            return new Vector3(Mathf.Sin(time * 61f) * amplitude, Mathf.Sin(time * 47f + 1.3f) * amplitude * 0.6f, 0f);
        }

        private static float ComputeTargetY(float targetY, float lead, float maxY)
        {
            return Mathf.Min(targetY + lead, maxY);
        }
    }
}
