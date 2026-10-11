using System;
using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoatGame.Presentation.BoatLevelUI
{
    /// <summary>
    /// Elder dialogue with a typewriter effect and optional voice-over per line.
    /// With a voice clip, letters follow the voice: they only advance while the elder is actually speaking
    /// (a speech timeline is extracted from each clip once, on load). Without a clip a fixed speed is used.
    /// Tap once to finish the current line, tap again for the next line. Runs on unscaled time.
    /// </summary>
    public class DialoguePanel : MonoBehaviour
    {
        public event Action OnFinished;
        public event Action<int> OnLineStarted;

        private const float MaxTypingDelta = 1f / 20f;
        private const float FirstLineDelay = 0.4f;
        private const int TimelineRate = 50;               // speech timeline samples per second
        private const float SpeechThreshold = 0.08f;       // fraction of the clip's loudest frame counted as speech
        private const float TypingLead = 0.12f;            // letters run slightly ahead of the voice, which reads better

        [SerializeField] private TMP_Text _text;
        [SerializeField] private Button _tapArea;
        [SerializeField] private GameObject _continueHint;
        [SerializeField] private float _charactersPerSecond = 30f;
        [SerializeField] private DialogueLine[] _lines = Array.Empty<DialogueLine>();

        private float[][] _speechTimelines;
        private int _lineIndex;
        private float _elapsed;
        private int _totalCharacters;
        private bool _isTyping;
        private bool _isVoiceStarted;
        private float _startDelayLeft;
        private double _voiceStartDsp;

        public void Play()
        {
            gameObject.SetActive(true);
            _lineIndex = 0;
            ShowLine(_lineIndex);
            _startDelayLeft = FirstLineDelay;
        }

        public void OnButtonClick_Advance()
        {
            EventBus.Publish(new PlaySfxEvent(SfxId.UiTap));
            AdvanceDialogue();
        }

        private void Awake()
        {
            _tapArea.onClick.AddListener(OnButtonClick_Advance);
            BuildSpeechTimelines();
        }

        private void OnDestroy()
        {
            _tapArea.onClick.RemoveListener(OnButtonClick_Advance);
        }

        private void Update()
        {
            if (_isTyping == false)
            {
                return;
            }

            // clamp: the first frames after a scene load report huge deltas, which would type the whole line at once
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxTypingDelta);
            if (_startDelayLeft > 0f)
            {
                _startDelayLeft -= dt;
                return;
            }

            if (_isVoiceStarted == false)
            {
                _isVoiceStarted = true;
                _elapsed = 0f;
                _voiceStartDsp = AudioSettings.dspTime;
                EventBus.Publish(new PlayVoiceEvent(_lines[_lineIndex].Voice));
            }

            // follow the audio clock when it runs ahead (frame hitch), frame time otherwise (audio muted/disabled)
            float dspElapsed = (float)(AudioSettings.dspTime - _voiceStartDsp);
            _elapsed = Mathf.Max(_elapsed + dt, dspElapsed);
            float progress = ComputeProgress(_speechTimelines[_lineIndex], _elapsed + TypingLead, _elapsed * _charactersPerSecond / Mathf.Max(1, _totalCharacters));
            int visible = Mathf.Min(Mathf.CeilToInt(progress * _totalCharacters), _totalCharacters);
            _text.maxVisibleCharacters = visible;

            if (visible >= _totalCharacters)
            {
                FinishTyping();
            }
        }

        private void AdvanceDialogue()
        {
            if (_isTyping)
            {
                FinishTyping();
                return;
            }

            _lineIndex++;
            if (_lineIndex < _lines.Length)
            {
                ShowLine(_lineIndex);
                return;
            }

            EventBus.Publish(new PlayVoiceEvent(null));
            gameObject.SetActive(false);
            OnFinished?.Invoke();
        }

        private void ShowLine(int index)
        {
            DialogueLine line = _lines[index];
            _text.text = line.Text;
            _text.ForceMeshUpdate();
            _totalCharacters = _text.textInfo.characterCount;
            _text.maxVisibleCharacters = 0;
            _elapsed = 0f;
            _startDelayLeft = 0f;
            _isVoiceStarted = false;
            _isTyping = true;
            _continueHint.SetActive(false);
            OnLineStarted?.Invoke(index);
        }

        private void FinishTyping()
        {
            _isTyping = false;
            _text.maxVisibleCharacters = _totalCharacters;
            _continueHint.SetActive(true);
        }

        /// <summary>Speech progress 0..1 at the given time; falls back to the fixed-speed progress without a timeline.</summary>
        private static float ComputeProgress(float[] timeline, float time, float fallbackProgress)
        {
            if (timeline == null || timeline.Length == 0)
            {
                return Mathf.Clamp01(fallbackProgress);
            }

            float index = time * TimelineRate;
            int i = Mathf.FloorToInt(index);
            if (i >= timeline.Length - 1)
            {
                return 1f;
            }

            return Mathf.Lerp(timeline[i], timeline[i + 1], index - i);
        }

        // Load-time only: turns each voice clip into a cumulative "how much has been spoken" curve.
        private void BuildSpeechTimelines()
        {
            _speechTimelines = new float[_lines.Length][];

            for (int line = 0; line < _lines.Length; line++)
            {
                AudioClip clip = _lines[line].Voice;
                if (clip == null || clip.loadType != AudioClipLoadType.DecompressOnLoad)
                {
                    continue;
                }

                // exact-size buffer: GetData warns when the array is longer than the clip
                var samples = new float[clip.samples * clip.channels];
                if (clip.GetData(samples, 0) == false)
                {
                    continue;
                }

                _speechTimelines[line] = ComputeSpeechTimeline(samples, clip.samples, clip.channels, clip.frequency);
            }
        }

        private static float[] ComputeSpeechTimeline(float[] samples, int frames, int channels, int frequency)
        {
            int frameSize = Mathf.Max(1, frequency / TimelineRate);
            int steps = Mathf.Max(1, frames / frameSize);
            var energy = new float[steps];
            float loudest = 0f;

            for (int s = 0; s < steps; s++)
            {
                double sum = 0;
                int start = s * frameSize * channels;
                int end = Mathf.Min(start + frameSize * channels, frames * channels);
                for (int i = start; i < end; i++)
                {
                    sum += samples[i] * samples[i];
                }

                energy[s] = (float)Math.Sqrt(sum / Mathf.Max(1, end - start));
                loudest = Mathf.Max(loudest, energy[s]);
            }

            // cumulative count of speaking frames, normalized to 0..1
            var timeline = new float[steps + 1];
            float threshold = loudest * SpeechThreshold;
            float spoken = 0f;
            for (int s = 0; s < steps; s++)
            {
                timeline[s] = spoken;
                if (energy[s] > threshold)
                {
                    spoken += 1f;
                }
            }

            timeline[steps] = spoken;
            float total = Mathf.Max(1f, spoken);
            for (int s = 0; s <= steps; s++)
            {
                timeline[s] /= total;
            }

            return timeline;
        }
    }
}
