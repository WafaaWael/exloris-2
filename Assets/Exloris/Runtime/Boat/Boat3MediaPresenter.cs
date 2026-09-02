using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using UnityEngine;

namespace Exloris.Runtime.Boat
{
    public enum Boat3MediaMode { Choice, Help }

    public readonly struct Boat3MediaCue
    {
        public Boat3MediaCue(Guid route, Guid media, Boat3MediaMode mode, Sprite visual, string captionId, bool visible, double start, double elapsed)
        { RouteTokenId=route; MediaTokenId=media; Mode=mode; VisualEquivalent=visual; CaptionId=captionId; CaptionVisible=visible; CaptionCueStartSeconds=start; ElapsedSeconds=elapsed; }
        public Guid RouteTokenId{get;} public Guid MediaTokenId{get;} public Boat3MediaMode Mode{get;} public Sprite VisualEquivalent{get;}
        public string CaptionId{get;} public bool CaptionVisible{get;} public double CaptionCueStartSeconds{get;} public double ElapsedSeconds{get;}
        public bool RequiresReading=>false; public bool UsesNegativeSfx=>false;
    }

    public readonly struct Boat3MediaHandle
    {
        public Boat3MediaHandle(Guid route,Guid media,Guid? voice,Guid? music,Guid? ambience){RouteTokenId=route;MediaTokenId=media;VoiceTokenId=voice;MusicTokenId=music;AmbienceTokenId=ambience;}
        public Guid RouteTokenId{get;} public Guid MediaTokenId{get;} public Guid? VoiceTokenId{get;} public Guid? MusicTokenId{get;} public Guid? AmbienceTokenId{get;}
    }

    public interface IBoat3MediaCueSink
    {
        void PresentMediaCue(Boat3MediaCue cue);
        void FreezeMediaCue(double elapsedSeconds);
        void ResumeMediaCue(Guid mediaTokenId,double elapsedSeconds);
        void SetCallerInputLocked(bool locked);
        void ShowPauseOverlay(Boat3PauseReason reason);
    }

    public sealed class Boat3MediaPresenter
    {
        public const string ChoiceVoiceId="VO-BOAT-CHOICE", HelpVoiceId="VO-BOAT-HELP", MusicId="MUS-MAIN-JOURNEY-LOOP", AmbienceId="AMB-BOAT-WATER-LOOP", ArrivalSfxId="SFX-BOAT-ARRIVAL", IbisSfxId="SFX-IBIS-CUE";
        public const string ChoiceCaption1Id="VO-BOAT-CHOICE-C01", ChoiceCaption2Id="VO-BOAT-CHOICE-C02", ChoiceCaption3Id="VO-BOAT-CHOICE-C03", HelpCaption1Id="VO-BOAT-HELP-C01", HelpCaption2Id="VO-BOAT-HELP-C02";
        private readonly AudioDirector _director; private readonly Boat3Content _content; private readonly SessionState _session; private readonly AccessibilityState _accessibility; private readonly IBoat3MediaCueSink _sink; private readonly Func<Guid> _ids; private readonly HashSet<Guid> _issued=new HashSet<Guid>();
        private Guid _routeToken,_mediaToken; private AudioPlaybackToken _voice,_music,_ambience,_arrival,_ibis; private Boat3MediaMode _mode; private double _startedAt,_elapsed,_pausedAt,_arrivalAt,_ibisAt; private bool _active,_paused;

        public Boat3MediaPresenter(AudioDirector director,IAudioClipRegistry registry,Boat3Content content,SessionState session,AccessibilityState accessibility,IBoat3MediaCueSink sink,Func<Guid> idFactory=null)
        {
            _director=director??throw new ArgumentNullException(nameof(director)); if(registry==null)throw new ArgumentNullException(nameof(registry)); _content=content??throw new ArgumentNullException(nameof(content)); _session=session??throw new ArgumentNullException(nameof(session)); _accessibility=accessibility??throw new ArgumentNullException(nameof(accessibility)); _sink=sink??throw new ArgumentNullException(nameof(sink)); _ids=idFactory??Guid.NewGuid;
            Register(registry,ChoiceVoiceId,content.VoiceClips[0],false); Register(registry,HelpVoiceId,content.VoiceClips[1],false); Register(registry,MusicId,content.Music,true); Register(registry,AmbienceId,content.Ambience,true); Register(registry,ArrivalSfxId,content.ArrivalSfx,false); Register(registry,IbisSfxId,content.IbisSfx,false);
        }

        public double ElapsedSeconds=>_elapsed; public Guid CurrentMediaTokenId=>_mediaToken; public bool IsPaused=>_paused;
        public bool ArrivalSfxPlaying => _arrival != null;
        public bool IbisSfxPlaying => _ibis != null;
        public Boat3MediaHandle StartChoice(Guid route,double now)=>Start(route,Boat3MediaMode.Choice,now);
        public Boat3MediaHandle StartHelp(Guid route,double now)=>Start(route,Boat3MediaMode.Help,now);
        public Boat3MediaHandle Repeat(Guid route,double now){if(!_active||route!=_routeToken)throw new InvalidOperationException("Cannot repeat stale Boat 3 media.");return Start(route,_mode,now);}

        public bool Advance(Guid route,Guid media,double now)
        {
            if(!Current(route,media)||_paused||!Finite(now)||now<_startedAt)return false; CompleteExpiredSfx(now); _elapsed=Math.Min(Duration(_mode),now-_startedAt); Present(); return true;
        }
        public bool PlayArrivalSfx(Guid route,double now)=>PlaySfx(route,now,ArrivalSfxId,ref _arrival,ref _arrivalAt);
        public bool PlayIbisCueSfx(Guid route,double now)=>PlaySfx(route,now,IbisSfxId,ref _ibis,ref _ibisAt);
        public bool Pause(Guid route,Guid media,double now,Boat3PauseReason reason)
        {
            if(!Current(route,media)||_paused||!Advance(route,media,now))return false; _paused=true;_pausedAt=now;_director.PauseAndDuckImmediately();_sink.FreezeMediaCue(_elapsed);_sink.SetCallerInputLocked(true);_sink.ShowPauseOverlay(reason);return true;
        }
        public bool CanPause(Guid route, Guid media, double now) => Current(route, media)
            && !_paused && Finite(now) && now >= _startedAt;
        public Boat3MediaHandle? ResumeAfterMappedUserAction(Guid route,double now)
        {
            if(!_active||!_paused||route!=_routeToken||!Finite(now)||now<_pausedAt)return null;var pause=now-_pausedAt;if(_arrival!=null)_arrivalAt+=pause;if(_ibis!=null)_ibisAt+=pause;_mediaToken=Next();_startedAt=now-_elapsed;_director.ResumeAfterMappedUserAction();_paused=false;_sink.ResumeMediaCue(_mediaToken,_elapsed);_sink.SetCallerInputLocked(false);Present();return Handle();
        }
        public void RefreshSettings(double now)
        {
            if(!_active)return;if(!_session.SoundEnabled){StopVoice();StopLoops();StopSfx();}else if(!_paused){EnsureLoops(now);if(_voice==null&&_elapsed<Duration(_mode))StartVoice(now);}Present();
        }
        public bool Stop(Guid route){if(!_active||route!=_routeToken)return false;StopVoice();StopLoops();StopSfx();_active=false;_paused=false;_routeToken=Guid.Empty;_mediaToken=Guid.Empty;_sink.SetCallerInputLocked(true);return true;}

        private Boat3MediaHandle Start(Guid route,Boat3MediaMode mode,double now)
        {
            if(route==Guid.Empty||!Finite(now))throw new ArgumentOutOfRangeException();StopVoice();_routeToken=route;_mode=mode;_mediaToken=Next();_startedAt=now;_elapsed=0;_active=true;_paused=false;_sink.SetCallerInputLocked(false);if(_session.SoundEnabled){EnsureLoops(now);StartVoice(now);}else StopLoops();Present();return Handle();
        }
        private bool PlaySfx(Guid route,double now,string semantic,ref AudioPlaybackToken token,ref double started)
        {
            if(!_active||_paused||route!=_routeToken||!_session.SoundEnabled||!Finite(now)||token!=null)return false;token=_director.TryStart(AudioBus.Sfx,semantic,now);if(token!=null)started=now;return token!=null;
        }
        private void StartVoice(double now)=>_voice=_director.TryStart(AudioBus.Voice,_mode==Boat3MediaMode.Choice?ChoiceVoiceId:HelpVoiceId,now);
        private void EnsureLoops(double now)
        {
            if(_music==null&&!_director.TryGetActive(AudioBus.Music,MusicId,out _music))_music=_director.TryStart(AudioBus.Music,MusicId,now);
            if(_ambience==null&&!_director.TryGetActive(AudioBus.Ambience,AmbienceId,out _ambience))_ambience=_director.TryStart(AudioBus.Ambience,AmbienceId,now);
        }
        private void CompleteExpiredSfx(double now)
        {
            if(_arrival!=null&&now-_arrivalAt>=_content.ArrivalSfx.length){_director.Complete(_arrival.Id);_arrival=null;_arrivalAt=0;}
            if(_ibis!=null&&now-_ibisAt>=_content.IbisSfx.length){_director.Complete(_ibis.Id);_ibis=null;_ibisAt=0;}
        }
        private void StopVoice(){if(_voice!=null)_director.Complete(_voice.Id);_voice=null;}
        private void StopLoops(){if(_music!=null)_director.Complete(_music.Id);if(_ambience!=null)_director.Complete(_ambience.Id);_music=null;_ambience=null;}
        private void StopSfx(){if(_arrival!=null)_director.Complete(_arrival.Id);if(_ibis!=null)_director.Complete(_ibis.Id);_arrival=null;_ibis=null;_arrivalAt=0;_ibisAt=0;}
        private void Present()
        {
            ResolveCue(_mode,_elapsed,out var id,out var start,out var visible);_sink.PresentMediaCue(new Boat3MediaCue(_routeToken,_mediaToken,_mode,_content.PresentationSprites[_mode==Boat3MediaMode.Choice?2:3],id,_accessibility.CaptionsEnabled&&visible,_startedAt+start,_elapsed));
        }
        private Boat3MediaHandle Handle()=>new Boat3MediaHandle(_routeToken,_mediaToken,_voice?.Id,_music?.Id,_ambience?.Id);
        private bool Current(Guid route,Guid media)=>_active&&route==_routeToken&&media==_mediaToken;
        private Guid Next(){var id=_ids();if(id==Guid.Empty||!_issued.Add(id))throw new InvalidOperationException("Boat 3 media IDs must be unique.");return id;}
        private static double Duration(Boat3MediaMode mode)=>mode==Boat3MediaMode.Choice?7.078d:6.635d;
        private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        private static void Register(IAudioClipRegistry registry,string id,AudioClip clip,bool loop){if(!(registry is IAudioClipRegistryLookup lookup)||!lookup.IsClipRegistered(id))registry.RegisterClip(id,clip,loop);}
        private static void ResolveCue(Boat3MediaMode mode,double elapsed,out string id,out double start,out bool visible)
        {
            id=null;start=elapsed;visible=false;
            if(mode==Boat3MediaMode.Choice){if(At(elapsed,0)&&Before(elapsed,2.411)){id=ChoiceCaption1Id;start=0;visible=true;}else if(At(elapsed,2.561)&&Before(elapsed,4.767)){id=ChoiceCaption2Id;start=2.561;visible=true;}else if(At(elapsed,4.917)&&Before(elapsed,7.078)){id=ChoiceCaption3Id;start=4.917;visible=true;}}
            else if(At(elapsed,0)&&Before(elapsed,4.225)){id=HelpCaption1Id;start=0;visible=true;}else if(At(elapsed,4.375)&&Before(elapsed,6.635)){id=HelpCaption2Id;start=4.375;visible=true;}
        }
        private static bool At(double value,double boundary)=>value>=boundary-0.000000001d; private static bool Before(double value,double boundary)=>!At(value,boundary);
    }

    public static class Boat3CaptionStrings
    {
        private static readonly IReadOnlyDictionary<string,string> English=new Dictionary<string,string>(StringComparer.Ordinal)
        {
            [Boat3MediaPresenter.ChoiceCaption1Id]="Choose a landmark,",[Boat3MediaPresenter.ChoiceCaption2Id]="follow the bird,",[Boat3MediaPresenter.ChoiceCaption3Id]="or tap Skip.",
            [Boat3MediaPresenter.HelpCaption1Id]="Watch the bird and the direction sign.",[Boat3MediaPresenter.HelpCaption2Id]="Now you choose."
        };
        public static string ResolveEnglish(string id)=>string.IsNullOrEmpty(id)?string.Empty:English.TryGetValue(id,out var value)?value:throw new KeyNotFoundException(id);
    }
}
