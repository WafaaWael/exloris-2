using UnityEngine;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>Optional gold coin. Spins in place; pops and fades out when collected.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Coin : MonoBehaviour
    {
        private const float SpinSpeed = 3f;
        private const float CollectDuration = 0.3f;
        private const float CollectRise = 1.2f;
        private const float TwinklePeriod = 1.7f;
        private const float TwinkleLength = 0.35f;

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteRenderer _halo;
        [SerializeField] private SpriteRenderer _twinkle;

        private Transform _visual;
        private Collider2D _collider;
        private Vector3 _baseScale;
        private float _phase;
        private float _collectTime = -1f;

        public bool IsCollected => _collectTime >= 0f;

        public void Collect()
        {
            if (IsCollected)
            {
                return;
            }

            _collectTime = 0f;
            _collider.enabled = false;
        }

        private void Awake()
        {
            _visual = _renderer.transform;
            _collider = GetComponent<Collider2D>();
            _baseScale = _visual.localScale;
            _phase = transform.position.y * 0.37f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (IsCollected)
            {
                UpdateCollect(dt);
                return;
            }

            _phase += dt * SpinSpeed;
            Vector3 scale = _baseScale;
            scale.x *= ComputeSpinWidth(_phase);
            _visual.localScale = scale;
            UpdateGlow(_phase);
        }

        private void UpdateGlow(float phase)
        {
            if (_halo != null)
            {
                Color halo = _halo.color;
                halo.a = 0.35f + 0.2f * Mathf.Sin(phase * 0.8f);
                _halo.color = halo;
            }

            if (_twinkle != null)
            {
                // a quick star flash once per period, offset per coin so they don't blink together
                float t = Mathf.Repeat(phase / SpinSpeed, TwinklePeriod);
                float flash = t < TwinkleLength ? Mathf.Sin(t / TwinkleLength * Mathf.PI) : 0f;
                _twinkle.transform.localScale = Vector3.one * (0.2f + 0.8f * flash);
                Color twinkle = _twinkle.color;
                twinkle.a = flash;
                _twinkle.color = twinkle;
            }
        }

        private void UpdateCollect(float dt)
        {
            _collectTime += dt;
            float t = Mathf.Clamp01(_collectTime / CollectDuration);

            _visual.localScale = _baseScale * (1f + t * 0.6f);
            _visual.localPosition = new Vector3(0f, t * CollectRise, 0f);
            Color color = _renderer.color;
            color.a = 1f - t;
            _renderer.color = color;
            if (_halo != null)
            {
                _halo.enabled = false;
            }

            if (_twinkle != null)
            {
                _twinkle.enabled = false;
            }

            if (t >= 1f)
            {
                gameObject.SetActive(false);
            }
        }

        private static float ComputeSpinWidth(float phase)
        {
            // never fully flat so the coin stays readable for kids
            return 0.35f + 0.65f * Mathf.Abs(Mathf.Cos(phase));
        }
    }
}
