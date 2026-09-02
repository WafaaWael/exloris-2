using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3ControllerTests
    {
        [Test]
        public void EntryCreatesQuietChoiceWithFixedVisibleLeftCue()
        {
            var context = Create();

            Assert.That(context.Controller.Enter(10d, false), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Choice));
            Assert.That(context.Controller.Snapshot.IbisDirection, Is.EqualTo(BoatDirection.Left));
            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(Boat3Choice.None));
            Assert.That(context.Controller.Snapshot.TravelElapsedSeconds, Is.Zero);
            Assert.That(context.View.InputReady, Is.True);
        }

        [TestCase(Boat3Choice.GrainStore)]
        [TestCase(Boat3Choice.Irrigation)]
        public void LandmarkChoicesStartTheSameSixSecondClock(Boat3Choice choice)
        {
            var context = Create();
            context.Controller.Enter(1d, false);

            Assert.That(context.Controller.HandleChoice(context.Controller.CurrentTokenId, choice, 3d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Traveling));
            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(choice));
            Assert.That(context.Controller.Snapshot.StateStartedAtSeconds, Is.EqualTo(3d));
            Assert.That(context.Controller.Snapshot.TravelElapsedSeconds, Is.Zero);
            Assert.That(Boat3Controller.TravelSeconds, Is.EqualTo(6d));
        }

        [Test]
        public void FollowIbisUsesTheVisibleLeftCueAndSameTravelClock()
        {
            var context = Create();
            context.Controller.Enter(2d, false);

            Assert.That(context.Controller.HandleFollowIbis(context.Controller.CurrentTokenId, 4d), Is.True);
            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(Boat3Choice.FollowIbis));
            Assert.That(context.Controller.Snapshot.IbisDirection, Is.EqualTo(BoatDirection.Left));
            Assert.That(context.Controller.Snapshot.StateStartedAtSeconds, Is.EqualTo(4d));
        }

        [Test]
        public void InactivityNeverChoosesOrAdvances()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.Advance(token, 120d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Choice));
            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(Boat3Choice.None));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void NaturalTravelUsesReachableArrivalThenRequestsStage3ExactlyOnce()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleChoice(token, Boat3Choice.GrainStore, 5d);

            Assert.That(context.Controller.Advance(token, 10.999d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Traveling));
            Assert.That(context.Controller.Advance(token, 11d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Arriving));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.Advance(token, 11.699d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Arriving));
            Assert.That(context.Controller.Advance(token, 11.7d), Is.True);
            Assert.That(context.Navigation.ActiveTransaction.Caller, Is.EqualTo(RouteId.Boat3));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Stage3));
            Assert.That(context.Controller.Advance(token, 12d), Is.False);
            Assert.That(context.Controller.HandleSkip(token, 12d), Is.False);
        }

        [Test]
        public void SkipHasTheSameArrivalBeatAndRejectsStaleOrDuplicateInput()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleSkip(Guid.NewGuid(), 1d), Is.False);
            Assert.That(context.Controller.HandleSkip(token, 1d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Arriving));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.HandleSkip(token, 1.1d), Is.False);
            Assert.That(context.Controller.Advance(token, 1.699d), Is.True);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.Advance(token, 1.7d), Is.True);
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Stage3));
        }

        [Test]
        public void HelpAndRepeatPreserveTheExactCallerState()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleChoice(token, Boat3Choice.Irrigation, 2d);
            context.Controller.Advance(token, 4.5d);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHelp(), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Help));
            Assert.That(context.Controller.RestoreFromHelp(captured), Is.True);
            Assert.That(context.Controller.Snapshot, Is.EqualTo(captured));
            var count = context.View.PresentationCount;
            Assert.That(context.Controller.HandleRepeat(5d), Is.True);
            Assert.That(context.Controller.Snapshot, Is.EqualTo(captured));
            Assert.That(context.View.PresentationCount, Is.EqualTo(count + 1));
        }

        [Test]
        public void PauseAndHomeRoundTripTheExactTravelSnapshot()
        {
            var context = Create();
            context.Controller.Enter(0d, true);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleFollowIbis(token, 1d);
            context.Controller.Advance(token, 3d);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandlePause(3d), Is.True);
            Assert.That(context.Pause.Captured, Is.EqualTo(captured));
            Assert.That(context.Controller.Restore(captured, 10d), Is.True);
            Assert.That(context.Controller.Snapshot.State, Is.EqualTo(Boat3State.Traveling));
            Assert.That(context.Controller.Snapshot.TravelElapsedSeconds, Is.EqualTo(captured.TravelElapsedSeconds));

            var homeCaptured = context.Controller.Snapshot;
            Assert.That(context.Controller.HandleHome(10d), Is.True);
            Assert.That(context.Leave.Caller, Is.EqualTo(RouteId.Boat3));
            var leavingCaller = context.Controller.Snapshot;
            Assert.That(context.Controller.RestoreFromLeave(leavingCaller, 11d), Is.False,
                "Only the snapshot captured before Leave may restore the caller.");
            Assert.That(context.Controller.RestoreFromLeave(homeCaptured, 11d), Is.True);
        }

        [Test]
        public void ArrivalPausePreservesRemainingPointSevenSeconds()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleSkip(token, 1d);
            context.Controller.Advance(token, 1.2d);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandlePause(1.2d), Is.True);
            Assert.That(context.Controller.Restore(captured, 10d), Is.True);
            Assert.That(context.Controller.Advance(token, 10.499d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Arriving));
            Assert.That(context.Controller.Advance(token, 10.5d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat3State.Navigating));
        }

        [Test]
        public void TravelClockCannotMoveBackward()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleChoice(token, Boat3Choice.GrainStore, 1d);
            context.Controller.Advance(token, 5d);
            var captured = context.Controller.Snapshot.TravelElapsedSeconds;

            Assert.That(context.Controller.Advance(token, 4d), Is.False);
            Assert.That(context.Controller.Snapshot.TravelElapsedSeconds, Is.EqualTo(captured));
        }

        [Test]
        public void NonFiniteClocksAreRejected()
        {
            var context = Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => context.Controller.Enter(double.NaN, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => context.Controller.Enter(double.PositiveInfinity, false));
        }

        [Test]
        public void WrongClockSessionTokenAndLockedRouteFailClosed()
        {
            var context = Create();
            context.Controller.Enter(10d, false);
            Assert.That(context.Controller.HandleChoice(context.Controller.CurrentTokenId, Boat3Choice.GrainStore, 9d), Is.False);
            Assert.That(context.Controller.HandleChoice(Guid.NewGuid(), Boat3Choice.GrainStore, 11d), Is.False);

            var closed = Create(false);
            Assert.That(closed.Controller.Enter(0d, false), Is.False);
            var locked = Create();
            locked.Navigation.Request(RouteId.Stage3, 0d);
            Assert.That(locked.Controller.Enter(0d, false), Is.False);
        }

        [Test]
        public void TimedOutStage3BoundaryAcceptsOnlyFreshRetry()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleSkip(token, 1d);
            context.Controller.Advance(token, 1.7d);
            var stale = context.Navigation.ActiveTransaction.Id;
            context.Navigation.CheckTimeout(11.7d);

            Assert.That(context.Controller.EnterRecovery(), Is.True);
            Assert.That(context.Navigation.Complete(stale), Is.EqualTo(NavigationCompletion.RejectedStale));
            var retry = context.Navigation.Retry(12d);
            Assert.That(retry.Accepted, Is.True);
            Assert.That(context.Controller.RetryBoundary(), Is.True);
            Assert.That(retry.Transaction.Id, Is.Not.EqualTo(stale));
        }

        private static TestContext Create(bool beginSession = true)
        {
            var transactions = new Queue<Guid>(new[]
            {
                Guid.Parse("50000000-0000-0000-0000-000000000001"),
                Guid.Parse("50000000-0000-0000-0000-000000000002"),
            });
            var tokens = new Queue<Guid>(new[]
            {
                Guid.Parse("60000000-0000-0000-0000-000000000001"),
                Guid.Parse("60000000-0000-0000-0000-000000000002"),
            });
            var session = new SessionState();
            if (beginSession) session.Begin(RouteId.Boat3);
            var navigation = new NavigationAuthority(RouteId.Boat3, () => transactions.Dequeue());
            var view = new RecordingView();
            var pause = new RecordingPause();
            var leave = new RecordingLeave();
            var controller = new Boat3Controller(session, navigation, view, pause, leave, () => tokens.Dequeue());
            return new TestContext(controller, navigation, view, pause, leave);
        }

        private sealed class RecordingView : IBoat3View
        {
            public Boat3Snapshot LastSnapshot { get; private set; }
            public bool InputReady { get; private set; }
            public int PresentationCount { get; private set; }
            public void Present(Boat3Snapshot snapshot, bool reducedMotion) { LastSnapshot = snapshot; PresentationCount++; }
            public void SetInputReady(bool ready) => InputReady = ready;
        }

        private sealed class RecordingPause : IBoat3PauseSink
        {
            public Boat3Snapshot Captured { get; private set; }
            public bool TryShow(Boat3Snapshot snapshot) { Captured = snapshot; return true; }
        }

        private sealed class RecordingLeave : ILeaveConfirmationSink
        {
            public RouteId? Caller { get; private set; }
            public bool TryShow(RouteId caller) { Caller = caller; return true; }
        }

        private readonly struct TestContext
        {
            public TestContext(Boat3Controller controller, NavigationAuthority navigation, RecordingView view, RecordingPause pause, RecordingLeave leave)
            { Controller = controller; Navigation = navigation; View = view; Pause = pause; Leave = leave; }
            public Boat3Controller Controller { get; }
            public NavigationAuthority Navigation { get; }
            public RecordingView View { get; }
            public RecordingPause Pause { get; }
            public RecordingLeave Leave { get; }
        }
    }
}
