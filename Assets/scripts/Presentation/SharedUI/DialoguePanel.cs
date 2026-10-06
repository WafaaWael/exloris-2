using System;
using System.Collections.Generic;
using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoatGame.Presentation.SharedUI
{
    /// <summary>
    /// Elder dialogue with a typewriter effect and optional voice-over per line.
    /// With a voice clip, letters follow the voice: they only advance while the elder is actually speaking
    /// (a speech timeline is extracted from each clip once and cached). Without a clip a fixed speed is used.
    /// Tap once to finish the current line, tap again for the next line. Auto-advance mode moves on by itself
    /// once the voice has finished, and can keep the last line on screen (e.g. a question waiting for an answer).
    /// Runs on unscaled time.
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
        private const float AutoAdvancePause = 0.7f;       // silence after a voiced line before the next one
        private const float ReadingSecondsPerChar = 0.05f; // hold time for lines without a voice clip

        [SerializeField] private TMP_Text _text;
        [SerializeField] private Button2 _tapArea;
        [SerializeField] private GameObject _continueHint;
        [SerializeField] private float _charactersPerSecond = 30f;
        [SerializeField] private DialogueLine[] _lines = Array.Empty<DialogueLine>();

        private readonly Dictionary<AudioClip, float[]> _timelineCache = new Dictionary<AudioClip, float[]>();
        private DialogueLine[] _activeLines = Array.Empty<DialogueLine>();
        private float[] _activeTimeline;
        private int _lineIndex;
        private float _elapsed;
        private int _totalCharacters;
        private bool _isTyping;
        private bool _isWaitingForAdvance;
        private bool _isVoiceStarted;
        private bool _isAutoAdvance;
        private bool _isLastLineKept;
        private float _startDelayLeft;
        private float _holdLeft;
        private double _voiceStartDsp;
        private bool _isPaused;
        private double _pauseStartDsp;

        public bool IsPlaying => _isTyping || _isWaitingForAdvance;

        /// <summary>Plays the lines set up in the inspector, tap to advance.</summary>
        public void Play()
        {
            PlayLines(_lines, false, false);
        }

        /// <summary>
        /// Plays the given lines. Auto-advance moves on once each voice line ends; with the last line kept,
        /// the panel stays visible on the final line and <see cref="OnFinished"/> fires when it has been spoken.
        /// </summary>
        public void PlayLines(DialogueLine[] lines, bool isAutoAdvance, bool isLastLineKept)
        {
            gameObject.SetActive(true);
            _activeLines = lines ?? Array.Empty<DialogueLine>();
            _isAutoAdvance = isAutoAdvance;
            _isLastLineKept = isLastLineKept;
            _lineIndex = 0;

            if (_activeLines.Length == 0)
            {
                Finish();
                return;
            }

            ShowLine(_lineIndex);
            _startDelayLeft = FirstLineDelay;
        }

        /// <summary>Stops the voice and hides the panel without raising <see cref="OnFinished"/>.</summary>
        public void Hide()
        {
            _isTyping = false;
            _isWaitingForAdvance = false;
            EventBus.Publish(new PlayVoiceEvent(null));
            gameObject.SetActive(false);
        }

        /// <summary>Extracts and caches speech timelines up front, so starting a line later never stalls a frame.</summary>
        public void PreloadTimelines(DialogueLine[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                GetTimeline(lines[i].Voice);
            }
        }

        public void OnButtonClick_Advance()
        {
            EventBus.Publish(new PlaySfxEvent(SfxId.UiTap));
            AdvanceDialogue();
        }

        private void Awake()
        {
            _tapArea.onClick.AddListener(OnButtonClick_Advance);
            PreloadTimelines(_lines);
            EventBus.Subscribe<GamePausedEvent>(GamePausedCallback);
        }

        private void OnDestroy()
        {
            _tapArea.onClick.RemoveListener(OnButtonClick_Advance);
            EventBus.Unsubscribe<GamePausedEvent>(GamePausedCallback);
        }

        // subscribed for the panel's whole life (it is often hidden while the game pauses)
        private void GamePausedCallback(GamePausedEvent evt)
        {
            if (evt.IsPaused == _isPaused)
            {
                return;
            }

            _isPaused = evt.IsPaused;
            if (_isPaused)
            {
                _pauseStartDsp = AudioSettings.dspTime;
            }
            else
            {
                // the voice was paused too, so shift its start time by the pause length to stay in sync
                _voiceStartDsp += AudioSettings.dspTime - _pauseStartDsp;
            }
        }

        private void Update()
        {
            // clamp: the first frames after a scene load report huge deltas, which would type the whole line at once
            if (_isPaused)
            {
                return;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxTypingDelta);

            if (_isTyping)
            {
                UpdateTyping(dt);
            }
            else if (_isWaitingForAdvance && _isAutoAdvance)
            {
                UpdateAutoAdvance(dt);
            }
        }

        private void UpdateTyping(float dt)
        {
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
                EventBus.Publish(new PlayVoiceEvent(_activeLines[_lineIndex].Voice));
            }

            // follow the audio clock when it runs ahead (frame hitch), frame time otherwise (audio muted/disabled)
            float dspElapsed = (float)(AudioSettings.dspTime - _voiceStartDsp);
            _elapsed = Mathf.Max(_elapsed + dt, dspElapsed);
            float progress = ComputeProgress(_activeTimeline, _elapsed + TypingLead, _elapsed * _charactersPerSecond / Mathf.Max(1, _totalCharacters));
            int visible = Mathf.Min(Mathf.CeilToInt(progress * _totalCharacters), _totalCharacters);
            _text.maxVisibleCharacters = visible;

            if (visible >= _totalCharacters)
            {
                FinishTyping();
            }
        }

        private void UpdateAutoAdvance(float dt)
        {
            _holdLeft -= dt;
            if (_holdLeft <= 0f)
            {
                AdvanceDialogue();
            }
        }

        private void AdvanceDialogue()
        {
            if (_isTyping)
            {
                FinishTyping();
                return;
            }

            if (_isWaitingForAdvance == false)
            {
                return;
            }

            bool isLast = _lineIndex + 1 >= _activeLines.Length;
            if (isLast && _isLastLineKept)
            {
                // the final line stays up (e.g. a question); the caller decides when to hide it
                _isWaitingForAdvance = false;
                _continueHint.SetActive(false);
                OnFinished?.Invoke();
                return;
            }

            _lineIndex++;
            if (_lineIndex < _activeLines.Length)
            {
                ShowLine(_lineIndex);
                return;
            }

            Finish();
        }

        private void Finish()
        {
            _isTyping = false;
            _isWaitingForAdvance = false;
            EventBus.Publish(new PlayVoiceEvent(null));
            gameObject.SetActive(false);
            OnFinished?.Invoke();
        }

        private void ShowLine(int index)
        {
            DialogueLine line = _activeLines[index];
            _activeTimeline = GetTimeline(line.Voice);
            _text.text = line.Text;
            _text.ForceMeshUpdate();
            _totalCharacters = _text.textInfo.characterCount;
            _text.maxVisibleCharacters = 0;
            _elapsed = 0f;
            _startDelayLeft = 0f;
            _isVoiceStarted = false;
            _isTyping = true;
            _isWaitingForAdvance = false;
            _continueHint.SetActive(false);
            OnLineStarted?.Invoke(index);
        }

        private void FinishTyping()
        {
            _isTyping = false;
            _isWaitingForAdvance = true;
            _text.maxVisibleCharacters = _totalCharacters;
            _continueHint.SetActive(_isAutoAdvance == false);
            _holdLeft = ComputeHoldSeconds(_activeLines[_lineIndex].Voice, _elapsed, _totalCharacters);
        }

        /// <summary>How long a finished line stays before auto-advancing: the rest of its voice clip plus a short pause.</summary>
        private static float ComputeHoldSeconds(AudioClip voice, float elapsed, int characters)
        {
            if (voice != null)
            {
                return Mathf.Max(0f, voice.length - elapsed) + AutoAdvancePause;
            }

            return 1f + characters * ReadingSecondsPerChar;
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

        // turns a voice clip into a cumulative "how much has been spoken" curve, once per clip
        private float[] GetTimeline(AudioClip clip)
        {
            if (clip == null)
            {
                return null;
            }

            if (_timelineCache.TryGetValue(clip, out float[] cached))
            {
                return cached;
            }

            float[] timeline = null;
            if (clip.loadType == AudioClipLoadType.DecompressOnLoad)
            {
                // exact-size buffer: GetData warns when the array is longer than the clip
                var samples = new float[clip.samples * clip.channels];
                if (clip.GetData(samples, 0))
                {
                    timeline = ComputeSpeechTimeline(samples, clip.samples, clip.channels, clip.frequency);
                }
            }

            _timelineCache[clip] = timeline;
            return timeline;
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
