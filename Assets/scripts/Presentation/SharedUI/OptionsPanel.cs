using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoatGame.Presentation.SharedUI
{
    /// <summary>
    /// Options window: music, sounds and voice volume, plus VSync and the frame cap. Changes apply live and are saved
    /// when the window closes. A sample sound plays when the sounds or voice slider is released, so the child hears
    /// the new level. The frame cap is dimmed while VSync is on (the display refresh paces the game then); the VSync
    /// switch is hidden on phones, which always sync.
    /// </summary>
    public class OptionsPanel : MonoBehaviour
    {
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _soundsSlider;
        [SerializeField] private Slider _voiceSlider;
        [SerializeField] private Button2 _closeButton;
        [SerializeField] private AudioClip _voiceSample;

      

        private bool _isRefreshing;
        private bool _isSoundsDirty;
        private bool _isVoiceDirty;

        public void Open()
        {
            _isRefreshing = true;
            _musicSlider.value = GameSettings.Music;
            _soundsSlider.value = GameSettings.Sounds;
            _voiceSlider.value = GameSettings.Voice;
            _isRefreshing = false;
            gameObject.SetActive(true);
        }




        public void OnButtonClick_Close()
        {
            EventBus.Publish(new PlaySfxEvent(SfxId.UiTap));
            GameSettings.Save();
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            _musicSlider.onValueChanged.AddListener(MusicChangedCallback);
            _soundsSlider.onValueChanged.AddListener(SoundsChangedCallback);
            _voiceSlider.onValueChanged.AddListener(VoiceChangedCallback);
            _closeButton.onClick.AddListener(OnButtonClick_Close);
 
        }

        private void OnDestroy()
        {
            _musicSlider.onValueChanged.RemoveListener(MusicChangedCallback);
            _soundsSlider.onValueChanged.RemoveListener(SoundsChangedCallback);
            _voiceSlider.onValueChanged.RemoveListener(VoiceChangedCallback);
            _closeButton.onClick.RemoveListener(OnButtonClick_Close);            
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }




        private static int ComputeChoiceIndex(int[] choices, int count, int cap)
        {
            for (int i = 0; i < count; i++)
            {
                if (choices[i] == cap)
                {
                    return i;
                }
            }

            return 1;   // 60
        }

        /*private void RefreshDisplayOptions()
        {
            bool isVSync = FrameRateSettings.IsVSyncSupported && GameSettings.IsVSync;
            if (_vSyncLabel != null)
            {
                _vSyncLabel.text = isVSync ? "ON" : "OFF";
            }

            if (_vSyncImage != null)
            {
                _vSyncImage.color = isVSync ? _onColor : _offColor;
            }

            if (_frameCapLabel == null)
            {
                return;
            }

            int[] choices = FrameRateSettings.FrameCapChoices;
            int count = ComputeChoiceCount(choices);
            int index = ComputeChoiceIndex(choices, count, GameSettings.FrameCap);
            int cap = choices[index];
            _frameCapLabel.text = cap == GameSettings.UnlimitedFrameRate ? "MAX" : cap.ToString();
            _frameCapLess.interactable = isVSync == false && index > 0;
            _frameCapMore.interactable = isVSync == false && index < count - 1;
            _frameCapGroup.alpha = isVSync ? 0.45f : 1f;
        }*/

        private void Update()
        {
            // a short preview after letting go of a slider (mouse or touch release)
            if (UnityEngine.InputSystem.Pointer.current == null || UnityEngine.InputSystem.Pointer.current.press.wasReleasedThisFrame == false)
            {
                return;
            }

            if (_isSoundsDirty)
            {
                EventBus.Publish(new PlaySfxEvent(SfxId.Correct));
            }

            if (_isVoiceDirty && _voiceSample != null)
            {
                EventBus.Publish(new PlayVoiceEvent(_voiceSample));
            }

            _isSoundsDirty = false;
            _isVoiceDirty = false;
        }

        private void MusicChangedCallback(float value)
        {
            if (_isRefreshing == false)
            {
                GameSettings.Music = value;
                EventBus.Publish(new AudioSettingsChangedEvent());
            }
        }

        private void SoundsChangedCallback(float value)
        {
            if (_isRefreshing == false)
            {
                GameSettings.Sounds = value;
                _isSoundsDirty = true;
                EventBus.Publish(new AudioSettingsChangedEvent());
            }
        }

        private void VoiceChangedCallback(float value)
        {
            if (_isRefreshing == false)
            {
                GameSettings.Voice = value;
                _isVoiceDirty = true;
                EventBus.Publish(new AudioSettingsChangedEvent());
            }
        }
    }
}
