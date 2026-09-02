using System;
using System.Collections.Generic;
using Exloris.Runtime.Media;
using UnityEngine;

namespace Exloris.Runtime.Audio
{
    public interface IAudioClipRegistry
    {
        void RegisterClip(string semanticId, AudioClip clip, bool loop = false);
    }

    public interface IAudioClipRegistryLookup
    {
        bool IsClipRegistered(string semanticId);
    }

    public sealed class UnityAudioBackend :
        IAudioBackend,
        IAudioClipRegistry,
        IAudioClipRegistryLookup,
        IMediaPlaybackClock,
        ISynchronizedMediaTrack,
        IPlaybackTokenTrack,
        IDisposable
    {
        private sealed class Channel
        {
            public Channel(AudioBus bus, GameObject owner, AudioSource source)
            {
                Bus = bus;
                Owner = owner;
                Source = source;
            }

            public AudioBus Bus { get; }
            public GameObject Owner { get; }
            public AudioSource Source { get; }
        }

        private readonly struct RegisteredClip
        {
            public RegisteredClip(AudioClip clip, bool loop)
            {
                Clip = clip;
                Loop = loop;
            }

            public AudioClip Clip { get; }
            public bool Loop { get; }
        }

        private const float SilenceDb = -80f;
        private const float ResumeGainDeltaDb = 6f;

        private readonly GameObject _owner;
        private readonly AudioClip _probe;
        private readonly Dictionary<Guid, Channel> _channels = new Dictionary<Guid, Channel>();
        private readonly Dictionary<string, RegisteredClip> _registeredClips =
            new Dictionary<string, RegisteredClip>(StringComparer.Ordinal);
        private readonly Dictionary<AudioBus, DbGainRamp> _busRamps =
            new Dictionary<AudioBus, DbGainRamp>();
        private readonly DbGainRamp _masterRamp = new DbGainRamp();
        private Guid _boundMediaToken;
        private bool _paused;

        public UnityAudioBackend(GameObject owner)
        {
            _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
            foreach (AudioBus bus in Enum.GetValues(typeof(AudioBus)))
            {
                var ramp = new DbGainRamp();
                ramp.SetImmediate(0f);
                _busRamps.Add(bus, ramp);
            }

            _masterRamp.SetImmediate(LinearToDb(AudioPolicy.DefaultMasterVolume));
            const int sampleRate = 48000;
            var samples = new float[sampleRate / 8];
            for (var index = 0; index < samples.Length; index++)
            {
                var envelope = 1f - index / (float)samples.Length;
                samples[index] = Mathf.Sin(2f * Mathf.PI * 440f * index / sampleRate) * envelope * 0.08f;
            }

            _probe = AudioClip.Create("FoundationDiagnosticTone", samples.Length, 1, sampleRate, false);
            _probe.SetData(samples, 0);
        }

        public float MasterGain => DbToLinear(_masterRamp.CurrentDb);
        public bool MasterRampActive => _masterRamp.Active;
        public float MasterRampDurationSeconds => _masterRamp.DurationSeconds;
        public bool IsPaused => _paused;

        public bool IsClipRegistered(string semanticId)
        {
            return !string.IsNullOrWhiteSpace(semanticId)
                && _registeredClips.ContainsKey(semanticId);
        }

        public void RegisterClip(string semanticId, AudioClip clip, bool loop = false)
        {
            if (string.IsNullOrWhiteSpace(semanticId)) throw new ArgumentException("Semantic audio ID is required.", nameof(semanticId));
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (_registeredClips.TryGetValue(semanticId, out var existing))
            {
                if (existing.Clip == clip && existing.Loop == loop) return;
                throw new InvalidOperationException("Semantic audio ID is already registered with different content: " + semanticId);
            }

            _registeredClips.Add(semanticId, new RegisteredClip(clip, loop));
        }

        public void Start(AudioPlaybackToken token, AudioBus bus)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            if (token.Bus != bus) throw new ArgumentException("Playback token bus must match its routed bus.", nameof(bus));
            if (_channels.ContainsKey(token.Id)) throw new InvalidOperationException("Playback token is already active.");

            var channelOwner = new GameObject("AudioChannel-" + bus + "-" + token.Id.ToString("N"));
            channelOwner.transform.SetParent(_owner.transform, false);
            var source = channelOwner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            if (_registeredClips.TryGetValue(token.SemanticId, out var registered))
            {
                source.clip = registered.Clip;
                source.loop = registered.Loop;
            }
            else
            {
                source.clip = _probe;
                source.loop = false;
            }
            source.volume = EffectiveVolume(bus);
            _channels.Add(token.Id, new Channel(bus, channelOwner, source));
            source.Play();
            if (_paused) source.Pause();
        }

        public void Stop(Guid tokenId)
        {
            if (!_channels.TryGetValue(tokenId, out var channel)) return;
            channel.Source.Stop();
            _channels.Remove(tokenId);
            DestroyOwned(channel.Owner);
        }

        public void PauseAllImmediately()
        {
            _paused = true;
            foreach (var channel in _channels.Values) channel.Source.Pause();
        }

        public void SeekAll(double timeSeconds)
        {
            foreach (var channel in _channels.Values)
            {
                channel.Source.time = Mathf.Clamp((float)timeSeconds, 0f, channel.Source.clip.length);
            }
        }

        public void ResumeAll(float targetMasterVolume, float rampSeconds)
        {
            var targetDb = LinearToDb(Mathf.Clamp01(targetMasterVolume));
            _masterRamp.SetImmediate(Mathf.Max(SilenceDb, targetDb - ResumeGainDeltaDb));
            _masterRamp.Start(targetDb, rampSeconds);
            _paused = false;
            foreach (var channel in _channels.Values) channel.Source.UnPause();
            ApplyVolumes();
        }

        public void SetMasterVolume(float linearVolume)
        {
            _masterRamp.SetImmediate(LinearToDb(Mathf.Clamp01(linearVolume)));
            ApplyVolumes();
        }

        public void ApplyDuck(AudioDuckState state)
        {
            _busRamps[AudioBus.Music].Start(state.MusicDb, state.RampSeconds);
            _busRamps[AudioBus.Ambience].Start(state.AmbienceDb, state.RampSeconds);
            _busRamps[AudioBus.Sfx].Start(state.SfxDb, state.RampSeconds);
        }

        public void Advance(float unscaledDeltaTime)
        {
            _masterRamp.Advance(unscaledDeltaTime);
            foreach (var ramp in _busRamps.Values) ramp.Advance(unscaledDeltaTime);
            ApplyVolumes();
        }

        public float GetBusDuckDb(AudioBus bus) => _busRamps[bus].CurrentDb;

        public float GetTokenVolume(Guid tokenId)
        {
            if (!_channels.TryGetValue(tokenId, out var channel))
            {
                throw new KeyNotFoundException("Audio token is not active: " + tokenId);
            }

            return channel.Source.volume;
        }

        public AudioClip GetTokenClip(Guid tokenId)
        {
            if (!_channels.TryGetValue(tokenId, out var channel))
            {
                throw new KeyNotFoundException("Audio token is not active: " + tokenId);
            }

            return channel.Source.clip;
        }

        public bool GetTokenLoop(Guid tokenId)
        {
            if (!_channels.TryGetValue(tokenId, out var channel))
            {
                throw new KeyNotFoundException("Audio token is not active: " + tokenId);
            }

            return channel.Source.loop;
        }

        public double TimeSeconds
        {
            get
            {
                foreach (var channel in _channels.Values) return channel.Source.time;
                return 0d;
            }
        }

        public void Pause() => PauseAllImmediately();

        public void Resume()
        {
            // Preparation only. AudioDirector commits the synchronized unpause and gain ramp.
        }

        public void Seek(double timeSeconds) => SeekAll(timeSeconds);

        public void Resume(float gainRampSeconds)
        {
            // Preparation only. AudioDirector commits after every synchronized track has sought.
        }

        public void Bind(Guid tokenId)
        {
            if (tokenId == Guid.Empty) throw new ArgumentException("Media token cannot be empty.", nameof(tokenId));
            _boundMediaToken = tokenId;
        }

        public void Dispose()
        {
            foreach (var channel in _channels.Values) DestroyOwned(channel.Owner);
            _channels.Clear();
            if (_probe != null) DestroyOwned(_probe);
        }

        private float EffectiveVolume(AudioBus bus)
        {
            return MasterGain * DbToLinear(_busRamps[bus].CurrentDb);
        }

        private void ApplyVolumes()
        {
            foreach (var channel in _channels.Values)
            {
                channel.Source.volume = EffectiveVolume(channel.Bus);
            }
        }

        private static float LinearToDb(float linear)
        {
            return linear <= 0f ? SilenceDb : 20f * Mathf.Log10(linear);
        }

        private static float DbToLinear(float db)
        {
            return db <= SilenceDb ? 0f : Mathf.Pow(10f, db / 20f);
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
