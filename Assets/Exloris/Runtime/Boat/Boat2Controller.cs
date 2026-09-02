using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;

namespace Exloris.Runtime.Boat
{
    public sealed class Boat2Controller
    {
        public const double TravelSeconds = 6d;
        public const double ArrivalFeedbackSeconds = 0.7d;

        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IBoat2View _view;
        private readonly IBoat2PauseSink _pauseSink;
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
        private Boat2Choice _choice;
        private Boat2Snapshot _helpSnapshot;
        private Boat2Snapshot _pauseSnapshot;
        private Boat2Snapshot _leaveSnapshot;

        public Boat2Controller(SessionState session, NavigationAuthority navigation,
            IBoat2View view, IBoat2PauseSink pauseSink,
            ILeaveConfirmationSink leaveSink, Func<Guid> tokenSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _pauseSink = pauseSink ?? throw new ArgumentNullException(nameof(pauseSink));
            _leaveSink = leaveSink ?? throw new ArgumentNullException(nameof(leaveSink));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public Boat2State State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public Boat2Snapshot Snapshot => new Boat2Snapshot(CurrentTokenId, State, _choice,
            BoatDirection.Left, _stateStartedAtSeconds, _travelElapsedSeconds,
            _focusedControlId, _arrivalCommitted);

        public bool Enter(double nowSeconds, bool reducedMotion)
        {
            if (State != Boat2State.Hidden || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            CurrentTokenId = NextToken();
            State = Boat2State.Choice;
            _choice = Boat2Choice.None;
            _stateStartedAtSeconds = nowSeconds;
            _travelElapsedSeconds = 0d;
            _focusedControlId = null;
            _arrivalCommitted = false;
            _reducedMotion = reducedMotion;
            _reanchorOnNextAdvance = false;
            Present(true);
            return true;
        }

        public bool HandleChoice(Guid tokenId, Boat2Choice choice, double nowSeconds)
        {
            if (choice != Boat2Choice.FieldBank && choice != Boat2Choice.Dock) return false;
            return StartTravel(tokenId, choice, nowSeconds);
        }

        public bool HandleFollowIbis(Guid tokenId, double nowSeconds) =>
            StartTravel(tokenId, Boat2Choice.FollowIbis, nowSeconds);

        public bool HandleHelp()
        {
            if (!IsChildActionable() || !CanPresent()) return false;
            _helpSnapshot = Snapshot;
            State = Boat2State.Help;
            Present(false);
            return true;
        }

        public bool RestoreFromHelp(Boat2Snapshot snapshot)
        {
            if (State != Boat2State.Help || !snapshot.Equals(_helpSnapshot)
                || !IsRestorable(snapshot.State) || !CanPresent()) return false;
            RestoreSnapshot(snapshot);
            return true;
        }

        public bool HandleRepeat(double nowSeconds)
        {
            if (!IsChildActionable() || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            if (nowSeconds < _stateStartedAtSeconds) return false;
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
            State = Boat2State.Paused;
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
            if (!_leaveSink.TryShow(RouteId.Boat2)) return false;
            _leaveSnapshot = captured;
            _leaveCapturedAtSeconds = nowSeconds;
            State = Boat2State.Leaving;
            Present(false);
            return true;
        }

        public bool HandleSkip(Guid tokenId, double nowSeconds) => tokenId == CurrentTokenId
            && IsChildActionable() && CanPresent() && BeginArrival(nowSeconds);

        public bool Advance(Guid tokenId, double nowSeconds)
        {
            if (tokenId != CurrentTokenId || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            if (State == Boat2State.Arriving)
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
            if (State == Boat2State.Choice)
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
            if (_travelElapsedSeconds >= TravelSeconds) return BeginArrival(nowSeconds);
            Present(true);
            return true;
        }

        public bool Restore(Boat2Snapshot snapshot) => Restore(snapshot, _pauseCapturedAtSeconds);

        public bool Restore(Boat2Snapshot snapshot, double nowSeconds)
        {
            if (State != Boat2State.Paused || !snapshot.Equals(_pauseSnapshot)
                || !IsRestorable(snapshot.State) || !CanPresent()) return false;
            if (!CanRestoreAt(nowSeconds, _pauseCapturedAtSeconds)) return false;
            RestoreSnapshot(snapshot, _pauseCapturedAtSeconds, nowSeconds);
            return true;
        }

        public bool RestoreFromLeave(Boat2Snapshot snapshot) => RestoreFromLeave(snapshot, _leaveCapturedAtSeconds);

        public bool RestoreFromLeave(Boat2Snapshot snapshot, double nowSeconds)
        {
            if (State != Boat2State.Leaving || !snapshot.Equals(_leaveSnapshot)
                || !IsRestorable(snapshot.State) || !CanPresent()) return false;
            if (!CanRestoreAt(nowSeconds, _leaveCapturedAtSeconds)) return false;
            RestoreSnapshot(snapshot, _leaveCapturedAtSeconds, nowSeconds);
            return true;
        }

        public bool EnterRecovery()
        {
            if (State != Boat2State.Navigating || _navigation.Recovery == null) return false;
            State = Boat2State.Recovery;
            Present(false);
            return true;
        }

        public bool RetryBoundary()
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != Boat2State.Recovery || transaction == null
                || transaction.Caller != RouteId.Boat2 || transaction.Destination != RouteId.Stage2)
                return false;
            State = Boat2State.Navigating;
            Present(false);
            return true;
        }

        private bool StartTravel(Guid tokenId, Boat2Choice choice, double nowSeconds)
        {
            if (State != Boat2State.Choice || tokenId != CurrentTokenId || !CanPresent()) return false;
            ValidateClock(nowSeconds);
            if (nowSeconds < _stateStartedAtSeconds) return false;
            _choice = choice;
            State = Boat2State.Traveling;
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
            if (nowSeconds < _stateStartedAtSeconds) return false;
            _travelElapsedSeconds = TravelSeconds;
            _stateStartedAtSeconds = nowSeconds;
            State = Boat2State.Arriving;
            Present(true);
            return true;
        }

        private bool CommitArrival(double nowSeconds)
        {
            if (_arrivalCommitted || State != Boat2State.Arriving) return false;
            ValidateClock(nowSeconds);
            var request = _navigation.Request(RouteId.Stage2, nowSeconds);
            if (!request.Accepted) return false;
            _arrivalCommitted = true;
            _travelElapsedSeconds = TravelSeconds;
            State = Boat2State.Navigating;
            Present(false);
            return true;
        }

        private void RestoreSnapshot(Boat2Snapshot snapshot) => RestoreSnapshot(snapshot, 0d, 0d);

        private void RestoreSnapshot(Boat2Snapshot snapshot, double suspendedAtSeconds, double nowSeconds)
        {
            State = snapshot.State;
            _choice = snapshot.Choice;
            _stateStartedAtSeconds = snapshot.StateStartedAtSeconds;
            if (State == Boat2State.Arriving) _stateStartedAtSeconds += nowSeconds - suspendedAtSeconds;
            _travelElapsedSeconds = snapshot.TravelElapsedSeconds;
            _focusedControlId = snapshot.FocusedControlId;
            _arrivalCommitted = snapshot.ArrivalCommitted;
            _reanchorOnNextAdvance = State == Boat2State.Traveling;
            Present(State != Boat2State.Help && State != Boat2State.Leaving);
        }

        private bool CanPresent() => _session.IsOpen
            && _session.CurrentOpenSessionLocation == RouteId.Boat2
            && _navigation.CurrentRoute == RouteId.Boat2
            && !_navigation.IsLocked && _navigation.Recovery == null;

        private bool IsChildActionable() => State == Boat2State.Choice || State == Boat2State.Traveling;
        private bool CanPause() => IsChildActionable() || State == Boat2State.Arriving || State == Boat2State.Help || State == Boat2State.Leaving;
        private bool CanLeave() => IsChildActionable() || State == Boat2State.Arriving;
        private static bool IsRestorable(Boat2State state) => state == Boat2State.Choice
            || state == Boat2State.Traveling || state == Boat2State.Arriving
            || state == Boat2State.Help || state == Boat2State.Leaving;
        private double CurrentSnapshotTime() => State == Boat2State.Traveling
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
                throw new InvalidOperationException("Boat 2 tokens must be non-empty and unique.");
            return token;
        }

        private static void ValidateClock(double nowSeconds)
        {
            if (double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds))
                throw new ArgumentOutOfRangeException(nameof(nowSeconds));
        }
    }
}
