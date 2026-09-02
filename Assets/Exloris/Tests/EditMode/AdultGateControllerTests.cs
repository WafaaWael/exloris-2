using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Runtime.Adult;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class AdultGateControllerTests
    {
        [TestCase(AdultGateRequestOrigin.Welcome, AdultGatePurpose.Dashboard, RouteId.Welcome)]
        [TestCase(AdultGateRequestOrigin.Completion, AdultGatePurpose.Dashboard, RouteId.Completion)]
        [TestCase(AdultGateRequestOrigin.SessionChoice, AdultGatePurpose.Restart, RouteId.Welcome)]
        [TestCase(AdultGateRequestOrigin.ParentReport, AdultGatePurpose.Restart, RouteId.ParentReport)]
        public void ExactAllowedRequestEntersGateOnce(
            AdultGateRequestOrigin origin, AdultGatePurpose purpose, RouteId caller)
        {
            var context = Create(origin, purpose, caller);
            Assert.That(context.Controller.Enter(context.Request, context.EntryId, 5d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.Holding));
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.AdultGate));
            Assert.That(context.Controller.Snapshot.HoldProgress, Is.Zero);
            Assert.That(context.Controller.Enter(context.Request, context.EntryId, 5d), Is.False);
        }

        [Test]
        public void MismatchedPurposeOriginReturnAndTokenFailClosed()
        {
            var context = Create(AdultGateRequestOrigin.Completion, AdultGatePurpose.Dashboard, RouteId.Completion);
            Assert.That(context.Controller.Enter(
                new AdultGateRequest(AdultGatePurpose.Restart, AdultGateRequestOrigin.Completion, RouteId.Completion, Guid.NewGuid()),
                context.EntryId, 5d), Is.False);
            Assert.That(context.Controller.Enter(
                new AdultGateRequest(AdultGatePurpose.Dashboard, AdultGateRequestOrigin.Completion, RouteId.Welcome, Guid.NewGuid()),
                context.EntryId, 5d), Is.False);
            Assert.That(context.Controller.Enter(
                new AdultGateRequest(AdultGatePurpose.Dashboard, AdultGateRequestOrigin.Completion, RouteId.Completion, Guid.Empty),
                context.EntryId, 5d), Is.False);
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.Completion));
        }

        [Test]
        public void ExactThreeSecondHoldRequiresSeparateConfirmEvent()
        {
            var context = CreateAndEnter(AdultGateRequestOrigin.Completion, AdultGatePurpose.Dashboard, RouteId.Completion);
            var token = context.Controller.CurrentTokenId;
            var pointer = 7;
            Assert.That(context.Controller.HandleHoldBegin(token, pointer, 6d), Is.True);
            Assert.That(context.Controller.AdvanceHold(token, pointer, 8.999d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.Holding));
            Assert.That(context.Controller.Snapshot.HoldProgress, Is.EqualTo(0.999666666d).Within(0.000001d));
            Assert.That(context.Controller.HandleConfirm(token, 8.999d), Is.False);
            Assert.That(context.Controller.AdvanceHold(token, pointer, 9d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.AwaitingConfirm));
            Assert.That(context.Controller.HandleHoldRelease(token, pointer, 9d), Is.False);
            Assert.That(context.Controller.HandleConfirm(token, 9d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.NavigatingReport));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.ParentReport));
        }

        [TestCase(0.001d)]
        [TestCase(2.999d)]
        public void EarlyReleaseResetsAllProgressAndRemainsOnGate(double elapsed)
        {
            var context = CreateAndEnter(AdultGateRequestOrigin.Welcome, AdultGatePurpose.Dashboard, RouteId.Welcome);
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandleHoldBegin(token, 3, 10d), Is.True);
            Assert.That(context.Controller.AdvanceHold(token, 3, 10d + elapsed), Is.True);
            Assert.That(context.Controller.HandleHoldRelease(token, 3, 10d + elapsed), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.Holding));
            Assert.That(context.Controller.Snapshot.HoldProgress, Is.Zero);
            Assert.That(context.Controller.Snapshot.ActivePointerId, Is.EqualTo(-1));
        }

        [Test]
        public void WrongPointerSecondPointerExitAndInterruptionResetOrRejectSafely()
        {
            var context = CreateAndEnter(AdultGateRequestOrigin.Completion, AdultGatePurpose.Dashboard, RouteId.Completion);
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandleHoldBegin(token, 4, 10d), Is.True);
            Assert.That(context.Controller.HandleHoldBegin(token, 5, 10.1d), Is.False);
            Assert.That(context.Controller.AdvanceHold(token, 5, 11d), Is.False);
            Assert.That(context.Controller.HandleHoldRelease(token, 5, 11d), Is.False);
            Assert.That(context.Controller.HandlePointerExit(token, 4), Is.True);
            Assert.That(context.Controller.Snapshot.HoldProgress, Is.Zero);

            Assert.That(context.Controller.HandleHoldBegin(token, 4, 12d), Is.True);
            Assert.That(context.Controller.AdvanceHold(token, 4, 13d), Is.True);
            Assert.That(context.Controller.HandleInterruption(), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.Holding));
            Assert.That(context.Controller.Snapshot.HoldProgress, Is.Zero);
        }

        [Test]
        public void NonFiniteBackwardAndStaleClockCannotAccumulateProgress()
        {
            var context = CreateAndEnter(AdultGateRequestOrigin.Completion, AdultGatePurpose.Dashboard, RouteId.Completion);
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandleHoldBegin(token, 1, 10d), Is.True);
            Assert.That(context.Controller.AdvanceHold(token, 1, double.NaN), Is.False);
            Assert.That(context.Controller.AdvanceHold(token, 1, 9.9d), Is.False);
            Assert.That(context.Controller.AdvanceHold(Guid.NewGuid(), 1, 13d), Is.False);
            Assert.That(context.Controller.Snapshot.HoldProgress, Is.Zero);
        }

        [Test]
        public void RestartPurposeOpensInternalConfirmationAndResetRunsOnce()
        {
            var context = CreateAndEnter(AdultGateRequestOrigin.ParentReport, AdultGatePurpose.Restart, RouteId.ParentReport);
            CompleteHold(context, 10d);
            Assert.That(context.Controller.HandleConfirm(context.Controller.CurrentTokenId, 13.1d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.ResetConfirmation));
            Assert.That(context.Reset.Count, Is.Zero);
            Assert.That(context.Controller.HandleResetConfirm(context.Controller.CurrentTokenId), Is.True);
            Assert.That(context.Reset.Count, Is.EqualTo(1));
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.Resetting));
            Assert.That(context.Controller.HandleResetConfirm(context.Controller.CurrentTokenId), Is.False);
        }

        [Test]
        public void GateCancelAndResetCancelRestoreExactStoredCallerWithoutReset()
        {
            var gate = CreateAndEnter(AdultGateRequestOrigin.Completion, AdultGatePurpose.Dashboard, RouteId.Completion);
            Assert.That(gate.Controller.HandleCancel(gate.Controller.CurrentTokenId), Is.True);
            Assert.That(gate.Return.Count, Is.EqualTo(1));
            Assert.That(gate.Return.LastRequest.ReturnRoute, Is.EqualTo(RouteId.Completion));

            var reset = CreateAndEnter(AdultGateRequestOrigin.ParentReport, AdultGatePurpose.Restart, RouteId.ParentReport);
            CompleteHold(reset, 20d);
            reset.Controller.HandleConfirm(reset.Controller.CurrentTokenId, 23.1d);
            Assert.That(reset.Controller.HandleResetCancel(reset.Controller.CurrentTokenId), Is.True);
            Assert.That(reset.Return.LastRequest.ReturnRoute, Is.EqualTo(RouteId.ParentReport));
            Assert.That(reset.Reset.Count, Is.Zero);
        }

        private static void CompleteHold(Context context, double start)
        {
            Assert.That(context.Controller.HandleHoldBegin(context.Controller.CurrentTokenId, 1, start), Is.True);
            Assert.That(context.Controller.AdvanceHold(context.Controller.CurrentTokenId, 1, start + 3d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(AdultGateState.AwaitingConfirm));
        }

        private static Context CreateAndEnter(AdultGateRequestOrigin origin, AdultGatePurpose purpose, RouteId caller)
        {
            var context = Create(origin, purpose, caller);
            Assert.That(context.Controller.Enter(context.Request, context.EntryId, 5d), Is.True);
            return context;
        }

        private static Context Create(AdultGateRequestOrigin origin, AdultGatePurpose purpose, RouteId caller)
        {
            var ids = new Queue<Guid>(new[]
            {
                Guid.Parse("30000000-0000-0000-0000-000000000001"),
                Guid.Parse("30000000-0000-0000-0000-000000000002"),
                Guid.Parse("30000000-0000-0000-0000-000000000003"),
            });
            var navigation = new NavigationAuthority(caller, () => ids.Dequeue());
            var entry = purpose == AdultGatePurpose.Dashboard
                ? navigation.RequestAdultDashboard(origin, 4d)
                : navigation.RequestAdultRestart(origin, 4d);
            var returnToken = origin == AdultGateRequestOrigin.Welcome || origin == AdultGateRequestOrigin.SessionChoice
                ? Guid.Empty
                : Guid.Parse("40000000-0000-0000-0000-000000000001");
            var returnPresentation = origin == AdultGateRequestOrigin.Welcome
                ? "welcome"
                : origin == AdultGateRequestOrigin.SessionChoice ? "session_choice" : null;
            var request = new AdultGateRequest(purpose, origin, caller, returnToken, returnPresentation);
            var returns = new RecordingReturnSink();
            var reset = new RecordingResetSink();
            var controller = new AdultGateController(navigation, new RecordingView(), returns, reset, () => ids.Dequeue());
            return new Context(navigation, controller, request, returns, reset, entry.Transaction.Id);
        }

        private sealed class Context
        {
            public Context(NavigationAuthority navigation, AdultGateController controller, AdultGateRequest request,
                RecordingReturnSink returns, RecordingResetSink reset, Guid entryId)
            { Navigation = navigation; Controller = controller; Request = request; Return = returns; Reset = reset; EntryId = entryId; }
            public NavigationAuthority Navigation { get; }
            public AdultGateController Controller { get; }
            public AdultGateRequest Request { get; }
            public RecordingReturnSink Return { get; }
            public RecordingResetSink Reset { get; }
            public Guid EntryId { get; }
        }

        private sealed class RecordingView : IAdultGateView
        {
            public void Present(AdultGateSnapshot snapshot) { }
            public void SetInputReady(bool ready) { }
        }

        private sealed class RecordingReturnSink : IAdultGateReturnSink
        {
            public int Count;
            public AdultGateRequest LastRequest;
            public bool TryReturn(AdultGateRequest request) { Count++; LastRequest = request; return true; }
        }

        private sealed class RecordingResetSink : IConfirmedSessionResetSink
        {
            public int Count;
            public bool TryReset() { Count++; return true; }
        }
    }
}
