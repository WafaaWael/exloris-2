using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Stores the Nilometer water-level data and moves the water to a selected level.
/// NilometerUI chooses when to advance and owns the player-facing feedback.
/// </summary>
public sealed class WaterOperator : MonoBehaviour
{
    [Serializable]
    public sealed class WaterLevel
    {
        [SerializeField] private string _id;
        [SerializeField] private float _targetY;
        [SerializeField] private int _correctAnswerIndex;
        [Tooltip("Index of the old Nilometer correct-dialogue line (zero cubits = 0).")]
        [SerializeField, Min(0)] private int _correctDialogueLineIndex;
        [SerializeField] private float _duration = 1f;

        public string Id => _id;
        public float TargetY => _targetY;
        public int CorrectAnswerIndex => _correctAnswerIndex;
        public int CorrectDialogueLineIndex => _correctDialogueLineIndex;
        public float Duration=> _duration;
        public WaterLevel(string id, float targetY, int correctAnswerIndex, int correctDialogueLineIndex = 0, float duration = 1f)
        {
            _id = id;
            _targetY = targetY;
            _correctAnswerIndex = correctAnswerIndex;
            _correctDialogueLineIndex = correctDialogueLineIndex;
            _duration = duration;
        }
    }

    [Serializable]
    public sealed class WaterLevelReachedEvent : UnityEvent2<int, float> { }

    [SerializeField] private WaterAnimator _waterAnimator;
    [SerializeField] private List<WaterLevel> _waterLevels = new List<WaterLevel>();
    [SerializeField] private WaterLevelReachedEvent _onWaterLevelReached = new WaterLevelReachedEvent();

    // Migrates values already configured in the Nilometer scene before this refactor.
    [FormerlySerializedAs("_targetYSize")]
    [SerializeField, HideInInspector] private List<float> _legacyTargetYPositions = new List<float> { 6f, -8f, -5f };

    private int _currentLevelIndex = -1;
    private bool _isAnimating;

    /// <summary>Raised after the water arrives at a level: level index, then target Y position.</summary>
    public event Action<int, float> WaterLevelReached;

    public int LevelCount => _waterLevels.Count;
    public int CurrentLevelIndex => _currentLevelIndex;
    public bool IsAnimating => _isAnimating;

    private void Awake()
    {
        EnsureWaterLevels();
    }

    /// <summary>Moves the water to a level. Returns false when the level is invalid or a move is in progress.</summary>
    public bool MoveToLevel(int levelIndex)
    {
        if (_isAnimating || _waterAnimator == null || levelIndex < 0 || levelIndex >= _waterLevels.Count)
        {
            return false;
        }

        _isAnimating = true;
        _currentLevelIndex = levelIndex;
        WaterLevel level = _waterLevels[levelIndex];
        _waterAnimator.AnimateYSize(level.TargetY, () => CompleteMove(levelIndex, level.TargetY));
        return true;
    }

    public bool MoveToNextLevel()
    {
        return MoveToLevel(_currentLevelIndex + 1);
    }
    public void MoveWaterTemp(float targetY){
        if (_waterAnimator == null)
        {
            return;
        }

        _waterAnimator.AnimateYSize(targetY);
    }
    public bool IsCorrectAnswer(int levelIndex, int answerIndex)
    {
        return levelIndex >= 0
            && levelIndex < _waterLevels.Count
            && _waterLevels[levelIndex].CorrectAnswerIndex == answerIndex;
    }

    public string GetLevelId(int levelIndex)
    {
        return levelIndex >= 0 && levelIndex < _waterLevels.Count
            ? _waterLevels[levelIndex].Id
            : string.Empty;
    }

    public int GetCorrectDialogueLineIndex(int levelIndex)
    {
        return levelIndex >= 0 && levelIndex < _waterLevels.Count
            ? _waterLevels[levelIndex].CorrectDialogueLineIndex
            : 0;
    }

    private void CompleteMove(int levelIndex, float targetY)
    {
        _isAnimating = false;
        _onWaterLevelReached?.Invoke(levelIndex, targetY);
        WaterLevelReached?.Invoke(levelIndex, targetY);
    }

    private void EnsureWaterLevels()
    {
        if (_waterLevels.Count > 0)
        {
            return;
        }

        if (_legacyTargetYPositions == null || _legacyTargetYPositions.Count == 0)
        {
            _legacyTargetYPositions = new List<float> { 6f, -8f, -5f };
        }

        for (int i = 0; i < _legacyTargetYPositions.Count; i++)
        {
            _waterLevels.Add(new WaterLevel($"Level{i + 1}", _legacyTargetYPositions[i], 0, i));
        }
    }
}
