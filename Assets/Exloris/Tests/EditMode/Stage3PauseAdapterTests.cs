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
using Exloris.Runtime.Stage3;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CoreSessionState = Exloris.Core.Session.SessionState;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3PauseAdapterTests
    {
        [Test]
        public void AdapterCapturesExactOutcomeAndMappedResumeUsesFreshMediaAndPresentationTokens()
        {
            var context = Create(); var token = context.Controller.CurrentTokenId;
            var media = context.Media.StartIntro(token, 0d); context.Adapter.BindMediaHandle(media);
            context.Controller.HandlePlay(token, 0.1d);
            context.Controller.HandleSelectPlan(token, Stage3Plan.Channel, Guid.NewGuid(), 0.2d);
            context.Outcome.Start(context.Controller.Snapshot, false, 0.2d);
            context.Outcome.Advance(token, context.Controller.CurrentOutcomeTokenId, 1.2d);
            Assert.That(context.Adapter.PreparePause(1.2d), Is.True);
            Assert.That(context.Adapter.TryCapture(out var handle), Is.True);
            var captured = context.Adapter.CapturedSnapshot;
            Assert.That(context.Adapter.PauseMedia(handle, 1.2d), Is.True);
            Assert.That(context.Controller.HandlePause(1.2d), Is.True);
            var oldMedia = context.Media.CurrentMediaTokenId;
            var oldPresentation = context.Outcome.ActivePresentationTokenId;
            Assert.That(context.Adapter.Restore(handle, 100d), Is.True);
            Assert.That(context.Controller.Snapshot.State, Is.EqualTo(captured.State));
            Assert.That(context.Controller.Snapshot.SelectedRiver, Is.EqualTo(captured.SelectedRiver));
            Assert.That(context.Controller.Snapshot.SelectedPlan, Is.EqualTo(captured.SelectedPlan));
            Assert.That(context.Controller.Snapshot.CurrentOutcome, Is.EqualTo(captured.CurrentOutcome));
            Assert.That(context.Controller.Snapshot.OutcomeTokenId, Is.EqualTo(captured.OutcomeTokenId));
            Assert.That(context.Controller.Snapshot.OutcomeElapsedSeconds, Is.EqualTo(captured.OutcomeElapsedSeconds));
            Assert.That(context.Adapter.ResumeMedia(handle, 100d), Is.True);
            Assert.That(context.Media.CurrentMediaTokenId, Is.Not.EqualTo(oldMedia));
            Assert.That(context.Outcome.ActivePresentationTokenId, Is.Not.EqualTo(oldPresentation));
            Assert.That(context.Outcome.ElapsedSeconds, Is.EqualTo(1d).Within(0.001d));
        }

        [Test]
        public void RejectedPreparedPauseHasNoMediaOrOutcomeSideEffects()
        {
            var context = Create(); var token = context.Controller.CurrentTokenId;
            context.Adapter.BindMediaHandle(context.Media.StartIntro(token, 0d));
            Assert.That(context.Adapter.PreparePause(-1d), Is.False);
            Assert.That(context.Media.IsPaused, Is.False);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.Intro));
        }

        private static Context Create()
        {
            var session = new CoreSessionState(); session.Begin(RouteId.Boat3);
            var navigation = new NavigationAuthority(RouteId.Boat3, Ids(1));
            var transaction = navigation.Request(RouteId.Stage3, 0d).Transaction;
            var controller = new Stage3Controller(session, navigation, new View(), new Pause(), new Leave(), Ids(2));
            Assert.That(controller.Enter(transaction.Id, new Stage3EntryContext(Stage2River.Middle), 0d, false), Is.True);
            var backend = new Backend(); var director = new AudioDirector(backend, Ids(3));
            var media = new Stage3MediaPresenter(director, backend, Content(), session,
                new AccessibilityState(false, true), new MediaSink(), Ids(4));
            var outcome = new Stage3OutcomePresenter(new OutcomeSink());
            return new Context(controller, media, outcome, new Stage3PauseAdapter(controller, media, outcome, Ids(5)));
        }

        private static Stage3Content Content() => AssetDatabase.LoadAssetAtPath<Stage3Content>(Stage3ContentGenerator.ContentAssetPath);
        private static Func<Guid> Ids(byte prefix) { var value = 0; return () => { var bytes = new byte[16]; bytes[0] = prefix; bytes[15] = (byte)++value; return new Guid(bytes); }; }
        private readonly struct Context
        {
            internal Context(Stage3Controller c, Stage3MediaPresenter m, Stage3OutcomePresenter o, Stage3PauseAdapter a)
            { Controller = c; Media = m; Outcome = o; Adapter = a; }
            internal Stage3Controller Controller { get; }
            internal Stage3MediaPresenter Media { get; }
            internal Stage3OutcomePresenter Outcome { get; }
            internal Stage3PauseAdapter Adapter { get; }
        }
        private sealed class View : IStage3View { public void Present(Stage3Snapshot s, bool r) { } public void SetInputReady(bool r) { } }
        private sealed class Pause : IStage3PauseSink { public bool TryShow(Stage3Snapshot s) => true; }
        private sealed class Leave : ILeaveConfirmationSink { public bool TryShow(RouteId r) => true; }
        private sealed class OutcomeSink : IStage3OutcomeSink { public void Present(Stage3OutcomePresentation p) { } }
        private sealed class MediaSink : IStage3MediaCueSink { public void Present(Stage3MediaCue c) { } }
        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _ids = new HashSet<string>();
            public bool IsClipRegistered(string id) => _ids.Contains(id);
            public void RegisterClip(string id, AudioClip c, bool l = false) => _ids.Add(id);
            public void Start(AudioPlaybackToken t, AudioBus b) { } public void Stop(Guid i) { }
            public void PauseAllImmediately() { } public void SeekAll(double t) { }
            public void ResumeAll(float v, float r) { } public void SetMasterVolume(float v) { }
            public void ApplyDuck(AudioDuckState s) { }
        }
    }
}
