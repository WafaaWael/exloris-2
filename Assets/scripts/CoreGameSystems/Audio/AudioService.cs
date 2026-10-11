using System;
using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using UnityEngine;
using UnityEngine.Audio;

namespace BoatGame.CoreGameSystems.Audio
{
    /// <summary>
    /// Plays music, ambience loops, one-shot SFX and voice lines. Driven entirely by events.
    /// Music fades in, crossfades to the victory tune, and is smoothly ducked (held, no pumping) while the elder speaks.
    /// All fades run on unscaled time so they keep working while the game is paused.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        [Serializable]
        public struct SfxEntry
        {
            public SfxId Id;
            public AudioClip[] Clips;
            [Range(0f, 1f)] public float Volume;
            [Range(0f, 0.3f)] public float PitchJitter;
        }

        private const float SilentDb = -80f;

        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _ambienceSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _voiceSource;
        [SerializeField] private AudioClip _music;
        [SerializeField] private AudioClip _victoryMusic;
        [SerializeField] private AudioClip _ambience;
        [SerializeField] private SfxEntry[] _sfx = Array.Empty<SfxEntry>();

        [Header("Player volume settings (mixer exposed params, base level at slider = 1)")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private float _musicBaseDb = -18f;
        [SerializeField] private float _ambienceBaseDb = -12f;
        [SerializeField] private float _sfxBaseDb = -7f;
        [SerializeField] private float _voiceBaseDb;

        [Header("Mixer snapshots")]
        [SerializeField] private AudioMixerSnapshot _gameplaySnapshot;
        [SerializeField] private AudioMixerSnapshot _pausedSnapshot;
        [SerializeField] private AudioMixerSnapshot _completeSnapshot;
        [SerializeField] private float _snapshotBlendSeconds = 0.5f;

        [Header("Fades & voice ducking")]
        [SerializeField] private float _musicFadeInSeconds = 1.5f;
        [SerializeField] private float _musicCrossfadeSeconds = 0.8f;
        [SerializeField] private float _voiceDuckMusicDb = -10f;
        [SerializeField] private float _voiceDuckAmbienceDb = -8f;
        [SerializeField] private float _duckAttackSeconds = 0.35f;
        [SerializeField] private float _duckReleaseSeconds = 0.8f;
        [SerializeField] private float _duckHoldSeconds = 0.6f;

        private SfxEntry[] _sfxById;
        private float _musicFade;            // 0..1 fade envelope of the current music clip
        private float _musicFadeTarget = 1f;
        private AudioClip _pendingMusic;     // waits for the fade-out before starting
        private float _duck;                 // 0 = no duck, 1 = fully ducked
        private float _duckHoldLeft;
        private bool _isStoryScreen;         // intro / arrival: keep the music down for the whole conversation
        private bool _isVoiceActive;
        private bool _isVoiceHeld;          // a line paused mid-way still counts as being spoken
        private bool _isPaused;
        private LevelState _levelState;

        private void Awake()
        {
            BuildLookup();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlaySfxEvent>(PlaySfxCallback);
            EventBus.Subscribe<PlayVoiceEvent>(PlayVoiceCallback);
            EventBus.Subscribe<LevelStateChangedEvent>(LevelStateChangedCallback);
            EventBus.Subscribe<AudioSettingsChangedEvent>(AudioSettingsChangedCallback);
            EventBus.Subscribe<GamePausedEvent>(GamePausedCallback);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlaySfxEvent>(PlaySfxCallback);
            EventBus.Unsubscribe<PlayVoiceEvent>(PlayVoiceCallback);
            EventBus.Unsubscribe<LevelStateChangedEvent>(LevelStateChangedCallback);
            EventBus.Unsubscribe<AudioSettingsChangedEvent>(AudioSettingsChangedCallback);
            EventBus.Unsubscribe<GamePausedEvent>(GamePausedCallback);
        }

        private void Start()
        {
            ApplyPlayerVolumes();
            if (_musicSource != null && !_musicSource.isPlaying)
            {
                StartLoop(_musicSource, _music);
            }

            if (_ambienceSource != null && !_ambienceSource.isPlaying)
            {
                StartLoop(_ambienceSource, _ambience);
            }

            _musicFade = 0f;
            _musicFadeTarget = 1f;
            ApplyVolumes();
        }

        /// <summary>
        /// Plays <paramref name="clip" as the active looping music track. A playing track is faded out before the
        /// new one starts, while the same track is simply resumed.
        /// </summary>
        public void SetLoopMusic(AudioClip clip)
        {
            if (_musicSource == null || clip == null)
            {
                return;
            }

            if (_musicSource.clip == clip)
            {
                _pendingMusic = null;
                _musicSource.loop = true;
                if (!_musicSource.isPlaying)
                {
                    _musicSource.Play();
                }

                _musicFadeTarget = 1f;
                return;
            }

            if (!_musicSource.isPlaying || _musicSource.clip == null)
            {
                _pendingMusic = null;
                StartLoop(_musicSource, clip);
                _musicFade = 0f;
                _musicFadeTarget = 1f;
                return;
            }

            CrossfadeMusic(clip);
        }

        /// <summary>Stops the current music track and cancels a queued music change.</summary>
        public void StopMusic()
        {
            _pendingMusic = null;
            _musicFade = 0f;
            _musicFadeTarget = 0f;
            _musicSource?.Stop();
        }

        /// <summary>True while the dedicated music source is playing.</summary>
        public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

        /// <summary>Plays a one-shot clip through the SFX mixer route.</summary>
        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        /// <summary>Plays a voice clip, replacing any voice line currently playing.</summary>
        public void PlayVoice(AudioClip clip, float volume = 1f)
        {
            if (_voiceSource == null)
            {
                return;
            }

            _voiceSource.Stop();
            _isVoiceHeld = false;
            if (clip == null)
            {
                return;
            }

            _voiceSource.clip = clip;
            _voiceSource.volume = Mathf.Clamp01(volume);
            _voiceSource.Play();
        }

        /// <summary>Mutes or unmutes all sources owned by this service.</summary>
        public void SetMuted(bool muted)
        {
            if (_musicSource != null) _musicSource.mute = muted;
            if (_ambienceSource != null) _ambienceSource.mute = muted;
            if (_sfxSource != null) _sfxSource.mute = muted;
            if (_voiceSource != null) _voiceSource.mute = muted;
        }

        /// <summary>Stops all currently playing clips and cancels a queued music change.</summary>
        public void StopAll()
        {
            StopMusic();
            _ambienceSource?.Stop();
            _sfxSource?.Stop();
            _voiceSource?.Stop();
            _isVoiceHeld = false;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateDuck(dt);
            UpdateMusicFade(dt);
            ApplyVolumes();
            UpdateVoiceActivity();
        }

        // whole-line state (not loudness): the speaker holds one talking pose for the line instead of flickering
        private void UpdateVoiceActivity()
        {
            bool isActive = _voiceSource != null && (_voiceSource.isPlaying || _isVoiceHeld);
            if (isActive != _isVoiceActive)
            {
                _isVoiceActive = isActive;
                EventBus.Publish(new VoiceActivityEvent(isActive));
            }
        }

        private void UpdateDuck(float dt)
        {
            bool isSpeaking = _voiceSource != null && _voiceSource.isPlaying;
            if (isSpeaking)
            {
                _duckHoldLeft = _duckHoldSeconds;
            }
            else
            {
                _duckHoldLeft -= dt;
            }

            bool isDucked = isSpeaking || _duckHoldLeft > 0f || _isStoryScreen;
            float rate = isDucked ? dt / _duckAttackSeconds : -dt / _duckReleaseSeconds;
            _duck = Mathf.Clamp01(_duck + rate);
        }

        private void UpdateMusicFade(float dt)
        {
            float seconds = _pendingMusic != null ? _musicCrossfadeSeconds : _musicFadeInSeconds;
            _musicFade = Mathf.MoveTowards(_musicFade, _musicFadeTarget, dt / Mathf.Max(0.01f, seconds));

            if (_pendingMusic != null && _musicFade <= 0f)
            {
                StartLoop(_musicSource, _pendingMusic);
                _pendingMusic = null;
                _musicFadeTarget = 1f;
            }
        }

        private void ApplyVolumes()
        {
            float duck = Mathf.SmoothStep(0f, 1f, _duck);
            if (_musicSource != null)
            {
                _musicSource.volume = ComputeFadeGain(_musicFade) * DbToGain(_voiceDuckMusicDb * duck);
            }

            if (_ambienceSource != null)
            {
                _ambienceSource.volume = DbToGain(_voiceDuckAmbienceDb * duck);
            }
        }

        private void ApplyPlayerVolumes()
        {
            if (_mixer == null)
            {
                return;
            }

            _mixer.SetFloat("MusicVolume", _musicBaseDb + SliderToDb(GameSettings.Music));
            _mixer.SetFloat("AmbienceVolume", _ambienceBaseDb + SliderToDb(GameSettings.Sounds));
            _mixer.SetFloat("SfxVolume", _sfxBaseDb + SliderToDb(GameSettings.Sounds));
            _mixer.SetFloat("VoiceVolume", _voiceBaseDb + SliderToDb(GameSettings.Voice));
        }

        /// <summary>Slider 0..1 to a dB offset: 1 = 0 dB, 0.5 ≈ -12 dB, 0 = silent. Squared curve feels even to the ear.</summary>
        private static float SliderToDb(float value)
        {
            return value <= 0.001f ? SilentDb : 20f * Mathf.Log10(value * value);
        }

        private static float ComputeFadeGain(float fade01)
        {
            // equal-power curve sounds even across the whole fade
            return Mathf.Sin(fade01 * Mathf.PI * 0.5f);
        }

        private static float DbToGain(float db)
        {
            return db <= SilentDb ? 0f : Mathf.Pow(10f, db / 20f);
        }

        private void BuildLookup()
        {
            int count = Enum.GetValues(typeof(SfxId)).Length;
            _sfxById = new SfxEntry[count];
            for (int i = 0; i < _sfx.Length; i++)
            {
                _sfxById[(int)_sfx[i].Id] = _sfx[i];
            }
        }

        private static void StartLoop(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.clip = clip;
            source.loop = true;
            source.Play();
        }

        private void CrossfadeMusic(AudioClip clip)
        {
            if (clip == null || clip == _musicSource.clip)
            {
                return;
            }

            _pendingMusic = clip;
            _musicFadeTarget = 0f;
        }

        private void PlaySfxCallback(PlaySfxEvent evt)
        {
            SfxEntry entry = _sfxById[(int)evt.Id];
            if (entry.Clips == null || entry.Clips.Length == 0 || _sfxSource == null)
            {
                return;
            }

            AudioClip clip = entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Length)];
            _sfxSource.pitch = 1f + UnityEngine.Random.Range(-entry.PitchJitter, entry.PitchJitter);
            _sfxSource.PlayOneShot(clip, entry.Volume <= 0f ? 1f : entry.Volume);
        }

        private void LevelStateChangedCallback(LevelStateChangedEvent evt)
        {
            _levelState = evt.Current;
            _isStoryScreen = evt.Current == LevelState.Intro || evt.Current == LevelState.Arrived || evt.Current == LevelState.Lesson;

            AudioMixerSnapshot snapshot = SelectSnapshot(evt.Current);
            if (snapshot != null && _isPaused == false)
            {
                // the mixer runs in unscaled-time mode, so this blend also works while paused
                snapshot.TransitionTo(_snapshotBlendSeconds);
            }

            if (evt.Current == LevelState.Arrived || evt.Current == LevelState.Complete)
            {
                CrossfadeMusic(_victoryMusic);
            }
        }

        private AudioMixerSnapshot SelectSnapshot(LevelState state)
        {
            switch (state)
            {
                case LevelState.Paused:
                case LevelState.Tutorial:
                    return _pausedSnapshot;
                case LevelState.Arrived:
                case LevelState.Complete:
                    return _completeSnapshot;
                default:
                    return _gameplaySnapshot;
            }
        }

        private void GamePausedCallback(GamePausedEvent evt)
        {
            _isPaused = evt.IsPaused;
            AudioMixerSnapshot snapshot = _isPaused ? _pausedSnapshot : SelectSnapshot(_levelState);
            if (snapshot != null)
            {
                snapshot.TransitionTo(_snapshotBlendSeconds);
            }

            if (_voiceSource == null)
            {
                return;
            }

            if (_isPaused)
            {
                _isVoiceHeld = _voiceSource.isPlaying;
                _voiceSource.Pause();
            }
            else
            {
                _isVoiceHeld = false;
                _voiceSource.UnPause();
            }
        }

        private void AudioSettingsChangedCallback(AudioSettingsChangedEvent evt)
        {
            ApplyPlayerVolumes();
        }

        private void PlayVoiceCallback(PlayVoiceEvent evt)
        {
            PlayVoice(evt.Clip);
        }
    }
}
