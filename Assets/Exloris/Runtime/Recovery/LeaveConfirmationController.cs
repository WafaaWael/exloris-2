using System;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;

namespace Exloris.Runtime.Recovery
{
    public interface ILeaveConfirmationView
    {
        void ShowAndBlockCaller(RouteId caller);
        void HideAndRestoreCaller(RouteId caller);
        void HoldBlockedForTransition(RouteId caller);
    }

    public sealed class LeaveConfirmationController : ILeaveConfirmationSink, INestedLeaveConfirmationSink
    {
        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly ILeaveConfirmationView _view;
        private RouteId _caller;
        private bool _nested;
        private LeaveRequest _nestedRequest;
        private Guid _transitionToken;
        private INestedLeaveReturnSink _nestedReturn;

        public LeaveConfirmationController(
            SessionState session, NavigationAuthority navigation, ILeaveConfirmationView view)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public LeaveConfirmationState State { get; private set; }
        public bool IsVisible => State != LeaveConfirmationState.Hidden;
        public RouteId? Caller => State != LeaveConfirmationState.Hidden ? _caller : (RouteId?)null;

        public void BindNestedReturnSink(INestedLeaveReturnSink sink)
        {
            _nestedReturn = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool TryShow(RouteId caller)
        {
            if (State != LeaveConfirmationState.Hidden || !IsApprovedCaller(caller) || !_session.IsOpen
                || _session.CurrentOpenSessionLocation != caller
                || _navigation.CurrentRoute != caller
                || _navigation.IsLocked || _navigation.Recovery != null)
                return false;

            _caller = caller;
            _nested = false;
            State = LeaveConfirmationState.Visible;
            _view.ShowAndBlockCaller(caller);
            return true;
        }

        public bool TryShow(LeaveRequest request)
        {
            if (State != LeaveConfirmationState.Hidden
                || !RouteGraph.CanPresentLeaveConfirmation(request.Caller)
                || request.ReturnLayer != LeaveReturnLayer.PauseOverlay
                || request.StackToken == Guid.Empty
                || _nestedReturn == null
                || !CanShow(request.Caller)
                || !(_view is INestedLeaveConfirmationView nestedView))
                return false;
            _caller = request.Caller;
            _nested = true;
            _nestedRequest = request;
            State = LeaveConfirmationState.Visible;
            nestedView.ShowAndBlockPause(request);
            return true;
        }

        public bool HandleStay() => RestoreCaller();
        public bool HandleCancel() => RestoreCaller();

        public bool HandleLeave(double nowSeconds)
        {
            if (State != LeaveConfirmationState.Visible) return false;
            var result = _navigation.RequestHome(nowSeconds);
            if (!result.Accepted) return false;
            State = LeaveConfirmationState.Transitioning;
            _transitionToken = result.Transaction.Id;
            if (_nested)
            {
                if (_nestedReturn != null && !_nestedReturn.HoldForLeaveTransition(_nestedRequest))
                    throw new InvalidOperationException("Nested Pause layer rejected the Leave transition.");
                ((INestedLeaveConfirmationView)_view).HoldPauseBlockedForTransition(_nestedRequest);
            }
            else
                _view.HoldBlockedForTransition(_caller);
            return true;
        }

        public bool CompleteTransition(Guid transactionId)
        {
            if (State != LeaveConfirmationState.Transitioning
                || transactionId == Guid.Empty || transactionId != _transitionToken)
                return false;
            State = LeaveConfirmationState.Hidden;
            _nested = false;
            _transitionToken = Guid.Empty;
            return true;
        }

        private bool RestoreCaller()
        {
            if (State != LeaveConfirmationState.Visible) return false;
            if (_nested)
            {
                if (_nestedReturn != null && !_nestedReturn.RestoreFromLeave(_nestedRequest))
                    return false;
                ((INestedLeaveConfirmationView)_view).HideAndRestorePause(_nestedRequest);
            }
            else
                _view.HideAndRestoreCaller(_caller);
            State = LeaveConfirmationState.Hidden;
            _nested = false;
            return true;
        }

        private bool CanShow(RouteId caller)
        {
            return IsApprovedCaller(caller) && _session.IsOpen
                && _session.CurrentOpenSessionLocation == caller
                && _navigation.CurrentRoute == caller
                && !_navigation.IsLocked && _navigation.Recovery == null;
        }

        private static bool IsApprovedCaller(RouteId route) => RouteGraph.CanPresentLeaveConfirmation(route);
    }
}
