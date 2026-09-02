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
    public sealed class Boat1MediaTests
    {
        [Test]
        public void ChoiceAndHelpUseExactCaptionIntervalsAndSilentGaps()
        {
            var c = Create(true, true); var token = Guid.NewGuid(); var h = c.Presenter.StartChoice(token, 10);
            AssertCue(c, Boat1MediaPresenter.ChoiceCaption1Id, true, 10);
            c.Presenter.Advance(token, h.MediaTokenId, 12.411); AssertCue(c, null, false, 12.411);
            c.Presenter.Advance(token, h.MediaTokenId, 12.561); AssertCue(c, Boat1MediaPresenter.ChoiceCaption2Id, true, 12.561);
            c.Presenter.Advance(token, h.MediaTokenId, 14.767); AssertCue(c, null, false, 14.767);
            c.Presenter.Advance(token, h.MediaTokenId, 14.917); AssertCue(c, Boat1MediaPresenter.ChoiceCaption3Id, true, 14.917);
            h = c.Presenter.StartHelp(token, 20);
            AssertCue(c, Boat1MediaPresenter.HelpCaption1Id, true, 20);
            c.Presenter.Advance(token, h.MediaTokenId, 24.225); AssertCue(c, null, false, 24.225);
            c.Presenter.Advance(token, h.MediaTokenId, 24.375); AssertCue(c, Boat1MediaPresenter.HelpCaption2Id, true, 24.375);
            Assert.That(c.Backend.Ducks.Any(d => d.MusicDb == -6f), Is.True);
        }

        [Test]
        public void MutedKeepsVisualEquivalentAndRepeatReplacesTokensWithoutDuplicateLoops()
        {
            var muted = Create(false, false); var token = Guid.NewGuid(); var mh = muted.Presenter.StartChoice(token, 0);
            Assert.That(mh.VoiceTokenId, Is.Null); Assert.That(muted.Backend.Started, Is.Empty);
            Assert.That(muted.Sink.Last.VisualEquivalent, Is.SameAs(Content().PresentationSprites[2])); Assert.That(muted.Sink.Last.CaptionVisible, Is.False);
            var c = Create(true, true); var first = c.Presenter.StartChoice(token, 1); var second = c.Presenter.Repeat(token, 2);
            Assert.That(second.MediaTokenId, Is.Not.EqualTo(first.MediaTokenId)); Assert.That(second.VoiceTokenId, Is.Not.EqualTo(first.VoiceTokenId));
            Assert.That(c.Backend.Started.Count(x => x.semantic == Boat1MediaPresenter.MusicId), Is.EqualTo(1));
            Assert.That(c.Backend.Started.Count(x => x.semantic == Boat1MediaPresenter.AmbienceId), Is.EqualTo(1));
            Assert.That(c.Presenter.Advance(token, first.MediaTokenId, 3), Is.False);
        }

        [Test]
        public void FollowIbisAndArrivalFeedbackUseSfxBusOnceAndMutedStartsNeither()
        {
            var token = Guid.NewGuid();
            var audible = Create(true, true);
            audible.Presenter.StartChoice(token, 10d);

            Assert.That(audible.Presenter.PlayIbisCueSfx(token, 10.1d), Is.True);
            Assert.That(audible.Presenter.PlayArrivalSfx(token, 16.1d), Is.True);
            Assert.That(audible.Backend.Started.Count(item => item.bus == AudioBus.Sfx), Is.EqualTo(2));
            Assert.That(audible.Backend.Started.Count(item => item.semantic == Boat1MediaPresenter.IbisSfxId), Is.EqualTo(1));
            Assert.That(audible.Backend.Started.Count(item => item.semantic == Boat1MediaPresenter.ArrivalSfxId), Is.EqualTo(1));
            Assert.That(audible.Presenter.PlayArrivalSfx(token, 16.2d), Is.False);

            var muted = Create(false, true);
            muted.Presenter.StartChoice(token, 20d);
            Assert.That(muted.Presenter.PlayIbisCueSfx(token, 20.1d), Is.False);
            Assert.That(muted.Presenter.PlayArrivalSfx(token, 26.1d), Is.False);
            Assert.That(muted.Backend.Started, Is.Empty);
        }

        [Test]
        public void ArrivalSfxPreservesItsExactRemainingDurationAcrossMappedPause()
        {
            var token = Guid.NewGuid();
            var context = Create(true, true);
            var handle = context.Presenter.StartChoice(token, 0d);
            Assert.That(context.Presenter.PlayArrivalSfx(token, 1d), Is.True);
            Assert.That(context.Presenter.Pause(
                token, handle.MediaTokenId, 1.2d, Boat1PauseReason.AppBackground), Is.True);
            Assert.That(context.Presenter.ResumeAfterMappedUserAction(token, 10d).HasValue, Is.True);

            Assert.That(context.Presenter.Advance(token, context.Presenter.CurrentMediaTokenId, 10.499d), Is.True);
            Assert.That(context.Presenter.ArrivalSfxPlaying, Is.True);
            Assert.That(context.Presenter.Advance(token, context.Presenter.CurrentMediaTokenId, 10.5d), Is.True);
            Assert.That(context.Presenter.ArrivalSfxPlaying, Is.False);
        }

        [Test]
        public void PauseFreezesAndMappedResumeUsesFreshMediaTokenAtExactBeat()
        {
            var c = Create(true, true); var token = Guid.NewGuid(); var h = c.Presenter.StartChoice(token, 10);
            c.Presenter.Advance(token, h.MediaTokenId, 13); Assert.That(c.Presenter.Pause(token, h.MediaTokenId, 13, Boat1PauseReason.AppBackground), Is.True);
            Assert.That(c.Sink.Frozen, Is.EqualTo(3)); Assert.That(c.Sink.Locked, Is.True); Assert.That(c.Presenter.Advance(token, h.MediaTokenId, 14), Is.False);
            var resumed = c.Presenter.ResumeAfterMappedUserAction(token, 100); Assert.That(resumed.HasValue, Is.True);
            Assert.That(resumed.Value.MediaTokenId, Is.Not.EqualTo(h.MediaTokenId)); Assert.That(c.Presenter.ElapsedSeconds, Is.EqualTo(3));
            Assert.That(c.Sink.Last.CaptionCueStartSeconds, Is.EqualTo(99.561).Within(0.100)); Assert.That(c.Backend.ResumeCount, Is.EqualTo(1));
        }

        private static void AssertCue(Context c, string id, bool visible, double start) { Assert.That(c.Sink.Last.CaptionId, Is.EqualTo(id)); Assert.That(c.Sink.Last.CaptionVisible, Is.EqualTo(visible)); Assert.That(c.Sink.Last.CaptionCueStartSeconds, Is.EqualTo(start).Within(0.100)); }
        private static Boat1Content Content() => AssetDatabase.LoadAssetAtPath<Boat1Content>(Boat1ContentGenerator.ContentAssetPath);
        private static Context Create(bool sound, bool captions)
        {
            var backend = new Backend(); var director = new AudioDirector(backend, Ids(1)); var session = new Exloris.Core.Session.SessionState { SoundEnabled = sound }; session.Begin(RouteId.Boat1);
            var access = new AccessibilityState(false, captions); access.SetOsState(ReduceMotionOsState.NotReduced); var sink = new Sink();
            return new Context(backend, sink, new Boat1MediaPresenter(director, backend, Content(), session, access, sink, Ids(2)));
        }
        private static Func<Guid> Ids(byte prefix) { var value = 0; return () => { var b = new byte[16]; b[0] = prefix; b[15] = (byte)++value; return new Guid(b); }; }
        private sealed class Context { public Context(Backend b, Sink s, Boat1MediaPresenter p) { Backend=b; Sink=s; Presenter=p; } public Backend Backend {get;} public Sink Sink {get;} public Boat1MediaPresenter Presenter {get;} }
        private sealed class Sink : IBoat1MediaCueSink
        {
            public Boat1MediaCue Last { get; private set; } public double Frozen {get; private set;} public bool Locked {get; private set;}
            public void PresentMediaCue(Boat1MediaCue cue) => Last=cue; public void FreezeMediaCue(double e)=>Frozen=e; public void ResumeMediaCue(Guid id,double e){} public void SetCallerInputLocked(bool v)=>Locked=v; public void ShowPauseOverlay(Boat1PauseReason r){}
        }
        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _registered = new HashSet<string>(); public List<(Guid id,string semantic,AudioBus bus)> Started {get;}=new List<(Guid,string,AudioBus)>(); public List<AudioDuckState> Ducks {get;}=new List<AudioDuckState>(); public int ResumeCount {get;private set;}
            public bool IsClipRegistered(string id)=>_registered.Contains(id); public void RegisterClip(string id,AudioClip c,bool loop=false)=>_registered.Add(id); public void Start(AudioPlaybackToken t,AudioBus b)=>Started.Add((t.Id,t.SemanticId,b)); public void Stop(Guid id){} public void PauseAllImmediately(){} public void SeekAll(double t){} public void ResumeAll(float v,float r)=>ResumeCount++; public void SetMasterVolume(float v){} public void ApplyDuck(AudioDuckState d)=>Ducks.Add(d);
        }
    }
}
