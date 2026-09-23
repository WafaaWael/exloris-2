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

        private void Awake()
        {
            BuildLookup();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlaySfxEvent>(PlaySfxCallback);
            EventBus.Subscribe<PlayVoiceEvent>(PlayVoiceCallback);
            EventBus.Subscribe<LevelStateChangedEvent>(LevelStateChangedCallback);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlaySfxEvent>(PlaySfxCallback);
            EventBus.Unsubscribe<PlayVoiceEvent>(PlayVoiceCallback);
            EventBus.Unsubscribe<LevelStateChangedEvent>(LevelStateChangedCallback);
        }

        private void Start()
        {
            StartLoop(_musicSource, _music);
            StartLoop(_ambienceSource, _ambience);
            _musicFade = 0f;
            _musicFadeTarget = 1f;
            ApplyVolumes();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateDuck(dt);
            UpdateMusicFade(dt);
            ApplyVolumes();
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
            _isStoryScreen = evt.Current == LevelState.Intro || evt.Current == LevelState.Arrived;

            AudioMixerSnapshot snapshot = SelectSnapshot(evt.Current);
            if (snapshot != null)
            {
                // the mixer runs in unscaled-time mode, so this blend also works while paused
                snapshot.TransitionTo(_snapshotBlendSeconds);
            }

            if (evt.Current == LevelState.Arrived)
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

        private void PlayVoiceCallback(PlayVoiceEvent evt)
        {
            if (_voiceSource == null)
            {
                return;
            }

            _voiceSource.Stop();
            if (evt.Clip == null)
            {
                return;
            }

            _voiceSource.clip = evt.Clip;
            _voiceSource.Play();
        }
    }
}
