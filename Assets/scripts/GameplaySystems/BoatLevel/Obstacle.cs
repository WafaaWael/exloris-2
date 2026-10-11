using UnityEngine;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>A rock or small island. The boat cannot pass through it. Wobbles when bumped.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Obstacle : MonoBehaviour
    {
        private const float WobbleDuration = 0.35f;
        private const float WobbleAngle = 6f;

        [SerializeField] private Transform _visual;

        private float _wobbleTime = -1f;

        public void PlayBump()
        {
            _wobbleTime = 0f;
            enabled = true;
        }

        private void Awake()
        {
            enabled = false;
        }

        private void Update()
        {
            if (_visual == null || _wobbleTime < 0f)
            {
                enabled = false;
                return;
            }

            _wobbleTime += Time.deltaTime;
            float t = _wobbleTime / WobbleDuration;
            float angle = t >= 1f ? 0f : ComputeWobble(t);
            _visual.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (t >= 1f)
            {
                _wobbleTime = -1f;
                enabled = false;
            }
        }

        private static float ComputeWobble(float t)
        {
            return Mathf.Sin(t * Mathf.PI * 4f) * WobbleAngle * (1f - t);
        }
    }
}
