using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using UnityEngine;

namespace Exloris.Runtime.Boat
{
    public enum Boat1MediaMode { Choice, Help }

    public readonly struct Boat1MediaCue
    {
        public Boat1MediaCue(Guid routeTokenId, Guid mediaTokenId, Boat1MediaMode mode, Sprite visual,
            string captionId, bool captionVisible, double captionStart, double elapsed)
        {
            RouteTokenId = routeTokenId; MediaTokenId = mediaTokenId; Mode = mode; VisualEquivalent = visual;
            CaptionId = captionId; CaptionVisible = captionVisible; CaptionCueStartSeconds = captionStart; ElapsedSeconds = elapsed;
        }
        public Guid RouteTokenId { get; }
        public Guid MediaTokenId { get; }
        public Boat1MediaMode Mode { get; }
        public Sprite VisualEquivalent { get; }
        public string CaptionId { get; }
        public bool CaptionVisible { get; }
        public double CaptionCueStartSeconds { get; }
        public double ElapsedSeconds { get; }
        public bool RequiresReading => false;
        public bool UsesNegativeSfx => false;
    }

    public readonly struct Boat1MediaHandle
    {
        public Boat1MediaHandle(Guid route, Guid media, Guid? voice, Guid? music, Guid? ambience)
        { RouteTokenId = route; MediaTokenId = media; VoiceTokenId = voice; MusicTokenId = music; AmbienceTokenId = ambience; }
        public Guid RouteTokenId { get; }
        public Guid MediaTokenId { get; }
        public Guid? VoiceTokenId { get; }
        public Guid? MusicTokenId { get; }
        public Guid? AmbienceTokenId { get; }
    }

    public interface IBoat1MediaCueSink
    {
        void PresentMediaCue(Boat1MediaCue cue);
        void FreezeMediaCue(double elapsedSeconds);
        void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds);
        void SetCallerInputLocked(bool locked);
        void ShowPauseOverlay(Boat1PauseReason reason);
    }

    public sealed class Boat1MediaPresenter
    {
        public const string ChoiceVoiceId = "VO-BOAT-CHOICE";
        public const string HelpVoiceId = "VO-BOAT-HELP";
        public const string MusicId = "MUS-MAIN-JOURNEY-LOOP";
        public const string AmbienceId = "AMB-BOAT-WATER-LOOP";
        public const string ArrivalSfxId = "SFX-BOAT-ARRIVAL";
        public const string IbisSfxId = "SFX-IBIS-CUE";
        public const string ChoiceCaption1Id = "VO-BOAT-CHOICE-C01";
        public const string ChoiceCaption2Id = "VO-BOAT-CHOICE-C02";
        public const string ChoiceCaption3Id = "VO-BOAT-CHOICE-C03";
        public const string HelpCaption1Id = "VO-BOAT-HELP-C01";
        public const string HelpCaption2Id = "VO-BOAT-HELP-C02";

        private readonly AudioDirector _director;
        private readonly Boat1Content _content;
        private readonly SessionState _session;
        private readonly AccessibilityState _accessibility;
        private readonly IBoat1MediaCueSink _sink;
        private readonly Func<Guid> _idFactory;
        private readonly HashSet<Guid> _issued = new HashSet<Guid>();
        private Guid _routeToken, _mediaToken;
        private AudioPlaybackToken _voice, _music, _ambience, _arrivalSfx, _ibisSfx;
        private Boat1MediaMode _mode;
        private double _startedAt, _elapsed;
        private double _pausedAt, _arrivalSfxStartedAt, _ibisSfxStartedAt;
        private bool _active, _paused;

        public Boat1MediaPresenter(AudioDirector director, IAudioClipRegistry registry, Boat1Content content,
            SessionState session, AccessibilityState accessibility, IBoat1MediaCueSink sink, Func<Guid> idFactory = null)
        {
            _director = director ?? throw new ArgumentNullException(nameof(director));
            if (registry == null) throw new ArgumentNullException(nameof(registry));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _idFactory = idFactory ?? Guid.NewGuid;
            Register(registry, ChoiceVoiceId, content.VoiceClips[0], false);
            Register(registry, HelpVoiceId, content.VoiceClips[1], false);
            Register(registry, MusicId, content.Music, true);
            Register(registry, AmbienceId, content.Ambience, true);
            Register(registry, ArrivalSfxId, content.ArrivalSfx, false);
            Register(registry, IbisSfxId, content.IbisSfx, false);
        }

        public double ElapsedSeconds => _elapsed;
        public Guid CurrentMediaTokenId => _mediaToken;
        public bool IsPaused => _paused;
        public bool ArrivalSfxPlaying => _arrivalSfx != null;
        public bool IbisSfxPlaying => _ibisSfx != null;
        public Boat1MediaHandle StartChoice(Guid routeToken, double now) => Start(routeToken, Boat1MediaMode.Choice, now);
        public Boat1MediaHandle StartHelp(Guid routeToken, double now) => Start(routeToken, Boat1MediaMode.Help, now);
        public Boat1MediaHandle Repeat(Guid routeToken, double now)
        {
            if (!_active || routeToken != _routeToken) throw new InvalidOperationException("Cannot repeat stale Boat 1 media.");
            return Start(routeToken, _mode, now);
        }

        public bool Advance(Guid routeToken, Guid mediaToken, double now)
        {
            if (!Current(routeToken, mediaToken) || _paused || !Finite(now) || now < _startedAt) return false;
            CompleteExpiredSfx(now);
            _elapsed = Math.Min(Duration(_mode), now - _startedAt);
            Present();
            return true;
        }

        public bool PlayArrivalSfx(Guid routeToken, double now)
        {
            if (!CanPlaySfx(routeToken, now) || _arrivalSfx != null) return false;
            _arrivalSfx = _director.TryStart(AudioBus.Sfx, ArrivalSfxId, now);
            if (_arrivalSfx != null) _arrivalSfxStartedAt = now;
            return _arrivalSfx != null;
        }

        public bool PlayIbisCueSfx(Guid routeToken, double now)
        {
            if (!CanPlaySfx(routeToken, now) || _ibisSfx != null) return false;
            _ibisSfx = _director.TryStart(AudioBus.Sfx, IbisSfxId, now);
            if (_ibisSfx != null) _ibisSfxStartedAt = now;
            return _ibisSfx != null;
        }

        public bool CompleteVoice(Guid routeToken, Guid mediaToken, Guid audioToken)
        {
            if (!Current(routeToken, mediaToken) || _paused || _voice == null || _voice.Id != audioToken) return false;
            if (!_director.Complete(audioToken)) return false;
            _voice = null;
            return true;
        }

        public bool Pause(Guid routeToken, Guid mediaToken, double now, Boat1PauseReason reason)
        {
            if (!Current(routeToken, mediaToken) || _paused || !Advance(routeToken, mediaToken, now)) return false;
            _paused = true; _pausedAt = now; _director.PauseAndDuckImmediately(); _sink.FreezeMediaCue(_elapsed);
            _sink.SetCallerInputLocked(true); _sink.ShowPauseOverlay(reason); return true;
        }

        public bool CanPause(Guid routeToken, Guid mediaToken, double now)
        {
            return Current(routeToken, mediaToken) && !_paused && Finite(now) && now >= _startedAt;
        }

        public Boat1MediaHandle? ResumeAfterMappedUserAction(Guid routeToken, double now)
        {
            if (!_active || !_paused || routeToken != _routeToken || !Finite(now) || now < _pausedAt) return null;
            var pauseDuration = now - _pausedAt;
            if (_arrivalSfx != null) _arrivalSfxStartedAt += pauseDuration;
            if (_ibisSfx != null) _ibisSfxStartedAt += pauseDuration;
            _mediaToken = NextId(); _startedAt = now - _elapsed; _director.ResumeAfterMappedUserAction(); _paused = false;
            _sink.ResumeMediaCue(_mediaToken, _elapsed); _sink.SetCallerInputLocked(false); Present(); return Handle();
        }

        public void RefreshSettings(double now)
        {
            if (!_active) return;
            if (!_session.SoundEnabled) { StopVoice(); StopLoops(); StopSfx(); }
            else if (!_paused) { EnsureLoops(now); if (_voice == null && _elapsed < Duration(_mode)) StartVoice(now); }
            Present();
        }

        public bool Stop(Guid routeToken)
        {
            if (!_active || routeToken != _routeToken) return false;
            StopVoice(); StopLoops(); StopSfx(); _active = false; _paused = false; _routeToken = Guid.Empty; _mediaToken = Guid.Empty;
            _sink.SetCallerInputLocked(true); return true;
        }

        private Boat1MediaHandle Start(Guid routeToken, Boat1MediaMode mode, double now)
        {
            if (routeToken == Guid.Empty || !Finite(now)) throw new ArgumentOutOfRangeException();
            StopVoice(); _routeToken = routeToken; _mode = mode; _mediaToken = NextId(); _startedAt = now; _elapsed = 0; _active = true; _paused = false;
            _sink.SetCallerInputLocked(false);
            if (_session.SoundEnabled) { EnsureLoops(now); StartVoice(now); } else StopLoops();
            Present(); return Handle();
        }

        private void StartVoice(double now) => _voice = _director.TryStart(AudioBus.Voice, _mode == Boat1MediaMode.Choice ? ChoiceVoiceId : HelpVoiceId, now);
        private void EnsureLoops(double now)
        {
            if (_music == null && !_director.TryGetActive(AudioBus.Music, MusicId, out _music)) _music = _director.TryStart(AudioBus.Music, MusicId, now);
            if (_ambience == null && !_director.TryGetActive(AudioBus.Ambience, AmbienceId, out _ambience)) _ambience = _director.TryStart(AudioBus.Ambience, AmbienceId, now);
        }
        private void StopVoice() { if (_voice != null) _director.Complete(_voice.Id); _voice = null; }
        private void StopLoops() { if (_music != null) _director.Complete(_music.Id); if (_ambience != null) _director.Complete(_ambience.Id); _music = null; _ambience = null; }
        private void StopSfx()
        {
            if (_arrivalSfx != null) _director.Complete(_arrivalSfx.Id);
            if (_ibisSfx != null) _director.Complete(_ibisSfx.Id);
            _arrivalSfx = null; _ibisSfx = null;
            _arrivalSfxStartedAt = 0d; _ibisSfxStartedAt = 0d;
        }
        private bool CanPlaySfx(Guid routeToken, double now) => _active && !_paused
            && routeToken == _routeToken && _session.SoundEnabled && Finite(now);
        private void CompleteExpiredSfx(double now)
        {
            if (_arrivalSfx != null && now - _arrivalSfxStartedAt >= _content.ArrivalSfx.length)
            { _director.Complete(_arrivalSfx.Id); _arrivalSfx = null; _arrivalSfxStartedAt = 0d; }
            if (_ibisSfx != null && now - _ibisSfxStartedAt >= _content.IbisSfx.length)
            { _director.Complete(_ibisSfx.Id); _ibisSfx = null; _ibisSfxStartedAt = 0d; }
        }
        private void Present()
        {
            ResolveCue(_mode, _elapsed, out var id, out var start, out var visible);
            _sink.PresentMediaCue(new Boat1MediaCue(_routeToken, _mediaToken, _mode,
                _content.PresentationSprites[_mode == Boat1MediaMode.Choice ? 2 : 3], id,
                _accessibility.CaptionsEnabled && visible, _startedAt + start, _elapsed));
        }
        private Boat1MediaHandle Handle() => new Boat1MediaHandle(_routeToken, _mediaToken, _voice?.Id, _music?.Id, _ambience?.Id);
        private bool Current(Guid route, Guid media) => _active && route == _routeToken && media == _mediaToken;
        private Guid NextId() { var id = _idFactory(); if (id == Guid.Empty || !_issued.Add(id)) throw new InvalidOperationException("Boat 1 media IDs must be unique."); return id; }
        private static double Duration(Boat1MediaMode mode) => mode == Boat1MediaMode.Choice ? 7.078d : 6.635d;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static void Register(IAudioClipRegistry registry, string id, AudioClip clip, bool loop)
        { if (!(registry is IAudioClipRegistryLookup lookup) || !lookup.IsClipRegistered(id)) registry.RegisterClip(id, clip, loop); }

        private static void ResolveCue(Boat1MediaMode mode, double elapsed, out string id, out double start, out bool visible)
        {
            id = null; start = elapsed; visible = false;
            if (mode == Boat1MediaMode.Choice)
            {
                if (At(elapsed, 0) && Before(elapsed, 2.411)) { id = ChoiceCaption1Id; start = 0; visible = true; }
                else if (At(elapsed, 2.561) && Before(elapsed, 4.767)) { id = ChoiceCaption2Id; start = 2.561; visible = true; }
                else if (At(elapsed, 4.917) && Before(elapsed, 7.078)) { id = ChoiceCaption3Id; start = 4.917; visible = true; }
            }
            else if (At(elapsed, 0) && Before(elapsed, 4.225)) { id = HelpCaption1Id; start = 0; visible = true; }
            else if (At(elapsed, 4.375) && Before(elapsed, 6.635)) { id = HelpCaption2Id; start = 4.375; visible = true; }
        }
        private static bool At(double value, double boundary) => value >= boundary - 0.000000001d;
        private static bool Before(double value, double boundary) => !At(value, boundary);
    }

    public static class Boat1CaptionStrings
    {
        private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Boat1MediaPresenter.ChoiceCaption1Id] = "Choose a landmark,",
            [Boat1MediaPresenter.ChoiceCaption2Id] = "follow the bird,",
            [Boat1MediaPresenter.ChoiceCaption3Id] = "or tap Skip.",
            [Boat1MediaPresenter.HelpCaption1Id] = "Watch the bird and the direction sign.",
            [Boat1MediaPresenter.HelpCaption2Id] = "Now you choose."
        };
        public static string ResolveEnglish(string id) => string.IsNullOrEmpty(id) ? string.Empty
            : English.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException(id);
    }
}
