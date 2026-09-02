using System;
using System.Collections.Generic;
using Exloris.Runtime.Audio;

namespace Exloris.Runtime.Avatar
{
    public sealed class AvatarMediaPresenter
    {
        private const double ClockEpsilonSeconds = 0.000000001d;
        public const string SelectVoiceId = "VO-AVATAR-SELECT";
        public const string PreviewVoiceId = "VO-AVATAR-PREVIEW";
        public const string LeaveVoiceId = "VO-LEAVE";
        public const string PressSfxId = "SFX-UI-PRESS";
        public const string ConfirmSfxId = "SFX-UI-CONFIRM";
        public const string BackSfxId = "SFX-UI-BACK";
        public const string SelectCaption1Id = "VO-AVATAR-SELECT-C01";
        public const string PreviewCaption1Id = "VO-AVATAR-PREVIEW-C01";
        public const string PreviewCaption2Id = "VO-AVATAR-PREVIEW-C02";
        public const string PreviewCaption3Id = "VO-AVATAR-PREVIEW-C03";
        public const string LeaveCaption1Id = "VO-LEAVE-C01";
        public const string LeaveCaption2Id = "VO-LEAVE-C02";

        private readonly AudioDirector _director;
        private readonly AvatarContent _content;
        private readonly IAvatarMediaCueSink _sink;
        private AudioPlaybackToken _voice;
        private bool _hasPresentation;
        private bool _captionsEnabled;
        private double _startedAtSeconds;
        private AvatarMediaState _state;

        public bool VoiceIsPlaying => _voice != null;

        public AvatarMediaPresenter(
            AudioDirector director,
            IAudioClipRegistry registry,
            AvatarContent content,
            IAvatarMediaCueSink sink)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            registry.RegisterClip(SelectVoiceId, content.VoiceClips[0]);
            registry.RegisterClip(PreviewVoiceId, content.VoiceClips[1]);
            registry.RegisterClip(LeaveVoiceId, content.VoiceClips[2]);
            registry.RegisterClip(PressSfxId, content.SfxClips[0]);
            registry.RegisterClip(ConfirmSfxId, content.SfxClips[1]);
            registry.RegisterClip(BackSfxId, content.SfxClips[2]);
        }

        public AudioPlaybackToken Present(
            AvatarMediaState state,
            bool soundEnabled,
            bool captionsEnabled,
            double nowSeconds)
        {
            StopVoice();
            _state = state;
            _hasPresentation = true;
            _captionsEnabled = captionsEnabled;
            _startedAtSeconds = nowSeconds;
            _sink.ShowMediaCue(
                state,
                _content.VisualEquivalents[(int)state],
                FirstCaption(state),
                captionsEnabled,
                nowSeconds);

            if (!soundEnabled)
            {
                return null;
            }

            _voice = _director.TryStart(AudioBus.Voice, VoiceId(state), nowSeconds);
            return _voice;
        }

        public bool Advance(double nowSeconds)
        {
            if (!_hasPresentation || nowSeconds < _startedAtSeconds)
            {
                return false;
            }

            var elapsed = nowSeconds - _startedAtSeconds;
            ResolveCue(_state, elapsed, out var captionId, out var cueStart, out var visible);
            _sink.UpdateCaption(
                captionId,
                _captionsEnabled && visible,
                _startedAtSeconds + cueStart);
            return true;
        }

        public bool CompleteVoice(Guid tokenId)
        {
            if (_voice == null || _voice.Id != tokenId)
            {
                return false;
            }

            var completed = _director.Complete(tokenId);
            if (completed) _voice = null;
            return completed;
        }

        public AudioPlaybackToken PlaySfx(AvatarSfxCue cue, bool soundEnabled, double nowSeconds)
        {
            if (!soundEnabled)
            {
                return null;
            }

            return _director.TryStart(AudioBus.Sfx, SfxId(cue), nowSeconds);
        }

        public void Stop()
        {
            StopVoice();
            _hasPresentation = false;
        }

        private void StopVoice()
        {
            if (_voice == null) return;
            _director.Complete(_voice.Id);
            _voice = null;
        }

        private static string VoiceId(AvatarMediaState state)
        {
            switch (state)
            {
                case AvatarMediaState.Select: return SelectVoiceId;
                case AvatarMediaState.Preview: return PreviewVoiceId;
                case AvatarMediaState.Leave: return LeaveVoiceId;
                default: throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static string FirstCaption(AvatarMediaState state)
        {
            switch (state)
            {
                case AvatarMediaState.Select: return SelectCaption1Id;
                case AvatarMediaState.Preview: return PreviewCaption1Id;
                case AvatarMediaState.Leave: return LeaveCaption1Id;
                default: throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static string SfxId(AvatarSfxCue cue)
        {
            switch (cue)
            {
                case AvatarSfxCue.Press: return PressSfxId;
                case AvatarSfxCue.Confirm: return ConfirmSfxId;
                case AvatarSfxCue.Back: return BackSfxId;
                default: throw new ArgumentOutOfRangeException(nameof(cue));
            }
        }

        private static void ResolveCue(
            AvatarMediaState state,
            double elapsed,
            out string captionId,
            out double cueStart,
            out bool visible)
        {
            switch (state)
            {
                case AvatarMediaState.Select:
                    ResolveSingle(elapsed, 2.474d, SelectCaption1Id,
                        out captionId, out cueStart, out visible);
                    return;
                case AvatarMediaState.Preview:
                    ResolveThree(elapsed,
                        2.956d, 3.106d, 5.710d, 5.860d, 8.666d,
                        PreviewCaption1Id, PreviewCaption2Id, PreviewCaption3Id,
                        out captionId, out cueStart, out visible);
                    return;
                case AvatarMediaState.Leave:
                    ResolveTwo(elapsed,
                        2.832d, 2.982d, 7.557d,
                        LeaveCaption1Id, LeaveCaption2Id,
                        out captionId, out cueStart, out visible);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static void ResolveSingle(double elapsed, double end, string first,
            out string captionId, out double cueStart, out bool visible)
        {
            if (elapsed <= end + ClockEpsilonSeconds)
            {
                captionId = first;
                cueStart = 0d;
                visible = true;
                return;
            }
            captionId = null;
            cueStart = elapsed;
            visible = false;
        }

        private static void ResolveTwo(
            double elapsed, double firstEnd, double secondStart, double secondEnd,
            string first, string second,
            out string captionId, out double cueStart, out bool visible)
        {
            if (elapsed <= firstEnd + ClockEpsilonSeconds)
            {
                captionId = first;
                cueStart = 0d;
                visible = true;
            }
            else if (elapsed >= secondStart - ClockEpsilonSeconds
                && elapsed <= secondEnd + ClockEpsilonSeconds)
            {
                captionId = second;
                cueStart = secondStart;
                visible = true;
            }
            else
            {
                captionId = null;
                cueStart = elapsed;
                visible = false;
            }
        }

        private static void ResolveThree(
            double elapsed,
            double firstEnd,
            double secondStart,
            double secondEnd,
            double thirdStart,
            double thirdEnd,
            string first,
            string second,
            string third,
            out string captionId,
            out double cueStart,
            out bool visible)
        {
            if (elapsed <= firstEnd + ClockEpsilonSeconds)
            {
                captionId = first;
                cueStart = 0d;
                visible = true;
            }
            else if (elapsed >= secondStart - ClockEpsilonSeconds
                && elapsed <= secondEnd + ClockEpsilonSeconds)
            {
                captionId = second;
                cueStart = secondStart;
                visible = true;
            }
            else if (elapsed >= thirdStart - ClockEpsilonSeconds
                && elapsed <= thirdEnd + ClockEpsilonSeconds)
            {
                captionId = third;
                cueStart = thirdStart;
                visible = true;
            }
            else
            {
                captionId = null;
                cueStart = elapsed;
                visible = false;
            }
        }
    }

    public static class AvatarCaptionStrings
    {
        private static readonly IReadOnlyDictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [AvatarMediaPresenter.SelectCaption1Id] = "Choose your traveller.",
                [AvatarMediaPresenter.PreviewCaption1Id] = "This traveller is ready.",
                [AvatarMediaPresenter.PreviewCaption2Id] = "Choose this traveller,",
                [AvatarMediaPresenter.PreviewCaption3Id] = "or choose the other one.",
                [AvatarMediaPresenter.LeaveCaption1Id] = "Stay here, or go Home.",
                [AvatarMediaPresenter.LeaveCaption2Id] = "Your journey stays while the app is open.",
            };

        public static string ResolveEnglish(string captionId)
        {
            if (string.IsNullOrEmpty(captionId)) return string.Empty;
            if (!English.TryGetValue(captionId, out var value))
                throw new KeyNotFoundException("Unknown Avatar caption ID: " + captionId);
            return value;
        }
    }
}
