using System;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Recovery;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class LeaveConfirmationControllerTests
    {
        [TestCase(RouteId.AvatarSelect)]
        [TestCase(RouteId.AvatarPreview)]
        [TestCase(RouteId.JourneyOverview)]
        [TestCase(RouteId.Boat2)]
        public void StayAndCancelRestoreExactCallerWithoutNavigation(RouteId caller)
        {
            var context = Create(caller);
            Assert.That(context.Controller.TryShow(caller), Is.True);
            Assert.That(context.View.BlockedCaller, Is.EqualTo(caller));

            Assert.That(context.Controller.HandleStay(), Is.True);
            Assert.That(context.View.RestoredCaller, Is.EqualTo(caller));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(caller));

            Assert.That(context.Controller.TryShow(caller), Is.True);
            Assert.That(context.Controller.HandleCancel(), Is.True);
            Assert.That(context.View.RestoredCaller, Is.EqualTo(caller));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [TestCase(RouteId.AvatarSelect)]
        [TestCase(RouteId.AvatarPreview)]
        [TestCase(RouteId.JourneyOverview)]
        [TestCase(RouteId.Boat2)]
        public void LeaveRequestsWelcomeExactlyOnceAndPreservesVolatileSession(RouteId caller)
        {
            var context = Create(caller);
            context.Session.SelectAvatar("avatar_girl");
            Assert.That(context.Controller.TryShow(caller), Is.True);

            Assert.That(context.Controller.HandleLeave(10d), Is.True);
            Assert.That(context.Controller.HandleLeave(10.1d), Is.False);
            Assert.That(context.Navigation.ActiveTransaction.Caller, Is.EqualTo(caller));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Welcome));
            Assert.That(context.Session.IsOpen, Is.True);
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(caller));
            Assert.That(context.Session.SelectedAvatarId, Is.EqualTo("avatar_girl"));
            Assert.That(context.View.TransitionCaller, Is.EqualTo(caller));
        }

        [Test]
        public void RejectsWrongCallerLockedNavigationAndDuplicateOverlay()
        {
            var context = Create(RouteId.AvatarSelect);
            Assert.That(context.Controller.TryShow(RouteId.AvatarPreview), Is.False);
            Assert.That(context.Controller.TryShow(RouteId.Welcome), Is.False);
            Assert.That(context.Controller.TryShow(RouteId.AvatarSelect), Is.True);
            Assert.That(context.Controller.TryShow(RouteId.AvatarSelect), Is.False);
            Assert.That(context.Controller.HandleCancel(), Is.True);

            Assert.That(context.Navigation.Request(RouteId.AvatarPreview, 20d).Accepted, Is.True);
            Assert.That(context.Controller.TryShow(RouteId.AvatarSelect), Is.False);
        }

        [TestCase(RouteId.JourneyOverview)]
        [TestCase(RouteId.Boat2)]
        public void NestedLeaveRestoresPauseAndRetainsStackUntilMatchingCompletion(RouteId caller)
        {
            var context = Create(caller);
            var request = new LeaveRequest(
                caller, LeaveReturnLayer.PauseOverlay, Guid.NewGuid());

            Assert.That(context.Controller.TryShow(request), Is.True);
            Assert.That(context.View.NestedBlocked, Is.EqualTo(request));
            Assert.That(context.Controller.HandleStay(), Is.True);
            Assert.That(context.View.NestedRestored, Is.EqualTo(request));

            Assert.That(context.Controller.TryShow(request), Is.True);
            Assert.That(context.Controller.HandleLeave(50d), Is.True);
            var transaction = context.Navigation.ActiveTransaction;
            Assert.That(context.Controller.State, Is.EqualTo(LeaveConfirmationState.Transitioning));
            Assert.That(context.Controller.TryShow(request), Is.False);
            Assert.That(context.Controller.CompleteTransition(Guid.NewGuid()), Is.False);
            Assert.That(context.Controller.CompleteTransition(transaction.Id), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(LeaveConfirmationState.Hidden));
        }

        private static Context Create(RouteId caller)
        {
            var session = new SessionState();
            session.Begin(caller);
            var navigation = new NavigationAuthority(caller, new IdFactory().Next);
            var view = new RecordingView();
            var controller = new LeaveConfirmationController(session, navigation, view);
            controller.BindNestedReturnSink(view);
            return new Context(session, navigation, view, controller);
        }

        private sealed class Context
        {
            public Context(SessionState session, NavigationAuthority navigation,
                RecordingView view, LeaveConfirmationController controller)
            {
                Session = session;
                Navigation = navigation;
                View = view;
                Controller = controller;
            }

            public SessionState Session { get; }
            public NavigationAuthority Navigation { get; }
            public RecordingView View { get; }
            public LeaveConfirmationController Controller { get; }
        }

        private sealed class RecordingView : ILeaveConfirmationView, INestedLeaveConfirmationView,
            INestedLeaveReturnSink
        {
            public RouteId? BlockedCaller { get; private set; }
            public RouteId? RestoredCaller { get; private set; }
            public RouteId? TransitionCaller { get; private set; }
            public LeaveRequest NestedBlocked { get; private set; }
            public LeaveRequest NestedRestored { get; private set; }
            public LeaveRequest NestedTransition { get; private set; }

            public void ShowAndBlockCaller(RouteId caller) => BlockedCaller = caller;
            public void HideAndRestoreCaller(RouteId caller) => RestoredCaller = caller;
            public void HoldBlockedForTransition(RouteId caller) => TransitionCaller = caller;
            public void ShowAndBlockPause(LeaveRequest request) => NestedBlocked = request;
            public void HideAndRestorePause(LeaveRequest request) => NestedRestored = request;
            public void HoldPauseBlockedForTransition(LeaveRequest request) => NestedTransition = request;
            public bool RestoreFromLeave(LeaveRequest request) => true;
            public bool HoldForLeaveTransition(LeaveRequest request) => true;
        }

        private sealed class IdFactory
        {
            private byte _value;
            public Guid Next()
            {
                var bytes = new byte[16];
                bytes[15] = ++_value;
                return new Guid(bytes);
            }
        }
    }
}
