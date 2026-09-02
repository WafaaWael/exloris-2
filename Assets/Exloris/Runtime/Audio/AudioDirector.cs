using System;
using System.Collections.Generic;
using Exloris.Runtime.Lifecycle;
using UnityEngine;

namespace Exloris.Runtime.Audio
{
    public static class AudioPolicy
    {
        public const float DefaultMasterVolume = 0.60f;
        public const int MaximumConcurrentVoices = 16;
        public const float AssembledIntegratedLufs = -23f;
        public const float MaximumShortTermLufs = -18f;
        public const float MaximumTruePeakDbtp = -1f;
        public const float VoiceTargetLufs = -23f;
        public const float MusicTargetLufs = -29f;
        public const float AmbienceTargetLufs = -35f;
        public const float SfxMaximumShortTermLufs = -20f;
        public const float CaptionToleranceSeconds = 0.100f;
        public const float InteractiveOnsetP95Seconds = 0.150f;

        public static float CalculateGainRiseSeconds(float gainDeltaLu)
        {
            return Mathf.Max(0.250f, Mathf.Abs(gainDeltaLu) * 0.080f);
        }

        public static bool PassesInteractiveOnsetP95(IReadOnlyList<double> onsetSeconds)
        {
            if (onsetSeconds == null || onsetSeconds.Count == 0)
            {
                throw new ArgumentException("At least one onset measurement is required.", nameof(onsetSeconds));
            }

            var sorted = new double[onsetSeconds.Count];
            for (var i = 0; i < onsetSeconds.Count; i++)
            {
                sorted[i] = onsetSeconds[i];
            }

            Array.Sort(sorted);
            var rank = Mathf.CeilToInt(0.95f * sorted.Length) - 1;
            return sorted[rank] <= InteractiveOnsetP95Seconds;
        }
    }

    public readonly struct AudioDuckState
    {
        public AudioDuckState(float musicDb, float ambienceDb, float sfxDb, float rampSeconds)
        {
            MusicDb = musicDb;
            AmbienceDb = ambienceDb;
            SfxDb = sfxDb;
            RampSeconds = rampSeconds;
        }

        public float MusicDb { get; }
        public float AmbienceDb { get; }
        public float SfxDb { get; }
        public float RampSeconds { get; }
    }

    public interface IAudioBackend
    {
        void Start(AudioPlaybackToken token, AudioBus bus);
        void Stop(Guid tokenId);
        void PauseAllImmediately();
        void SeekAll(double timeSeconds);
        void ResumeAll(float targetMasterVolume, float rampSeconds);
        void SetMasterVolume(float linearVolume);
        void ApplyDuck(AudioDuckState state);
    }

    public sealed class AudioDirector : IAppLifecycleParticipant
    {
        private static readonly AudioDuckState VoiceDuck =
            new AudioDuckState(-6f, -3f, -3f, 0.100f);
        private static readonly AudioDuckState ReleasedDuck =
            new AudioDuckState(0f, 0f, 0f, 0.600f);

        private readonly IAudioBackend _backend;
        private readonly Func<Guid> _idFactory;
        private readonly Dictionary<Guid, AudioPlaybackToken> _active =
            new Dictionary<Guid, AudioPlaybackToken>();
        private readonly HashSet<Guid> _issued = new HashSet<Guid>();
        private int _activeVoiceCount;
        private bool _paused;

        public AudioDirector(IAudioBackend backend, Func<Guid> idFactory = null)
        {
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _idFactory = idFactory ?? Guid.NewGuid;
            MasterVolume = AudioPolicy.DefaultMasterVolume;
            _backend.SetMasterVolume(MasterVolume);
        }

        public float MasterVolume { get; private set; }
        public int ActiveVoiceCount => _activeVoiceCount;

        public bool TryGetActive(AudioBus bus, string semanticId, out AudioPlaybackToken token)
        {
            foreach (var item in _active.Values)
            {
                if (item.Bus == bus
                    && string.Equals(item.SemanticId, semanticId, StringComparison.Ordinal))
                {
                    token = item;
                    return true;
                }
            }

            token = null;
            return false;
        }

        public AudioPlaybackToken TryStart(AudioBus bus, string semanticId, double nowSeconds)
        {
            if (_active.Count >= AudioPolicy.MaximumConcurrentVoices || _paused)
            {
                return null;
            }

            var token = new AudioPlaybackToken(NextId(), semanticId, bus, nowSeconds);
            _active.Add(token.Id, token);
            if (bus == AudioBus.Voice)
            {
                _activeVoiceCount++;
                if (_activeVoiceCount == 1)
                {
                    _backend.ApplyDuck(VoiceDuck);
                }
            }

            _backend.Start(token, bus);
            return token;
        }

        public bool Complete(Guid tokenId)
        {
            if (!_active.TryGetValue(tokenId, out var token))
            {
                return false;
            }

            _active.Remove(tokenId);
            _backend.Stop(tokenId);
            if (token.Bus == AudioBus.Voice)
            {
                _activeVoiceCount--;
                if (_activeVoiceCount == 0)
                {
                    _backend.ApplyDuck(ReleasedDuck);
                }
            }

            return true;
        }

        public void SetUserMasterVolume(float linearVolume)
        {
            MasterVolume = Mathf.Clamp01(linearVolume);
            _backend.SetMasterVolume(MasterVolume);
        }

        public void PauseAndDuckImmediately()
        {
            _paused = true;
            _backend.PauseAllImmediately();
        }

        public void ResumeAfterMappedUserAction()
        {
            if (!_paused)
            {
                return;
            }

            _paused = false;
            _backend.ResumeAll(
                MasterVolume,
                AudioPolicy.CalculateGainRiseSeconds(6f));
        }

        private Guid NextId()
        {
            var id = _idFactory();
            if (id == Guid.Empty || !_issued.Add(id))
            {
                throw new InvalidOperationException("Audio playback IDs must be non-empty and unique.");
            }

            return id;
        }
    }
}
