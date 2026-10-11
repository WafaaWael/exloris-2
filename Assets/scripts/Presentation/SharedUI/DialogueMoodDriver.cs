using System;
using BoatGame.CoreData;
using UnityEngine;
using BoatDialoguePanel = BoatGame.Presentation.BoatLevelUI.DialoguePanel;

namespace BoatGame.Presentation.SharedUI
{
    /// <summary>Changes a character's pose as each line of a dialogue starts, and back to idle when it ends.</summary>
    public class DialogueMoodDriver : MonoBehaviour
    {
        [SerializeField] private BoatDialoguePanel _dialogue;
        [SerializeField] private CharacterExpressions _character;
        [SerializeField] private CharacterMood[] _moods = Array.Empty<CharacterMood>();

        private void Awake()
        {
            _dialogue.OnLineStarted += LineStartedCallback;
            _dialogue.OnFinished += FinishedCallback;
        }

        private void OnDestroy()
        {
            _dialogue.OnLineStarted -= LineStartedCallback;
            _dialogue.OnFinished -= FinishedCallback;
        }

        private void LineStartedCallback(int index)
        {
            if (index < _moods.Length)
            {
                _character.SetMood(_moods[index]);
            }
        }

        private void FinishedCallback()
        {
            _character.SetMood(CharacterMood.Idle);
        }
    }
}
