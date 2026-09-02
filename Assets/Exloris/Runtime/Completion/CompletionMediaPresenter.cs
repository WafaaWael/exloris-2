using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using UnityEngine;

namespace Exloris.Runtime.Completion
{
    public readonly struct CompletionMediaCue
    {
        public CompletionMediaCue(
            Guid completionTokenId, Guid mediaTokenId, Sprite visualEquivalent,
            string captionId, string captionText, bool captionVisible,
            double captionStartedAtSeconds, double elapsedSeconds)
        {
            CompletionTokenId = completionTokenId;
            MediaTokenId = mediaTokenId;
            VisualEquivalent = visualEquivalent;
            CaptionId = captionId;
            CaptionText = captionText;
            CaptionVisible = captionVisible;
            CaptionStartedAtSeconds = captionStartedAtSeconds;
            ElapsedSeconds = elapsedSeconds;
        }

        public Guid CompletionTokenId { get; }
        public Guid MediaTokenId { get; }
        public Sprite VisualEquivalent { get; }
        public string CaptionId { get; }
        public string CaptionText { get; }
        public bool CaptionVisible { get; }
        public double CaptionStartedAtSeconds { get; }
        public double ElapsedSeconds { get; }
    }

    public interface ICompletionMediaCueSink
    {
        void PresentCompletionMedia(CompletionMediaCue cue);
        void FreezeCompletionMedia(double elapsedSeconds);
        void ResumeCompletionMedia(Guid mediaTokenId, double elapsedSeconds);
        void SetCompletionInputLocked(bool locked);
    }

    public readonly struct CompletionMediaHandle
    {
        public CompletionMediaHandle(
            Guid completionTokenId, Guid mediaTokenId,
            Guid? voiceTokenId, Guid? musicTokenId, Guid? ambienceTokenId)
        {
            CompletionTokenId = completionTokenId;
            MediaTokenId = mediaTokenId;
            VoiceTokenId = voiceTokenId;
            MusicTokenId = musicTokenId;
            AmbienceTokenId = ambienceTokenId;
        }
        public Guid CompletionTokenId { get; }
        public Guid MediaTokenId { get; }
        public Guid? VoiceTokenId { get; }
        public Guid? MusicTokenId { get; }
        public Guid? AmbienceTokenId { get; }
    }

    public sealed class CompletionMediaPresenter
    {
        public const string VoiceId = "VO-COMPLETION";
        public const string MusicId = "MUS-COMPLETION-LOOP";
        public const string AmbienceId = "AMB-VILLAGE-LOOP";
        public const string CaptionId = "VO-COMPLETION-C01";
        public const string CaptionText = "You explored the whole Nile journey.";

        private readonly AudioDirector _director;
        private readonly CompletionAdultReportContent _content;
        private readonly SessionState _session;
        private readonly AccessibilityState _accessibility;
        private readonly ICompletionMediaCueSink _sink;
        private readonly Func<Guid> _mediaIdFactory;
        private readonly HashSet<Guid> _issuedMediaIds = new HashSet<Guid>();
        private Guid _completionTokenId;
        private Guid _mediaTokenId;
        private AudioPlaybackToken _voice;
        private AudioPlaybackToken _music;
        private AudioPlaybackToken _ambience;
        private double _startedAt;
        private double _elapsed;
        private bool _active;
        private bool _paused;

        public CompletionMediaPresenter(
            AudioDirector director,
            IAudioClipRegistry registry,
            CompletionAdultReportContent content,
            SessionState session,
            AccessibilityState accessibility,
            ICompletionMediaCueSink sink,
            Func<Guid> mediaIdFactory = null)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _mediaIdFactory = mediaIdFactory ?? Guid.NewGuid;
            RegisterIfMissing(registry, VoiceId, content.CompletionVoice, false);
            RegisterIfMissing(registry, MusicId, content.CompletionMusic, true);
            RegisterIfMissing(registry, AmbienceId, content.VillageAmbience, true);
        }

        public CompletionMediaHandle Start(Guid completionTokenId, double nowSeconds)
        {
            if (completionTokenId == Guid.Empty || !IsValidClock(nowSeconds))
                throw new ArgumentException("A valid completion token and clock are required.");
            StopVoice();
            _completionTokenId = completionTokenId;
            _mediaTokenId = NextMediaId();
            _startedAt = nowSeconds;
            _elapsed = 0d;
            _active = true;
            _paused = false;
            _sink.SetCompletionInputLocked(false);
            if (_session.SoundEnabled)
            {
                EnsureLoops(nowSeconds);
                _voice = _director.TryStart(AudioBus.Voice, VoiceId, nowSeconds);
            }
            else StopLoops();
            Present();
            return Handle();
        }

        public bool Advance(Guid completionTokenId, Guid mediaTokenId, double nowSeconds)
        {
            if (!IsCurrent(completionTokenId, mediaTokenId) || _paused
                || !IsValidClock(nowSeconds) || nowSeconds < _startedAt)
                return false;
            _elapsed = Math.Min(CompletionController.SemanticSeconds, nowSeconds - _startedAt);
            Present();
            return true;
        }

        public bool Pause(Guid completionTokenId, Guid mediaTokenId, double nowSeconds)
        {
            if (!IsCurrent(completionTokenId, mediaTokenId) || _paused
                || !Advance(completionTokenId, mediaTokenId, nowSeconds)) return false;
            _paused = true;
            _director.PauseAndDuckImmediately();
            _sink.FreezeCompletionMedia(_elapsed);
            _sink.SetCompletionInputLocked(true);
            return true;
        }

        public bool HandleFocusRestored() => false;

        public CompletionMediaHandle? ResumeAfterMappedUserAction(Guid completionTokenId, double nowSeconds)
        {
            if (!_active || !_paused || completionTokenId != _completionTokenId || !IsValidClock(nowSeconds))
                return null;
            _mediaTokenId = NextMediaId();
            _startedAt = nowSeconds - _elapsed;
            _director.ResumeAfterMappedUserAction();
            _paused = false;
            _sink.ResumeCompletionMedia(_mediaTokenId, _elapsed);
            _sink.SetCompletionInputLocked(false);
            Present();
            return Handle();
        }

        public bool CompleteVoice(Guid completionTokenId, Guid mediaTokenId, Guid voiceTokenId)
        {
            if (_paused || !IsCurrent(completionTokenId, mediaTokenId)
                || _voice == null || _voice.Id != voiceTokenId || !_director.Complete(voiceTokenId))
                return false;
            _voice = null;
            return true;
        }

        public void RefreshSettings(double nowSeconds)
        {
            if (!_active) return;
            if (!_session.SoundEnabled)
            {
                StopVoice();
                StopLoops();
            }
            else if (!_paused)
            {
                EnsureLoops(nowSeconds);
                if (_voice == null && _elapsed < CompletionController.SemanticSeconds)
                    _voice = _director.TryStart(AudioBus.Voice, VoiceId, nowSeconds);
            }
            Present();
        }

        private void Present()
        {
            _sink.PresentCompletionMedia(new CompletionMediaCue(
                _completionTokenId, _mediaTokenId, _content.CompletionVisualEquivalent,
                CaptionId, CaptionText,
                _accessibility.CaptionsEnabled && _elapsed < CompletionController.SemanticSeconds,
                _startedAt, _elapsed));
        }

        private CompletionMediaHandle Handle() => new CompletionMediaHandle(
            _completionTokenId, _mediaTokenId, _voice?.Id, _music?.Id, _ambience?.Id);

        private void EnsureLoops(double nowSeconds)
        {
            if (_music == null && !_director.TryGetActive(AudioBus.Music, MusicId, out _music))
                _music = _director.TryStart(AudioBus.Music, MusicId, nowSeconds);
            if (_ambience == null && !_director.TryGetActive(AudioBus.Ambience, AmbienceId, out _ambience))
                _ambience = _director.TryStart(AudioBus.Ambience, AmbienceId, nowSeconds);
        }

        private void StopVoice()
        {
            if (_voice == null) return;
            _director.Complete(_voice.Id);
            _voice = null;
        }

        private void StopLoops()
        {
            if (_music != null) { _director.Complete(_music.Id); _music = null; }
            if (_ambience != null) { _director.Complete(_ambience.Id); _ambience = null; }
        }

        private bool IsCurrent(Guid completion, Guid media)
            => _active && completion == _completionTokenId && media == _mediaTokenId;

        private Guid NextMediaId()
        {
            var id = _mediaIdFactory();
            if (id == Guid.Empty || !_issuedMediaIds.Add(id))
                throw new InvalidOperationException("Completion media IDs must be non-empty and unique.");
            return id;
        }

        private static void RegisterIfMissing(
            IAudioClipRegistry registry, string semanticId, AudioClip clip, bool loop)
        {
            if (registry is IAudioClipRegistryLookup lookup && lookup.IsClipRegistered(semanticId)) return;
            registry.RegisterClip(semanticId, clip, loop);
        }

        private static bool IsValidClock(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }
}
