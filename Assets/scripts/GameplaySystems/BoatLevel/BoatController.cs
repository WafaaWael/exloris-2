using System;
using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using UnityEngine;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>
    /// Auto-forward boat on 3 lanes. Hitting an obstacle ahead pushes the boat back and holds it
    /// until the player changes lane; hitting one sideways bounces it back to its previous lane.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BoatController : MonoBehaviour
    {
        public event Action<Obstacle, bool> OnObstacleHit;   // obstacle, isFirstHitOnIt
        public event Action<Coin> OnCoinTouched;

        private const int LaneCount = 3;
        private const float LaneSnapEpsilon = 0.05f;
        private const float OarInterval = 1.4f;
        private const float AutoPilotMinSpeed = 0.8f;
        private const float AutoPilotArriveEpsilon = 0.02f;

        [Header("Lanes")]
        [SerializeField] private float _laneSpacing = 3f;
        [SerializeField] private float _laneChangeTime = 0.18f;
        [SerializeField] private float _tiltAngle = 12f;

        [Header("Forward speed")]
        [SerializeField] private float _startSpeed = 3.5f;
        [SerializeField] private float _endSpeed = 5.5f;
        [SerializeField] private float _acceleration = 3f;

        [Header("Bump")]
        [SerializeField] private float _pushBackDistance = 1.2f;
        [SerializeField] private float _pushBackTime = 0.25f;
        [SerializeField] private float _flickerTime = 1.1f;
        [SerializeField] private float _flickerRate = 14f;

        [Header("Visual")]
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _bobAngle = 2.5f;
        [SerializeField] private float _bobSpeed = 2.2f;

        private Rigidbody2D _body;
        private int _lane = 1;
        private int _previousLane = 1;
        private float _laneVelocity;
        private float _speed;
        private float _speedProgress01;
        private float _pushBackLeft;
        private float _flickerLeft;
        private float _oarTimer;
        private float _bobPhase;
        private int _blockedLane = -1;
        private Obstacle _lastHitObstacle;

        // arrival autopilot
        private bool _isAutoPilot;
        private float _autoPilotStopY;

        public bool IsMoving { get; set; }
        public bool IsBlocked => _blockedLane >= 0;
        public int Lane => _lane;

        public void StepLane(int direction)
        {
            int target = Mathf.Clamp(_lane + direction, 0, LaneCount - 1);
            if (target == _lane || _isAutoPilot)
            {
                return;
            }

            _previousLane = _lane;
            _lane = target;
            EventBus.Publish(new PlaySfxEvent(SfxId.LaneMove));

            if (IsBlocked && _lane != _blockedLane)
            {
                SetBlocked(-1);
            }
        }

        /// <summary>0 at the start of the river, 1 at the island. Drives the forward speed ramp.</summary>
        public void SetProgress(float progress01)
        {
            _speedProgress01 = Mathf.Clamp01(progress01);
        }

        public void StartAutoPilot(float stopY)
        {
            _isAutoPilot = true;
            _autoPilotStopY = stopY;
            _previousLane = _lane;
            _lane = 1;
            SetBlocked(-1);
        }

        public bool HasReachedAutoPilotStop()
        {
            return _isAutoPilot && _body.position.y >= _autoPilotStopY - AutoPilotArriveEpsilon;
        }

        public float LaneToX(int lane)
        {
            return (lane - 1) * _laneSpacing;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            // dynamic (no gravity) so triggers fire against static obstacle colliders; all motion goes through MovePosition
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = 0f;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            UpdateVisual(dt);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 position = _body.position;

            float targetSpeed = ComputeTargetSpeed(position.y);
            _speed = Mathf.MoveTowards(_speed, targetSpeed, _acceleration * dt);

            float forward = _speed * dt;
            if (_isAutoPilot)
            {
                forward = Mathf.Min(forward, Mathf.Max(0f, _autoPilotStopY - position.y));
                if (forward <= 0f)
                {
                    _speed = 0f;
                }
            }

            if (_pushBackLeft > 0f)
            {
                float step = Mathf.Min(_pushBackLeft, _pushBackDistance / _pushBackTime * dt);
                _pushBackLeft -= step;
                forward = -step;
            }

            float targetX = LaneToX(_lane);
            float x = Mathf.SmoothDamp(position.x, targetX, ref _laneVelocity, _laneChangeTime, Mathf.Infinity, dt);
            if (Mathf.Abs(x - targetX) < LaneSnapEpsilon)
            {
                x = targetX;
            }

            _body.MovePosition(new Vector2(x, position.y + forward));
            UpdateOarSound(dt);
        }

        private float ComputeTargetSpeed(float y)
        {
            if (IsMoving == false)
            {
                return 0f;
            }

            if (_isAutoPilot)
            {
                // ease into the dock: speed proportional to remaining distance
                float remaining = _autoPilotStopY - y;
                return remaining <= AutoPilotArriveEpsilon ? 0f : Mathf.Clamp(remaining * 1.2f, AutoPilotMinSpeed, _startSpeed);
            }

            if (IsBlocked)
            {
                return 0f;
            }

            return Mathf.Lerp(_startSpeed, _endSpeed, _speedProgress01);
        }

        private void UpdateOarSound(float dt)
        {
            if (_speed < 0.5f)
            {
                return;
            }

            _oarTimer += dt;
            if (_oarTimer >= OarInterval)
            {
                _oarTimer = 0f;
                EventBus.Publish(new PlaySfxEvent(SfxId.Oar));
            }
        }

        private void UpdateVisual(float dt)
        {
            _bobPhase += dt * _bobSpeed;

            float lateral = _laneVelocity / (_laneSpacing / _laneChangeTime);
            float tilt = -Mathf.Clamp(lateral, -1f, 1f) * _tiltAngle;
            float bob = Mathf.Sin(_bobPhase) * _bobAngle;
            _visual.localRotation = Quaternion.Euler(0f, 0f, tilt + bob);
            float breathe = 1f + Mathf.Sin(_bobPhase * 2f) * 0.015f;
            _visual.localScale = new Vector3(breathe, breathe, 1f);

            if (_flickerLeft > 0f)
            {
                _flickerLeft -= dt;
                bool isVisible = _flickerLeft <= 0f || Mathf.Repeat(_flickerLeft * _flickerRate, 1f) > 0.4f;
                _renderer.color = isVisible ? Color.white : new Color(1f, 0.6f, 0.6f, 0.35f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Coin coin))
            {
                OnCoinTouched?.Invoke(coin);
                return;
            }

            if (other.TryGetComponent(out Obstacle obstacle))
            {
                HitObstacle(obstacle);
            }
        }

        private void HitObstacle(Obstacle obstacle)
        {
            if (_isAutoPilot)
            {
                return;
            }

            bool isFirstHit = obstacle != _lastHitObstacle && _flickerLeft <= 0f;
            _lastHitObstacle = obstacle;

            int obstacleLane = ComputeNearestLane(obstacle.transform.position.x);
            bool isSideHit = obstacleLane == _lane && _previousLane != _lane
                             && Mathf.Abs(_body.position.x - LaneToX(_lane)) > _laneSpacing * 0.25f;

            if (isSideHit)
            {
                // steered into it: bounce back to where we came from
                int cameFrom = _previousLane;
                _previousLane = _lane;
                _lane = cameFrom;
            }
            else
            {
                _pushBackLeft = _pushBackDistance;
                _speed = 0f;
                SetBlocked(_lane);
            }

            if (isFirstHit)
            {
                _flickerLeft = _flickerTime;
            }

            obstacle.PlayBump();
            OnObstacleHit?.Invoke(obstacle, isFirstHit);
        }

        private int ComputeNearestLane(float x)
        {
            return Mathf.Clamp(Mathf.RoundToInt(x / _laneSpacing) + 1, 0, LaneCount - 1);
        }

        private void SetBlocked(int lane)
        {
            bool wasBlocked = IsBlocked;
            _blockedLane = lane;
            if (wasBlocked != IsBlocked)
            {
                EventBus.Publish(new BoatBlockedEvent(IsBlocked));
            }
        }
    }
}
