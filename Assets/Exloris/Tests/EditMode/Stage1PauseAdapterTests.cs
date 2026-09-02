using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CoreSessionState = Exloris.Core.Session.SessionState;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1PauseAdapterTests
    {
        [Test]
        public void AdapterCapturesExactOutcomeBeatAndMappedResumeReanchorsEveryOwner()
        {
            using (var context = Create())
            {
                EnterOutcome(context, 0.1d);
                var routeToken = context.Controller.CurrentTokenId;
                var outcomeToken = context.Controller.CurrentOutcomeTokenId;
                context.Controller.AdvanceOutcome(routeToken, outcomeToken, 6.2d);
                context.Outcome.Advance(routeToken, outcomeToken, 6.2d);
                var handle = context.Media.StartOutside(routeToken, 5d);
                context.Media.Advance(routeToken, handle.MediaTokenId, 6.2d);
                context.Adapter.BindMediaHandle(handle);

                Assert.That(context.Adapter.PreparePause(6.2d, Stage1PauseReason.AudioFocusLoss), Is.True);
                Assert.That(context.Adapter.TryCapture(out var pauseHandle), Is.True);
                var captured = context.Adapter.CapturedSnapshot;
                Assert.That(context.Adapter.PauseMedia(pauseHandle, 6.2d), Is.True);
                Assert.That(context.Controller.HandlePause(6.2d), Is.True);
                var oldMediaToken = context.Media.CurrentMediaTokenId;

                Assert.That(context.Adapter.Restore(pauseHandle, 100d), Is.True);
                Assert.That(context.Adapter.ResumeMedia(pauseHandle, 100d), Is.True);
                Assert.That(context.Controller.State, Is.EqualTo(Stage1State.OutcomePlaying));
                Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(captured.MarkerPosition));
                Assert.That(context.Controller.Snapshot.OutcomeElapsedSeconds, Is.EqualTo(1.2d).Within(0.000001d));
                Assert.That(context.Media.CurrentMediaTokenId, Is.Not.EqualTo(oldMediaToken));
                Assert.That(context.Outcome.Advance(routeToken, outcomeToken, 101.999d), Is.True);
                Assert.That(context.OutcomeSink.Last.Phase, Is.Not.EqualTo(Stage1OutcomePhase.Complete));
                Assert.That(context.Outcome.Advance(routeToken, outcomeToken, 102d), Is.True);
                Assert.That(context.OutcomeSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Complete));
                Assert.That(context.Adapter.ResumeMedia(pauseHandle, 101d), Is.False);
            }
        }

        [Test]
        public void SharedPauseOverlayAcceptsStage1AndPreservesNestedHelpAndLeaveOrder()
        {
            using (var context = Create())
            {
                var handle = context.Media.StartIntro(context.Controller.CurrentTokenId, 2d);
                context.Adapter.BindMediaHandle(handle);
                context.PauseProxy.Target = context.Pause;
                Assert.That(context.Adapter.PreparePause(3d, Stage1PauseReason.AppBackground), Is.True);
                Assert.That(context.Controller.HandlePause(3d), Is.True);
                Assert.That(context.Pause.State, Is.EqualTo(PauseLayerState.Pause));
                Assert.That(context.Pause.Snapshot.CallerHandle.Route, Is.EqualTo(RouteId.Stage1));

                var frozen = context.Pause.Snapshot;
                Assert.That(context.Pause.HandleHelp(), Is.True);
                Assert.That(context.Help.Last, Is.EqualTo(frozen));
                Assert.That(context.Pause.RestoreFromHelp(frozen), Is.True);
                Assert.That(context.Pause.HandleHome(), Is.True);
                Assert.That(context.Leave.Last.Caller, Is.EqualTo(RouteId.Stage1));
                Assert.That(context.Leave.Last.ReturnLayer, Is.EqualTo(LeaveReturnLayer.PauseOverlay));
                Assert.That(context.Leave.Last.StackToken, Is.EqualTo(frozen.StackToken));
                Assert.That(context.Pause.RestoreFromLeave(context.Leave.Last), Is.True);
                Assert.That(context.Pause.State, Is.EqualTo(PauseLayerState.Pause));
            }
        }

        [Test]
        public void RejectedPreparedPauseLeavesMediaAndOutcomeUntouched()
        {
            using (var context = Create())
            {
                var handle = context.Media.StartIntro(context.Controller.CurrentTokenId, 2d);
                context.Adapter.BindMediaHandle(handle);
                Assert.That(context.Media.Stop(context.Controller.CurrentTokenId), Is.True);
                Assert.That(context.Adapter.PreparePause(3d, Stage1PauseReason.Manual), Is.False);
                Assert.That(context.Media.IsPaused, Is.False);
                Assert.That(context.Outcome.IsPaused, Is.False);
                Assert.That(context.Controller.State, Is.EqualTo(Stage1State.Intro));
            }
        }

        [Test]
        public void LeaveConfirmationAcceptsStage1DirectAndNestedCallers()
        {
            var session = new CoreSessionState();
            session.Begin(RouteId.Stage1);
            var navigation = new NavigationAuthority(RouteId.Stage1, Ids(70));
            var view = new LeaveView();
            var controller = new LeaveConfirmationController(session, navigation, view);
            var nestedReturn = new NestedReturn();
            controller.BindNestedReturnSink(nestedReturn);

            Assert.That(controller.TryShow(RouteId.Stage1), Is.True);
            Assert.That(controller.HandleStay(), Is.True);
            var request = new LeaveRequest(RouteId.Stage1, LeaveReturnLayer.PauseOverlay, Guid.NewGuid());
            Assert.That(controller.TryShow(request), Is.True);
            Assert.That(controller.HandleCancel(), Is.True);
            Assert.That(view.DirectCaller, Is.EqualTo(RouteId.Stage1));
            Assert.That(view.NestedRequest, Is.EqualTo(request));
            Assert.That(nestedReturn.Restored, Is.EqualTo(request));
        }

        private static Context Create()
        {
            var session = new CoreSessionState();
            session.Begin(RouteId.Boat1);
            var navigation = new NavigationAuthority(RouteId.Boat1, Ids(1));
            var entry = navigation.Request(RouteId.Stage1, 1d);
            var view = new StageView();
            var pauseProxy = new PauseProxy();
            var controller = new Stage1Controller(session, navigation, view, pauseProxy, new NoLeave(), Ids(2));
            Assert.That(controller.Enter(entry.Transaction.Id, 2d, false), Is.True);
            var backend = new Backend();
            var director = new AudioDirector(backend, Ids(3));
            var accessibility = new AccessibilityState(false, true);
            accessibility.SetOsState(ReduceMotionOsState.NotReduced);
            var media = new Stage1MediaPresenter(director, backend, Content(), session, accessibility, new MediaSink(), Ids(4));
            var outcomeSink = new OutcomeSink();
            var outcome = new Stage1OutcomePresenter(Content(), outcomeSink);
            var adapter = new Stage1PauseAdapter(controller, media, outcome, Ids(5));
            var coordinator = new AccessibilityCoordinator(accessibility, new MotionAdapter());
            var pauseView = new PauseView();
            var help = new HelpSink();
            var leave = new LeaveSink();
            var pause = new PauseOverlayController(
                session, navigation, adapter, accessibility, coordinator,
                new AccessibilityPreferenceStore(new PreferenceBackend()), pauseView,
                help, leave, Ids(6));
            pauseProxy.Target = new AcceptPause();
            return new Context(controller, media, outcome, outcomeSink, adapter, pauseProxy,
                pause, help, leave, coordinator);
        }

        private static void EnterOutcome(Context context, double markerPosition)
        {
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandlePlay(token, 3d), Is.True);
            Assert.That(context.Controller.HandleDragAttach(token, Guid.NewGuid(), true, true, 3.5d), Is.True);
            Assert.That(context.Controller.HandleDrag(token, markerPosition, 4d), Is.EqualTo(Stage1MoveResult.Moved));
            Assert.That(context.Controller.HandleConfirm(token, 5d), Is.True);
            Assert.That(context.Outcome.Start(context.Controller.Snapshot, false, 5d), Is.True);
        }

        private static Stage1Content Content() => AssetDatabase.LoadAssetAtPath<Stage1Content>(
            "Assets/Exloris/Resources/Content/Stage1Content.asset");

        private static Func<Guid> Ids(byte prefix)
        {
            var value = 0;
            return () => { var bytes = new byte[16]; bytes[0] = prefix; bytes[15] = (byte)++value; return new Guid(bytes); };
        }

        private sealed class Context : IDisposable
        {
            internal Context(Stage1Controller controller, Stage1MediaPresenter media,
                Stage1OutcomePresenter outcome, OutcomeSink outcomeSink, Stage1PauseAdapter adapter,
                PauseProxy pauseProxy, PauseOverlayController pause, HelpSink help, LeaveSink leave,
                AccessibilityCoordinator coordinator)
            {
                Controller = controller; Media = media; Outcome = outcome; OutcomeSink = outcomeSink;
                Adapter = adapter; PauseProxy = pauseProxy; Pause = pause; Help = help; Leave = leave;
                Coordinator = coordinator;
            }
            internal Stage1Controller Controller { get; }
            internal Stage1MediaPresenter Media { get; }
            internal Stage1OutcomePresenter Outcome { get; }
            internal OutcomeSink OutcomeSink { get; }
            internal Stage1PauseAdapter Adapter { get; }
            internal PauseProxy PauseProxy { get; }
            internal PauseOverlayController Pause { get; }
            internal HelpSink Help { get; }
            internal LeaveSink Leave { get; }
            private AccessibilityCoordinator Coordinator { get; }
            public void Dispose() => Coordinator.Dispose();
        }

        private sealed class StageView : IStage1View { public void Present(Stage1Snapshot snapshot, bool reduced) { } public void SetInputReady(bool ready) { } }
        private sealed class PauseProxy : IStage1PauseSink { internal IStage1PauseSink Target; public bool TryShow(Stage1Snapshot snapshot) => Target != null && Target.TryShow(snapshot); }
        private sealed class AcceptPause : IStage1PauseSink { public bool TryShow(Stage1Snapshot snapshot) => true; }
        private sealed class NoLeave : Exloris.Runtime.Avatar.ILeaveConfirmationSink { public bool TryShow(RouteId caller) => false; }
        private sealed class MediaSink : IStage1MediaCueSink { public void PresentMediaCue(Stage1MediaCue cue) { } public void FreezeMediaCue(double elapsed) { } public void ResumeMediaCue(Guid token, double elapsed) { } public void SetCallerInputLocked(bool locked) { } public void ShowPauseOverlay(Stage1PauseReason reason) { } }
        private sealed class OutcomeSink : IStage1OutcomeSink { internal Stage1OutcomeFrame Last; public void PresentOutcome(Stage1OutcomeFrame frame) => Last = frame; }
        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _ids = new HashSet<string>();
            public bool IsClipRegistered(string id) => _ids.Contains(id);
            public void RegisterClip(string id, AudioClip clip, bool loop = false) => _ids.Add(id);
            public void Start(AudioPlaybackToken token, AudioBus bus) { }
            public void Stop(Guid tokenId) { }
            public void PauseAllImmediately() { }
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float volume, float ramp) { }
            public void SetMasterVolume(float volume) { }
            public void ApplyDuck(AudioDuckState state) { }
        }
        private sealed class PauseView : IPauseOverlayView { public void ShowAndBlockCaller(PauseOverlaySnapshot snapshot) { } public void RestoreCaller(PauseOverlaySnapshot snapshot) { } public void HoldBlockedForChild(PauseOverlaySnapshot snapshot) { } public void RestoreFromChild(PauseOverlaySnapshot snapshot) { } public void HoldBlockedForTransition(PauseOverlaySnapshot snapshot) { } public void ApplySettings(PauseSettings settings) { } }
        private sealed class HelpSink : IJourneyOverviewPauseHelpSink { internal PauseOverlaySnapshot Last; public bool TryShow(PauseOverlaySnapshot snapshot) { Last = snapshot; return true; } }
        private sealed class LeaveSink : INestedLeaveConfirmationSink { internal LeaveRequest Last; public bool TryShow(LeaveRequest request) { Last = request; return true; } }
        private sealed class MotionAdapter : IReduceMotionAdapter { public event Action<ReduceMotionOsState> Changed { add { } remove { } } public System.Threading.Tasks.Task<ReduceMotionOsState> QueryAsync(System.Threading.CancellationToken token) => System.Threading.Tasks.Task.FromResult(ReduceMotionOsState.NotReduced); }
        private sealed class PreferenceBackend : IPreferenceBackend { public bool GetBool(string key, bool fallback) => fallback; public void SetBool(string key, bool value) { } public void Save() { } }
        private sealed class LeaveView : ILeaveConfirmationView, INestedLeaveConfirmationView
        {
            internal RouteId DirectCaller; internal LeaveRequest NestedRequest;
            public void ShowAndBlockCaller(RouteId caller) => DirectCaller = caller;
            public void HideAndRestoreCaller(RouteId caller) { }
            public void HoldBlockedForTransition(RouteId caller) { }
            public void ShowAndBlockPause(LeaveRequest request) => NestedRequest = request;
            public void HideAndRestorePause(LeaveRequest request) { }
            public void HoldPauseBlockedForTransition(LeaveRequest request) { }
        }
        private sealed class NestedReturn : INestedLeaveReturnSink
        {
            internal LeaveRequest Restored;
            public bool RestoreFromLeave(LeaveRequest request) { Restored = request; return true; }
            public bool HoldForLeaveTransition(LeaveRequest request) => true;
        }
    }
}
