using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;

namespace Exloris.Runtime.Overview
{
    public sealed class JourneyOverviewMediaPresenter : IJourneyOverviewPauseMedia
    {
        private const double ClockEpsilonSeconds = 0.000000001d;
        public const string VoiceId = "VO-OVERVIEW-001";
        public const string MusicId = "MUS-MAIN-JOURNEY-LOOP";
        public const string AmbienceId = "AMB-NILE-DAY-LOOP";
        public const string CaptionMeasureId = "VO-OVERVIEW-001-C01";
        public const string CaptionReadId = "VO-OVERVIEW-001-C02";
        public const string CaptionHelpId = "VO-OVERVIEW-001-C03";

        private readonly AudioDirector _director;
        private readonly JourneyOverviewContent _content;
        private readonly SessionState _session;
        private readonly AccessibilityState _accessibility;
        private readonly IJourneyOverviewMediaCueSink _sink;
        private readonly Func<Guid> _mediaIdFactory;
        private readonly HashSet<Guid> _issuedMediaIds = new HashSet<Guid>();

        private Guid _revealTokenId;
        private Guid _mediaTokenId;
        private AudioPlaybackToken _voice;
        private AudioPlaybackToken _music;
        private AudioPlaybackToken _ambience;
        private double _presentationStartedAt;
        private double _elapsedSeconds;
        private bool _hasSession;
        private bool _paused;

        public JourneyOverviewMediaPresenter(
            AudioDirector director,
            IAudioClipRegistry registry,
            JourneyOverviewContent content,
            SessionState session,
            AccessibilityState accessibility,
            IJourneyOverviewMediaCueSink sink,
            Func<Guid> mediaIdFactory = null)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _mediaIdFactory = mediaIdFactory ?? Guid.NewGuid;

            RegisterIfMissing(registry, VoiceId, content.Voice, false);
            RegisterIfMissing(registry, MusicId, content.Music, true);
            RegisterIfMissing(registry, AmbienceId, content.Ambience, true);
        }

        public Guid CurrentRevealTokenId => _revealTokenId;
        public Guid CurrentMediaTokenId => _mediaTokenId;
        public Guid? CurrentVoiceTokenId => _voice?.Id;
        public double ElapsedSeconds => _elapsedSeconds;
        public bool IsPaused => _paused;

        public JourneyOverviewMediaHandle Start(Guid revealTokenId, double nowSeconds)
        {
            ValidateTokenAndTime(revealTokenId, nowSeconds);
            StopVoice();
            _revealTokenId = revealTokenId;
            _mediaTokenId = NextMediaId();
            _presentationStartedAt = nowSeconds;
            _elapsedSeconds = 0d;
            _hasSession = true;
            _paused = false;
            _sink.SetCallerInputLocked(false);

            if (_session.SoundEnabled)
            {
                EnsureLoops(nowSeconds);
                _voice = _director.TryStart(AudioBus.Voice, VoiceId, nowSeconds);
            }
            else
            {
                StopSharedLoops();
            }

            PresentAtElapsed();
            return CurrentHandle();
        }

        public bool Advance(Guid revealTokenId, Guid mediaTokenId, double nowSeconds)
        {
            if (!IsCurrent(revealTokenId, mediaTokenId) || _paused
                || double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds)
                || nowSeconds < _presentationStartedAt)
            {
                return false;
            }

            _elapsedSeconds = Math.Min(
                JourneyOverviewController.CompletionSeconds,
                nowSeconds - _presentationStartedAt);
            PresentAtElapsed();
            return true;
        }

        public bool CompleteVoice(Guid revealTokenId, Guid mediaTokenId, Guid audioTokenId)
        {
            if (_paused || !IsCurrent(revealTokenId, mediaTokenId)
                || _voice == null || _voice.Id != audioTokenId)
            {
                return false;
            }

            if (!_director.Complete(audioTokenId)) return false;
            _voice = null;
            return true;
        }

        public bool Pause(
            Guid revealTokenId,
            Guid mediaTokenId,
            double nowSeconds,
            JourneyOverviewPauseReason reason)
        {
            if (!IsCurrent(revealTokenId, mediaTokenId) || _paused)
            {
                return false;
            }

            if (!Advance(revealTokenId, mediaTokenId, nowSeconds))
            {
                return false;
            }

            _paused = true;
            _director.PauseAndDuckImmediately();
            _sink.FreezeMediaCue(_elapsedSeconds);
            _sink.SetCallerInputLocked(true);
            _sink.ShowPauseOverlay(reason);
            return true;
        }

        public bool HandleFocusRestored()
        {
            return false;
        }

        public JourneyOverviewMediaHandle? ResumeAfterMappedUserAction(
            Guid revealTokenId,
            double nowSeconds)
        {
            if (!_hasSession || !_paused || revealTokenId != _revealTokenId
                || double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds))
            {
                return null;
            }

            _mediaTokenId = NextMediaId();
            _presentationStartedAt = nowSeconds - _elapsedSeconds;
            _director.ResumeAfterMappedUserAction();
            _paused = false;
            _sink.ResumeMediaCue(_mediaTokenId, _elapsedSeconds);
            _sink.SetCallerInputLocked(false);
            PresentAtElapsed();
            return CurrentHandle();
        }

        public bool Stop(Guid revealTokenId)
        {
            if (!_hasSession || revealTokenId != _revealTokenId)
            {
                return false;
            }

            StopVoice();
            StopSharedLoops();
            _hasSession = false;
            _paused = false;
            _revealTokenId = Guid.Empty;
            _mediaTokenId = Guid.Empty;
            _sink.SetCallerInputLocked(true);
            return true;
        }

        public void RefreshSettings(double nowSeconds)
        {
            if (!_hasSession) return;
            if (!_session.SoundEnabled)
            {
                StopVoice();
                StopSharedLoops();
            }
            else if (!_paused)
            {
                EnsureLoops(nowSeconds);
                if (_voice == null && _elapsedSeconds < JourneyOverviewController.CompletionSeconds)
                {
                    _voice = _director.TryStart(AudioBus.Voice, VoiceId, nowSeconds);
                }
            }

            PresentAtElapsed();
        }

        private void PresentAtElapsed()
        {
            ResolveCue(_elapsedSeconds, out var captionId, out var cueStart, out var visible);
            var mask = IsAtOrAfter(_elapsedSeconds, JourneyOverviewController.HelpBeatSeconds)
                ? JourneyOverviewRevealPresenter.HelpMaskPixels
                : IsAtOrAfter(_elapsedSeconds, JourneyOverviewController.ReadBeatSeconds)
                    ? JourneyOverviewRevealPresenter.ReadMaskPixels
                    : JourneyOverviewRevealPresenter.MeasureMaskPixels;
            _sink.PresentMediaCue(new JourneyOverviewMediaCue(
                _revealTokenId,
                _mediaTokenId,
                _content.OverviewVisualEquivalent,
                mask,
                captionId,
                _accessibility.CaptionsEnabled && visible,
                _presentationStartedAt + cueStart,
                _elapsedSeconds));
        }

        private void EnsureLoops(double nowSeconds)
        {
            if (_music == null)
            {
                if (!_director.TryGetActive(AudioBus.Music, MusicId, out _music))
                    _music = _director.TryStart(AudioBus.Music, MusicId, nowSeconds);
            }

            if (_ambience == null)
            {
                if (!_director.TryGetActive(AudioBus.Ambience, AmbienceId, out _ambience))
                    _ambience = _director.TryStart(AudioBus.Ambience, AmbienceId, nowSeconds);
            }
        }

        private void StopVoice()
        {
            if (_voice == null) return;
            _director.Complete(_voice.Id);
            _voice = null;
        }

        private void StopSharedLoops()
        {
            if (_music != null)
            {
                _director.Complete(_music.Id);
                _music = null;
            }

            if (_ambience != null)
            {
                _director.Complete(_ambience.Id);
                _ambience = null;
            }
        }

        private bool IsCurrent(Guid revealTokenId, Guid mediaTokenId)
        {
            return _hasSession
                && revealTokenId == _revealTokenId
                && mediaTokenId == _mediaTokenId;
        }

        private JourneyOverviewMediaHandle CurrentHandle()
        {
            return new JourneyOverviewMediaHandle(
                _revealTokenId,
                _mediaTokenId,
                _voice?.Id,
                _music?.Id,
                _ambience?.Id);
        }

        private Guid NextMediaId()
        {
            var id = _mediaIdFactory();
            if (id == Guid.Empty || !_issuedMediaIds.Add(id))
                throw new InvalidOperationException("Journey Overview media IDs must be non-empty and unique.");
            return id;
        }

        private static void RegisterIfMissing(
            IAudioClipRegistry registry,
            string semanticId,
            UnityEngine.AudioClip clip,
            bool loop)
        {
            if (registry is IAudioClipRegistryLookup lookup
                && lookup.IsClipRegistered(semanticId))
            {
                return;
            }

            registry.RegisterClip(semanticId, clip, loop);
        }

        private static void ResolveCue(
            double elapsed,
            out string captionId,
            out double cueStart,
            out bool visible)
        {
            if (elapsed >= 0d && IsBefore(elapsed, 3.139d))
            {
                captionId = CaptionMeasureId;
                cueStart = 0d;
                visible = true;
            }
            else if (IsAtOrAfter(elapsed, 3.289d)
                && IsBefore(elapsed, 6.032d))
            {
                captionId = CaptionReadId;
                cueStart = 3.289d;
                visible = true;
            }
            else if (IsAtOrAfter(elapsed, 6.182d)
                && IsBefore(elapsed, 9.000d))
            {
                captionId = CaptionHelpId;
                cueStart = 6.182d;
                visible = true;
            }
            else
            {
                captionId = null;
                cueStart = elapsed;
                visible = false;
            }
        }

        private static bool IsAtOrAfter(double elapsed, double boundary)
        {
            return elapsed >= boundary - ClockEpsilonSeconds;
        }

        private static bool IsBefore(double elapsed, double boundary)
        {
            return !IsAtOrAfter(elapsed, boundary);
        }

        private static void ValidateTokenAndTime(Guid tokenId, double nowSeconds)
        {
            if (tokenId == Guid.Empty) throw new ArgumentException("Reveal token cannot be empty.", nameof(tokenId));
            if (double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds))
                throw new ArgumentOutOfRangeException(nameof(nowSeconds));
        }
    }

    public static class JourneyOverviewCaptionStrings
    {
        private static readonly IReadOnlyDictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [JourneyOverviewMediaPresenter.CaptionMeasureId] = "First, measure the river.",
                [JourneyOverviewMediaPresenter.CaptionReadId] = "Next, read the river.",
                [JourneyOverviewMediaPresenter.CaptionHelpId] = "Then, help the village.",
            };

        public static string ResolveEnglish(string captionId)
        {
            if (string.IsNullOrEmpty(captionId)) return string.Empty;
            if (!English.TryGetValue(captionId, out var value))
                throw new KeyNotFoundException("Unknown Journey Overview caption ID: " + captionId);
            return value;
        }
    }
}
