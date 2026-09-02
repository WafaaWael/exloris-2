using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;

namespace Exloris.Runtime.Boat
{
    public sealed class Boat1Controller
    {
        public const double TravelSeconds = 6d;
        public const double ArrivalFeedbackSeconds = 0.7d;

        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IBoat1View _view;
        private readonly IBoat1PauseSink _pauseSink;
        private readonly ILeaveConfirmationSink _leaveSink;
        private readonly Func<Guid> _tokenSource;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();

        private double _stateStartedAtSeconds;
        private double _travelElapsedSeconds;
        private double _pauseCapturedAtSeconds;
        private double _leaveCapturedAtSeconds;
        private string _focusedControlId;
        private bool _arrivalCommitted;
        private bool _reducedMotion;
        private bool _reanchorOnNextAdvance;
        private Boat1Choice _choice;
        private Boat1Snapshot _helpSnapshot;
        private Boat1Snapshot _pauseSnapshot;
        private Boat1Snapshot _leaveSnapshot;

        public Boat1Controller(
            SessionState session,
            NavigationAuthority navigation,
            IBoat1View view,
            IBoat1PauseSink pauseSink,
            ILeaveConfirmationSink leaveSink,
            Func<Guid> tokenSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _pauseSink = pauseSink ?? throw new ArgumentNullException(nameof(pauseSink));
            _leaveSink = leaveSink ?? throw new ArgumentNullException(nameof(leaveSink));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public Boat1State State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public Boat1Snapshot Snapshot => new Boat1Snapshot(
            CurrentTokenId,
            State,
            _choice,
            BoatDirection.Right,
            _stateStartedAtSeconds,
            _travelElapsedSeconds,
            _focusedControlId,
            _arrivalCommitted);

        public bool Enter(double nowSeconds, bool reducedMotion)
        {
            if (State != Boat1State.Hidden || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            CurrentTokenId = NextToken();
            State = Boat1State.Choice;
            _choice = Boat1Choice.None;
            _stateStartedAtSeconds = nowSeconds;
            _travelElapsedSeconds = 0d;
            _focusedControlId = null;
            _arrivalCommitted = false;
            _reducedMotion = reducedMotion;
            _reanchorOnNextAdvance = false;
            Present(true);
            return true;
        }

        public bool HandleChoice(
            Guid tokenId,
            Boat1Choice choice,
            double nowSeconds)
        {
            if (choice != Boat1Choice.Reeds && choice != Boat1Choice.Village) return false;
            return StartTravel(tokenId, choice, nowSeconds);
        }

        public bool HandleFollowIbis(Guid tokenId, double nowSeconds)
        {
            return StartTravel(tokenId, Boat1Choice.FollowIbis, nowSeconds);
        }

        public bool HandleHelp()
        {
            if (!IsChildActionable() || !CanPresent()) return false;
            _helpSnapshot = Snapshot;
            State = Boat1State.Help;
            Present(false);
            return true;
        }

        public bool RestoreFromHelp(Boat1Snapshot snapshot)
        {
            if (State != Boat1State.Help
                || !snapshot.Equals(_helpSnapshot)
                || !IsRestorable(snapshot.State)
                || !CanPresent())
                return false;
            RestoreSnapshot(snapshot);
            return true;
        }

        public bool HandleRepeat(double nowSeconds)
        {
            if (!IsChildActionable() || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            Present(true);
            return true;
        }

        public bool HandlePause() => HandlePause(CurrentSnapshotTime());

        public bool HandlePause(double nowSeconds)
        {
            if (!CanHandlePause(nowSeconds)) return false;
            var captured = Snapshot;
            if (!_pauseSink.TryShow(captured)) return false;
            _pauseSnapshot = captured;
            _pauseCapturedAtSeconds = nowSeconds;
            State = Boat1State.Paused;
            Present(false);
            return true;
        }

        public bool CanHandlePause(double nowSeconds)
        {
            if (!CanPause() || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            return nowSeconds >= _stateStartedAtSeconds;
        }

        public bool HandleHome() => HandleHome(CurrentSnapshotTime());

        public bool HandleHome(double nowSeconds)
        {
            if (!CanLeave() || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            if (nowSeconds < _stateStartedAtSeconds) return false;
            var captured = Snapshot;
            if (!_leaveSink.TryShow(RouteId.Boat1)) return false;
            _leaveSnapshot = captured;
            _leaveCapturedAtSeconds = nowSeconds;
            State = Boat1State.Leaving;
            Present(false);
            return true;
        }

        public bool HandleSkip(Guid tokenId, double nowSeconds)
        {
            return tokenId == CurrentTokenId
                && IsChildActionable()
                && CanPresent()
                && BeginArrival(nowSeconds);
        }

        public bool Advance(Guid tokenId, double nowSeconds)
        {
            if (tokenId != CurrentTokenId || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            if (State == Boat1State.Arriving)
            {
                if (nowSeconds < _stateStartedAtSeconds) return false;
                if (nowSeconds - _stateStartedAtSeconds < ArrivalFeedbackSeconds - 0.000000001d)
                {
                    Present(true);
                    return true;
                }
                return CommitArrival(nowSeconds);
            }
            if (!IsChildActionable()) return false;
            if (State == Boat1State.Choice)
            {
                Present(true);
                return true;
            }

            if (_reanchorOnNextAdvance)
            {
                _stateStartedAtSeconds = nowSeconds - _travelElapsedSeconds;
                _reanchorOnNextAdvance = false;
            }
            if (nowSeconds < _stateStartedAtSeconds) return false;
            _travelElapsedSeconds = nowSeconds - _stateStartedAtSeconds;
            if (_travelElapsedSeconds >= TravelSeconds)
                return BeginArrival(nowSeconds);
            Present(true);
            return true;
        }

        public bool Restore(Boat1Snapshot snapshot) => Restore(snapshot, _pauseCapturedAtSeconds);

        public bool Restore(Boat1Snapshot snapshot, double nowSeconds)
        {
            if (State != Boat1State.Paused
                || !snapshot.Equals(_pauseSnapshot)
                || !IsRestorable(snapshot.State)
                || !CanPresent())
                return false;
            if (!CanRestoreAt(nowSeconds, _pauseCapturedAtSeconds)) return false;
            RestoreSnapshot(snapshot, _pauseCapturedAtSeconds, nowSeconds);
            return true;
        }

        public bool RestoreFromLeave(Boat1Snapshot snapshot) =>
            RestoreFromLeave(snapshot, _leaveCapturedAtSeconds);

        public bool RestoreFromLeave(Boat1Snapshot snapshot, double nowSeconds)
        {
            if (State != Boat1State.Leaving
                || !snapshot.Equals(_leaveSnapshot)
                || !IsRestorable(snapshot.State)
                || !CanPresent())
                return false;
            if (!CanRestoreAt(nowSeconds, _leaveCapturedAtSeconds)) return false;
            RestoreSnapshot(snapshot, _leaveCapturedAtSeconds, nowSeconds);
            return true;
        }

        public bool EnterRecovery()
        {
            if (State != Boat1State.Navigating || _navigation.Recovery == null) return false;
            State = Boat1State.Recovery;
            Present(false);
            return true;
        }

        public bool RetryBoundary()
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != Boat1State.Recovery
                || transaction == null
                || transaction.Caller != RouteId.Boat1
                || transaction.Destination != RouteId.Stage1)
                return false;
            State = Boat1State.Navigating;
            Present(false);
            return true;
        }

        private bool StartTravel(Guid tokenId, Boat1Choice choice, double nowSeconds)
        {
            if (State != Boat1State.Choice
                || tokenId != CurrentTokenId
                || !CanPresent())
                return false;
            ValidateClock(nowSeconds);
            if (nowSeconds < _stateStartedAtSeconds) return false;
            _choice = choice;
            State = Boat1State.Traveling;
            _stateStartedAtSeconds = nowSeconds;
            _travelElapsedSeconds = 0d;
            _reanchorOnNextAdvance = false;
            Present(true);
            return true;
        }

        private bool BeginArrival(double nowSeconds)
        {
            if (_arrivalCommitted || !IsChildActionable()) return false;
            ValidateClock(nowSeconds);
            _travelElapsedSeconds = TravelSeconds;
            _stateStartedAtSeconds = nowSeconds;
            State = Boat1State.Arriving;
            Present(true);
            return true;
        }

        private bool CommitArrival(double nowSeconds)
        {
            if (_arrivalCommitted || State != Boat1State.Arriving) return false;
            ValidateClock(nowSeconds);
            var request = _navigation.Request(RouteId.Stage1, nowSeconds);
            if (!request.Accepted) return false;
            _arrivalCommitted = true;
            _travelElapsedSeconds = TravelSeconds;
            State = Boat1State.Navigating;
            Present(false);
            return true;
        }

        private void RestoreSnapshot(Boat1Snapshot snapshot)
        {
            RestoreSnapshot(snapshot, 0d, 0d);
        }

        private void RestoreSnapshot(
            Boat1Snapshot snapshot,
            double suspendedAtSeconds,
            double nowSeconds)
        {
            State = snapshot.State;
            _choice = snapshot.Choice;
            _stateStartedAtSeconds = snapshot.StateStartedAtSeconds;
            if (State == Boat1State.Arriving)
                _stateStartedAtSeconds += nowSeconds - suspendedAtSeconds;
            _travelElapsedSeconds = snapshot.TravelElapsedSeconds;
            _focusedControlId = snapshot.FocusedControlId;
            _arrivalCommitted = snapshot.ArrivalCommitted;
            _reanchorOnNextAdvance = State == Boat1State.Traveling;
            Present(State != Boat1State.Help && State != Boat1State.Leaving);
        }

        private bool CanPresent()
        {
            return _session.IsOpen
                && _session.CurrentOpenSessionLocation == RouteId.Boat1
                && _navigation.CurrentRoute == RouteId.Boat1
                && !_navigation.IsLocked
                && _navigation.Recovery == null;
        }

        private bool IsChildActionable()
        {
            return State == Boat1State.Choice || State == Boat1State.Traveling;
        }

        private bool CanPause()
        {
            return IsChildActionable() || State == Boat1State.Arriving
                || State == Boat1State.Help || State == Boat1State.Leaving;
        }

        private bool CanLeave()
        {
            return IsChildActionable() || State == Boat1State.Arriving;
        }

        private static bool IsRestorable(Boat1State state)
        {
            return state == Boat1State.Choice || state == Boat1State.Traveling
                || state == Boat1State.Arriving || state == Boat1State.Help
                || state == Boat1State.Leaving;
        }

        private double CurrentSnapshotTime() => State == Boat1State.Traveling
            ? _stateStartedAtSeconds + _travelElapsedSeconds : _stateStartedAtSeconds;

        private static bool CanRestoreAt(double nowSeconds, double capturedAtSeconds)
        {
            ValidateClock(nowSeconds);
            return nowSeconds >= capturedAtSeconds;
        }

        private void Present(bool inputReady)
        {
            _view.Present(Snapshot, _reducedMotion);
            _view.SetInputReady(inputReady);
        }

        private Guid NextToken()
        {
            var token = _tokenSource();
            if (token == Guid.Empty || !_issuedTokens.Add(token))
                throw new InvalidOperationException("Boat 1 tokens must be non-empty and unique.");
            return token;
        }

        private static void ValidateClock(double nowSeconds)
        {
            if (double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds))
                throw new ArgumentOutOfRangeException(nameof(nowSeconds));
        }
    }
}
