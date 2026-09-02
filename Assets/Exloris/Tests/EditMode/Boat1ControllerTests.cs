using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat1ControllerTests
    {
        [Test]
        public void RuntimeAssemblyContainsBoat1Controller()
        {
            var type = Type.GetType("Exloris.Runtime.Boat.Boat1Controller, Exloris.Runtime");

            Assert.That(type, Is.Not.Null,
                "Boat 1 must have a dedicated runtime controller before behavior is implemented.");
        }

        [Test]
        public void EntryCreatesChoiceStateWithFixedVisibleRightCue()
        {
            var context = Create();

            Assert.That(context.Controller.Enter(10d, false), Is.True);

            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Choice));
            Assert.That(context.Controller.Snapshot.IbisDirection, Is.EqualTo(BoatDirection.Right));
            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(Boat1Choice.None));
            Assert.That(context.Controller.Snapshot.TravelElapsedSeconds, Is.Zero);
            Assert.That(context.View.InputReady, Is.True);
        }

        [TestCase(Boat1Choice.Reeds)]
        [TestCase(Boat1Choice.Village)]
        public void LandmarkChoiceStartsTheSameTravelClock(Boat1Choice choice)
        {
            var context = Create();
            context.Controller.Enter(1d, false);
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleChoice(token, choice, 3d), Is.True);

            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Traveling));
            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(choice));
            Assert.That(context.Controller.Snapshot.StateStartedAtSeconds, Is.EqualTo(3d));
            Assert.That(context.Controller.Snapshot.TravelElapsedSeconds, Is.Zero);
        }

        [Test]
        public void ChoiceRejectsAClockEarlierThanEntry()
        {
            var context = Create();
            context.Controller.Enter(10d, false);

            Assert.That(context.Controller.HandleChoice(
                context.Controller.CurrentTokenId, Boat1Choice.Reeds, 9.999d), Is.False);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Choice));
            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(Boat1Choice.None));
        }

        [Test]
        public void FollowIbisAcceptsTheVisibleRightCueWithoutRandomRoute()
        {
            var context = Create();
            context.Controller.Enter(2d, false);

            Assert.That(context.Controller.HandleFollowIbis(
                context.Controller.CurrentTokenId, 4d), Is.True);

            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(Boat1Choice.FollowIbis));
            Assert.That(context.Controller.Snapshot.IbisDirection, Is.EqualTo(BoatDirection.Right));
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Traveling));
        }

        [Test]
        public void TravelEntersArrivalFeedbackAtSixThenRequestsStage1ExactlyOnce()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleChoice(token, Boat1Choice.Reeds, 5d);

            Assert.That(context.Controller.Advance(token, 10.999d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Traveling));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);

            Assert.That(context.Controller.Advance(token, 11d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Arriving));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.Snapshot.ArrivalCommitted, Is.False);

            Assert.That(context.Controller.Advance(token, 11.699d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Arriving));
            Assert.That(context.Controller.Advance(token, 11.7d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Navigating));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Stage1));
            Assert.That(context.Controller.Snapshot.ArrivalCommitted, Is.True);

            Assert.That(context.Controller.Advance(token, 12d), Is.False);
            Assert.That(context.Controller.HandleSkip(token, 12d), Is.False);
        }

        [Test]
        public void SkipFromChoiceCommitsArrivalOnceAndRejectsStaleOrDuplicateTokens()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleSkip(Guid.NewGuid(), 1d), Is.False);
            Assert.That(context.Controller.HandleSkip(token, 1d), Is.True);
            Assert.That(context.Controller.HandleSkip(token, 1.1d), Is.False);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Arriving));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.Advance(token, 1.7d), Is.True);
            Assert.That(context.Navigation.ActiveTransaction.Caller, Is.EqualTo(RouteId.Boat1));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Stage1));
        }

        [Test]
        public void InactivityNeverChoosesSpeaksOrAdvances()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var before = context.Controller.Snapshot;

            Assert.That(context.Controller.Advance(before.TokenId, 120d), Is.True);

            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Choice));
            Assert.That(context.Controller.Snapshot.Choice, Is.EqualTo(Boat1Choice.None));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.View.LastSnapshot.IbisDirection, Is.EqualTo(BoatDirection.Right));
        }

        [Test]
        public void HelpReturnsToTheExactCapturedBeatWithoutProgress()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleChoice(token, Boat1Choice.Village, 2d);
            context.Controller.Advance(token, 4.5d);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHelp(), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Help));
            Assert.That(context.Controller.RestoreFromHelp(captured), Is.True);

            AssertSnapshot(context.Controller.Snapshot, captured);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void RepeatRePresentsTheCurrentInstructionWithoutChangingState()
        {
            var context = Create();
            context.Controller.Enter(7d, false);
            var captured = context.Controller.Snapshot;
            var presentations = context.View.PresentationCount;

            Assert.That(context.Controller.HandleRepeat(8d), Is.True);

            AssertSnapshot(context.Controller.Snapshot, captured);
            Assert.That(context.View.PresentationCount, Is.EqualTo(presentations + 1));
        }

        [Test]
        public void PauseCapturesAndRestoresTheExactTravelState()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleChoice(token, Boat1Choice.Reeds, 2d);
            context.Controller.Advance(token, 4d);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandlePause(), Is.True);
            AssertSnapshot(context.Pause.Captured, captured);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Paused));
            Assert.That(context.Controller.Restore(captured), Is.True);
            AssertSnapshot(context.Controller.Snapshot, captured);
        }

        [Test]
        public void HomeCapturesAndRestoresTheExactTravelState()
        {
            var context = Create();
            context.Controller.Enter(0d, true);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleFollowIbis(token, 1d);
            context.Controller.Advance(token, 3d);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHome(), Is.True);
            Assert.That(context.Leave.Caller, Is.EqualTo(RouteId.Boat1));
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Leaving));
            Assert.That(context.Controller.RestoreFromLeave(captured), Is.True);
            AssertSnapshot(context.Controller.Snapshot, captured);
        }

        [Test]
        public void ArrivalPauseRestoresTheExactRemainingFeedbackTime()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandleSkip(token, 1d), Is.True);
            Assert.That(context.Controller.Advance(token, 1.2d), Is.True);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandlePause(1.2d), Is.True);
            AssertSnapshot(context.Pause.Captured, captured);
            Assert.That(context.Controller.Restore(captured, 10d), Is.True);
            Assert.That(context.Controller.Advance(token, 10.499d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Arriving));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.Advance(token, 10.5d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Navigating));
        }

        [Test]
        public void ArrivalHomeStayRestoresTheExactRemainingFeedbackTime()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandleSkip(token, 1d), Is.True);
            Assert.That(context.Controller.Advance(token, 1.3d), Is.True);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHome(1.3d), Is.True);
            Assert.That(context.Controller.RestoreFromLeave(captured, 20d), Is.True);
            Assert.That(context.Controller.Advance(token, 20.399d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Arriving));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Controller.Advance(token, 20.4d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Navigating));
        }

        [Test]
        public void InterruptionPauseCapturesAndRestoresTheDirectHelpLayer()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            Assert.That(context.Controller.HandleHelp(), Is.True);
            var help = context.Controller.Snapshot;

            Assert.That(context.Controller.HandlePause(1d), Is.True);
            Assert.That(context.Pause.Captured.State, Is.EqualTo(Boat1State.Help));
            Assert.That(context.Controller.Restore(help, 2d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Help));
            Assert.That(context.View.InputReady, Is.False);
        }

        [Test]
        public void InterruptionPauseCapturesAndRestoresTheDirectLeaveLayer()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var caller = context.Controller.Snapshot;
            Assert.That(context.Controller.HandleHome(1d), Is.True);
            var leaving = context.Controller.Snapshot;

            Assert.That(context.Controller.HandlePause(2d), Is.True);
            Assert.That(context.Pause.Captured.State, Is.EqualTo(Boat1State.Leaving));
            Assert.That(context.Controller.Restore(leaving, 3d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Leaving));
            Assert.That(context.View.InputReady, Is.False);
            Assert.That(context.Controller.RestoreFromLeave(caller, 4d), Is.True);
            AssertSnapshot(context.Controller.Snapshot, caller);
        }

        [Test]
        public void WrongSessionLockedNavigationAndRecoveryRejectEntryOrCommands()
        {
            var closed = Create(beginSession: false);
            Assert.That(closed.Controller.Enter(0d, false), Is.False);

            var locked = Create();
            locked.Navigation.Request(RouteId.Stage1, 0d);
            Assert.That(locked.Controller.Enter(0d, false), Is.False);

            var recovery = Create();
            recovery.Navigation.Request(RouteId.Stage1, 0d);
            recovery.Navigation.CheckTimeout(NavigationAuthority.TimeoutSeconds);
            Assert.That(recovery.Controller.Enter(0d, false), Is.False);
        }

        [Test]
        public void TimedOutStageBoundaryEntersRecoveryAndAcceptsOnlyTheFreshRetry()
        {
            var context = Create();
            context.Controller.Enter(0d, false);
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandleSkip(token, 1d);
            context.Controller.Advance(token, 1.7d);
            var staleTransaction = context.Navigation.ActiveTransaction.Id;
            context.Navigation.CheckTimeout(11.7d);

            Assert.That(context.Controller.EnterRecovery(), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Recovery));
            Assert.That(context.Navigation.Complete(staleTransaction),
                Is.EqualTo(NavigationCompletion.RejectedStale));

            var retry = context.Navigation.Retry(12d);
            Assert.That(retry.Accepted, Is.True);
            Assert.That(context.Controller.RetryBoundary(), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Boat1State.Navigating));
            Assert.That(retry.Transaction.Id, Is.Not.EqualTo(staleTransaction));
        }

        private static TestContext Create(bool beginSession = true)
        {
            var transactionIds = new Queue<Guid>(new[]
            {
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Guid.Parse("10000000-0000-0000-0000-000000000002"),
            });
            var journeyTokens = new Queue<Guid>(new[]
            {
                Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Guid.Parse("20000000-0000-0000-0000-000000000002"),
            });
            var session = new SessionState();
            if (beginSession) session.Begin(RouteId.Boat1);
            var navigation = new NavigationAuthority(RouteId.Boat1, () => transactionIds.Dequeue());
            var view = new RecordingView();
            var pause = new RecordingPause();
            var leave = new RecordingLeave();
            var controller = new Boat1Controller(
                session, navigation, view, pause, leave, () => journeyTokens.Dequeue());
            return new TestContext(controller, navigation, view, pause, leave);
        }

        private static void AssertSnapshot(Boat1Snapshot actual, Boat1Snapshot expected)
        {
            Assert.That(actual, Is.EqualTo(expected));
        }

        private sealed class RecordingView : IBoat1View
        {
            public Boat1Snapshot LastSnapshot { get; private set; }
            public bool InputReady { get; private set; }
            public int PresentationCount { get; private set; }

            public void Present(Boat1Snapshot snapshot, bool reducedMotion)
            {
                LastSnapshot = snapshot;
                PresentationCount++;
            }

            public void SetInputReady(bool ready) => InputReady = ready;
        }

        private sealed class RecordingPause : IBoat1PauseSink
        {
            public Boat1Snapshot Captured { get; private set; }

            public bool TryShow(Boat1Snapshot snapshot)
            {
                Captured = snapshot;
                return true;
            }
        }

        private sealed class RecordingLeave : ILeaveConfirmationSink
        {
            public RouteId? Caller { get; private set; }

            public bool TryShow(RouteId caller)
            {
                Caller = caller;
                return true;
            }
        }

        private readonly struct TestContext
        {
            public TestContext(
                Boat1Controller controller,
                NavigationAuthority navigation,
                RecordingView view,
                RecordingPause pause,
                RecordingLeave leave)
            {
                Controller = controller;
                Navigation = navigation;
                View = view;
                Pause = pause;
                Leave = leave;
            }

            public Boat1Controller Controller { get; }
            public NavigationAuthority Navigation { get; }
            public RecordingView View { get; }
            public RecordingPause Pause { get; }
            public RecordingLeave Leave { get; }
        }
    }
}
