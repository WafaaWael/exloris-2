using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Editor;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CoreSessionState = Exloris.Core.Session.SessionState;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2PauseAdapterTests
    {
        [Test]
        public void AdapterCapturesExactConsequenceAndMappedResumeUsesFreshMediaToken()
        {
            var c = Create(); var token = c.Controller.CurrentTokenId;
            var mediaHandle = c.Media.StartIntro(token, 0d); c.Adapter.BindMediaHandle(mediaHandle);
            c.Controller.HandlePlay(token, 0.1d);
            c.Controller.HandleSelectRiver(token, Stage2River.Middle, Guid.NewGuid(), 0.2d);
            c.Controller.HandleHotspot(token, Stage2Hotspot.Path, Guid.NewGuid(), 0.3d);
            c.Outcome.Start(c.Controller.Snapshot, false, 0.3d);
            c.Outcome.Advance(token, c.Controller.CurrentConsequenceTokenId, 1.3d);
            Assert.That(c.Adapter.PreparePause(1.3d, Stage2PauseReason.User), Is.True);
            Assert.That(c.Adapter.TryCapture(out var handle), Is.True);
            var captured = c.Adapter.CapturedSnapshot;
            Assert.That(c.Adapter.PauseMedia(handle, 1.3d), Is.True);
            Assert.That(c.Controller.HandlePause(1.3d), Is.True);
            var oldMedia = c.Media.CurrentMediaTokenId;
            Assert.That(c.Adapter.Restore(handle, 100d), Is.True);
            Assert.That(c.Controller.Snapshot.State, Is.EqualTo(captured.State));
            Assert.That(c.Controller.Snapshot.SelectedRiver, Is.EqualTo(captured.SelectedRiver));
            Assert.That(c.Controller.Snapshot.CurrentHotspot, Is.EqualTo(captured.CurrentHotspot));
            Assert.That(c.Controller.Snapshot.ConsequenceTokenId, Is.EqualTo(captured.ConsequenceTokenId));
            Assert.That(c.Controller.Snapshot.ConsequenceElapsedSeconds, Is.EqualTo(captured.ConsequenceElapsedSeconds));
            Assert.That(c.Adapter.ResumeMedia(handle, 100d), Is.True);
            Assert.That(c.Media.CurrentMediaTokenId, Is.Not.EqualTo(oldMedia));
            Assert.That(c.Outcome.ElapsedSeconds, Is.EqualTo(1d).Within(0.001d));
        }

        [Test]
        public void RejectedPreparedPauseHasNoMediaOrOutcomeSideEffects()
        {
            var c = Create(); var token = c.Controller.CurrentTokenId;
            c.Adapter.BindMediaHandle(c.Media.StartIntro(token, 0d));
            Assert.That(c.Adapter.PreparePause(-1d, Stage2PauseReason.AppBackground), Is.False);
            Assert.That(c.Media.IsPaused, Is.False);
            Assert.That(c.Controller.State, Is.EqualTo(Stage2State.Intro));
        }

        private static Context Create()
        {
            var session = new CoreSessionState(); session.Begin(RouteId.Boat2);
            var navigation = new NavigationAuthority(RouteId.Boat2, Ids(1));
            var transaction = navigation.Request(RouteId.Stage2, 0d).Transaction;
            var controller = new Stage2Controller(session, navigation, new View(), new Pause(), new Leave(), Ids(2));
            Assert.That(controller.Enter(transaction.Id, 0d, false), Is.True);
            var backend = new Backend(); var director = new AudioDirector(backend, Ids(3));
            var media = new Stage2MediaPresenter(director, backend, Content(), session,
                new AccessibilityState(false, true), new MediaSink(), Ids(4));
            var outcome = new Stage2OutcomePresenter(Content(), new OutcomeSink());
            return new Context(controller, media, outcome,
                new Stage2PauseAdapter(controller, media, outcome, Ids(5)));
        }

        private static Stage2Content Content() => AssetDatabase.LoadAssetAtPath<Stage2Content>(Stage2ContentGenerator.ContentAssetPath);
        private static Func<Guid> Ids(byte prefix) { var value = 0; return () => { var bytes = new byte[16]; bytes[0] = prefix; bytes[15] = (byte)++value; return new Guid(bytes); }; }
        private readonly struct Context
        {
            internal Context(Stage2Controller c, Stage2MediaPresenter m, Stage2OutcomePresenter o, Stage2PauseAdapter a)
            { Controller = c; Media = m; Outcome = o; Adapter = a; }
            internal Stage2Controller Controller { get; }
            internal Stage2MediaPresenter Media { get; }
            internal Stage2OutcomePresenter Outcome { get; }
            internal Stage2PauseAdapter Adapter { get; }
        }
        private sealed class View : IStage2View { public void Present(Stage2Snapshot s, bool r) { } public void SetInputReady(bool r) { } }
        private sealed class Pause : IStage2PauseSink { public bool TryShow(Stage2Snapshot s) => true; }
        private sealed class Leave : ILeaveConfirmationSink { public bool TryShow(RouteId r) => true; }
        private sealed class OutcomeSink : IStage2OutcomeSink { public void PresentOutcome(Stage2OutcomeFrame f) { } }
        private sealed class MediaSink : IStage2MediaCueSink
        { public void PresentMediaCue(Stage2MediaCue c) { } public void FreezeMediaCue(double e) { } public void ResumeMediaCue(Guid i, double e) { } public void SetCallerInputLocked(bool l) { } public void ShowPauseOverlay(Stage2PauseReason r) { } }
        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> ids = new HashSet<string>();
            public bool IsClipRegistered(string id) => ids.Contains(id);
            public void RegisterClip(string id, AudioClip c, bool l = false) => ids.Add(id);
            public void Start(AudioPlaybackToken t, AudioBus b) { } public void Stop(Guid i) { }
            public void PauseAllImmediately() { } public void SeekAll(double t) { }
            public void ResumeAll(float v, float r) { } public void SetMasterVolume(float v) { }
            public void ApplyDuck(AudioDuckState s) { }
        }
    }
}
