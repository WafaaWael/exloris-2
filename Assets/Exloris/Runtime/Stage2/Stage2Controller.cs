using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;

namespace Exloris.Runtime.Stage2
{
    public sealed class Stage2Controller
    {
        public const double ConsequenceSeconds = 2.4d;
        private const double ClockEpsilon = 0.000000001d;

        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IStage2View _view;
        private readonly IStage2PauseSink _pauseSink;
        private readonly ILeaveConfirmationSink _leaveSink;
        private readonly Func<Guid> _tokenSource;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();
        private readonly HashSet<Guid> _consumedInteractions = new HashSet<Guid>();

        private bool _reducedMotion;
        private Stage2River _selectedRiver;
        private Stage2ObservedHotspots _lowObserved;
        private Stage2ObservedHotspots _middleObserved;
        private Stage2ObservedHotspots _highObserved;
        private Stage2Hotspot _currentHotspot;
        private double _stateStartedAtSeconds;
        private double _consequenceElapsedSeconds;
        private string _focusedControlId;
        private int _optionCount;
        private bool _completionRecorded;
        private double _pauseCapturedAtSeconds;
        private double _leaveCapturedAtSeconds;
        private Stage2Snapshot _helpSnapshot;
        private Stage2Snapshot _pauseSnapshot;
        private Stage2Snapshot _leaveSnapshot;

        public Stage2Controller(SessionState session, NavigationAuthority navigation, IStage2View view,
            IStage2PauseSink pauseSink, ILeaveConfirmationSink leaveSink, Func<Guid> tokenSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _pauseSink = pauseSink ?? throw new ArgumentNullException(nameof(pauseSink));
            _leaveSink = leaveSink ?? throw new ArgumentNullException(nameof(leaveSink));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public Stage2State State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public Guid CurrentConsequenceTokenId { get; private set; }
        public Guid CurrentInstructionTokenId { get; private set; }
        public Stage2Snapshot Snapshot => new Stage2Snapshot(
            CurrentTokenId,
            State,
            _selectedRiver,
            _lowObserved,
            _middleObserved,
            _highObserved,
            _currentHotspot,
            CurrentConsequenceTokenId,
            CurrentInstructionTokenId,
            _stateStartedAtSeconds,
            _consequenceElapsedSeconds,
            _focusedControlId,
            _optionCount,
            _completionRecorded);

        public bool CanFinish => State == Stage2State.Inspecting
            && _selectedRiver != Stage2River.None
            && ObservedFor(_selectedRiver) == Stage2ObservedHotspots.All;

        public Stage2Hotspot NextUnvisitedHotspot
        {
            get
            {
                var observed = ObservedFor(_selectedRiver);
                if (_selectedRiver == Stage2River.None) return Stage2Hotspot.None;
                if ((observed & Stage2ObservedHotspots.Field) == 0) return Stage2Hotspot.Field;
                if ((observed & Stage2ObservedHotspots.Path) == 0) return Stage2Hotspot.Path;
                if ((observed & Stage2ObservedHotspots.Boat) == 0) return Stage2Hotspot.Boat;
                return Stage2Hotspot.None;
            }
        }

        public bool Enter(Guid transactionId, double nowSeconds, bool reducedMotion)
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != Stage2State.Hidden
                || transaction == null
                || transaction.Id != transactionId
                || transaction.Caller != RouteId.Boat2
                || transaction.Destination != RouteId.Stage2
                || _navigation.CurrentRoute != RouteId.Boat2
                || !_session.IsOpen
                || _session.CurrentOpenSessionLocation != RouteId.Boat2
                || _navigation.Recovery != null
                || !IsValidClock(nowSeconds))
                return false;

            CurrentTokenId = NextToken();
            CurrentInstructionTokenId = NextToken();
            CurrentConsequenceTokenId = Guid.Empty;
            _selectedRiver = Stage2River.None;
            _lowObserved = Stage2ObservedHotspots.None;
            _middleObserved = Stage2ObservedHotspots.None;
            _highObserved = Stage2ObservedHotspots.None;
            _currentHotspot = Stage2Hotspot.None;
            _stateStartedAtSeconds = nowSeconds;
            _consequenceElapsedSeconds = 0d;
            _focusedControlId = null;
            _optionCount = 0;
            _completionRecorded = false;
            _reducedMotion = reducedMotion;

            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed) return false;
            if (!_session.MoveTo(RouteId.Stage2))
                throw new InvalidOperationException("A prevalidated Stage 2 session failed to advance.");
            State = Stage2State.Intro;
            Present(true);
            return true;
        }

        public bool HandlePlay(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage2State.Intro || !CanActAt(nowSeconds)) return false;
            State = Stage2State.Choosing;
            _stateStartedAtSeconds = nowSeconds;
            CurrentInstructionTokenId = NextToken();
            Present(true);
            return true;
        }

        public bool HandleRepeat(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || !CanActAt(nowSeconds)) return false;
            if (State == Stage2State.Inspecting
                && _currentHotspot != Stage2Hotspot.None
                && (ObservedFor(_selectedRiver) & Flag(_currentHotspot)) != 0)
            {
                CurrentConsequenceTokenId = NextToken();
                _consequenceElapsedSeconds = 0d;
                _stateStartedAtSeconds = nowSeconds;
                State = Stage2State.ConsequencePlaying;
                Present(true);
                return true;
            }
            if (State != Stage2State.Intro && State != Stage2State.Choosing && State != Stage2State.Inspecting)
                return false;
            CurrentInstructionTokenId = NextToken();
            Present(true);
            return true;
        }

        public bool HandleHelp(Guid tokenId)
        {
            if (!IsCurrent(tokenId)
                || (State != Stage2State.Intro && State != Stage2State.Choosing && State != Stage2State.Inspecting)
                || !CanPresent())
                return false;
            _helpSnapshot = Snapshot;
            State = Stage2State.Help;
            Present(false);
            return true;
        }

        public bool RestoreFromHelp(Stage2Snapshot snapshot)
        {
            if (State != Stage2State.Help || !snapshot.Equals(_helpSnapshot) || !IsRestorable(snapshot.State) || !CanPresent())
                return false;
            RestoreSnapshot(snapshot, 0d, 0d, false);
            return true;
        }

        public bool HandleSelectRiver(Guid tokenId, Stage2River river, Guid interactionId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage2State.Choosing
                || !IsRiver(river)
                || !CanActAt(nowSeconds)
                || !ConsumeInteraction(interactionId))
                return false;
            _selectedRiver = river;
            _currentHotspot = Stage2Hotspot.None;
            _optionCount++;
            _stateStartedAtSeconds = nowSeconds;
            CurrentInstructionTokenId = NextToken();
            State = Stage2State.Inspecting;
            Present(true);
            return true;
        }

        public bool HandleHotspot(Guid tokenId, Stage2Hotspot hotspot, Guid interactionId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage2State.Inspecting
                || _selectedRiver == Stage2River.None
                || !IsHotspot(hotspot)
                || !CanActAt(nowSeconds)
                || !ConsumeInteraction(interactionId))
                return false;
            _currentHotspot = hotspot;
            _optionCount++;
            CurrentConsequenceTokenId = NextToken();
            _consequenceElapsedSeconds = 0d;
            _stateStartedAtSeconds = nowSeconds;
            State = Stage2State.ConsequencePlaying;
            Present(true);
            return true;
        }

        public bool AdvanceConsequence(Guid tokenId, Guid consequenceTokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage2State.ConsequencePlaying
                || consequenceTokenId == Guid.Empty
                || consequenceTokenId != CurrentConsequenceTokenId
                || !CanActAt(nowSeconds))
                return false;

            var nextElapsed = Math.Min(ConsequenceSeconds, nowSeconds - _stateStartedAtSeconds);
            if (nextElapsed < _consequenceElapsedSeconds - ClockEpsilon) return false;
            _consequenceElapsedSeconds = nextElapsed;
            if (_consequenceElapsedSeconds < ConsequenceSeconds - ClockEpsilon)
            {
                Present(true);
                return true;
            }

            _consequenceElapsedSeconds = ConsequenceSeconds;
            var flag = Flag(_currentHotspot);
            var observed = ObservedFor(_selectedRiver);
            if ((observed & flag) == 0)
            {
                SetObserved(_selectedRiver, observed | flag);
                _session.Record(ObservationEvent.ChoiceOutcome(
                    ObservationEventType.Stage2Option,
                    "choice_" + RiverToken(_selectedRiver),
                    "outcome_" + HotspotToken(_currentHotspot)));
            }
            State = Stage2State.Inspecting;
            Present(true);
            return true;
        }

        public bool HandleCompare(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage2State.Inspecting || !CanActAt(nowSeconds)) return false;
            _selectedRiver = Stage2River.None;
            _currentHotspot = Stage2Hotspot.None;
            CurrentConsequenceTokenId = Guid.Empty;
            CurrentInstructionTokenId = NextToken();
            _stateStartedAtSeconds = nowSeconds;
            _consequenceElapsedSeconds = 0d;
            State = Stage2State.Choosing;
            Present(true);
            return true;
        }

        public bool HandleFinish(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || !CanFinish || _completionRecorded || !CanActAt(nowSeconds)) return false;
            var request = _navigation.Request(RouteId.Boat3, nowSeconds);
            if (!request.Accepted) return false;
            _session.Record(ObservationEvent.OptionsTried(ObservationEventType.Stage2Option, "stage_2", _optionCount));
            _session.Record(ObservationEvent.CompletedStages("stage_2"));
            _completionRecorded = true;
            State = Stage2State.Navigating;
            _view.SetInputReady(false);
            return true;
        }

        public bool HandlePause(double nowSeconds)
        {
            if (!CanPause() || !CanActAt(nowSeconds)) return false;
            var captured = Snapshot;
            if (!_pauseSink.TryShow(captured)) return false;
            _pauseSnapshot = captured;
            _pauseCapturedAtSeconds = nowSeconds;
            State = Stage2State.Paused;
            Present(false);
            return true;
        }

        public bool RestoreFromPause(Stage2Snapshot snapshot, double nowSeconds)
        {
            if (State != Stage2State.Paused
                || !snapshot.Equals(_pauseSnapshot)
                || !IsRestorable(snapshot.State)
                || !IsValidClock(nowSeconds)
                || nowSeconds < _pauseCapturedAtSeconds
                || !CanPresent())
                return false;
            RestoreSnapshot(snapshot, _pauseCapturedAtSeconds, nowSeconds, true);
            return true;
        }

        public bool HandleHome(double nowSeconds)
        {
            if (!CanLeave() || !CanActAt(nowSeconds)) return false;
            var captured = Snapshot;
            if (!_leaveSink.TryShow(RouteId.Stage2)) return false;
            _leaveSnapshot = captured;
            _leaveCapturedAtSeconds = nowSeconds;
            State = Stage2State.Leaving;
            Present(false);
            return true;
        }

        public bool RestoreFromLeave(Stage2Snapshot snapshot, double nowSeconds)
        {
            if (State != Stage2State.Leaving
                || !snapshot.Equals(_leaveSnapshot)
                || !IsRestorable(snapshot.State)
                || !IsValidClock(nowSeconds)
                || nowSeconds < _leaveCapturedAtSeconds
                || !CanPresent())
                return false;
            RestoreSnapshot(snapshot, _leaveCapturedAtSeconds, nowSeconds, true);
            return true;
        }

        public bool TrySetFocus(Guid tokenId, string controlId)
        {
            if (!IsCurrent(tokenId) || !CanPresent() || string.IsNullOrWhiteSpace(controlId) || controlId.Length > 64)
                return false;
            _focusedControlId = controlId;
            Present(State != Stage2State.Help && State != Stage2State.Paused && State != Stage2State.Leaving);
            return true;
        }

        public bool EnterRecovery()
        {
            if (State != Stage2State.Navigating || _navigation.Recovery == null) return false;
            State = Stage2State.Recovery;
            _view.SetInputReady(false);
            return true;
        }

        public bool RetryBoundary()
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != Stage2State.Recovery
                || transaction == null
                || transaction.Caller != RouteId.Stage2
                || transaction.Destination != RouteId.Boat3)
                return false;
            State = Stage2State.Navigating;
            _view.SetInputReady(false);
            return true;
        }

        public Stage2ObservedHotspots ObservedFor(Stage2River river)
        {
            switch (river)
            {
                case Stage2River.Low: return _lowObserved;
                case Stage2River.Middle: return _middleObserved;
                case Stage2River.High: return _highObserved;
                default: return Stage2ObservedHotspots.None;
            }
        }

        private void SetObserved(Stage2River river, Stage2ObservedHotspots value)
        {
            switch (river)
            {
                case Stage2River.Low: _lowObserved = value; break;
                case Stage2River.Middle: _middleObserved = value; break;
                case Stage2River.High: _highObserved = value; break;
                default: throw new InvalidOperationException("A Stage 2 observed set requires a selected river.");
            }
        }

        private bool ConsumeInteraction(Guid interactionId)
        {
            return interactionId != Guid.Empty && _consumedInteractions.Add(interactionId);
        }

        private bool IsCurrent(Guid tokenId) => tokenId != Guid.Empty && tokenId == CurrentTokenId;

        private bool CanActAt(double nowSeconds)
        {
            return CanPresent() && IsValidClock(nowSeconds) && nowSeconds >= _stateStartedAtSeconds - ClockEpsilon;
        }

        private bool CanPresent()
        {
            return _session.IsOpen
                && _session.CurrentOpenSessionLocation == RouteId.Stage2
                && _navigation.CurrentRoute == RouteId.Stage2
                && !_navigation.IsLocked
                && _navigation.Recovery == null;
        }

        private bool CanPause()
        {
            return State == Stage2State.Intro
                || State == Stage2State.Choosing
                || State == Stage2State.Inspecting
                || State == Stage2State.ConsequencePlaying
                || State == Stage2State.Help
                || State == Stage2State.Leaving;
        }

        private bool CanLeave()
        {
            return State == Stage2State.Intro
                || State == Stage2State.Choosing
                || State == Stage2State.Inspecting
                || State == Stage2State.ConsequencePlaying;
        }

        private static bool IsRestorable(Stage2State state)
        {
            return state == Stage2State.Intro
                || state == Stage2State.Choosing
                || state == Stage2State.Inspecting
                || state == Stage2State.ConsequencePlaying
                || state == Stage2State.Help
                || state == Stage2State.Leaving;
        }

        private void RestoreSnapshot(Stage2Snapshot snapshot, double capturedAtSeconds, double nowSeconds, bool reanchorTimedState)
        {
            State = snapshot.State;
            _selectedRiver = snapshot.SelectedRiver;
            _lowObserved = snapshot.LowObserved;
            _middleObserved = snapshot.MiddleObserved;
            _highObserved = snapshot.HighObserved;
            _currentHotspot = snapshot.CurrentHotspot;
            CurrentConsequenceTokenId = snapshot.ConsequenceTokenId;
            CurrentInstructionTokenId = snapshot.InstructionTokenId;
            _stateStartedAtSeconds = snapshot.StateStartedAtSeconds;
            _consequenceElapsedSeconds = snapshot.ConsequenceElapsedSeconds;
            _focusedControlId = snapshot.FocusedControlId;
            _optionCount = snapshot.OptionCount;
            _completionRecorded = snapshot.CompletionRecorded;
            if (reanchorTimedState && State == Stage2State.ConsequencePlaying)
                _stateStartedAtSeconds = nowSeconds - _consequenceElapsedSeconds;
            Present(State != Stage2State.Help && State != Stage2State.Leaving);
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
                throw new InvalidOperationException("Stage 2 tokens must be non-empty and unique.");
            return token;
        }

        private static Stage2ObservedHotspots Flag(Stage2Hotspot hotspot)
        {
            switch (hotspot)
            {
                case Stage2Hotspot.Field: return Stage2ObservedHotspots.Field;
                case Stage2Hotspot.Path: return Stage2ObservedHotspots.Path;
                case Stage2Hotspot.Boat: return Stage2ObservedHotspots.Boat;
                default: throw new ArgumentOutOfRangeException(nameof(hotspot));
            }
        }

        private static string RiverToken(Stage2River river)
        {
            switch (river)
            {
                case Stage2River.Low: return "low";
                case Stage2River.Middle: return "middle";
                case Stage2River.High: return "high";
                default: throw new ArgumentOutOfRangeException(nameof(river));
            }
        }

        private static string HotspotToken(Stage2Hotspot hotspot)
        {
            switch (hotspot)
            {
                case Stage2Hotspot.Field: return "field";
                case Stage2Hotspot.Path: return "path";
                case Stage2Hotspot.Boat: return "boat";
                default: throw new ArgumentOutOfRangeException(nameof(hotspot));
            }
        }

        private static bool IsRiver(Stage2River river) => river == Stage2River.Low || river == Stage2River.Middle || river == Stage2River.High;
        private static bool IsHotspot(Stage2Hotspot hotspot) => hotspot == Stage2Hotspot.Field || hotspot == Stage2Hotspot.Path || hotspot == Stage2Hotspot.Boat;
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool IsValidClock(double value) => IsFinite(value) && value >= 0d;
    }
}
