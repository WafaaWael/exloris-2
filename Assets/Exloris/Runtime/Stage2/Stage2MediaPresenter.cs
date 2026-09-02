using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using UnityEngine;

namespace Exloris.Runtime.Stage2
{
    public enum Stage2MediaMode { Intro, Help }

    public readonly struct Stage2MediaCue
    {
        public Stage2MediaCue(Guid routeTokenId, Guid mediaTokenId, Stage2MediaMode mode,
            Sprite visualEquivalent, string captionId, bool captionVisible,
            double captionCueStartSeconds, double elapsedSeconds)
        {
            RouteTokenId = routeTokenId;
            MediaTokenId = mediaTokenId;
            Mode = mode;
            VisualEquivalent = visualEquivalent;
            CaptionId = captionId;
            CaptionVisible = captionVisible;
            CaptionCueStartSeconds = captionCueStartSeconds;
            ElapsedSeconds = elapsedSeconds;
        }

        public Guid RouteTokenId { get; }
        public Guid MediaTokenId { get; }
        public Stage2MediaMode Mode { get; }
        public Sprite VisualEquivalent { get; }
        public string CaptionId { get; }
        public bool CaptionVisible { get; }
        public double CaptionCueStartSeconds { get; }
        public double ElapsedSeconds { get; }
        public bool RequiresReading => false;
        public bool UsesEvaluationLanguage => false;
        public bool UsesSuccessOrFailureAudio => false;
    }

    public readonly struct Stage2MediaHandle
    {
        public Stage2MediaHandle(Guid routeTokenId, Guid mediaTokenId, Guid? voiceTokenId,
            Guid? musicTokenId, Guid? ambienceTokenId)
        {
            RouteTokenId = routeTokenId;
            MediaTokenId = mediaTokenId;
            VoiceTokenId = voiceTokenId;
            MusicTokenId = musicTokenId;
            AmbienceTokenId = ambienceTokenId;
        }
        public Guid RouteTokenId { get; }
        public Guid MediaTokenId { get; }
        public Guid? VoiceTokenId { get; }
        public Guid? MusicTokenId { get; }
        public Guid? AmbienceTokenId { get; }
    }

    public interface IStage2MediaCueSink
    {
        void PresentMediaCue(Stage2MediaCue cue);
        void FreezeMediaCue(double elapsedSeconds);
        void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds);
        void SetCallerInputLocked(bool locked);
        void ShowPauseOverlay(Stage2PauseReason reason);
    }

    public sealed class Stage2MediaPresenter
    {
        public const string IntroVoiceId = "VO-S2-INTRO";
        public const string HelpVoiceId = "VO-S2-HELP";
        public const string MusicId = "MUS-STAGE2-LOOP";
        public const string AmbienceId = "AMB-RIVER-LOOP";
        public const string LowStoneSfxId = "SFX-STONE-SELECT-LOW";
        public const string MiddleStoneSfxId = "SFX-STONE-SELECT-MIDDLE";
        public const string HighStoneSfxId = "SFX-STONE-SELECT-HIGH";
        public const string IntroCaption1Id = "VO-S2-INTRO-C01";
        public const string IntroCaption2Id = "VO-S2-INTRO-C02";
        public const string HelpCaption1Id = "VO-S2-HELP-C01";
        public const string HelpCaption2Id = "VO-S2-HELP-C02";

        private sealed class ActiveSfx
        {
            internal ActiveSfx(AudioPlaybackToken token, double startedAt, double duration)
            { Token = token; StartedAt = startedAt; Duration = duration; }
            internal AudioPlaybackToken Token { get; }
            internal double StartedAt { get; set; }
            internal double Duration { get; }
        }

        private readonly AudioDirector _director;
        private readonly Stage2Content _content;
        private readonly SessionState _session;
        private readonly AccessibilityState _accessibility;
        private readonly IStage2MediaCueSink _sink;
        private readonly Func<Guid> _idFactory;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();
        private readonly HashSet<Guid> _consumedSfxActions = new HashSet<Guid>();
        private readonly List<ActiveSfx> _activeSfx = new List<ActiveSfx>();
        private Guid _routeTokenId;
        private Guid _mediaTokenId;
        private AudioPlaybackToken _voice;
        private AudioPlaybackToken _music;
        private AudioPlaybackToken _ambience;
        private Stage2MediaMode _mode;
        private double _startedAt;
        private double _elapsed;
        private double _pausedAt;
        private bool _active;
        private bool _paused;
        private bool _narrationSilenced;

        public Stage2MediaPresenter(AudioDirector director, IAudioClipRegistry registry,
            Stage2Content content, SessionState session, AccessibilityState accessibility,
            IStage2MediaCueSink sink, Func<Guid> idFactory = null)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _idFactory = idFactory ?? Guid.NewGuid;
            Register(registry, IntroVoiceId, content.VoiceClips[0], false);
            Register(registry, HelpVoiceId, content.VoiceClips[1], false);
            Register(registry, MusicId, content.Music, true);
            Register(registry, AmbienceId, content.Ambience, true);
            Register(registry, LowStoneSfxId, content.StoneSfx[0], false);
            Register(registry, MiddleStoneSfxId, content.StoneSfx[1], false);
            Register(registry, HighStoneSfxId, content.StoneSfx[2], false);
        }

        public double ElapsedSeconds => _elapsed;
        public bool HasActiveVoice => _voice != null;
        public bool HasActiveLoops => _music != null || _ambience != null;
        public bool HasActiveStoneSfx => _activeSfx.Count > 0;
        public bool IsPaused => _paused;
        public Guid CurrentMediaTokenId => _mediaTokenId;
        public Stage2MediaHandle StartIntro(Guid routeTokenId, double now) => Start(routeTokenId, Stage2MediaMode.Intro, now);
        public Stage2MediaHandle StartHelp(Guid routeTokenId, double now) => Start(routeTokenId, Stage2MediaMode.Help, now);

        public Stage2MediaHandle Repeat(Guid routeTokenId, double now)
        {
            if (!_active || _paused || routeTokenId != _routeTokenId)
                throw new InvalidOperationException("Cannot repeat stale Stage 2 media.");
            return Start(routeTokenId, _mode, now);
        }

        public bool Advance(Guid routeTokenId, Guid mediaTokenId, double now)
        {
            if (!Current(routeTokenId, mediaTokenId) || _paused || !Finite(now) || now < _startedAt) return false;
            CompleteExpiredSfx(now);
            if (_narrationSilenced) _elapsed = Duration(_mode);
            else
            {
                _elapsed = Math.Min(Duration(_mode), now - _startedAt);
                if (_elapsed >= Duration(_mode)) StopVoice();
            }
            Present();
            return true;
        }

        public bool PlayStoneSfx(Guid routeTokenId, Stage2River river, Guid actionId, double now)
        {
            if (!_active || _paused || routeTokenId != _routeTokenId || !IsRiver(river)
                || actionId == Guid.Empty || !Finite(now) || !_consumedSfxActions.Add(actionId)) return false;
            if (!_session.SoundEnabled) return false;
            CompleteExpiredSfx(now);
            var index = (int)river - 1;
            var token = _director.TryStart(AudioBus.Sfx, StoneSfxId(river), now);
            if (token == null) return false;
            _activeSfx.Add(new ActiveSfx(token, now, _content.StoneSfx[index].length));
            return true;
        }

        public bool SilenceNarration(Guid routeTokenId)
        {
            if (!_active || _paused || routeTokenId != _routeTokenId) return false;
            StopVoice();
            _narrationSilenced = true;
            _elapsed = Duration(_mode);
            Present();
            return true;
        }

        public bool Pause(Guid routeTokenId, Guid mediaTokenId, double now, Stage2PauseReason reason)
        {
            if (!Current(routeTokenId, mediaTokenId) || _paused || !Finite(now) || now < _startedAt
                || !Advance(routeTokenId, mediaTokenId, now)) return false;
            _paused = true;
            _pausedAt = now;
            _director.PauseAndDuckImmediately();
            _sink.FreezeMediaCue(_elapsed);
            _sink.SetCallerInputLocked(true);
            _sink.ShowPauseOverlay(reason);
            return true;
        }

        public bool CanPause(Guid routeTokenId, Guid mediaTokenId, double now) =>
            Current(routeTokenId, mediaTokenId) && !_paused && Finite(now) && now >= _startedAt;

        public bool CanResumeAfterMappedUserAction(Guid routeTokenId, double now) =>
            _active && _paused && routeTokenId == _routeTokenId && Finite(now) && now >= _pausedAt;

        public Stage2MediaHandle? ResumeAfterMappedUserAction(Guid routeTokenId, double now)
        {
            if (!_active || !_paused || routeTokenId != _routeTokenId || !Finite(now) || now < _pausedAt) return null;
            var pauseDuration = now - _pausedAt;
            for (var index = 0; index < _activeSfx.Count; index++) _activeSfx[index].StartedAt += pauseDuration;
            _mediaTokenId = NextTokenId();
            _startedAt = now - _elapsed;
            _director.ResumeAfterMappedUserAction();
            _paused = false;
            _sink.ResumeMediaCue(_mediaTokenId, _elapsed);
            _sink.SetCallerInputLocked(false);
            Present();
            return Handle();
        }

        public void RefreshSettings(double now)
        {
            if (!_active || !Finite(now)) return;
            if (!_session.SoundEnabled) { StopVoice(); StopLoops(); StopSfx(); }
            else if (!_paused)
            {
                EnsureLoops(now);
                if (_voice == null && !_narrationSilenced && _elapsed < Duration(_mode)) StartVoice(now);
            }
            Present();
        }

        public bool Stop(Guid routeTokenId)
        {
            if (!_active || routeTokenId != _routeTokenId) return false;
            StopVoice(); StopLoops(); StopSfx();
            _active = false; _paused = false; _routeTokenId = Guid.Empty; _mediaTokenId = Guid.Empty;
            _sink.SetCallerInputLocked(true);
            return true;
        }

        private Stage2MediaHandle Start(Guid routeTokenId, Stage2MediaMode mode, double now)
        {
            if (routeTokenId == Guid.Empty || !Finite(now)) throw new ArgumentOutOfRangeException();
            if (_paused) throw new InvalidOperationException("Paused Stage 2 media must resume or stop before replacement.");
            if (_active && routeTokenId != _routeTokenId)
            {
                StopVoice(); StopLoops(); StopSfx(); _consumedSfxActions.Clear();
            }
            else StopVoice();
            _routeTokenId = routeTokenId; _mode = mode; _mediaTokenId = NextTokenId();
            _startedAt = now; _elapsed = 0d; _active = true; _paused = false; _narrationSilenced = false;
            _sink.SetCallerInputLocked(false);
            if (_session.SoundEnabled) { EnsureLoops(now); StartVoice(now); }
            else StopLoops();
            Present();
            return Handle();
        }

        private void StartVoice(double now) => _voice = _director.TryStart(AudioBus.Voice,
            _mode == Stage2MediaMode.Intro ? IntroVoiceId : HelpVoiceId, now);

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
        private void StopSfx()
        {
            for (var index = 0; index < _activeSfx.Count; index++) _director.Complete(_activeSfx[index].Token.Id);
            _activeSfx.Clear();
        }
        private void CompleteExpiredSfx(double now)
        {
            for (var index = _activeSfx.Count - 1; index >= 0; index--)
                if (now - _activeSfx[index].StartedAt >= _activeSfx[index].Duration)
                { _director.Complete(_activeSfx[index].Token.Id); _activeSfx.RemoveAt(index); }
        }

        private void Present()
        {
            ResolveCue(_mode, _elapsed, out var captionId, out var cueStart, out var visible);
            _sink.PresentMediaCue(new Stage2MediaCue(_routeTokenId, _mediaTokenId, _mode,
                _content.PresentationSprites[_mode == Stage2MediaMode.Intro ? 2 : 3], captionId,
                _accessibility.CaptionsEnabled && visible, _startedAt + cueStart, _elapsed));
        }

        private Stage2MediaHandle Handle() => new Stage2MediaHandle(
            _routeTokenId, _mediaTokenId, _voice?.Id, _music?.Id, _ambience?.Id);
        private bool Current(Guid routeTokenId, Guid mediaTokenId) =>
            _active && routeTokenId == _routeTokenId && mediaTokenId == _mediaTokenId;
        private Guid NextTokenId()
        {
            var token = _idFactory();
            if (token == Guid.Empty || !_issuedTokens.Add(token))
                throw new InvalidOperationException("Stage 2 media token IDs must be non-empty and unique.");
            return token;
        }

        private static string StoneSfxId(Stage2River river) => river == Stage2River.Low ? LowStoneSfxId
            : river == Stage2River.Middle ? MiddleStoneSfxId : HighStoneSfxId;
        private static double Duration(Stage2MediaMode mode) => mode == Stage2MediaMode.Intro ? 7.548d : 7.148d;
        private static void ResolveCue(Stage2MediaMode mode, double elapsed,
            out string id, out double start, out bool visible)
        {
            id = null; start = elapsed; visible = false;
            if (mode == Stage2MediaMode.Intro)
            {
                if (At(elapsed, 0d) && Before(elapsed, 2.754d)) { id = IntroCaption1Id; start = 0d; visible = true; }
                else if (At(elapsed, 2.904d) && Before(elapsed, 7.548d)) { id = IntroCaption2Id; start = 2.904d; visible = true; }
            }
            else
            {
                if (At(elapsed, 0d) && Before(elapsed, 2.437d)) { id = HelpCaption1Id; start = 0d; visible = true; }
                else if (At(elapsed, 2.587d) && Before(elapsed, 7.148d)) { id = HelpCaption2Id; start = 2.587d; visible = true; }
            }
        }
        private static bool At(double value, double boundary) => value >= boundary - 0.000000001d;
        private static bool Before(double value, double boundary) => !At(value, boundary);
        private static bool IsRiver(Stage2River river) => river >= Stage2River.Low && river <= Stage2River.High;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Register(IAudioClipRegistry registry, string semanticId, AudioClip clip, bool loop)
        {
            if (!(registry is IAudioClipRegistryLookup lookup) || !lookup.IsClipRegistered(semanticId))
                registry.RegisterClip(semanticId, clip, loop);
        }
    }

    public static class Stage2CaptionStrings
    {
        private static readonly IReadOnlyDictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [Stage2MediaPresenter.IntroCaption1Id] = "Choose a river stone.",
                [Stage2MediaPresenter.IntroCaption2Id] = "Then look at the field, path, and boat.",
                [Stage2MediaPresenter.HelpCaption1Id] = "Choose one stone.",
                [Stage2MediaPresenter.HelpCaption2Id] = "Then tap each place to see what changes.",
            };

        public static string ResolveEnglish(string captionId) => string.IsNullOrEmpty(captionId)
            ? string.Empty : English.TryGetValue(captionId, out var value)
                ? value : throw new KeyNotFoundException(captionId);
    }
}
