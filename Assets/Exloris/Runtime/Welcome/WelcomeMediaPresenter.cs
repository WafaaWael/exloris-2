using System;
using System.Collections.Generic;
using Exloris.Runtime.Audio;

namespace Exloris.Runtime.Welcome
{
    public sealed class WelcomeMediaPresenter
    {
        public const string WelcomeVoiceId = "VO-WELCOME-001";
        public const string SessionChoiceVoiceId = "VO-WELCOME-SESSION";
        public const string MusicId = "MUS-MAIN-JOURNEY-LOOP";
        public const string AmbienceId = "AMB-NILE-DAY-LOOP";
        public const string WelcomeCaption1Id = "VO-WELCOME-001-C01";
        public const string WelcomeCaption2Id = "VO-WELCOME-001-C02";
        public const string SessionChoiceCaption1Id = "VO-WELCOME-SESSION-C01";
        public const string SessionChoiceCaption2Id = "VO-WELCOME-SESSION-C02";

        private readonly AudioDirector _director;
        private readonly WelcomeContent _content;
        private readonly IWelcomeMediaCueSink _sink;
        private AudioPlaybackToken _music;
        private AudioPlaybackToken _ambience;
        private AudioPlaybackToken _voice;
        private bool _interrupted;
        private bool _hasPresentation;
        private WelcomePresentationState _state;
        private bool _soundEnabled;
        private bool _captionsEnabled;
        private double _presentationStartedAt;

        public WelcomeMediaPresenter(
            AudioDirector director,
            IAudioClipRegistry registry,
            WelcomeContent content,
            IWelcomeMediaCueSink sink)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            registry.RegisterClip(WelcomeVoiceId, content.WelcomeVoice);
            registry.RegisterClip(SessionChoiceVoiceId, content.SessionChoiceVoice);
            registry.RegisterClip(MusicId, content.Music, loop: true);
            registry.RegisterClip(AmbienceId, content.Ambience, loop: true);
        }

        public double? LastVoiceStartSeconds { get; private set; }
        public double LastVisualStartSeconds { get; private set; }
        public double LastCaptionStartSeconds { get; private set; }
        public bool IsInterrupted => _interrupted;
        public Guid? CurrentVoiceTokenId => _voice?.Id;

        public AudioPlaybackToken Present(
            WelcomePresentationState state,
            bool soundEnabled,
            bool captionsEnabled,
            double nowSeconds)
        {
            StopVoice();
            var visual = state == WelcomePresentationState.Welcome
                ? _content.WelcomeVisualEquivalent
                : _content.SessionChoiceVisualEquivalent;
            var caption = state == WelcomePresentationState.Welcome
                ? WelcomeCaption1Id
                : SessionChoiceCaption1Id;
            LastVisualStartSeconds = nowSeconds;
            LastCaptionStartSeconds = nowSeconds;
            _hasPresentation = true;
            _state = state;
            _soundEnabled = soundEnabled;
            _captionsEnabled = captionsEnabled;
            _presentationStartedAt = nowSeconds;
            _sink.ShowMediaCue(state, visual, caption, captionsEnabled, nowSeconds);

            if (!soundEnabled)
            {
                LastVoiceStartSeconds = null;
                StopLoops();
                return null;
            }

            EnsureLoops(nowSeconds);
            var semantic = state == WelcomePresentationState.Welcome
                ? WelcomeVoiceId
                : SessionChoiceVoiceId;
            _voice = _director.TryStart(AudioBus.Voice, semantic, nowSeconds);
            LastVoiceStartSeconds = _voice == null ? (double?)null : nowSeconds;
            return _voice;
        }

        public bool Advance(double nowSeconds)
        {
            if (!_hasPresentation || _interrupted || nowSeconds < _presentationStartedAt) return false;
            var elapsed = nowSeconds - _presentationStartedAt;
            string captionId;
            double cueStart;
            bool visible;
            if (_state == WelcomePresentationState.Welcome)
            {
                ResolveCue(elapsed, 2.932d, 3.082d, 5.648d, WelcomeCaption1Id, WelcomeCaption2Id,
                    out captionId, out cueStart, out visible);
            }
            else
            {
                ResolveCue(elapsed, 2.784d, 2.934d, 5.768d, SessionChoiceCaption1Id, SessionChoiceCaption2Id,
                    out captionId, out cueStart, out visible);
            }

            _sink.UpdateCaption(
                captionId,
                _captionsEnabled && visible,
                _presentationStartedAt + cueStart);
            return true;
        }

        public bool CompleteVoice(Guid tokenId)
        {
            if (_interrupted || _voice == null || _voice.Id != tokenId) return false;
            var completed = _director.Complete(tokenId);
            if (completed) _voice = null;
            return completed;
        }

        public bool Interrupt()
        {
            if (_interrupted) return false;
            _interrupted = true;
            _director.PauseAndDuckImmediately();
            StopVoice();
            _sink.PauseMediaCue();
            return true;
        }

        public bool ResumeAfterMappedUserAction(double nowSeconds)
        {
            if (!_interrupted) return false;
            _director.ResumeAfterMappedUserAction();
            _sink.ResumeMediaCue();
            _interrupted = false;
            Present(_state, _soundEnabled, _captionsEnabled, nowSeconds);
            return true;
        }

        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
            if (enabled) return;
            StopVoice();
            StopLoops();
        }

        private void EnsureLoops(double nowSeconds)
        {
            if (_music == null) _music = _director.TryStart(AudioBus.Music, MusicId, nowSeconds);
            if (_ambience == null) _ambience = _director.TryStart(AudioBus.Ambience, AmbienceId, nowSeconds);
        }

        private void StopVoice()
        {
            if (_voice == null) return;
            _director.Complete(_voice.Id);
            _voice = null;
        }

        private void StopLoops()
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

        private static void ResolveCue(
            double elapsed,
            double firstEnd,
            double secondStart,
            double secondEnd,
            string firstId,
            string secondId,
            out string captionId,
            out double cueStart,
            out bool visible)
        {
            if (elapsed <= firstEnd)
            {
                captionId = firstId;
                cueStart = 0d;
                visible = true;
                return;
            }

            if (elapsed < secondStart || elapsed > secondEnd)
            {
                captionId = null;
                cueStart = elapsed;
                visible = false;
                return;
            }

            captionId = secondId;
            cueStart = secondStart;
            visible = true;
        }
    }

    public static class WelcomeCaptionStrings
    {
        private static readonly IReadOnlyDictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [WelcomeMediaPresenter.WelcomeCaption1Id] = "Tap Play when you're ready",
                [WelcomeMediaPresenter.WelcomeCaption2Id] = "to explore the Nile.",
                [WelcomeMediaPresenter.SessionChoiceCaption1Id] = "Your journey stays here",
                [WelcomeMediaPresenter.SessionChoiceCaption2Id] = "while the app is open.",
            };

        public static string ResolveEnglish(string captionId)
        {
            if (string.IsNullOrEmpty(captionId)) return string.Empty;
            if (!English.TryGetValue(captionId, out var value))
            {
                throw new KeyNotFoundException("Unknown Welcome caption ID: " + captionId);
            }

            return value;
        }
    }
}
