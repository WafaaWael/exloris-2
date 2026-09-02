using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using UnityEngine;

namespace Exloris.Runtime.Stage1
{
    public enum Stage1MediaMode
    {
        Intro,
        Attach,
        Outside,
    }

    public readonly struct Stage1MediaCue
    {
        public Stage1MediaCue(
            Guid routeTokenId,
            Guid mediaTokenId,
            Stage1MediaMode mode,
            Sprite visualEquivalent,
            string captionId,
            bool captionVisible,
            double captionCueStartSeconds,
            double elapsedSeconds)
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
        public Stage1MediaMode Mode { get; }
        public Sprite VisualEquivalent { get; }
        public string CaptionId { get; }
        public bool CaptionVisible { get; }
        public double CaptionCueStartSeconds { get; }
        public double ElapsedSeconds { get; }
        public bool RequiresReading => false;
        public bool UsesEvaluationLanguage => false;
        public bool UsesSuccessOrFailureAudio => false;
    }

    public readonly struct Stage1MediaHandle
    {
        public Stage1MediaHandle(Guid routeTokenId, Guid mediaTokenId, Guid? voiceTokenId, Guid? musicTokenId, Guid? ambienceTokenId)
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

    public interface IStage1MediaCueSink
    {
        void PresentMediaCue(Stage1MediaCue cue);
        void FreezeMediaCue(double elapsedSeconds);
        void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds);
        void SetCallerInputLocked(bool locked);
        void ShowPauseOverlay(Stage1PauseReason reason);
    }

    public sealed class Stage1MediaPresenter
    {
        public const string IntroVoiceId = "VO-S1-INTRO";
        public const string AttachVoiceId = "VO-S1-ATTACH";
        public const string OutsideVoiceId = "VO-S1-OUTSIDE";
        public const string MusicId = "MUS-STAGE1-LOOP";
        public const string AmbienceId = "AMB-RIVER-LOOP";
        public const string AttachSfxId = "SFX-MARKER-ATTACH";
        public const string StepSfxId = "SFX-MARKER-STEP";
        public const string IntroCaption1Id = "VO-S1-INTRO-C01";
        public const string IntroCaption2Id = "VO-S1-INTRO-C02";
        public const string IntroCaption3Id = "VO-S1-INTRO-C03";
        public const string AttachCaption1Id = "VO-S1-ATTACH-C01";
        public const string OutsideCaption1Id = "VO-S1-OUTSIDE-C01";
        public const string OutsideCaption2Id = "VO-S1-OUTSIDE-C02";
        public const string OutsideCaption3Id = "VO-S1-OUTSIDE-C03";

        private sealed class ActiveSfx
        {
            internal ActiveSfx(AudioPlaybackToken token, double startedAt, double duration)
            {
                Token = token;
                StartedAt = startedAt;
                Duration = duration;
            }

            internal AudioPlaybackToken Token { get; }
            internal double StartedAt { get; set; }
            internal double Duration { get; }
        }

        private readonly AudioDirector _director;
        private readonly Stage1Content _content;
        private readonly SessionState _session;
        private readonly AccessibilityState _accessibility;
        private readonly IStage1MediaCueSink _sink;
        private readonly Func<Guid> _idFactory;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();
        private readonly HashSet<Guid> _consumedSfxActions = new HashSet<Guid>();
        private readonly List<ActiveSfx> _activeSfx = new List<ActiveSfx>();
        private Guid _routeTokenId;
        private Guid _mediaTokenId;
        private AudioPlaybackToken _voice;
        private AudioPlaybackToken _music;
        private AudioPlaybackToken _ambience;
        private Stage1MediaMode _mode;
        private double _startedAt;
        private double _elapsed;
        private double _pausedAt;
        private bool _active;
        private bool _paused;
        private bool _narrationSilenced;

        public Stage1MediaPresenter(
            AudioDirector director,
            IAudioClipRegistry registry,
            Stage1Content content,
            SessionState session,
            AccessibilityState accessibility,
            IStage1MediaCueSink sink,
            Func<Guid> idFactory = null)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _idFactory = idFactory ?? Guid.NewGuid;

            Register(registry, IntroVoiceId, content.VoiceClips[0], false);
            Register(registry, AttachVoiceId, content.VoiceClips[1], false);
            Register(registry, OutsideVoiceId, content.VoiceClips[2], false);
            Register(registry, MusicId, content.Music, true);
            Register(registry, AmbienceId, content.Ambience, true);
            Register(registry, AttachSfxId, content.AttachSfx, false);
            Register(registry, StepSfxId, content.StepSfx, false);
        }

        public double ElapsedSeconds => _elapsed;
        public Guid CurrentMediaTokenId => _mediaTokenId;
        public bool HasActiveVoice => _voice != null;
        public bool HasActiveLoops => _music != null || _ambience != null;
        public bool AttachSfxPlaying => _activeSfx.Exists(
            item => string.Equals(item.Token.SemanticId, AttachSfxId, StringComparison.Ordinal));
        public bool StepSfxPlaying => _activeSfx.Exists(
            item => string.Equals(item.Token.SemanticId, StepSfxId, StringComparison.Ordinal));
        public bool IsPaused => _paused;

        public Stage1MediaHandle StartIntro(Guid routeTokenId, double now) => Start(routeTokenId, Stage1MediaMode.Intro, now);
        public Stage1MediaHandle StartAttachCue(Guid routeTokenId, double now) => Start(routeTokenId, Stage1MediaMode.Attach, now);
        public Stage1MediaHandle StartOutside(Guid routeTokenId, double now) => Start(routeTokenId, Stage1MediaMode.Outside, now);

        public Stage1MediaHandle Repeat(Guid routeTokenId, double now)
        {
            if (!_active || _paused || routeTokenId != _routeTokenId)
                throw new InvalidOperationException("Cannot repeat stale Stage 1 media.");
            return Start(routeTokenId, _mode, now);
        }

        public bool Advance(Guid routeTokenId, Guid mediaTokenId, double now)
        {
            if (!Current(routeTokenId, mediaTokenId) || _paused || !Finite(now) || now < _startedAt) return false;
            CompleteExpiredSfx(now);
            if (_narrationSilenced)
            {
                _elapsed = Duration(_mode);
                Present();
                return true;
            }
            _elapsed = Math.Min(Duration(_mode), now - _startedAt);
            if (_elapsed >= Duration(_mode)) StopVoice();
            Present();
            return true;
        }

        public bool PlayAttachSfx(Guid routeTokenId, Guid actionId, double now) =>
            PlaySfx(routeTokenId, actionId, AttachSfxId, _content.AttachSfx.length, now);

        public bool PlayStepSfx(Guid routeTokenId, Guid actionId, double now) =>
            PlaySfx(routeTokenId, actionId, StepSfxId, _content.StepSfx.length, now);

        public bool SilenceNarration(Guid routeTokenId)
        {
            if (!_active || _paused || routeTokenId != _routeTokenId) return false;
            StopVoice();
            _narrationSilenced = true;
            _elapsed = Duration(_mode);
            Present();
            return true;
        }

        public bool CanPause(Guid routeTokenId, Guid mediaTokenId, double now) =>
            Current(routeTokenId, mediaTokenId) && !_paused && Finite(now) && now >= _startedAt;

        public bool CanResumeAfterMappedUserAction(Guid routeTokenId, double now) =>
            _active && _paused && routeTokenId == _routeTokenId && Finite(now) && now >= _pausedAt;

        public bool Pause(Guid routeTokenId, Guid mediaTokenId, double now, Stage1PauseReason reason)
        {
            if (!CanPause(routeTokenId, mediaTokenId, now) || !Advance(routeTokenId, mediaTokenId, now)) return false;
            _paused = true;
            _pausedAt = now;
            _director.PauseAndDuckImmediately();
            _sink.FreezeMediaCue(_elapsed);
            _sink.SetCallerInputLocked(true);
            _sink.ShowPauseOverlay(reason);
            return true;
        }

        public Stage1MediaHandle? ResumeAfterMappedUserAction(Guid routeTokenId, double now)
        {
            if (!CanResumeAfterMappedUserAction(routeTokenId, now)) return null;
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
            if (!_session.SoundEnabled)
            {
                StopVoice();
                StopLoops();
                StopSfx();
            }
            else if (!_paused)
            {
                EnsureLoops(now);
                if (_voice == null && _elapsed < Duration(_mode)) StartVoice(now);
            }
            Present();
        }

        public bool Stop(Guid routeTokenId)
        {
            if (!_active || routeTokenId != _routeTokenId) return false;
            StopVoice();
            StopLoops();
            StopSfx();
            _active = false;
            _paused = false;
            _routeTokenId = Guid.Empty;
            _mediaTokenId = Guid.Empty;
            _sink.SetCallerInputLocked(true);
            return true;
        }

        private Stage1MediaHandle Start(Guid routeTokenId, Stage1MediaMode mode, double now)
        {
            if (routeTokenId == Guid.Empty || !Finite(now)) throw new ArgumentOutOfRangeException();
            if (_paused) throw new InvalidOperationException("Paused Stage 1 media must resume or stop before replacement.");
            if (_active && routeTokenId != _routeTokenId)
            {
                StopVoice();
                StopLoops();
                StopSfx();
                _consumedSfxActions.Clear();
            }
            else StopVoice();

            _routeTokenId = routeTokenId;
            _mode = mode;
            _mediaTokenId = NextTokenId();
            _startedAt = now;
            _elapsed = 0d;
            _active = true;
            _paused = false;
            _narrationSilenced = false;
            _sink.SetCallerInputLocked(false);
            if (_session.SoundEnabled)
            {
                EnsureLoops(now);
                StartVoice(now);
            }
            else StopLoops();
            Present();
            return Handle();
        }

        private bool PlaySfx(Guid routeTokenId, Guid actionId, string semanticId, double duration, double now)
        {
            if (!_active || _paused || routeTokenId != _routeTokenId || actionId == Guid.Empty || !Finite(now)) return false;
            if (!_consumedSfxActions.Add(actionId)) return false;
            if (!_session.SoundEnabled) return false;
            CompleteExpiredSfx(now);
            var token = _director.TryStart(AudioBus.Sfx, semanticId, now);
            if (token == null) return false;
            _activeSfx.Add(new ActiveSfx(token, now, duration));
            return true;
        }

        private void StartVoice(double now) => _voice = _director.TryStart(AudioBus.Voice, VoiceId(_mode), now);

        private void EnsureLoops(double now)
        {
            if (_music == null && !_director.TryGetActive(AudioBus.Music, MusicId, out _music))
                _music = _director.TryStart(AudioBus.Music, MusicId, now);
            if (_ambience == null && !_director.TryGetActive(AudioBus.Ambience, AmbienceId, out _ambience))
                _ambience = _director.TryStart(AudioBus.Ambience, AmbienceId, now);
        }

        private void StopVoice()
        {
            if (_voice != null) _director.Complete(_voice.Id);
            _voice = null;
        }

        private void StopLoops()
        {
            if (_music != null) _director.Complete(_music.Id);
            if (_ambience != null) _director.Complete(_ambience.Id);
            _music = null;
            _ambience = null;
        }

        private void StopSfx()
        {
            for (var index = 0; index < _activeSfx.Count; index++) _director.Complete(_activeSfx[index].Token.Id);
            _activeSfx.Clear();
        }

        private void CompleteExpiredSfx(double now)
        {
            for (var index = _activeSfx.Count - 1; index >= 0; index--)
            {
                var active = _activeSfx[index];
                if (now - active.StartedAt < active.Duration) continue;
                _director.Complete(active.Token.Id);
                _activeSfx.RemoveAt(index);
            }
        }

        private void Present()
        {
            ResolveCue(_mode, _elapsed, out var captionId, out var cueStart, out var visible);
            var visualIndex = _mode == Stage1MediaMode.Intro ? 2 : _mode == Stage1MediaMode.Attach ? 3 : 4;
            _sink.PresentMediaCue(new Stage1MediaCue(
                _routeTokenId,
                _mediaTokenId,
                _mode,
                _content.PresentationSprites[visualIndex],
                captionId,
                _accessibility.CaptionsEnabled && visible,
                _startedAt + cueStart,
                _elapsed));
        }

        private Stage1MediaHandle Handle() => new Stage1MediaHandle(
            _routeTokenId, _mediaTokenId, _voice?.Id, _music?.Id, _ambience?.Id);

        private bool Current(Guid routeTokenId, Guid mediaTokenId) =>
            _active && routeTokenId == _routeTokenId && mediaTokenId == _mediaTokenId;

        private Guid NextTokenId()
        {
            var tokenId = _idFactory();
            if (tokenId == Guid.Empty || !_issuedTokens.Add(tokenId))
                throw new InvalidOperationException("Stage 1 media token IDs must be non-empty and unique.");
            return tokenId;
        }

        private static string VoiceId(Stage1MediaMode mode) =>
            mode == Stage1MediaMode.Intro ? IntroVoiceId : mode == Stage1MediaMode.Attach ? AttachVoiceId : OutsideVoiceId;

        private static double Duration(Stage1MediaMode mode) =>
            mode == Stage1MediaMode.Intro ? 8.227d : mode == Stage1MediaMode.Attach ? 3.966d : 9.540d;

        private static void ResolveCue(Stage1MediaMode mode, double elapsed, out string id, out double start, out bool visible)
        {
            id = null;
            start = elapsed;
            visible = false;
            if (mode == Stage1MediaMode.Intro)
            {
                if (At(elapsed, 0d) && Before(elapsed, 3.744d)) { id = IntroCaption1Id; start = 0d; visible = true; }
                else if (At(elapsed, 3.894d) && Before(elapsed, 5.554d)) { id = IntroCaption2Id; start = 3.894d; visible = true; }
                else if (At(elapsed, 5.704d) && Before(elapsed, 8.227d)) { id = IntroCaption3Id; start = 5.704d; visible = true; }
            }
            else if (mode == Stage1MediaMode.Attach)
            {
                if (At(elapsed, 0d) && Before(elapsed, 3.966d)) { id = AttachCaption1Id; start = 0d; visible = true; }
            }
            else
            {
                if (At(elapsed, 0d) && Before(elapsed, 3.249d)) { id = OutsideCaption1Id; start = 0d; visible = true; }
                else if (At(elapsed, 3.399d) && Before(elapsed, 5.983d)) { id = OutsideCaption2Id; start = 3.399d; visible = true; }
                else if (At(elapsed, 6.133d) && Before(elapsed, 9.540d)) { id = OutsideCaption3Id; start = 6.133d; visible = true; }
            }
        }

        private static bool At(double value, double boundary) => value >= boundary - 0.000000001d;
        private static bool Before(double value, double boundary) => !At(value, boundary);
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static void Register(IAudioClipRegistry registry, string semanticId, AudioClip clip, bool loop)
        {
            if (!(registry is IAudioClipRegistryLookup lookup) || !lookup.IsClipRegistered(semanticId))
                registry.RegisterClip(semanticId, clip, loop);
        }
    }

    public static class Stage1CaptionStrings
    {
        private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Stage1MediaPresenter.IntroCaption1Id] = "Put the marker on the river post.",
            [Stage1MediaPresenter.IntroCaption2Id] = "Move it.",
            [Stage1MediaPresenter.IntroCaption3Id] = "Then tap the check.",
            [Stage1MediaPresenter.AttachCaption1Id] = "First, put the marker on the post.",
            [Stage1MediaPresenter.OutsideCaption1Id] = "Something interesting happened.",
            [Stage1MediaPresenter.OutsideCaption2Id] = "Let's see what changed.",
            [Stage1MediaPresenter.OutsideCaption3Id] = "Now let's try another place.",
        };

        public static string ResolveEnglish(string captionId) => string.IsNullOrEmpty(captionId)
            ? string.Empty
            : English.TryGetValue(captionId, out var value) ? value : throw new KeyNotFoundException(captionId);
    }
}
