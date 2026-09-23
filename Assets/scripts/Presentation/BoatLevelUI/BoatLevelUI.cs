using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using BoatGame.GameplaySystems.BoatLevel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoatGame.Presentation.BoatLevelUI
{
    /// <summary>Switches the boat level screens (intro, tutorial, HUD, arrival, end card) from level state events.</summary>
    public class BoatLevelUI : MonoBehaviour
    {
        private const string TouchHelp = "Tap the <b>left</b> or <b>right</b> side of the screen\n(or swipe) to steer the boat!";
        private const string KeyboardHelp = "Press <b>A</b> / <b>D</b> or the <b>arrow keys</b>\nto steer the boat!";

        [Header("Intro")]
        [SerializeField] private GameObject _introScreen;
        [SerializeField] private DialoguePanel _introDialogue;

        [Header("Tutorial / help")]
        [SerializeField] private GameObject _tutorialOverlay;
        [SerializeField] private Button _tutorialTapArea;
        [SerializeField] private TMP_Text _tutorialText;
        [SerializeField] private TMP_Text _tutorialFooter;

        [Header("HUD")]
        [SerializeField] private GameObject _hud;
        [SerializeField] private StarsDisplay _hudStars;
        [SerializeField] private TMP_Text _hudCoins;
        [SerializeField] private Button _helpButton;
        [SerializeField] private Image _progressFill;
        [SerializeField] private GameObject _blockedHint;

        [Header("Arrival")]
        [SerializeField] private GameObject _arrivalBanner;
        [SerializeField] private GameObject _arrivalScreen;
        [SerializeField] private DialoguePanel _arrivalDialogue;

        [Header("End card")]
        [SerializeField] private GameObject _completeCard;
        [SerializeField] private StarsDisplay _completeStars;
        [SerializeField] private TMP_Text _completeCoins;
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _nextButton;

        private Service<BoatLevelFlow> _levelFlow;

        public void OnButtonClick_TutorialTap()
        {
            EventBus.Publish(new PlaySfxEvent(SfxId.UiTap));
            _levelFlow.Value.StartPlaying();
        }

        public void OnButtonClick_Help()
        {
            EventBus.Publish(new PlaySfxEvent(SfxId.UiTap));
            _levelFlow.Value.OpenHelp();
        }

        public void OnButtonClick_Replay()
        {
            EventBus.Publish(new PlaySfxEvent(SfxId.UiTap));
            _levelFlow.Value.ReplayLevel();
        }

        public void OnButtonClick_Next()
        {
            EventBus.Publish(new PlaySfxEvent(SfxId.UiTap));
            _levelFlow.Value.LoadNextSection();
        }

        private void Awake()
        {
            bool isTouch = Application.isMobilePlatform || UnityEngine.InputSystem.Touchscreen.current != null;
            _tutorialText.text = isTouch ? TouchHelp : KeyboardHelp;

            _tutorialTapArea.onClick.AddListener(OnButtonClick_TutorialTap);
            _helpButton.onClick.AddListener(OnButtonClick_Help);
            _replayButton.onClick.AddListener(OnButtonClick_Replay);
            _nextButton.onClick.AddListener(OnButtonClick_Next);
            _introDialogue.OnFinished += IntroFinishedCallback;
            _arrivalDialogue.OnFinished += ArrivalFinishedCallback;

            HideAll();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<LevelStateChangedEvent>(LevelStateChangedCallback);
            EventBus.Subscribe<BoatHitObstacleEvent>(BoatHitObstacleCallback);
            EventBus.Subscribe<CoinCollectedEvent>(CoinCollectedCallback);
            EventBus.Subscribe<LevelProgressEvent>(LevelProgressCallback);
            EventBus.Subscribe<BoatBlockedEvent>(BoatBlockedCallback);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<LevelStateChangedEvent>(LevelStateChangedCallback);
            EventBus.Unsubscribe<BoatHitObstacleEvent>(BoatHitObstacleCallback);
            EventBus.Unsubscribe<CoinCollectedEvent>(CoinCollectedCallback);
            EventBus.Unsubscribe<LevelProgressEvent>(LevelProgressCallback);
            EventBus.Unsubscribe<BoatBlockedEvent>(BoatBlockedCallback);
        }

        private void OnDestroy()
        {
            _tutorialTapArea.onClick.RemoveListener(OnButtonClick_TutorialTap);
            _helpButton.onClick.RemoveListener(OnButtonClick_Help);
            _replayButton.onClick.RemoveListener(OnButtonClick_Replay);
            _nextButton.onClick.RemoveListener(OnButtonClick_Next);
            _introDialogue.OnFinished -= IntroFinishedCallback;
            _arrivalDialogue.OnFinished -= ArrivalFinishedCallback;
        }

        private void HideAll()
        {
            _introScreen.SetActive(false);
            _tutorialOverlay.SetActive(false);
            _hud.SetActive(false);
            _blockedHint.SetActive(false);
            _arrivalBanner.SetActive(false);
            _arrivalScreen.SetActive(false);
            _completeCard.SetActive(false);
        }

        private void LevelStateChangedCallback(LevelStateChangedEvent evt)
        {
            BoatLevelFlow flow = _levelFlow.Value;
            HideAll();

            switch (evt.Current)
            {
                case LevelState.Intro:
                    _introScreen.SetActive(true);
                    _introDialogue.Play();
                    break;
                case LevelState.Tutorial:
                case LevelState.Paused:
                    ShowHud(flow);
                    _tutorialOverlay.SetActive(true);
                    _tutorialFooter.text = evt.Current == LevelState.Paused ? "Tap to continue" : "Tap to start!";
                    break;
                case LevelState.Playing:
                    ShowHud(flow);
                    break;
                case LevelState.Arriving:
                    ShowHud(flow);
                    _arrivalBanner.SetActive(true);
                    break;
                case LevelState.Arrived:
                    _arrivalScreen.SetActive(true);
                    _arrivalDialogue.Play();
                    break;
                case LevelState.Complete:
                    _completeCard.SetActive(true);
                    _completeStars.ShowStars(flow.Stars, false);
                    _completeCoins.text = flow.Coins.ToString();
                    break;
            }
        }

        private void ShowHud(BoatLevelFlow flow)
        {
            _hud.SetActive(true);
            _hudStars.ShowStars(flow.Stars, false);
            _hudCoins.text = flow.Coins.ToString();
        }

        private void IntroFinishedCallback()
        {
            _levelFlow.Value.CompleteIntro();
        }

        private void ArrivalFinishedCallback()
        {
            _levelFlow.Value.CompleteArrival();
        }

        private void BoatHitObstacleCallback(BoatHitObstacleEvent evt)
        {
            _hudStars.ShowStars(evt.StarsLeft, evt.DidLoseStar);
        }

        private void CoinCollectedCallback(CoinCollectedEvent evt)
        {
            _hudCoins.text = evt.TotalCoins.ToString();
        }

        private void LevelProgressCallback(LevelProgressEvent evt)
        {
            _progressFill.fillAmount = evt.Progress01;
        }

        private void BoatBlockedCallback(BoatBlockedEvent evt)
        {
            _blockedHint.SetActive(evt.IsBlocked && _hud.activeSelf);
        }
    }
}
