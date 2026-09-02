using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using UnityEngine;

namespace Exloris.Runtime.Stage3
{
    public enum Stage3MediaMode { Intro, Help, Outcome, OutcomeReady }

    public readonly struct Stage3MediaHandle
    {
        public Stage3MediaHandle(Guid routeTokenId, Guid mediaTokenId, Guid? voiceTokenId,
            Guid? musicTokenId, Guid? ambienceTokenId)
        {
            RouteTokenId = routeTokenId; MediaTokenId = mediaTokenId;
            VoiceTokenId = voiceTokenId; MusicTokenId = musicTokenId; AmbienceTokenId = ambienceTokenId;
        }
        public Guid RouteTokenId { get; }
        public Guid MediaTokenId { get; }
        public Guid? VoiceTokenId { get; }
        public Guid? MusicTokenId { get; }
        public Guid? AmbienceTokenId { get; }
    }

    public sealed class Stage3MediaPresenter
    {
        public const string IntroVoiceId = "VO-S3-INTRO";
        public const string OutcomeVoiceId = "VO-S3-OUTCOME";
        public const string MusicId = "MUS-S3";
        public const string AmbienceId = "AMB-VILLAGE";
        public const string IntroCaption1Id = "VO-S3-INTRO-C01";
        public const string IntroCaption2Id = "VO-S3-INTRO-C02";
        public const string OutcomeCaption1Id = "VO-S3-OUTCOME-C01";
        public const string OutcomeCaption2Id = "VO-S3-OUTCOME-C02";
        public const double IntroDurationSeconds = 8.032d;
        public const double OutcomeDurationSeconds = 6.735d;
        private const double ClockEpsilon = 0.000000001d;

        private readonly AudioDirector _director;
        private readonly Stage3Content _content;
        private readonly SessionState _session;
        private readonly AccessibilityState _accessibility;
        private readonly IStage3MediaCueSink _sink;
        private readonly Func<Guid> _idFactory;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();
        private Guid _routeTokenId;
        private Guid _mediaTokenId;
        private AudioPlaybackToken _voice;
        private AudioPlaybackToken _music;
        private AudioPlaybackToken _ambience;
        private Stage3MediaMode _mode;
        private Stage3Plan _plan;
        private double _startedAt;
        private double _lastAdvancedAt;
        private double _elapsed;
        private double _pausedAt;
        private bool _active;
        private bool _paused;
        private bool _completed;

        public Stage3MediaPresenter(AudioDirector director, IAudioClipRegistry registry,
            Stage3Content content, SessionState session, AccessibilityState accessibility,
            IStage3MediaCueSink sink, Func<Guid> idFactory = null)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _idFactory = idFactory ?? Guid.NewGuid;
            Register(registry, IntroVoiceId, content.VoiceClips[0], false);
            Register(registry, OutcomeVoiceId, content.VoiceClips[1], false);
            Register(registry, MusicId, content.Music, true);
            Register(registry, AmbienceId, content.Ambience, true);
        }

        public double ElapsedSeconds => _elapsed;
        public bool HasActiveVoice => _voice != null;
        public bool HasActiveLoops => _music != null || _ambience != null;
        public bool IsPaused => _paused;
        public Guid CurrentMediaTokenId => _mediaTokenId;
        public bool CanPause(Guid routeTokenId, Guid mediaTokenId, double now) =>
            Current(routeTokenId, mediaTokenId) && !_paused && !_completed && Finite(now)
            && now >= _lastAdvancedAt - ClockEpsilon;
        public bool CanResumeAfterMappedUserAction(Guid routeTokenId, double now) =>
            _active && _paused && routeTokenId == _routeTokenId && Finite(now) && now >= _pausedAt;

        public Stage3MediaHandle StartIntro(Guid routeTokenId, double now) =>
            Start(routeTokenId, Stage3MediaMode.Intro, Stage3Plan.None, now);
        public Stage3MediaHandle StartHelp(Guid routeTokenId, double now) =>
            Start(routeTokenId, Stage3MediaMode.Help, Stage3Plan.None, now);
        public Stage3MediaHandle StartOutcome(Guid routeTokenId, Stage3Plan plan, double now)
        {
            if (!IsPlan(plan)) throw new ArgumentOutOfRangeException(nameof(plan));
            return Start(routeTokenId, Stage3MediaMode.Outcome, plan, now);
        }
        public Stage3MediaHandle StartOutcomeReady(Guid routeTokenId, Stage3Plan plan, double now)
        {
            if (!IsPlan(plan)) throw new ArgumentOutOfRangeException(nameof(plan));
            return Start(routeTokenId, Stage3MediaMode.OutcomeReady, plan, now);
        }

        public Stage3MediaHandle Repeat(Guid routeTokenId, double now)
        {
            if (!_active || _paused || routeTokenId != _routeTokenId)
                throw new InvalidOperationException("Cannot repeat stale Stage 3 media.");
            return Start(routeTokenId, _mode, _plan, now);
        }

        public bool Advance(Guid routeTokenId, Guid mediaTokenId, double now)
        {
            if (!Current(routeTokenId, mediaTokenId) || _paused || _completed || !Finite(now)
                || now <= _lastAdvancedAt + ClockEpsilon || now < _startedAt) return false;
            _lastAdvancedAt = now;
            var next = Math.Min(Duration(_mode), now - _startedAt);
            if (next < _elapsed - ClockEpsilon) return false;
            _elapsed = next;
            if (_elapsed >= Duration(_mode) - ClockEpsilon) { _elapsed = Duration(_mode); StopVoice(); _completed = true; }
            Present();
            return true;
        }

        public bool Pause(Guid routeTokenId, Guid mediaTokenId, double now)
        {
            if (!Current(routeTokenId, mediaTokenId) || _paused || _completed || !Finite(now)
                || now < _lastAdvancedAt - ClockEpsilon) return false;
            if (now > _lastAdvancedAt + ClockEpsilon && !Advance(routeTokenId, mediaTokenId, now)) return false;
            _paused = true; _pausedAt = now; _director.PauseAndDuckImmediately(); return true;
        }

        public Stage3MediaHandle? ResumeAfterMappedUserAction(Guid routeTokenId, double now)
        {
            if (!_active || !_paused || routeTokenId != _routeTokenId || !Finite(now) || now < _pausedAt) return null;
            _mediaTokenId = NextTokenId(); _startedAt = now - _elapsed; _lastAdvancedAt = now;
            _director.ResumeAfterMappedUserAction(); _paused = false; Present(); return Handle();
        }

        public void RefreshSettings(double now)
        {
            if (!_active || !Finite(now)) return;
            if (!_session.SoundEnabled) { StopVoice(); StopLoops(); }
            else if (!_paused)
            {
                EnsureLoops(now);
                if (_mode != Stage3MediaMode.OutcomeReady
                    && _voice == null && !_completed && _elapsed < Duration(_mode)) StartVoice(now);
            }
            Present();
        }

        public bool Stop(Guid routeTokenId)
        {
            if (!_active || routeTokenId != _routeTokenId) return false;
            StopVoice(); StopLoops(); _active = false; _paused = false; _completed = false;
            _routeTokenId = Guid.Empty; _mediaTokenId = Guid.Empty; return true;
        }

        private Stage3MediaHandle Start(Guid routeTokenId, Stage3MediaMode mode, Stage3Plan plan, double now)
        {
            if (routeTokenId == Guid.Empty || !Finite(now)) throw new ArgumentOutOfRangeException();
            if (_paused) throw new InvalidOperationException("Paused Stage 3 media must resume or stop before replacement.");
            if (_active && routeTokenId != _routeTokenId) { StopVoice(); StopLoops(); }
            else StopVoice();
            _routeTokenId = routeTokenId; _mode = mode; _plan = plan; _mediaTokenId = NextTokenId();
            _startedAt = now; _lastAdvancedAt = now; _elapsed = 0; _active = true; _paused = false; _completed = false;
            if (_session.SoundEnabled)
            {
                EnsureLoops(now);
                if (mode != Stage3MediaMode.OutcomeReady) StartVoice(now);
            }
            else StopLoops();
            Present(); return Handle();
        }

        private void StartVoice(double now) => _voice = _director.TryStart(AudioBus.Voice,
            _mode == Stage3MediaMode.Outcome ? OutcomeVoiceId : IntroVoiceId, now);
        private void EnsureLoops(double now)
        {
            if (_music == null && !_director.TryGetActive(AudioBus.Music, MusicId, out _music))
                _music = _director.TryStart(AudioBus.Music, MusicId, now);
            if (_ambience == null && !_director.TryGetActive(AudioBus.Ambience, AmbienceId, out _ambience))
                _ambience = _director.TryStart(AudioBus.Ambience, AmbienceId, now);
        }
        private void StopVoice() { if (_voice != null) _director.Complete(_voice.Id); _voice = null; }
        private void StopLoops()
        {
            if (_music != null) _director.Complete(_music.Id);
            if (_ambience != null) _director.Complete(_ambience.Id);
            _music = null; _ambience = null;
        }

        private void Present()
        {
            ResolveCue(_mode, _elapsed, out var captionId, out var cueStart, out var visible);
            var kind = _mode == Stage3MediaMode.Outcome ? Stage3MediaCueKind.Outcome
                : _mode == Stage3MediaMode.OutcomeReady ? Stage3MediaCueKind.None : Stage3MediaCueKind.Intro;
            var visualIndex = _mode == Stage3MediaMode.Outcome || _mode == Stage3MediaMode.OutcomeReady ? (int)_plan : 0;
            _sink.Present(new Stage3MediaCue(_routeTokenId, _mediaTokenId, kind, _plan,
                _content.VisualEquivalents[visualIndex], captionId,
                _accessibility.CaptionsEnabled && visible, _startedAt + cueStart, _elapsed));
        }

        private Stage3MediaHandle Handle() => new Stage3MediaHandle(
            _routeTokenId, _mediaTokenId, _voice?.Id, _music?.Id, _ambience?.Id);
        private bool Current(Guid routeTokenId, Guid mediaTokenId) => _active
            && routeTokenId == _routeTokenId && mediaTokenId == _mediaTokenId;
        private Guid NextTokenId()
        {
            var token = _idFactory();
            if (token == Guid.Empty || !_issuedTokens.Add(token))
                throw new InvalidOperationException("Stage 3 media token IDs must be non-empty and unique.");
            return token;
        }
        private static double Duration(Stage3MediaMode mode) => mode == Stage3MediaMode.Outcome
            ? OutcomeDurationSeconds : mode == Stage3MediaMode.OutcomeReady ? double.MaxValue : IntroDurationSeconds;
        private static void ResolveCue(Stage3MediaMode mode, double elapsed,
            out string id, out double start, out bool visible)
        {
            id = null; start = elapsed; visible = false;
            if (mode == Stage3MediaMode.OutcomeReady) return;
            if (mode == Stage3MediaMode.Outcome)
            {
                if (At(elapsed, 0) && Before(elapsed, 2.850)) { id = OutcomeCaption1Id; start = 0; visible = true; }
                else if (At(elapsed, 3.000) && Before(elapsed, OutcomeDurationSeconds + ClockEpsilon))
                { id = OutcomeCaption2Id; start = 3.000; visible = true; }
            }
            else
            {
                if (At(elapsed, 0) && Before(elapsed, 3.772)) { id = IntroCaption1Id; start = 0; visible = true; }
                else if (At(elapsed, 3.922) && Before(elapsed, IntroDurationSeconds + ClockEpsilon))
                { id = IntroCaption2Id; start = 3.922; visible = true; }
            }
        }
        private static bool At(double value, double boundary) => value >= boundary - ClockEpsilon;
        private static bool Before(double value, double boundary) => value < boundary - ClockEpsilon;
        private static bool IsPlan(Stage3Plan plan) => plan >= Stage3Plan.Grain && plan <= Stage3Plan.Boat;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Register(IAudioClipRegistry registry, string semanticId, AudioClip clip, bool loop)
        {
            if (!(registry is IAudioClipRegistryLookup lookup) || !lookup.IsClipRegistered(semanticId))
                registry.RegisterClip(semanticId, clip, loop);
        }
    }

    public static class Stage3CaptionStrings
    {
        private static readonly IReadOnlyDictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [Stage3MediaPresenter.IntroCaption1Id] = "Choose one way to help the village.",
                [Stage3MediaPresenter.IntroCaption2Id] = "Every plan shows something different.",
                [Stage3MediaPresenter.OutcomeCaption1Id] = "This helped in one way.",
                [Stage3MediaPresenter.OutcomeCaption2Id] = "The village still needs something too.",
            };

        public static string ResolveEnglish(string captionId) => string.IsNullOrEmpty(captionId)
            ? string.Empty : English.TryGetValue(captionId, out var value)
                ? value : throw new KeyNotFoundException(captionId);
    }
}
