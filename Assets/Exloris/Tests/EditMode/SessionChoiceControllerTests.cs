using System;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Welcome;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class SessionChoiceControllerTests
    {
        [Test]
        public void ContinueRequestsTheExactOpenSessionLocation()
        {
            var session = new SessionState();
            session.Begin(RouteId.Stage2);
            var navigation = new NavigationAuthority(RouteId.Welcome);
            var view = new RecordingWelcomeView();
            var controller = new SessionChoiceController(session, navigation, view);

            var accepted = controller.HandleContinue(30.0);

            Assert.That(accepted, Is.True);
            Assert.That(navigation.ActiveTransaction, Is.Not.Null);
            Assert.That(navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Stage2));
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage2));
        }

        [Test]
        public void ContinueWithoutOpenSessionIsRejectedWithoutGuessingDestination()
        {
            var session = new SessionState();
            var navigation = new NavigationAuthority(RouteId.Welcome);
            var controller = new SessionChoiceController(
                session,
                navigation,
                new RecordingWelcomeView());

            var accepted = controller.HandleContinue(31.0);

            Assert.That(accepted, Is.False);
            Assert.That(navigation.ActiveTransaction, Is.Null);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(RouteId.Welcome));
        }

        [Test]
        public void BackReturnsToWelcomeWithoutChangingOpenSession()
        {
            var session = new SessionState();
            session.Begin(RouteId.Boat1);
            var navigation = new NavigationAuthority(RouteId.Welcome);
            var view = new RecordingWelcomeView();
            var controller = new SessionChoiceController(session, navigation, view);

            var accepted = controller.HandleBack();

            Assert.That(accepted, Is.True);
            Assert.That(view.State, Is.EqualTo(WelcomePresentationState.Welcome));
            Assert.That(session.IsOpen, Is.True);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Boat1));
            Assert.That(navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void RestartRequestsAdultGateOnceWithoutOwningItsHold()
        {
            var session = new SessionState();
            session.Begin(RouteId.Stage3);
            var navigation = new NavigationAuthority(RouteId.Welcome);
            var sink = new RecordingAdultGateSink();
            var controller = new SessionChoiceController(
                session,
                navigation,
                new RecordingWelcomeView(),
                sink);

            Assert.That(controller.HandleRestart(), Is.True);
            Assert.That(controller.HandleRestart(), Is.False);

            Assert.That(sink.Count, Is.EqualTo(1));
            Assert.That(sink.LastRequest.Purpose, Is.EqualTo(AdultGatePurpose.Restart));
            Assert.That(sink.LastRequest.Origin, Is.EqualTo(AdultGateRequestOrigin.SessionChoice));
            Assert.That(sink.LastRequest.ReturnRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(sink.LastRequest.ReturnStateToken, Is.EqualTo(Guid.Empty));
            Assert.That(sink.LastRequest.ReturnPresentationId, Is.EqualTo("session_choice"));
            Assert.That(navigation.ActiveTransaction, Is.Null);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage3));
        }

        private sealed class RecordingWelcomeView : IWelcomeView
        {
            public WelcomePresentationState State { get; private set; } =
                WelcomePresentationState.SessionChoice;

            public void Show(WelcomePresentationState state)
            {
                State = state;
            }
        }

        private sealed class RecordingAdultGateSink : IAdultGateRequestSink
        {
            public int Count { get; private set; }
            public AdultGateRequest LastRequest { get; private set; }

            public bool TryRequest(AdultGateRequest request)
            {
                Count++;
                LastRequest = request;
                return true;
            }
        }
    }
}
