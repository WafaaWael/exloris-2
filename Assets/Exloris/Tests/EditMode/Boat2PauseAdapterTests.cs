using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat2PauseAdapterTests
    {
        [Test]
        public void AdapterCapturesExactTravelBeatAndMappedResumeUsesFreshMediaToken()
        {
            var c = Create(); c.Controller.Enter(0, false); var token = c.Controller.CurrentTokenId;
            var mediaHandle = c.Media.StartChoice(token, 0); c.Controller.HandleChoice(token, Boat2Choice.Dock, 1); c.Controller.Advance(token, 3.5);
            c.Motion.Present(c.Controller.Snapshot, false, 1000); c.Adapter.BindMediaHandle(mediaHandle);
            Assert.That(c.Adapter.PreparePause(3.5, Boat2PauseReason.Manual), Is.True);
            Assert.That(c.Adapter.TryCapture(out var handle), Is.True); var captured = c.Adapter.CapturedSnapshot;
            Assert.That(c.Adapter.PauseMedia(handle, 3.5), Is.True); Assert.That(c.Controller.HandlePause(), Is.True);
            var oldMedia = c.Media.CurrentMediaTokenId; Assert.That(c.Adapter.Restore(handle, 100), Is.True);
            Assert.That(c.Controller.Snapshot, Is.EqualTo(captured)); Assert.That(c.Adapter.ResumeMedia(handle, 100), Is.True);
            Assert.That(c.Media.CurrentMediaTokenId, Is.Not.EqualTo(oldMedia)); Assert.That(c.Controller.Snapshot.TravelElapsedSeconds, Is.EqualTo(2.5));
            Assert.That(c.Adapter.ResumeMedia(new PauseCallerHandle(RouteId.Boat2, token, Guid.NewGuid()), 101), Is.False);
        }

        [Test]
        public void RejectedPreparedPauseLeavesMediaAndMotionUntouched()
        {
            var c = Create(); c.Controller.Enter(0, false); var token = c.Controller.CurrentTokenId;
            var mediaHandle = c.Media.StartChoice(token, 0); c.Adapter.BindMediaHandle(mediaHandle);

            Assert.That(c.Adapter.PreparePause(1d, Boat2PauseReason.AppBackground), Is.False);
            Assert.That(c.Media.IsPaused, Is.False);
            Assert.That(c.Controller.State, Is.EqualTo(Boat2State.Choice));
        }

        private static Context Create()
        {
            var session = new Exloris.Core.Session.SessionState(); session.Begin(RouteId.Boat2);
            var nav = new NavigationAuthority(RouteId.Boat2, Ids(1)); var pause = new Pause();
            var controller = new Boat2Controller(session, nav, new View(), pause, new Leave(), Ids(2));
            var backend = new Backend(); var director = new AudioDirector(backend, Ids(3)); var access = new AccessibilityState(false, true);
            var media = new Boat2MediaPresenter(director, backend, Content(), session, access, new MediaSink(), Ids(4));
            var motion = new Boat2MotionPresenter(Content(), new MotionSink()); var adapter = new Boat2PauseAdapter(controller, media, motion, Ids(5));
            return new Context(controller, media, motion, adapter);
        }
        private static Boat2Content Content() => AssetDatabase.LoadAssetAtPath<Boat2Content>("Assets/Exloris/Resources/Content/Boat2Content.asset");
        private static Func<Guid> Ids(byte prefix) { var value=0; return () => { var b=new byte[16]; b[0]=prefix; b[15]=(byte)++value; return new Guid(b); }; }
        private readonly struct Context { public Context(Boat2Controller c,Boat2MediaPresenter m,Boat2MotionPresenter o,Boat2PauseAdapter a){Controller=c;Media=m;Motion=o;Adapter=a;} public Boat2Controller Controller{get;} public Boat2MediaPresenter Media{get;} public Boat2MotionPresenter Motion{get;} public Boat2PauseAdapter Adapter{get;} }
        private sealed class View : IBoat2View { public void Present(Boat2Snapshot s,bool r){} public void SetInputReady(bool r){} }
        private sealed class Pause : IBoat2PauseSink { public bool TryShow(Boat2Snapshot s)=>true; }
        private sealed class Leave : ILeaveConfirmationSink { public bool TryShow(RouteId r)=>true; }
        private sealed class MotionSink : IBoat2MotionSink { public void PresentMotion(Boat2MotionFrame f){} }
        private sealed class MediaSink : IBoat2MediaCueSink { public void PresentMediaCue(Boat2MediaCue c){} public void FreezeMediaCue(double e){} public void ResumeMediaCue(Guid i,double e){} public void SetCallerInputLocked(bool l){} public void ShowPauseOverlay(Boat2PauseReason r){} }
        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> ids=new HashSet<string>(); public bool IsClipRegistered(string id)=>ids.Contains(id); public void RegisterClip(string id,AudioClip c,bool l=false)=>ids.Add(id); public void Start(AudioPlaybackToken t,AudioBus b){} public void Stop(Guid i){} public void PauseAllImmediately(){} public void SeekAll(double t){} public void ResumeAll(float v,float r){} public void SetMasterVolume(float v){} public void ApplyDuck(AudioDuckState s){}
        }
    }
}
