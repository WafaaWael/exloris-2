using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Overview;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewControllerTests
    {
        [Test]
        public void FullMotionEntryRevealsInOrderAndCompletesAtExactlyNineSeconds()
        {
            var context = CreateContext();

            Assert.That(context.Controller.Enter(100d, false), Is.True);
            var token = context.Controller.CurrentTokenId;
            AssertSnapshot(context.Controller.Snapshot, token,
                JourneyOverviewRevealState.RevealingMeasure, 0d, 1);

            Assert.That(context.Controller.Advance(token, 103.288d), Is.True);
            Assert.That(context.Controller.Snapshot.State,
                Is.EqualTo(JourneyOverviewRevealState.RevealingMeasure));
            Assert.That(context.Controller.Advance(token, 103.289d), Is.True);
            Assert.That(context.Controller.Snapshot.State,
                Is.EqualTo(JourneyOverviewRevealState.RevealingRead));
            Assert.That(context.Controller.Snapshot.RevealedBeatCount, Is.EqualTo(2));
            Assert.That(context.Controller.Advance(token, 106.182d), Is.True);
            Assert.That(context.Controller.Snapshot.State,
                Is.EqualTo(JourneyOverviewRevealState.RevealingHelp));
            Assert.That(context.Controller.Snapshot.RevealedBeatCount, Is.EqualTo(3));

            Assert.That(context.Controller.Advance(token, 108.999d), Is.True);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.Advance(token, 109d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(JourneyOverviewRevealState.Navigating));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Boat1));
            Assert.That(context.Session.CurrentOpenSessionLocation,
                Is.EqualTo(RouteId.JourneyOverview));
        }

        [Test]
        public void ReducedMotionEntryPresentsAllMeaningImmediatelyWithoutEarlyCompletion()
        {
            var context = CreateContext();

            Assert.That(context.Controller.Enter(20d, true), Is.True);

            Assert.That(context.Controller.State, Is.EqualTo(JourneyOverviewRevealState.Ready));
            Assert.That(context.Controller.Snapshot.RevealedBeatCount, Is.EqualTo(3));
            Assert.That(context.View.LastReducedMotion, Is.True);
            Assert.That(context.View.InputReady, Is.True);
            Assert.That(context.Controller.Advance(context.Controller.CurrentTokenId, 28.999d), Is.True);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void PlaybackCompletionRequiresCurrentTokenAndExactDurationAndConsumesOnce()
        {
            var context = CreateContext();
            context.Controller.Enter(30d, false);
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandlePlaybackCompleted(Guid.NewGuid(), 39d), Is.False);
            Assert.That(context.Controller.HandlePlaybackCompleted(token, 38.999d), Is.False);
            Assert.That(context.Controller.HandlePlaybackCompleted(token, 39d), Is.True);
            Assert.That(context.Controller.HandlePlaybackCompleted(token, 39.001d), Is.False);
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Boat1));
        }

        [Test]
        public void SkipConsumesOnlyTheCurrentTokenOnce()
        {
            var context = CreateContext();
            context.Controller.Enter(40d, false);
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleSkip(Guid.NewGuid(), 40.1d), Is.False);
            Assert.That(context.Controller.HandleSkip(token, 40.2d), Is.True);
            Assert.That(context.Controller.HandleSkip(token, 40.3d), Is.False);
            Assert.That(context.Navigation.ActiveTransaction.Caller,
                Is.EqualTo(RouteId.JourneyOverview));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Boat1));
        }

        [Test]
        public void RepeatCreatesFreshTokenAndRejectsOldCallbacks()
        {
            var context = CreateContext();
            context.Controller.Enter(50d, false);
            var first = context.Controller.CurrentTokenId;
            context.Controller.Advance(first, 54d);

            Assert.That(context.Controller.HandleRepeat(55d), Is.True);
            var second = context.Controller.CurrentTokenId;

            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(context.Controller.State,
                Is.EqualTo(JourneyOverviewRevealState.RevealingMeasure));
            Assert.That(context.Controller.Advance(first, 59d), Is.False);
            Assert.That(context.Controller.HandlePlaybackCompleted(first, 59d), Is.False);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void PauseCapturesExactSnapshotAndRestoreReturnsWithoutNavigation()
        {
            var context = CreateContext();
            context.Controller.Enter(60d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.Advance(token, 64d);

            Assert.That(context.Controller.HandlePause(), Is.True);
            var captured = context.PauseSink.Snapshots[0];

            AssertSnapshot(captured, token, JourneyOverviewRevealState.RevealingRead, 4d, 2);
            Assert.That(context.Controller.State, Is.EqualTo(JourneyOverviewRevealState.Paused));
            Assert.That(context.View.InputReady, Is.False);
            Assert.That(context.Controller.HandlePause(), Is.False);
            Assert.That(context.Controller.Restore(captured), Is.True);
            AssertSnapshot(context.Controller.Snapshot, token,
                JourneyOverviewRevealState.RevealingRead, 4d, 2);
            Assert.That(context.View.InputReady, Is.True);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void RestoredRevealReanchorsClockAndNeverCountsTimeSpentPaused()
        {
            var context = CreateContext();
            context.Controller.Enter(10d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.Advance(token, 14d);
            context.Controller.HandlePause();
            var captured = context.PauseSink.Snapshots[0];

            Assert.That(context.Controller.Restore(captured), Is.True);
            Assert.That(context.Controller.Advance(token, 100d), Is.True);
            Assert.That(context.Controller.Snapshot.ElapsedSeconds, Is.EqualTo(4d).Within(0.0001d));
            Assert.That(context.Controller.Advance(token, 101d), Is.True);
            Assert.That(context.Controller.Snapshot.ElapsedSeconds, Is.EqualTo(5d).Within(0.0001d));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void HomeUsesExactJourneyCallerWithoutStartingNavigation()
        {
            var context = CreateContext();
            context.Controller.Enter(70d, false);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHome(), Is.True);

            Assert.That(context.LeaveSink.Callers,
                Is.EqualTo(new[] { RouteId.JourneyOverview }));
            Assert.That(context.Controller.State, Is.EqualTo(JourneyOverviewRevealState.Leaving));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.RestoreFromLeave(captured), Is.True);
            Assert.That(context.Controller.Snapshot.TokenId, Is.EqualTo(captured.TokenId));
            Assert.That(context.Controller.Snapshot.ElapsedSeconds,
                Is.EqualTo(captured.ElapsedSeconds));
            Assert.That(context.Controller.State, Is.EqualTo(captured.State));
        }

        [Test]
        public void ReducedMotionChangeWhilePausedPreservesSnapshotAndSettlesOnResume()
        {
            var context = CreateContext();
            context.Controller.Enter(80d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.Advance(token, 82d);
            Assert.That(context.Controller.HandlePause(), Is.True);
            var paused = context.PauseSink.Snapshots[0];

            Assert.That(context.Controller.ApplyReducedMotion(true), Is.True);
            Assert.That(context.Controller.Restore(paused), Is.True);
            Assert.That(context.Controller.Advance(token, 100d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(JourneyOverviewRevealState.Ready));
            Assert.That(context.Controller.Snapshot.RevealedBeatCount, Is.EqualTo(3));
        }

        [Test]
        public void EntryRejectsLockedRecoveryOrMismatchedSessionState()
        {
            var locked = CreateContext();
            Assert.That(locked.Navigation.Request(RouteId.Boat1, 1d).Accepted, Is.True);
            Assert.That(locked.Controller.Enter(1.1d, false), Is.False);

            var recovery = CreateContext();
            Assert.That(recovery.Navigation.Request(RouteId.Boat1, 2d).Accepted, Is.True);
            Assert.That(recovery.Navigation.Interrupt(), Is.True);
            Assert.That(recovery.Controller.Enter(2.1d, false), Is.False);

            var wrongSession = CreateContext();
            wrongSession.Session.MoveTo(RouteId.Boat1);
            Assert.That(wrongSession.Controller.Enter(3d, false), Is.False);
        }

        private static TestContext CreateContext()
        {
            var session = new SessionState();
            session.Begin(RouteId.JourneyOverview);
            var navigation = new NavigationAuthority(
                RouteId.JourneyOverview,
                new IncrementingGuidFactory(100).Next);
            var view = new RecordingView();
            var pauseSink = new RecordingPauseSink();
            var leaveSink = new RecordingLeaveSink();
            var controller = new JourneyOverviewController(
                session,
                navigation,
                view,
                pauseSink,
                leaveSink,
                new IncrementingGuidFactory(1).Next);
            return new TestContext(session, navigation, view, pauseSink, leaveSink, controller);
        }

        private static void AssertSnapshot(
            JourneyOverviewSnapshot snapshot,
            Guid token,
            JourneyOverviewRevealState state,
            double elapsed,
            int beats)
        {
            Assert.That(snapshot.TokenId, Is.EqualTo(token));
            Assert.That(snapshot.State, Is.EqualTo(state));
            Assert.That(snapshot.ElapsedSeconds, Is.EqualTo(elapsed).Within(0.0001d));
            Assert.That(snapshot.RevealedBeatCount, Is.EqualTo(beats));
        }

        private sealed class TestContext
        {
            public TestContext(
                SessionState session,
                NavigationAuthority navigation,
                RecordingView view,
                RecordingPauseSink pauseSink,
                RecordingLeaveSink leaveSink,
                JourneyOverviewController controller)
            {
                Session = session;
                Navigation = navigation;
                View = view;
                PauseSink = pauseSink;
                LeaveSink = leaveSink;
                Controller = controller;
            }

            public SessionState Session { get; }
            public NavigationAuthority Navigation { get; }
            public RecordingView View { get; }
            public RecordingPauseSink PauseSink { get; }
            public RecordingLeaveSink LeaveSink { get; }
            public JourneyOverviewController Controller { get; }
        }

        private sealed class RecordingView : IJourneyOverviewView
        {
            public JourneyOverviewSnapshot LastSnapshot { get; private set; }
            public bool LastReducedMotion { get; private set; }
            public bool InputReady { get; private set; }

            public void Present(JourneyOverviewSnapshot snapshot, bool reducedMotion)
            {
                LastSnapshot = snapshot;
                LastReducedMotion = reducedMotion;
            }

            public void SetInputReady(bool ready)
            {
                InputReady = ready;
            }
        }

        private sealed class RecordingPauseSink : IPauseOverlaySink
        {
            public List<JourneyOverviewSnapshot> Snapshots { get; } =
                new List<JourneyOverviewSnapshot>();

            public bool TryShow(JourneyOverviewSnapshot snapshot)
            {
                Snapshots.Add(snapshot);
                return true;
            }
        }

        private sealed class RecordingLeaveSink : ILeaveConfirmationSink
        {
            public List<RouteId> Callers { get; } = new List<RouteId>();

            public bool TryShow(RouteId caller)
            {
                Callers.Add(caller);
                return true;
            }
        }

        private sealed class IncrementingGuidFactory
        {
            private readonly int _prefix;
            private int _value;

            public IncrementingGuidFactory(int prefix)
            {
                _prefix = prefix;
            }

            public Guid Next()
            {
                _value++;
                var bytes = new byte[16];
                bytes[0] = (byte)_prefix;
                bytes[15] = (byte)_value;
                return new Guid(bytes);
            }
        }
    }
}
