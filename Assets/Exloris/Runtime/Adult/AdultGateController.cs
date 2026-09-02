using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;

namespace Exloris.Runtime.Adult
{
    public sealed class AdultGateController
    {
        public const double HoldSeconds = 3d;
        private const int NoPointer = -1;

        private readonly NavigationAuthority _navigation;
        private readonly IAdultGateView _view;
        private readonly IAdultGateReturnSink _returnSink;
        private readonly IConfirmedSessionResetSink _resetSink;
        private readonly Func<Guid> _tokenSource;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();

        private AdultGateRequest _request;
        private double _holdProgress;
        private double _holdStartedAtSeconds;
        private double _lastHoldClock;
        private int _activePointerId = NoPointer;

        public AdultGateController(
            NavigationAuthority navigation,
            IAdultGateView view,
            IAdultGateReturnSink returnSink,
            IConfirmedSessionResetSink resetSink,
            Func<Guid> tokenSource)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _returnSink = returnSink ?? throw new ArgumentNullException(nameof(returnSink));
            _resetSink = resetSink ?? throw new ArgumentNullException(nameof(resetSink));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public AdultGateState State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public AdultGateSnapshot Snapshot => new AdultGateSnapshot(
            CurrentTokenId,
            State,
            _request,
            _holdProgress,
            _activePointerId,
            _holdStartedAtSeconds);

        public bool Enter(AdultGateRequest request, Guid transactionId, double nowSeconds)
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != AdultGateState.Hidden
                || !IsValidRequest(request)
                || transaction == null
                || transaction.Id != transactionId
                || transaction.Caller != ExpectedCaller(request.Origin)
                || transaction.Destination != RouteId.AdultGate
                || _navigation.CurrentRoute != transaction.Caller
                || _navigation.Recovery != null
                || !IsValidClock(nowSeconds))
                return false;

            var token = NextToken();
            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed) return false;
            CurrentTokenId = token;
            _request = request;
            State = AdultGateState.Holding;
            ResetProgress();
            Present(true);
            return true;
        }

        public bool HandleHoldBegin(Guid tokenId, int pointerId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != AdultGateState.Holding
                || _activePointerId != NoPointer
                || pointerId < 0
                || !CanAct()
                || !IsValidClock(nowSeconds))
                return false;
            _activePointerId = pointerId;
            _holdStartedAtSeconds = nowSeconds;
            _lastHoldClock = nowSeconds;
            _holdProgress = 0d;
            Present(true);
            return true;
        }

        public bool AdvanceHold(Guid tokenId, int pointerId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != AdultGateState.Holding
                || _activePointerId != pointerId
                || !CanAct()
                || !IsValidClock(nowSeconds)
                || nowSeconds < _lastHoldClock)
                return false;
            _lastHoldClock = nowSeconds;
            _holdProgress = Math.Min(1d, (nowSeconds - _holdStartedAtSeconds) / HoldSeconds);
            if (_holdProgress >= 1d)
            {
                _holdProgress = 1d;
                _activePointerId = NoPointer;
                State = AdultGateState.AwaitingConfirm;
            }
            Present(true);
            return true;
        }

        public bool HandleHoldRelease(Guid tokenId, int pointerId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != AdultGateState.Holding
                || _activePointerId != pointerId
                || !IsValidClock(nowSeconds)
                || nowSeconds < _lastHoldClock)
                return false;
            ResetProgress();
            Present(true);
            return true;
        }

        public bool HandlePointerExit(Guid tokenId, int pointerId)
        {
            if (!IsCurrent(tokenId) || State != AdultGateState.Holding || _activePointerId != pointerId)
                return false;
            ResetProgress();
            Present(true);
            return true;
        }

        public bool HandleInterruption()
        {
            if (State != AdultGateState.Holding && State != AdultGateState.AwaitingConfirm) return false;
            State = AdultGateState.Holding;
            ResetProgress();
            Present(true);
            return true;
        }

        public bool HandleConfirm(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != AdultGateState.AwaitingConfirm
                || !CanAct()
                || !IsValidClock(nowSeconds))
                return false;
            if (_request.Purpose == AdultGatePurpose.Restart)
            {
                State = AdultGateState.ResetConfirmation;
                Present(true);
                return true;
            }

            var result = _navigation.Request(RouteId.ParentReport, nowSeconds);
            if (!result.Accepted) return false;
            State = AdultGateState.NavigatingReport;
            _view.SetInputReady(false);
            return true;
        }

        public bool HandleCancel(Guid tokenId)
        {
            if (!IsCurrent(tokenId)
                || (State != AdultGateState.Holding && State != AdultGateState.AwaitingConfirm)
                || !_returnSink.TryReturn(_request))
                return false;
            ResetProgress();
            State = AdultGateState.Returning;
            _view.SetInputReady(false);
            return true;
        }

        public bool HandleResetConfirm(Guid tokenId)
        {
            if (!IsCurrent(tokenId) || State != AdultGateState.ResetConfirmation || !_resetSink.TryReset())
                return false;
            State = AdultGateState.Resetting;
            _view.SetInputReady(false);
            return true;
        }

        public bool HandleResetCancel(Guid tokenId)
        {
            if (!IsCurrent(tokenId)
                || State != AdultGateState.ResetConfirmation
                || !_returnSink.TryReturn(_request))
                return false;
            State = AdultGateState.Returning;
            _view.SetInputReady(false);
            return true;
        }

        private bool CanAct() => _navigation.CurrentRoute == RouteId.AdultGate
            && !_navigation.IsLocked
            && _navigation.Recovery == null;

        private bool IsCurrent(Guid tokenId) => tokenId != Guid.Empty && tokenId == CurrentTokenId;

        private bool IsValidRequest(AdultGateRequest request)
        {
            if (request.ReturnRoute != ExpectedCaller(request.Origin) || !HasExactReturnState(request)) return false;
            return request.Purpose == AdultGatePurpose.Dashboard
                ? RouteGraph.CanRequestAdultDashboard(request.Origin)
                : request.Purpose == AdultGatePurpose.Restart
                    && RouteGraph.CanRequestAdultRestart(request.Origin);
        }

        private static bool HasExactReturnState(AdultGateRequest request)
        {
            switch (request.Origin)
            {
                case AdultGateRequestOrigin.Welcome:
                    return request.ReturnStateToken == Guid.Empty
                        && string.Equals(request.ReturnPresentationId, "welcome", StringComparison.Ordinal);
                case AdultGateRequestOrigin.SessionChoice:
                    return request.ReturnStateToken == Guid.Empty
                        && string.Equals(request.ReturnPresentationId, "session_choice", StringComparison.Ordinal);
                case AdultGateRequestOrigin.Completion:
                case AdultGateRequestOrigin.ParentReport:
                    return request.ReturnStateToken != Guid.Empty
                        && string.IsNullOrEmpty(request.ReturnPresentationId);
                default:
                    return false;
            }
        }

        private static RouteId ExpectedCaller(AdultGateRequestOrigin origin)
        {
            switch (origin)
            {
                case AdultGateRequestOrigin.Welcome:
                case AdultGateRequestOrigin.SessionChoice:
                    return RouteId.Welcome;
                case AdultGateRequestOrigin.Completion:
                    return RouteId.Completion;
                case AdultGateRequestOrigin.ParentReport:
                    return RouteId.ParentReport;
                default:
                    return RouteId.StartupLoading;
            }
        }

        private void ResetProgress()
        {
            _holdProgress = 0d;
            _holdStartedAtSeconds = 0d;
            _lastHoldClock = 0d;
            _activePointerId = NoPointer;
        }

        private void Present(bool ready)
        {
            _view.Present(Snapshot);
            _view.SetInputReady(ready);
        }

        private Guid NextToken()
        {
            var token = _tokenSource();
            if (token == Guid.Empty || !_issuedTokens.Add(token))
                throw new InvalidOperationException("Adult Gate token source produced an empty or duplicate token.");
            return token;
        }

        private static bool IsValidClock(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }
}
