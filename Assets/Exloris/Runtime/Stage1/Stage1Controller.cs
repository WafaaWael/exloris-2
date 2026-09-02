using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;

namespace Exloris.Runtime.Stage1
{
    public sealed class Stage1Controller
    {
        public const double MinimumPosition = 0d;
        public const double InclusiveLow = 0.25d;
        public const double InitialPosition = 0.5d;
        public const double InclusiveHigh = 0.75d;
        public const double MaximumPosition = 1d;
        public const double ArrowStep = 0.25d;
        public const double OutcomeSeconds = 3.2d;
        private const double ClockEpsilon = 0.000000001d;

        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IStage1View _view;
        private readonly IStage1PauseSink _pauseSink;
        private readonly ILeaveConfirmationSink _leaveSink;
        private readonly Func<Guid> _tokenSource;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();
        private readonly HashSet<Guid> _consumedInteractions = new HashSet<Guid>();

        private bool _reducedMotion;
        private bool _markerAttached;
        private double _markerPosition;
        private int _supportTier;
        private int _optionCount;
        private Stage1OutcomeKind _outcome;
        private double _stateStartedAtSeconds;
        private double _outcomeElapsedSeconds;
        private string _focusedControlId;
        private bool _outcomeFactRecorded;
        private bool _completionRecorded;
        private Guid _pendingTapInteractionId;
        private double _pauseCapturedAtSeconds;
        private double _leaveCapturedAtSeconds;
        private Stage1Snapshot _helpSnapshot;
        private Stage1Snapshot _pauseSnapshot;
        private Stage1Snapshot _leaveSnapshot;

        public Stage1Controller(
            SessionState session,
            NavigationAuthority navigation,
            IStage1View view,
            IStage1PauseSink pauseSink,
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

        public Stage1State State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public Guid CurrentOutcomeTokenId { get; private set; }
        public Guid CurrentInstructionTokenId { get; private set; }
        public Stage1Snapshot Snapshot => new Stage1Snapshot(
            CurrentTokenId,
            State,
            _markerAttached,
            _markerPosition,
            _supportTier,
            _optionCount,
            _outcome,
            CurrentOutcomeTokenId,
            CurrentInstructionTokenId,
            _stateStartedAtSeconds,
            _outcomeElapsedSeconds,
            _focusedControlId,
            _outcomeFactRecorded,
            _completionRecorded);

        public bool Enter(Guid transactionId, double nowSeconds, bool reducedMotion)
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != Stage1State.Hidden
                || transaction == null
                || transaction.Id != transactionId
                || transaction.Caller != RouteId.Boat1
                || transaction.Destination != RouteId.Stage1
                || _navigation.CurrentRoute != RouteId.Boat1
                || !_session.IsOpen
                || _session.CurrentOpenSessionLocation != RouteId.Boat1
                || _navigation.Recovery != null
                || !IsValidClock(nowSeconds))
                return false;

            CurrentTokenId = NextToken();
            CurrentInstructionTokenId = NextToken();
            CurrentOutcomeTokenId = Guid.Empty;
            _markerAttached = false;
            _markerPosition = InitialPosition;
            _supportTier = 0;
            _optionCount = 0;
            _outcome = Stage1OutcomeKind.None;
            _stateStartedAtSeconds = nowSeconds;
            _outcomeElapsedSeconds = 0d;
            _focusedControlId = null;
            _outcomeFactRecorded = false;
            _completionRecorded = false;
            _pendingTapInteractionId = Guid.Empty;
            _reducedMotion = reducedMotion;

            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed)
                return false;
            if (!_session.MoveTo(RouteId.Stage1))
                throw new InvalidOperationException("A prevalidated Stage 1 session failed to advance.");

            State = Stage1State.Intro;
            Present(true);
            return true;
        }

        public bool HandlePlay(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage1State.Intro || !CanActAt(nowSeconds)) return false;
            State = Stage1State.Playing;
            _stateStartedAtSeconds = nowSeconds;
            CurrentInstructionTokenId = NextToken();
            Present(true);
            return true;
        }

        public bool HandleRepeat(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || !CanRepeat() || !CanActAt(nowSeconds)) return false;
            CurrentInstructionTokenId = NextToken();
            Present(true);
            return true;
        }

        public bool HandleHelp(Guid tokenId)
        {
            if (!IsCurrent(tokenId) || !CanHelp() || !CanPresent()) return false;
            _helpSnapshot = Snapshot;
            State = Stage1State.Help;
            Present(false);
            return true;
        }

        public bool RestoreFromHelp(Stage1Snapshot snapshot)
        {
            if (State != Stage1State.Help || !snapshot.Equals(_helpSnapshot) || !IsRestorable(snapshot.State) || !CanPresent())
                return false;
            RestoreSnapshot(snapshot, 0d, 0d, false);
            return true;
        }

        public bool BeginTapAttach(Guid tokenId, Guid interactionId, bool markerHit, double nowSeconds)
        {
            if (!CanAttach(tokenId, interactionId, nowSeconds)
                || !markerHit
                || _pendingTapInteractionId != Guid.Empty)
                return false;
            _pendingTapInteractionId = interactionId;
            return true;
        }

        public bool CompleteTapAttach(Guid tokenId, Guid interactionId, bool socketHit, double nowSeconds)
        {
            if (!CanAttach(tokenId, interactionId, nowSeconds)
                || !socketHit
                || _pendingTapInteractionId != interactionId)
                return false;
            _pendingTapInteractionId = Guid.Empty;
            _consumedInteractions.Add(interactionId);
            Attach(nowSeconds);
            return true;
        }

        public bool HandleDragAttach(
            Guid tokenId,
            Guid interactionId,
            bool markerOrigin,
            bool socketHit,
            double nowSeconds)
        {
            if (!CanAttach(tokenId, interactionId, nowSeconds) || !markerOrigin || !socketHit) return false;
            _consumedInteractions.Add(interactionId);
            _pendingTapInteractionId = Guid.Empty;
            Attach(nowSeconds);
            return true;
        }

        public Stage1MoveResult HandleArrow(Guid tokenId, int direction, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage1State.Playing || (direction != -1 && direction != 1) || !CanActAt(nowSeconds))
                return Stage1MoveResult.Rejected;
            if (!_markerAttached) return PresentAttachCue();

            var next = Clamp(_markerPosition + direction * ArrowStep);
            if (next.Equals(_markerPosition)) return Stage1MoveResult.AtBoundary;
            _markerPosition = next;
            _stateStartedAtSeconds = nowSeconds;
            Present(true);
            return Stage1MoveResult.Moved;
        }

        public Stage1MoveResult HandleDrag(Guid tokenId, double normalizedPosition, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage1State.Playing || !IsFinite(normalizedPosition) || !CanActAt(nowSeconds))
                return Stage1MoveResult.Rejected;
            if (!_markerAttached) return PresentAttachCue();

            var next = Clamp(normalizedPosition);
            if (next.Equals(_markerPosition)) return Stage1MoveResult.AtBoundary;
            _markerPosition = next;
            _stateStartedAtSeconds = nowSeconds;
            Present(true);
            return Stage1MoveResult.Moved;
        }

        public bool HandleConfirm(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage1State.Playing || !_markerAttached || !CanActAt(nowSeconds)) return false;
            _outcome = Classify(_markerPosition);
            CurrentOutcomeTokenId = NextToken();
            _optionCount++;
            _outcomeElapsedSeconds = 0d;
            _outcomeFactRecorded = false;
            _stateStartedAtSeconds = nowSeconds;
            State = Stage1State.OutcomePlaying;
            Present(true);
            return true;
        }

        public bool AdvanceOutcome(Guid tokenId, Guid outcomeTokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage1State.OutcomePlaying
                || outcomeTokenId == Guid.Empty
                || outcomeTokenId != CurrentOutcomeTokenId
                || !CanActAt(nowSeconds))
                return false;

            _outcomeElapsedSeconds = Math.Min(OutcomeSeconds, nowSeconds - _stateStartedAtSeconds);
            if (_outcomeElapsedSeconds < OutcomeSeconds - ClockEpsilon)
            {
                Present(true);
                return true;
            }

            _outcomeElapsedSeconds = OutcomeSeconds;
            State = Stage1State.OutcomeReady;
            if (!_outcomeFactRecorded)
            {
                _session.Record(ObservationEvent.ChoiceOutcome(
                    ObservationEventType.Stage1Outcome,
                    "choice_marker_position",
                    OutcomeObservationToken(_outcome)));
                _outcomeFactRecorded = true;
            }
            Present(true);
            return true;
        }

        public bool HandleTryAgain(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage1State.OutcomeReady
                || (_outcome != Stage1OutcomeKind.OutsideLow && _outcome != Stage1OutcomeKind.OutsideHigh)
                || !CanActAt(nowSeconds))
                return false;

            _supportTier = Math.Min(3, _supportTier + 1);
            if (_supportTier == 3)
                _markerPosition = _outcome == Stage1OutcomeKind.OutsideLow ? MinimumPosition : MaximumPosition;
            _outcome = Stage1OutcomeKind.None;
            CurrentOutcomeTokenId = Guid.Empty;
            CurrentInstructionTokenId = NextToken();
            _outcomeElapsedSeconds = 0d;
            _outcomeFactRecorded = false;
            _stateStartedAtSeconds = nowSeconds;
            State = Stage1State.Playing;
            Present(true);
            return true;
        }

        public bool HandleWatchAgain(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage1State.OutcomeReady || _outcome == Stage1OutcomeKind.None || !CanActAt(nowSeconds))
                return false;
            CurrentOutcomeTokenId = NextToken();
            _outcomeElapsedSeconds = 0d;
            _stateStartedAtSeconds = nowSeconds;
            State = Stage1State.OutcomePlaying;
            Present(true);
            return true;
        }

        public bool HandleContinue(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage1State.OutcomeReady
                || _outcome != Stage1OutcomeKind.InRange
                || _completionRecorded
                || !CanActAt(nowSeconds))
                return false;

            var request = _navigation.Request(RouteId.Boat2, nowSeconds);
            if (!request.Accepted) return false;
            _session.Record(ObservationEvent.OptionsTried(ObservationEventType.Stage1Option, "stage_1", _optionCount));
            _session.Record(ObservationEvent.CompletedStages("stage_1"));
            _completionRecorded = true;
            State = Stage1State.Navigating;
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
            State = Stage1State.Paused;
            Present(false);
            return true;
        }

        public bool RestoreFromPause(Stage1Snapshot snapshot, double nowSeconds)
        {
            if (State != Stage1State.Paused
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
            if (!_leaveSink.TryShow(RouteId.Stage1)) return false;
            _leaveSnapshot = captured;
            _leaveCapturedAtSeconds = nowSeconds;
            State = Stage1State.Leaving;
            Present(false);
            return true;
        }

        public bool RestoreFromLeave(Stage1Snapshot snapshot, double nowSeconds)
        {
            if (State != Stage1State.Leaving
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
            if (!IsCurrent(tokenId) || !CanPresent() || string.IsNullOrWhiteSpace(controlId) || controlId.Length > 64) return false;
            _focusedControlId = controlId;
            Present(State != Stage1State.Help && State != Stage1State.Paused && State != Stage1State.Leaving);
            return true;
        }

        public bool EnterRecovery()
        {
            if (State != Stage1State.Navigating || _navigation.Recovery == null) return false;
            State = Stage1State.Recovery;
            _view.SetInputReady(false);
            return true;
        }

        public bool RetryBoundary()
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != Stage1State.Recovery
                || transaction == null
                || transaction.Caller != RouteId.Stage1
                || transaction.Destination != RouteId.Boat2)
                return false;
            State = Stage1State.Navigating;
            _view.SetInputReady(false);
            return true;
        }

        public static Stage1OutcomeKind Classify(double normalizedPosition)
        {
            if (!IsFinite(normalizedPosition) || normalizedPosition < MinimumPosition || normalizedPosition > MaximumPosition)
                throw new ArgumentOutOfRangeException(nameof(normalizedPosition));
            if (normalizedPosition < InclusiveLow) return Stage1OutcomeKind.OutsideLow;
            if (normalizedPosition > InclusiveHigh) return Stage1OutcomeKind.OutsideHigh;
            return Stage1OutcomeKind.InRange;
        }

        private bool CanAttach(Guid tokenId, Guid interactionId, double nowSeconds)
        {
            return IsCurrent(tokenId)
                && State == Stage1State.Playing
                && !_markerAttached
                && interactionId != Guid.Empty
                && !_consumedInteractions.Contains(interactionId)
                && CanActAt(nowSeconds);
        }

        private void Attach(double nowSeconds)
        {
            _markerAttached = true;
            _stateStartedAtSeconds = nowSeconds;
            CurrentInstructionTokenId = NextToken();
            Present(true);
        }

        private Stage1MoveResult PresentAttachCue()
        {
            CurrentInstructionTokenId = NextToken();
            Present(true);
            return Stage1MoveResult.AttachCue;
        }

        private bool IsCurrent(Guid tokenId) => tokenId != Guid.Empty && tokenId == CurrentTokenId;

        private bool CanActAt(double nowSeconds)
        {
            return CanPresent() && IsValidClock(nowSeconds) && nowSeconds >= _stateStartedAtSeconds - ClockEpsilon;
        }

        private bool CanPresent()
        {
            return _session.IsOpen
                && _session.CurrentOpenSessionLocation == RouteId.Stage1
                && _navigation.CurrentRoute == RouteId.Stage1
                && !_navigation.IsLocked
                && _navigation.Recovery == null;
        }

        private bool CanRepeat()
        {
            return State == Stage1State.Intro || State == Stage1State.Playing || State == Stage1State.OutcomeReady;
        }

        private bool CanHelp()
        {
            return State == Stage1State.Intro
                || State == Stage1State.Playing
                || (State == Stage1State.OutcomeReady && _outcome != Stage1OutcomeKind.InRange);
        }

        private bool CanPause()
        {
            return State == Stage1State.Intro
                || State == Stage1State.Playing
                || State == Stage1State.OutcomePlaying
                || State == Stage1State.OutcomeReady
                || State == Stage1State.Help
                || State == Stage1State.Leaving;
        }

        private bool CanLeave()
        {
            return State == Stage1State.Intro
                || State == Stage1State.Playing
                || State == Stage1State.OutcomePlaying
                || State == Stage1State.OutcomeReady;
        }

        private static bool IsRestorable(Stage1State state)
        {
            return state == Stage1State.Intro
                || state == Stage1State.Playing
                || state == Stage1State.OutcomePlaying
                || state == Stage1State.OutcomeReady
                || state == Stage1State.Help
                || state == Stage1State.Leaving;
        }

        private void RestoreSnapshot(
            Stage1Snapshot snapshot,
            double capturedAtSeconds,
            double nowSeconds,
            bool reanchorTimedState)
        {
            State = snapshot.State;
            _markerAttached = snapshot.MarkerAttached;
            _markerPosition = snapshot.MarkerPosition;
            _supportTier = snapshot.SupportTier;
            _optionCount = snapshot.OptionCount;
            _outcome = snapshot.Outcome;
            CurrentOutcomeTokenId = snapshot.OutcomeTokenId;
            CurrentInstructionTokenId = snapshot.InstructionTokenId;
            _stateStartedAtSeconds = snapshot.StateStartedAtSeconds;
            _outcomeElapsedSeconds = snapshot.OutcomeElapsedSeconds;
            _focusedControlId = snapshot.FocusedControlId;
            _outcomeFactRecorded = snapshot.OutcomeFactRecorded;
            _completionRecorded = snapshot.CompletionRecorded;
            if (reanchorTimedState && State == Stage1State.OutcomePlaying)
                _stateStartedAtSeconds = nowSeconds - _outcomeElapsedSeconds;
            Present(State != Stage1State.Help && State != Stage1State.Leaving);
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
                throw new InvalidOperationException("Stage 1 tokens must be non-empty and unique.");
            return token;
        }

        private static string OutcomeObservationToken(Stage1OutcomeKind outcome)
        {
            switch (outcome)
            {
                case Stage1OutcomeKind.OutsideLow: return "outcome_outside_low";
                case Stage1OutcomeKind.InRange: return "outcome_in_range";
                case Stage1OutcomeKind.OutsideHigh: return "outcome_outside_high";
                default: throw new InvalidOperationException("Stage 1 outcome is not observable.");
            }
        }

        private static double Clamp(double value) => Math.Max(MinimumPosition, Math.Min(MaximumPosition, value));
        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool IsValidClock(double value) => IsFinite(value) && value >= 0d;
    }
}
