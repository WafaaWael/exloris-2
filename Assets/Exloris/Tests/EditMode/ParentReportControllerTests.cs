using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Adult;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class ParentReportControllerTests
    {
        [TestCase(AdultGateRequestOrigin.Welcome, RouteId.Welcome, "welcome")]
        [TestCase(AdultGateRequestOrigin.Completion, RouteId.Completion, null)]
        public void ExactDashboardTransactionBuildsOneImmutableCurrentSessionSnapshot(
            AdultGateRequestOrigin origin, RouteId returnRoute, string presentationId)
        {
            var context = Create(origin, returnRoute, presentationId);
            context.Session.Record(ObservationEvent.BirdGuideUsed(2));

            Assert.That(context.Controller.Enter(context.SourceRequest, context.EntryId, context.Session, 5d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(ParentReportState.Ready));
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.ParentReport));
            Assert.That(context.Controller.Snapshot.Cards, Has.Count.EqualTo(2));
            Assert.That(context.Controller.Enter(context.SourceRequest, context.EntryId, context.Session, 5d), Is.False);

            context.Session.Record(ObservationEvent.BirdGuideUsed(8));
            Assert.That(context.Controller.Snapshot.Cards, Has.Count.EqualTo(2));
            Assert.That(context.Controller.Snapshot.Cards[1].Text,
                Is.EqualTo("Your child used the bird guide 2 time(s) during the boat journeys."));
        }

        [Test]
        public void CloseRequestsOnlyTheExactStoredDashboardCaller()
        {
            var context = Create(AdultGateRequestOrigin.Completion, RouteId.Completion, null);
            context.Controller.Enter(context.SourceRequest, context.EntryId, context.Session, 5d);

            Assert.That(context.Controller.HandleClose(context.Controller.CurrentTokenId, 6d), Is.True);
            Assert.That(context.Navigation.ActiveTransaction.Caller, Is.EqualTo(RouteId.ParentReport));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Completion));
            Assert.That(context.Controller.State, Is.EqualTo(ParentReportState.NavigatingClose));
            Assert.That(context.Controller.HandleClose(context.Controller.CurrentTokenId, 6d), Is.False);
        }

        [Test]
        public void RestartPreservesReportViewStateAndRequestsProtectedAdultGateWithoutClearingSession()
        {
            var context = Create(AdultGateRequestOrigin.Completion, RouteId.Completion, null);
            context.Session.Record(ObservationEvent.BirdGuideUsed(2));
            context.Controller.Enter(context.SourceRequest, context.EntryId, context.Session, 5d);

            Assert.That(context.Controller.HandleRestart(
                context.Controller.CurrentTokenId, 0.42f, "parent_report.card.2"), Is.True);
            Assert.That(context.Adult.Count, Is.EqualTo(1));
            Assert.That(context.Adult.LastRequest.Purpose, Is.EqualTo(AdultGatePurpose.Restart));
            Assert.That(context.Adult.LastRequest.Origin, Is.EqualTo(AdultGateRequestOrigin.ParentReport));
            Assert.That(context.Adult.LastRequest.ReturnRoute, Is.EqualTo(RouteId.ParentReport));
            Assert.That(context.Adult.LastRequest.ReturnStateToken, Is.EqualTo(context.Controller.CurrentTokenId));
            Assert.That(context.Controller.Snapshot.ScrollNormalized, Is.EqualTo(0.42f));
            Assert.That(context.Controller.Snapshot.FocusedControlId, Is.EqualTo("parent_report.card.2"));
            Assert.That(context.Session.IsOpen, Is.True);
            Assert.That(context.Session.Observations, Has.Count.EqualTo(1));
            Assert.That(context.Controller.HandleRestart(
                context.Controller.CurrentTokenId, 0.42f, "parent_report.card.2"), Is.False);
        }

        [Test]
        public void StaleInvalidAndNonFiniteActionsAreInert()
        {
            var context = Create(AdultGateRequestOrigin.Completion, RouteId.Completion, null);
            Assert.That(context.Controller.Enter(context.SourceRequest, context.EntryId, context.Session, double.NaN), Is.False);
            Assert.That(context.Controller.Enter(context.SourceRequest, context.EntryId, context.Session, 5d), Is.True);
            Assert.That(context.Controller.HandleClose(Guid.NewGuid(), 6d), Is.False);
            Assert.That(context.Controller.HandleClose(context.Controller.CurrentTokenId, double.PositiveInfinity), Is.False);
            Assert.That(context.Controller.HandleRestart(
                context.Controller.CurrentTokenId, -0.1f, "restart"), Is.False);
            Assert.That(context.Controller.HandleRestart(
                context.Controller.CurrentTokenId, 0.5f, null), Is.False);
            Assert.That(context.Adult.Count, Is.Zero);
        }

        private static Context Create(
            AdultGateRequestOrigin origin, RouteId returnRoute, string presentationId)
        {
            var ids = new Queue<Guid>(new[]
            {
                Guid.Parse("51000000-0000-0000-0000-000000000001"),
                Guid.Parse("51000000-0000-0000-0000-000000000002"),
                Guid.Parse("51000000-0000-0000-0000-000000000003"),
            });
            var navigation = new NavigationAuthority(RouteId.AdultGate, () => ids.Dequeue());
            var entry = navigation.Request(RouteId.ParentReport, 4d);
            var sourceToken = origin == AdultGateRequestOrigin.Completion
                ? Guid.Parse("52000000-0000-0000-0000-000000000001")
                : Guid.Empty;
            var request = new AdultGateRequest(
                AdultGatePurpose.Dashboard, origin, returnRoute, sourceToken, presentationId);
            var adult = new RecordingAdultSink();
            var view = new RecordingView();
            var controller = new ParentReportController(navigation, view, adult, () => ids.Dequeue());
            var session = new SessionState();
            if (origin == AdultGateRequestOrigin.Completion) session.Begin(RouteId.Completion);
            return new Context(
                session, navigation, controller, adult, request, entry.Transaction.Id);
        }

        private sealed class Context
        {
            public Context(SessionState session, NavigationAuthority navigation,
                ParentReportController controller, RecordingAdultSink adult,
                AdultGateRequest sourceRequest, Guid entryId)
            { Session = session; Navigation = navigation; Controller = controller; Adult = adult; SourceRequest = sourceRequest; EntryId = entryId; }
            public SessionState Session { get; }
            public NavigationAuthority Navigation { get; }
            public ParentReportController Controller { get; }
            public RecordingAdultSink Adult { get; }
            public AdultGateRequest SourceRequest { get; }
            public Guid EntryId { get; }
        }

        private sealed class RecordingView : IParentReportView
        {
            public void Present(ParentReportSnapshot snapshot) { }
            public void SetInputReady(bool ready) { }
        }

        private sealed class RecordingAdultSink : IAdultGateRequestSink
        {
            public int Count;
            public AdultGateRequest LastRequest;
            public bool TryRequest(AdultGateRequest request) { Count++; LastRequest = request; return true; }
        }
    }
}
