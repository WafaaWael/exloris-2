using System.Collections;
using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>
    /// Owns the boat level: state machine (intro, tutorial, play, pause, arrival, complete), stars and coins.
    /// UI drives it through the public methods and listens to the published events.
    /// </summary>
    public class BoatLevelFlow : MonoBehaviour
    {
        private const int MaxStars = 3;
        private const float ProgressPublishStep = 0.005f;

        [SerializeField] private BoatController _boat;
        [SerializeField] private BoatInput _input;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private Transform _dockStopPoint;
        [SerializeField] private float _autoPilotDistance = 9f;
        [SerializeField] private float _arrivedDelay = 1.6f;
        [SerializeField] private float _cameraStopOffset = 3.5f;
        [SerializeField] private string _nextSceneName = "BoatHistory";

        private float _startY;
        private float _lastPublishedProgress = -1f;

        public LevelState State { get; private set; } = LevelState.Intro;
        public int Stars { get; private set; } = MaxStars;
        public int Coins { get; private set; }
        public int MaxStarCount => MaxStars;

        public void CompleteIntro()
        {
            if (State == LevelState.Intro)
            {
                SetState(LevelState.Tutorial);
            }
        }

        public void StartPlaying()
        {
            if (State != LevelState.Tutorial && State != LevelState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            _boat.IsMoving = true;
            _input.IsInputEnabled = true;
            SetState(LevelState.Playing);
        }

        public void OpenHelp()
        {
            if (State != LevelState.Playing)
            {
                return;
            }

            Time.timeScale = 0f;
            _input.IsInputEnabled = false;
            SetState(LevelState.Paused);
        }

        public void CompleteArrival()
        {
            if (State == LevelState.Arrived)
            {
                EventBus.Publish(new PlaySfxEvent(SfxId.Win));
                SetState(LevelState.Complete);
            }
        }

        public void ReplayLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadNextSection()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_nextSceneName);
        }

        private void Awake()
        {
            _startY = _boat.transform.position.y;
            _cameraFollow.MaxY = _dockStopPoint.position.y + _cameraStopOffset;
        }

        private void OnEnable()
        {
            _boat.OnObstacleHit += ObstacleHitCallback;
            _boat.OnCoinTouched += CoinTouchedCallback;
            _input.OnLaneStep += LaneStepCallback;
        }

        private void OnDisable()
        {
            _boat.OnObstacleHit -= ObstacleHitCallback;
            _boat.OnCoinTouched -= CoinTouchedCallback;
            _input.OnLaneStep -= LaneStepCallback;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            _boat.IsMoving = false;
            _input.IsInputEnabled = false;
            // announce the initial state so UI and audio can set themselves up
            EventBus.Publish(new LevelStateChangedEvent(LevelState.Intro, LevelState.Intro));
        }

        private void Update()
        {
            if (State != LevelState.Playing && State != LevelState.Arriving)
            {
                return;
            }

            float boatY = _boat.transform.position.y;
            float stopY = _dockStopPoint.position.y;
            float progress = ComputeProgress(_startY, stopY, boatY);
            _boat.SetProgress(progress);
            PublishProgress(progress);

            if (State == LevelState.Playing && boatY >= stopY - _autoPilotDistance)
            {
                BeginArrival(stopY);
            }
        }

        private static float ComputeProgress(float startY, float endY, float y)
        {
            return Mathf.Clamp01((y - startY) / Mathf.Max(0.01f, endY - startY));
        }

        private void PublishProgress(float progress)
        {
            if (Mathf.Abs(progress - _lastPublishedProgress) < ProgressPublishStep && progress < 1f)
            {
                return;
            }

            _lastPublishedProgress = progress;
            EventBus.Publish(new LevelProgressEvent(progress));
        }

        private void BeginArrival(float stopY)
        {
            _input.IsInputEnabled = false;
            _boat.StartAutoPilot(stopY);
            SetState(LevelState.Arriving);
            StartCoroutine(WaitForDocking());
        }

        private IEnumerator WaitForDocking()
        {
            while (_boat.HasReachedAutoPilotStop() == false)
            {
                yield return null;
            }

            _boat.IsMoving = false;
            yield return new WaitForSeconds(_arrivedDelay);
            SetState(LevelState.Arrived);
        }

        private void SetState(LevelState next)
        {
            LevelState previous = State;
            State = next;
            EventBus.Publish(new LevelStateChangedEvent(previous, next));
        }

        private void LaneStepCallback(int direction)
        {
            _boat.StepLane(direction);
        }

        private void ObstacleHitCallback(Obstacle obstacle, bool isFirstHit)
        {
            EventBus.Publish(new PlaySfxEvent(SfxId.Bump));

            bool didLoseStar = isFirstHit && Stars > 0;
            if (didLoseStar)
            {
                Stars--;
                EventBus.Publish(new PlaySfxEvent(SfxId.StarLost));
            }

            // splash where the hull meets the obstacle, roughly between the two
            Vector3 contact = Vector3.Lerp(obstacle.transform.position, _boat.transform.position, 0.45f);
            EventBus.Publish(new BoatHitObstacleEvent(Stars, didLoseStar, contact));
        }

        private void CoinTouchedCallback(Coin coin)
        {
            if (coin.IsCollected)
            {
                return;
            }

            coin.Collect();
            Coins++;
            EventBus.Publish(new PlaySfxEvent(SfxId.Coin));
            EventBus.Publish(new CoinCollectedEvent(Coins, coin.transform.position));
        }
    }
}
