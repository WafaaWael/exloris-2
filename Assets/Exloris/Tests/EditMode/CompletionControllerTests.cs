using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Completion;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionControllerTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CurrentStage3TransactionEntersNeutralCompletionExactlyOnce(bool reducedMotion)
        {
            var context = Create();

            Assert.That(context.Controller.Enter(context.EntryId, 10d, reducedMotion), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.Presenting));
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.Completion));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Completion));
            Assert.That(context.Controller.Snapshot.ReducedMotion, Is.EqualTo(reducedMotion));
            Assert.That(context.Controller.Enter(context.EntryId, 10d, reducedMotion), Is.False);
            Assert.That(context.Session.Observations, Is.Empty);
        }

        [Test]
        public void WrongStaleAndInvalidEntryFailWithoutMutation()
        {
            var context = Create();
            Assert.That(context.Controller.Enter(Guid.Empty, 10d, false), Is.False);
            Assert.That(context.Controller.Enter(Guid.NewGuid(), 10d, false), Is.False);
            Assert.That(context.Controller.Enter(context.EntryId, double.NaN, false), Is.False);
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.Stage3));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage3));
        }

        [Test]
        public void ExactSemanticBoundaryUnlocksOnceAndRejectsStaleOrBackwardTime()
        {
            var context = CreateAndEnter();
            var route = context.Controller.CurrentTokenId;
            var presentation = context.Controller.CurrentPresentationTokenId;

            Assert.That(context.Controller.Advance(route, presentation, 13.645d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.Presenting));
            Assert.That(context.Controller.Snapshot.ElapsedSeconds, Is.EqualTo(3.645d).Within(0.000001d));
            Assert.That(context.Controller.Advance(route, Guid.NewGuid(), 13.646d), Is.False);
            Assert.That(context.Controller.Advance(route, presentation, 13.644d), Is.False);
            Assert.That(context.Controller.Advance(route, presentation, 13.646d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.Ready));
            Assert.That(context.Controller.Snapshot.ElapsedSeconds, Is.EqualTo(CompletionController.SemanticSeconds));
            Assert.That(context.Controller.Advance(route, presentation, 14d), Is.False);
        }

        [Test]
        public void AdultAreaIsLockedUntilReadyAndCreatesOneExactDashboardRequest()
        {
            var context = CreateAndEnter();
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandleAdult(token, 10.1d), Is.False);
            Ready(context);

            Assert.That(context.Controller.HandleAdult(token, 14d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.NavigatingAdult));
            Assert.That(context.Adult.RequestCount, Is.EqualTo(1));
            Assert.That(context.Adult.LastRequest.Purpose, Is.EqualTo(AdultGatePurpose.Dashboard));
            Assert.That(context.Adult.LastRequest.Origin, Is.EqualTo(AdultGateRequestOrigin.Completion));
            Assert.That(context.Adult.LastRequest.ReturnRoute, Is.EqualTo(RouteId.Completion));
            Assert.That(context.Adult.LastRequest.ReturnStateToken, Is.EqualTo(token));
            Assert.That(context.Controller.HandleAdult(token, 14.1d), Is.False);
        }

        [Test]
        public void RejectedAdultRequestLeavesReadyStateUntouched()
        {
            var context = CreateAndEnter();
            Ready(context);
            context.Adult.Accept = false;
            var before = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleAdult(context.Controller.CurrentTokenId, 14d), Is.False);
            Assert.That(context.Controller.Snapshot, Is.EqualTo(before));
        }

        [Test]
        public void HomeIsLockedUntilReadyAndRestoresExactSnapshot()
        {
            var context = CreateAndEnter();
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandleHome(token, 10.1d), Is.False);
            Ready(context);
            context.Controller.TrySetFocus(token, "completion.adult");
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHome(token, 14d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.Leaving));
            Assert.That(context.Leave.Caller, Is.EqualTo(RouteId.Completion));
            Assert.That(context.Controller.RestoreFromLeave(captured, 15d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.Ready));
            Assert.That(context.Controller.Snapshot.FocusedControlId, Is.EqualTo("completion.adult"));
        }

        [Test]
        public void PauseDuringPresentationShiftsStartAndPreservesExactRemainingTime()
        {
            var context = CreateAndEnter();
            var token = context.Controller.CurrentTokenId;
            var presentation = context.Controller.CurrentPresentationTokenId;
            context.Controller.Advance(token, presentation, 11.2d);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandlePause(token, 11.2d), Is.True);
            Assert.That(context.Controller.RestoreFromPause(captured, 21.2d), Is.True);
            Assert.That(context.Controller.Advance(token, presentation, 23.645d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.Presenting));
            Assert.That(context.Controller.Advance(token, presentation, 23.646d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.Ready));
        }

        [Test]
        public void DuplicateTokensAndNonFiniteActionsAreInertAndNeverRecordFacts()
        {
            var context = CreateAndEnter();
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.Advance(Guid.NewGuid(), context.Controller.CurrentPresentationTokenId, 14d), Is.False);
            Assert.That(context.Controller.Advance(token, context.Controller.CurrentPresentationTokenId, double.PositiveInfinity), Is.False);
            Assert.That(context.Controller.HandleAdult(Guid.NewGuid(), 14d), Is.False);
            Assert.That(context.Controller.HandleHome(Guid.NewGuid(), 14d), Is.False);
            Assert.That(context.Session.Observations, Is.Empty);
        }

        private static Context CreateAndEnter()
        {
            var context = Create();
            Assert.That(context.Controller.Enter(context.EntryId, 10d, false), Is.True);
            return context;
        }

        private static void Ready(Context context)
        {
            Assert.That(context.Controller.Advance(
                context.Controller.CurrentTokenId,
                context.Controller.CurrentPresentationTokenId,
                10d + CompletionController.SemanticSeconds), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(CompletionState.Ready));
        }

        private static Context Create()
        {
            var ids = new Queue<Guid>(new[]
            {
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Guid.Parse("20000000-0000-0000-0000-000000000002"),
                Guid.Parse("20000000-0000-0000-0000-000000000003"),
            });
            var session = new SessionState();
            session.Begin(RouteId.Stage3);
            var navigation = new NavigationAuthority(RouteId.Stage3, () => ids.Dequeue());
            var entry = navigation.Request(RouteId.Completion, 9d).Transaction.Id;
            var view = new RecordingView();
            var adult = new RecordingAdultSink();
            var pause = new RecordingPauseSink();
            var leave = new RecordingLeaveSink();
            var controller = new CompletionController(session, navigation, view, adult, pause, leave, () => ids.Dequeue());
            return new Context(session, navigation, controller, adult, pause, leave, entry);
        }

        private sealed class Context
        {
            public Context(SessionState session, NavigationAuthority navigation, CompletionController controller,
                RecordingAdultSink adult, RecordingPauseSink pause, RecordingLeaveSink leave, Guid entryId)
            { Session = session; Navigation = navigation; Controller = controller; Adult = adult; Pause = pause; Leave = leave; EntryId = entryId; }
            public SessionState Session { get; }
            public NavigationAuthority Navigation { get; }
            public CompletionController Controller { get; }
            public RecordingAdultSink Adult { get; }
            public RecordingPauseSink Pause { get; }
            public RecordingLeaveSink Leave { get; }
            public Guid EntryId { get; }
        }

        private sealed class RecordingView : ICompletionView
        {
            public void Present(CompletionSnapshot snapshot) { }
            public void SetInputReady(bool ready) { }
        }

        private sealed class RecordingAdultSink : IAdultGateRequestSink
        {
            public bool Accept = true;
            public int RequestCount;
            public AdultGateRequest LastRequest;
            public bool TryRequest(AdultGateRequest request)
            { if (!Accept) return false; RequestCount++; LastRequest = request; return true; }
        }

        private sealed class RecordingPauseSink : ICompletionPauseSink
        {
            public bool TryShow(CompletionSnapshot snapshot) => true;
        }

        private sealed class RecordingLeaveSink : ILeaveConfirmationSink
        {
            public RouteId Caller;
            public bool TryShow(RouteId caller) { Caller = caller; return true; }
        }
    }
}
