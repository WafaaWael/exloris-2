using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Avatar;

namespace Exloris.Runtime.Completion
{
    public sealed class CompletionController
    {
        public const double MotionSeconds = 2.4d;
        public const double SemanticSeconds = 3.646d;
        private const double ClockEpsilon = 0.000000001d;

        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly ICompletionView _view;
        private readonly IAdultGateRequestSink _adultGateSink;
        private readonly ICompletionPauseSink _pauseSink;
        private readonly ILeaveConfirmationSink _leaveSink;
        private readonly Func<Guid> _tokenSource;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();

        private double _stateStartedAtSeconds;
        private double _elapsedSeconds;
        private double _pauseCapturedAtSeconds;
        private double _leaveCapturedAtSeconds;
        private string _focusedControlId;
        private bool _reducedMotion;
        private CompletionSnapshot _pauseSnapshot;
        private CompletionSnapshot _leaveSnapshot;

        public CompletionController(
            SessionState session,
            NavigationAuthority navigation,
            ICompletionView view,
            IAdultGateRequestSink adultGateSink,
            ICompletionPauseSink pauseSink,
            ILeaveConfirmationSink leaveSink,
            Func<Guid> tokenSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _adultGateSink = adultGateSink ?? throw new ArgumentNullException(nameof(adultGateSink));
            _pauseSink = pauseSink ?? throw new ArgumentNullException(nameof(pauseSink));
            _leaveSink = leaveSink ?? throw new ArgumentNullException(nameof(leaveSink));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public CompletionState State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public Guid CurrentPresentationTokenId { get; private set; }
        public CompletionSnapshot Snapshot => new CompletionSnapshot(
            CurrentTokenId,
            State,
            CurrentPresentationTokenId,
            _stateStartedAtSeconds,
            _elapsedSeconds,
            _focusedControlId,
            _reducedMotion);

        public bool Enter(Guid transactionId, double nowSeconds, bool reducedMotion)
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != CompletionState.Hidden
                || transaction == null
                || transaction.Id != transactionId
                || transaction.Caller != RouteId.Stage3
                || transaction.Destination != RouteId.Completion
                || _navigation.CurrentRoute != RouteId.Stage3
                || !_session.IsOpen
                || _session.CurrentOpenSessionLocation != RouteId.Stage3
                || _navigation.Recovery != null
                || !IsValidClock(nowSeconds))
                return false;

            var routeToken = NextToken();
            var presentationToken = NextToken();
            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed) return false;
            if (!_session.MoveTo(RouteId.Completion))
                throw new InvalidOperationException("A prevalidated Completion session failed to advance.");

            CurrentTokenId = routeToken;
            CurrentPresentationTokenId = presentationToken;
            _stateStartedAtSeconds = nowSeconds;
            _elapsedSeconds = 0d;
            _focusedControlId = null;
            _reducedMotion = reducedMotion;
            State = CompletionState.Presenting;
            Present(false);
            return true;
        }

        public bool Advance(Guid tokenId, Guid presentationTokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || presentationTokenId == Guid.Empty
                || presentationTokenId != CurrentPresentationTokenId
                || State != CompletionState.Presenting
                || !CanActAt(nowSeconds))
                return false;

            var nextElapsed = Math.Min(SemanticSeconds, nowSeconds - _stateStartedAtSeconds);
            if (nextElapsed < _elapsedSeconds - ClockEpsilon) return false;
            _elapsedSeconds = nextElapsed;
            if (_elapsedSeconds >= SemanticSeconds - ClockEpsilon)
            {
                _elapsedSeconds = SemanticSeconds;
                State = CompletionState.Ready;
            }
            Present(State == CompletionState.Ready);
            return true;
        }

        public bool HandleAdult(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != CompletionState.Ready || !CanActAt(nowSeconds)) return false;
            var request = new AdultGateRequest(
                AdultGatePurpose.Dashboard,
                AdultGateRequestOrigin.Completion,
                RouteId.Completion,
                CurrentTokenId);
            if (!_adultGateSink.TryRequest(request)) return false;
            State = CompletionState.NavigatingAdult;
            _view.SetInputReady(false);
            return true;
        }

        public bool HandlePause(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || (State != CompletionState.Presenting && State != CompletionState.Ready)
                || !CanActAt(nowSeconds))
                return false;
            var captured = Snapshot;
            if (!_pauseSink.TryShow(captured)) return false;
            _pauseSnapshot = captured;
            _pauseCapturedAtSeconds = nowSeconds;
            State = CompletionState.Paused;
            Present(false);
            return true;
        }

        public bool RestoreFromAdultGate(Guid tokenId)
        {
            if (!IsCurrent(tokenId) || State != CompletionState.NavigatingAdult || !CanPresent()) return false;
            State = CompletionState.Ready;
            Present(true);
            return true;
        }

        public bool RestoreFromPause(CompletionSnapshot snapshot, double nowSeconds)
        {
            if (State != CompletionState.Paused
                || !snapshot.Equals(_pauseSnapshot)
                || !IsRestorable(snapshot.State)
                || !IsValidClock(nowSeconds)
                || nowSeconds < _pauseCapturedAtSeconds
                || !CanPresent())
                return false;
            Restore(snapshot, nowSeconds - _pauseCapturedAtSeconds);
            return true;
        }

        public bool HandleHome(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != CompletionState.Ready || !CanActAt(nowSeconds)) return false;
            var captured = Snapshot;
            if (!_leaveSink.TryShow(RouteId.Completion)) return false;
            _leaveSnapshot = captured;
            _leaveCapturedAtSeconds = nowSeconds;
            State = CompletionState.Leaving;
            Present(false);
            return true;
        }

        public bool RestoreFromLeave(CompletionSnapshot snapshot, double nowSeconds)
        {
            if (State != CompletionState.Leaving
                || !snapshot.Equals(_leaveSnapshot)
                || snapshot.State != CompletionState.Ready
                || !IsValidClock(nowSeconds)
                || nowSeconds < _leaveCapturedAtSeconds
                || !CanPresent())
                return false;
            Restore(snapshot, nowSeconds - _leaveCapturedAtSeconds);
            return true;
        }

        public bool TrySetFocus(Guid tokenId, string controlId)
        {
            if (!IsCurrent(tokenId)
                || (State != CompletionState.Presenting && State != CompletionState.Ready)
                || string.IsNullOrWhiteSpace(controlId)
                || controlId.Length > 64
                || !CanPresent())
                return false;
            _focusedControlId = controlId;
            Present(State == CompletionState.Ready);
            return true;
        }

        private void Restore(CompletionSnapshot snapshot, double pausedDuration)
        {
            State = snapshot.State;
            CurrentPresentationTokenId = snapshot.PresentationTokenId;
            _stateStartedAtSeconds = snapshot.StateStartedAtSeconds + pausedDuration;
            _elapsedSeconds = snapshot.ElapsedSeconds;
            _focusedControlId = snapshot.FocusedControlId;
            _reducedMotion = snapshot.ReducedMotion;
            Present(State == CompletionState.Ready);
        }

        private bool IsCurrent(Guid tokenId) => tokenId != Guid.Empty && tokenId == CurrentTokenId;
        private bool IsRestorable(CompletionState state) => state == CompletionState.Presenting || state == CompletionState.Ready;

        private bool CanActAt(double nowSeconds) => CanPresent()
            && IsValidClock(nowSeconds)
            && nowSeconds >= _stateStartedAtSeconds - ClockEpsilon;

        private bool CanPresent() => _session.IsOpen
            && _session.CurrentOpenSessionLocation == RouteId.Completion
            && _navigation.CurrentRoute == RouteId.Completion
            && !_navigation.IsLocked
            && _navigation.Recovery == null;

        private void Present(bool inputReady)
        {
            _view.Present(Snapshot);
            _view.SetInputReady(inputReady);
        }

        private Guid NextToken()
        {
            var token = _tokenSource();
            if (token == Guid.Empty || !_issuedTokens.Add(token))
                throw new InvalidOperationException("Completion token source produced an empty or duplicate token.");
            return token;
        }

        private static bool IsValidClock(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }
}
