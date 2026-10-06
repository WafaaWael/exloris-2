using System;
using System.Collections;
using System.Collections.Generic;
using BoatGame.CoreData;
using BoatGame.Presentation.BoatLevelUI;
using BoatGame.Presentation.SharedUI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Nilometer presentation and quiz flow. WaterOperator owns water positions; this component owns
/// the elder dialogue, player reactions, Tail buttons, stars, and lesson dialogue.
/// </summary>
public sealed class NilometerUI : MonoBehaviour
{
    [Serializable]
    public sealed class AnswerSubmittedEvent : UnityEvent2<int, int, bool> { }

    private enum PendingDialogue
    {
        None,
        StartFirstLevel,
        ShowQuestion,
        ResumeQuestion,
        MoveToNextLevel,
        StartLesson,
        StartCubitLesson,
        Complete,
    }

    private struct ButtonBinding
    {
        public Button2 Button;
        public UnityAction Listener;
    }

    [Header("Water and current Tail UI")]
    [SerializeField] private WaterOperator _waterOperator;
    [SerializeField] private Tail[] _questionPanels = Array.Empty<Tail>();
    [SerializeField] private StarsDisplay _starsDisplay;
    [SerializeField, Min(0)] private int _startingStars = 3;
    [SerializeField] private AnswerSubmittedEvent _onAnswerSubmitted = new AnswerSubmittedEvent();
    [SerializeField] private UnityEvent _onCompleted = new UnityEvent();

    [Header("Dialogue")]
    [SerializeField] private BoatGame.Presentation.SharedUI.DialoguePanel _dialogue;
    [SerializeField] private DialogueLine[] _introLines = Array.Empty<DialogueLine>();
    [SerializeField] private CharacterMood[] _introMoods = Array.Empty<CharacterMood>();
    [SerializeField] private DialogueLine[] _askLines = Array.Empty<DialogueLine>();
    [SerializeField] private DialogueLine[] _correctLines = Array.Empty<DialogueLine>();
    [SerializeField] private DialogueLine _almostLine;
    [SerializeField] private DialogueLine _sorryLine;
    [SerializeField] private DialogueLine[] _hintLines = Array.Empty<DialogueLine>();
    [SerializeField] private DialogueLine[] _lessonLines = Array.Empty<DialogueLine>();
    [SerializeField] private CharacterMood[] _lessonMoods = Array.Empty<CharacterMood>();
    [SerializeField] private DialogueLine[] _cubitLines = Array.Empty<DialogueLine>();
    [SerializeField] private CharacterMood[] _cubitMoods = Array.Empty<CharacterMood>();
    [SerializeField] private Button2 _hintButton;

    [Header("Characters")]
    [SerializeField] private PlayerAvatarSelector _playerAvatarSelector;
    [SerializeField] private CharacterExpressions _elder;
    [SerializeField] private CharacterExpressions _boy;
    [SerializeField] private CharacterExpressions _girl;

    [Header("Optional old Nilometer overlays")]
    [SerializeField] private GameObject _lessonBanner;
    [SerializeField] private GameObject _cubitCard;

    private readonly List<ButtonBinding> _buttonBindings = new List<ButtonBinding>();
    private readonly HashSet<Button2> _disabledAnswers = new HashSet<Button2>();
    private readonly DialogueLine[] _singleLine = new DialogueLine[1];
    private int _activeLevelIndex = -1;
    private int _starsRemaining;
    private int _attemptsForLevel;
    private int _hintIndex;
    private bool _acceptingAnswers;
    private PendingDialogue _pendingDialogue;
    private CharacterMood[] _activeElderMoods;
    private CharacterExpressions _traveler;

    private void Awake()
    {
        _waterOperator ??= FindAnyObjectByType<WaterOperator>();
        _starsDisplay ??= FindAnyObjectByType<StarsDisplay>();
        _dialogue ??= FindAnyObjectByType<BoatGame.Presentation.SharedUI.DialoguePanel>();
        _playerAvatarSelector ??= FindAnyObjectByType<PlayerAvatarSelector>();
        _elder ??= FindAnyObjectByType<CharacterExpressions>();

        CacheQuestionPanels();
        BindButtons();

        if (_dialogue != null)
        {
            _dialogue.OnFinished += DialogueFinishedCallback;
            _dialogue.OnLineStarted += DialogueLineStartedCallback;
        }

        if (_hintButton != null)
        {
            _hintButton.onClick.AddListener(OnButtonClick_Hint);
        }
    }

    private IEnumerator Start()
    {
        // Let PlayerAvatarSelector apply the saved choice in its Start method before querying GetCurrentGender().
        yield return null;
        SelectTravelerFromAvatarSelector();
        _starsRemaining = _startingStars;
        _starsDisplay?.ShowStars(_starsRemaining, false);
        ShowOnlyPanel(-1);
        SetActive(_lessonBanner, false);
        SetActive(_cubitCard, false);
        PlayLines(_introLines, _introMoods, PendingDialogue.StartFirstLevel);
    }

    private void OnEnable()
    {
        if (_waterOperator != null)
        {
            _waterOperator.WaterLevelReached += WaterLevelReachedCallback;
        }
    }

    private void OnDisable()
    {
        if (_waterOperator != null)
        {
            _waterOperator.WaterLevelReached -= WaterLevelReachedCallback;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _buttonBindings.Count; i++)
        {
            ButtonBinding binding = _buttonBindings[i];
            if (binding.Button != null)
            {
                binding.Button.onClick.RemoveListener(binding.Listener);
            }
        }

        if (_dialogue != null)
        {
            _dialogue.OnFinished -= DialogueFinishedCallback;
            _dialogue.OnLineStarted -= DialogueLineStartedCallback;
        }

        if (_hintButton != null)
        {
            _hintButton.onClick.RemoveListener(OnButtonClick_Hint);
        }
    }

    /// <summary>Can be connected to an Info button. Replays the next old-Nilometer hint for the current question.</summary>
    public void OnButtonClick_Hint()
    {
        if (_acceptingAnswers == false || _hintLines.Length == 0)
        {
            return;
        }

        SetAnswersInteractable(_activeLevelIndex, false);
        DialogueLine hint = _hintLines[_hintIndex % _hintLines.Length];
        _hintIndex++;
        PlaySingleLine(hint, CharacterMood.Point, PendingDialogue.ResumeQuestion);
    }

    /// <summary>Called by the Tail buttons. This combines current star/button feedback with the old spoken feedback.</summary>
    public void SubmitAnswer(int levelIndex, int answerIndex)
    {
        if (_acceptingAnswers == false || levelIndex != _activeLevelIndex || _waterOperator == null)
        {
            return;
        }

        bool isCorrect = _waterOperator.IsCorrectAnswer(levelIndex, answerIndex);
        _onAnswerSubmitted?.Invoke(levelIndex, answerIndex, isCorrect);
        _acceptingAnswers = false;
        SetAnswersInteractable(levelIndex, false);

        if (isCorrect)
        {
            _traveler?.SetMood(CharacterMood.Happy);
            _traveler?.PlayHop();
            _elder?.SetMood(CharacterMood.Happy);
            PlaySingleLine(SelectCorrectLine(_waterOperator.GetCorrectDialogueLineIndex(levelIndex)), CharacterMood.Happy,
                levelIndex + 1 < _waterOperator.LevelCount ? PendingDialogue.MoveToNextLevel : PendingDialogue.StartLesson);
            return;
        }

        _attemptsForLevel++;
        _starsRemaining = Mathf.Max(0, _starsRemaining - 1);
        _starsDisplay?.ShowStars(_starsRemaining, true);
        SetButtonInteractable(levelIndex, answerIndex, false);

        if (_attemptsForLevel < 2)
        {
            // Current feedback: a star is removed and the wrong answer is disabled.
            // Old feedback: elder says "Almost" and allows one more try.
            _traveler?.SetMood(CharacterMood.Oops);
            PlaySingleLine(_almostLine, CharacterMood.Point, PendingDialogue.ResumeQuestion);
            return;
        }

        // The second miss keeps current star feedback and continues after the old "Sorry" explanation.
        _traveler?.SetMood(CharacterMood.Sad);
        PlaySingleLine(_sorryLine, CharacterMood.Sorry,
            levelIndex + 1 < _waterOperator.LevelCount ? PendingDialogue.MoveToNextLevel : PendingDialogue.StartLesson);
    }

    private void WaterLevelReachedCallback(int levelIndex, float targetY)
    {
        _activeLevelIndex = levelIndex;
        _attemptsForLevel = 0;
        _acceptingAnswers = false;
        ResetAnswerButtons(levelIndex);
        SetAllButtonsInteractable(levelIndex, true);
        ShowOnlyPanel(-1);
        _elder?.SetMood(CharacterMood.Idle);
        _traveler?.SetMood(CharacterMood.Think);

        DialogueLine ask = SelectAskLine(levelIndex);
        if (string.IsNullOrEmpty(ask.Text))
        {
            CompletePendingDialogue(PendingDialogue.ShowQuestion);
            return;
        }

        PlaySingleLine(ask, CharacterMood.Point, PendingDialogue.ShowQuestion);
    }

    private void PlayLines(DialogueLine[] lines, CharacterMood[] moods, PendingDialogue pending)
    {
        _pendingDialogue = pending;
        _activeElderMoods = moods;

        if (_dialogue == null || lines == null || lines.Length == 0)
        {
            CompletePendingDialogue(pending);
            return;
        }

        _dialogue.PlayLines(lines, true, true);
    }

    private void PlaySingleLine(DialogueLine line, CharacterMood mood, PendingDialogue pending)
    {
        _elder?.SetMood(mood);
        _singleLine[0] = line;
        PlayLines(_singleLine, null, pending);
    }

    private void DialogueFinishedCallback()
    {
        PendingDialogue pending = _pendingDialogue;
        _pendingDialogue = PendingDialogue.None;
        CompletePendingDialogue(pending);
    }

    private void DialogueLineStartedCallback(int index)
    {
        if (_activeElderMoods != null && index < _activeElderMoods.Length)
        {
            _elder?.SetMood(_activeElderMoods[index]);
        }
    }

    private void CompletePendingDialogue(PendingDialogue pending)
    {
        switch (pending)
        {
            case PendingDialogue.StartFirstLevel:
                if (_waterOperator == null || _waterOperator.MoveToLevel(0) == false)
                {
                    Debug.LogWarning("NilometerUI could not start the first water level.", this);
                }
                break;
            case PendingDialogue.ShowQuestion:
            case PendingDialogue.ResumeQuestion:
                _acceptingAnswers = true;
                SetAnswersInteractable(_activeLevelIndex, true);
                ShowOnlyPanel(_activeLevelIndex);
                break;
            case PendingDialogue.MoveToNextLevel:
                ShowOnlyPanel(-1);
                _dialogue?.Hide();
                _waterOperator?.MoveToNextLevel();
                break;
            case PendingDialogue.StartLesson:
                ShowOnlyPanel(-1);
                SetActive(_lessonBanner, true);
                _traveler?.SetMood(CharacterMood.Idle);
                PlayLines(_lessonLines, _lessonMoods, PendingDialogue.StartCubitLesson);
                break;
            case PendingDialogue.StartCubitLesson:
                SetActive(_lessonBanner, false);
                SetActive(_cubitCard, true);
                _traveler?.SetMood(CharacterMood.Think);
                PlayLines(_cubitLines, _cubitMoods, PendingDialogue.Complete);
                break;
            case PendingDialogue.Complete:
                SetActive(_cubitCard, false);
                _elder?.SetMood(CharacterMood.Happy);
                _traveler?.SetMood(CharacterMood.Happy);
                _traveler?.PlayHop();
                _onCompleted?.Invoke();
                break;
        }
    }

    private void SelectTravelerFromAvatarSelector()
    {
        // PlayerAvatarSelector is the source of truth for the selected child; do not read the save system here.
        PlayerGender gender = _playerAvatarSelector != null
            ? _playerAvatarSelector.GetCurrentGender()
            : PlayerGender.NotSelected;

        _traveler = gender == PlayerGender.Female ? _girl : _boy;
        if (_traveler == null && _playerAvatarSelector != null)
        {
            CharacterExpressions[] avatars = _playerAvatarSelector.GetComponentsInChildren<CharacterExpressions>(true);
            for (int i = 0; i < avatars.Length; i++)
            {
                if (avatars[i].gameObject.activeInHierarchy)
                {
                    _traveler = avatars[i];
                    break;
                }
            }
        }
    }

    private void CacheQuestionPanels()
    {
        if (_questionPanels != null && _questionPanels.Length > 0)
        {
            return;
        }

        _questionPanels = GetComponentsInChildren<Tail>(true);
        Array.Sort(_questionPanels, ComparePanels);
    }

    private void BindButtons()
    {
        for (int levelIndex = 0; levelIndex < _questionPanels.Length; levelIndex++)
        {
            Button2[] buttons = _questionPanels[levelIndex].GetComponentsInChildren<Button2>(true);
            for (int answerIndex = 0; answerIndex < buttons.Length; answerIndex++)
            {
                int capturedLevel = levelIndex;
                int capturedAnswer = answerIndex;
                Button2 button = buttons[answerIndex];
                UnityAction listener = () => SubmitAnswer(capturedLevel, capturedAnswer);
                button.onClick.AddListener(listener);
                _buttonBindings.Add(new ButtonBinding { Button = button, Listener = listener });
            }
        }
    }

    private void ShowOnlyPanel(int panelIndex)
    {
        for (int i = 0; i < _questionPanels.Length; i++)
        {
            if (_questionPanels[i] != null)
            {
                _questionPanels[i].gameObject.SetActive(i == panelIndex);
            }
        }
    }

    private void SetAnswersInteractable(int levelIndex, bool interactable)
    {
        if (levelIndex < 0 || levelIndex >= _questionPanels.Length)
        {
            return;
        }

        Button2[] buttons = _questionPanels[levelIndex].GetComponentsInChildren<Button2>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = interactable && _disabledAnswers.Contains(buttons[i]) == false;
        }
    }

    private void SetAllButtonsInteractable(int levelIndex, bool interactable)
    {
        SetAnswersInteractable(levelIndex, interactable);
    }

    private void SetButtonInteractable(int levelIndex, int answerIndex, bool interactable)
    {
        if (levelIndex < 0 || levelIndex >= _questionPanels.Length)
        {
            return;
        }

        Button2[] buttons = _questionPanels[levelIndex].GetComponentsInChildren<Button2>(true);
        if (answerIndex >= 0 && answerIndex < buttons.Length)
        {
            if (interactable)
            {
                _disabledAnswers.Remove(buttons[answerIndex]);
            }
            else
            {
                _disabledAnswers.Add(buttons[answerIndex]);
            }

            buttons[answerIndex].interactable = interactable;
        }
    }

    private void ResetAnswerButtons(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= _questionPanels.Length)
        {
            return;
        }

        Button2[] buttons = _questionPanels[levelIndex].GetComponentsInChildren<Button2>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            _disabledAnswers.Remove(buttons[i]);
        }
    }

    private DialogueLine SelectAskLine(int levelIndex)
    {
        if (_askLines == null || _askLines.Length == 0)
        {
            return default;
        }

        return _askLines[Mathf.Min(levelIndex == 0 ? 0 : 1, _askLines.Length - 1)];
    }

    private DialogueLine SelectCorrectLine(int levelIndex)
    {
        if (_correctLines == null || _correctLines.Length == 0)
        {
            return default;
        }

        return _correctLines[Mathf.Clamp(levelIndex, 0, _correctLines.Length - 1)];
    }

    private static void SetActive(GameObject gameObject, bool value)
    {
        if (gameObject != null)
        {
            gameObject.SetActive(value);
        }
    }

    private static int ComparePanels(Tail left, Tail right)
    {
        return GetPanelIndex(left.name).CompareTo(GetPanelIndex(right.name));
    }

    private static int GetPanelIndex(string panelName)
    {
        int openParenthesis = panelName.IndexOf('(');
        if (openParenthesis < 0)
        {
            return 0;
        }

        int closeParenthesis = panelName.IndexOf(')', openParenthesis + 1);
        return closeParenthesis > openParenthesis
            && int.TryParse(panelName.Substring(openParenthesis + 1, closeParenthesis - openParenthesis - 1), out int index)
            ? index + 1
            : 0;
    }
}
