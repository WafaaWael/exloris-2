using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using UnityEngine;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>
    /// One-shot world effects driven by level events: coin sparkle bursts, bump splashes and the arrival celebration.
    /// Each effect is a single shared ParticleSystem that is emitted into at the event position (no instantiation, no GC).
    /// </summary>
    public class BoatLevelVfx : MonoBehaviour
    {
        [Header("Coin collect")]
        [SerializeField] private ParticleSystem _coinSparkles;
        [SerializeField] private ParticleSystem _coinRing;
        [SerializeField] private int _coinSparkleCount = 18;

        [Header("Bump")]
        [SerializeField] private ParticleSystem _splashDrops;
        [SerializeField] private ParticleSystem _splashRing;
        [SerializeField] private int _splashDropCount = 18;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private float _shakeAmplitude = 0.12f;
        [SerializeField] private float _shakeDuration = 0.25f;

        [Header("Arrival")]
        [SerializeField] private ParticleSystem _celebration;
        [SerializeField] private Transform _celebrationPoint;
        [SerializeField] private float _celebrationBurstInterval = 0.45f;
        [SerializeField] private int _celebrationBursts = 6;
        [SerializeField] private int _celebrationBurstSize = 55;

        [Header("Ambient")]
        [SerializeField] private GodRaysPulse _godRays;

        private float _celebrationTimer;
        private int _celebrationBurstsLeft;

        private void OnEnable()
        {
            EventBus.Subscribe<CoinCollectedEvent>(CoinCollectedCallback);
            EventBus.Subscribe<BoatHitObstacleEvent>(BoatHitObstacleCallback);
            EventBus.Subscribe<LevelStateChangedEvent>(LevelStateChangedCallback);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CoinCollectedEvent>(CoinCollectedCallback);
            EventBus.Unsubscribe<BoatHitObstacleEvent>(BoatHitObstacleCallback);
            EventBus.Unsubscribe<LevelStateChangedEvent>(LevelStateChangedCallback);
        }

        private void Update()
        {
            if (_celebrationBurstsLeft <= 0)
            {
                return;
            }

            _celebrationTimer -= Time.deltaTime;
            if (_celebrationTimer > 0f)
            {
                return;
            }

            _celebrationTimer = _celebrationBurstInterval;
            _celebrationBurstsLeft--;
            // bursts pop around the boat, alternating sides
            float side = (_celebrationBurstsLeft % 2 == 0) ? -1f : 1f;
            Vector3 offset = new Vector3(side * Random.Range(1.2f, 3.5f), Random.Range(-1f, 2.5f), 0f);
            Emit(_celebration, _celebrationPoint.position + offset, _celebrationBurstSize);
        }

        private static void Emit(ParticleSystem system, Vector3 position, int count)
        {
            if (system == null)
            {
                return;
            }

            var emitParams = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
            };
            system.Emit(emitParams, count);
        }

        private void CoinCollectedCallback(CoinCollectedEvent evt)
        {
            Emit(_coinSparkles, evt.Position, _coinSparkleCount);
            Emit(_coinRing, evt.Position, 1);
        }

        private void BoatHitObstacleCallback(BoatHitObstacleEvent evt)
        {
            Emit(_splashDrops, evt.ContactPoint, _splashDropCount);
            Emit(_splashRing, evt.ContactPoint, 1);
            if (_cameraFollow != null)
            {
                _cameraFollow.Shake(_shakeAmplitude, _shakeDuration);
            }
        }

        private void LevelStateChangedCallback(LevelStateChangedEvent evt)
        {
            if (evt.Current != LevelState.Arriving)
            {
                return;
            }

            _celebrationBurstsLeft = _celebrationBursts;
            _celebrationTimer = 0.6f;
            if (_godRays != null)
            {
                _godRays.Brighten();
            }
        }
    }
}
