using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.Recovery;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class PauseOverlayControllerTests
    {
        [Test]
        public void PauseCapturesExactCallerAndMappedResumeRestoresOnceWithoutNavigation()
        {
            var context = Create();
            context.Journey.Enter(10d, false);
            var token = context.Journey.CurrentTokenId;
            context.Media.RevealToken = token;
            context.Journey.Advance(token, 14d);

            Assert.That(context.Journey.HandlePause(), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(context.Controller.Snapshot.CallerSnapshot.ElapsedSeconds,
                Is.EqualTo(4d).Within(0.0001d));
            Assert.That(context.View.Blocked.CallerSnapshot.TokenId, Is.EqualTo(token));

            Assert.That(context.Controller.HandleResume(100d), Is.True);
            Assert.That(context.Journey.State, Is.EqualTo(JourneyOverviewRevealState.RevealingRead));
            Assert.That(context.Journey.Snapshot.ElapsedSeconds, Is.EqualTo(4d).Within(0.0001d));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Media.ResumeCount, Is.EqualTo(1));
            Assert.That(context.Controller.HandleResume(101d), Is.False);
        }

        [Test]
        public void PauseSettingsPersistOnlyApprovedAccessibilityKeysAndNeverChangeCallerSnapshot()
        {
            var context = Create();
            context.Journey.Enter(20d, false);
            context.Media.RevealToken = context.Journey.CurrentTokenId;
            context.Journey.Advance(context.Journey.CurrentTokenId, 22d);
            context.Journey.HandlePause();
            var before = context.Controller.Snapshot.CallerSnapshot;

            Assert.That(context.Controller.HandleSound(), Is.True);
            Assert.That(context.Controller.HandleCaptions(), Is.True);
            Assert.That(context.Controller.HandleMotion(), Is.True);

            Assert.That(context.Session.SoundEnabled, Is.False);
            Assert.That(context.Accessibility.CaptionsEnabled, Is.True);
            Assert.That(context.Accessibility.AlwaysReduce, Is.True);
            Assert.That(context.PreferenceBackend.Keys, Is.EquivalentTo(new[]
            {
                AccessibilityPreferenceStore.AlwaysReduceKey,
                AccessibilityPreferenceStore.CaptionsEnabledKey,
            }));
            AssertSnapshotEqual(context.Controller.Snapshot.CallerSnapshot, before);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.View.LastSettings.ReducedMotion, Is.True);
        }

        [Test]
        public void HelpAndNestedLeaveRestoreTheByteIdenticalPauseLayerAndRejectDuplicates()
        {
            var context = Create();
            context.Journey.Enter(30d, true);
            context.Media.RevealToken = context.Journey.CurrentTokenId;
            context.Journey.HandlePause();
            var pause = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHelp(), Is.True);
            Assert.That(context.Controller.HandleHelp(), Is.False);
            Assert.That(context.Help.Snapshots, Is.EqualTo(new[] { pause }));
            Assert.That(context.Controller.RestoreFromHelp(pause), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(context.View.RestoredFromChild, Is.EqualTo(pause));

            Assert.That(context.Controller.HandleHome(), Is.True);
            Assert.That(context.Controller.HandleHome(), Is.False);
            Assert.That(context.Leave.Requests, Has.Count.EqualTo(1));
            Assert.That(context.Leave.Requests[0].Caller, Is.EqualTo(RouteId.JourneyOverview));
            Assert.That(context.Leave.Requests[0].ReturnLayer, Is.EqualTo(LeaveReturnLayer.PauseOverlay));
            Assert.That(context.Leave.Requests[0].StackToken, Is.EqualTo(pause.StackToken));
            Assert.That(context.Controller.RestoreFromLeave(context.Leave.Requests[0]), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(PauseLayerState.Pause));
            Assert.That(context.View.RestoredFromChild, Is.EqualTo(pause));
        }

        [Test]
        public void WrongStaleLockedAndDuplicatePauseRequestsAreRejected()
        {
            var context = Create();
            context.Journey.Enter(40d, false);
            context.Media.RevealToken = context.Journey.CurrentTokenId;
            var wrong = new JourneyOverviewSnapshot(Guid.NewGuid(),
                JourneyOverviewRevealState.RevealingMeasure, 0d, 1);
            Assert.That(context.Controller.TryShow(wrong), Is.False);
            Assert.That(context.Journey.HandlePause(), Is.True);
            Assert.That(context.Controller.TryShow(context.Controller.Snapshot.CallerSnapshot), Is.False);
        }

        private static Context Create()
        {
            var session = new SessionState();
            session.Begin(RouteId.JourneyOverview);
            var navigation = new NavigationAuthority(RouteId.JourneyOverview, SequentialIds(10));
            var journeyView = new RecordingJourneyView();
            var pauseProxy = new PauseProxy();
            var journey = new JourneyOverviewController(
                session, navigation, journeyView, pauseProxy, new NoLeave(), SequentialIds(20));
            var media = new RecordingPauseMedia();
            var accessibility = new AccessibilityState(false, false);
            accessibility.SetOsState(ReduceMotionOsState.NotReduced);
            var coordinator = new AccessibilityCoordinator(accessibility, new StaticMotionAdapter());
            var preferenceBackend = new RecordingPreferenceBackend();
            var preferences = new AccessibilityPreferenceStore(preferenceBackend);
            var view = new RecordingPauseView();
            var help = new RecordingHelpSink();
            var leave = new RecordingNestedLeaveSink();
            var controller = new PauseOverlayController(
                session, navigation, journey, media, accessibility, coordinator,
                preferences, view, help, leave, SequentialIds(30));
            pauseProxy.Target = controller;
            return new Context(session, navigation, journey, media, accessibility,
                preferenceBackend, view, help, leave, controller, coordinator);
        }

        private static Func<Guid> SequentialIds(int prefix)
        {
            var value = 0;
            return () =>
            {
                var bytes = new byte[16];
                bytes[0] = (byte)prefix;
                bytes[15] = (byte)++value;
                return new Guid(bytes);
            };
        }

        private static void AssertSnapshotEqual(
            JourneyOverviewSnapshot actual, JourneyOverviewSnapshot expected)
        {
            Assert.That(actual.TokenId, Is.EqualTo(expected.TokenId));
            Assert.That(actual.State, Is.EqualTo(expected.State));
            Assert.That(actual.ElapsedSeconds, Is.EqualTo(expected.ElapsedSeconds));
            Assert.That(actual.RevealedBeatCount, Is.EqualTo(expected.RevealedBeatCount));
        }

        private sealed class Context : IDisposable
        {
            public Context(SessionState session, NavigationAuthority navigation,
                JourneyOverviewController journey, RecordingPauseMedia media,
                AccessibilityState accessibility, RecordingPreferenceBackend preferenceBackend,
                RecordingPauseView view, RecordingHelpSink help,
                RecordingNestedLeaveSink leave, PauseOverlayController controller,
                AccessibilityCoordinator coordinator)
            {
                Session = session; Navigation = navigation; Journey = journey; Media = media;
                Accessibility = accessibility; PreferenceBackend = preferenceBackend;
                View = view; Help = help; Leave = leave; Controller = controller;
                Coordinator = coordinator;
            }
            public SessionState Session { get; }
            public NavigationAuthority Navigation { get; }
            public JourneyOverviewController Journey { get; }
            public RecordingPauseMedia Media { get; }
            public AccessibilityState Accessibility { get; }
            public RecordingPreferenceBackend PreferenceBackend { get; }
            public RecordingPauseView View { get; }
            public RecordingHelpSink Help { get; }
            public RecordingNestedLeaveSink Leave { get; }
            public PauseOverlayController Controller { get; }
            private AccessibilityCoordinator Coordinator { get; }
            public void Dispose() => Coordinator.Dispose();
        }

        private sealed class PauseProxy : IPauseOverlaySink
        {
            public IPauseOverlaySink Target { get; set; }
            public bool TryShow(JourneyOverviewSnapshot snapshot) => Target.TryShow(snapshot);
        }

        private sealed class RecordingJourneyView : IJourneyOverviewView
        {
            public void Present(JourneyOverviewSnapshot snapshot, bool reducedMotion) { }
            public void SetInputReady(bool ready) { }
        }

        private sealed class NoLeave : Exloris.Runtime.Avatar.ILeaveConfirmationSink
        {
            public bool TryShow(RouteId caller) => false;
        }

        private sealed class RecordingPauseMedia : IJourneyOverviewPauseMedia
        {
            public Guid RevealToken { get; set; }
            public bool IsPaused { get; set; } = true;
            public Guid CurrentRevealTokenId => RevealToken;
            public int ResumeCount { get; private set; }
            public int RefreshCount { get; private set; }
            public JourneyOverviewMediaHandle? ResumeAfterMappedUserAction(Guid revealTokenId, double nowSeconds)
            {
                if (!IsPaused || revealTokenId != RevealToken) return null;
                IsPaused = false; ResumeCount++;
                return new JourneyOverviewMediaHandle(revealTokenId, Guid.NewGuid(), null, null, null);
            }
            public void RefreshSettings(double nowSeconds) => RefreshCount++;
        }

        private sealed class RecordingPauseView : IPauseOverlayView
        {
            public PauseOverlaySnapshot Blocked { get; private set; }
            public PauseOverlaySnapshot RestoredFromChild { get; private set; }
            public PauseSettings LastSettings { get; private set; }
            public void ShowAndBlockCaller(PauseOverlaySnapshot snapshot) => Blocked = snapshot;
            public void RestoreCaller(PauseOverlaySnapshot snapshot) { }
            public void HoldBlockedForChild(PauseOverlaySnapshot snapshot) { }
            public void RestoreFromChild(PauseOverlaySnapshot snapshot) => RestoredFromChild = snapshot;
            public void HoldBlockedForTransition(PauseOverlaySnapshot snapshot) { }
            public void ApplySettings(PauseSettings settings) => LastSettings = settings;
        }

        private sealed class RecordingHelpSink : IJourneyOverviewPauseHelpSink
        {
            public List<PauseOverlaySnapshot> Snapshots { get; } = new List<PauseOverlaySnapshot>();
            public bool TryShow(PauseOverlaySnapshot snapshot) { Snapshots.Add(snapshot); return true; }
        }

        private sealed class RecordingNestedLeaveSink : INestedLeaveConfirmationSink
        {
            public List<LeaveRequest> Requests { get; } = new List<LeaveRequest>();
            public bool TryShow(LeaveRequest request) { Requests.Add(request); return true; }
        }

        private sealed class StaticMotionAdapter : IReduceMotionAdapter
        {
            public event Action<ReduceMotionOsState> Changed { add { } remove { } }
            public System.Threading.Tasks.Task<ReduceMotionOsState> QueryAsync(
                System.Threading.CancellationToken cancellationToken) =>
                System.Threading.Tasks.Task.FromResult(ReduceMotionOsState.NotReduced);
        }

        private sealed class RecordingPreferenceBackend : IPreferenceBackend
        {
            private readonly Dictionary<string, bool> _values = new Dictionary<string, bool>();
            public ICollection<string> Keys => _values.Keys;
            public bool GetBool(string key, bool defaultValue) =>
                _values.TryGetValue(key, out var value) ? value : defaultValue;
            public void SetBool(string key, bool value) => _values[key] = value;
            public void Save() { }
        }
    }
}
