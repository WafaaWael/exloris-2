using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using System.Collections.Generic;
public class LocalizedInstructionPlayer : MonoBehaviour
{
    [Serializable]
    private class Instruction
    {
        [Header("Caption")]
        public LocalizedString caption = new LocalizedString();
        [Header("Voice")]
        public LocalizedAudioClip voice = new LocalizedAudioClip();
        [Range(0f, 1f)]
        public float volume = 1f;
    }
[Header("Caption")]
    [SerializeField] private TMP_Text captionText;

    [SerializeField] private List<Instruction> instructions = new List<Instruction>();
    [SerializeField] private int startingInstruction;
    private float volume = 1f;

    private int currentInstruction;

    private AudioClip currentVoiceClip;

    private Instruction activeInstruction = new Instruction();
    private void OnEnable()
    {
        SelectInstruction(startingInstruction);

    }

    private void OnDisable()
    {
        UnsubscribeFromCurrentInstruction();
    }
    public void SelectInstruction(int index)
    {
        if (index < 0 || index >= instructions.Count)
        {
            Debug.LogWarning(
                $"Instruction index {index} is invalid."
            );

            return;
        }

        UnsubscribeFromCurrentInstruction();

        currentInstruction = index;
        activeInstruction = instructions[currentInstruction];
        currentVoiceClip = null;

        activeInstruction.caption.StringChanged +=
            HandleCaptionChanged;

        activeInstruction.voice.AssetChanged +=
            HandleVoiceChanged;
    }

    private void UnsubscribeFromCurrentInstruction()
    {
        if (activeInstruction == null)
            return;

        activeInstruction.caption.StringChanged -=
            HandleCaptionChanged;

        activeInstruction.voice.AssetChanged -=
            HandleVoiceChanged;

        activeInstruction = null;
    }

    private void HandleCaptionChanged(string newCaption)
    {
        captionText.text = newCaption;
    }

    private void HandleVoiceChanged(AudioClip newVoiceClip)
    {
        currentVoiceClip = newVoiceClip;
    }

    public void Play()
    {
        if (currentVoiceClip == null)
        {
            Debug.LogWarning(
                "The localized voice clip is not ready."
            );

            return;
        }
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning(
                "SoundManager instance is not available."
            );

            return;
        }
        SoundManager.Instance.PlayLocalizedClip(
            currentVoiceClip,
            volume
        );
    }
}
