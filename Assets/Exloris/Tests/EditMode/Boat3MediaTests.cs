using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Editor;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Boat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3MediaTests
    {
        [Test]
        public void ChoiceAndHelpUseExactCaptionIntervalsAndSilentGaps()
        {
            var c = Create(true,true); var token = Guid.NewGuid(); var h = c.Presenter.StartChoice(token,10);
            AssertCue(c,Boat3MediaPresenter.ChoiceCaption1Id,true,10);
            c.Presenter.Advance(token,h.MediaTokenId,12.411); AssertCue(c,null,false,12.411);
            c.Presenter.Advance(token,h.MediaTokenId,12.561); AssertCue(c,Boat3MediaPresenter.ChoiceCaption2Id,true,12.561);
            c.Presenter.Advance(token,h.MediaTokenId,14.917); AssertCue(c,Boat3MediaPresenter.ChoiceCaption3Id,true,14.917);
            h = c.Presenter.StartHelp(token,20); AssertCue(c,Boat3MediaPresenter.HelpCaption1Id,true,20);
            c.Presenter.Advance(token,h.MediaTokenId,24.225); AssertCue(c,null,false,24.225);
            c.Presenter.Advance(token,h.MediaTokenId,24.375); AssertCue(c,Boat3MediaPresenter.HelpCaption2Id,true,24.375);
        }

        [Test]
        public void MutedKeepsVisualMeaningAndStartsNoAudio()
        {
            var c = Create(false,true); var token = Guid.NewGuid(); var h = c.Presenter.StartChoice(token,0);
            Assert.That(h.VoiceTokenId,Is.Null); Assert.That(c.Backend.Started,Is.Empty);
            Assert.That(c.Sink.Last.VisualEquivalent,Is.SameAs(Content().PresentationSprites[2]));
            Assert.That(c.Sink.Last.CaptionVisible,Is.True);
            Assert.That(c.Presenter.PlayIbisCueSfx(token,1),Is.False); Assert.That(c.Presenter.PlayArrivalSfx(token,6),Is.False);
        }

        [Test]
        public void RepeatReplacesTokensWithoutDuplicatingLoopsAndSfxPlayOnce()
        {
            var c = Create(true,true); var token = Guid.NewGuid(); var first = c.Presenter.StartChoice(token,1); var second = c.Presenter.Repeat(token,2);
            Assert.That(second.MediaTokenId,Is.Not.EqualTo(first.MediaTokenId)); Assert.That(c.Presenter.Advance(token,first.MediaTokenId,3),Is.False);
            Assert.That(c.Backend.Started.Count(x=>x.semantic==Boat3MediaPresenter.MusicId),Is.EqualTo(1));
            Assert.That(c.Backend.Started.Count(x=>x.semantic==Boat3MediaPresenter.AmbienceId),Is.EqualTo(1));
            Assert.That(c.Presenter.PlayIbisCueSfx(token,2.1),Is.True); Assert.That(c.Presenter.PlayIbisCueSfx(token,2.2),Is.False);
            Assert.That(c.Presenter.PlayArrivalSfx(token,6.1),Is.True); Assert.That(c.Presenter.PlayArrivalSfx(token,6.2),Is.False);
        }

        [Test]
        public void PauseAndMappedResumeReanchorWithFreshMediaToken()
        {
            var c=Create(true,true); var token=Guid.NewGuid(); var h=c.Presenter.StartChoice(token,10);
            c.Presenter.Advance(token,h.MediaTokenId,13); Assert.That(c.Presenter.Pause(token,h.MediaTokenId,13,Boat3PauseReason.AppBackground),Is.True);
            Assert.That(c.Sink.Frozen,Is.EqualTo(3)); Assert.That(c.Sink.Locked,Is.True); Assert.That(c.Presenter.Advance(token,h.MediaTokenId,14),Is.False);
            var resumed=c.Presenter.ResumeAfterMappedUserAction(token,100); Assert.That(resumed.HasValue,Is.True);
            Assert.That(resumed.Value.MediaTokenId,Is.Not.EqualTo(h.MediaTokenId)); Assert.That(c.Presenter.ElapsedSeconds,Is.EqualTo(3)); Assert.That(c.Backend.ResumeCount,Is.EqualTo(1));
        }

        private static void AssertCue(Context c,string id,bool visible,double start) { Assert.That(c.Sink.Last.CaptionId,Is.EqualTo(id)); Assert.That(c.Sink.Last.CaptionVisible,Is.EqualTo(visible)); Assert.That(c.Sink.Last.CaptionCueStartSeconds,Is.EqualTo(start).Within(0.100)); }
        private static Boat3Content Content()=>AssetDatabase.LoadAssetAtPath<Boat3Content>(Boat3ContentGenerator.ContentAssetPath);
        private static Context Create(bool sound,bool captions)
        {
            var backend=new Backend(); var director=new AudioDirector(backend,Ids(1)); var session=new Exloris.Core.Session.SessionState{SoundEnabled=sound}; session.Begin(RouteId.Boat3);
            var access=new AccessibilityState(false,captions); access.SetOsState(ReduceMotionOsState.NotReduced); var sink=new Sink();
            return new Context(backend,sink,new Boat3MediaPresenter(director,backend,Content(),session,access,sink,Ids(2)));
        }
        private static Func<Guid> Ids(byte prefix) { var value=0; return ()=>{var bytes=new byte[16];bytes[0]=prefix;bytes[15]=(byte)++value;return new Guid(bytes);}; }
        private sealed class Context { public Context(Backend backend,Sink sink,Boat3MediaPresenter presenter){Backend=backend;Sink=sink;Presenter=presenter;} public Backend Backend{get;} public Sink Sink{get;} public Boat3MediaPresenter Presenter{get;} }
        private sealed class Sink : IBoat3MediaCueSink
        {
            public Boat3MediaCue Last{get;private set;} public double Frozen{get;private set;} public bool Locked{get;private set;}
            public void PresentMediaCue(Boat3MediaCue cue)=>Last=cue; public void FreezeMediaCue(double elapsed)=>Frozen=elapsed; public void ResumeMediaCue(Guid token,double elapsed){} public void SetCallerInputLocked(bool locked)=>Locked=locked; public void ShowPauseOverlay(Boat3PauseReason reason){}
        }
        private sealed class Backend : IAudioBackend,IAudioClipRegistry,IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _registered=new HashSet<string>(); public List<(Guid id,string semantic,AudioBus bus)> Started{get;}=new List<(Guid,string,AudioBus)>(); public int ResumeCount{get;private set;}
            public bool IsClipRegistered(string id)=>_registered.Contains(id); public void RegisterClip(string id,AudioClip clip,bool loop=false)=>_registered.Add(id); public void Start(AudioPlaybackToken token,AudioBus bus)=>Started.Add((token.Id,token.SemanticId,bus)); public void Stop(Guid id){} public void PauseAllImmediately(){} public void SeekAll(double time){} public void ResumeAll(float volume,float ramp)=>ResumeCount++; public void SetMasterVolume(float volume){} public void ApplyDuck(AudioDuckState state){}
        }
    }
}
